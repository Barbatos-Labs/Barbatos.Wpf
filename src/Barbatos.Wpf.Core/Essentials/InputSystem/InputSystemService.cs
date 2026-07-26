// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Input;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Input;

/// <summary>
/// The default <see cref="IInputSystemService"/>. Owns two dispatch paths that both ultimately
/// raise the same <see cref="InputAction"/> events: real WPF key events (via a class handler
/// registered against <see cref="Window"/>, safe to set up before any window exists - see
/// <see cref="Hook"/>) for <see cref="KeyBinding"/>s, and <see cref="IHotkeyPlatform"/>'s
/// <c>WM_HOTKEY</c> notifications for <see cref="GlobalKeyBinding"/>s.
/// </summary>
/// <remarks>
/// <b>The two paths never actually race for the same physical keystroke</b> - confirmed
/// empirically, not assumed (an earlier draft of this class had a "defer to the in-app sibling
/// while focused" guard in <see cref="OnPlatformHotkeyPressed"/> that turned out to make a
/// dually-bound action fire from *neither* path while focused; removed once the real mechanism
/// was understood). Once <c>RegisterHotKey</c> successfully claims an exact key+modifier
/// combination, Windows routes every press of it into <c>WM_HOTKEY</c> exclusively, system-wide,
/// for as long as the registration holds - <i>including</i> while the registering app's own
/// window has focus; the ordinary <c>WM_KEYDOWN</c>/<c>WM_KEYUP</c> sequence (and therefore
/// <see cref="OnPreviewKeyDown"/>/<see cref="ProcessKeyDown"/>) simply never fires for that exact
/// combination at all while the global registration is active. This is standard, well-known
/// `RegisterHotKey` behavior (it is *why* global hotkey tools need no separate "my own window is
/// focused" special case at all), not something this library adds on top. A <see cref="KeyBinding"/>
/// sharing the identical <see cref="SingleKeyBinding.Key"/>/<see cref="SingleKeyBinding.Modifiers"/> as a
/// registered <see cref="GlobalKeyBinding"/> on the same or a different action is therefore only
/// ever reachable via a real keystroke if the global registration itself failed (see the
/// warning logged from <see cref="RegisterGlobalBindingsWithPlatform"/>) - or, of course, via a
/// direct <see cref="ProcessKeyDown"/> call, which never goes through the OS at all.
/// </remarks>
internal sealed class InputSystemService : IInputSystemService, IDisposable
{
    readonly InputSystemOptions _options;
    readonly IHotkeyPlatform _platform;
    readonly IServiceProvider _services;
    readonly ILogger<InputSystemService> _logger;

    readonly List<InputActionMap> _actionMaps = new();
    readonly List<(InputActionMap Map, InputAction Action, GlobalKeyBinding Binding)> _globalBindings = new();
    readonly Dictionary<InputBinding, BindingState> _bindingStates = new();

    bool _wpfEventsHooked;
    bool _disposed;

    public InputSystemService(IOptions<InputSystemOptions> options, IHotkeyPlatform platform, IServiceProvider services, ILogger<InputSystemService> logger)
    {
        _options = options.Value;
        _platform = platform;
        _services = services;
        _logger = logger;

        _platform.HotkeyPressed += OnPlatformHotkeyPressed;
    }

    public event EventHandler? IsEnabledChanged;

    public bool IsEnabled { get; private set; }

    public IReadOnlyList<InputActionMap> ActionMaps => _actionMaps;

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
            return;

        IsEnabled = enabled;

        if (enabled)
            RegisterGlobalBindingsWithPlatform();
        else
            UnregisterGlobalBindingsFromPlatform();

        if (!enabled)
        {
            // A disable is the one clean, service-level moment to reset every in-progress
            // interaction/chord attempt - there is no per-map "just got disabled" signal to hook
            // instead, since InputActionMap.Enabled is a plain property, not an observable service.
            var bindings = _actionMaps.SelectMany(m => m.Actions).SelectMany(a => a.Bindings);

            foreach (var binding in bindings.OfType<SingleKeyBinding>().Where(b => b.Interaction is not null))
                binding.Interaction!.Reset();

            foreach (var chord in bindings.OfType<ChordBinding>())
                if (_bindingStates.TryGetValue(chord, out var state))
                    state.ChordProgress = 0;
        }

        IsEnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    public InputActionMap AddActionMap(InputActionMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        _actionMaps.Add(map);
        RebuildGlobalBindings();
        LogDuplicateKeyBindings();

        return map;
    }

    public bool RemoveActionMap(InputActionMap map)
    {
        if (!_actionMaps.Remove(map))
            return false;

        RebuildGlobalBindings();
        LogDuplicateKeyBindings();
        PurgeStaleBindingStates();
        return true;
    }

    /// <summary>
    /// Reused for both the public <see cref="RefreshBindings"/> and the two existing
    /// map-add/remove paths above - all three already do a full unregister-all/rescan-all/
    /// register-all every time they run, so there is no separate partial-rebuild concept to keep
    /// in sync.
    /// </summary>
    public void RefreshBindings()
    {
        RebuildGlobalBindings();
        LogDuplicateKeyBindings();
        PurgeStaleBindingStates();
    }

    void PurgeStaleBindingStates()
    {
        if (_bindingStates.Count == 0)
            return;

        var reachable = new HashSet<InputBinding>(_actionMaps.SelectMany(m => m.Actions).SelectMany(a => a.Bindings));

        foreach (var stale in _bindingStates.Keys.Where(b => !reachable.Contains(b)).ToList())
            _bindingStates.Remove(stale);
    }

    public InputAction? FindAction(string name) =>
        _actionMaps.SelectMany(m => m.Actions).FirstOrDefault(a => a.Name == name);

    public InputActionMap? FindActionMap(string name) =>
        _actionMaps.FirstOrDefault(m => m.Name == name);

    public void ProcessKeyDown(Key key, ModifierKeys modifiers = ModifierKeys.None) => Process(key, modifiers, isPressed: true);

    public void ProcessKeyUp(Key key, ModifierKeys modifiers = ModifierKeys.None) => Process(key, modifiers, isPressed: false);

    /// <summary>Applies <see cref="InputSystemOptions"/> - called once, during application construction. See <see cref="AppHostBuilderExtensions.ConfigureInputSystem"/>.</summary>
    internal void ApplyOptions()
    {
        foreach (var map in _options.ActionMaps)
            AddActionMap(map);

        Hook();

        if (_options.Enabled)
            SetEnabled(true);
    }

    void Process(Key key, ModifierKeys modifiers, bool isPressed)
    {
        if (!IsEnabled)
            return;

        foreach (var map in _actionMaps)
        {
            if (!map.Enabled || !IsReachableInApp(map))
                continue;

            foreach (var action in map.Actions)
            {
                foreach (var binding in action.Bindings.OfType<KeyBinding>())
                    if (binding.Matches(key, modifiers))
                        Dispatch(action, binding, isPressed);

                // Chords only ever advance on a press - a release carries no ordering
                // information a sequence needs - and never on a bare modifier press, which
                // genuinely arrives as its own key event before the real step's key does (see
                // ChordBinding's own remarks).
                if (isPressed && !IsBareModifierKey(key))
                    foreach (var chord in action.Bindings.OfType<ChordBinding>())
                        DispatchChordStep(action, chord, key, modifiers);
            }
        }
    }

    static bool IsReachableInApp(InputActionMap map) => map.Scope is null || map.Scope.IsKeyboardFocusWithin;

    static bool IsBareModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftShift or Key.RightShift or
        Key.LeftAlt or Key.RightAlt or
        Key.LWin or Key.RWin;

    void Dispatch(InputAction action, KeyBinding binding, bool isPressed)
    {
        var state = GetOrCreateState(binding);
        var interaction = binding.Interaction ?? DefaultInteraction.Instance;

        var context = new InputInteractionContext(isPressed);
        interaction.Process(ref context);

        ApplyResult(action, binding, state, interaction, context);
    }

    void ApplyResult(InputAction action, InputBinding binding, BindingState state, IInputInteraction interaction, InputInteractionContext context)
    {
        // Independent flags, not a single overwritable result - Started and Performed commonly
        // fire together in one Process() call (see DefaultInteraction), and both must reach
        // InputAction as their own real event, in that order, exactly like Unity's own Button
        // action type genuinely invokes both callbacks rather than only the "final" one.
        if (context.WasStarted)
        {
            state.StartedAt = DateTime.UtcNow;
            action.RaiseStarted(binding, TimeSpan.Zero);
        }

        if (context.WasPerformed)
            action.RaisePerformed(binding, DateTime.UtcNow - state.StartedAt);

        if (context.WasCanceled)
            action.RaiseCanceled(binding, DateTime.UtcNow - state.StartedAt);

        if (context.RequestedTimeout is { } timeout)
            ScheduleTimeout(action, binding, state, interaction, timeout);
    }

    void ScheduleTimeout(InputAction action, InputBinding binding, BindingState state, IInputInteraction interaction, TimeSpan timeout)
    {
        var version = ++state.PendingTimeoutVersion;

        var dispatcher = _services.GetOptionalApplicationDispatcher();
        if (dispatcher is null)
            return; // No Application yet - not practically reachable from a real keystroke.

        dispatcher.DispatchDelayed(timeout, () =>
        {
            if (!IsEnabled || state.PendingTimeoutVersion != version)
                return; // Disabled, or superseded by a newer key event on this same binding - stale, ignore.

            var context = new InputInteractionContext(isPressed: false, isTimeout: true);
            interaction.Process(ref context);
            ApplyResult(action, binding, state, interaction, context);
        });
    }

    void DispatchChordStep(InputAction action, ChordBinding chord, Key key, ModifierKeys modifiers)
    {
        var state = GetOrCreateState(chord);

        if (chord.MatchesStep(state.ChordProgress, key, modifiers))
        {
            var isFirstStep = state.ChordProgress == 0;
            state.ChordProgress++;

            if (isFirstStep)
            {
                state.StartedAt = DateTime.UtcNow;
                action.RaiseStarted(chord, TimeSpan.Zero);
            }

            if (state.ChordProgress == chord.Steps.Count)
            {
                state.ChordProgress = 0;
                action.RaisePerformed(chord, DateTime.UtcNow - state.StartedAt);
            }
            else
            {
                // Re-arms on every non-final advance, not just the first step - a 3+-step
                // chord's total time budget is one StepTimeout per gap, not one StepTimeout
                // total from the very first key.
                ScheduleChordStepTimeout(action, chord, state);
            }
        }
        else if (state.ChordProgress > 0)
        {
            state.ChordProgress = 0;
            action.RaiseCanceled(chord, DateTime.UtcNow - state.StartedAt);
        }
    }

    void ScheduleChordStepTimeout(InputAction action, ChordBinding chord, BindingState state)
    {
        var version = ++state.PendingTimeoutVersion;

        var dispatcher = _services.GetOptionalApplicationDispatcher();
        if (dispatcher is null)
            return; // No Application yet - not practically reachable from a real keystroke.

        dispatcher.DispatchDelayed(chord.StepTimeout, () =>
        {
            // Besides the usual disabled/superseded-by-a-newer-step checks, ChordProgress == 0
            // also catches "the chord already completed on its final step" - completing doesn't
            // reschedule (there is no next step to time out), so this exact version is still
            // "current" by the version check alone; without this it would wrongly cancel an
            // already-successful attempt once this timeout eventually fires.
            if (!IsEnabled || state.PendingTimeoutVersion != version || state.ChordProgress == 0)
                return;

            state.ChordProgress = 0;
            action.RaiseCanceled(chord, DateTime.UtcNow - state.StartedAt);
        });
    }

    BindingState GetOrCreateState(InputBinding binding)
    {
        if (!_bindingStates.TryGetValue(binding, out var state))
            _bindingStates[binding] = state = new BindingState();
        return state;
    }

    void OnPlatformHotkeyPressed(object? sender, int id)
    {
        if (!IsEnabled || id < 0 || id >= _globalBindings.Count)
            return;

        var (map, action, binding) = _globalBindings[id];
        if (!map.Enabled)
            return;

        // No focus check needed here - see this type's own remarks for why a WM_HOTKEY
        // notification and a live KeyBinding for the same exact chord structurally cannot both
        // fire for one physical keystroke in the first place.
        var state = GetOrCreateState(binding);
        state.StartedAt = DateTime.UtcNow;
        action.RaiseStarted(binding, TimeSpan.Zero);
        action.RaisePerformed(binding, TimeSpan.Zero);
    }

    void RebuildGlobalBindings()
    {
        if (IsEnabled)
            UnregisterGlobalBindingsFromPlatform();

        _globalBindings.Clear();
        foreach (var map in _actionMaps)
            foreach (var action in map.Actions)
                foreach (var binding in action.Bindings.OfType<GlobalKeyBinding>())
                    _globalBindings.Add((map, action, binding));

        if (IsEnabled)
            RegisterGlobalBindingsWithPlatform();
    }

    void RegisterGlobalBindingsWithPlatform()
    {
        for (var id = 0; id < _globalBindings.Count; id++)
        {
            var (_, action, binding) = _globalBindings[id];
            if (!_platform.Register(id, binding.Key, binding.Modifiers))
            {
                _logger.LogWarning(
                    "Failed to register the global hotkey for action '{Action}' ({Key}+{Modifiers}) - another application, or another action in this app, may already be using it.",
                    action.Name, binding.Key, binding.Modifiers);
            }
        }
    }

    void UnregisterGlobalBindingsFromPlatform()
    {
        for (var id = 0; id < _globalBindings.Count; id++)
            _platform.Unregister(id);
    }

    void LogDuplicateKeyBindings()
    {
        var duplicates = _actionMaps
            .Where(map => map.Enabled)
            .SelectMany(map => map.Actions.Select(action => (Map: map, Action: action)))
            .SelectMany(entry => entry.Action.Bindings.OfType<KeyBinding>().Select(binding => (entry.Map, entry.Action, Binding: binding)))
            .GroupBy(entry => (entry.Map.Scope, entry.Binding.Key, entry.Binding.Modifiers))
            .Where(group => group.Count() > 1);

        foreach (var group in duplicates)
        {
            var actionNames = string.Join(", ", group.Select(entry => entry.Action.Name));
            _logger.LogWarning(
                "Multiple enabled actions share the exact same key binding ({Key}+{Modifiers}): {Actions}. All of them will fire.",
                group.Key.Key, group.Key.Modifiers, actionNames);
        }
    }

    void Hook()
    {
        if (_wpfEventsHooked)
            return;
        _wpfEventsHooked = true;

        // Registered against the TYPE, not an instance - safe to call here, before any Window
        // exists (ApplyOptions runs during WpfAppBuilder.Build(), pre-Window - see
        // IWpfInitializeService's own remarks). PreviewKeyDown/Up tunnel from the Window down to
        // the focused element, so this fires exactly once per keystroke regardless of which
        // element inside the window actually has focus.
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
        EventManager.RegisterClassHandler(typeof(Window), UIElement.PreviewKeyUpEvent, new KeyEventHandler(OnPreviewKeyUp), true);
    }

    void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_disposed || e.IsRepeat)
            return;
        ProcessKeyDown(UnwrapSystemKey(e), Keyboard.Modifiers);
    }

    void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (_disposed)
            return;
        ProcessKeyUp(UnwrapSystemKey(e), Keyboard.Modifiers);
    }

    // Alt-chords arrive with e.Key == Key.System and the real key in e.SystemKey.
    static Key UnwrapSystemKey(KeyEventArgs e) => e.Key == Key.System ? e.SystemKey : e.Key;

    public void Dispose()
    {
        _disposed = true;
        _platform.HotkeyPressed -= OnPlatformHotkeyPressed;

        if (IsEnabled)
            SetEnabled(false);

        // EventManager.RegisterClassHandler has no unregister API - OnPreviewKeyDown/Up's own
        // _disposed check is what makes the still-registered delegate a harmless no-op for the
        // remainder of the process lifetime, the same pattern WpfApplication.cs's own
        // un-unregisterable class handler already relies on.
    }

    sealed class BindingState
    {
        public DateTime StartedAt;
        public int PendingTimeoutVersion;
        public int ChordProgress;
    }
}
