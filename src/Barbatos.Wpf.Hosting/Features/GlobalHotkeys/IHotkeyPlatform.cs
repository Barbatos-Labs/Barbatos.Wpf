// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using WpfDispatcher = System.Windows.Threading.Dispatcher;

namespace Barbatos.Wpf.Hotkeys;

/// <summary>
/// Abstraction over the OS mechanism used to register system-wide hotkeys.
/// </summary>
public interface IHotkeyPlatform
{
    bool Register(int id, HotkeyGesture gesture);

    void Unregister(int id);

    /// <summary>
    /// Occurs when a registered hotkey is pressed; the argument is the hotkey id.
    /// </summary>
    event EventHandler<int>? HotkeyPressed;
}

/// <summary>
/// The default <see cref="IHotkeyPlatform"/> that uses the Win32 <c>RegisterHotKey</c> API.
/// The hotkeys are registered for the UI thread and the <c>WM_HOTKEY</c> thread messages are
/// observed through <see cref="ComponentDispatcher.ThreadPreprocessMessage"/>, which is the
/// WPF-supported way to see thread messages (they are not dispatched to any window procedure).
/// </summary>
internal sealed class Win32HotkeyPlatform : IHotkeyPlatform, IDisposable
{
    const int WmHotkey = 0x0312;

    const uint ModAlt = 0x0001;
    const uint ModControl = 0x0002;
    const uint ModShift = 0x0004;
    const uint ModWin = 0x0008;
    const uint ModNoRepeat = 0x4000;

    WpfDispatcher? _dispatcher;

    public event EventHandler<int>? HotkeyPressed;

    public bool Register(int id, HotkeyGesture gesture) =>
        OnDispatcherThread(() =>
        {
            EnsureHook();
            var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(gesture.Key);
            return NativeMethods.RegisterHotKey(IntPtr.Zero, id, GetNativeModifiers(gesture.Modifiers) | ModNoRepeat, virtualKey);
        });

    public void Unregister(int id) =>
        OnDispatcherThread(() => NativeMethods.UnregisterHotKey(IntPtr.Zero, id));

    void EnsureHook()
    {
        if (_dispatcher is null)
        {
            // The thread that registers the hotkeys receives the WM_HOTKEY messages,
            // so remember its dispatcher and marshal all later operations onto it.
            _dispatcher = WpfDispatcher.CurrentDispatcher;
            ComponentDispatcher.ThreadPreprocessMessage += OnThreadPreprocessMessage;
        }
    }

    T OnDispatcherThread<T>(Func<T> func)
    {
        if (_dispatcher is null || _dispatcher.CheckAccess())
            return func();

        return _dispatcher.Invoke(func);
    }

    void OnThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message == WmHotkey)
            HotkeyPressed?.Invoke(this, msg.wParam.ToInt32());
    }

    static uint GetNativeModifiers(ModifierKeys modifiers)
    {
        uint native = 0;

        if (modifiers.HasFlag(ModifierKeys.Alt))
            native |= ModAlt;
        if (modifiers.HasFlag(ModifierKeys.Control))
            native |= ModControl;
        if (modifiers.HasFlag(ModifierKeys.Shift))
            native |= ModShift;
        if (modifiers.HasFlag(ModifierKeys.Windows))
            native |= ModWin;

        return native;
    }

    public void Dispose()
    {
        if (_dispatcher is null)
            return;

        try
        {
            // ComponentDispatcher is thread-affine, so unhook on the registering thread.
            OnDispatcherThread<object?>(() =>
            {
                ComponentDispatcher.ThreadPreprocessMessage -= OnThreadPreprocessMessage;
                return null;
            });
        }
        catch (Exception)
        {
            // The dispatcher may already have shut down together with the application.
        }

        _dispatcher = null;
    }

    static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
