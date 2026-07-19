// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel.Communication;

/// <summary>
/// The Contacts API lets a user pick a contact and retrieve information about it.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IContacts</c>.</remarks>
public interface IContacts
{
    /// <summary>
    /// Opens the operating system's default UI for picking a contact from the device.
    /// </summary>
    /// <returns>A single contact, or <see langword="null"/> if the user cancelled the operation.</returns>
    Task<Contact?> PickContactAsync();

    /// <summary>
    /// Gets a collection of all the contacts on the device.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used for cancelling the operation.</param>
    /// <returns>A collection of contacts on the device.</returns>
    Task<IEnumerable<Contact>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The Contacts API lets a user pick a contact and retrieve information about it.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>Contacts</c>. .NET MAUI's Windows
/// implementation is built on the WinRT <c>Windows.ApplicationModel.Contacts</c> contract
/// (<c>ContactPicker</c>/<c>ContactManager</c>), which is not available to a plain
/// unpackaged desktop WPF app. Both members therefore throw
/// <see cref="FeatureNotSupportedException"/> on this platform; the type surface (this
/// interface, <see cref="Contact"/>, <see cref="ContactEmail"/>, <see cref="ContactPhone"/>)
/// is kept identical so code written against it compiles and can be swapped for a real
/// implementation (for example a custom <see cref="IContacts"/> backed by an address book
/// service) via <c>Contacts.SetDefault</c>.
/// </remarks>
public static class Contacts
{
    /// <inheritdoc cref="IContacts.PickContactAsync" />
    public static Task<Contact?> PickContactAsync() =>
        Default.PickContactAsync();

    /// <inheritdoc cref="IContacts.GetAllAsync(CancellationToken)" />
    public static Task<IEnumerable<Contact>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Default.GetAllAsync(cancellationToken);

    static IContacts? defaultImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IContacts Default =>
        defaultImplementation ??= new ContactsImplementation();

    internal static void SetDefault(IContacts? implementation) =>
        defaultImplementation = implementation;
}

/// <summary>
/// The Windows implementation of <see cref="IContacts"/>. See the remarks on
/// <see cref="Contacts"/> for why this throws <see cref="FeatureNotSupportedException"/>.
/// </summary>
class ContactsImplementation : IContacts
{
    public Task<Contact?> PickContactAsync() =>
        throw new FeatureNotSupportedException(
            "Picking a contact requires the WinRT Windows.ApplicationModel.Contacts.ContactPicker API, " +
            "which is not available to an unpackaged WPF application.");

    public Task<IEnumerable<Contact>> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new FeatureNotSupportedException(
            "Reading the contacts list requires the WinRT Windows.ApplicationModel.Contacts.ContactManager API, " +
            "which is not available to an unpackaged WPF application.");
}
