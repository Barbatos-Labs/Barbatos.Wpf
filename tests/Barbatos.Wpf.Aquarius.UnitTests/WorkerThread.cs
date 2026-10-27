// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Runs a body on a dedicated MTA thread that owns no dispatcher - the shape of a background
/// worker raising <c>PropertyChanged</c> - and blocks until it finishes.
/// </summary>
/// <remarks>
/// A dedicated <see cref="Thread"/> rather than <c>Task.Run</c>: the pool can hand the work back
/// to the calling thread, which would prove nothing about "another thread". An exception is
/// captured and rethrown on the calling thread, since one escaping a bare thread would terminate
/// the test host instead of failing the test.
/// </remarks>
internal static class WorkerThread
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

        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw failure;
    }
}
