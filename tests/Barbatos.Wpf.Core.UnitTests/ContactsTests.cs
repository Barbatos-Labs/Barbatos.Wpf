// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.ApplicationModel.Communication;

namespace Barbatos.Wpf.Core.UnitTests;

public class ContactsTests
{
    [Fact]
    public void DefaultIsCached()
    {
        Assert.NotNull(Contacts.Default);
        Assert.Same(Contacts.Default, Contacts.Default);
    }

    [Fact]
    public async Task PickContactAsyncThrowsFeatureNotSupported()
    {
        // Windows does not support this without the WinRT ContactPicker contract, which is
        // unavailable to an unpackaged WPF app. See the remarks on Contacts.
        await Assert.ThrowsAsync<FeatureNotSupportedException>(() => Contacts.PickContactAsync());
    }

    [Fact]
    public async Task GetAllAsyncThrowsFeatureNotSupported()
    {
        await Assert.ThrowsAsync<FeatureNotSupportedException>(() => Contacts.GetAllAsync());
    }

    [Fact]
    public void ContactDisplayNameFallsBackToGivenAndFamilyName()
    {
        var contact = new Contact(id: "1", namePrefix: null, givenName: "Ada", middleName: null, familyName: "Lovelace", nameSuffix: null, phones: null, email: null);

        Assert.Equal("Ada Lovelace", contact.DisplayName);
        Assert.Equal("Ada Lovelace", contact.ToString());
    }

    [Fact]
    public void ContactDisplayNameUsesTheExplicitValueWhenProvided()
    {
        var contact = new Contact("1", null, "Ada", null, "Lovelace", null, null, null, displayName: "Countess of Lovelace");

        Assert.Equal("Countess of Lovelace", contact.DisplayName);
    }

    [Fact]
    public void ContactEmailAndPhoneToStringReturnTheValue()
    {
        var email = new ContactEmail("ada@example.com");
        var phone = new ContactPhone("555-0100");

        Assert.Equal("ada@example.com", email.ToString());
        Assert.Equal("555-0100", phone.ToString());
    }
}
