// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Barbatos.Wpf.Tray;

/// <summary>
/// Abstraction over the OS mechanism used to display a system tray icon.
/// </summary>
public interface ITrayIconPlatform
{
    void Show(TrayIconOptions options);

    void Hide();

    void SetToolTip(string toolTip);

    event EventHandler? Clicked;

    event EventHandler? DoubleClicked;
}

/// <summary>
/// The default <see cref="ITrayIconPlatform"/>, backed directly by the Win32
/// <c>Shell_NotifyIcon</c> API through a hidden <see cref="HwndSource"/> message window. Unlike
/// the previous <c>System.Windows.Forms.NotifyIcon</c>-based implementation, this does not
/// require <c>UseWindowsForms</c> / a reference to <c>System.Windows.Forms</c>.
/// </summary>
internal sealed class Win32TrayIconPlatform : ITrayIconPlatform, IDisposable
{
    const int IconId = 1;

    // An application-defined message (WM_APP range) the shell posts back to our window on
    // mouse/keyboard interaction with the icon. Only meaningful within this window.
    const int CallbackMessageId = 0x8000 + 1;

    const int WM_LBUTTONUP = 0x0202;
    const int WM_LBUTTONDBLCLK = 0x0203;
    const int WM_RBUTTONUP = 0x0205;
    const int WM_CONTEXTMENU = 0x007B;
    const int WM_NULL = 0x0000;

    // NOTIFYICONDATA.szTip holds 128 WCHARs including the null terminator.
    const int MaxToolTipLength = 127;

    readonly int _taskbarRestartMessage;

    HwndSource? _hwndSource;
    IntPtr _icon;
    bool _iconIsOwned;
    string _toolTip = string.Empty;
    IReadOnlyList<TrayMenuItem> _menuItems = Array.Empty<TrayMenuItem>();

    // Parallel to _menuItems: the HBITMAP rendered from the matching item's IconPath, or
    // IntPtr.Zero when that item has none (or it failed to load). Built once, alongside
    // _menuItems, and torn down in Dispose().
    IntPtr[] _menuItemIcons = Array.Empty<IntPtr>();

    bool _added;

    public Win32TrayIconPlatform() =>
        _taskbarRestartMessage = NativeMethods.RegisterWindowMessage("TaskbarCreated");

    public event EventHandler? Clicked;

    public event EventHandler? DoubleClicked;

    public void Show(TrayIconOptions options)
    {
        _ = options ?? throw new ArgumentNullException(nameof(options));

        EnsureWindow();

        // Mirrors the previous implementation: the icon and menu are only ever read from
        // options once, the first time the icon is shown. Later Show() calls (after a Hide())
        // just re-add the same icon; use SetToolTip to change the tooltip afterwards.
        if (_icon == IntPtr.Zero)
        {
            (_icon, _iconIsOwned) = LoadIcon(options);
            _toolTip = Truncate(options.ToolTip ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty);
            _menuItems = options.MenuItems.ToArray();
            _menuItemIcons = LoadMenuItemIcons(_menuItems);
        }

        UpdateShellIcon(add: !_added);
        _added = true;
    }

    public void Hide()
    {
        if (!_added || _hwndSource is null)
            return;

        var data = CreateData();
        NativeMethods.Shell_NotifyIcon(NativeMethods.NIM_DELETE, ref data);
        _added = false;
    }

    public void SetToolTip(string toolTip)
    {
        _toolTip = Truncate(toolTip);

        if (_added)
            UpdateShellIcon(add: false);
    }

    void EnsureWindow()
    {
        if (_hwndSource is not null)
            return;

        var parameters = new HwndSourceParameters("Barbatos.Wpf.TrayIcon")
        {
            WindowStyle = 0,
            ExtendedWindowStyle = 0,
            ParentWindow = new IntPtr(-3), // HWND_MESSAGE: a message-only window, never visible.
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    void UpdateShellIcon(bool add)
    {
        var data = CreateData();
        data.uFlags = NativeMethods.NIF_MESSAGE | NativeMethods.NIF_ICON | NativeMethods.NIF_TIP;
        data.uCallbackMessage = CallbackMessageId;
        data.hIcon = _icon;
        data.szTip = _toolTip;

        NativeMethods.Shell_NotifyIcon(add ? NativeMethods.NIM_ADD : NativeMethods.NIM_MODIFY, ref data);
    }

    NativeMethods.NOTIFYICONDATA CreateData() => new()
    {
        cbSize = Marshal.SizeOf<NativeMethods.NOTIFYICONDATA>(),
        hWnd = _hwndSource!.Handle,
        uID = IconId,
    };

    IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == CallbackMessageId)
        {
            switch (unchecked((int)lParam.ToInt64()) & 0xFFFF)
            {
                case WM_LBUTTONUP:
                    Clicked?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;
                case WM_LBUTTONDBLCLK:
                    DoubleClicked?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;
                case WM_RBUTTONUP:
                case WM_CONTEXTMENU:
                    ShowContextMenu();
                    handled = true;
                    break;
            }
        }
        else if (msg == _taskbarRestartMessage && _added)
        {
            // explorer.exe (and with it, every notification-area icon) restarted - re-add ours.
            UpdateShellIcon(add: true);
        }

        return IntPtr.Zero;
    }

    void ShowContextMenu()
    {
        if (_menuItems.Count == 0 || _hwndSource is null)
            return;

        var hwnd = _hwndSource.Handle;
        var hMenu = NativeMethods.CreatePopupMenu();
        if (hMenu == IntPtr.Zero)
            return;

        try
        {
            for (var i = 0; i < _menuItems.Count; i++)
            {
                var item = _menuItems[i];
                var id = (UIntPtr)(i + 1);

                if (item.IsSeparator)
                {
                    NativeMethods.AppendMenu(hMenu, NativeMethods.MF_SEPARATOR, id, string.Empty);
                    continue;
                }

                var flags = NativeMethods.MF_STRING;
                if (!item.IsEnabled)
                    flags |= NativeMethods.MF_GRAYED | NativeMethods.MF_DISABLED;

                NativeMethods.AppendMenu(hMenu, flags, id, item.Header);

                if (item.IsDefault)
                    NativeMethods.SetMenuDefaultItem(hMenu, (uint)(i + 1), false);

                if (_menuItemIcons.Length > i && _menuItemIcons[i] != IntPtr.Zero)
                {
                    var info = new NativeMethods.MENUITEMINFO
                    {
                        cbSize = (uint)Marshal.SizeOf<NativeMethods.MENUITEMINFO>(),
                        fMask = NativeMethods.MIIM_BITMAP,
                        hbmpItem = _menuItemIcons[i],
                    };
                    NativeMethods.SetMenuItemInfo(hMenu, (uint)(i + 1), false, ref info);
                }
            }

            NativeMethods.GetCursorPos(out var cursor);

            // The window must be the foreground window or the menu will not dismiss itself when
            // the user clicks elsewhere; the trailing WM_NULL works around a documented Windows
            // bug where the menu can otherwise stay stuck open. Both are called out in the
            // Shell_NotifyIcon remarks on learn.microsoft.com.
            NativeMethods.SetForegroundWindow(hwnd);
            var command = NativeMethods.TrackPopupMenuEx(
                hMenu,
                NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_NONOTIFY,
                cursor.X,
                cursor.Y,
                hwnd,
                IntPtr.Zero);
            NativeMethods.PostMessage(hwnd, WM_NULL, IntPtr.Zero, IntPtr.Zero);

            if (command > 0 && command <= _menuItems.Count)
            {
                var clicked = _menuItems[command - 1];
                if (!clicked.IsSeparator && clicked.IsEnabled)
                    clicked.Action();
            }
        }
        finally
        {
            NativeMethods.DestroyMenu(hMenu);
        }
    }

    static (IntPtr Icon, bool Owned) LoadIcon(TrayIconOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.IconPath))
        {
            var cx = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSMICON);
            var cy = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSMICON);
            var handle = NativeMethods.LoadImage(IntPtr.Zero, options.IconPath, NativeMethods.IMAGE_ICON, cx, cy, NativeMethods.LR_LOADFROMFILE);
            if (handle != IntPtr.Zero)
                return (handle, true);
        }

        if (Environment.ProcessPath is string processPath)
        {
            var smallIcons = new IntPtr[1];
            var extracted = NativeMethods.ExtractIconEx(processPath, 0, null, smallIcons, 1);
            if (extracted > 0 && smallIcons[0] != IntPtr.Zero)
                return (smallIcons[0], true);
        }

        // IDI_APPLICATION: the default system application icon, a shared OS resource that must
        // not be destroyed.
        return (NativeMethods.LoadIcon(IntPtr.Zero, new IntPtr(32512)), false);
    }

    // NOTIFYICONDATA.szTip is limited to 127 characters plus a null terminator.
    static string Truncate(string text) =>
        text.Length <= MaxToolTipLength ? text : text[..MaxToolTipLength];

    static IntPtr[] LoadMenuItemIcons(IReadOnlyList<TrayMenuItem> items)
    {
        var icons = new IntPtr[items.Count];
        var size = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSMICON);

        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].IsSeparator || string.IsNullOrWhiteSpace(items[i].IconPath))
                continue;

            var hIcon = NativeMethods.LoadImage(IntPtr.Zero, items[i].IconPath!, NativeMethods.IMAGE_ICON, size, size, NativeMethods.LR_LOADFROMFILE);
            if (hIcon == IntPtr.Zero)
                continue;

            icons[i] = IconToBitmap(hIcon, size);
            NativeMethods.DestroyIcon(hIcon);
        }

        return icons;
    }

    // Renders hIcon into a 32bpp top-down DIB section, matching the icon's own alpha channel,
    // so it can be used as a native menu item's HBITMAP (which - unlike Shell_NotifyIcon's
    // hIcon - takes a bitmap, not an icon handle).
    static IntPtr IconToBitmap(IntPtr hIcon, int size)
    {
        var header = new NativeMethods.BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<NativeMethods.BITMAPINFOHEADER>(),
            biWidth = size,
            biHeight = -size, // negative = top-down, matching DrawIconEx's own orientation.
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0, // BI_RGB
        };

        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        var memDc = NativeMethods.CreateCompatibleDC(screenDc);
        NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);

        var bitmap = NativeMethods.CreateDIBSection(memDc, ref header, 0, out _, IntPtr.Zero, 0);
        if (bitmap == IntPtr.Zero)
        {
            NativeMethods.DeleteDC(memDc);
            return IntPtr.Zero;
        }

        var previous = NativeMethods.SelectObject(memDc, bitmap);
        NativeMethods.DrawIconEx(memDc, 0, 0, hIcon, size, size, 0, IntPtr.Zero, NativeMethods.DI_NORMAL);
        NativeMethods.SelectObject(memDc, previous);
        NativeMethods.DeleteDC(memDc);

        return bitmap;
    }

    public void Dispose()
    {
        Hide();

        if (_icon != IntPtr.Zero)
        {
            if (_iconIsOwned)
                NativeMethods.DestroyIcon(_icon);
            _icon = IntPtr.Zero;
        }

        foreach (var bitmap in _menuItemIcons)
        {
            if (bitmap != IntPtr.Zero)
                NativeMethods.DeleteObject(bitmap);
        }
        _menuItemIcons = Array.Empty<IntPtr>();

        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }

    static class NativeMethods
    {
        internal const int NIM_ADD = 0x00000000;
        internal const int NIM_MODIFY = 0x00000001;
        internal const int NIM_DELETE = 0x00000002;

        internal const int NIF_MESSAGE = 0x00000001;
        internal const int NIF_ICON = 0x00000002;
        internal const int NIF_TIP = 0x00000004;

        internal const int MF_STRING = 0x00000000;
        internal const int MF_GRAYED = 0x00000001;
        internal const int MF_DISABLED = 0x00000002;
        internal const int MF_SEPARATOR = 0x00000800;

        internal const uint MIIM_BITMAP = 0x00000080;

        internal const uint TPM_RETURNCMD = 0x0100;
        internal const uint TPM_NONOTIFY = 0x0080;

        internal const int SM_CXSMICON = 49;
        internal const int SM_CYSMICON = 50;

        internal const uint IMAGE_ICON = 1;
        internal const uint LR_LOADFROMFILE = 0x0010;

        internal const uint DI_NORMAL = 0x0003;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct NOTIFYICONDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uID;
            public int uFlags;
            public int uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public int dwState;
            public int dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public int uVersionOrTimeout;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public int dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int X;
            public int Y;
        }

        // Only cbSize/fMask/hbmpItem are actually populated (see IconToBitmap's caller); every
        // other field must still be declared in the right order for the struct to line up with
        // the native layout.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MENUITEMINFO
        {
            public uint cbSize;
            public uint fMask;
            public uint fType;
            public uint fState;
            public uint wID;
            public IntPtr hSubMenu;
            public IntPtr hbmpChecked;
            public IntPtr hbmpUnchecked;
            public IntPtr dwItemData;
            public IntPtr dwTypeData;
            public uint cch;
            public IntPtr hbmpItem;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadImage(IntPtr hinst, string name, uint type, int cx, int cy, uint fuLoad);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        internal static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool AppendMenu(IntPtr hMenu, int uFlags, UIntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetMenuDefaultItem(IntPtr hMenu, uint uItem, [MarshalAs(UnmanagedType.Bool)] bool fByPos);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetMenuItemInfo(IntPtr hMenu, uint uItem, [MarshalAs(UnmanagedType.Bool)] bool fByPosition, ref MENUITEMINFO lpmii);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyWidth, uint istepIfAniCur, IntPtr hbrFlickerFreeDraw, uint diFlags);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        internal static extern IntPtr SelectObject(IntPtr hdc, IntPtr hObject);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        internal static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFOHEADER pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);
    }
}
