// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.ApplicationModel.Communication;

/// <summary>
/// Represents a contact on the user's device.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>Contact</c>.</remarks>
public class Contact
{
    string? displayName;

    /// <summary>
    /// Initializes a new instance of the <see cref="Contact"/> class.
    /// </summary>
    public Contact()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Contact"/> class with the given data.
    /// </summary>
    /// <param name="id">The identifier of the contact.</param>
    /// <param name="namePrefix">The prefix of the contact.</param>
    /// <param name="givenName">The given name (or first name) of the contact.</param>
    /// <param name="middleName">The middle name(s) of the contact.</param>
    /// <param name="familyName">The family name (or last name) of the contact.</param>
    /// <param name="nameSuffix">The suffix of the contact.</param>
    /// <param name="phones">A collection of phone numbers for the contact.</param>
    /// <param name="email">A collection of email addresses for the contact.</param>
    /// <param name="displayName">The display name of the contact.</param>
    public Contact(
        string? id,
        string? namePrefix,
        string? givenName,
        string? middleName,
        string? familyName,
        string? nameSuffix,
        IEnumerable<ContactPhone>? phones,
        IEnumerable<ContactEmail>? email,
        string? displayName = null)
    {
        Id = id;
        NamePrefix = namePrefix;
        GivenName = givenName;
        MiddleName = middleName;
        FamilyName = familyName;
        NameSuffix = nameSuffix;
        if (phones != null)
            Phones.AddRange(phones);
        if (email != null)
            Emails.AddRange(email);
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets or sets the identifier of the contact.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the display name of the contact.
    /// </summary>
    /// <remarks>If no display name is set, a display name is inferred from <see cref="GivenName"/> and <see cref="FamilyName"/>.</remarks>
    public string? DisplayName
    {
        get => !string.IsNullOrWhiteSpace(displayName) ? displayName : BuildDisplayName();
        private set => displayName = value;
    }

    /// <summary>
    /// Gets or sets the name prefix of the contact.
    /// </summary>
    public string? NamePrefix { get; set; }

    /// <summary>
    /// Gets or sets the given name (or first name) of the contact.
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// Gets or sets the middle name(s) of the contact.
    /// </summary>
    public string? MiddleName { get; set; }

    /// <summary>
    /// Gets or sets the family name (or last name) of the contact.
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Gets or sets the name suffix of the contact.
    /// </summary>
    public string? NameSuffix { get; set; }

    /// <summary>
    /// Gets or sets a collection of phone numbers of the contact.
    /// </summary>
    public List<ContactPhone> Phones { get; set; } = [];

    /// <summary>
    /// Gets or sets a collection of email addresses of the contact.
    /// </summary>
    public List<ContactEmail> Emails { get; set; } = [];

    /// <inheritdoc />
    public override string? ToString() => DisplayName;

    string? BuildDisplayName()
    {
        if (string.IsNullOrWhiteSpace(GivenName))
            return FamilyName;
        if (string.IsNullOrWhiteSpace(FamilyName))
            return GivenName;

        return $"{GivenName} {FamilyName}";
    }
}

/// <summary>
/// Represents an email address that is associated with a <see cref="Contact"/>.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>ContactEmail</c>.</remarks>
public class ContactEmail
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactEmail"/> class.
    /// </summary>
    public ContactEmail()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactEmail"/> class with the given data.
    /// </summary>
    /// <param name="emailAddress">The email address.</param>
    public ContactEmail(string? emailAddress)
    {
        EmailAddress = emailAddress;
    }

    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    public string? EmailAddress { get; set; }

    /// <inheritdoc />
    public override string? ToString() => EmailAddress;
}

/// <summary>
/// Represents a phone number that is associated with a <see cref="Contact"/>.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>ContactPhone</c>.</remarks>
public class ContactPhone
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContactPhone"/> class.
    /// </summary>
    public ContactPhone()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ContactPhone"/> class with the given data.
    /// </summary>
    /// <param name="phoneNumber">The phone number.</param>
    public ContactPhone(string? phoneNumber)
    {
        PhoneNumber = phoneNumber;
    }

    /// <summary>
    /// Gets or sets the phone number.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <inheritdoc />
    public override string? ToString() => PhoneNumber;
}
