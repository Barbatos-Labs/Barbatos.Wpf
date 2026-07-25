// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class StoreFeatureTests
{
    [Fact]
    public void AddStoreRegistersTheStoreAsASingleton()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStore<SimpleStore>();
        var wpfApp = builder.Build();

        var first = wpfApp.Services.GetRequiredService<SimpleStore>();
        var second = wpfApp.Services.GetRequiredService<SimpleStore>();

        Assert.Same(first, second);
    }

    [Fact]
    public void StoreIsNotConstructedUntilFirstResolved()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStore<SimpleStore>();
        var wpfApp = builder.Build();

        var registry = wpfApp.Services.GetRequiredService<IStoreRegistry>();
        Assert.Empty(registry.Stores);

        wpfApp.Services.GetRequiredService<SimpleStore>();

        Assert.Single(registry.Stores);
    }

    [Fact]
    public void StoreRegistryTracksAStoreOnceItIsResolved()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStore<SimpleStore>();
        var wpfApp = builder.Build();

        var store = wpfApp.Services.GetRequiredService<SimpleStore>();
        var registry = wpfApp.Services.GetRequiredService<IStoreRegistry>();

        Assert.Contains(store, registry.Stores);
    }

    [Fact]
    public void StoreRegistryDoesNotTrackAStoreThatWasNeverResolved()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStore<SimpleStore>();
        var wpfApp = builder.Build();

        var registry = wpfApp.Services.GetRequiredService<IStoreRegistry>();

        Assert.Empty(registry.Stores);
    }

    [Fact]
    public void MultipleAddStoreCallsShareOneRegistry()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddStore<SimpleStore>();
        builder.Services.AddStore<AnotherStore>();
        var wpfApp = builder.Build();

        var simple = wpfApp.Services.GetRequiredService<SimpleStore>();
        var another = wpfApp.Services.GetRequiredService<AnotherStore>();
        var registry = wpfApp.Services.GetRequiredService<IStoreRegistry>();

        Assert.Equal(2, registry.Stores.Count);
        Assert.Contains(simple, registry.Stores);
        Assert.Contains(another, registry.Stores);
    }

    [Fact]
    public void RegisteredPluginsApplyToEveryStoreOnceAtConstruction()
    {
        var plugin = new RecordingPlugin();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IStorePlugin>(plugin);
        builder.Services.AddStore<SimpleStore>();
        builder.Services.AddStore<AnotherStore>();
        var wpfApp = builder.Build();

        var simple = wpfApp.Services.GetRequiredService<SimpleStore>();
        var another = wpfApp.Services.GetRequiredService<AnotherStore>();

        Assert.Equal(2, plugin.AppliedTo.Count);
        Assert.Contains(simple, plugin.AppliedTo);
        Assert.Contains(another, plugin.AppliedTo);
    }

    [Fact]
    public void StoreConstructorCanInjectOtherServices()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<IFooService, FooService>();
        builder.Services.AddStore<StoreWithDependency>();
        var wpfApp = builder.Build();

        var store = wpfApp.Services.GetRequiredService<StoreWithDependency>();

        Assert.IsType<FooService>(store.Foo);
    }

    sealed class SimpleStore : StoreBase
    {
    }

    sealed class AnotherStore : StoreBase
    {
    }

    sealed class StoreWithDependency : StoreBase
    {
        public StoreWithDependency(IFooService foo) => Foo = foo;

        public IFooService Foo { get; }
    }

    sealed class RecordingPlugin : IStorePlugin
    {
        public List<StoreBase> AppliedTo { get; } = new();

        public void Apply(StoreBase store) => AppliedTo.Add(store);
    }
}
