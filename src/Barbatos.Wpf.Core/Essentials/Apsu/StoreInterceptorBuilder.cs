// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Fluent surface for registering <typeparamref name="TStore"/>'s own local interceptors from
/// inside <see cref="StoreServiceCollectionExtensions.AddStore{TStore}(IServiceCollection, Action{StoreInterceptorBuilder{TStore}})"/> -
/// one <c>AddStore</c> call registers the store and every local interceptor it needs, chaining
/// as many <see cref="Add(IStoreActionInterceptor)"/>-family calls as there are interceptors,
/// instead of a separate <c>AddStoreInterceptor</c> call per interceptor plus a trailing
/// <c>AddStore</c>. Never constructed directly - <c>AddStore</c> hands you one.
/// </summary>
public sealed class StoreInterceptorBuilder<TStore> where TStore : StoreBase
{
    readonly IServiceCollection _services;

    internal StoreInterceptorBuilder(IServiceCollection services) => _services = services;

    /// <summary>Adds <typeparamref name="TInterceptor"/>, DI-constructed the first time
    /// <typeparamref name="TStore"/> is resolved - no <see langword="new"/> needed as long as its
    /// constructor needs nothing but other registered services.</summary>
    public StoreInterceptorBuilder<TStore> Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TInterceptor>()
        where TInterceptor : class, IStoreActionInterceptor
    {
        _services.AddKeyedSingleton<IStoreActionInterceptor, TInterceptor>(typeof(TStore));
        return this;
    }

    /// <summary>Adds an already-constructed interceptor instance.</summary>
    public StoreInterceptorBuilder<TStore> Add(IStoreActionInterceptor interceptor)
    {
        _services.AddKeyedSingleton<IStoreActionInterceptor>(typeof(TStore), interceptor);
        return this;
    }

    /// <summary>Adds an interceptor built by <paramref name="factory"/> the first time
    /// <typeparamref name="TStore"/> is resolved - for an interceptor whose constructor needs a
    /// value the container can't supply on its own (a threshold, a duration, ...) alongside one
    /// or more services it can. <paramref name="factory"/> receives the same
    /// <see cref="IServiceProvider"/> the store itself resolves from, so
    /// <c>sp.GetRequiredService&lt;T&gt;()</c> works normally inside it.</summary>
    public StoreInterceptorBuilder<TStore> Add(Func<IServiceProvider, IStoreActionInterceptor> factory)
    {
        _services.AddKeyedSingleton<IStoreActionInterceptor>(typeof(TStore), (sp, _) => factory(sp));
        return this;
    }
}
