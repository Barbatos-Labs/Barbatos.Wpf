// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Globalization;
using Barbatos.Wpf.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class AppInfoTests
{
    [Fact]
    public void CurrentIsCached()
    {
        Assert.NotNull(AppInfo.Current);
        Assert.Same(AppInfo.Current, AppInfo.Current);
    }

    [Fact]
    public void NameIsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(AppInfo.Name));
    }

    [Fact]
    public void AppIdIsNotEmpty()
    {
        Assert.False(string.IsNullOrEmpty(AppInfo.AppId));
    }

    [Fact]
    public void ObsoleteAppGuidStillForwardsToAppId()
    {
#pragma warning disable CS0618 // AppGuid is obsolete — that is exactly what this test covers.
        Assert.Equal(AppInfo.AppId, AppInfo.AppGuid);
        Assert.Equal(AppInfo.AppId, AppInfo.Current.AppGuid);
#pragma warning restore CS0618
    }

    [Fact]
    public void VersionStringMatchesVersion()
    {
        Assert.Equal(AppInfo.Version.ToString(), AppInfo.VersionString);
    }

    [Fact]
    public void BuildStringIsTheVersionRevision()
    {
        Assert.Equal(AppInfo.Version.Revision.ToString(CultureInfo.InvariantCulture), AppInfo.BuildString);
    }

    [Fact]
    public void TestHostIsUnpackaged()
    {
        Assert.Equal(AppPackagingModel.Unpackaged, AppInfo.PackagingModel);
    }

    [Fact]
    public void RequestedThemeIsADefinedValue()
    {
        Assert.True(Enum.IsDefined(AppInfo.RequestedTheme));
    }

    [Fact]
    public void RequestedLayoutDirectionMatchesTheCurrentCulture()
    {
        var expected = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft
            ? LayoutDirection.RightToLeft
            : LayoutDirection.LeftToRight;

        Assert.Equal(expected, AppInfo.RequestedLayoutDirection);
    }

    [Fact]
    public void InstallDateIsNullWhenThereIsNoMatchingUninstallEntry()
    {
        // The test host's AppId falls back to the assembly title/name, and nothing ever
        // installed a "...\Uninstall\{AppId}" registry entry under that name — this is also
        // the common case for any app that runs from the IDE rather than an installer.
        Assert.Null(AppInfo.InstallDate);
    }

    [Fact]
    public void InstallLocationIsNullWhenThereIsNoMatchingUninstallEntry()
    {
        Assert.Null(AppInfo.InstallLocation);
    }

    [Fact]
    public void AppInfoIsRegisteredInTheDefaultBuilder()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        var appInfo = wpfApp.Services.GetService<IAppInfo>();

        Assert.NotNull(appInfo);
        Assert.Same(AppInfo.Current, appInfo);
    }

    [Fact]
    public void AppInfoIsNotRegisteredWithoutDefaults()
    {
        var wpfApp = WpfApp.CreateBuilder(useDefaults: false).Build();

        Assert.Null(wpfApp.Services.GetService<IAppInfo>());
    }
}
