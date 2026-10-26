// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.LifecycleEvents;

/// <summary>
/// Chainable registration of the <see cref="WpfLifecycle"/> events on an
/// <see cref="IWpfLifecycleBuilder"/> - one method per event, named after its delegate.
/// </summary>
public static class WpfLifecycleBuilderExtensions
{
    /// <summary>Adds a <see cref="WpfLifecycle.OnStartup"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnStartup(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnStartup del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnExit"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnExit(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnExit del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnActivated"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnActivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnActivated del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnDeactivated"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnDeactivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnDeactivated del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnSessionEnding"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnSessionEnding(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnSessionEnding del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnDispatcherUnhandledException"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnDispatcherUnhandledException(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnDispatcherUnhandledException del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnWindowCreated"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnWindowCreated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowCreated del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnWindowLoaded"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnWindowLoaded(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowLoaded del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnWindowActivated"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnWindowActivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowActivated del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnWindowDeactivated"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnWindowDeactivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowDeactivated del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnWindowStateChanged"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnWindowStateChanged(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowStateChanged del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnWindowClosing"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnWindowClosing(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowClosing del) => lifecycle.OnEvent(del);
    /// <summary>Adds a <see cref="WpfLifecycle.OnWindowClosed"/> handler.</summary>
    /// <returns><paramref name="lifecycle"/>, for chaining.</returns>
    public static IWpfLifecycleBuilder OnWindowClosed(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowClosed del) => lifecycle.OnEvent(del);
}
