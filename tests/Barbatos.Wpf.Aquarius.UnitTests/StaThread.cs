using System.Windows.Threading;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Runs test bodies on a dedicated STA thread, since WPF UI objects (windows, controls,
/// dependency properties) require one. Mirrors the per-file <c>RunOnStaThread</c> helper
/// in Barbatos.Wpf.Core.UnitTests, factored into one shared helper here since every
/// usage in this project is identical.
/// </summary>
internal static class StaThread
{
    public static void Run(Action body)
    {
        ArgumentNullException.ThrowIfNull(body);

        Exception? failure = null;

        var thread = new Thread(() =>
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

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw failure;
    }

    /// <summary>
    /// Blocks until every dispatcher operation queued at <see cref="DispatcherPriority.Background"/>
    /// or higher (which includes WPF's own internal <c>Loaded</c>-priority tree-connection
    /// work) has run - the standard way to flush a WPF dispatcher without a full message loop.
    /// </summary>
    public static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
}
