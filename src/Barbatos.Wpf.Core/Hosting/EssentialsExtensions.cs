// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.ApplicationModel.Communication;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Devices.Sensors;
using Barbatos.Wpf.LifecycleEvents;
using Barbatos.Wpf.Networking;
using Barbatos.Wpf.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Configures the optional Essentials features added from code (app actions, version
/// tracking). Mirrors .NET MAUI's <c>IEssentialsBuilder</c>.
/// </summary>
public interface IEssentialsBuilder
{
    /// <summary>
    /// Adds an app action, shown on the Windows taskbar Jump List.
    /// </summary>
    /// <remarks>
    /// The actions added here are applied during <c>Build()</c>, which is also what lets a
    /// relaunched process recognise a click on one of them: an action that exists only through a
    /// later <see cref="AppActions.SetAsync(IEnumerable{AppAction})"/> call is not recognised
    /// after a restart.
    /// </remarks>
    IEssentialsBuilder AddAppAction(AppAction appAction);

    /// <summary>
    /// Registers a handler invoked whenever any app action is activated.
    /// </summary>
    /// <remarks>
    /// The handler runs during startup, from the <c>WpfLifecycle.OnStartup</c> event, before
    /// any main window exists: record the action and act on it after the main window is shown.
    /// Under <c>ConfigureSingleInstance</c>, a click while the app is already running is not
    /// reported - see <see cref="IAppActions.AppActionActivated"/>.
    /// </remarks>
    IEssentialsBuilder OnAppAction(Action<AppAction> action);

    /// <summary>
    /// Starts tracking the app's version/build history (see <see cref="VersionTracking"/>) as
    /// soon as the host is built.
    /// </summary>
    IEssentialsBuilder UseVersionTracking();
}

/// <summary>
/// The WPF counterpart of .NET MAUI's <c>EssentialsMauiAppBuilderExtensions</c>: registers the
/// essentials services (<see cref="IAppInfo"/>, <see cref="IDeviceInfo"/>, ...) in the service
/// collection and wires up the optional <see cref="ConfigureEssentials"/> features.
/// </summary>
public static class EssentialsExtensions
{
    /// <summary>
    /// Registers the essentials services so they can be resolved from the container in
    /// addition to their <c>.Current</c>/<c>.Default</c> statics, and wires app-action
    /// activation detection into the application startup lifecycle event. Called by default
    /// when the builder is created with defaults.
    /// </summary>
    public static WpfAppBuilder UseEssentials(this WpfAppBuilder builder)
    {
        builder.Services.TryAddSingleton<IAppInfo>(sp => AppInfo.Current);
        builder.Services.TryAddSingleton<IPublisherInfo>(sp => PublisherInfo.Current);
        builder.Services.TryAddSingleton<IDeviceIdentity>(sp => DeviceIdentity.Default);
        builder.Services.TryAddSingleton<IDeviceInfo>(sp => DeviceInfo.Current);
        builder.Services.TryAddSingleton<IFileSystem>(sp => FileSystem.Current);
        builder.Services.TryAddSingleton<IPreferences>(sp => Preferences.Default);
        builder.Services.TryAddSingleton<ISecureStorage>(sp => SecureStorage.Default);
        builder.Services.TryAddSingleton<IVersionTracking>(sp => VersionTracking.Default);
        builder.Services.TryAddSingleton<IConnectivity>(sp => Connectivity.Current);
        builder.Services.TryAddSingleton<IDeviceDisplay>(sp => DeviceDisplay.Current);
        builder.Services.TryAddSingleton<IEmail>(sp => Email.Default);
        builder.Services.TryAddSingleton<IContacts>(sp => Contacts.Default);
        builder.Services.TryAddSingleton<IGeolocation>(sp => Geolocation.Default);
        builder.Services.TryAddSingleton<IAppActions>(sp => AppActions.Current);
        builder.Services.TryAddSingleton<ILauncher>(sp => Launcher.Default);

        // Mirrors .NET MAUI's life.AddWindows(windows => windows.OnLaunched((app, args) =>
        // ApplicationModel.Platform.OnLaunched(args))): detect an app-action activation from
        // the process's startup command-line arguments.
        builder.ConfigureLifecycleEvents(life => life.AddWpf(wpf => wpf
            .OnStartup((app, args) =>
            {
                if (AppActions.Current is AppActionsImplementation impl)
                    impl.HandleStartupArguments(args.Args);
            })));

        return builder;
    }

    /// <summary>
    /// Adds the optional Essentials features (app actions, version tracking) configured from
    /// code via <paramref name="configureDelegate"/>.
    /// </summary>
    public static WpfAppBuilder ConfigureEssentials(this WpfAppBuilder builder, Action<IEssentialsBuilder>? configureDelegate = null)
    {
        if (configureDelegate != null)
        {
            builder.Services.AddSingleton(new EssentialsRegistration(configureDelegate));
        }

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, EssentialsInitializer>());

        return builder;
    }

    /// <inheritdoc cref="IEssentialsBuilder.AddAppAction(AppAction)" />
    /// <param name="essentials">The Essentials builder.</param>
    /// <param name="id">A unique identifier used to respond to the action tap.</param>
    /// <param name="title">The visible title to display on the taskbar Jump List.</param>
    /// <param name="subtitle">If supported, a sub-title to display under the title.</param>
    /// <param name="icon">The path of an <c>.ico</c> file to show next to the title.</param>
    public static IEssentialsBuilder AddAppAction(this IEssentialsBuilder essentials, string id, string title, string? subtitle = null, string? icon = null) =>
        essentials.AddAppAction(new AppAction(id, title, subtitle, icon));

    internal class EssentialsRegistration
    {
        readonly Action<IEssentialsBuilder> _registerEssentials;

        public EssentialsRegistration(Action<IEssentialsBuilder> registerEssentials)
        {
            _registerEssentials = registerEssentials;
        }

        internal void RegisterEssentialsOptions(IEssentialsBuilder essentials) =>
            _registerEssentials(essentials);
    }

    class EssentialsInitializer : IWpfInitializeService
    {
        readonly IEnumerable<EssentialsRegistration> _essentialsRegistrations;
        EssentialsBuilder? _essentialsBuilder;

        public EssentialsInitializer(IEnumerable<EssentialsRegistration> essentialsRegistrations)
        {
            _essentialsRegistrations = essentialsRegistrations;
        }

        public void Initialize(IServiceProvider services)
        {
            _essentialsBuilder = new EssentialsBuilder();
            foreach (var essentialsRegistration in _essentialsRegistrations)
                essentialsRegistration.RegisterEssentialsOptions(_essentialsBuilder);

            AppActions.OnAppAction += HandleOnAppAction;

            if (_essentialsBuilder.AppActions is not null)
                SetAppActions(services, _essentialsBuilder.AppActions);

            if (_essentialsBuilder.TrackVersions)
                VersionTracking.Track();
        }

        static async void SetAppActions(IServiceProvider services, List<AppAction> appActions)
        {
            try
            {
                await AppActions.SetAsync(appActions);
            }
            catch (Exception ex)
            {
                // Applying the app actions is best-effort: a failure here (unsupported
                // platform, or — as in a unit test host — no running WPF Application to
                // attach the Jump List to) must never prevent the rest of the app from
                // starting.
                services.GetService<ILoggerFactory>()?
                    .CreateLogger<IEssentialsBuilder>()?
                    .LogError(ex, "App Actions could not be applied.");
            }
        }

        void HandleOnAppAction(object? sender, AppActionEventArgs e) =>
            _essentialsBuilder?.AppActionHandlers?.Invoke(e.AppAction);
    }

    class EssentialsBuilder : IEssentialsBuilder
    {
        List<AppAction>? _appActions;
        internal Action<AppAction>? AppActionHandlers;
        internal bool TrackVersions;

        internal List<AppAction>? AppActions => _appActions;

        public IEssentialsBuilder AddAppAction(AppAction appAction)
        {
            _appActions ??= [];
            _appActions.Add(appAction);
            return this;
        }

        public IEssentialsBuilder OnAppAction(Action<AppAction> action)
        {
            AppActionHandlers += action;
            return this;
        }

        public IEssentialsBuilder UseVersionTracking()
        {
            TrackVersions = true;
            return this;
        }
    }
}
