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
    }

    void SendTestNotificationButton_Click(object sender, RoutedEventArgs e) =>
        ((MainViewModel)DataContext).SendTestNotification();

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
