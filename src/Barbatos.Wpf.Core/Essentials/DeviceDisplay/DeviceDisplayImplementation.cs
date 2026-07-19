// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Barbatos.Wpf.ApplicationModel;
using Microsoft.Win32;

namespace Barbatos.Wpf.Devices;

/// <summary>
/// The Windows implementation of <see cref="IDeviceDisplay"/>, ported from .NET MAUI's
/// Windows <c>DeviceDisplayImplementation</c>. The monitor-info Win32 interop block is
/// carried over almost verbatim (it was already pure Win32, not WinRT); the active window is
/// obtained from WPF's <see cref="Application.Windows"/> instead of MAUI's WinUI
/// <c>WindowStateManager</c>, <see cref="IDeviceDisplay.KeepScreenOn"/> uses
/// <c>SetThreadExecutionState(ES_DISPLAY_REQUIRED)</c> instead of the WinRT
/// <c>DisplayRequest</c>, and display-change notifications come from
/// <see cref="SystemEvents.DisplaySettingsChanged"/> instead of window-message subclassing.
/// </summary>
sealed class DeviceDisplayImplementation : DeviceDisplayImplementationBase
{
    readonly object locker = new();

    bool keepScreenOn;

    protected override bool GetKeepScreenOn()
    {
        lock (locker)
        {
            return keepScreenOn;
        }
    }

    protected override void SetKeepScreenOn(bool value)
    {
        lock (locker)
        {
            if (keepScreenOn == value)
                return;

            keepScreenOn = value;

            var flags = NativeMethods.ES_CONTINUOUS;
            if (value)
                flags |= NativeMethods.ES_DISPLAY_REQUIRED;

            NativeMethods.SetThreadExecutionState(flags);
        }
    }

    protected override DisplayInfo GetMainDisplayInfo()
    {
        var windowHandle = GetActiveWindowHandle();
        if (windowHandle == IntPtr.Zero)
            return default;

        var mi = GetDisplay(windowHandle);
        if (mi == null)
            return default;

        var vDevMode = new DEVMODE();
        NativeMethods.EnumDisplaySettings(mi.Value.DeviceNameToLPTStr(), -1, ref vDevMode);

        var w = vDevMode.dmPelsWidth;
        var h = vDevMode.dmPelsHeight;

        var dpi = (double)NativeMethods.GetDpiForWindow(windowHandle);
        dpi = dpi != 0 ? dpi / DeviceDisplay.BaseLogicalDpi : 1.0;

        var displayOrientation = GetDisplayOrientation(vDevMode);
        var rotation = CalculateRotation(displayOrientation);

        var orientation = displayOrientation is DisplayOrientations.Landscape or DisplayOrientations.LandscapeFlipped
            ? DisplayOrientation.Landscape
            : DisplayOrientation.Portrait;

        return new DisplayInfo(
            width: w,
            height: h,
            density: dpi,
            orientation: orientation,
            rotation: rotation,
            rate: vDevMode.dmDisplayFrequency);
    }

    static IntPtr GetActiveWindowHandle()
    {
        var window = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
            ?? Application.Current?.MainWindow;

        if (window is null)
            return IntPtr.Zero;

        return new WindowInteropHelper(window).Handle;
    }

    static MONITORINFOEX? GetDisplay(IntPtr hwnd)
    {
        NativeMethods.GetWindowRect(hwnd, out var rc);
        var hMonitor = NativeMethods.MonitorFromRect(ref rc, MonitorOptions.MONITOR_DEFAULTTONEAREST);

        var mi = new MONITORINFOEX();
        mi.Size = Marshal.SizeOf(mi);

        return NativeMethods.GetMonitorInfo(hMonitor, ref mi) ? mi : null;
    }

    protected override void StartScreenMetricsListeners() =>
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

    protected override void StopScreenMetricsListeners() =>
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

    void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Utils.InvokeOnMainThread(OnMainDisplayInfoChanged);

    static DisplayRotation CalculateRotation(DisplayOrientations orientation) =>
        orientation switch
        {
            DisplayOrientations.Landscape => DisplayRotation.Rotation0,
            DisplayOrientations.Portrait => DisplayRotation.Rotation270,
            DisplayOrientations.LandscapeFlipped => DisplayRotation.Rotation180,
            DisplayOrientations.PortraitFlipped => DisplayRotation.Rotation90,
            _ => DisplayRotation.Rotation0,
        };

    static DisplayOrientations GetDisplayOrientation(DEVMODE devMode) =>
        devMode.dmDisplayOrientation switch
        {
            0 => DisplayOrientations.Landscape,
            1 => DisplayOrientations.Portrait,
            2 => DisplayOrientations.LandscapeFlipped,
            3 => DisplayOrientations.PortraitFlipped,
            _ => DisplayOrientations.Landscape,
        };

    enum DisplayOrientations
    {
        Landscape,
        Portrait,
        LandscapeFlipped,
        PortraitFlipped,
    }

    enum MonitorOptions : uint
    {
        MONITOR_DEFAULTTONULL,
        MONITOR_DEFAULTTOPRIMARY,
        MONITOR_DEFAULTTONEAREST,
    }

    [StructLayout(LayoutKind.Sequential)]
    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct MONITORINFOEX
    {
        public int Size;
        public RECT Monitor;
        public RECT WorkArea;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string DeviceName;

        public byte[] DeviceNameToLPTStr()
        {
            var lptArray = new byte[DeviceName.Length + 1];

            var index = 0;
            foreach (var c in DeviceName.ToCharArray())
                lptArray[index++] = Convert.ToByte(c);

            lptArray[index] = Convert.ToByte('\0');

            return lptArray;
        }
    }

    struct DEVMODE
    {
        const int CCHDEVICENAME = 0x20;
        const int CCHFORMNAME = 0x20;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    static class NativeMethods
    {
        internal const uint ES_CONTINUOUS = 0x80000000;
        internal const uint ES_DISPLAY_REQUIRED = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint SetThreadExecutionState(uint esFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr MonitorFromRect(ref RECT lprc, MonitorOptions dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumDisplaySettings(
            byte[] lpszDeviceName,
            [param: MarshalAs(UnmanagedType.U4)] int iModeNum,
            [In, Out] ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);
    }
}
