// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

/// <summary>
/// Provides identifiers suitable for license enforcement (e.g. "this license may only be
/// active on N machines") without collecting raw, reversible hardware serial numbers.
/// </summary>
/// <remarks>
/// This has no .NET MAUI counterpart — MAUI's own <c>DeviceInfo</c> deliberately exposes no
/// device identifier or network address, for the same privacy reasons described here. Both
/// members of this API return values that are still personal data under most privacy laws
/// (GDPR, Vietnam's Decree 13/2023/NĐ-CP, ...) once associated with a customer record (e.g. a
/// purchase/email on your license server) — using them still requires a privacy policy
/// disclosure and a lawful basis for processing; this API only controls how invasive the
/// underlying identifier itself is, not whether you need to disclose collecting it.
/// </remarks>
public interface IDeviceIdentity
{
    /// <summary>
    /// Gets a random identifier generated on first use and persisted via
    /// <see cref="Barbatos.Wpf.Storage.SecureStorage"/> under the key
    /// <c>Barbatos.Wpf.DeviceIdentity.InstanceId</c>. The least invasive option: it identifies
    /// this app's data for this Windows user, not the physical machine.
    /// </summary>
    /// <remarks>
    /// The id lives in <c>%LocalAppData%\{PublisherInfo.Name}\{AppInfo.AppId}\Settings\securestorage.dat</c>,
    /// which an ordinary uninstaller leaves behind, so it usually <b>survives a reinstall</b>. It
    /// changes when that file is lost (deleted by the user or a cleanup tool, a new Windows user
    /// profile, a new machine), when <see cref="Barbatos.Wpf.Storage.ISecureStorage.RemoveAll"/>
    /// runs, or when <see cref="IAppInfo.AppId"/> or <see cref="IPublisherInfo.Name"/> changes
    /// (both are part of the path). Calls within one process are serialized, so concurrent first
    /// calls all receive the same id; two processes starting at the same moment can still each
    /// mint one, and the last one written wins on disk.
    /// </remarks>
    /// <returns>A newly generated or previously stored GUID string.</returns>
    Task<string> GetInstanceIdAsync();

    /// <summary>
    /// Gets a stable hash derived from a few motherboard/BIOS/CPU identifiers (read via WMI)
    /// and salted with <see cref="IAppInfo.AppId"/>, so the result is scoped to this specific
    /// app — like Apple's <c>IdentifierForVendor</c> — rather than being a single hardware ID
    /// usable to correlate the same machine across unrelated apps. The underlying serial
    /// numbers are never stored or returned in raw form, only as a one-way SHA-256 hash.
    /// Survives reinstalls, deleting the app's local data and switching Windows user (unlike
    /// <see cref="GetInstanceIdAsync"/>, which lives in that data), which is what makes it
    /// suitable for enforcing a per-machine activation limit that a user can't bypass by
    /// clearing the app's data.
    /// </summary>
    /// <returns>
    /// A 64-character uppercase hex SHA-256 hash. Falls back to hashing
    /// <see cref="Environment.MachineName"/> alone when no WMI identifier is readable (e.g. a
    /// locked-down environment) — still deterministic per machine, just weaker.
    /// </returns>
    Task<string> GetHardwareFingerprintAsync();
}

/// <summary>
/// Provides identifiers suitable for license enforcement without collecting raw, reversible
/// hardware serial numbers.
/// </summary>
public static class DeviceIdentity
{
    /// <inheritdoc cref="IDeviceIdentity.GetInstanceIdAsync" />
    public static Task<string> GetInstanceIdAsync() =>
        Default.GetInstanceIdAsync();

    /// <inheritdoc cref="IDeviceIdentity.GetHardwareFingerprintAsync" />
    public static Task<string> GetHardwareFingerprintAsync() =>
        Default.GetHardwareFingerprintAsync();

    static IDeviceIdentity? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IDeviceIdentity Default =>
        defaultImplementation ??= new DeviceIdentityImplementation();

    internal static void SetDefault(IDeviceIdentity? implementation) =>
        defaultImplementation = implementation;
}
