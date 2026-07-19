// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Windows;

namespace Barbatos.Wpf.Core.Sample;

public partial class DetailsWindow : Window
{
    public DetailsWindow()
    {
        InitializeComponent();

        Loaded += (sender, e) => OwnerText.Text = $"Owner: {Owner?.Title ?? "(none)"} — check the box below, then close About: " +
            "with CascadeCloseOwnedDialogs enabled (the default), About's own close is blocked too, instead of forcing this window shut.";

        Closing += OnClosing;
    }

    void OnClosing(object? sender, CancelEventArgs e) =>
        e.Cancel = VetoCloseCheckBox.IsChecked == true;
}
