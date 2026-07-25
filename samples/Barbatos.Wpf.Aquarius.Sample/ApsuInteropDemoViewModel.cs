// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;
using Barbatos.Wpf.Aquarius.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="ApsuInteropDemoView"/> - combines two Barbatos.Wpf.Apsu stores
/// (<see cref="UserStore"/>/<see cref="OrderStore"/>, DI-resolved via <see cref="WpfProgram.CreateWpfApp"/>,
/// the same as every other service <see cref="MainViewModel"/> hands to a child demo) with
/// Aquarius's own <see cref="Ref{T}"/>/<see cref="Watch"/>: <see cref="UserIdInput"/> is
/// ordinary view-local Aquarius state (a text box's value), while the stores are shared/global -
/// the split a real app mixing both packages would have. Also depends on
/// <see cref="ApsuActivityLog"/>, the single shared sink all six registered
/// <see cref="IStoreActionInterceptor"/>s report into (3 global -
/// <see cref="ApsuActionLoggingInterceptor"/>, <see cref="ApsuTimingWarningInterceptor"/>,
/// <see cref="ApsuAuditTrailInterceptor"/> - and 3 local - <see cref="UserIdValidationInterceptor"/>
/// on <see cref="UserStore"/>; <see cref="CheckoutCooldownInterceptor"/> and
/// <see cref="NonEmptyCartGuardInterceptor"/>, both on <see cref="OrderStore"/> - see
/// <see cref="WpfProgram.CreateWpfApp"/> for how all six are registered and in what order they
/// chain). This class never references any of the six interceptors directly - one dependency on
/// the shared sink is enough, the same way a Vue component using a Pinia store never needs to
/// know which plugins are registered on it.
/// </summary>
public sealed partial class ApsuInteropDemoViewModel : ObservableObject
{
    readonly Action<string> _log;

    /// <summary>Global state - see <see cref="UserStore"/>'s own remarks for the cross-store scenarios it demonstrates.</summary>
    public UserStore UserStore { get; }

    /// <summary>Global state - depends on <see cref="UserStore"/>; see <see cref="OrderStore"/>'s own remarks.</summary>
    public OrderStore OrderStore { get; }

    /// <summary>Ordinary Aquarius view-local state (<c>ref(1)</c>) - not part of either store. A
    /// JSONPlaceholder user id (valid range 1-10).</summary>
    public Ref<int> UserIdInput { get; } = new(1);

    public ApsuInteropDemoViewModel(UserStore userStore, OrderStore orderStore, ApsuActivityLog activityLog, Action<string> log)
    {
        _log = log;
        UserStore = userStore;
        OrderStore = orderStore;

        // Both stores are plain INotifyPropertyChanged underneath (like Aquarius's own Ref/Computed),
        // so they compose into the same activity log the same way UserIdInput's Watch does below.
        UserStore.PropertyChanged += (_, args) => _log($"UserStore.{args.PropertyName} changed (Barbatos.Wpf.Apsu)");
        OrderStore.PropertyChanged += (_, args) => _log($"OrderStore.{args.PropertyName} changed (Barbatos.Wpf.Apsu)");
        Watch.On(UserIdInput, (newValue, oldValue) => _log($"UserIdInput changed {oldValue} -> {newValue} (Aquarius ref, local to this view)"));

        // One subscription covers all six interceptors - each reports into this same shared
        // sink instead of exposing its own event (see ApsuActivityLog's own remarks for why).
        activityLog.Logged += _log;
    }

    [RelayCommand]
    void SelectUser() => UserStore.SelectUser(UserIdInput.Value);

    /// <summary>Deliberately calls <see cref="UserStore.SelectUser"/> with an out-of-range id, to
    /// exercise <see cref="UserIdValidationInterceptor"/>'s short-circuit without needing the user
    /// to type an invalid value in the text box first.</summary>
    [RelayCommand]
    void SelectInvalidUser() => UserStore.SelectUser(99);

    [RelayCommand]
    async Task RefreshOrdersAsync() => await OrderStore.RefreshOrdersAsync();

    [RelayCommand]
    async Task CheckoutAsync() => await OrderStore.CheckoutAsync();

    [RelayCommand]
    void ResetAll()
    {
        UserStore.Reset();
        OrderStore.Reset();
    }
}
