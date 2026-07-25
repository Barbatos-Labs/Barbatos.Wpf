// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Marks a <see langword="public virtual"/> method on a <see cref="StoreBase"/>-derived store as
/// an action that every applicable <see cref="IStoreActionInterceptor"/> (global and/or local -
/// see <see cref="StoreServiceCollectionExtensions.AddStoreInterceptor{TStore, TInterceptor}"/>)
/// wraps - the <c>$onAction</c> counterpart. Only takes effect when at least one interceptor
/// applies to this store (see <see cref="StoreServiceCollectionExtensions.AddStore{TStore}"/>);
/// the method must be <see langword="virtual"/> and the store must not be <see langword="sealed"/>,
/// since interception works by generating a runtime subclass that overrides every attributed
/// method. Supports synchronous return values and <see cref="Task"/>/<see cref="Task{TResult}"/> -
/// a <see cref="System.Threading.Tasks.ValueTask"/>/<see cref="ValueTask{TResult}"/>-returning
/// method is not recognized as asynchronous by the interceptor chain (it isn't a
/// <see cref="Task"/>), so it is treated as synchronous - the still-unfinished
/// <see cref="ValueTask"/> itself becomes the chain's "result" rather than something the chain
/// waits for - use <see cref="Task"/> instead if this method needs interception.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ActionAttribute : Attribute
{
}
