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
    public StoreActionContext(StoreBase store, string actionName, IReadOnlyList<object?> arguments)
    {
        Store = store;
        ActionName = actionName;
        Arguments = arguments;
    }

    public StoreBase Store { get; }

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
/// <see cref="Task.Delay(TimeSpan)"/>, ...) inside it is fine for a <see cref="Task"/>/<see cref="Task{TResult}"/>-returning
/// action. For a <i>synchronous</i> action, the chain is still run through this same
/// <see langword="async"/> shape, but the proxy has no way to hand a <see cref="Task"/> back to
/// a caller expecting an ordinary return value - it blocks on the chain's result instead
/// (<c>GetAwaiter().GetResult()</c>). If every interceptor in that chain only does synchronous
/// work (even if written with <see langword="async"/>/<see langword="await"/> syntax over
/// already-completed tasks), this is safe; an interceptor that genuinely suspends while wrapping
/// a synchronous action risks the classic WPF sync-over-async deadlock. Pair asynchronous
/// interceptor logic with a <see cref="Task"/>-returning action to avoid this entirely.
/// </para>
/// <para>
/// <b>Call <c>next</c> as your first meaningful step if you are going to call it at all - never
/// <see langword="await"/> anything genuinely asynchronous before calling it.</b> This is not a
/// soft recommendation: Castle DynamicProxy's underlying <c>IInvocation.Proceed()</c> (what
/// <c>next</c> eventually calls, at the innermost interceptor) is not safe to reach from a
/// continuation that already crossed a real <see langword="await"/> suspension relative to
/// <see cref="InterceptAsync"/>'s own synchronous call - doing so hangs the action forever rather
/// than throwing, confirmed by isolating and killing the stuck process during this feature's own
/// testing. Awaiting real asynchronous work is fully supported <i>after</i> <c>next</c> returns
/// (post-processing, e.g. flushing a log) - only work ordered before it is unsafe.
/// </para>
/// </remarks>
public interface IStoreActionInterceptor
{
    /// <summary>
    /// Called for every intercepted action. Call <paramref name="next"/> to continue the chain
    /// (its result becomes the action's return value), or don't call it at all to short-circuit
    /// the action entirely. Wrap it in a <see langword="try"/>/<see langword="catch"/> to observe
    /// or translate an error; wrap it in timing code to measure it; call it more than once to retry.
    /// </summary>
    /// <param name="context">Which store and which action this call wraps.</param>
    /// <param name="next">Continues to the next interceptor, or to the action itself. Call this
    /// before awaiting anything else - see this interface's remarks.</param>
    Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next);
}
