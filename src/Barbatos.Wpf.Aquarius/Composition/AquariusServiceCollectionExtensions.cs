// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Aquarius.Composition;

/// <summary>
/// Registers Aquarius with a <c>Microsoft.Extensions.DependencyInjection</c> container.
/// </summary>
/// <remarks>
/// <code>
/// var builder = WpfApp.CreateBuilder();
/// builder.Services.AddAquarius(options => options.ThrowOnUnresolvedViewModels = true);
///
/// var app = builder.Build();
/// app.Services.UseAquarius();
/// </code>
/// Two calls, deliberately, matching the split this repository already uses everywhere else:
/// <see cref="AddAquarius"/> only declares registrations (nothing is resolved, nothing global is
/// touched), and <see cref="AquariusServiceProviderExtensions.UseAquarius"/> is the one-shot
/// wiring step you run once the container exists - the same shape as Core's own
/// <c>UseEssentials</c>.
/// <para>
/// The wiring step is what a XAML-driven library needs and a pure service library does not:
/// <c>aq:Setup.Enable</c> and <see cref="Xaml.Component.Is"/> both resolve types from
/// markup, where there is no constructor to inject into and no scope to resolve from, so they go
/// through the ambient <see cref="Setup.ServiceProvider"/>. <c>UseAquarius</c> is what points that
/// at your container - replacing the bare <c>Setup.ServiceProvider = app.Services</c> line an app
/// used to have to remember on its own.
/// </para>
/// </remarks>
public static class AquariusServiceCollectionExtensions
{
    /// <summary>
    /// Registers Aquarius's own options, configured from code via <paramref name="configure"/>.
    /// Call <see cref="AquariusServiceProviderExtensions.UseAquarius"/> once the container is
    /// built to actually apply them.
    /// </summary>
    /// <remarks>
    /// Nothing here reads configuration: this package depends on
    /// <c>Microsoft.Extensions.Options</c> and the DI abstractions only, deliberately not on
    /// <c>Microsoft.Extensions.Configuration</c>, so it stays installable in an app with no
    /// configuration system at all. <see cref="AquariusOptions.SectionName"/> names the section
    /// this repository's <c>Barbatos:</c> convention reserves for these options; an app that wants
    /// them in <c>appsettings.json</c> binds it itself, after this call so file values override
    /// code values (<c>Bind</c> comes from <c>Microsoft.Extensions.Options.ConfigurationExtensions</c>,
    /// which a Barbatos.Wpf.Core app already has):
    /// <code>
    /// builder.Services.AddAquarius(options => options.ThrowOnUnresolvedViewModels = true);
    /// builder.Services.AddOptions&lt;AquariusOptions&gt;().Bind(builder.Configuration.GetSection(AquariusOptions.SectionName));
    /// </code>
    /// <see cref="AquariusOptions.ViewModelResolver"/> is a delegate and cannot come from a file.
    /// </remarks>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configure">Optional code-side configuration - see <see cref="AquariusOptions"/>.</param>
    public static IServiceCollection AddAquarius(this IServiceCollection services, Action<AquariusOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<AquariusOptions>();

        if (configure is not null)
            options.Configure(configure);

        return services;
    }
}

/// <summary>
/// Applies the Aquarius registrations made by
/// <see cref="AquariusServiceCollectionExtensions.AddAquarius"/> to a built container.
/// </summary>
public static class AquariusServiceProviderExtensions
{
    /// <summary>
    /// Points Aquarius's markup-driven type resolution at <paramref name="services"/> and applies
    /// <see cref="AquariusOptions"/> - call once at startup, right after the container is built.
    /// </summary>
    /// <remarks>
    /// Safe to call without <see cref="AquariusServiceCollectionExtensions.AddAquarius"/> having
    /// run: the options lookup is optional, so this still does the useful half (wiring
    /// <see cref="Setup.ServiceProvider"/>) for an app that only wants DI-resolved ViewModels and
    /// has nothing to configure.
    /// </remarks>
    /// <param name="services">The built container - returned unchanged, so this can be chained.</param>
    public static IServiceProvider UseAquarius(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Setup.ServiceProvider = services;

        if (services.GetService<IOptions<AquariusOptions>>()?.Value is not { } options)
            return services;

        Setup.ThrowOnUnresolved = options.ThrowOnUnresolvedViewModels;
        Xaml.Expr.ThrowOnUnresolvedIdentifiers = options.ThrowOnUnresolvedExprIdentifiers;

        if (options.ViewModelResolver is { } resolver)
            Setup.Resolver = resolver;

        return services;
    }
}
