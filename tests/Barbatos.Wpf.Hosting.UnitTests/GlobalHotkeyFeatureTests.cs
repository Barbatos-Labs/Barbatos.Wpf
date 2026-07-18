// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Hotkeys;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Hosting.UnitTests;

public class GlobalHotkeyFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IGlobalHotkeyService>());
    }

    [Fact]
    public void RegisteredHotkeysUseTheDefaultGesture()
    {
        var platform = new FakeHotkeyPlatform();
        var service = BuildService(platform, hotkeys => hotkeys.Add("QuickEntry", "Control+Alt+Space"));

        var hotkey = Assert.Single(service.Hotkeys);
        Assert.Equal("QuickEntry", hotkey.Name);
        Assert.Equal(HotkeyGesture.Parse("Control+Alt+Space"), hotkey.Gesture);
    }

    [Fact]
    public void HotkeysAreRegisteredWithTheOsDuringBuildByDefault()
    {
        var platform = new FakeHotkeyPlatform();
        var service = BuildService(platform, hotkeys => hotkeys.Add("QuickEntry", "Control+Alt+Space"));

        Assert.True(service.IsEnabled);
        Assert.Equal(HotkeyGesture.Parse("Control+Alt+Space"), Assert.Single(platform.Registered).Value);
    }

    [Fact]
    public void ConfigurationCanDisableTheHotkeys()
    {
        var platform = new FakeHotkeyPlatform();

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:GlobalHotkeys:Enabled"] = "false",
        });
        builder.Services.AddSingleton<IHotkeyPlatform>(platform);
        builder.ConfigureGlobalHotkeys(hotkeys => hotkeys.Add("QuickEntry", "Control+Alt+Space"));
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<IGlobalHotkeyService>();

        Assert.False(service.IsEnabled);
        Assert.Empty(platform.Registered);
    }

    [Fact]
    public void ConfigurationOverridesTheDefaultGesture()
    {
        var platform = new FakeHotkeyPlatform();

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:GlobalHotkeys:Gestures:QuickEntry"] = "Control+Shift+K",
        });
        builder.Services.AddSingleton<IHotkeyPlatform>(platform);
        builder.ConfigureGlobalHotkeys(hotkeys => hotkeys.Add("QuickEntry", "Control+Alt+Space"));
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<IGlobalHotkeyService>();

        Assert.Equal(HotkeyGesture.Parse("Control+Shift+K"), Assert.Single(service.Hotkeys).Gesture);
    }

    [Fact]
    public void PressingTheHotkeyInvokesTheCallbackAndTheEvent()
    {
        var platform = new FakeHotkeyPlatform();
        var callbackCount = 0;
        var service = BuildService(platform, hotkeys => hotkeys.Add("QuickEntry", "Control+Alt+Space", () => callbackCount++));

        string? pressedName = null;
        service.HotkeyPressed += (sender, args) => pressedName = args.Hotkey.Name;

        platform.RaiseHotkeyPressed(0);

        Assert.Equal(1, callbackCount);
        Assert.Equal("QuickEntry", pressedName);
    }

    [Fact]
    public void SetEnabledFalseUnregistersAllHotkeys()
    {
        var platform = new FakeHotkeyPlatform();
        var service = BuildService(platform, hotkeys => hotkeys
            .Add("First", "Control+Alt+A")
            .Add("Second", "Control+Alt+B"));

        service.SetEnabled(false);

        Assert.False(service.IsEnabled);
        Assert.Empty(platform.Registered);
        Assert.Equal(new[] { 0, 1 }, platform.Unregistered);
    }

    [Fact]
    public void UpdateGestureReRegistersWhileEnabled()
    {
        var platform = new FakeHotkeyPlatform();
        var service = BuildService(platform, hotkeys => hotkeys.Add("QuickEntry", "Control+Alt+Space"));

        service.UpdateGesture("QuickEntry", HotkeyGesture.Parse("Control+Shift+K"));

        Assert.Equal(HotkeyGesture.Parse("Control+Shift+K"), Assert.Single(service.Hotkeys).Gesture);
        Assert.Equal(HotkeyGesture.Parse("Control+Shift+K"), platform.Registered[0]);
        Assert.Contains(0, platform.Unregistered);
    }

    [Fact]
    public void UpdateGestureThrowsForUnknownHotkeys()
    {
        var platform = new FakeHotkeyPlatform();
        var service = BuildService(platform, hotkeys => hotkeys.Add("QuickEntry", "Control+Alt+Space"));

        Assert.Throws<ArgumentException>(() => service.UpdateGesture("Unknown", HotkeyGesture.Parse("Control+K")));
    }

    static IGlobalHotkeyService BuildService(FakeHotkeyPlatform platform, Action<IGlobalHotkeyBuilder>? configureDelegate = null)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IHotkeyPlatform>(platform);
        builder.ConfigureGlobalHotkeys(configureDelegate);
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IGlobalHotkeyService>();
    }
}
