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
    bool IsRegistered(string entryName);

    void Register(string entryName, string command);

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
