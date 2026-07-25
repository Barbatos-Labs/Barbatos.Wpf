// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Core.UnitTests;

public class StoreBaseTests
{
    [Fact]
    public void PropertyChangedFiresWhenAPropertyActuallyChanges()
    {
        var store = new TestStore();
        var raised = 0;
        store.PropertyChanged += (_, _) => raised++;

        store.Count = 1;

        Assert.Equal(1, raised);
    }

    [Fact]
    public void PropertyChangedDoesNotFireWhenTheNewValueEqualsTheOldValue()
    {
        var store = new TestStore { Count = 1 };
        var raised = 0;
        store.PropertyChanged += (_, _) => raised++;

        store.Count = 1;

        Assert.Equal(0, raised);
    }

    [Fact]
    public void StateChangedFiresOnceForEachPropertySetOutsideAPatch()
    {
        var store = new TestStore();
        var raised = 0;
        store.StateChanged += (_, _) => raised++;

        store.Count = 1;
        store.Count = 2;

        Assert.Equal(2, raised);
    }

    [Fact]
    public void PatchBatchesMultiplePropertyChangesIntoASingleStateChangedNotification()
    {
        var store = new TestStore();
        var raised = 0;
        store.StateChanged += (_, _) => raised++;

        store.Patch(_ =>
        {
            store.Count = 1;
            store.Name = "a";
        });

        Assert.Equal(1, raised);
    }

    [Fact]
    public void PatchStillFiresPropertyChangedForEachIndividualPropertyInsideIt()
    {
        var store = new TestStore();
        var raised = 0;
        store.PropertyChanged += (_, _) => raised++;

        store.Patch(_ =>
        {
            store.Count = 1;
            store.Name = "a";
        });

        Assert.Equal(2, raised);
    }

    [Fact]
    public void NestedPatchCallsDoNotFireStateChangedUntilTheOutermostPatchCompletes()
    {
        var store = new TestStore();
        var raised = 0;
        store.StateChanged += (_, _) => raised++;

        store.Patch(_ =>
        {
            store.Count = 1;
            store.Patch(_ => store.Name = "a");

            // Still inside the outer Patch: the inner call must not have fired StateChanged yet.
            Assert.Equal(0, raised);
        });

        Assert.Equal(1, raised);
    }

    [Fact]
    public void ResetThrowsNotSupportedExceptionWhenNotOverridden()
    {
        var store = new NonResettableStore();

        Assert.Throws<NotSupportedException>(() => store.Reset());
    }

    [Fact]
    public void ResetRestoresStateWhenOverriddenByTheStore()
    {
        var store = new TestStore { Count = 5, Name = "changed" };

        store.Reset();

        Assert.Equal(0, store.Count);
        Assert.Equal(string.Empty, store.Name);
    }

    [Fact]
    public void GetterValueIsComputedLazilyOnFirstAccess()
    {
        var store = new TestStore();
        var computations = 0;
        var getter = new Getter<int>(() =>
        {
            computations++;
            return store.Count * 2;
        }, store);

        Assert.Equal(0, computations);

        _ = getter.Value;

        Assert.Equal(1, computations);
    }

    [Fact]
    public void GetterValueIsCachedUntilTheStoreRaisesStateChanged()
    {
        var store = new TestStore();
        var computations = 0;
        var getter = new Getter<int>(() =>
        {
            computations++;
            return store.Count * 2;
        }, store);

        _ = getter.Value;
        _ = getter.Value;
        _ = getter.Value;

        Assert.Equal(1, computations);
    }

    [Fact]
    public void GetterInvalidatesEvenWhenAnUnrelatedPropertyChanges()
    {
        var store = new TestStore();
        var computations = 0;
        var getter = new Getter<int>(() =>
        {
            computations++;
            return store.Count * 2;
        }, store);
        _ = getter.Value;

        // Name has nothing to do with the getter's own computation - this proves the
        // (deliberately coarse) invalidation is "any state change", not per-dependency tracking.
        store.Name = "unrelated change";
        _ = getter.Value;

        Assert.Equal(2, computations);
    }

    sealed class TestStore : StoreBase
    {
        int _count;
        string _name = string.Empty;

        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public override void Reset()
        {
            Count = 0;
            Name = string.Empty;
        }
    }

    sealed class NonResettableStore : StoreBase
    {
    }
}
