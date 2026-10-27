// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// A third <b>global</b> <see cref="IStoreActionInterceptor"/> - registered after
/// <see cref="ApsuActionLoggingInterceptor"/>/<see cref="ApsuTimingWarningInterceptor"/> in
/// <see cref="WpfProgram.CreateWpfApp"/>, so a call to any action on any store passes through all
/// three global interceptors (in that order) before ever reaching a store's own local ones. Reads
/// <see cref="StoreActionContext.Arguments"/> too, not just the action's name - an audit trail
/// needs to know what a call actually did, not just that it happened.
/// </summary>
public sealed class ApsuAuditTrailInterceptor : IStoreActionInterceptor
{
    readonly ApsuActivityLog _activityLog;

    public ApsuAuditTrailInterceptor(ApsuActivityLog activityLog) => _activityLog = activityLog;

    /// <summary>Every action call recorded so far, oldest first.</summary>
    public List<string> Trail { get; } = new();

    public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        var arguments = string.Join(", ", context.Arguments);
        // BaseType: context.Store is the generated proxy subclass, not the store's own type.
        var entry = $"{DateTimeOffset.Now:T} {context.Store.GetType().BaseType!.Name}.{context.ActionName}({arguments})";
        Trail.Add(entry);
        _activityLog.Log($"[Global interceptor #3 - audit] recorded: {entry}");
        return next();
    }
}
