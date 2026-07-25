// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// A <b>local</b> <see cref="IStoreActionInterceptor"/> - registered in
/// <see cref="WpfProgram.CreateWpfApp"/> via <c>AddStoreInterceptor&lt;OrderStore&gt;(...)</c>,
/// so it only ever wraps <see cref="OrderStore"/>'s actions, never <see cref="UserStore"/>'s (even
/// though the same <see cref="ApsuActionLoggingInterceptor"/> above wraps both, globally). Narrows
/// itself further, to just <see cref="OrderStore.CheckoutAsync"/>, since a store can have several
/// actions but this interceptor is only registered once per store, not once per action. Stacked
/// with (registered right after, so nested inside) <see cref="NonEmptyCartGuardInterceptor"/>,
/// the other local interceptor on <see cref="OrderStore"/>.
/// </summary>
/// <remarks>
/// Demonstrates genuine short-circuiting - the real <see cref="OrderStore.CheckoutAsync"/> body
/// (and therefore <see cref="UserStore.RefreshProfileAsync"/>, which it calls) never runs at all
/// while the cooldown is active, because <c>next</c> is simply never called. This is the one
/// capability Pinia's own separate <c>$onAction</c> before/after/error callbacks cannot do -
/// they can only observe, not prevent - but this middleware shape can.
/// </remarks>
public sealed class CheckoutCooldownInterceptor : IStoreActionInterceptor
{
    readonly TimeSpan _cooldown;
    readonly ApsuActivityLog _activityLog;
    DateTimeOffset _lastCheckoutAt = DateTimeOffset.MinValue;

    public CheckoutCooldownInterceptor(TimeSpan cooldown, ApsuActivityLog activityLog)
    {
        _cooldown = cooldown;
        _activityLog = activityLog;
    }

    public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        if (context.ActionName != nameof(OrderStore.CheckoutAsync))
            return next();

        var now = DateTimeOffset.UtcNow;
        var remaining = _cooldown - (now - _lastCheckoutAt);
        if (remaining > TimeSpan.Zero)
        {
            _activityLog.Log($"[Local interceptor] Checkout blocked - {remaining.TotalSeconds:0.0}s cooldown left (short-circuited: CheckoutAsync's body, and its call into RefreshProfileAsync, never ran)");
            return Task.FromResult<object?>(null);
        }

        _lastCheckoutAt = now;
        return next();
    }
}
