// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Power;

/// <summary>
/// The default <see cref="IKeepAwakeService"/> implementation.
/// </summary>
internal sealed class KeepAwakeService : IKeepAwakeService, IDisposable
{
    readonly KeepAwakeOptions _options;
    readonly IPowerManager _powerManager;

    public KeepAwakeService(IOptions<KeepAwakeOptions> options, IPowerManager powerManager)
    {
        _options = options.Value;
        _powerManager = powerManager;
    }

    public event EventHandler? IsEnabledChanged;

    public bool IsEnabled { get; private set; }

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
            return;

        IsEnabled = enabled;
        _powerManager.SetKeepAwake(enabled, _options.KeepDisplayOn);

        IsEnabledChanged?.Invoke(this, EventArgs.Empty);
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
        // Make sure the sleep block is released when the host is disposed.
        if (IsEnabled)
            SetEnabled(false);
    }
}
