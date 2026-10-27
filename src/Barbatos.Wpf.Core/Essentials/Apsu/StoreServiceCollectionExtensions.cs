// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics.CodeAnalysis;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Registers <see cref="StoreBase"/>-derived stores with the dependency injection container -
/// the <c>defineStore()</c>/<c>useStore()</c> counterpart. Unlike this library's other optional
/// features (<c>ConfigureMcp</c>, <c>ConfigureTrayIcon</c>, ...), there is no single
/// <c>ConfigureStore()</c> gate to call first: a store is a pattern applied per user-defined
/// store type, so <see cref="AddStore{TStore}"/> is the entire surface, and it bootstraps its
/// own <see cref="IStoreRegistry"/> the first time it is called.
/// </summary>
public static class StoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers <typeparamref name="TStore"/> as a singleton, created lazily on first
    /// resolution (matching Pinia's own <c>useStore()</c> laziness) and cached for the lifetime
    /// of the container thereafter (matching Pinia's <c>pinia._s</c> cache, keyed by
    /// <typeparamref name="TStore"/> itself instead of by a string id). <typeparamref name="TStore"/>'s
    /// own constructor may take further dependencies - other services, or other stores.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureInterceptors">
    /// Registers <typeparamref name="TStore"/>'s own local interceptors, if any, in this same
    /// call - <c>i => i.Add&lt;MyInterceptor&gt;().Add(sp => new OtherInterceptor(...))</c> - one
    /// <c>StoreInterceptorBuilder&lt;TStore&gt;.Add(...)</c> per interceptor, chained, instead of a
    /// separate <c>AddStoreInterceptor&lt;TStore, TInterceptor&gt;()</c> call per interceptor plus
    /// a trailing <c>AddStore&lt;TStore&gt;()</c>. Omit entirely for a store with no local
    /// interceptors (the default) - a store that only needs global interceptors, or none at all,
    /// never has to mention interceptors here.
    /// </param>
    /// <remarks>
    /// <para>
    /// Interceptors and plugins are read when <typeparamref name="TStore"/> is first resolved, not
    /// when this method runs, so they may be registered before or after it; from then on that
    /// store's chain is fixed (an <see cref="InterceptorHandle"/> can still eject one link).
    /// </para>
    /// <para>
    /// With no interceptor applying, the store is built by <see cref="ActivatorUtilities"/>. As
    /// soon as one does - a <b>global</b> interceptor applies to every store, so adding one can
    /// change a store that has none of its own - it is built as a Castle DynamicProxy subclass
    /// instead, which demands more of <typeparamref name="TStore"/>: it must be non-sealed
    /// (<see cref="InvalidOperationException"/> otherwise), <see langword="public"/> along with
    /// every enclosing type (Castle's generated assembly cannot subclass anything less visible and
    /// throws <see cref="ArgumentException"/>), and have exactly one public constructor
    /// (<see cref="InvalidOperationException"/> otherwise). Each parameter of that constructor is
    /// resolved with <c>GetRequiredService</c>: unlike <see cref="ActivatorUtilities"/>, an
    /// optional parameter's default value is not used, so a parameter whose type is not
    /// registered throws <see cref="InvalidOperationException"/> even when it has a default.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(
        this IServiceCollection services, Action<StoreInterceptorBuilder<TStore>>? configureInterceptors = null)
        where TStore : StoreBase
    {
        services.TryAddSingleton<StoreRegistry>();
        services.TryAddSingleton<IStoreRegistry>(sp => sp.GetRequiredService<StoreRegistry>());

        configureInterceptors?.Invoke(new StoreInterceptorBuilder<TStore>(services));

        services.AddSingleton(CreateStore<TStore>);

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TInterceptor"/> as a <b>local</b> interceptor: it only wraps
    /// actions on <typeparamref name="TStore"/>, unlike a <b>global</b> one registered directly
    /// with <c>services.AddSingleton&lt;IStoreActionInterceptor, T&gt;()</c> (every store).
    /// Backed by keyed DI (<c>AddKeyedSingleton</c>, keyed by <see langword="typeof"/>(<typeparamref name="TStore"/>)) -
    /// call this more than once for the same <typeparamref name="TStore"/> to register several
    /// local interceptors; they run in registration order, innermost (closest to the action)
    /// relative to every global interceptor.
    /// </summary>
    public static IServiceCollection AddStoreInterceptor<TStore, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TInterceptor>(this IServiceCollection services)
        where TStore : StoreBase
        where TInterceptor : class, IStoreActionInterceptor
    {
        services.AddKeyedSingleton<IStoreActionInterceptor, TInterceptor>(typeof(TStore));
        return services;
    }

    /// <inheritdoc cref="AddStoreInterceptor{TStore, TInterceptor}(IServiceCollection)"/>
    public static IServiceCollection AddStoreInterceptor<TStore>(this IServiceCollection services, IStoreActionInterceptor interceptor)
        where TStore : StoreBase
    {
        services.AddKeyedSingleton<IStoreActionInterceptor>(typeof(TStore), interceptor);
        return services;
    }

    /// <summary>
    /// Registers a <b>local</b> interceptor for <typeparamref name="TStore"/>, built by
    /// <paramref name="factory"/> the first time <typeparamref name="TStore"/> is itself resolved -
    /// for an interceptor whose constructor needs a value the container can't supply on its own (a
    /// threshold, a duration, ...) alongside one or more services it can. <paramref name="factory"/>
    /// receives the same <see cref="IServiceProvider"/> the store itself resolves from, so
    /// <c>sp.GetRequiredService&lt;T&gt;()</c> works normally inside it - this is still DI
    /// construction, just explicit about the one input that has to come from the call site rather
    /// than the container. Prefer <see cref="AddStoreInterceptor{TStore, TInterceptor}(IServiceCollection)"/>
    /// instead when the interceptor's constructor needs nothing but registered services - it needs
    /// no factory, or any other call from here, at all.
    /// </summary>
    public static IServiceCollection AddStoreInterceptor<TStore>(this IServiceCollection services, Func<IServiceProvider, IStoreActionInterceptor> factory)
        where TStore : StoreBase
    {
        services.AddKeyedSingleton<IStoreActionInterceptor>(typeof(TStore), (sp, _) => factory(sp));
        return services;
    }

    /// <summary>
    /// Registers <paramref name="interceptor"/> as a <b>global</b> interceptor (wraps every
    /// store's actions) - the same as <c>services.AddSingleton&lt;IStoreActionInterceptor&gt;(interceptor)</c>,
    /// but wrapped so it can be removed later via <paramref name="handle"/> - the
    /// <c>axios.interceptors.request.eject(id)</c> counterpart. Use the plain
    /// <c>AddSingleton&lt;IStoreActionInterceptor, T&gt;()</c> registration instead if this
    /// interceptor never needs to be ejectable.
    /// </summary>
    public static IServiceCollection AddStoreInterceptor(this IServiceCollection services, IStoreActionInterceptor interceptor, out InterceptorHandle handle)
    {
        handle = new InterceptorHandle();
        services.AddSingleton<IStoreActionInterceptor>(new EjectableInterceptor(interceptor, handle));
        return services;
    }

    /// <summary>
    /// Registers <paramref name="interceptor"/> as a <b>local</b> interceptor on
    /// <typeparamref name="TStore"/> - the ejectable counterpart of
    /// <see cref="AddStoreInterceptor{TStore}(IServiceCollection, IStoreActionInterceptor)"/>;
    /// see <see cref="AddStoreInterceptor(IServiceCollection, IStoreActionInterceptor, out InterceptorHandle)"/>
    /// for what ejecting does.
    /// </summary>
    public static IServiceCollection AddStoreInterceptor<TStore>(this IServiceCollection services, IStoreActionInterceptor interceptor, out InterceptorHandle handle)
        where TStore : StoreBase
    {
        handle = new InterceptorHandle();
        services.AddKeyedSingleton<IStoreActionInterceptor>(typeof(TStore), new EjectableInterceptor(interceptor, handle));
        return services;
    }

    static TStore CreateStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(IServiceProvider services) where TStore : StoreBase
    {
        // Global interceptors wrap outside every local one - closer to "every store" scope wraps
        // closer to "just this store" scope, the same nesting a global ASP.NET Core middleware
        // pipeline has relative to endpoint-specific behavior.
        var interceptors = services.GetServices<IStoreActionInterceptor>()
            .Concat(services.GetKeyedServices<IStoreActionInterceptor>(typeof(TStore)))
            .ToList();

        var store = interceptors.Count == 0
            ? ActivatorUtilities.CreateInstance<TStore>(services)
            : CreateInterceptedStore<TStore>(services, interceptors);

        foreach (var plugin in services.GetServices<IStorePlugin>())
            plugin.Apply(store);

        services.GetRequiredService<StoreRegistry>().Track(store);

        return store;
    }

    static readonly ProxyGenerator ProxyGenerator = new();

    /// <summary>
    /// Builds <typeparamref name="TStore"/> behind a Castle DynamicProxy class proxy so every
    /// <see cref="ActionAttribute"/>-marked method routes through <see cref="ActionInterceptor"/> -
    /// only reached when at least one <see cref="IStoreActionInterceptor"/> is registered, so a
    /// store that never uses interceptors keeps the plain, proxy-free <see cref="ActivatorUtilities"/>
    /// path above untouched.
    /// </summary>
    static TStore CreateInterceptedStore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStore>(
        IServiceProvider services, IReadOnlyList<IStoreActionInterceptor> interceptors) where TStore : StoreBase
    {
        if (typeof(TStore).IsSealed)
        {
            throw new InvalidOperationException(
                $"{typeof(TStore).Name} must not be sealed to use action interceptors - Castle DynamicProxy needs " +
                $"to subclass it to intercept its [Action]-marked virtual methods. Remove 'sealed', or don't " +
                $"register any {nameof(IStoreActionInterceptor)} if this store doesn't need interception.");
        }

        var constructors = typeof(TStore).GetConstructors();
        if (constructors.Length != 1)
        {
            throw new InvalidOperationException(
                $"{typeof(TStore).Name} must have exactly one public constructor to use action interceptors " +
                $"(found {constructors.Length}).");
        }

        var constructorArguments = constructors[0].GetParameters()
            .Select(parameter => services.GetRequiredService(parameter.ParameterType))
            .ToArray();

        return (TStore)ProxyGenerator.CreateClassProxy(
            typeof(TStore), constructorArguments, new ActionInterceptor(interceptors));
    }
}
