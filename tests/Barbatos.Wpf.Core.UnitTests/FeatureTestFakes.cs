// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Notifications;
using Barbatos.Wpf.Power;
using Barbatos.Wpf.PushNotifications;
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

    /// <summary>When set, <see cref="Show"/> throws instead of recording - simulates a real
    /// toast platform failure (e.g. an unpackaged-app edge case) independent of <see cref="Availability"/>.</summary>
    public bool ThrowOnShow { get; set; }

    public event EventHandler<NotificationActivatedEventArgs>? Activated;

    public void Show(NotificationOptions options, NotificationContent content)
    {
        if (ThrowOnShow)
            throw new InvalidOperationException("Simulated Show() failure.");

        Shown.Add((options, content));
    }

    public NotificationAvailability GetAvailability() => Availability;

    public void OpenSystemSettings() => OpenSystemSettingsCallCount++;

    public void RaiseActivated(string title, string message, string? arguments = null) =>
        Activated?.Invoke(this, new NotificationActivatedEventArgs(title, message, arguments));
}

public sealed class FakePushNotificationTransport : IPushNotificationTransport
{
    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public bool IsConnected { get; private set; }

    public event EventHandler<bool>? ConnectionStateChanged;

    public event EventHandler<string>? NotificationReceived;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCount++;
        IsConnected = true;
        ConnectionStateChanged?.Invoke(this, true);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCount++;
        IsConnected = false;
        ConnectionStateChanged?.Invoke(this, false);
        return Task.CompletedTask;
    }

    /// <summary>Simulates the transport delivering a raw notification payload.</summary>
    public void RaiseNotificationReceived(string rawJson) =>
        NotificationReceived?.Invoke(this, rawJson);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class FakePushNotificationFallbackPresenter : IPushNotificationFallbackPresenter
{
    public List<(IPushNotification Notification, DateTimeOffset ReceivedAt)> Notified { get; } = new();

    public event EventHandler<IPushNotification>? Activated;

    public void Notify(IPushNotification notification, DateTimeOffset receivedAt) =>
        Notified.Add((notification, receivedAt));

    public void RaiseActivated(IPushNotification notification) =>
        Activated?.Invoke(this, notification);
}

public sealed class FakeLauncher : ILauncher
{
    public List<Uri> Opened { get; } = new();

    public List<Uri> TryOpened { get; } = new();

    public Task<bool> CanOpenAsync(Uri uri) => Task.FromResult(true);

    public Task<bool> OpenAsync(Uri uri)
    {
        Opened.Add(uri);
        return Task.FromResult(true);
    }

    public Task<bool> OpenAsync(OpenFileRequest request) => Task.FromResult(true);

    public Task<bool> TryOpenAsync(Uri uri)
    {
        TryOpened.Add(uri);
        return Task.FromResult(true);
    }
}

public sealed class FakeAppInfo : IAppInfo
{
    public string AppGuid { get; set; } = "test-app-guid";

    public string Name { get; set; } = "Test App";

    public string VersionString { get; set; } = "1.2.3";

    public Version Version { get; set; } = new(1, 2, 3);

    public string BuildString { get; set; } = "0";

    public void ShowSettingsUI()
    {
    }

    public AppTheme RequestedTheme { get; set; } = AppTheme.Unspecified;

    public AppPackagingModel PackagingModel { get; set; } = AppPackagingModel.Unpackaged;

    public LayoutDirection RequestedLayoutDirection { get; set; } = LayoutDirection.LeftToRight;

    public DateTime? InstallDate { get; set; }

    public string? InstallLocation { get; set; }
}

public sealed class FakeDeviceInfo : IDeviceInfo
{
    public string Model { get; set; } = "Test Model";

    public string Manufacturer { get; set; } = "Test Manufacturer";

    public string Name { get; set; } = "Test Device";

    public string VersionString { get; set; } = "10.0";

    public Version Version { get; set; } = new(10, 0);

    public DevicePlatform Platform { get; set; } = DevicePlatform.WPF;

    public DeviceIdiom Idiom { get; set; } = DeviceIdiom.Desktop;

    public DeviceType DeviceType { get; set; } = DeviceType.Physical;
}

public sealed class FakeDeviceIdentity : IDeviceIdentity
{
    public string InstanceId { get; set; } = "test-instance-id";

    public string HardwareFingerprint { get; set; } = "test-fingerprint";

    public Task<string> GetInstanceIdAsync() => Task.FromResult(InstanceId);

    public Task<string> GetHardwareFingerprintAsync() => Task.FromResult(HardwareFingerprint);
}
