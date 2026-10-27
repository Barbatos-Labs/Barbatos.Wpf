// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Core.Sample;

/// <summary>
/// Demonstrates <see cref="IStoreActionInterceptor"/> as global middleware: logs every
/// <see cref="ActionAttribute"/>-marked action call on every store, across the whole app, with no
/// per-store or per-action wiring beyond registering this one interceptor in
/// <see cref="WpfProgram.CreateWpfApp"/>. Reports into the shared <see cref="SampleActivityLog"/> -
/// see that type's own remarks for why, and for how an unrelated unhandled dispatcher exception
/// (not just a failed store action) ends up sharing the same log.
/// </summary>
public sealed class SampleActionLoggingInterceptor : IStoreActionInterceptor
{
    readonly SampleActivityLog _activityLog;

    public SampleActionLoggingInterceptor(SampleActivityLog activityLog) => _activityLog = activityLog;

    public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        // context.Store is the Castle proxy subclass the container hands out (an interceptor only
        // ever sees a proxied store), so the store's own type is its BaseType.
        var label = $"{context.Store.GetType().BaseType!.Name}.{context.ActionName}()";
        _activityLog.Log($"[Interceptor] {label} starting");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await next();
            _activityLog.Log($"[Interceptor] {label} finished in {stopwatch.Elapsed.TotalMilliseconds:0}ms");
            return result;
        }
        catch (Exception ex)
        {
            _activityLog.Log($"[Interceptor] {label} failed: {ex.Message}");
            throw; // still propagates - to the caller, and from there potentially all the way to
                   // DispatcherUnhandledException, exactly like any other unhandled exception would.
        }
    }
}
