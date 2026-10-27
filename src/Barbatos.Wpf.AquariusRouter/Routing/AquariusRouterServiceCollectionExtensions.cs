// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// Registers a <see cref="Router"/> with a <c>Microsoft.Extensions.DependencyInjection</c>
/// container.
/// </summary>
/// <remarks>
/// <code>
/// var builder = WpfApp.CreateBuilder();
/// builder.Services.AddSingleton&lt;AuthState&gt;();
///
/// // The route table is built from the container, so a per-route guard can close over a
/// // DI-resolved service without anything having to be constructed by hand first:
/// builder.Services.AddAquariusRouter(
///     sp =&gt; AppRoutes.Build(sp.GetRequiredService&lt;AuthState&gt;()),
///     options =&gt; options.PathOptions = new PathParserOptions { Strict = true });
///
/// var app = builder.Build();
/// app.Services.UseAquarius().UseAquariusRouter();
/// </code>
/// Same two-step split as Aquarius's own <c>AddAquarius</c>/<c>UseAquarius</c>:
/// <see cref="AddAquariusRouter"/> only declares registrations, and
/// <see cref="AquariusRouterServiceProviderExtensions.UseAquariusRouter"/> is the one-shot wiring
/// step that publishes the resolved <see cref="Router"/> as <see cref="Router.Current"/> - the
/// ambient default <see cref="Xaml.RouterView"/>/<see cref="Xaml.RouterLink"/> fall back to when
/// neither sets a <see cref="Router"/> of its own.
/// <para>
/// <see cref="Router"/> and <see cref="IRouterHistory"/> are both registered with
/// <c>TryAdd</c>, so an app that needs something else registers it first and wins - an
/// <see cref="IRouterHistory"/> that persists across restarts, or a <see cref="Router"/> built
/// differently (<c>services.AddSingleton(sp =&gt; new Router(...))</c>). <see cref="Router"/> is
/// sealed, so a replacement is a differently-constructed instance, never a subclass.
/// </para>
/// </remarks>
public static class AquariusRouterServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="Router"/> singleton, its <see cref="IRouterHistory"/>, and
    /// <see cref="RouterOptions"/>, configured from code via <paramref name="configure"/>.
    /// </summary>
    /// <remarks>
    /// Nothing here binds <see cref="RouterOptions"/> from configuration: this package references
    /// only the dependency-injection and options abstractions, not the configuration binder, so
    /// <see cref="RouterOptions.SectionName"/> is a convention name rather than something read
    /// automatically. An app that wants the options in a settings file binds the section itself,
    /// after this call so the file wins over <paramref name="configure"/> -
    /// <c>services.Configure&lt;RouterOptions&gt;(configuration.GetSection(RouterOptions.SectionName))</c>,
    /// with <c>Microsoft.Extensions.Options.ConfigurationExtensions</c>, which a
    /// <c>Barbatos.Wpf.Core</c> host already brings.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="routes">
    /// Builds the route table from the container, so a per-route <see cref="RouteRecord.BeforeEnter"/>
    /// guard can close over a DI-resolved service. Called once, when the <see cref="Router"/> is
    /// first resolved.
    /// </param>
    /// <param name="configure">Optional code-side configuration - see <see cref="RouterOptions"/>.</param>
    public static IServiceCollection AddAquariusRouter(
        this IServiceCollection services,
        Func<IServiceProvider, IEnumerable<RouteRecord>> routes,
        Action<RouterOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(routes);

        var options = services.AddOptions<RouterOptions>();

        if (configure is not null)
            options.Configure(configure);

        services.TryAddSingleton<IRouterHistory, MemoryRouterHistory>();
        services.TryAddSingleton(provider => new Router(
            routes(provider),
            provider.GetRequiredService<IRouterHistory>(),
            provider.GetRequiredService<IOptions<RouterOptions>>().Value.PathOptions));

        return services;
    }
}

/// <summary>
/// Applies the router registrations made by
/// <see cref="AquariusRouterServiceCollectionExtensions.AddAquariusRouter"/> to a built container.
/// </summary>
public static class AquariusRouterServiceProviderExtensions
{
    /// <summary>
    /// Resolves the registered <see cref="Router"/> and publishes it as <see cref="Router.Current"/>
    /// - call once at startup, right after the container is built, so a plain
    /// <c>&lt;aqr:RouterView /&gt;</c> and every <c>aqr:RouterLink.To</c> work with no further
    /// wiring. Returns the resolved router, for the common "…and now run the first navigation"
    /// next step.
    /// </summary>
    /// <remarks>
    /// A multi-window app that wants independent per-window navigation stacks can still hand any
    /// other <see cref="Router"/> to a specific window through
    /// <see cref="Xaml.RouterScope.RouterProperty"/>; this only sets the fallback.
    /// </remarks>
    public static Router UseAquariusRouter(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var router = services.GetRequiredService<Router>();
        Router.Current = router;
        return router;
    }
}
