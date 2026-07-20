// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Notifications;
using Barbatos.Wpf.Power;
using Barbatos.Wpf.Startup;
using Barbatos.Wpf.Tray;

namespace Barbatos.Wpf.Core.UnitTests;

public sealed class FakeStartupRegistrar : IStartupRegistrar
{
    public Dictionary<string, string> Entries { get; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsRegistered(string entryName) => Entries.ContainsKey(entryName);

    public void Register(string entryName, string command) => Entries[entryName] = command;

    public void Unregister(string entryName) => Entries.Remove(entryName);
}

public sealed class FakePowerManager : IPowerManager
{
    public List<(bool KeepAwake, bool KeepDisplayOn)> Calls { get; } = new();

    public void SetKeepAwake(bool keepAwake, bool keepDisplayOn) => Calls.Add((keepAwake, keepDisplayOn));
}

public sealed class FakeTrayIconPlatform : ITrayIconPlatform
{
    public TrayIconOptions? ShownOptions { get; private set; }

    public int ShowCount { get; private set; }

    public int HideCount { get; private set; }

    public string? LastToolTip { get; private set; }

    public event EventHandler? Clicked;

    public event EventHandler? DoubleClicked;

    public void Show(TrayIconOptions options)
    {
        ShownOptions = options;
        ShowCount++;
    }

    public void Hide() => HideCount++;

    public void SetToolTip(string toolTip) => LastToolTip = toolTip;

    public void RaiseClicked() => Clicked?.Invoke(this, EventArgs.Empty);

    public void RaiseDoubleClicked() => DoubleClicked?.Invoke(this, EventArgs.Empty);
}

public sealed class FakeNotificationPlatform : INotificationPlatform
{
    public List<(NotificationOptions Options, NotificationContent Content)> Shown { get; } = new();

    public NotificationAvailability Availability { get; set; } = NotificationAvailability.Enabled;

    public int OpenSystemSettingsCallCount { get; private set; }

    public event EventHandler<NotificationActivatedEventArgs>? Activated;

    public void Show(NotificationOptions options, NotificationContent content) =>
        Shown.Add((options, content));

    public NotificationAvailability GetAvailability() => Availability;

    public void OpenSystemSettings() => OpenSystemSettingsCallCount++;

    public void RaiseActivated(string title, string message, string? arguments = null) =>
        Activated?.Invoke(this, new NotificationActivatedEventArgs(title, message, arguments));
}
