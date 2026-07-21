// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Core.Sample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += (sender, e) => viewModel.RefreshDisplayInfo();

        // The user may switch to Settings to toggle notifications and back - re-check
        // availability each time the window regains focus, not just once at startup.
        Activated += (sender, e) => viewModel.RefreshNotificationsAvailability();
    }

    void SendTestNotificationButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).SendTestNotification();

    void SendRichTestNotificationButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).SendRichTestNotification();

    void SendImageTestNotificationButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).SendImageTestNotification();

    void OpenNotificationSettingsButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).OpenNotificationSettings();

    void ConnectPushNotificationsButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).ConnectPushNotifications();

    void DisconnectPushNotificationsButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).DisconnectPushNotifications();

    void SimulatePushNotificationButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).SimulatePushNotification();

    void SaveSecureValueButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).SaveSecureValue();

    void LoadSecureValueButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).LoadSecureValue();

    void ComposeEmailButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).ComposeTestEmail();

    void ShowAboutButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).ShowAbout();

    void ShowDialogAboutButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).ShowDialogAbout();

    void CloseAllDialogsButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).CloseAllDialogs();

    void ShowDeviceIdentityButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).LoadDeviceIdentity();
}
