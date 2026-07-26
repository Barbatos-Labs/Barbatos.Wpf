// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;
using Barbatos.Wpf.Dispatching;
using Barbatos.Wpf.Hosting;
using Barbatos.Wpf.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
// WPF's own System.Windows.Input.KeyBinding collides with Barbatos.Wpf.Input.KeyBinding - this
// alias resolves every unqualified KeyBinding reference in this file to ours.
using KeyBinding = Barbatos.Wpf.Input.KeyBinding;

namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// Wires <see cref="ShortcutRouting.Register"/> the DI-idiomatic way (constructor-injected,
/// invoked automatically via <see cref="IWpfInitializeService"/> - no manual
/// <c>GetRequiredService</c> calls in <c>WpfProgram.cs</c>), and applies each
/// <see cref="KeyBindingOverride"/> live whenever the underlying settings file changes - no app
/// restart required.
/// </summary>
/// <remarks>
/// <see cref="IOptionsMonitor{TOptions}.OnChange(Action{TOptions,string})"/>'s callback fires
/// from whatever thread the file-watcher change token completes on - a thread-pool thread, never
/// the UI thread - while <see cref="InputAction"/>/<see cref="InputActionMap"/>/
/// <see cref="IInputSystemService"/> all assume single-threaded (UI-thread) access. This is why
/// the UI dispatcher is captured exactly once, in <see cref="Initialize"/> (which runs
/// synchronously on the UI thread during <c>WpfAppBuilder.Build()</c>) via
/// <see cref="IDispatcherProvider"/> - the internal helper <c>InputSystemService.cs</c> itself
/// uses for the same purpose is not visible outside <c>Barbatos.Wpf.Core</c> (this repo has no
/// <c>InternalsVisibleTo</c> anywhere) - rather than re-resolved later from the background
/// callback, which would silently vivify a second, wrong, background-thread dispatcher (WPF
/// creates one for whatever thread asks).
/// </remarks>
public sealed class ShortcutRoutingInitializer(
    IInputSystemService inputSystem,
    Router router,
    IOptionsMonitor<Dictionary<string, KeyBindingOverride>> overrides,
    IDispatcherProvider dispatcherProvider,
    ILogger<ShortcutRoutingInitializer> logger) : IWpfInitializeService, IDisposable
{
    IDispatcher? _uiDispatcher;
    IDisposable? _changeSubscription;

    public void Initialize(IServiceProvider services)
    {
        _uiDispatcher = dispatcherProvider.GetForCurrentThread();

        ShortcutRouting.Register(inputSystem, router);

        ApplyOverrides(overrides.CurrentValue);
        _changeSubscription = overrides.OnChange(value => _uiDispatcher?.Dispatch(() => ApplyOverrides(value)));
    }

    void ApplyOverrides(Dictionary<string, KeyBindingOverride> value)
    {
        var appliedAny = false;

        foreach (var (actionName, overrideValue) in value)
        {
            var action = inputSystem.FindAction(actionName);
            if (action is null)
            {
                logger.LogWarning("Barbatos:InputSystem:KeyBindingOverrides references unknown action '{Action}'.", actionName);
                continue;
            }

            try
            {
                foreach (var existing in action.Bindings.OfType<KeyBinding>().ToList())
                    action.RemoveBinding(existing);

                action.AddKeyBinding(overrideValue.Key, overrideValue.Modifiers);
                appliedAny = true;
            }
            catch (Exception ex)
            {
                // Caught per-entry: one bad override must not stop the rest of the dictionary
                // from applying, and nothing may escape the dispatched delegate uncaught - a
                // synchronous OnChange callback has no Task for a caller to observe a fault on.
                logger.LogWarning(ex, "Failed to apply the key binding override for action '{Action}'.", actionName);
            }
        }

        if (appliedAny)
            inputSystem.RefreshBindings();
    }

    public void Dispose() => _changeSubscription?.Dispose();
}

public static class ShortcutRoutingInitializerExtensions
{
    /// <summary>
    /// Registers the pieces <see cref="ShortcutRoutingInitializer"/> needs - call after
    /// <c>ConfigureInputSystem</c> (its <see cref="IInputSystemService.FindAction"/> lookups only
    /// find anything once that feature's own initializer has already applied the declared action
    /// maps).
    /// </summary>
    public static WpfAppBuilder ConfigureShortcutRouting(this WpfAppBuilder builder)
    {
        builder.Services.AddSingleton<ShortcutSettingsStore>();
        builder.Services.AddOptions<Dictionary<string, KeyBindingOverride>>()
            .Bind(builder.Configuration.GetSection(ShortcutSettingsStore.SectionName));
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IWpfInitializeService, ShortcutRoutingInitializer>());
        return builder;
    }
}
