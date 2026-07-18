// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting.UnitTests;

// Service stubs mirroring the ones used by .NET MAUI's hosting unit tests.

public interface IFooService
{
}

public interface IBarService
{
}

public interface ICatService
{
}

public interface IFooBarService
{
}

public class FooService : IFooService
{
}

public class FooService2 : IFooService
{
}

public class BarService : IBarService
{
}

public class CatService : ICatService
{
}

public class BadFooService : IFooService
{
    private BadFooService()
    {
    }
}

public class FooBarService : IFooBarService
{
    public FooBarService(IFooService foo, IBarService bar)
    {
        Foo = foo;
        Bar = bar;
    }

    public IFooService Foo { get; }

    public IBarService Bar { get; }
}

public class FooDualConstructor : IFooBarService
{
    public FooDualConstructor(IFooService foo)
    {
        Foo = foo;
    }

    public FooDualConstructor(IBarService bar)
    {
        Bar = bar;
    }

    public IFooService? Foo { get; }

    public IBarService? Bar { get; }
}

public class FooDefaultValueConstructor : IFooBarService
{
    public FooDefaultValueConstructor(IBarService? bar = null)
    {
        Bar = bar;
    }

    public IBarService? Bar { get; }
}

public class FooDefaultSystemValueConstructor : IFooBarService
{
    public FooDefaultSystemValueConstructor(string text = "Default Value")
    {
        Text = text;
    }

    public string Text { get; }
}

public class FooEnumerableService : IFooBarService
{
    public FooEnumerableService(IEnumerable<IFooService> foos)
    {
        Foos = foos;
    }

    public IEnumerable<IFooService> Foos { get; }
}

public sealed class DisposableService : IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose() => IsDisposed = true;
}
