// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// A <b>local</b> <see cref="IStoreActionInterceptor"/> - registered in
/// <see cref="WpfProgram.CreateWpfApp"/> via <c>AddStoreInterceptor&lt;UserStore&gt;(...)</c>, so
/// it only ever wraps <see cref="UserStore"/>'s actions. Blocks <see cref="UserStore.SelectUser"/>
/// when its own <c>userId</c> argument is outside JSONPlaceholder's actual user id range (1-10),
/// reading it straight from <see cref="StoreActionContext.Arguments"/> - the argument-based
/// counterpart to <see cref="CheckoutCooldownInterceptor"/>/<see cref="NonEmptyCartGuardInterceptor"/>'s
/// state-based short-circuits.
/// </summary>
public sealed class UserIdValidationInterceptor : IStoreActionInterceptor
{
    readonly ApsuActivityLog _activityLog;

    public UserIdValidationInterceptor(ApsuActivityLog activityLog) => _activityLog = activityLog;

    public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        if (context.ActionName == nameof(UserStore.SelectUser)
            && context.Arguments.Count > 0
            && context.Arguments[0] is int userId
            && (userId < 1 || userId > 10))
        {
            _activityLog.Log($"[Local interceptor] SelectUser({userId}) blocked - JSONPlaceholder only has users 1-10 (short-circuited: SelectedUserId was never touched, so OrderStore's watch never fired either)");
            return Task.FromResult<object?>(null);
        }

        return next();
    }
}
