// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Reflection;
using Castle.DynamicProxy;

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// The Castle DynamicProxy <see cref="IInterceptor"/> behind every <see cref="ActionAttribute"/>-marked
/// method on a store built with at least one <see cref="IStoreActionInterceptor"/> registered (see
/// <see cref="StoreServiceCollectionExtensions"/>). Methods without <see cref="ActionAttribute"/> -
/// including inherited <see cref="StoreBase"/> members like <see cref="StoreBase.Reset"/> - pass
/// through untouched. Composes <see cref="_interceptors"/> (already ordered global-then-local by
/// <see cref="StoreServiceCollectionExtensions"/>) into a single middleware chain per call.
/// </summary>
internal sealed class ActionInterceptor : IInterceptor
{
    static readonly MethodInfo AwaitTypedResultAsyncMethod =
        typeof(ActionInterceptor).GetMethod(nameof(AwaitTypedResultAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    // The invocation whose action body InterceptedCall.CallThroughProxy is about to run (see
    // there). Castle runs the Intercept that call triggers on this same thread before any other
    // code, so that Intercept is the one that finds it here, and takes it.
    [ThreadStatic]
    static IInvocation? _directCallOf;

    readonly IReadOnlyList<IStoreActionInterceptor> _interceptors;

    public ActionInterceptor(IReadOnlyList<IStoreActionInterceptor> interceptors) => _interceptors = interceptors;

    public void Intercept(IInvocation invocation)
    {
        if (_directCallOf is { } original
            && ReferenceEquals(original.Proxy, invocation.Proxy)
            && original.Method == invocation.Method)
        {
            _directCallOf = null;
            try
            {
                invocation.Proceed();
            }
            finally
            {
                // Reflection, which made this call, copies ref/out values back into the original
                // invocation's arguments only when the call returns normally; a direct call keeps
                // a value written before a throw. Copying here keeps that on this path too.
                Array.Copy(invocation.Arguments, original.Arguments, original.Arguments.Length);
            }

            return;
        }

        if (invocation.MethodInvocationTarget?.GetCustomAttribute<ActionAttribute>() is null)
        {
            invocation.Proceed();
            return;
        }

        var call = new InterceptedCall(invocation);
        var context = new StoreActionContext((StoreBase)invocation.Proxy, invocation.Method.Name, invocation.Arguments);
        var pipeline = BuildPipeline(context, call.ProceedAsync);

        try
        {
            var returnType = invocation.Method.ReturnType;
            if (returnType == typeof(Task))
            {
                invocation.ReturnValue = AwaitDiscardingResultAsync(pipeline());
                return;
            }

            if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var method = AwaitTypedResultAsyncMethod.MakeGenericMethod(returnType.GetGenericArguments()[0]);
                invocation.ReturnValue = method.Invoke(null, new object[] { pipeline() });
                return;
            }

            // A synchronous action (void or an ordinary return value): there is no Task to hand back
            // to the caller, so the chain must be blocked on right here - see IStoreActionInterceptor's
            // remarks for the sync-over-async risk this carries if an interceptor genuinely suspends.
            var result = pipeline().GetAwaiter().GetResult();
            if (returnType != typeof(void))
                invocation.ReturnValue = result;
        }
        finally
        {
            call.InterceptReturned();
        }
    }

    StoreActionDelegate BuildPipeline(StoreActionContext context, StoreActionDelegate innermost)
    {
        var pipeline = innermost;
        for (var i = _interceptors.Count - 1; i >= 0; i--)
        {
            var interceptor = _interceptors[i];
            var next = pipeline;
            pipeline = () => interceptor.InterceptAsync(context, next);
        }

        return pipeline;
    }

    static async Task<object?> AwaitAndBoxResultAsync(Task task, Type declaredReturnType)
    {
        // Deliberately not ConfigureAwait(false): this resumes on whatever context called the
        // action (typically a WPF UI thread), since an interceptor further out in the chain may
        // itself read or write UI-bound store state after awaiting `next()`.
        await task;

        // The declared type decides, not the runtime one: an `async Task` method returns a
        // Task<VoidTaskResult> under the hood, which next() would otherwise hand back boxed
        // instead of the null StoreActionDelegate documents.
        if (declaredReturnType == typeof(Task))
            return null;

        var taskType = task.GetType();
        return taskType.IsGenericType ? taskType.GetProperty(nameof(Task<object>.Result))!.GetValue(task) : null;
    }

    static async Task AwaitDiscardingResultAsync(Task<object?> pipelineResult) => await pipelineResult;

    static async Task<T> AwaitTypedResultAsync<T>(Task<object?> pipelineResult) => (T)(await pipelineResult)!;

    /// <summary>
    /// The innermost <c>next</c> of one intercepted call: runs the action body, as often as it is
    /// called, from wherever it is called.
    /// </summary>
    /// <remarks>
    /// Castle's <see cref="IInvocation"/> is single-threaded state: <see cref="IInvocation.Proceed"/>
    /// steps an interceptor index that is only right while <see cref="Intercept"/> is on the stack,
    /// and both the action body and <see cref="Intercept"/> write <see cref="IInvocation.ReturnValue"/>,
    /// which the proxy reads after <see cref="Intercept"/> returns. So the invocation is proceeded
    /// only from inside <see cref="Intercept"/>, on its own thread - the ordinary case, where every
    /// interceptor reaches <c>next</c> synchronously, and exactly what the pipeline always did.
    /// Any other call - after an interceptor awaited before <c>next</c>, a retry after the action's
    /// first real await, a call from another thread - calls the method through the proxy again
    /// instead, flagged in <see cref="_directCallOf"/> so the <see cref="Intercept"/> it triggers
    /// runs the body without building a second chain. That call has an invocation of its own, so
    /// it never touches the original one while Castle may still be unwinding it on the caller's
    /// thread. Proceeding the original from a continuation through
    /// <see cref="IInvocation.CaptureProceedInfo"/> instead is safe on a UI thread, whose
    /// continuations wait for the call to return, but a thread-pool continuation can run during
    /// that unwinding: against that version,
    /// <c>NextCalledFromAThreadPoolContinuationWhileTheCallIsStillReturningRunsTheActionOnce</c>
    /// (<c>StoreActionInterceptorTests</c>) failed within a few thousand calls on every run, by
    /// running the chain twice or by a call that never completed.
    /// </remarks>
    sealed class InterceptedCall
    {
        readonly IInvocation _invocation;
        readonly int _threadId = Environment.CurrentManagedThreadId;
        volatile bool _interceptRunning = true;

        // Set while ProceedInPlace runs the body (only ever touched on _threadId): a next() the
        // body itself reaches - a stored next called from inside the action - must not proceed
        // the invocation again from inside its own Proceed, which Castle rejects ("Cannot proceed
        // past the end of the interception pipeline"), so it goes through the proxy instead.
        bool _proceedingInPlace;

        public InterceptedCall(IInvocation invocation) => _invocation = invocation;

        public void InterceptReturned() => _interceptRunning = false;

        public Task<object?> ProceedAsync()
        {
            var returnValue = _interceptRunning && Environment.CurrentManagedThreadId == _threadId && !_proceedingInPlace
                ? ProceedInPlace()
                : CallThroughProxy();

            return returnValue is Task task
                ? AwaitAndBoxResultAsync(task, _invocation.Method.ReturnType)
                : Task.FromResult(returnValue);
        }

        object? ProceedInPlace()
        {
            // ReturnValue is read straight after Proceed, on the thread that set it, so each call
            // (a synchronous retry included) sees its own result.
            _proceedingInPlace = true;
            try
            {
                _invocation.Proceed();
                return _invocation.ReturnValue;
            }
            finally
            {
                _proceedingInPlace = false;
            }
        }

        object? CallThroughProxy()
        {
            _directCallOf = _invocation;
            try
            {
                return _invocation.GetConcreteMethod().Invoke(
                    _invocation.Proxy,
                    BindingFlags.DoNotWrapExceptions,
                    binder: null,
                    _invocation.Arguments,
                    culture: null);
            }
            finally
            {
                // Normally already taken by the Intercept the call reached; cleared here as well
                // in case the call failed before reaching it.
                _directCallOf = null;
            }
        }
    }
}
