// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.LifecycleEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class HostBuilderLifecycleTests
{
    [Fact]
    public void LifecycleEventServiceIsRegisteredByDefault()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<ILifecycleEventService>());
    }

    [Fact]
    public void LifecycleEventServiceIsSingleton()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        var service1 = wpfApp.Services.GetRequiredService<ILifecycleEventService>();
        var service2 = wpfApp.Services.GetRequiredService<ILifecycleEventService>();

        Assert.Same(service1, service2);
    }

    [Fact]
    public void CanRegisterWpfLifecycleEvents()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
            .OnStartup((app, args) => { })
            .OnExit((app, args) => { })));
        var wpfApp = builder.Build();

        var lifecycle = wpfApp.Services.GetRequiredService<ILifecycleEventService>();

        Assert.True(lifecycle.ContainsEvent(nameof(WpfLifecycle.OnStartup)));
        Assert.True(lifecycle.ContainsEvent(nameof(WpfLifecycle.OnExit)));
        Assert.False(lifecycle.ContainsEvent(nameof(WpfLifecycle.OnActivated)));
    }

    [Fact]
    public void EventNameMatchesTheDelegateName()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
            .OnWindowCreated(window => { })));
        var wpfApp = builder.Build();

        var lifecycle = wpfApp.Services.GetRequiredService<ILifecycleEventService>();
        var delegates = lifecycle.GetEventDelegates<WpfLifecycle.OnWindowCreated>(nameof(WpfLifecycle.OnWindowCreated));

        Assert.Single(delegates);
    }

    [Fact]
    public void MultipleConfigureLifecycleEventsCombine()
    {
        // useDefaults: false, since the default builder also registers its own OnStartup
        // handler (for app-action activation detection via UseEssentials) which is
        // orthogonal to what this test is verifying.
        var builder = WpfApp.CreateBuilder(useDefaults: false);
        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf.OnStartup((app, args) => { })));
        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf.OnStartup((app, args) => { })));
        var wpfApp = builder.Build();

        var lifecycle = wpfApp.Services.GetRequiredService<ILifecycleEventService>();
        var delegates = lifecycle.GetEventDelegates<WpfLifecycle.OnStartup>(nameof(WpfLifecycle.OnStartup)).ToArray();

        Assert.Equal(2, delegates.Length);
    }

    [Fact]
    public void CanRegisterCustomEvents()
    {
        var invoked = false;

        var builder = WpfApp.CreateBuilder();
        builder.ConfigureLifecycleEvents(events => events.AddEvent("MyCustomEvent", new Action(() => invoked = true)));
        var wpfApp = builder.Build();

        var lifecycle = wpfApp.Services.GetRequiredService<ILifecycleEventService>();
        lifecycle.InvokeEvents("MyCustomEvent");

        Assert.True(invoked);
    }

    [Fact]
    public void DelegatesAreInvokedWithTheExpectedArguments()
    {
        System.Windows.Window? receivedWindow = null;

        var builder = WpfApp.CreateBuilder();
        builder.ConfigureLifecycleEvents(events => events.AddWpf(wpf => wpf
            .OnWindowCreated(window => receivedWindow = window)));
        var wpfApp = builder.Build();

        var lifecycle = wpfApp.Services.GetRequiredService<ILifecycleEventService>();

        var thread = new Thread(() =>
        {
            var window = new System.Windows.Window();
            lifecycle.InvokeEvents<WpfLifecycle.OnWindowCreated>(nameof(WpfLifecycle.OnWindowCreated), del => del(window));

            Assert.Same(window, receivedWindow);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.NotNull(receivedWindow);
    }
}
