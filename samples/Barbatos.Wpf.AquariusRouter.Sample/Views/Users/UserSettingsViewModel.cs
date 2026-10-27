using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Users;

/// <summary>
/// The sample's leave+update-guard demo: <see cref="OnBeforeRouteLeave"/> and
/// <see cref="OnBeforeRouteUpdate"/> both ask for confirmation via a plain <see cref="MessageBox"/>
/// whenever <see cref="Nickname"/> no longer matches what was loaded, the same "unsaved changes"
/// scenario Vue Router's own <c>beforeRouteLeave</c>/<c>beforeRouteUpdate</c> docs use. Both hooks
/// matter here for different reasons: leaving covers navigating to an entirely different route,
/// but this route (<c>/users/:id/settings</c>) also re-fires an *update* - not a leave+enter -
/// whenever you switch straight from one user's settings to another's, since the same
/// <c>UserSettingsViewModel</c> instance stays mounted for the shared parent path. Without its own
/// <see cref="OnBeforeRouteUpdate"/>, unsaved text would be silently discarded on that switch.
/// </summary>
/// <remarks>
/// <b>Deliberately no <c>IOnRouteUpdate</c>.</b> The router runs every updating record's
/// <c>IOnRouteUpdate</c> before any <c>IOnBeforeRouteUpdate</c>, so reloading there would overwrite
/// the edit - and reset what it is compared against - before the update guard could see it, and
/// the prompt would never appear. The next user's data is loaded inside the update guard instead,
/// once the user has agreed to discard the edit (or there was none).
/// </remarks>
public sealed partial class UserSettingsViewModel : ObservableObject, IOnRouteEnter, IOnBeforeRouteLeave, IOnBeforeRouteUpdate
{
    private string _savedNickname = "";

    [ObservableProperty]
    private string _userId = "";

    [ObservableProperty]
    private string _nickname = "";

    public void OnRouteEnter(RouteLocation to, RouteLocation from) => Load(to);

    public Task<NavigationGuardResult> OnBeforeRouteLeave(RouteLocation to, RouteLocation from) =>
        Task.FromResult<NavigationGuardResult>(ConfirmDiscardUnsavedChanges());

    public Task<NavigationGuardResult> OnBeforeRouteUpdate(RouteLocation to, RouteLocation from)
    {
        if (!ConfirmDiscardUnsavedChanges())
            return Task.FromResult(NavigationGuardResult.Cancel);

        // A later stage (this sample's global BeforeResolve, or a newer navigation) could still
        // stop this one after the load; an app where that matters reloads once the route has
        // actually changed instead, for example from a Watch on Router.CurrentRoute.
        Load(to);
        return Task.FromResult(NavigationGuardResult.Continue);
    }

    private void Load(RouteLocation to)
    {
        UserId = (string)to.Params["id"]!;
        Nickname = _savedNickname = $"user-{UserId}";
    }

    private bool ConfirmDiscardUnsavedChanges()
    {
        if (Nickname == _savedNickname)
            return true;

        var result = MessageBox.Show(
            "You have unsaved changes to the nickname. Leave anyway?",
            "Unsaved changes",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }
}
