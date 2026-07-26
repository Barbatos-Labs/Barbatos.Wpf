using Barbatos.Wpf.Hosting;

namespace BarbatosWpfApp;

public sealed class HeartbeatService : IWpfPeriodicService
{
    public string Name => "Heartbeat";

    public PeriodicSchedule Schedule => new()
    {
        Frequency = PeriodicFrequency.Custom,
        Interval = TimeSpan.FromSeconds(5),
        Description = "Ticks every 5 seconds while the app is running.",
    };

    public Task ExecuteAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        System.Diagnostics.Debug.WriteLine($"[Heartbeat] tick at {DateTimeOffset.Now:T}");
        return Task.CompletedTask;
    }
}
