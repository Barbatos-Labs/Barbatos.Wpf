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
        // The supported shape for real interceptor-side async work (e.g. flushing a log to a
        // remote server): call next() as the first meaningful step, then await freely afterward.
        // See DelayingMiddleware's own remarks - awaiting BEFORE next() is not safe and is
        // deliberately not exercised here.
        var middleware = new DelayingMiddleware(TimeSpan.FromMilliseconds(15));
        var store = BuildStore<InterceptedStore>(middleware);

        await store.IncrementAsync();

        Assert.Equal(1, store.Count);
        Assert.True(middleware.RanAfterNext);
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
        var log = new List<string>();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton(log);
        builder.Services.AddStore<InterceptedStore>(i => i
            .Add<RecordingMiddleware>() // type-based, DI-constructed - its own separate Events list
            .Add(new ChainMiddleware("Instance", log)) // instance-based
            .Add(sp => new ChainMiddleware("Factory", sp.GetRequiredService<List<string>>()))); // factory-based
        var wpfApp = builder.Build();

        wpfApp.Services.GetRequiredService<InterceptedStore>().Increment();

        Assert.Equal(new[] { "Instance:Before", "Factory:Before", "Factory:After", "Instance:After" }, log);
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
    /// Awaits real asynchronous work only <i>after</i> calling <c>next()</c> - the only safe
    /// position for it (see <see cref="IStoreActionInterceptor"/>'s remarks). An earlier version
    /// of this fixture also awaited a delay <i>before</i> calling <c>next()</c>, which hung every
    /// test that used it indefinitely (confirmed by isolating and killing the stuck test host) -
    /// Castle DynamicProxy's <c>IInvocation.Proceed()</c> is not safe to reach from a continuation
    /// that has already crossed a genuine <see langword="await"/> suspension relative to
    /// <c>Intercept()</c>'s own synchronous call.
    /// </summary>
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
