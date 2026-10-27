// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// What a validation pass does when a rule throws instead of returning a message.
/// </summary>
/// <remarks>
/// <see cref="Field{T}"/> used to catch only <see cref="OperationCanceledException"/>, whoever
/// raised it. Any other exception skipped the line that lowers <see cref="FieldMeta.Pending"/>, so
/// the field - and with it its whole <see cref="Form"/> - reported "checking..." forever, and in a
/// pass nobody awaited the exception disappeared with the discarded task. An
/// <c>HttpClient</c> timeout was worse still: its <see cref="TaskCanceledException"/> was mistaken
/// for Valee's own supersession and silently swallowed, leaving <c>Pending</c> stuck as well.
/// <para>
/// The plain <c>async</c> tests here only ever await their passes, and add the throwing rule with
/// <c>AddRule</c> after construction, so no unawaited pass faults on an xUnit thread - which may own
/// a dispatcher that nothing pumps.
/// </para>
/// </remarks>
public class FieldRuleExceptionTests
{
    [Fact]
    public async Task AThrowingAsyncRuleStillLowersPendingAndFaultsTheAwaitedPass()
    {
        var field = new Field<string>("Username", "bob");
        field.AddRule(async (_, _) =>
        {
            await Task.Yield();
            throw new InvalidOperationException("Availability service unavailable.");
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(field.ValidateAsync);

        Assert.Equal("Availability service unavailable.", exception.Message);
        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public async Task AThrowingRuleLeavesTheWholeFormNoLongerPending()
    {
        var form = new Form();
        var username = form.DefineField("Username", "bob").AddRule(async (_, _) =>
        {
            await Task.Yield();
            throw new HttpRequestException("offline");
        });

        await Assert.ThrowsAsync<HttpRequestException>(form.ValidateAllAsync);

        Assert.False(username.Meta.Pending);
        Assert.False(form.Meta.Pending);
    }

    [Fact]
    public async Task AThrowingPassKeepsThePreviousResultInsteadOfWritingAPartialOne()
    {
        // "bobby" passes the sync rule, so the pass that throws has collected no message by then -
        // writing that partial result would clear every error and report the field valid although
        // the availability check never answered.
        var shouldThrow = false;
        var field = new Field<string>("Username", "bob");
        field.AddRule(value => value.Length < 5 ? "Too short." : null);
        field.AddRule(async (_, _) =>
        {
            await Task.Yield();
            return shouldThrow ? throw new HttpRequestException("offline") : "Taken.";
        });

        await field.ValidateAsync();
        Assert.Equal(["Too short.", "Taken."], field.Errors);

        shouldThrow = true;
        field.HandleChange("bobby", shouldValidate: false);
        await Assert.ThrowsAsync<HttpRequestException>(field.ValidateAsync);

        Assert.Equal(["Too short.", "Taken."], field.Errors);
        Assert.False(field.Meta.Valid);
        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public async Task ATimeoutStyleTaskCanceledExceptionIsAFailureNotASupersededPass()
    {
        var field = new Field<string>("Username", "bob");
        field.AddRule(async (_, _) =>
        {
            await Task.Yield();

            // What HttpClient raises once HttpClient.Timeout elapses: a TaskCanceledException that
            // has nothing to do with the token Valee passed in, which was never cancelled.
            throw new TaskCanceledException(
                "The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.",
                new TimeoutException());
        });

        var exception = await Assert.ThrowsAsync<TaskCanceledException>(field.ValidateAsync);

        Assert.IsType<TimeoutException>(exception.InnerException);
        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public async Task ASupersededPassEndsQuietlyAndLeavesPendingToThePassThatReplacedIt()
    {
        // The counterweight to the tests above: lowering Pending in a finally must not let a pass
        // Valee cancelled lower the flag the newer, still-running pass has raised.
        var secondGate = new TaskCompletionSource();
        var calls = 0;
        var field = new Field<string>("Username", "bob");
        field.AddRule(async (_, cancellationToken) =>
        {
            if (++calls == 1)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return "Stale.";
            }

            await secondGate.Task;
            return null;
        });

        var first = field.ValidateAsync();
        var second = field.ValidateAsync();
        await first;

        Assert.True(field.Meta.Pending);

        secondGate.SetResult();
        await second;

        Assert.False(field.Meta.Pending);
        Assert.Empty(field.Errors);
    }

    [Fact]
    public async Task ASupersededPassIsQuietWhateverItsRuleThrows()
    {
        // Not every client library reports cancellation as an OperationCanceledException, and a
        // superseded pass's outcome is discarded either way - so its exception is not surfaced.
        var firstGate = new TaskCompletionSource();
        var calls = 0;
        var field = new Field<string>("Username", "bob");
        field.AddRule(async (_, _) =>
        {
            if (++calls == 1)
            {
                await firstGate.Task;
                throw new InvalidOperationException("Call cancelled by the transport.");
            }

            return null;
        });

        var first = field.ValidateAsync();
        await field.ValidateAsync();
        firstGate.SetResult();

        var exception = await Record.ExceptionAsync(() => first);

        Assert.Null(exception);
        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public void ARuleThatThrowsInAPassNobodyAwaitsIsRethrownOnTheDispatcher()
    {
        // HandleBlur discards its pass's task. Before, the exception went down with it; now it is
        // rethrown on the dispatcher of the thread that started the pass, the same route an
        // ordinary UI-thread failure takes. TestApplication's continuously running dispatcher is
        // the only one here that ever executes the deferred rethrow.
        Exception? observed = null;
        var pendingAfterBlur = true;
        using var signal = new ManualResetEventSlim();

        void OnUnhandled(object? sender, DispatcherUnhandledExceptionEventArgs e)
        {
            observed = e.Exception;
            e.Handled = true;
            signal.Set();
        }

        TestApplication.Invoke(() =>
        {
            Application.Current.DispatcherUnhandledException += OnUnhandled;
            var field = new Field<string>("Username", "bob");
            field.AddRule((_, _) => Task.FromException<string?>(new HttpRequestException("offline")));

            field.HandleBlur();
            pendingAfterBlur = field.Meta.Pending;
        });

        try
        {
            Assert.True(signal.Wait(TimeSpan.FromSeconds(5)), "Expected the rule's exception to reach DispatcherUnhandledException.");
            Assert.IsType<HttpRequestException>(observed);
            Assert.False(pendingAfterBlur);
        }
        finally
        {
            TestApplication.Invoke(() => Application.Current.DispatcherUnhandledException -= OnUnhandled);
        }
    }

    [Fact]
    public async Task APassNobodyAwaitsOnAThreadPoolThreadLeavesItsFaultOnTheTask()
    {
        // A pool thread can own a Dispatcher - any code that reads Dispatcher.CurrentDispatcher there
        // creates one, as FormMeta's NextTick used to in the very HandleChange that starts the pass -
        // and nothing ever pumps it. Routing the rethrow there observed the fault (so
        // UnobservedTaskException never fired) and queued it where it would never run. Pumping that
        // dispatcher here is only a probe: anything Valee posted to it would throw out of the pump.
        var marker = $"offline {Guid.NewGuid():N}";
        using var reported = new ManualResetEventSlim();

        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception.InnerExceptions.Any(inner => inner.Message == marker))
            {
                e.SetObserved();
                reported.Set();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;

        try
        {
            await Task.Run(() =>
            {
                var poolDispatcher = Dispatcher.CurrentDispatcher;
                ChangeAFormFieldWhoseRuleThrows(marker);
                poolDispatcher.Invoke(() => { }, DispatcherPriority.Background);
            });

            for (var attempt = 0; attempt < 5 && !reported.IsSet; attempt++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
            }

            Assert.True(reported.IsSet, "Expected the fault to stay on the discarded task, for TaskScheduler.UnobservedTaskException.");
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    [Fact]
    public void APassNobodyAwaitsOnAWorkerThatNeverPumpsLeavesItsFaultOnTheTask()
    {
        // The same hole on a dedicated (not pool) MTA thread: owning a Dispatcher does not make a
        // thread a UI thread. A pass started outside any dispatcher operation on a non-STA thread
        // used to post its rethrow to that thread's dispatcher regardless; now it is judged the way
        // Aquarius's NextTick judges the calling thread, and the fault stays on the task. The pump
        // is the same probe as above - a posted rethrow would throw out of it.
        var marker = $"offline {Guid.NewGuid():N}";
        using var reported = new ManualResetEventSlim();
        Exception? workerFailure = null;

        void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            if (e.Exception.InnerExceptions.Any(inner => inner.Message == marker))
            {
                e.SetObserved();
                reported.Set();
            }
        }

        TaskScheduler.UnobservedTaskException += OnUnobserved;

        try
        {
            var worker = new Thread(() =>
            {
                try
                {
                    var workerDispatcher = Dispatcher.CurrentDispatcher;
                    ChangeAFormFieldWhoseRuleThrows(marker);
                    workerDispatcher.Invoke(() => { }, DispatcherPriority.Background);
                }
                catch (Exception exception)
                {
                    workerFailure = exception;
                }
            })
            {
                IsBackground = true,
            };
            worker.SetApartmentState(ApartmentState.MTA);
            worker.Start();

            Assert.True(worker.Join(TimeSpan.FromSeconds(10)), "The worker thread did not finish.");
            Assert.Null(workerFailure);

            for (var attempt = 0; attempt < 5 && !reported.IsSet; attempt++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                GC.WaitForPendingFinalizers();
            }

            Assert.True(reported.IsSet, "Expected the fault to stay on the discarded task, for TaskScheduler.UnobservedTaskException.");
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= OnUnobserved;
        }
    }

    // Separate and non-inlined so nothing in the test's own frame keeps the discarded pass's task
    // reachable - it has to be collected for UnobservedTaskException to fire.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ChangeAFormFieldWhoseRuleThrows(string marker)
    {
        var form = new Form();
        var username = form.DefineField("Username", "bob").AddRule((_, _) => Task.FromException<string?>(new HttpRequestException(marker)));

        username.HandleChange("bobby");
    }
}
