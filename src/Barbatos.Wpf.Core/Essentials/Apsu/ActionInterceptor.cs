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

    readonly IReadOnlyList<IStoreActionInterceptor> _interceptors;

    public ActionInterceptor(IReadOnlyList<IStoreActionInterceptor> interceptors) => _interceptors = interceptors;

    public void Intercept(IInvocation invocation)
    {
        if (invocation.MethodInvocationTarget?.GetCustomAttribute<ActionAttribute>() is null)
        {
            invocation.Proceed();
            return;
        }

        // Captured here, while Castle's interceptor index still points at this interceptor: a
        // plain invocation.Proceed() made after Intercept has returned - from an interceptor that
        // awaited before next(), or one calling next() again to retry after the action's first
        // real await - finds that index reset and re-enters Intercept instead of reaching the
        // action, rebuilding the whole chain every time (an endless loop, verified by running
        // it). The captured proceed info restores the index for each call, so next() reaches the
        // action from any continuation, as often as it is called.
        var proceed = invocation.CaptureProceedInfo();
        var context = new StoreActionContext((StoreBase)invocation.Proxy, invocation.Method.Name, invocation.Arguments);
        var pipeline = BuildPipeline(context, () => InvokeCoreAsync(invocation, proceed));

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

    static Task<object?> InvokeCoreAsync(IInvocation invocation, IInvocationProceedInfo proceed)
    {
        // ReturnValue is read straight after the call, on the same thread, before anything else
        // can run the action again - so each call (a retry included) sees its own result. The
        // value the proxy hands its caller was already read when Intercept returned, so a later
        // call overwriting ReturnValue cannot change what that caller got.
        proceed.Invoke();
        return invocation.ReturnValue is Task task
            ? AwaitAndBoxResultAsync(task)
            : Task.FromResult(invocation.ReturnValue);
    }

    static async Task<object?> AwaitAndBoxResultAsync(Task task)
    {
        // Deliberately not ConfigureAwait(false): this resumes on whatever context called the
        // action (typically a WPF UI thread), since an interceptor further out in the chain may
        // itself read or write UI-bound store state after awaiting `next()`.
        await task;
        var taskType = task.GetType();
        return taskType.IsGenericType ? taskType.GetProperty(nameof(Task<object>.Result))!.GetValue(task) : null;
    }

    static async Task AwaitDiscardingResultAsync(Task<object?> pipelineResult) => await pipelineResult;

    static async Task<T> AwaitTypedResultAsync<T>(Task<object?> pipelineResult) => (T)(await pipelineResult)!;
}
