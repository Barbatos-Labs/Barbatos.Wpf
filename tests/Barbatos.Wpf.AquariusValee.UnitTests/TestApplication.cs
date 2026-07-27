using System.Windows;
using System.Windows.Threading;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// Hosts the single process-wide <see cref="Application"/> WPF allows, on one dedicated
/// background STA thread that lives for the whole test run. A per-project copy of the identical
/// helper both sibling test projects already carry (same reasoning: <see cref="Application.Current"/>
/// is a process-wide singleton bound to whichever thread first constructs it, so routing every
/// Application-touching test through one shared long-lived dispatcher avoids a fresh
/// <c>StaThread.Run</c> thread's dispatcher going dead the moment that thread exits - and, unlike
/// <c>StaThread.Run</c>, this dispatcher is actually pumping continuously via
/// <see cref="Dispatcher.Run"/>, which is required to ever observe a deferred
/// <c>Dispatcher.BeginInvoke</c>'d exception).
/// </summary>
internal static class TestApplication
{
    private static readonly Lazy<Dispatcher> LazyDispatcher = new(() =>
    {
        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            var app = Application.Current ?? new Application();
            // Default WPF behavior tears the whole Application down the moment the last open
            // Window closes - fatal for a process-lifetime shared instance, since any single
            // test that Show()s then Close()s its own Window would silently kill this dispatcher
            // for every test that runs after it.
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        ready.Wait();
        return dispatcher!;
    });

    public static void Invoke(Action body)
    {
        ArgumentNullException.ThrowIfNull(body);

        Exception? failure = null;

        LazyDispatcher.Value.Invoke(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });

        if (failure is not null)
            throw failure;
    }
}
