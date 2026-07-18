// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;

namespace Barbatos.Wpf.Hosting.Sample;

/// <summary>
/// The view model for <see cref="MainWindow"/>, resolved from the dependency injection container.
/// </summary>
public class MainViewModel
{
    public MainViewModel(IGreetingService greetingService)
    {
        Greeting = greetingService.GetGreeting();
        EnvironmentDescription = greetingService.GetEnvironmentDescription();
    }

    public string Greeting { get; }

    public string EnvironmentDescription { get; }

    public ObservableCollection<string> LifecycleEvents { get; } = new();

    public void LogLifecycleEvent(string message) =>
        LifecycleEvents.Add($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
}
