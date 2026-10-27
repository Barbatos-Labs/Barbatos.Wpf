// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using Barbatos.Wpf.Apsu;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class StoreActionInterceptorTests
{
    [Fact]
    public void MiddlewareRunsCodeBeforeAndAfterTheAction()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        store.Increment();

        Assert.Equal(new[] { "Before:Increment", "After:Increment" }, middleware.Events);
    }

    [Fact]
    public void ActionStillRunsWhenMiddlewareCallsNext()
    {
        var store = BuildStore<InterceptedStore>(new RecordingMiddleware());

        store.Increment();

        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void NonAttributedVirtualMethodIsNotIntercepted()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        store.NotAnAction();

        Assert.Empty(middleware.Events);
        Assert.Equal(100, store.Count);
    }

    [Fact]
    public void NextReturnsTheSynchronousActionResult()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);
        store.Increment();

        store.GetDoubled();

        Assert.Equal(2, middleware.LastNextResult);
    }

    [Fact]
    public void MiddlewareNotCallingNextShortCircuitsTheActionEntirely()
    {
        var store = BuildStore<InterceptedStore>(new ShortCircuitMiddleware(sentinel: -1));

        var result = store.GetDoubled();

        Assert.Equal(-1, result); // the middleware's own value, not Count * 2
        Assert.Equal(0, store.Count); // Increment (if it had been called) never ran either
    }

    [Fact]
    public void ContextArgumentsContainsTheActualCallArguments()
    {
        StoreActionContext? captured = null;
        var store = BuildStore<InterceptedStore>(new CapturingMiddleware(context => captured = context));

        store.SetName("Ada");

        Assert.NotNull(captured);
        Assert.Equal(new object?[] { "Ada" }, captured!.Arguments);
    }

    [Fact]
    public void MiddlewareCanRejectAnActionBasedOnItsArguments()
    {
        // The capability this session's user asked for: block, not just observe, based on a
        // condition - here, on the action's own argument rather than store state (contrast with
        // CheckoutCooldownInterceptor in the sample app, which blocks based on elapsed time).
        var store = BuildStore<InterceptedStore>(new RejectBlankNameMiddleware());

        store.SetName("   ");

        Assert.Equal("", store.Name); // SetName's body never ran - still the default
    }

    [Fact]
    public void MiddlewareAllowsTheActionWhenTheArgumentConditionPasses()
    {
        var store = BuildStore<InterceptedStore>(new RejectBlankNameMiddleware());

        store.SetName("Ada");

        Assert.Equal("Ada", store.Name);
    }

    [Fact]
    public void MiddlewareCanCatchAnErrorAndReturnAFallbackInstead()
    {
        // Demonstrates a capability Pinia's own separate before/after/onError callbacks don't
        // have: because this is a single wrapping call, not three independent observers, a
        // middleware can swallow an error and substitute a result instead of only observing it.
        var store = BuildStore<InterceptedStore>(new FallbackOnErrorMiddleware(fallback: 999));

        var result = store.ThrowSyncWithResult();

        Assert.Equal(999, result);
    }

    [Fact]
    public void SyncExceptionReachesMiddlewareAndStillPropagatesWhenRethrown()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        Assert.Throws<InvalidOperationException>(() => store.ThrowSync());

        Assert.Equal(new[] { "Before:ThrowSync", "Error:ThrowSync" }, middleware.Events);
    }

    [Fact]
    public async Task AsyncActionResolvesAfterTheReturnedTaskIsAwaited()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        var task = store.IncrementAsync();

        // The action's own body hasn't reached its await yet's continuation, but more
        // importantly the middleware's own "After" line runs inside its await next(), so it
        // cannot have logged yet either - proves the task returned to the caller is what the
        // whole chain (middleware included) is still suspended on.
        Assert.DoesNotContain("After:IncrementAsync", middleware.Events);

        await task;

        Assert.Equal(new[] { "Before:IncrementAsync", "After:IncrementAsync" }, middleware.Events);
    }

    [Fact]
    public async Task AwaitingTheReturnedTaskGuaranteesTheMiddlewareAlreadyRanToCompletion()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        await store.IncrementAsync();

        Assert.Contains("After:IncrementAsync", middleware.Events);
    }

    [Fact]
    public async Task AsyncActionWithATypedResultPreservesTheResultThroughTheChain()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);
        store.Increment();
        store.Increment();

        var result = await store.GetDoubledAsync();

        Assert.Equal(4, result);
        Assert.Equal(4, middleware.LastNextResult);
    }

    [Fact]
    public async Task AsyncExceptionReachesMiddlewareAndStillPropagatesWhenRethrown()
    {
        var middleware = new RecordingMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ThrowAsync());

        Assert.Equal(new[] { "Before:ThrowAsync", "Error:ThrowAsync" }, middleware.Events);
    }

    [Fact]
    public async Task MiddlewareCanGenuinelyAwaitAsynchronousWorkAfterCallingNext()
    {
        // Real interceptor-side async work after the action (e.g. flushing a log to a remote
        // server). The before-next() counterpart is
        // MiddlewareCanGenuinelyAwaitAsynchronousWorkBeforeCallingNext.
        var middleware = new DelayingMiddleware(TimeSpan.FromMilliseconds(15));
        var store = BuildStore<InterceptedStore>(middleware);

        await store.IncrementAsync();

        Assert.Equal(1, store.Count);
        Assert.True(middleware.RanAfterNext);
    }

    [Fact]
    public async Task NextCalledFromAContinuationAfterTheInterceptedCallReturnedReachesTheAction()
    {
        // The exact condition the pipeline used to get wrong: next() reached only after Castle's
        // Intercept() call had already returned. A plain IInvocation.Proceed() from there found
        // Castle's interceptor index reset and re-entered the whole chain instead of running the
        // action (so Entries became 2). The gate is opened only after the call returned to this
        // test, so the continuation that calls next() cannot run any earlier.
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var middleware = new AwaitBeforeNextMiddleware(() => gate.Task);
        var store = BuildStore<InterceptedStore>(middleware);

        var call = store.IncrementAsync();
        Assert.False(call.IsCompleted); // suspended before next(): the action has not started

        gate.SetResult();
        await call.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(1, store.Count);
        Assert.Equal(1, middleware.Entries);
    }

    [Fact]
    public async Task MiddlewareCanGenuinelyAwaitAsynchronousWorkBeforeCallingNext()
    {
        // An async precondition (a delay standing in for a server check) awaited before next().
        // This used to hang the action forever; it now runs the action once.
        var middleware = new AwaitBeforeNextMiddleware(() => Task.Delay(15));
        var store = BuildStore<InterceptedStore>(middleware);

        await store.IncrementAsync().WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(1, store.Count);
        Assert.Equal(1, middleware.Entries);
    }

    [Fact]
    public async Task MiddlewareCanRetryAnAsyncActionThatFailedAfterItsFirstAwait()
    {
        // Retrying calls next() again from the continuation that observed the failure - after
        // Intercept() returned, so this used to re-enter the chain: Outer and the retrying
        // interceptor ran again, nested, each nested level with a fresh retry budget (so an
        // action whose every attempt suspends and fails re-entered forever). A retry re-runs
        // only what is inside the retrying interceptor (Inner, then the action).
        var log = new List<string>();
        var retry = new RetryMiddleware(maxAttempts: 3);
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(new RecordingMiddleware("Outer", log));
        builder.Services.AddSingleton<IStoreActionInterceptor>(retry);
        builder.Services.AddSingleton<IStoreActionInterceptor>(new RecordingMiddleware("Inner", log));
        builder.Services.AddStore<InterceptedStore>();
        var store = builder.Build().Services.GetRequiredService<InterceptedStore>();

        var call = store.FlakyAsync(failures: 2);
        store.FlakyGate.SetResult(); // only now can the first attempt fail
        var result = await call.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(3, result);
        Assert.Equal(3, store.FlakyAttempts);
        Assert.Equal(1, retry.Entries);
        Assert.Equal(
            new[]
            {
                "Outer.Before:FlakyAsync",
                "Inner.Before:FlakyAsync", "Inner.Error:FlakyAsync",
                "Inner.Before:FlakyAsync", "Inner.Error:FlakyAsync",
                "Inner.Before:FlakyAsync", "Inner.After:FlakyAsync",
                "Outer.After:FlakyAsync",
            },
            log);
    }

    [Fact]
    public async Task RetryGivesUpAndPropagatesTheErrorOnceItsAttemptsAreSpent()
    {
        var retry = new RetryMiddleware(maxAttempts: 3);
        var store = BuildStore<InterceptedStore>(retry);

        var call = store.FlakyAsync(failures: int.MaxValue);
        store.FlakyGate.SetResult();

        await Assert.ThrowsAsync<InvalidOperationException>(() => call.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(3, store.FlakyAttempts);
        Assert.Equal(1, retry.Entries);
    }

    [Fact]
    public void MiddlewareCanRetryASynchronousActionByCallingNextAgain()
    {
        var retry = new RetryMiddleware(maxAttempts: 3);
        var store = BuildStore<InterceptedStore>(retry);

        var result = store.FlakySync(failures: 2);

        Assert.Equal(3, result);
        Assert.Equal(3, store.FlakyAttempts);
    }

    [Fact]
    public async Task NextCalledFromAThreadPoolContinuationWhileTheCallIsStillReturningRunsTheActionOnce()
    {
        // On a thread with no synchronization context, Task.Yield queues the continuation that
        // calls next() to the thread pool at once, so it can start while the intercepted call is
        // still returning through Castle on the calling thread. Proceeding Castle's invocation
        // from there (IInvocation.CaptureProceedInfo) raced that return: on Linux every one of 10
        // runs of this test against that code failed within 4,500 calls, some by running the
        // chain twice and some by a call that never completed. Only the old code depends on
        // timing - nothing in the pass condition does - and every wait is bounded, so a
        // regression fails instead of hanging the run. The loop runs on a dedicated thread
        // because from a pool thread Task.Yield queues to that thread's local queue, where
        // another thread has to steal it, which makes the race rarer.
        var middleware = new YieldBeforeNextMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        await Task.Factory.StartNew(
            () =>
            {
                for (var call = 1; call <= 20_000; call++)
                {
                    var completed = store.IncrementWithoutAwaitingAsync().Wait(TimeSpan.FromSeconds(10));

                    Assert.True(completed, $"call {call} never completed");
                    Assert.Equal(call, middleware.Entries);
                    Assert.Equal(call, store.Count);
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    [Fact]
    public async Task SynchronousActionGetsItsArgumentsAndOutParameterWhenNextRunsOnAnotherThread()
    {
        // next() on a thread other than the blocked caller reaches the action through a second
        // call on the proxy, so arguments go in and the out value comes back through the
        // invocation's argument array. Called from the thread pool: on a UI thread the same
        // blocking would be the documented sync-over-async deadlock.
        var middleware = new NextOnAnotherThreadMiddleware();
        var store = BuildStore<InterceptedStore>(middleware);

        var (found, doubled) = await Task.Run(() => (store.TryDouble(21, out var value), value))
            .WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(found);
        Assert.Equal(42, doubled);
        Assert.NotEqual(middleware.CallerThreadId, middleware.NextThreadId);
        Assert.Equal(1, middleware.Entries);
    }

    [Fact]
    public void MultipleGlobalInterceptorsNestLikeMiddlewareInRegistrationOrder()
    {
        var log = new List<string>();
        var outer = new RecordingMiddleware("Outer", log);
        var inner = new RecordingMiddleware("Inner", log);

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(outer);
        builder.Services.AddSingleton<IStoreActionInterceptor>(inner);
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        // Registered first = outermost, matching how a global ASP.NET Core middleware pipeline
        // nests relative to whatever runs closer to the actual endpoint.
        Assert.Equal(new[] { "Outer.Before:Increment", "Inner.Before:Increment", "Inner.After:Increment", "Outer.After:Increment" }, log);
    }

    [Fact]
    public void LocalInterceptorRunsInsideEveryGlobalInterceptor()
    {
        var log = new List<string>();
        var global = new RecordingMiddleware("Global", log);
        var local = new RecordingMiddleware("Local", log);

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(global);
        builder.Services.AddStoreInterceptor<InterceptedStore>(local);
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        Assert.Equal(new[] { "Global.Before:Increment", "Local.Before:Increment", "Local.After:Increment", "Global.After:Increment" }, log);
    }

    [Fact]
    public void AddStoreWithChainedLocalInterceptorsComposesTheSameAsRegisteringThemSeparately()
    {
        var log = new List<string>();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("A", log));
        builder.Services.AddStore<InterceptedStore>(i => i
            .Add(new ChainMiddleware("D", log))
            .Add(new ChainMiddleware("E", log)));
        var wpfApp = builder.Build();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        // Same nesting two separate AddStoreInterceptor<T>() calls would produce - the builder is
        // pure sugar over that, not a different composition rule.
        Assert.Equal(new[] { "A:Before", "D:Before", "E:Before", "E:After", "D:After", "A:After" }, log);
    }

    [Fact]
    public void AddStoreBuilderGenericAddIsDIConstructedWithNoNewNeeded()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStore<InterceptedStore>(i => i.Add<RecordingMiddleware>());
        var wpfApp = builder.Build();

        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();
        store.Increment();

        Assert.Equal(1, store.Count); // the DI-constructed interceptor's chain still reached the action
    }

    [Fact]
    public void AddStoreBuilderMixesTypeInstanceAndFactoryInterceptorsInOneCall()
    {
        // All three kinds write to the one shared log, so the assertion pins their relative
        // order too. An earlier version used Add<RecordingMiddleware>() for the type-based link
        // and expected it to keep a private log - but the container fills that type's optional
        // List<string> parameter from the AddSingleton(log) below, so it wrote "Before:Increment"
        // first and the test failed on Windows CI while the builder itself was right.
        var log = new List<string>();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton(log);
        builder.Services.AddStore<InterceptedStore>(i => i
            .Add<TypeRegisteredChainMiddleware>() // type-based, DI-constructed
            .Add(new ChainMiddleware("Instance", log)) // instance-based
            .Add(sp => new ChainMiddleware("Factory", sp.GetRequiredService<List<string>>()))); // factory-based
        var wpfApp = builder.Build();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        // Add() order is chain order, whichever overload registered each link.
        Assert.Equal(
            new[] { "Type:Before", "Instance:Before", "Factory:Before", "Factory:After", "Instance:After", "Type:After" },
            log);
    }

    [Fact]
    public void AddStoreInterceptorFactoryOverloadResolvesDependenciesFromTheContainer()
    {
        var log = new List<string>();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton(log); // what the factory below resolves via sp.GetRequiredService
        builder.Services.AddStoreInterceptor<InterceptedStore>(sp => new ChainMiddleware("Factory", sp.GetRequiredService<List<string>>()));
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        Assert.Equal(new[] { "Factory:Before", "Factory:After" }, log);
    }

    [Fact]
    public void AddStoreInterceptorFactoryIsInvokedLazilyOnlyOnceOnFirstStoreResolution()
    {
        var factoryCallCount = 0;
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStoreInterceptor<InterceptedStore>(sp =>
        {
            factoryCallCount++;
            return new RecordingMiddleware();
        });
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();

        Assert.Equal(0, factoryCallCount); // not built at registration time

        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();
        Assert.Equal(1, factoryCallCount); // built once, when the store was first resolved

        store.Increment();
        store.Increment();
        Assert.Equal(1, factoryCallCount); // still just once - not rebuilt per action call
    }

    [Fact]
    public void SixChainedInterceptorsAllRunInOrderWhenEveryOneCallsNext()
    {
        // 3 global (A, B, C) + 3 local (D, E, F, registered in that order) -> chain A B C D E F,
        // outermost to innermost - matching this session's own scenario.
        var log = new List<string>();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("A", log));
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("B", log));
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("C", log));
        builder.Services.AddStoreInterceptor<InterceptedStore>(new ChainMiddleware("D", log));
        builder.Services.AddStoreInterceptor<InterceptedStore>(new ChainMiddleware("E", log));
        builder.Services.AddStoreInterceptor<InterceptedStore>(new ChainMiddleware("F", log));
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        Assert.Equal(1, wpfApp.Services.GetRequiredService<InterceptedStore>().Count); // the action really ran
        Assert.Equal(
            new[]
            {
                "A:Before", "B:Before", "C:Before", "D:Before", "E:Before", "F:Before",
                "F:After", "E:After", "D:After", "C:After", "B:After", "A:After",
            },
            log);
    }

    [Fact]
    public void AnyInterceptorBlockingInASixDeepChainPreventsTheActionAndEverythingAfterIt()
    {
        // Same A B C D E F chain, but D (the first local one) blocks by never calling next().
        var log = new List<string>();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("A", log));
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("B", log));
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("C", log));
        builder.Services.AddStoreInterceptor<InterceptedStore>(new ChainMiddleware("D", log, block: true));
        builder.Services.AddStoreInterceptor<InterceptedStore>(new ChainMiddleware("E", log));
        builder.Services.AddStoreInterceptor<InterceptedStore>(new ChainMiddleware("F", log));
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();

        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();
        store.Increment();

        Assert.Equal(0, store.Count); // the real action body never ran - not even once

        // E and F never even started - the chain never reached them.
        Assert.DoesNotContain("E:Before", log);
        Assert.DoesNotContain("F:Before", log);

        // A, B, C still run their own "after" logic normally: from their point of view next()
        // simply returned (with D's short-circuit value) - they have no way to know the action
        // itself never ran. This is the same behavior ASP.NET Core middleware has when an inner
        // middleware returns without calling next().
        Assert.Equal(
            new[] { "A:Before", "B:Before", "C:Before", "D:Before", "D:Blocked", "C:After", "B:After", "A:After" },
            log);
    }

    [Fact]
    public void HandleIsNotEjectedUntilEjectIsCalled()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStoreInterceptor(new RecordingMiddleware(), out var handle);

        Assert.False(handle.IsEjected);
        handle.Eject();
        Assert.True(handle.IsEjected);
    }

    [Fact]
    public void EjectingAGlobalInterceptorBeforeTheFirstCallMakesItANoOpPassThrough()
    {
        var middleware = new RecordingMiddleware();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStoreInterceptor(middleware, out var handle);
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();
        handle.Eject();

        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();
        store.Increment();

        Assert.Empty(middleware.Events); // the interceptor never actually ran
        Assert.Equal(1, store.Count); // but the action itself still did - eject skips straight to next()
    }

    [Fact]
    public void EjectingALocalInterceptorBeforeTheFirstCallMakesItANoOpPassThrough()
    {
        var middleware = new RecordingMiddleware();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStoreInterceptor<InterceptedStore>(middleware, out var handle);
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();
        handle.Eject();

        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();
        store.Increment();

        Assert.Empty(middleware.Events);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void EjectingTakesEffectOnTheNextCallEvenAfterTheInterceptorAlreadyRanOnceBefore()
    {
        // The same handle/store instance across two calls - proves ejection is a live check, not
        // something only honored if set up before the store's very first resolution.
        var middleware = new RecordingMiddleware();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStoreInterceptor(middleware, out var handle);
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();
        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();

        store.Increment();
        Assert.Equal(new[] { "Before:Increment", "After:Increment" }, middleware.Events);

        handle.Eject();
        store.Increment();

        // No new events recorded - the interceptor did not run for the second call.
        Assert.Equal(new[] { "Before:Increment", "After:Increment" }, middleware.Events);
        Assert.Equal(2, store.Count); // the action itself ran both times
    }

    [Fact]
    public void EjectingOneInterceptorInAChainLeavesTheOthersRunningInTheSameOrder()
    {
        var log = new List<string>();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("A", log));
        builder.Services.AddStoreInterceptor(new ChainMiddleware("B", log), out var handleB);
        builder.Services.AddSingleton<IStoreActionInterceptor>(new ChainMiddleware("C", log));
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();
        handleB.Eject();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        // B's slot is skipped entirely - A and C still nest around each other correctly, exactly
        // as if B had never been registered at all.
        Assert.Equal(new[] { "A:Before", "C:Before", "C:After", "A:After" }, log);
    }

    [Fact]
    public void EjectingAShortCircuitingInterceptorLetsTheActionRunOnASubsequentCall()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStoreInterceptor(new ShortCircuitMiddleware(sentinel: -1), out var handle);
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();
        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();

        var blocked = store.GetDoubled();
        Assert.Equal(-1, blocked); // the interceptor's own sentinel - the real action never ran

        handle.Eject();
        var afterEject = store.GetDoubled();

        Assert.Equal(0, afterEject); // Count * 2 - the real action, now actually reached
    }

    [Fact]
    public void LocalInterceptorDoesNotApplyToAnUnrelatedStoreType()
    {
        var local = new RecordingMiddleware("Local");

        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStoreInterceptor<InterceptedStore>(local);
        builder.Services.AddStore<InterceptedStore>();
        builder.Services.AddStore<OtherStore>();
        var wpfApp = builder.Build();

        var other = wpfApp.Services.GetRequiredService<OtherStore>();
        other.DoSomething();

        Assert.Empty(local.Events);
        // No interceptor at all applies to OtherStore, so it never goes through the proxy path.
        Assert.Equal(typeof(OtherStore), other.GetType());
    }

    [Fact]
    public void SealedStoreThrowsAFriendlyErrorWhenInterceptorsAreRegistered()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStoreActionInterceptor>(new RecordingMiddleware());
        builder.Services.AddStore<SealedStore>();
        var wpfApp = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => wpfApp.Services.GetRequiredService<SealedStore>());
        Assert.Contains("sealed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StoreWithoutInterceptorsRegisteredUsesThePlainNonProxiedPath()
    {
        // No IStoreActionInterceptor registered anywhere - regression check that AddStore's
        // original (pre-interceptor-feature) path is unaffected: the resolved instance is the
        // real store type itself, not a Castle-generated proxy subclass.
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStore<InterceptedStore>();
        var wpfApp = builder.Build();

        var store = wpfApp.Services.GetRequiredService<InterceptedStore>();
        store.Increment();

        Assert.Equal(1, store.Count);
        Assert.Equal(typeof(InterceptedStore), store.GetType());
    }

    static TStore BuildStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(IStoreActionInterceptor interceptor) where TStore : StoreBase
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton(interceptor);
        builder.Services.AddStore<TStore>();
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<TStore>();
    }

    public class InterceptedStore : StoreBase
    {
        public int Count { get; private set; }

        [Action]
        public virtual void Increment() => Count++;

        [Action]
        public virtual int GetDoubled() => Count * 2;

        [Action]
        public virtual async Task<int> GetDoubledAsync()
        {
            await Task.Delay(5);
            return Count * 2;
        }

        [Action]
        public virtual void ThrowSync() => throw new InvalidOperationException("sync failure");

        [Action]
        public virtual int ThrowSyncWithResult() => throw new InvalidOperationException("sync failure");

        [Action]
        public virtual async Task IncrementAsync()
        {
            await Task.Delay(20);
            Count++;
        }

        [Action]
        public virtual async Task ThrowAsync()
        {
            await Task.Delay(5);
            throw new InvalidOperationException("async failure");
        }

        public virtual void NotAnAction() => Count += 100;

        public string Name { get; private set; } = "";

        [Action]
        public virtual void SetName(string name) => Name = name;

        /// <summary>Opened by a test only after its <see cref="FlakyAsync"/> call has returned,
        /// so the first attempt's failure - and every retry after it - happens in a continuation
        /// that runs after Castle's Intercept() call is over.</summary>
        public TaskCompletionSource FlakyGate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int FlakyAttempts { get; private set; }

        [Action]
        public virtual async Task<int> FlakyAsync(int failures)
        {
            var attempt = ++FlakyAttempts;
            await FlakyGate.Task;
            if (attempt <= failures)
                throw new InvalidOperationException($"attempt {attempt} failed");

            return attempt;
        }

        [Action]
        public virtual int FlakySync(int failures)
        {
            var attempt = ++FlakyAttempts;
            if (attempt <= failures)
                throw new InvalidOperationException($"attempt {attempt} failed");

            return attempt;
        }

        [Action]
        public virtual Task IncrementWithoutAwaitingAsync()
        {
            Count++;
            return Task.CompletedTask;
        }

        [Action]
        public virtual bool TryDouble(int value, out int doubled)
        {
            doubled = value * 2;
            return true;
        }
    }

    public class OtherStore : StoreBase
    {
        [Action]
        public virtual void DoSomething()
        {
        }
    }

    public sealed class SealedStore : StoreBase
    {
        [Action]
        public void DoSomething()
        {
        }
    }

    /// <summary>One labeled link in a long interceptor chain, sharing one ordered log with every
    /// other link - see <see cref="StoreActionInterceptorTests.SixChainedInterceptorsAllRunInOrderWhenEveryOneCallsNext"/>
    /// and <see cref="StoreActionInterceptorTests.AnyInterceptorBlockingInASixDeepChainPreventsTheActionAndEverythingAfterIt"/>.</summary>
    sealed class ChainMiddleware : IStoreActionInterceptor
    {
        readonly string _label;
        readonly List<string> _log;
        readonly bool _block;

        public ChainMiddleware(string label, List<string> log, bool block = false)
        {
            _label = label;
            _log = log;
            _block = block;
        }

        public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            _log.Add($"{_label}:Before");
            if (_block)
            {
                _log.Add($"{_label}:Blocked");
                return Task.FromResult<object?>(null);
            }

            return CallNextAsync(next);
        }

        async Task<object?> CallNextAsync(StoreActionDelegate next)
        {
            var result = await next();
            _log.Add($"{_label}:After");
            return result;
        }
    }

    /// <summary>The DI-constructed link in <see cref="StoreActionInterceptorTests.AddStoreBuilderMixesTypeInstanceAndFactoryInterceptorsInOneCall"/>:
    /// its only constructor parameter is the shared log that test registers, so the container
    /// has exactly one way to build it.</summary>
    sealed class TypeRegisteredChainMiddleware : IStoreActionInterceptor
    {
        readonly ChainMiddleware _inner;

        public TypeRegisteredChainMiddleware(List<string> log) => _inner = new ChainMiddleware("Type", log);

        public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next) =>
            _inner.InterceptAsync(context, next);
    }

    class RecordingMiddleware : IStoreActionInterceptor
    {
        readonly string _label;

        public RecordingMiddleware(string label = "", List<string>? sharedLog = null)
        {
            _label = label.Length == 0 ? "" : label + ".";
            Events = sharedLog ?? new List<string>();
        }

        public List<string> Events { get; }

        public object? LastNextResult { get; private set; }

        public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            Events.Add($"{_label}Before:{context.ActionName}");
            try
            {
                var result = await next();
                LastNextResult = result;
                Events.Add($"{_label}After:{context.ActionName}");
                return result;
            }
            catch (Exception)
            {
                Events.Add($"{_label}Error:{context.ActionName}");
                throw;
            }
        }
    }

    sealed class CapturingMiddleware : IStoreActionInterceptor
    {
        readonly Action<StoreActionContext> _capture;

        public CapturingMiddleware(Action<StoreActionContext> capture) => _capture = capture;

        public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            _capture(context);
            return next();
        }
    }

    /// <summary>Blocks <see cref="InterceptedStore.SetName"/> when its own <c>name</c> argument
    /// is blank - the argument-based counterpart to <c>CheckoutCooldownInterceptor</c>'s
    /// state-based (elapsed time) short-circuit in the sample app.</summary>
    sealed class RejectBlankNameMiddleware : IStoreActionInterceptor
    {
        public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            if (context.ActionName == nameof(InterceptedStore.SetName)
                && context.Arguments.Count > 0
                && context.Arguments[0] is string name
                && string.IsNullOrWhiteSpace(name))
            {
                return Task.FromResult<object?>(null); // short-circuited: SetName's body never runs
            }

            return next();
        }
    }

    sealed class ShortCircuitMiddleware : IStoreActionInterceptor
    {
        readonly object? _sentinel;

        public ShortCircuitMiddleware(object? sentinel) => _sentinel = sentinel;

        public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next) =>
            Task.FromResult(_sentinel); // next() is never called - the action never runs
    }

    sealed class FallbackOnErrorMiddleware : IStoreActionInterceptor
    {
        readonly object? _fallback;

        public FallbackOnErrorMiddleware(object? fallback) => _fallback = fallback;

        public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            try
            {
                return await next();
            }
            catch (Exception)
            {
                return _fallback;
            }
        }
    }

    /// <summary>
    /// Awaits <c>before</c>, then calls <c>next()</c>. An earlier version of
    /// <see cref="DelayingMiddleware"/> awaited a delay before <c>next()</c> and hung every test
    /// that used it (confirmed by killing the stuck test host): the pipeline then reached the
    /// action through a plain <c>IInvocation.Proceed()</c>, which after <c>Intercept()</c> had
    /// returned re-entered the chain instead. <see cref="Entries"/> above 1 is that regression;
    /// from the second entry on this short-circuits rather than awaiting again, so the regression
    /// fails the test at once instead of re-entering for the rest of the run.
    /// </summary>
    sealed class AwaitBeforeNextMiddleware : IStoreActionInterceptor
    {
        readonly Func<Task> _before;
        int _entries;

        public AwaitBeforeNextMiddleware(Func<Task> before) => _before = before;

        public int Entries => Volatile.Read(ref _entries);

        public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            if (Interlocked.Increment(ref _entries) > 1)
                return null;

            await _before();
            return await next();
        }
    }

    /// <summary>Calls <c>next()</c> again after an <see cref="InvalidOperationException"/>, up to
    /// <c>maxAttempts</c> calls in all. <see cref="Entries"/> counts how often the chain reached
    /// this interceptor - once per action call, however many attempts it makes.</summary>
    sealed class RetryMiddleware : IStoreActionInterceptor
    {
        readonly int _maxAttempts;
        int _entries;

        public RetryMiddleware(int maxAttempts) => _maxAttempts = maxAttempts;

        public int Entries => Volatile.Read(ref _entries);

        public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            Interlocked.Increment(ref _entries);
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await next();
                }
                catch (InvalidOperationException) when (attempt < _maxAttempts)
                {
                }
            }
        }
    }

    /// <summary>Yields before calling <c>next()</c>: off a synchronization context, the
    /// continuation goes straight to the thread pool. <see cref="Entries"/> counts how often the
    /// chain reached this interceptor.</summary>
    sealed class YieldBeforeNextMiddleware : IStoreActionInterceptor
    {
        int _entries;

        public int Entries => Volatile.Read(ref _entries);

        public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            Interlocked.Increment(ref _entries);
            await Task.Yield();
            return await next();
        }
    }

    /// <summary>Calls <c>next()</c> on a dedicated thread (<see cref="TaskCreationOptions.LongRunning"/>)
    /// and records both thread ids, so a test can tell the action was reached from a thread other
    /// than its caller's.</summary>
    sealed class NextOnAnotherThreadMiddleware : IStoreActionInterceptor
    {
        int _entries;

        public int Entries => Volatile.Read(ref _entries);

        public int CallerThreadId { get; private set; }

        public int NextThreadId { get; private set; }

        public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            Interlocked.Increment(ref _entries);
            CallerThreadId = Environment.CurrentManagedThreadId;
            return Task.Factory.StartNew(
                () =>
                {
                    NextThreadId = Environment.CurrentManagedThreadId;
                    return next();
                },
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }
    }

    /// <summary>Awaits real asynchronous work after calling <c>next()</c> - see
    /// <see cref="AwaitBeforeNextMiddleware"/> for the before-next() shape.</summary>
    sealed class DelayingMiddleware : IStoreActionInterceptor
    {
        readonly TimeSpan _delay;

        public DelayingMiddleware(TimeSpan delay) => _delay = delay;

        public bool RanAfterNext { get; private set; }

        public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
        {
            var result = await next();
            await Task.Delay(_delay);
            RanAfterNext = true;
            return result;
        }
    }
}
