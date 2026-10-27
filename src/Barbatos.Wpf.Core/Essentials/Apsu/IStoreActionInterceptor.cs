// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Continues an intercepted action call to the next interceptor in the chain, or to the action
/// itself if this is the last one. Returns the eventual (boxed) result - <see langword="null"/>
/// for a <see langword="void"/>/<see cref="Task"/>-returning action.
/// </summary>
public delegate Task<object?> StoreActionDelegate();

/// <summary>Identifies which store and which action a <see cref="IStoreActionInterceptor"/> is
/// currently wrapping, and what it was called with - enough to decide whether to call
/// <c>next</c> at all (e.g. reject an empty/invalid argument by short-circuiting instead of
/// calling it), not just to observe or log the call.</summary>
public sealed class StoreActionContext
{
    /// <summary>Creates the context for one intercepted call.</summary>
    /// <param name="store">The store whose action is being called.</param>
    /// <param name="actionName">The name of the action method being called.</param>
    /// <param name="arguments">The arguments the action was called with, in declaration order.</param>
    public StoreActionContext(StoreBase store, string actionName, IReadOnlyList<object?> arguments)
    {
        Store = store;
        ActionName = actionName;
        Arguments = arguments;
    }

    /// <summary>The store instance whose action is being called.</summary>
    public StoreBase Store { get; }

    /// <summary>The name of the action method being called.</summary>
    public string ActionName { get; }

    /// <summary>The arguments <see cref="ActionName"/> was called with, in declaration order -
    /// empty for a parameterless action.</summary>
    public IReadOnlyList<object?> Arguments { get; }
}

/// <summary>
/// Wraps <see cref="ActionAttribute"/>-marked action calls - the <c>$onAction</c> counterpart,
/// shaped as ASP.NET Core-style middleware rather than Pinia's own separate before/after/error
/// callbacks.
/// </summary>
/// <remarks>
/// <para>
/// Register a <b>global</b> interceptor (applies to every store) with
/// <c>services.AddSingleton&lt;IStoreActionInterceptor, MyInterceptor&gt;()</c>, or a
/// <b>local</b> one (applies only to one store type) with
/// <see cref="StoreServiceCollectionExtensions.AddStoreInterceptor{TStore, TInterceptor}"/>.
/// Both kinds compose into a single chain per store - every global interceptor runs (in
/// registration order) outside every local one, closest to the actual action call. Use the
/// <c>out</c> <see cref="InterceptorHandle"/> overloads of either registration method instead if
/// this interceptor may need to be removed later without rebuilding the host - see
/// <see cref="InterceptorHandle"/>'s own remarks.
/// </para>
/// <para>
/// <see cref="InterceptAsync"/> is genuinely awaitable - real asynchronous work (an HTTP call,
/// <see cref="Task.Delay(TimeSpan)"/>, ...) before or after <c>next</c> is fine for a
/// <see cref="Task"/>/<see cref="Task{TResult}"/>-returning action. For a <i>synchronous</i>
/// action, the chain is still run through this same <see langword="async"/> shape, but the
/// proxy has no way to hand a <see cref="Task"/> back to a caller expecting an ordinary return
/// value - it blocks on the chain's result instead (<c>GetAwaiter().GetResult()</c>). If every
/// interceptor in that chain only does synchronous work (even if written with
/// <see langword="async"/>/<see langword="await"/> syntax over already-completed tasks), this is
/// safe; an interceptor that genuinely suspends anywhere while wrapping a synchronous action -
/// before or after <c>next</c> - risks the classic WPF sync-over-async deadlock, since its
/// continuation needs the UI thread the proxy is blocking. Pair asynchronous interceptor logic
/// with a <see cref="Task"/>-returning action to avoid this entirely.
/// </para>
/// <para>
/// <c>next</c> reaches the action from any continuation and any thread, so an interceptor may
/// <see langword="await"/> an asynchronous precondition before calling it, and may call it again
/// to retry after an attempt failed - including one that failed after the action's own first
/// real <see langword="await"/>. Each call re-runs the rest of the chain: every interceptor
/// after this one in the chain (globals before locals, each group in registration order), then
/// the action body. Interceptors outside this one still see a single call. Call <c>next</c> one
/// attempt at a time (await one call before starting the next), and keep the awaits before it
/// on the caller's context (no <c>ConfigureAwait(false)</c>): the action body runs on whichever
/// thread calls <c>next</c>.
/// </para>
/// <para>
/// Both used to fail. Castle DynamicProxy's <c>IInvocation</c> - its interceptor index and its
/// <c>ReturnValue</c> - is only safe to proceed while the proxied call is still in progress on
/// its own thread. Proceeding it from a later continuation re-entered the whole chain instead of
/// running the action: an <see langword="await"/> before <c>next</c> hung with no exception
/// (confirmed by killing a stuck test host) and a retry re-ran the outer interceptors, nested.
/// <c>IInvocation.CaptureProceedInfo()</c> fixes that only where continuations wait for the call
/// to return, as on a UI thread; a thread-pool continuation can run while it is still returning,
/// and a stress test then saw calls run the chain twice or never complete. So the pipeline
/// proceeds Castle's invocation only from inside that call, on its thread, and every other
/// <c>next</c> - after an <see langword="await"/>, a retry, another thread - reaches the action
/// through a second call on the proxy that skips the chain.
/// </para>
/// </remarks>
public interface IStoreActionInterceptor
{
    /// <summary>
    /// Called for every intercepted action. Call <paramref name="next"/> to continue the chain
    /// (its result becomes the action's return value), or don't call it at all to short-circuit
    /// the action entirely. Wrap it in a <see langword="try"/>/<see langword="catch"/> to observe
    /// or translate an error; wrap it in timing code to measure it; call it again after a failed
    /// attempt to retry - each call re-runs the interceptors after this one and the action.
    /// </summary>
    /// <param name="context">Which store and which action this call wraps.</param>
    /// <param name="next">Continues to the next interceptor, or to the action itself. May be
    /// called after an <see langword="await"/>, and more than once, one call at a time - see this
    /// interface's remarks.</param>
    Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next);
}
