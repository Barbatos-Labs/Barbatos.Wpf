// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.Dialogs;

namespace Barbatos.Wpf.Core.Sample;

public partial class AboutWindow : Window
{
    readonly IDialogService _dialogService;

    public AboutWindow(IDialogService dialogService)
    {
        InitializeComponent();

        _dialogService = dialogService;

        Loaded += (sender, e) => OwnerText.Text = $"Owner: {Owner?.Title ?? "(none)"}";
    }

    void OpenDetailsButton_Click(object sender, RoutedEventArgs e) =>
        // No owner argument: IDialogService.ActiveWindow resolves to this window (the most
        // recently activated one it has seen), so Details ends up owned by About, not by
        // MainWindow - demonstrating the "dialog opened from another dialog" owner chain.
        _dialogService.Show<DetailsWindow>();
}
