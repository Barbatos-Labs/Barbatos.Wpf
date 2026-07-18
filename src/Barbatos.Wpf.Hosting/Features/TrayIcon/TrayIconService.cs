// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Tray;

/// <summary>
/// The default <see cref="ITrayIconService"/> implementation.
/// </summary>
internal sealed class TrayIconService : ITrayIconService
{
    readonly TrayIconOptions _options;
    readonly ITrayIconPlatform _platform;

    public TrayIconService(IOptions<TrayIconOptions> options, ITrayIconPlatform platform)
    {
        _options = options.Value;
        _platform = platform;
        _platform.Clicked += (sender, args) => Clicked?.Invoke(this, args);
        _platform.DoubleClicked += (sender, args) => DoubleClicked?.Invoke(this, args);
    }

    public event EventHandler? IsVisibleChanged;

    public event EventHandler? Clicked;

    public event EventHandler? DoubleClicked;

    public bool IsVisible { get; private set; }

    public void SetVisible(bool visible)
    {
        if (visible == IsVisible)
            return;

        if (visible)
            _platform.Show(_options);
        else
            _platform.Hide();

        IsVisible = visible;
        IsVisibleChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetToolTip(string toolTip)
    {
        _ = toolTip ?? throw new ArgumentNullException(nameof(toolTip));

        _options.ToolTip = toolTip;
        _platform.SetToolTip(toolTip);
    }

    /// <summary>
    /// Applies the configured options during application construction.
    /// </summary>
    internal void ApplyOptions()
    {
        if (_options.Enabled)
            SetVisible(true);
    }
}
