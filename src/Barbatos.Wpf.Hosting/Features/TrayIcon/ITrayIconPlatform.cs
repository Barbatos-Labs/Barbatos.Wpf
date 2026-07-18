// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Drawing;
using System.Windows.Forms;

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
/// The default <see cref="ITrayIconPlatform"/> that uses
/// <see cref="System.Windows.Forms.NotifyIcon"/>.
/// </summary>
internal sealed class WinFormsTrayIconPlatform : ITrayIconPlatform, IDisposable
{
    NotifyIcon? _notifyIcon;

    public event EventHandler? Clicked;

    public event EventHandler? DoubleClicked;

    public void Show(TrayIconOptions options)
    {
        _notifyIcon ??= CreateNotifyIcon(options);
        _notifyIcon.Visible = true;
    }

    public void Hide()
    {
        if (_notifyIcon is not null)
            _notifyIcon.Visible = false;
    }

    public void SetToolTip(string toolTip)
    {
        if (_notifyIcon is not null)
            _notifyIcon.Text = Truncate(toolTip);
    }

    NotifyIcon CreateNotifyIcon(TrayIconOptions options)
    {
        var notifyIcon = new NotifyIcon
        {
            Icon = LoadIcon(options),
            Text = Truncate(options.ToolTip ?? System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty),
        };

        if (options.MenuItems.Count > 0)
        {
            var menu = new ContextMenuStrip();
            foreach (var item in options.MenuItems)
            {
                var action = item.Action;
                menu.Items.Add(item.Header, image: null, (sender, args) => action());
            }

            notifyIcon.ContextMenuStrip = menu;
        }

        notifyIcon.MouseClick += (sender, args) =>
        {
            if (args.Button == MouseButtons.Left)
                Clicked?.Invoke(this, EventArgs.Empty);
        };
        notifyIcon.DoubleClick += (sender, args) => DoubleClicked?.Invoke(this, EventArgs.Empty);

        return notifyIcon;
    }

    static Icon LoadIcon(TrayIconOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.IconPath))
            return new Icon(options.IconPath);

        if (Environment.ProcessPath is string processPath &&
            Icon.ExtractAssociatedIcon(processPath) is Icon associated)
            return associated;

        return SystemIcons.Application;
    }

    // NotifyIcon.Text is limited to 127 characters (63 on older frameworks).
    static string Truncate(string text) =>
        text.Length <= 63 ? text : text[..63];

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenuStrip?.Dispose();
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
