namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Settings;

/// <summary>
/// Deliberately plain - no guard interface here at all, same as <see cref="Preferences.PreferencesViewModel"/>:
/// the whole <c>/settings</c> subtree is guarded globally via <c>Meta["requiresAuth"]</c> (see
/// <see cref="Routing.AppRoutes"/>'s own remarks), inherited by every child under it since
/// <c>RouteMatcher</c> merges <c>Meta</c> root-to-leaf on every resolve.
/// </summary>
public sealed class SettingsShellViewModel;
