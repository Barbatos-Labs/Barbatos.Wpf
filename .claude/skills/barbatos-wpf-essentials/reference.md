# Essentials reference tables

Contents:

1. [Module map](#module-map) - facade, interface, namespace, what the Windows code really does
2. [Identity metadata keys and fallback chains](#identity-metadata-keys-and-fallback-chains)
3. [Where the data lives](#where-the-data-lives)
4. [Installer field mapping](#installer-field-mapping)
5. [InstallDate / InstallLocation lookup](#installdate--installlocation-lookup)
6. [Permissions on Windows](#permissions-on-windows)
7. [Exception types](#exception-types)

Every row was read from `src/Barbatos.Wpf.Core/Essentials/<Module>/` and
`src/Barbatos.Wpf.Core/Hosting/EssentialsExtensions.cs`. Prose versions:
[README Essentials](../../../src/Barbatos.Wpf.Core/README.md#essentials).

---

## Module map

| Module | Static accessor | Interface | Namespace | Windows implementation and limits |
| --- | --- | --- | --- | --- |
| App info | `AppInfo.Current` | `IAppInfo` | `Barbatos.Wpf.ApplicationModel` | Entry-assembly attributes + `Barbatos.Wpf.ApplicationModel.AppInfo.*` metadata; `RequestedTheme` reads `AppsUseLightTheme` on every access (no change event); `PackagingModel` via `GetCurrentPackageFullName`; `ShowSettingsUI()` opens `ms-settings:appsfeatures-app` |
| Publisher info | `PublisherInfo.Current` | `IPublisherInfo` | `Barbatos.Wpf.ApplicationModel` | Metadata, then `AssemblyCompanyAttribute` / `AssemblyCopyrightAttribute`; `Website`/`SupportUrl`/`SupportEmail` are `null` unless set |
| Device identity | `DeviceIdentity.Default` | `IDeviceIdentity` | `Barbatos.Wpf.ApplicationModel` | Instance id in `SecureStorage.Default` under `Barbatos.Wpf.DeviceIdentity.InstanceId`, read-or-create serialized within the process; fingerprint = SHA-256 of `AppId` + WMI `Win32_BaseBoard.SerialNumber`, `Win32_BIOS.SerialNumber`, `Win32_Processor.ProcessorId` (falls back to `Environment.MachineName`), computed once per process on the thread pool |
| Device info | `DeviceInfo.Current` | `IDeviceInfo` | `Barbatos.Wpf.Devices` | `Model`/`Manufacturer` from `HKLM\HARDWARE\DESCRIPTION\System\BIOS`; `Name` = `Environment.MachineName`; `Version` = `Environment.OSVersion.Version` (Windows 11 is still major 10, build 22000+); `Platform` always `DevicePlatform.WPF`; `Idiom` Tablet/Desktop by a Chromium-style touch/rotation/power-role heuristic; `DeviceType.Virtual` when `SystemProductName` contains `Virtual` (ordinal) or equals `HMV domU` |
| File system | `FileSystem.Current` | `IFileSystem` | `Barbatos.Wpf.Storage` | See [Where the data lives](#where-the-data-lives); `OpenAppPackageFileAsync`/`AppPackageFileExistsAsync` resolve against `AppContext.BaseDirectory` (output folder, not WPF `pack://` resources) |
| Preferences | `Preferences.Default` | `IPreferences` | `Barbatos.Wpf.Storage` | In-memory `ConcurrentDictionary` loaded once at construction; every `Set`/`Remove`/`Clear` rewrites the whole JSON file under a lock |
| Secure storage | `SecureStorage.Default` | `ISecureStorage` | `Barbatos.Wpf.Storage` | DPAPI `ProtectedData`, `DataProtectionScope.CurrentUser`, fixed entropy `Barbatos.Wpf.SecureStorage`; same load-once / rewrite-on-write file pattern |
| Version tracking | `VersionTracking.Default` | `IVersionTracking` | `Barbatos.Wpf.ApplicationModel` | Pure C# over `Preferences.Default` + `AppInfo.Current`; constructing it records the launch |
| Connectivity | `Connectivity.Current` | `IConnectivity` | `Barbatos.Wpf.Networking` | `System.Net.NetworkInformation`: `Internet` = an up, non-loopback, non-tunnel adapter with a default gateway; otherwise `Local` or `None`. Never reports `ConstrainedInternet`; no internet probe |
| Device display | `DeviceDisplay.Current` | `IDeviceDisplay` | `Barbatos.Wpf.Devices` | Monitor of the active window (else `MainWindow`) via `MonitorFromRect`/`EnumDisplaySettings`/`GetDpiForWindow`; `KeepScreenOn` = `SetThreadExecutionState(ES_CONTINUOUS \| ES_DISPLAY_REQUIRED)` held by a background thread of its own while `true` (never the caller's thread, so it stacks with keep-awake and may be set and cleared from any thread); change event from `SystemEvents.DisplaySettingsChanged` |
| Email | `Email.Default` | `IEmail` | `Barbatos.Wpf.ApplicationModel.Communication` | Simple MAPI's Unicode `MAPISendMailW` on a thread-pool thread, modeless dialog; user cancel = success, no usable client = `FeatureNotSupportedException`, missing attachment = `FileNotFoundException` (checked first), other MAPI codes = `InvalidOperationException`; `IsComposeSupported` always `true` |
| Launcher | `Launcher.Default` | `ILauncher` | `Barbatos.Wpf.ApplicationModel` | `Process.Start` with `UseShellExecute = true`, `true` whenever it does not throw (also when the shell hands off to a running or COM-activated handler and returns no `Process`), `false` on any exception; `CanOpenAsync` = `HKCR\<scheme>` has `URL Protocol` or `shell\open\command` |
| App actions | `AppActions.Current` | `IAppActions` | `Barbatos.Wpf.ApplicationModel` | `System.Windows.Shell.JumpList` on `Application.Current` (`ShowRecentCategory`/`ShowFrequentCategory` = `false`); each `JumpTask` relaunches the exe with `BARBATOS_APP_ACTIONS-` + Base64(id); `GetAsync` returns the in-process cache |
| Contacts | `Contacts.Default` | `IContacts` | `Barbatos.Wpf.ApplicationModel.Communication` | Every member throws `FeatureNotSupportedException` (synchronously, at the call) |
| Geolocation | `Geolocation.Default` | `IGeolocation` | `Barbatos.Wpf.Devices.Sensors` | Every query throws `FeatureNotSupportedException`; `IsEnabled`/`IsListeningForeground` are `false`; `StopListeningForeground()` no-op; `Location.CalculateDistance(...)` works |
| Permissions | `Permissions.CheckStatusAsync<T>()` | *(none)* | `Barbatos.Wpf.ApplicationModel` | See [Permissions on Windows](#permissions-on-windows) |

`IEssentialsBuilder`, `EssentialsExtensions.UseEssentials` and `ConfigureEssentials` live in
`Barbatos.Wpf.Hosting`.

---

## Identity metadata keys and fallback chains

Set in the app's csproj as `<AssemblyMetadata Include="<key>" Value="..." />`. The metadata is
read from `Assembly.GetEntryAssembly()`, so it must be on the **exe** project, not a class
library. A key is checked before the standard attribute; first non-null wins.

| Property | Metadata key | Then |
| --- | --- | --- |
| `AppInfo.AppId` | `Barbatos.Wpf.ApplicationModel.AppInfo.AppId` | `...AppInfo.AppGuid` (deprecated key) → `AssemblyTitleAttribute` → assembly name |
| *(uninstall subkey)* | `Barbatos.Wpf.ApplicationModel.AppInfo.UninstallRegistryKey` | `AppId` |
| `AppInfo.Name` | `Barbatos.Wpf.ApplicationModel.AppInfo.Name` | `<Product>` → `AssemblyTitleAttribute` → assembly name |
| `AppInfo.Version` / `VersionString` | `Barbatos.Wpf.ApplicationModel.AppInfo.Version` (parsed with `Version.Parse` - numeric only) | the entry assembly's `AssemblyName.Version` (always four parts; the SDK derives it from `<Version>` with any prerelease suffix stripped) → `0.0` |
| `AppInfo.BuildString` | *(derived)* | `Version.Revision` - the fourth part |
| `PublisherInfo.Name` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.Name` | `<Company>` → empty string |
| `PublisherInfo.Copyright` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.Copyright` | `<Copyright>` |
| `PublisherInfo.Website` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.Website` | `null` |
| `PublisherInfo.SupportUrl` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportUrl` | `null` |
| `PublisherInfo.SupportEmail` | `Barbatos.Wpf.ApplicationModel.PublisherInfo.SupportEmail` | `null` |

Other places the same values surface: `IHostEnvironment.ApplicationName` and the built-in
splash screen title (unless `SplashScreenOptions.AppName` is set) use `AppInfo.Name`; the single-instance mutex name is
`Barbatos.Wpf.SingleInstance.` + `AppId` (from the injected `IAppInfo`); the SignalR push
handshake sends `AppInfo.VersionString` unless `SignalRPushNotificationOptions.AppVersion` is set.

---

## Where the data lives

All per-user, under `%LocalAppData%`. `{Publisher}` and `{AppId}` are `PublisherInfo.Name`
and `AppInfo.AppId` with invalid file-name characters replaced by `_` (braces survive, so a
GUID AppId gives a `{C6BB...}` folder).

| What | Path |
| --- | --- |
| `FileSystem.AppDataDirectory` | `%LocalAppData%\{Publisher}\{AppId}\Data` (created on first access) |
| `FileSystem.CacheDirectory` | `%LocalAppData%\{Publisher}\{AppId}\Cache` (created on first access; nothing cleans it) |
| `Preferences` (all `sharedName` containers, plus version-tracking history) | `%LocalAppData%\{Publisher}\{AppId}\Settings\preferences.dat` (JSON, plain text) |
| `SecureStorage` (plus the DeviceIdentity instance id and the default AI key / MCP token stores) | `%LocalAppData%\{Publisher}\{AppId}\Settings\securestorage.dat` (JSON of DPAPI blobs) |

Version tracking keeps its history in the `Preferences` container named
`{AppId}.barbatos.wpf.essentials.versiontracking` (keys `VersionTracking.Versions`,
`VersionTracking.Builds`, `|`-separated), so `Preferences.Clear()` without a `sharedName`
does not reset it.

The paths are computed once per process, from the static facades. Replacing `IAppInfo` or
`IPublisherInfo` in DI does not move them.

---

## Installer field mapping

The same table applies to Inno Setup and WiX/MSI, which both use the Windows `Uninstall`
registry convention.

| Inno Setup `[Setup]` | Uninstall value | Barbatos source |
| --- | --- | --- |
| `AppId` | the key name (Inno appends `_is1`) | `AppInfo.AppId`, plus `UninstallRegistryKey` = `{AppId}_is1` |
| `AppName` | `DisplayName` | `AppInfo.Name` (`<Product>`) |
| `AppVersion` | `DisplayVersion` | `AppInfo.VersionString` |
| `AppPublisher` | `Publisher` | `PublisherInfo.Name` (`<Company>`) |
| `AppPublisherURL` | `URLInfoAbout` | `PublisherInfo.Website` |
| `AppSupportURL` | `HelpLink` | `PublisherInfo.SupportUrl` |
| `AppUpdatesURL` | `URLUpdateInfo` | no dedicated property |

Inno's `GetVersionNumbers()` reads the Win32 `FileVersion` resource, while
`AppInfo.VersionString` reads `AssemblyVersion`. With only `<Version>` set they agree: the SDK
derives `AssemblyVersion` from it (prerelease suffix stripped, `1.3.0-beta.1` → `1.3.0.0`) and
defaults `FileVersion` to that. They diverge only when `<AssemblyVersion>` is pinned separately;
then set `<FileVersion>` to the same numeric value, or set the `...AppInfo.Version` metadata.
`<AssemblyVersion>$(Version)</AssemblyVersion>` with a prerelease `<Version>` fails with `CS7034`.

MSI/WiX needs no `UninstallRegistryKey`: its uninstall key is the product code, so set
`AppId` to that GUID.

---

## InstallDate / InstallLocation lookup

- Subkey `SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\<name>`, where `<name>` is
  `UninstallRegistryKey` (default `AppId`) used verbatim; a GUID-shaped name is normalised to
  the braced form, so `C6BB...` and `{C6BB...}` both match.
- Tried in order: HKLM 64-bit, HKLM 32-bit (`WOW6432Node`), HKCU 64-bit, HKCU 32-bit. The first
  key with either value set wins.
- `InstallDate` must be a `REG_SZ` in `yyyyMMdd`; anything else yields `null` while
  `InstallLocation` still resolves.
- Computed lazily once per process; registry failures read as "not installed". Never throws.
- `null` from the IDE / `dotnet run` is the normal case, not a bug.

---

## Permissions on Windows

`Permissions` is static and generic; there is no interface to inject. Every nested type is a
`Permissions.BasePlatformPermission` with `CheckStatusAsync()` → `Granted`,
`RequestAsync()` → `CheckStatusAsync()`, `EnsureDeclared()` no-op, `ShouldShowRationale()` →
`false`.

| Type | `CheckStatusAsync` / `RequestAsync` |
| --- | --- |
| `Battery`, `Bluetooth`, `CalendarRead`, `CalendarWrite`, `Camera`, `Flashlight`, `LaunchApp`, `Maps`, `Media`, `NearbyWifiDevices`, `NetworkState`, `Phone`, `Photos`, `PhotosAddOnly`, `PostNotifications`, `Reminders`, `Sms`, `Speech`, `StorageRead`, `StorageWrite`, `Vibrate` | `PermissionStatus.Granted`, unconditionally - nothing is checked |
| `ContactsRead`, `ContactsWrite`, `LocationWhenInUse`, `LocationAlways`, `Microphone`, `Sensors` | throw `FeatureNotSupportedException` |

`Granted` means "this library did not check", not "the OS allows it": Windows privacy toggles
can still block a desktop app's camera or microphone, and `PostNotifications` says nothing
about whether a toast will show (use `INotificationService.Availability`, see
`barbatos-wpf-host`). For a real check, subclass `Permissions.BasePlatformPermission` (or one
of the non-sealed nested types), override `CheckStatusAsync()` - `RequestAsync()` forwards to
it - and pass the subclass as `TPermission`:

```csharp
sealed class CameraPrivacyPermission : Permissions.BasePlatformPermission
{
    // ReadCameraConsent() is the app's own check, e.g. of the Windows privacy settings.
    public override Task<PermissionStatus> CheckStatusAsync() =>
        Task.FromResult(ReadCameraConsent() ? PermissionStatus.Granted : PermissionStatus.Denied);
}

var status = await Permissions.RequestAsync<CameraPrivacyPermission>();
```

`PermissionStatus`: `Unknown`, `Denied`, `Disabled`, `Granted`, `Restricted`, `Limited`.

---

## Exception types

All in `Barbatos.Wpf.ApplicationModel`.

| Type | Base | Thrown by |
| --- | --- | --- |
| `FeatureNotSupportedException` | `NotSupportedException` | `Contacts`, `Geolocation` queries, six `Permissions` types, `Email.ComposeAsync` with `EmailBodyFormat.Html` or when no Simple MAPI client answers (none installed or default, logon failure) |
| `FeatureNotEnabledException` | `InvalidOperationException` | reserved for features that exist but are off |
| `PermissionException` | `UnauthorizedAccessException` | reserved for permission-gated APIs |

`Email.ComposeAsync` also throws `FileNotFoundException` naming an attachment that does not
exist (before any mail client starts) and `InvalidOperationException` for any other Simple MAPI
error (the code is in the message); a user cancelling the compose window is not an error.

Not wrapped by the library: `CryptographicException` from `SecureStorage.GetAsync` (DPAPI
cannot decrypt), `NotSupportedException` from `IPreferences.Set<T>` with an unsupported `T`,
`FormatException` from `AppInfo.Version` when the `...AppInfo.Version` metadata is not a
numeric version, `UriFormatException` from the `string` overloads of `Launcher`,
`FileNotFoundException` (or `DirectoryNotFoundException` for a missing subfolder) from
`FileSystem.OpenAppPackageFileAsync`, `ArgumentNullException`
for a null/whitespace `SecureStorage` key or null value.
