// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Threading;
using Barbatos.Wpf.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Dispatcher = Barbatos.Wpf.Dispatching.Dispatcher;

namespace Barbatos.Wpf.Core.UnitTests;

public class DispatchingTests
{
    [Fact]
    public void GetForCurrentThreadReturnsADispatcher()
    {
        var dispatcher = Dispatcher.GetForCurrentThread();

        Assert.NotNull(dispatcher);
    }

    [Fact]
    public void GetForCurrentThreadReturnsTheSameInstanceForTheSameThread()
    {
        var dispatcher1 = Dispatcher.GetForCurrentThread();
        var dispatcher2 = Dispatcher.GetForCurrentThread();

        Assert.Same(dispatcher1, dispatcher2);
    }

    [Fact]
    public void DispatcherProviderCurrentIsSingleton()
    {
        Assert.Same(DispatcherProvider.Current, DispatcherProvider.Current);
    }

    [Fact]
    public void CanResolveDispatcherServicesFromTheApp()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp.Services.GetService<IDispatcherProvider>());
        Assert.NotNull(wpfApp.Services.GetService<IDispatcher>());
    }

    [Fact]
    public void ScopedDispatcherIsResolvable()
    {
        var builder = WpfApp.CreateBuilder();
        var wpfApp = builder.Build();

        using var scope = wpfApp.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetService<IDispatcher>());
    }

    [Fact]
    public void IsDispatchRequiredIsFalseOnTheOwningThread()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        Assert.False(dispatcher.IsDispatchRequired);
    }

    [Fact]
    public async Task IsDispatchRequiredIsTrueOnAnotherThread()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var required = await Task.Run(() => dispatcher.IsDispatchRequired);

        Assert.True(required);
    }

    [Fact]
    public void DispatchThrowsOnNullAction()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        Assert.Throws<ArgumentNullException>(() => dispatcher.Dispatch(null!));
    }

    [Fact]
    public void DispatchExecutesTheAction()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var executed = false;
        var frame = new DispatcherFrame();

        var dispatched = dispatcher.Dispatch(() =>
        {
            executed = true;
            frame.Continue = false;
        });

        Assert.True(dispatched);

        System.Windows.Threading.Dispatcher.PushFrame(frame);

        Assert.True(executed);
    }

    [Fact]
    public async Task DispatchAsyncReturnsTheResult()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var frame = new DispatcherFrame();

        var task = dispatcher.DispatchAsync(() =>
        {
            frame.Continue = false;
            return 42;
        });

        System.Windows.Threading.Dispatcher.PushFrame(frame);

        Assert.Equal(42, await task);
    }

    [Fact]
    public async Task DispatchIfRequiredExecutesInlineOnTheOwningThread()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var executed = false;
        await dispatcher.DispatchIfRequiredAsync(() => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void CreateTimerReturnsNewInstances()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var timer1 = dispatcher.CreateTimer();
        var timer2 = dispatcher.CreateTimer();

        Assert.NotNull(timer1);
        Assert.NotNull(timer2);
        Assert.NotSame(timer1, timer2);
    }

    [Fact]
    public void TimerDefaultsMatchMaui()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var timer = dispatcher.CreateTimer();

        Assert.True(timer.IsRepeating);
        Assert.False(timer.IsRunning);
    }

    [Fact]
    public void TimerIntervalCanBeSet()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(123);

        Assert.Equal(TimeSpan.FromMilliseconds(123), timer.Interval);
    }

    [Fact]
    public void TimerStartAndStopToggleIsRunning()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMinutes(1);

        timer.Start();
        Assert.True(timer.IsRunning);

        timer.Stop();
        Assert.False(timer.IsRunning);
    }

    [Fact]
    public void NonRepeatingTimerStopsAfterFirstTick()
    {
        var dispatcher = Dispatcher.GetForCurrentThread()!;

        var timer = dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(50);
        timer.IsRepeating = false;

        var ticks = 0;
        var frame = new DispatcherFrame();
        timer.Tick += (sender, args) =>
        {
            ticks++;
            frame.Continue = false;
        };

        timer.Start();
        System.Windows.Threading.Dispatcher.PushFrame(frame);

        Assert.Equal(1, ticks);
        Assert.False(timer.IsRunning);
    }
}
