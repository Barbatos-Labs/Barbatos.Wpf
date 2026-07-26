// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;
using Barbatos.Wpf.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class InputSystemFeatureTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IInputSystemService>());
    }

    [Fact]
    public void EnabledByDefaultOnceConfigured()
    {
        var platform = new FakeHotkeyPlatform();
        var service = BuildService(platform);

        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void ConfigurationCanDisableTheFeature()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("QuickEntry").AddGlobalBinding(Key.F8, ModifierKeys.Control);

        var builder = WpfApp.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Barbatos:InputSystem:Enabled"] = "false",
        });
        builder.Services.AddSingleton<IHotkeyPlatform>(platform);
        builder.ConfigureInputSystem(options => options.ActionMaps.Add(map));
        var wpfApp = builder.Build();

        var service = wpfApp.Services.GetRequiredService<IInputSystemService>();

        Assert.False(service.IsEnabled);
        Assert.Empty(platform.Registered);
    }

    [Fact]
    public void GlobalBindingsAreRegisteredWithThePlatformDuringBuild()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("QuickEntry").AddGlobalBinding(Key.F8, ModifierKeys.Control);

        BuildService(platform, options => options.ActionMaps.Add(map));

        var registered = Assert.Single(platform.Registered);
        Assert.Equal((Key.F8, ModifierKeys.Control), registered.Value);
    }

    [Fact]
    public void FindActionLocatesAnActionAcrossMaps()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("QuickEntry").AddGlobalBinding(Key.F8, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));

        var action = service.FindAction("QuickEntry");
        Assert.NotNull(action);
        Assert.Same(map, service.FindActionMap("Test"));
        Assert.Null(service.FindAction("DoesNotExist"));
    }

    [Fact]
    public void PressingTheGlobalHotkeyInvokesPerformed()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("QuickEntry").AddGlobalBinding(Key.F8, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));
        var action = service.FindAction("QuickEntry")!;

        var performedCount = 0;
        action.Performed += (_, _) => performedCount++;

        platform.RaiseHotkeyPressed(0);

        Assert.Equal(1, performedCount);
        Assert.Equal(InputActionPhase.Performed, action.Phase);
    }

    [Fact]
    public void SetEnabledFalseUnregistersEveryGlobalBinding()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("First").AddGlobalBinding(Key.F8, ModifierKeys.Control);
        map.AddAction("Second").AddGlobalBinding(Key.F9, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));

        service.SetEnabled(false);

        Assert.False(service.IsEnabled);
        Assert.Empty(platform.Registered);
        Assert.Equal(new[] { 0, 1 }, platform.Unregistered);
    }

    [Fact]
    public void RemoveActionMapUnregistersItsGlobalBindings()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("QuickEntry").AddGlobalBinding(Key.F8, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));
        Assert.Single(platform.Registered);

        Assert.True(service.RemoveActionMap(map));

        Assert.Empty(platform.Registered);
        Assert.False(service.RemoveActionMap(map));
    }

    [Fact]
    public void ProcessKeyDownAndUpFireAnUnscopedKeyBindingRegardlessOfWindowFocus()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test"); // no Scope - active whenever the app has focus.
        map.AddAction("Save").AddKeyBinding(Key.S, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));
        var action = service.FindAction("Save")!;

        var events = new List<InputActionPhase>();
        action.Started += (_, e) => events.Add(e.Phase);
        action.Performed += (_, e) => events.Add(e.Phase);
        action.Canceled += (_, e) => events.Add(e.Phase);

        service.ProcessKeyDown(Key.S, ModifierKeys.Control);
        service.ProcessKeyUp(Key.S, ModifierKeys.Control);

        Assert.Equal([InputActionPhase.Started, InputActionPhase.Performed, InputActionPhase.Canceled], events);
    }

    [Fact]
    public void ProcessKeyDownIgnoresANonMatchingModifierCombination()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("Save").AddKeyBinding(Key.S, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));
        var action = service.FindAction("Save")!;

        var performed = 0;
        action.Performed += (_, _) => performed++;

        service.ProcessKeyDown(Key.S, ModifierKeys.Control | ModifierKeys.Shift);

        Assert.Equal(0, performed);
    }

    [Fact]
    public void DisabledActionMapDoesNotDispatchInApp()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test") { Enabled = false };
        map.AddAction("Save").AddKeyBinding(Key.S, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));
        var action = service.FindAction("Save")!;

        var performed = 0;
        action.Performed += (_, _) => performed++;

        service.ProcessKeyDown(Key.S, ModifierKeys.Control);

        Assert.Equal(0, performed);
    }

    [Fact]
    public void DualBindingSharesOneActionAcrossInAppAndGlobalSources()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        map.AddAction("QuickEntry")
            .AddKeyBinding(Key.F8, ModifierKeys.Control)
            .AddGlobalBinding(Key.F8, ModifierKeys.Control);

        var service = BuildService(platform, options => options.ActionMaps.Add(map));
        var action = service.FindAction("QuickEntry")!;

        // In-app path (no window is focused in this test, so the map is "reachable" - see
        // IsReachableInApp's own unscoped-map behavior - but IsApplicationFocused() is false
        // with no real Application/Window running, so the global path below is NOT suppressed
        // by this call, matching a real unfocused-app scenario).
        service.ProcessKeyDown(Key.F8, ModifierKeys.Control);
        Assert.Equal(InputActionPhase.Performed, action.Phase);

        service.ProcessKeyUp(Key.F8, ModifierKeys.Control);

        // Global path.
        var performedCount = 0;
        action.Performed += (_, _) => performedCount++;
        platform.RaiseHotkeyPressed(0);

        Assert.Equal(1, performedCount);
    }

    [Fact]
    public void RemoveBindingOnAKeyBindingTakesEffectOnTheVeryNextKeystrokeWithNoRefreshCall()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        var action = map.AddAction("Save");
        var binding = action.AddKeyBinding(Key.S, ModifierKeys.Control).Bindings[0];

        var service = BuildService(platform, options => options.ActionMaps.Add(map));

        var performed = 0;
        action.Performed += (_, _) => performed++;

        service.ProcessKeyDown(Key.S, ModifierKeys.Control);
        Assert.Equal(1, performed);

        // No RefreshBindings() call here - a KeyBinding change is read fresh on the very next
        // keystroke, unlike a GlobalKeyBinding's cached OS registration (see the test below).
        action.RemoveBinding(binding);

        service.ProcessKeyDown(Key.S, ModifierKeys.Control);
        Assert.Equal(1, performed);
    }

    [Fact]
    public void RefreshBindingsReRegistersAChangedGlobalBindingWithThePlatform()
    {
        var platform = new FakeHotkeyPlatform();
        var map = new InputActionMap("Test");
        var action = map.AddAction("QuickEntry");
        var binding = action.AddGlobalBinding(Key.F8, ModifierKeys.Control).Bindings[0];

        var service = BuildService(platform, options => options.ActionMaps.Add(map));

        var registered = Assert.Single(platform.Registered);
        Assert.Equal((Key.F8, ModifierKeys.Control), registered.Value);

        action.RemoveBinding(binding);
        action.AddGlobalBinding(Key.F9, ModifierKeys.Control);

        // Stale until explicitly told to refresh - this is the asymmetry RefreshBindings exists for.
        registered = Assert.Single(platform.Registered);
        Assert.Equal((Key.F8, ModifierKeys.Control), registered.Value);

        service.RefreshBindings();

        registered = Assert.Single(platform.Registered);
        Assert.Equal((Key.F9, ModifierKeys.Control), registered.Value);
    }

    static IInputSystemService BuildService(FakeHotkeyPlatform platform, Action<InputSystemOptions>? configure = null)
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IHotkeyPlatform>(platform);
        builder.ConfigureInputSystem(configure);
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IInputSystemService>();
    }
}
