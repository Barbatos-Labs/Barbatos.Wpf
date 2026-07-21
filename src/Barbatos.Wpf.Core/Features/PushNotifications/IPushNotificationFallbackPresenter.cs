// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// Displays a push notification when it can't reach the OS's own notification system (the app
/// disabled notifications, Windows/Group Policy blocked them, or showing the real toast threw).
/// Registered via <c>TryAddSingleton</c> - replace it with your own implementation (e.g.
/// <c>services.AddSingleton&lt;IPushNotificationFallbackPresenter, MyFallback&gt;()</c>, called
/// before <c>ConfigurePushNotifications</c>) for something other than the built-in in-app toast
/// window.
/// </summary>
public interface IPushNotificationFallbackPresenter
{
    /// <summary>Shows <paramref name="notification"/> through the fallback mechanism.</summary>
    void Notify(IPushNotification notification, DateTimeOffset receivedAt);

    /// <summary>Occurs when the user interacts with a notification shown through this fallback.</summary>
    event EventHandler<IPushNotification>? Activated;
}

/// <summary>
/// The default <see cref="IPushNotificationFallbackPresenter"/>: a small, borderless,
/// non-activating window stacked at the work area's bottom-right corner, auto-dismissing after
/// <see cref="PushNotificationOptions.FallbackTimeout"/>.
/// </summary>
internal sealed class PushNotificationFallbackPresenter : IPushNotificationFallbackPresenter, IDisposable
{
    const double Margin = 12;
    const double SlotHeight = 110;

    readonly IOptions<PushNotificationOptions> _options;
    readonly List<FallbackNotificationWindow> _windows = [];

    public PushNotificationFallbackPresenter(IOptions<PushNotificationOptions> options) =>
        _options = options;

    public event EventHandler<IPushNotification>? Activated;

    public void Notify(IPushNotification notification, DateTimeOffset receivedAt)
    {
        var window = new FallbackNotificationWindow(notification, _options.Value.FallbackTimeout);
        window.BodyClicked += (_, _) => Activated?.Invoke(this, notification);
        window.Closed += (_, _) => RemoveAndReflow(window);

        _windows.Add(window);
        Reflow();

        window.Show();
    }

    public void Dispose()
    {
        foreach (var window in _windows.ToArray())
            window.Close();
    }

    void RemoveAndReflow(FallbackNotificationWindow window)
    {
        _windows.Remove(window);
        Reflow();
    }

    void Reflow()
    {
        var workArea = SystemParameters.WorkArea;

        for (var i = 0; i < _windows.Count; i++)
        {
            var slotFromBottom = _windows.Count - i;
            var window = _windows[i];

            window.Left = workArea.Right - window.Width - Margin;
            window.Top = workArea.Bottom - Margin - slotFromBottom * SlotHeight;
        }
    }
}
