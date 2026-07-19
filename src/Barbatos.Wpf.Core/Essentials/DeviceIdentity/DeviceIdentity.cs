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
    /// <see cref="Barbatos.Wpf.Storage.SecureStorage"/>, unique to this app installation. The least
    /// invasive option: it identifies "this install", not the physical machine, and resets if
    /// the app's local data is cleared or it is reinstalled.
    /// </summary>
    /// <returns>A newly generated or previously stored GUID string.</returns>
    Task<string> GetInstanceIdAsync();

    /// <summary>
    /// Gets a stable hash derived from a few motherboard/BIOS/CPU identifiers (read via WMI)
    /// and salted with <see cref="IAppInfo.AppGuid"/>, so the result is scoped to this specific
    /// app — like Apple's <c>IdentifierForVendor</c> — rather than being a single hardware ID
    /// usable to correlate the same machine across unrelated apps. The underlying serial
    /// numbers are never stored or returned in raw form, only as a one-way SHA-256 hash.
    /// Survives reinstalls and clearing the app's local data (unlike <see cref="GetInstanceIdAsync"/>),
    /// which is what makes it suitable for enforcing a per-machine activation limit that a
    /// user can't bypass by simply reinstalling.
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
