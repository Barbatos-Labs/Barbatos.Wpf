// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// A third <b>local</b> <see cref="IStoreActionInterceptor"/> (see <see cref="UserIdValidationInterceptor"/>,
/// local to <see cref="UserStore"/>, and <see cref="CheckoutCooldownInterceptor"/>, the other one
/// local to <see cref="OrderStore"/>) - registered for <see cref="OrderStore"/> too, so
/// <see cref="OrderStore.CheckoutAsync"/> specifically runs through <i>two</i> local interceptors
/// stacked on top of each other (this one, then <see cref="CheckoutCooldownInterceptor"/>), on
/// top of all three global ones - demonstrating that local interceptors compose with each other
/// exactly like global ones do, not just with the global layer.
/// </summary>
/// <remarks>
/// State-based (like <see cref="CheckoutCooldownInterceptor"/>), but on a different property -
/// reads <see cref="OrderStore.OrderCount"/> off <see cref="StoreActionContext.Store"/> rather
/// than tracking its own timer.
/// </remarks>
public sealed class NonEmptyCartGuardInterceptor : IStoreActionInterceptor
{
    readonly ApsuActivityLog _activityLog;

    public NonEmptyCartGuardInterceptor(ApsuActivityLog activityLog) => _activityLog = activityLog;

    public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        if (context.ActionName == nameof(OrderStore.CheckoutAsync) && context.Store is OrderStore { OrderCount: 0 })
        {
            _activityLog.Log("[Local interceptor] Checkout blocked - OrderStore.OrderCount is 0, nothing to check out (short-circuited)");
            return Task.FromResult<object?>(null);
        }

        return next();
    }
}
