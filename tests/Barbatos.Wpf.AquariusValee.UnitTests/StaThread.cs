using System.Windows.Threading;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// Runs test bodies on a dedicated STA thread, since WPF UI objects (windows, controls,
/// dependency properties) require one. A per-project copy of the identical helper both sibling
/// test projects already carry - this repo's established convention (no shared test-support
/// project).
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
    /// or higher (which includes WPF's own internal <c>Loaded</c>-priority tree-connection work,
    /// and this package's own <c>NextTick</c>) has run - the standard way to flush a WPF
    /// dispatcher without a full message loop.
    /// </summary>
    public static void PumpDispatcher() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);
}
