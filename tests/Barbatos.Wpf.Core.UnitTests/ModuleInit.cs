// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.ApplicationModel.Communication;
using Barbatos.Wpf.Devices;
using Barbatos.Wpf.Devices.Sensors;
using Barbatos.Wpf.Networking;
using Barbatos.Wpf.Storage;

namespace Barbatos.Wpf.Core.UnitTests;

/// <summary>
/// The essentials static facades (<c>AppInfo.Current</c>, <c>Preferences.Default</c>, ...)
/// use .NET MAUI's own <c>field ??= new Implementation()</c> lazy-singleton pattern, which is
/// not thread-safe against its very first access. A normal app only ever reaches that first
/// access once, synchronously, during startup — but xUnit runs many test classes in parallel,
/// so the first-ever access can otherwise race across worker threads. Resolving every facade
/// once here, in a module initializer that runs before any test (and therefore before any
/// parallelism), removes that race for the test suite without touching the ported production
/// code, which intentionally mirrors MAUI's pattern as-is.
/// </summary>
static class ModuleInit
{
    [ModuleInitializer]
    public static void WarmUpSingletons()
    {
        _ = AppInfo.Current;
        _ = PublisherInfo.Current;
        _ = DeviceIdentity.Default;
        _ = DeviceInfo.Current;
        _ = FileSystem.Current;
        _ = Preferences.Default;
        _ = SecureStorage.Default;
        _ = VersionTracking.Default;
        _ = Connectivity.Current;
        _ = DeviceDisplay.Current;
        _ = Email.Default;
        _ = Contacts.Default;
        _ = Geolocation.Default;
        _ = AppActions.Current;
        _ = Launcher.Default;
    }
}
