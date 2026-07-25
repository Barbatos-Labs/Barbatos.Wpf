// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Reflection;
using Microsoft.Extensions.Options;

namespace Barbatos.Wpf.Startup;

/// <summary>
/// The default <see cref="IRunOnStartupService"/> implementation.
/// </summary>
internal sealed class RunOnStartupService : IRunOnStartupService
{
    readonly RunOnStartupOptions _options;
    readonly IStartupRegistrar _registrar;

    public RunOnStartupService(IOptions<RunOnStartupOptions> options, IStartupRegistrar registrar)
    {
        _options = options.Value;
        _registrar = registrar;
    }

    public event EventHandler? IsEnabledChanged;

    public bool IsEnabled => _registrar.IsRegistered(EntryName);

    public void SetEnabled(bool enabled)
    {
        if (enabled == IsEnabled)
            return;

        if (enabled)
            _registrar.Register(EntryName, BuildCommand());
        else
            _registrar.Unregister(EntryName);

        IsEnabledChanged?.Invoke(this, EventArgs.Empty);
    }

    internal string EntryName =>
        _options.EntryName
        ?? Assembly.GetEntryAssembly()?.GetName().Name
        ?? "Barbatos.Wpf.App";

    internal string BuildCommand()
    {
        var executable = _options.ExecutablePath ?? Environment.ProcessPath
            ?? throw new InvalidOperationException("Unable to determine the executable path for the startup registration.");

        var command = $"\"{executable}\"";

        if (!string.IsNullOrWhiteSpace(_options.Arguments))
            command += $" {_options.Arguments}";

        return command;
    }

    /// <summary>
    /// Applies the configured options during application construction.
    /// </summary>
    internal void ApplyOptions()
    {
        if (_options.Enabled)
            SetEnabled(true);
    }
}
