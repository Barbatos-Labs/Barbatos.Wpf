// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.LifecycleEvents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// A builder for WPF applications and services.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>MauiAppBuilder</c>.
/// </remarks>
public sealed class WpfAppBuilder : IHostApplicationBuilder
{
    private readonly ServiceCollection _services = new();
    private Func<IServiceProvider>? _createServiceProvider;
    private readonly Lazy<ConfigurationManager> _configuration;
    private readonly Lazy<WpfHostEnvironment> _hostEnvironment;
    private readonly Lazy<WpfMetricsBuilder> _metricsBuilder;
    private ILoggingBuilder? _logging;
    private readonly IDictionary<object, object> _properties;

    internal WpfAppBuilder(bool useDefaults)
    {
        // Lazy-load these classes, so they aren't created if they are never used.
        // Don't capture the 'this' variable in AddSingleton, so WpfAppBuilder can be GC'd.
        var configuration = new Lazy<ConfigurationManager>(() =>
        {
            var configurationManager = new ConfigurationManager();

            // Bootstrap source, added first so it's the lowest-priority: appsettings.json,
            // command-line args, or any other source the app adds afterwards can still
            // override it. This mirrors the "DOTNET_"-prefixed environment variables source
            // Microsoft.Extensions.Hosting.HostBuilder wires up before building the rest of
            // its configuration, which is what makes DOTNET_ENVIRONMENT flow into
            // HostDefaults.EnvironmentKey (and hence WpfHostEnvironment.EnvironmentName) below.
            configurationManager.AddEnvironmentVariables(prefix: "DOTNET_");

            return configurationManager;
        });
        var hostEnvironment = new Lazy<WpfHostEnvironment>(() => new WpfHostEnvironment
        {
            EnvironmentName = configuration.Value[HostDefaults.EnvironmentKey] ?? Environments.Production,
        });
        var metricsBuilder = new Lazy<WpfMetricsBuilder>(() => new WpfMetricsBuilder(Services));
        Services.AddSingleton<IConfiguration>(sp => configuration.Value);
        Services.AddSingleton<IHostEnvironment>(sp => hostEnvironment.Value);
        Services.AddSingleton<IMetricsBuilder>(sp => metricsBuilder.Value);

        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _metricsBuilder = metricsBuilder;

        _properties = new Dictionary<object, object>();

        if (useDefaults)
        {
            // Register required services
            this.ConfigureLifecycleEvents(configureDelegate: null);
            this.ConfigureDispatching();

            this.UseEssentials();
        }
    }

    /// <summary>
    /// A collection of services for the application to compose. This is useful for adding user provided or framework provided services.
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// A collection of configuration providers for the application to compose. This is useful for adding new configuration sources and providers.
    /// </summary>
    public ConfigurationManager Configuration => _configuration.Value;

    IConfigurationManager IHostApplicationBuilder.Configuration => Configuration;

    /// <summary>
    /// A collection of logging providers for the application to compose. This is useful for adding new logging providers.
    /// </summary>
    public ILoggingBuilder Logging
    {
        get
        {
            return _logging ??= InitializeLogging();

            ILoggingBuilder InitializeLogging()
            {
                // if someone accesses the Logging builder, ensure Logging has been initialized.
                Services.AddLogging();
                return new LoggingBuilder(Services);
            }
        }
    }

    /// <summary>
    /// Gets a central location for sharing state between components during the host building process.
    /// </summary>
    public IDictionary<object, object> Properties => _properties;

    IDictionary<object, object> IHostApplicationBuilder.Properties => Properties;

    /// <summary>
    /// Information about the environment an application is running in.
    /// </summary>
    public WpfHostEnvironment Environment => _hostEnvironment.Value;

    IHostEnvironment IHostApplicationBuilder.Environment => Environment;

    /// <summary>
    /// Gets a builder for configuring metrics services.
    /// </summary>
    internal WpfMetricsBuilder Metrics => _metricsBuilder.Value;

    IMetricsBuilder IHostApplicationBuilder.Metrics => Metrics;

    /// <summary>
    /// Registers a <see cref="IServiceProviderFactory{TBuilder}" /> instance to be used to create the <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="factory">The <see cref="IServiceProviderFactory{TBuilder}" />.</param>
    /// <param name="configure">
    /// A delegate used to configure the <typeparamref name="TBuilder" />. This can be used to configure services using
    /// APIs specific to the <see cref="IServiceProviderFactory{TBuilder}" /> implementation.
    /// </param>
    /// <typeparam name="TBuilder">The type of builder provided by the <see cref="IServiceProviderFactory{TBuilder}" />.</typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="ConfigureContainer{TBuilder}(IServiceProviderFactory{TBuilder}, Action{TBuilder})"/> is called by <see cref="Build"/>
    /// and so the delegate provided by <paramref name="configure"/> will run after all other services have been registered.
    /// </para>
    /// <para>
    /// Multiple calls to <see cref="ConfigureContainer{TBuilder}(IServiceProviderFactory{TBuilder}, Action{TBuilder})"/> will replace
    /// the previously stored <paramref name="factory"/> and <paramref name="configure"/> delegate.
    /// </para>
    /// </remarks>
    public void ConfigureContainer<TBuilder>(IServiceProviderFactory<TBuilder> factory, Action<TBuilder>? configure = null) where TBuilder : notnull
    {
        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        _createServiceProvider = () =>
        {
            var container = factory.CreateBuilder(Services);
            configure?.Invoke(container);
            return factory.CreateServiceProvider(container);
        };
    }

    /// <summary>
    /// Builds the <see cref="WpfApp"/>.
    /// </summary>
    /// <returns>A configured <see cref="WpfApp"/>.</returns>
    public WpfApp Build()
    {
        ConfigureDefaultLogging();

        IServiceProvider serviceProvider = _createServiceProvider != null
            ? _createServiceProvider()
            : _services.BuildServiceProvider();

        // Mark the service collection as read-only to prevent future modifications
        _services.MakeReadOnly();

        WpfApp builtApplication = new WpfApp(serviceProvider);

        // Initialize any singleton/app services, for example the OS hooks
        builtApplication.InitializeAppServices();

        return builtApplication;
    }

    private sealed class LoggingBuilder : ILoggingBuilder
    {
        public LoggingBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }

    private void ConfigureDefaultLogging()
    {
        // By default, if no one else has configured logging, add a "no-op" LoggerFactory
        // and Logger services with no providers. This way when components try to get an
        // ILogger<> from the IServiceProvider, they don't get 'null'.
        Services.TryAdd(ServiceDescriptor.Singleton<ILoggerFactory, NullLoggerFactory>());
        Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
    }

    private sealed class NullLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose() { }
    }

    private sealed class NullLogger<T> : ILogger<T>, IDisposable
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => this;

        public void Dispose() { }

        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }
    }
}
