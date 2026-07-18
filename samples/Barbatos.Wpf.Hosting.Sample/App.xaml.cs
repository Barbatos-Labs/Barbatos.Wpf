// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.Sample;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : WpfApplication
{
    /// <inheritdoc />
    protected override WpfApp CreateWpfApp() => WpfProgram.CreateWpfApp();

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The main window is composed by (and resolved from) the dependency injection container.
        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }
}
