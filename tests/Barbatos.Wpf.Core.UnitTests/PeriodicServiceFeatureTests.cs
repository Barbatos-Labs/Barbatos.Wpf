// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class PeriodicServiceFeatureTests
{
    sealed class FakePeriodicService : IWpfPeriodicService
    {
        public FakePeriodicService(string name = "Fake", TimeSpan? interval = null, Action<FakePeriodicService>? onExecute = null)
        {
            Name = name;
            Schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Custom, Interval = interval ?? TimeSpan.FromMinutes(1) };
            OnExecute = onExecute;
        }

        public string Name { get; }

        public PeriodicSchedule Schedule { get; set; }

        public Action<FakePeriodicService>? OnExecute { get; }

        public int ExecuteCount { get; private set; }

        public IServiceProvider? LastServices { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
        {
            ExecuteCount++;
            LastServices = services;
            LastCancellationToken = cancellationToken;
            OnExecute?.Invoke(this);
            return Task.CompletedTask;
        }
    }

    sealed class ThrowingPeriodicService : IWpfPeriodicService
    {
        public string Name => "Throwing";

        public PeriodicSchedule Schedule => new() { Frequency = PeriodicFrequency.Custom, Interval = TimeSpan.FromMilliseconds(50) };

        public Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("boom");
    }

    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IPeriodicServiceScheduler>());
    }

    [Fact]
    public void SchedulerIsRegisteredAndStartedByDefault()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfPeriodicService>(new FakePeriodicService());
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        var scheduler = wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();

        Assert.True(scheduler.IsEnabled);
        var status = Assert.Single(scheduler.Services);
        Assert.Equal("Fake", status.Name);
        Assert.Equal(0, status.RunCount);
    }

    [Fact]
    public void ServicesUseTheirDefaultSchedule()
    {
        var scheduler = BuildScheduler(new FakePeriodicService(interval: TimeSpan.FromMinutes(7)));

        Assert.Equal(TimeSpan.FromMinutes(7), Assert.Single(scheduler.Services).Schedule.Interval);
    }

    [Fact]
    public void ConfigurationOverridesTheDefaultSchedule()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:PeriodicServices:Schedules:Fake:Frequency"] = "Custom",
            ["Barbatos:PeriodicServices:Schedules:Fake:Interval"] = "00:00:42",
        });
        builder.Services.AddSingleton<IWpfPeriodicService>(new FakePeriodicService(interval: TimeSpan.FromMinutes(1)));
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        var scheduler = wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();

        Assert.Equal(TimeSpan.FromSeconds(42), Assert.Single(scheduler.Services).Schedule.Interval);
    }

    [Fact]
    public void ConfigurationCanDisableTheScheduler()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:PeriodicServices:Enabled"] = "false",
        });
        builder.Services.AddSingleton<IWpfPeriodicService>(new FakePeriodicService());
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        Assert.False(wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>().IsEnabled);
    }

    [Fact]
    public void NonPositiveIntervalFailsFast()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfPeriodicService>(new FakePeriodicService(interval: TimeSpan.Zero));
        builder.ConfigurePeriodicServices();

        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void DuplicateNameAtDIRegistrationFailsFast()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfPeriodicService>(new FakePeriodicService());
        builder.Services.AddSingleton<IWpfPeriodicService>(new FakePeriodicService());
        builder.ConfigurePeriodicServices();

        Assert.Throws<ArgumentException>(() => builder.Build());
    }

    [Fact]
    public void UpdateScheduleChangesTheStatus()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        scheduler.UpdateSchedule("Fake", new PeriodicSchedule { Frequency = PeriodicFrequency.Custom, Interval = TimeSpan.FromSeconds(9) });

        Assert.Equal(TimeSpan.FromSeconds(9), Assert.Single(scheduler.Services).Schedule.Interval);
    }

    [Fact]
    public void UpdateScheduleThrowsForUnknownServices()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        Assert.Throws<ArgumentException>(() => scheduler.UpdateSchedule("Unknown",
            new PeriodicSchedule { Frequency = PeriodicFrequency.Custom, Interval = TimeSpan.FromSeconds(1) }));
    }

    [Fact]
    public void UpdateScheduleRejectsAnInvalidSchedule()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        Assert.Throws<InvalidOperationException>(() => scheduler.UpdateSchedule("Fake",
            new PeriodicSchedule { Frequency = PeriodicFrequency.Custom, Interval = TimeSpan.Zero }));
    }

    [Fact]
    public void SetEnabledRaisesIsEnabledChanged()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        var raised = 0;
        scheduler.IsEnabledChanged += (sender, args) => raised++;

        scheduler.SetEnabled(false);
        scheduler.SetEnabled(false);

        Assert.False(scheduler.IsEnabled);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void RegisterAddsAndArmsAServiceAfterBuild()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();
        var scheduler = wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();

        Assert.Empty(scheduler.Services);

        scheduler.Register(new FakePeriodicService());

        var status = Assert.Single(scheduler.Services);
        Assert.Equal("Fake", status.Name);
        // The scheduler is enabled by default, so a service registered afterwards is armed immediately.
        Assert.NotNull(status.NextRunTime);
    }

    [Fact]
    public void RegisterThrowsForDuplicateName()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        Assert.Throws<ArgumentException>(() => scheduler.Register(new FakePeriodicService()));
    }

    [Fact]
    public void UnregisterStopsAndRemovesTheService()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        Assert.True(scheduler.Unregister("Fake"));
        Assert.Empty(scheduler.Services);
    }

    [Fact]
    public void UnregisterReturnsFalseForUnknownServices()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        Assert.False(scheduler.Unregister("Unknown"));
    }

    [Fact]
    public void ServiceExecutesOnItsSchedule()
    {
        var frame = new DispatcherFrame();
        var service = new FakePeriodicService(interval: TimeSpan.FromMilliseconds(50), onExecute: s => frame.Continue = false);

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfPeriodicService>(service);
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        var scheduler = wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();

        PeriodicServiceStatus? executed = null;
        Exception? error = new InvalidOperationException("sentinel");
        scheduler.ServiceExecuted += (sender, args) => (executed, error) = (args.Service, args.Error);

        Dispatcher.PushFrame(frame);

        Assert.True(service.ExecuteCount >= 1);

        // The provider passed to the service is the root provider (the DI container hands
        // the scheduler its root engine scope, so compare resolved singletons, not instances).
        Assert.NotNull(service.LastServices);
        Assert.Same(scheduler, service.LastServices.GetRequiredService<IPeriodicServiceScheduler>());

        var status = Assert.Single(scheduler.Services);
        Assert.True(status.RunCount >= 1);
        Assert.NotNull(status.LastRunTime);
        Assert.NotNull(executed);
        Assert.Null(error);

        scheduler.SetEnabled(false);
    }

    [Fact]
    public void OnceScheduleCompletesAfterItsSingleRun()
    {
        var frame = new DispatcherFrame();
        var service = new FakePeriodicService(onExecute: s => frame.Continue = false)
        {
            Schedule = new PeriodicSchedule { Frequency = PeriodicFrequency.Once },
        };

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfPeriodicService>(service);
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        var scheduler = wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();

        Dispatcher.PushFrame(frame);

        var status = Assert.Single(scheduler.Services);
        Assert.Equal(1, status.RunCount);
        Assert.True(status.IsCompleted);
        Assert.Null(status.NextRunTime);

        // A completed Once schedule never ticks again - pump the dispatcher a while longer using
        // a plain WPF timer (not the scheduler) and confirm nothing changed.
        var secondFrame = new DispatcherFrame();
        var waitTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        waitTimer.Tick += (sender, args) =>
        {
            waitTimer.Stop();
            secondFrame.Continue = false;
        };
        waitTimer.Start();
        Dispatcher.PushFrame(secondFrame);

        Assert.Equal(1, service.ExecuteCount);

        scheduler.SetEnabled(false);
    }

    [Fact]
    public void FailingServiceRaisesTheEventWithTheError()
    {
        var frame = new DispatcherFrame();

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfPeriodicService>(new ThrowingPeriodicService());
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        var scheduler = wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();

        Exception? error = null;
        scheduler.ServiceExecuted += (sender, args) =>
        {
            error = args.Error;
            frame.Continue = false;
        };

        Dispatcher.PushFrame(frame);

        Assert.IsType<InvalidOperationException>(error);
        Assert.Equal("boom", error.Message);

        scheduler.SetEnabled(false);
    }

    [Fact]
    public void DisposingTheAppCancelsTheShutdownToken()
    {
        var frame = new DispatcherFrame();
        var service = new FakePeriodicService(interval: TimeSpan.FromMilliseconds(50), onExecute: s => frame.Continue = false);

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IWpfPeriodicService>(service);
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        Dispatcher.PushFrame(frame);

        Assert.True(service.ExecuteCount >= 1);
        Assert.False(service.LastCancellationToken.IsCancellationRequested);

        wpfApp.Dispose();

        Assert.True(service.LastCancellationToken.IsCancellationRequested);
    }

    static IPeriodicServiceScheduler BuildScheduler(IWpfPeriodicService service)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton(service);
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();
    }
}
