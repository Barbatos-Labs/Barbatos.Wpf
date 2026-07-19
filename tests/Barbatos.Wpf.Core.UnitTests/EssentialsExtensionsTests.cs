// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.ApplicationModel.Communication;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Devices.Sensors;
using Barbatos.Wpf.Networking;
using Barbatos.Wpf.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class EssentialsExtensionsTests
{
    [Fact]
    public void AllEssentialsServicesAreRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.NotNull(wpfApp.Services.GetService<IAppInfo>());
        Assert.NotNull(wpfApp.Services.GetService<IPublisherInfo>());
        Assert.NotNull(wpfApp.Services.GetService<IDeviceInfo>());
        Assert.NotNull(wpfApp.Services.GetService<IFileSystem>());
        Assert.NotNull(wpfApp.Services.GetService<IPreferences>());
        Assert.NotNull(wpfApp.Services.GetService<ISecureStorage>());
        Assert.NotNull(wpfApp.Services.GetService<IVersionTracking>());
        Assert.NotNull(wpfApp.Services.GetService<IConnectivity>());
        Assert.NotNull(wpfApp.Services.GetService<IDeviceDisplay>());
        Assert.NotNull(wpfApp.Services.GetService<IEmail>());
        Assert.NotNull(wpfApp.Services.GetService<IContacts>());
        Assert.NotNull(wpfApp.Services.GetService<IGeolocation>());
        Assert.NotNull(wpfApp.Services.GetService<IAppActions>());
        Assert.NotNull(wpfApp.Services.GetService<ILauncher>());
    }

    [Fact]
    public void NoEssentialsServicesAreRegisteredWithoutDefaults()
    {
        var wpfApp = WpfApp.CreateBuilder(useDefaults: false).Build();

        Assert.Null(wpfApp.Services.GetService<IAppInfo>());
        Assert.Null(wpfApp.Services.GetService<IFileSystem>());
        Assert.Null(wpfApp.Services.GetService<IAppActions>());
    }

    [Fact]
    public void RegisteredServicesMatchTheStaticFacades()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Same(AppInfo.Current, wpfApp.Services.GetService<IAppInfo>());
        Assert.Same(PublisherInfo.Current, wpfApp.Services.GetService<IPublisherInfo>());
        Assert.Same(DeviceInfo.Current, wpfApp.Services.GetService<IDeviceInfo>());
        Assert.Same(FileSystem.Current, wpfApp.Services.GetService<IFileSystem>());
        Assert.Same(Preferences.Default, wpfApp.Services.GetService<IPreferences>());
        Assert.Same(SecureStorage.Default, wpfApp.Services.GetService<ISecureStorage>());
        Assert.Same(VersionTracking.Default, wpfApp.Services.GetService<IVersionTracking>());
        Assert.Same(Connectivity.Current, wpfApp.Services.GetService<IConnectivity>());
        Assert.Same(DeviceDisplay.Current, wpfApp.Services.GetService<IDeviceDisplay>());
        Assert.Same(Email.Default, wpfApp.Services.GetService<IEmail>());
        Assert.Same(Contacts.Default, wpfApp.Services.GetService<IContacts>());
        Assert.Same(Geolocation.Default, wpfApp.Services.GetService<IGeolocation>());
        Assert.Same(AppActions.Current, wpfApp.Services.GetService<IAppActions>());
        Assert.Same(Launcher.Default, wpfApp.Services.GetService<ILauncher>());
    }

    [Fact]
    public void ConfigureEssentialsInvokesTheRegisteredDelegate()
    {
        var invoked = false;

        var builder = WpfApp.CreateBuilder();
        builder.ConfigureEssentials(essentials =>
        {
            invoked = true;
            essentials.AddAppAction("id", "Title");
            essentials.OnAppAction(action => { });
            essentials.UseVersionTracking();
        });
        builder.Build();

        Assert.True(invoked);
    }

    [Fact]
    public void ConfigureEssentialsBuilderMethodsAreChainable()
    {
        var builder = WpfApp.CreateBuilder();

        builder.ConfigureEssentials(essentials => essentials
            .AddAppAction("first", "First")
            .AddAppAction(new AppAction("second", "Second"))
            .OnAppAction(action => { })
            .UseVersionTracking());

        // Building must not throw even though AppActions.SetAsync cannot actually apply a
        // Jump List in the test host (no running System.Windows.Application): the exception
        // is caught and logged by the initializer as a best-effort feature, matching .NET
        // MAUI's own EssentialsInitializer.
        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp);
    }

    [Fact]
    public void ConfigureEssentialsWithoutADelegateStillBuilds()
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureEssentials();

        var wpfApp = builder.Build();

        Assert.NotNull(wpfApp);
    }
}
