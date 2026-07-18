// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Hotkeys;

/// <summary>
/// The default <see cref="IGlobalHotkeyService"/> implementation.
/// </summary>
internal sealed class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
{
    readonly GlobalHotkeyOptions _options;
    readonly IHotkeyPlatform _platform;
    readonly ILogger<GlobalHotkeyService> _logger;
    readonly List<GlobalHotkey> _hotkeys = new();

    public GlobalHotkeyService(
        IOptions<GlobalHotkeyOptions> options,
        IEnumerable<GlobalHotkeyRegistration> registrations,
        IHotkeyPlatform platform,
        ILogger<GlobalHotkeyService> logger)
    {
        _options = options.Value;
        _platform = platform;
        _logger = logger;
        _platform.HotkeyPressed += OnPlatformHotkeyPressed;

        foreach (var registration in registrations)
        {
            // A gesture from configuration overrides the default gesture from code.
            var gestureText = _options.Gestures.TryGetValue(registration.Name, out var configured)
                ? configured
                : registration.DefaultGesture;

            _hotkeys.Add(new GlobalHotkey(registration.Name, HotkeyGesture.Parse(gestureText), registration.Callback));
        }
    }

    public event EventHandler? IsEnabledChanged;

    public event EventHandler<GlobalHotkeyPressedEventArgs>? HotkeyPressed;

    public bool IsEnabled { get; private set; }

    public IReadOnlyList<GlobalHotkey> Hotkeys => _hotkeys;

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
            return;

        for (var id = 0; id < _hotkeys.Count; id++)
        {
            if (enabled)
                RegisterWithPlatform(id);
            else
                _platform.Unregister(id);
        }

        IsEnabled = enabled;
        IsEnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateGesture(string name, HotkeyGesture gesture)
    {
        _ = gesture ?? throw new ArgumentNullException(nameof(gesture));

        var id = _hotkeys.FindIndex(hotkey => string.Equals(hotkey.Name, name, StringComparison.OrdinalIgnoreCase));
        if (id < 0)
            throw new ArgumentException($"No hotkey named '{name}' has been registered.", nameof(name));

        _hotkeys[id].Gesture = gesture;

        if (IsEnabled)
        {
            _platform.Unregister(id);
            RegisterWithPlatform(id);
        }
    }

    void RegisterWithPlatform(int id)
    {
        var hotkey = _hotkeys[id];

        if (!_platform.Register(id, hotkey.Gesture))
            _logger.LogWarning(
                "Failed to register the global hotkey '{Name}' ({Gesture}). Another application may already be using this key combination.",
                hotkey.Name, hotkey.Gesture);
    }

    void OnPlatformHotkeyPressed(object? sender, int id)
    {
        if (id < 0 || id >= _hotkeys.Count)
            return;

        var hotkey = _hotkeys[id];

        hotkey.Callback?.Invoke();
        HotkeyPressed?.Invoke(this, new GlobalHotkeyPressedEventArgs(hotkey));
    }

    /// <summary>
    /// Applies the configured options during application construction.
    /// </summary>
    internal void ApplyOptions()
    {
        if (_options.Enabled)
            SetEnabled(true);
    }

    public void Dispose()
    {
        if (IsEnabled)
            SetEnabled(false);

        _platform.HotkeyPressed -= OnPlatformHotkeyPressed;
    }
}
