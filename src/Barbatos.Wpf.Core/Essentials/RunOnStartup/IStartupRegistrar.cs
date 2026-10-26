// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Win32;

namespace Barbatos.Wpf.Startup;

/// <summary>
/// Abstraction over the OS mechanism used to register an application to run on startup.
/// </summary>
public interface IStartupRegistrar
{
    /// <summary>Whether a startup entry named <paramref name="entryName"/> currently exists.</summary>
    bool IsRegistered(string entryName);

    /// <summary>
    /// Creates or overwrites the startup entry named <paramref name="entryName"/>, so the OS runs
    /// <paramref name="command"/> when the user signs in.
    /// </summary>
    /// <param name="entryName">The entry's name - unique per application.</param>
    /// <param name="command">The full command line to run, including the quoted executable path.</param>
    void Register(string entryName, string command);

    /// <summary>Removes the startup entry named <paramref name="entryName"/>. Does nothing if it does not exist.</summary>
    void Unregister(string entryName);
}

/// <summary>
/// The default <see cref="IStartupRegistrar"/> that uses the per-user
/// <c>HKCU\Software\Microsoft\Windows\CurrentVersion\Run</c> registry key.
/// </summary>
internal sealed class RegistryStartupRegistrar : IStartupRegistrar
{
    const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsRegistered(string entryName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(entryName) is not null;
    }

    public void Register(string entryName, string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        key.SetValue(entryName, command);
    }

    public void Unregister(string entryName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        key?.DeleteValue(entryName, throwOnMissingValue: false);
    }
}
