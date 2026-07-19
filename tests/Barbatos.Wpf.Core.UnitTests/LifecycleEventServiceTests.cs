// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.LifecycleEvents;

namespace Barbatos.Wpf.Core.UnitTests;

public class LifecycleEventServiceTests
{
    delegate void CustomDelegate(string value);

    [Fact]
    public void AddedEventCanBeRetrieved()
    {
        var service = new LifecycleEventService(Enumerable.Empty<LifecycleEventRegistration>());

        Action action = () => { };
        service.AddEvent("MyEvent", action);

        var delegates = service.GetEventDelegates<Action>("MyEvent").ToArray();

        Assert.Single(delegates);
        Assert.Same(action, delegates[0]);
    }

    [Fact]
    public void ContainsEventReturnsCorrectValues()
    {
        var service = new LifecycleEventService(Enumerable.Empty<LifecycleEventRegistration>());

        Assert.False(service.ContainsEvent("MyEvent"));

        service.AddEvent("MyEvent", () => { });

        Assert.True(service.ContainsEvent("MyEvent"));
        Assert.False(service.ContainsEvent("OtherEvent"));
    }

    [Fact]
    public void GetEventDelegatesFiltersByDelegateType()
    {
        var service = new LifecycleEventService(Enumerable.Empty<LifecycleEventRegistration>());

        service.AddEvent("MyEvent", new Action(() => { }));
        service.AddEvent("MyEvent", new CustomDelegate(value => { }));

        Assert.Single(service.GetEventDelegates<Action>("MyEvent"));
        Assert.Single(service.GetEventDelegates<CustomDelegate>("MyEvent"));
    }

    [Fact]
    public void GetEventDelegatesReturnsEmptyForUnknownEvent()
    {
        var service = new LifecycleEventService(Enumerable.Empty<LifecycleEventRegistration>());

        Assert.Empty(service.GetEventDelegates<Action>("Unknown"));
    }

    [Fact]
    public void RegistrationsAreAppliedInConstructor()
    {
        var registrations = new[]
        {
            new LifecycleEventRegistration(builder => builder.AddEvent("Event1", () => { })),
            new LifecycleEventRegistration(builder => builder.AddEvent("Event2", () => { })),
        };

        var service = new LifecycleEventService(registrations);

        Assert.True(service.ContainsEvent("Event1"));
        Assert.True(service.ContainsEvent("Event2"));
    }

    [Fact]
    public void InvokeEventsInvokesAllActions()
    {
        var service = new LifecycleEventService(Enumerable.Empty<LifecycleEventRegistration>());

        var count = 0;
        service.AddEvent("MyEvent", new Action(() => count++));
        service.AddEvent("MyEvent", new Action(() => count++));

        service.InvokeEvents("MyEvent");

        Assert.Equal(2, count);
    }

    [Fact]
    public void InvokeEventsWithDelegateTypePassesArguments()
    {
        var service = new LifecycleEventService(Enumerable.Empty<LifecycleEventRegistration>());

        string? received = null;
        service.AddEvent("MyEvent", new CustomDelegate(value => received = value));

        service.InvokeEvents<CustomDelegate>("MyEvent", del => del("hello"));

        Assert.Equal("hello", received);
    }
}
