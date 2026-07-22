// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

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

/// <summary>
/// Mirrors <c>ITrayIconPlatform</c>'s real shutdown hazard: its <see cref="Dispose"/> reaches
/// back into the very <see cref="IServiceProvider"/> that is disposing it (the way tearing
/// down its native window can reenter WPF's <c>Application</c> and, from there, code that
/// resolves <c>ILifecycleEventService</c> off <see cref="IServiceProvider"/> again) - without
/// needing any actual window or WPF <c>Application</c> to prove the underlying container
/// behavior this depends on.
/// </summary>
public sealed class ReentrantDisposeService : IDisposable
{
    private readonly IServiceProvider _services;

    public ReentrantDisposeService(IServiceProvider services) => _services = services;

    public Exception? CaughtDuringDispose { get; private set; }

    public void Dispose()
    {
        try
        {
            _services.GetService<IFooService>();
        }
        catch (Exception ex)
        {
            CaughtDuringDispose = ex;
        }
    }
}
