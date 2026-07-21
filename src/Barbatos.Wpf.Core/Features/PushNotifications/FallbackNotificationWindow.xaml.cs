// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Barbatos.Wpf.PushNotifications;

/// <summary>
/// The default in-app fallback shown when push notifications can't reach the OS's own
/// notification system (see <see cref="IPushNotificationFallbackPresenter"/>). A borderless,
/// topmost, non-activating window positioned at the work area's bottom-right corner - it never
/// steals keyboard focus from whatever you're doing, matching how real OS toasts behave.
/// </summary>
public partial class FallbackNotificationWindow : Window
{
    readonly DispatcherTimer _dismissTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackNotificationWindow"/> class for
    /// <paramref name="notification"/>, auto-dismissing after <paramref name="timeout"/>.
    /// </summary>
    public FallbackNotificationWindow(IPushNotification notification, TimeSpan timeout)
    {
        InitializeComponent();

        TitleText.Text = notification.Title;
        MessageText.Text = notification.Body;

        if (!string.IsNullOrWhiteSpace(notification.ImageUrl) && TryLoadImage(notification.ImageUrl) is { } bitmap)
        {
            HeroImage.Source = bitmap;
            HeroImage.Visibility = Visibility.Visible;
        }

        _dismissTimer = new DispatcherTimer { Interval = timeout };
        _dismissTimer.Tick += (_, _) => Close();
        _dismissTimer.Start();
    }

    /// <summary>Occurs when the user clicks anywhere on the notification body (not the close button).</summary>
    public event EventHandler? BodyClicked;

    void OnBodyClicked(object sender, MouseButtonEventArgs e) =>
        BodyClicked?.Invoke(this, EventArgs.Empty);

    void OnCloseClicked(object sender, RoutedEventArgs e) =>
        Close();

    protected override void OnClosed(EventArgs e)
    {
        _dismissTimer.Stop();
        base.OnClosed(e);
    }

    // Mirrors Barbatos.Wpf.Hosting.SplashWindow's own TryLoadImage helper - BitmapImage accepts a
    // remote http(s) Uri directly (downloaded async by WPF itself), unlike
    // NotificationContent.ImagePath which is passed through Path.GetFullPath by
    // ToastNotificationPlatform and throws for one. A failed/unreachable download surfaces as a
    // blank image (WPF's own DownloadFailed handling), not an exception, so nothing further is
    // needed beyond guarding the synchronous Uri-construction failure modes here.
    static BitmapImage? TryLoadImage(string source)
    {
        try
        {
            return new BitmapImage(new Uri(source, UriKind.RelativeOrAbsolute));
        }
        catch (Exception ex) when (ex is UriFormatException or NotSupportedException or IOException)
        {
            return null;
        }
    }
}
