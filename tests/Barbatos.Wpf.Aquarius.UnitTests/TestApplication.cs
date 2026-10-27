// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

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
/// <para>
/// Because every test shares it, one test's mistake must not take the rest of the run down, and
/// the first Windows CI run of this suite showed what happens otherwise: an exception escaped onto
/// this thread between tests (xUnit reported a bare "[FATAL ERROR]
/// System.InvalidOperationException"), the thread - and with it the process - died, and the run
/// hung until the job was killed. So, unlike the original copy of this helper:
/// </para>
/// <list type="bullet">
/// <item><see cref="ShutdownMode.OnExplicitShutdown"/>, as the Router and Valee copies already
/// had: by default the <see cref="Application"/> shuts down when its last window closes, and every
/// later <see cref="Invoke"/> would then have nothing to run on.</item>
/// <item>An exception that no <c>DispatcherUnhandledException</c> handler marks handled, raised by
/// deferred work an earlier test left queued (a <c>BeginInvoke</c>, an async continuation),
/// keeps this thread pumping instead of ending it, and is reported as a failure by the next
/// <see cref="Invoke"/>.</item>
/// <item><see cref="Invoke"/> gives up after <see cref="InvokeTimeout"/> with a
/// <see cref="TimeoutException"/>, and fails at once if the dispatcher has shut down, instead of
/// waiting forever or silently never running the body.</item>
/// </list>
/// </remarks>
internal static class TestApplication
{
    // Generous next to the slowest body routed through here (GcProbe's five collect-and-pump
    // rounds take well under a second) and short next to CI's hang timeout, so a blocked
    // dispatcher fails one test instead of hanging the whole run.
    private static readonly TimeSpan InvokeTimeout = TimeSpan.FromSeconds(30);

    private static readonly object EscapedGate = new();
    private static readonly List<Exception> Escaped = [];

    private static readonly Lazy<Dispatcher> LazyDispatcher = new(StartDispatcher);

    public static void Invoke(Action body)
    {
        ArgumentNullException.ThrowIfNull(body);

        ThrowIfAnythingEscaped();

        var dispatcher = LazyDispatcher.Value;
        Exception? failure = null;

        var operation = dispatcher.InvokeAsync(
            () =>
            {
                try
                {
                    body();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            },
            DispatcherPriority.Send);

        DispatcherOperationStatus status;

        try
        {
            status = operation.Wait(InvokeTimeout);
        }
        catch (OperationCanceledException ex)
        {
            throw new InvalidOperationException(
                "TestApplication's dispatcher has shut down, so this test body never ran. Something called " +
                "Application.Shutdown or Dispatcher.InvokeShutdown on the shared Application.",
                ex);
        }

        if (status == DispatcherOperationStatus.Pending)
        {
            operation.Abort();
            throw new TimeoutException(
                $"This test body did not start within {InvokeTimeout.TotalSeconds:0} s: TestApplication's dispatcher " +
                "is blocked - most likely by an earlier test still running on it or waiting on itself.");
        }

        if (status != DispatcherOperationStatus.Completed)
        {
            throw new TimeoutException(
                $"This test body was still running on TestApplication's dispatcher after {InvokeTimeout.TotalSeconds:0} s - " +
                "most likely waiting, on the dispatcher thread, for work only that same dispatcher can run.");
        }

        if (failure is not null)
            throw failure;
    }

    private static Dispatcher StartDispatcher()
    {
        Dispatcher? dispatcher = null;
        Exception? startupFailure = null;

        // Not disposed: if the wait below ever timed out, the thread would still Set it later.
        var ready = new ManualResetEventSlim();

        var thread = new Thread(() =>
        {
            try
            {
                var app = Application.Current ?? new Application();

                // Default WPF behavior tears the whole Application down the moment the last open
                // Window closes - fatal for a process-lifetime shared instance, since any single
                // test that Show()s then Close()s its own Window here would silently kill this
                // dispatcher for every test that runs after it.
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                dispatcher = Dispatcher.CurrentDispatcher;
            }
            catch (Exception ex)
            {
                // Escaping this thread would end the test process; the factory rethrows it instead.
                startupFailure = ex;
            }
            finally
            {
                ready.Set();
            }

            if (dispatcher is not null)
                PumpUntilShutdown(dispatcher);
        })
        {
            IsBackground = true,
            Name = nameof(TestApplication),
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        if (!ready.Wait(InvokeTimeout))
            throw new TimeoutException("TestApplication's thread never got its Application started.");

        if (startupFailure is not null)
            throw new InvalidOperationException("TestApplication could not create its Application.", startupFailure);

        return dispatcher!;
    }

    private static void PumpUntilShutdown(Dispatcher dispatcher)
    {
        while (!dispatcher.HasShutdownStarted)
        {
            try
            {
                Dispatcher.Run();
            }
            catch (Exception ex)
            {
                // Only an exception raised between test bodies gets here: one raised inside a body,
                // including in a nested PumpDispatcher, unwinds into Invoke's own catch. Every
                // handler has already declined it, and uncaught it would end this thread and the
                // test process. Re-entering Dispatcher.Run is safe - PushFrame restores its frame
                // depth in a finally, and the queue requests processing before each operation runs.
                lock (EscapedGate)
                    Escaped.Add(ex);
            }
        }
    }

    private static void ThrowIfAnythingEscaped()
    {
        Exception[] escaped;

        lock (EscapedGate)
        {
            if (Escaped.Count == 0)
                return;

            escaped = [.. Escaped];
            Escaped.Clear();
        }

        throw new AggregateException(
            "An exception escaped onto TestApplication's dispatcher after an earlier test had returned: deferred work " +
            "it queued (a BeginInvoke, an async continuation) threw, and no DispatcherUnhandledException handler marked " +
            "it handled. It is reported here, by the next test to use TestApplication, instead of ending the test process.",
            escaped);
    }
}
