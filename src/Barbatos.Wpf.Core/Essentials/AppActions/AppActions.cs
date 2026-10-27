// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The AppActions API lets you create and respond to app shortcuts from the taskbar icon.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IAppActions</c>.</remarks>
public interface IAppActions
{
    /// <summary>
    /// Gets if app actions are supported on this device.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Retrieves all the currently available <see cref="AppAction"/> instances.
    /// </summary>
    /// <returns>A collection of <see cref="AppAction"/> available for this app.</returns>
    Task<IEnumerable<AppAction>> GetAsync();

    /// <summary>
    /// Sets the app actions that will be available for this app.
    /// </summary>
    /// <param name="actions">A collection of <see cref="AppAction"/> that is to be set for this app.</param>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    /// <remarks>
    /// The Jump List itself outlives the process, but recognising a click does not: the
    /// relaunched process matches the clicked id only against the actions set <i>in that
    /// process</i> before its startup lifecycle event. An action set only here, at runtime,
    /// is shown in the Jump List yet raises nothing when clicked after a restart - declare every
    /// action at startup (<c>ConfigureEssentials(e =&gt; e.AddAppAction(...))</c>), and call
    /// this only to replace the list with one that is declared the same way on the next launch.
    /// </remarks>
    Task SetAsync(IEnumerable<AppAction> actions);

    /// <summary>
    /// Event triggered when an app action is activated.
    /// </summary>
    /// <remarks>
    /// On Windows a Jump List click starts a new process, and this event is raised once, from
    /// that process's <c>WpfLifecycle.OnStartup</c> event - inside
    /// <c>WpfApplication.OnStartup</c>, before an <c>App.OnStartup</c> override's code after
    /// <c>base.OnStartup(e)</c> runs and before any main window exists. Subscribe through
    /// <c>ConfigureEssentials(e =&gt; e.OnAppAction(...))</c> (or before <c>base.OnStartup</c>),
    /// record the action, and act on it once the main window is shown. With
    /// <c>ConfigureSingleInstance</c> and an instance already running, the new process exits
    /// inside <c>Build()</c>, before that event: the running instance only receives
    /// <c>ISingleInstanceService.SecondInstanceLaunched</c>, which carries no arguments, so the
    /// action is lost and only cold-start activations are reported.
    /// </remarks>
    event EventHandler<AppActionEventArgs>? AppActionActivated;
}

/// <summary>
/// The AppActions API lets you create and respond to app shortcuts from the taskbar icon.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>AppActions</c>. On Windows, app actions are
/// implemented with the taskbar Jump List (<see cref="System.Windows.Shell.JumpList"/>) — the
/// menu shown when a user right-clicks (or long-presses) the app's taskbar icon. This mirrors
/// .NET MAUI's own Windows implementation, which is also backed by a Jump List
/// (<c>Windows.UI.StartScreen.JumpList</c>). Two limits follow from a click relaunching the
/// executable: an activation is not reported while the app is already running under
/// <c>ConfigureSingleInstance</c>, and an action added only at runtime with
/// <see cref="SetAsync(IEnumerable{AppAction})"/> is not recognised after a restart - see
/// <see cref="IAppActions.AppActionActivated"/> and <see cref="IAppActions.SetAsync"/>.
/// </remarks>
public static class AppActions
{
    /// <inheritdoc cref="IAppActions.IsSupported" />
    public static bool IsSupported
        => Current.IsSupported;

    /// <inheritdoc cref="IAppActions.GetAsync" />
    public static Task<IEnumerable<AppAction>> GetAsync()
        => Current.GetAsync();

    /// <summary>
    /// Sets the app actions that will be available for this app.
    /// </summary>
    /// <param name="actions"><see cref="AppAction"/> objects that will be set for this app.</param>
    /// <returns>A <see cref="Task"/> object with the current status of the asynchronous operation.</returns>
    public static Task SetAsync(params AppAction[] actions)
        => Current.SetAsync(actions);

    /// <inheritdoc cref="IAppActions.SetAsync(IEnumerable{AppAction})" />
    public static Task SetAsync(IEnumerable<AppAction> actions)
        => Current.SetAsync(actions);

    /// <inheritdoc cref="IAppActions.AppActionActivated" />
    public static event EventHandler<AppActionEventArgs>? OnAppAction
    {
        add => Current.AppActionActivated += value;
        remove => Current.AppActionActivated -= value;
    }

    static IAppActions? currentImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IAppActions Current =>
        currentImplementation ??= new AppActionsImplementation();

    internal static void SetCurrent(IAppActions? implementation) =>
        currentImplementation = implementation;
}

/// <summary>
/// Event arguments containing data that is used when the app started through an <see cref="AppAction"/>.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>AppActionEventArgs</c>.</remarks>
public class AppActionEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppActionEventArgs"/> class.
    /// </summary>
    /// <param name="appAction">The <see cref="AppAction"/> that triggered this event.</param>
    public AppActionEventArgs(AppAction appAction)
        => AppAction = appAction;

    /// <summary>
    /// Gets the <see cref="AppAction"/> that triggered this event.
    /// </summary>
    public AppAction AppAction { get; }
}

/// <summary>
/// The <see cref="AppAction"/> class lets you create and respond to app shortcuts from the taskbar icon.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>AppAction</c>.</remarks>
public class AppAction
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppAction"/> class.
    /// </summary>
    /// <param name="id">A unique identifier used to respond to the action tap.</param>
    /// <param name="title">The visible title to display on the taskbar Jump List.</param>
    /// <param name="subtitle">If supported, a sub-title to display under the title.</param>
    /// <param name="icon">The path of an <c>.ico</c> file to show next to the title.</param>
    /// <exception cref="ArgumentNullException">Thrown when either <paramref name="id"/> or <paramref name="title"/> is <see langword="null"/>.</exception>
    public AppAction(string id, string title, string? subtitle = null, string? icon = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Title = title ?? throw new ArgumentNullException(nameof(title));

        Subtitle = subtitle;
        Icon = icon;
    }

    /// <summary>
    /// Gets or sets the visible title to display on the taskbar Jump List.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets a sub-title to display under the <see cref="Title"/>.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier used to respond to the action tap.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the path of an <c>.ico</c> file used as the Jump List entry's icon.
    /// Defaults to the application executable's own icon when not set.
    /// </summary>
    public string? Icon { get; set; }
}
