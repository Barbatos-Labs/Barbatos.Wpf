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
            Interval = interval ?? TimeSpan.FromMinutes(1);
            OnExecute = onExecute;
        }

        public string Name { get; }

        public TimeSpan Interval { get; }

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

        public TimeSpan Interval => TimeSpan.FromMilliseconds(50);

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
    public void ServicesUseTheirDefaultInterval()
    {
        var scheduler = BuildScheduler(new FakePeriodicService(interval: TimeSpan.FromMinutes(7)));

        Assert.Equal(TimeSpan.FromMinutes(7), Assert.Single(scheduler.Services).Interval);
    }

    [Fact]
    public void ConfigurationOverridesTheDefaultInterval()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:PeriodicServices:Intervals:Fake"] = "00:00:42",
        });
        builder.Services.AddSingleton<IWpfPeriodicService>(new FakePeriodicService(interval: TimeSpan.FromMinutes(1)));
        builder.ConfigurePeriodicServices();
        var wpfApp = builder.Build();

        var scheduler = wpfApp.Services.GetRequiredService<IPeriodicServiceScheduler>();

        Assert.Equal(TimeSpan.FromSeconds(42), Assert.Single(scheduler.Services).Interval);
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
    public void UpdateIntervalChangesTheStatus()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        scheduler.UpdateInterval("Fake", TimeSpan.FromSeconds(9));

        Assert.Equal(TimeSpan.FromSeconds(9), Assert.Single(scheduler.Services).Interval);
    }

    [Fact]
    public void UpdateIntervalThrowsForUnknownServices()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        Assert.Throws<ArgumentException>(() => scheduler.UpdateInterval("Unknown", TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void UpdateIntervalRejectsNonPositiveIntervals()
    {
        var scheduler = BuildScheduler(new FakePeriodicService());

        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.UpdateInterval("Fake", TimeSpan.Zero));
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
    public void ServiceExecutesOnItsInterval()
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
