// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Threading;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Runs test bodies on a dedicated STA thread, since WPF UI objects (windows, controls,
/// dependency properties) require one. A per-project copy of
/// Barbatos.Wpf.Aquarius.UnitTests's own identical helper - this repo's established convention
/// (no shared test-support project).
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
