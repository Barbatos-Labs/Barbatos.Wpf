// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// The first of three <b>global</b> <see cref="IStoreActionInterceptor"/>s registered in
/// <see cref="WpfProgram.CreateWpfApp"/> (see also <see cref="ApsuTimingWarningInterceptor"/>,
/// <see cref="ApsuAuditTrailInterceptor"/>) - together with three <b>local</b> ones
/// (<see cref="UserIdValidationInterceptor"/>, <see cref="CheckoutCooldownInterceptor"/>,
/// <see cref="NonEmptyCartGuardInterceptor"/>), this is the 3-global-plus-3-local chain-ordering
/// scenario this session's own tests verify. Wraps every <see cref="ActionAttribute"/>-marked
/// method on <i>every</i> store (both <see cref="UserStore"/> and <see cref="OrderStore"/>)
/// automatically. Reports into the shared <see cref="ApsuActivityLog"/> - see that type's own
/// remarks for why every interceptor reports there instead of exposing its own event.
/// </summary>
public sealed class ApsuActionLoggingInterceptor : IStoreActionInterceptor
{
    readonly ApsuActivityLog _activityLog;

    public ApsuActionLoggingInterceptor(ApsuActivityLog activityLog) => _activityLog = activityLog;

    public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        var label = $"{context.Store.GetType().Name}.{context.ActionName}()";
        _activityLog.Log($"[Global interceptor #1 - logging] {label} starting");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await next();
            _activityLog.Log($"[Global interceptor #1 - logging] {label} finished in {stopwatch.Elapsed.TotalMilliseconds:0}ms");
            return result;
        }
        catch (Exception ex)
        {
            _activityLog.Log($"[Global interceptor #1 - logging] {label} failed: {ex.Message}");
            throw; // still propagates - this is an observation, not a handler.
        }
    }
}
