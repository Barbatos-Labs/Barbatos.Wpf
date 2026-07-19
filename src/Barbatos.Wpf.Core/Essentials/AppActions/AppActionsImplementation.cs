// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Shell;

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// The Windows implementation of <see cref="IAppActions"/>, using
/// <see cref="System.Windows.Shell.JumpList"/> — the WPF-native taskbar Jump List API — in
/// place of .NET MAUI's WinRT <c>Windows.UI.StartScreen.JumpList</c>. The action id encoding
/// scheme (a fixed prefix followed by the Base64-encoded id, carried in the launched
/// process's command-line arguments) mirrors .NET MAUI's own approach exactly.
/// </summary>
class AppActionsImplementation : IAppActions
{
    /// <summary>
    /// Mirrors .NET MAUI's <c>AppActionsExtensions.AppActionPrefix</c>.
    /// </summary>
    internal const string AppActionPrefix = "BARBATOS_APP_ACTIONS-";

    // WPF's JumpList is a write-only API: unlike WinRT's JumpList.LoadCurrentAsync(), there
    // is no way to read back the items the OS is currently showing, so the last actions set
    // through SetAsync are cached here for GetAsync() to return.
    List<AppAction> _actions = [];

    public bool IsSupported => true;

    public Task<IEnumerable<AppAction>> GetAsync() =>
        Task.FromResult<IEnumerable<AppAction>>(_actions);

    public Task SetAsync(IEnumerable<AppAction> actions)
    {
        _actions = actions?.ToList() ?? [];

        var jumpList = new JumpList { ShowRecentCategory = false, ShowFrequentCategory = false };
        JumpList.SetJumpList(Application.Current, jumpList);

        foreach (var action in _actions)
            jumpList.JumpItems.Add(ToJumpTask(action));

        jumpList.Apply();

        return Task.CompletedTask;
    }

    public event EventHandler<AppActionEventArgs>? AppActionActivated;

    /// <summary>
    /// Checks the process's startup arguments for an app action id and, if found, raises
    /// <see cref="AppActionActivated"/>. Called from the <c>WpfLifecycle.OnStartup</c>
    /// lifecycle event, mirroring .NET MAUI's own <c>OnLaunched</c> wiring.
    /// </summary>
    internal void HandleStartupArguments(string[] args)
    {
        var id = args.Select(ArgumentsToId).FirstOrDefault(parsed => parsed != null);
        if (string.IsNullOrEmpty(id))
            return;

        var action = _actions.FirstOrDefault(a => a.Id == id);
        if (action != null)
            AppActionActivated?.Invoke(null, new AppActionEventArgs(action));
    }

    static JumpTask ToJumpTask(AppAction action)
    {
        var executablePath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];

        var task = new JumpTask
        {
            Title = action.Title,
            Description = action.Subtitle ?? string.Empty,
            ApplicationPath = executablePath,
            Arguments = AppActionPrefix + Convert.ToBase64String(Encoding.UTF8.GetBytes(action.Id)),
            IconResourcePath = action.Icon ?? executablePath,
        };

        return task;
    }

    static string? ArgumentsToId(string arguments)
    {
        if (!arguments.StartsWith(AppActionPrefix, StringComparison.Ordinal))
            return null;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(arguments[AppActionPrefix.Length..]));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
