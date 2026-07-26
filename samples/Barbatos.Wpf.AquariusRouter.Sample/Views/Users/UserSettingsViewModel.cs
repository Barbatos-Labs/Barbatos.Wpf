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
public sealed partial class UserSettingsViewModel : ObservableObject, IOnRouteEnter, IOnRouteUpdate, IOnBeforeRouteLeave, IOnBeforeRouteUpdate
{
    private string _savedNickname = "";

    [ObservableProperty]
    private string _userId = "";

    [ObservableProperty]
    private string _nickname = "";

    public void OnRouteEnter(RouteLocation to, RouteLocation from)
    {
        UserId = (string)to.Params["id"]!;
        Nickname = _savedNickname = $"user-{UserId}";
    }

    public void OnRouteUpdate(RouteLocation to, RouteLocation from) => OnRouteEnter(to, from);

    public Task<NavigationGuardResult> OnBeforeRouteLeave(RouteLocation to, RouteLocation from) =>
        ConfirmDiscardUnsavedChanges();

    public Task<NavigationGuardResult> OnBeforeRouteUpdate(RouteLocation to, RouteLocation from) =>
        ConfirmDiscardUnsavedChanges();

    private Task<NavigationGuardResult> ConfirmDiscardUnsavedChanges()
    {
        if (Nickname == _savedNickname)
            return Task.FromResult(NavigationGuardResult.Continue);

        var result = MessageBox.Show(
            "You have unsaved changes to the nickname. Leave anyway?",
            "Unsaved changes",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return Task.FromResult<NavigationGuardResult>(result == MessageBoxResult.Yes);
    }
}
