// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel;

public static partial class Permissions
{
    /// <summary>
    /// Represents the platform-specific abstract base class for all permissions on this platform.
    /// </summary>
    /// <remarks>
    /// This is the WPF counterpart of .NET MAUI's Windows <c>BasePlatformPermission</c>. MAUI's
    /// Windows implementation checks required capabilities against the app's <c>AppxManifest.xml</c>
    /// — a concept that does not exist for a plain unpackaged desktop WPF app, so
    /// <see cref="EnsureDeclared"/> is a no-op here and every permission that MAUI itself does not
    /// back with a real device check (see the list below) simply reports <see cref="PermissionStatus.Granted"/>,
    /// exactly like MAUI's own Windows implementation does for those same permissions.
    /// <para>
    /// A handful of permissions (<see cref="ContactsRead"/>, <see cref="ContactsWrite"/>,
    /// <see cref="LocationWhenInUse"/>, <see cref="LocationAlways"/>, <see cref="Microphone"/>,
    /// <see cref="Sensors"/>) are backed in .NET MAUI by real WinRT device-access contracts
    /// (<c>Windows.ApplicationModel.Contacts.ContactManager</c>,
    /// <c>Windows.Devices.Geolocation.Geolocator</c>,
    /// <c>Windows.Devices.Enumeration.DeviceAccessInformation</c>,
    /// <c>Windows.Media.Capture.MediaCapture</c>) that require a TargetFramework with WinRT
    /// projections and, for some APIs, MSIX packaging — the same machinery
    /// <see cref="Communication.Contacts"/> and <see cref="Devices.Sensors.Geolocation"/> already
    /// opt out of for this library. Those six
    /// permissions therefore throw <see cref="FeatureNotSupportedException"/> instead, keeping the
    /// type surface identical to MAUI while being honest that no permission check is actually
    /// performed. See the README's "Contacts and Geolocation" note for how to register a real
    /// implementation.
    /// </para>
    /// </remarks>
    public abstract partial class BasePlatformPermission : BasePermission
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasePlatformPermission"/> class.
        /// </summary>
        protected BasePlatformPermission()
        {
        }

        /// <inheritdoc/>
        public override Task<PermissionStatus> CheckStatusAsync() =>
            Task.FromResult(PermissionStatus.Granted);

        /// <inheritdoc/>
        public override Task<PermissionStatus> RequestAsync() =>
            CheckStatusAsync();

        /// <inheritdoc/>
        public override void EnsureDeclared()
        {
            // No-op: unpackaged WPF apps have no AppxManifest.xml capability declarations to
            // verify against, unlike MAUI's packaged Windows apps.
        }

        /// <inheritdoc/>
        public override bool ShouldShowRationale() => false;
    }

    public partial class Battery : BasePlatformPermission
    {
    }

    public partial class Bluetooth : BasePlatformPermission
    {
    }

    public partial class CalendarRead : BasePlatformPermission
    {
    }

    public partial class CalendarWrite : BasePlatformPermission
    {
    }

    public partial class Camera : BasePlatformPermission
    {
    }

    public partial class ContactsRead : BasePlatformPermission
    {
        /// <inheritdoc/>
        public override Task<PermissionStatus> CheckStatusAsync() =>
            throw new FeatureNotSupportedException(
                "Reading the contacts list requires the WinRT Windows.ApplicationModel.Contacts.ContactManager API, " +
                "which is not available to an unpackaged WPF application.");
    }

    public partial class ContactsWrite : BasePlatformPermission
    {
        /// <inheritdoc/>
        public override Task<PermissionStatus> CheckStatusAsync() =>
            throw new FeatureNotSupportedException(
                "Writing to the contacts list requires the WinRT Windows.ApplicationModel.Contacts.ContactManager API, " +
                "which is not available to an unpackaged WPF application.");
    }

    public partial class Flashlight : BasePlatformPermission
    {
    }

    public partial class LaunchApp : BasePlatformPermission
    {
    }

    public partial class LocationWhenInUse : BasePlatformPermission
    {
        /// <inheritdoc/>
        public override Task<PermissionStatus> CheckStatusAsync() =>
            throw new FeatureNotSupportedException(
                "Checking location permission requires the WinRT Windows.Devices.Geolocation.Geolocator API, " +
                "which is not available to an unpackaged WPF application.");
    }

    public partial class LocationAlways : BasePlatformPermission
    {
        /// <inheritdoc/>
        public override Task<PermissionStatus> CheckStatusAsync() =>
            throw new FeatureNotSupportedException(
                "Checking location permission requires the WinRT Windows.Devices.Geolocation.Geolocator API, " +
                "which is not available to an unpackaged WPF application.");
    }

    public partial class Maps : BasePlatformPermission
    {
    }

    public partial class Media : BasePlatformPermission
    {
    }

    public partial class Microphone : BasePlatformPermission
    {
        /// <inheritdoc/>
        public override Task<PermissionStatus> CheckStatusAsync() =>
            throw new FeatureNotSupportedException(
                "Checking microphone permission requires the WinRT Windows.Devices.Enumeration.DeviceAccessInformation " +
                "and Windows.Media.Capture.MediaCapture APIs, which are not available to an unpackaged WPF application.");
    }

    public partial class NearbyWifiDevices : BasePlatformPermission
    {
    }

    public partial class NetworkState : BasePlatformPermission
    {
    }

    public partial class Phone : BasePlatformPermission
    {
    }

    public partial class Photos : BasePlatformPermission
    {
    }

    public partial class PhotosAddOnly : BasePlatformPermission
    {
    }

    public partial class PostNotifications : BasePlatformPermission
    {
    }

    public partial class Reminders : BasePlatformPermission
    {
    }

    public partial class Sensors : BasePlatformPermission
    {
        /// <inheritdoc/>
        public override Task<PermissionStatus> CheckStatusAsync() =>
            throw new FeatureNotSupportedException(
                "Checking sensor permission requires the WinRT Windows.Devices.Enumeration.DeviceAccessInformation API, " +
                "which is not available to an unpackaged WPF application.");
    }

    public partial class Sms : BasePlatformPermission
    {
    }

    public partial class Speech : BasePlatformPermission
    {
    }

    public partial class StorageRead : BasePlatformPermission
    {
    }

    public partial class StorageWrite : BasePlatformPermission
    {
    }

    public partial class Vibrate : BasePlatformPermission
    {
    }
}
