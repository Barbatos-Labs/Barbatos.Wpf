using System.Windows;
using System.Windows.Threading;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Hosts the single process-wide <see cref="Application"/> WPF allows, on one dedicated
/// background STA thread that lives for the whole test run.
/// </summary>
/// <remarks>
/// <see cref="Application.Current"/> is a process-wide singleton whose
/// <see cref="Dispatcher"/> is bound to whichever thread first constructed it - a fresh
/// <c>StaThread.Run</c> thread per test would only work for the *first* test that touches
/// it: every later test would find <see cref="Application.Current"/> already set, but
/// pointing at a dispatcher whose owning thread had already exited, so anything posted to
/// it would simply never run. Routing every Application-touching test through this single
/// long-lived dispatcher avoids that.
/// </remarks>
internal static class TestApplication
{
    private static readonly Lazy<Dispatcher> LazyDispatcher = new(() =>
    {
        Dispatcher? dispatcher = null;
        using var ready = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            _ = Application.Current ?? new Application();
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
