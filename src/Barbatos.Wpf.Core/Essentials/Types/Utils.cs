// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Reflection;

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Small internal helpers shared across the essentials modules, ported from .NET MAUI's
/// internal <c>Utils</c> class.
/// </summary>
static class Utils
{
    /// <summary>
    /// Gets the value for a given key from the assembly metadata. Shared by
    /// <see cref="AppInfo"/> and <see cref="PublisherInfo"/>, which each prefix
    /// <paramref name="key"/> with their own metadata namespace.
    /// </summary>
    internal static string? GetMetadataAttributeValue(this Assembly assembly, string key)
    {
        foreach (var attr in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            if (attr.Key == key)
                return attr.Value;
        }

        return null;
    }

    internal static Version ParseVersion(string version)
    {
        if (Version.TryParse(version, out var number))
            return number;

        if (int.TryParse(version, out var major))
            return new Version(major, 0);

        return new Version(0, 0);
    }

    /// <summary>
    /// Marshals <paramref name="action"/> onto the WPF application's UI dispatcher when one
    /// is available and required (equivalent to .NET MAUI's
    /// <c>MainThread.BeginInvokeOnMainThread</c>), otherwise runs it inline.
    /// </summary>
    internal static void InvokeOnMainThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.CheckAccess())
            dispatcher.BeginInvoke(action);
        else
            action();
    }
}
