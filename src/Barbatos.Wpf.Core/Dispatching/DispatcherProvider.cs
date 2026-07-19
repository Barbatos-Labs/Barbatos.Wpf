// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Dispatching;

/// <summary>
/// The default implementation of <see cref="IDispatcherProvider"/>.
/// </summary>
/// <remarks>
/// This is the WPF counterpart of .NET MAUI's <c>DispatcherProvider</c>. Note that, following
/// WPF conventions, a dispatcher is created on demand for the current thread when one does not
/// exist yet (see <see cref="System.Windows.Threading.Dispatcher.CurrentDispatcher"/>).
/// </remarks>
public class DispatcherProvider : IDispatcherProvider
{
    [ThreadStatic]
    static IDispatcher? s_dispatcherInstance;

    // this is mainly settable for unit testing purposes
    static IDispatcherProvider? s_currentProvider;

    /// <summary>
    /// Gets the currently set <see cref="IDispatcherProvider"/> instance.
    /// </summary>
    public static IDispatcherProvider Current =>
        s_currentProvider ??= new DispatcherProvider();

    /// <summary>
    /// Sets the current dispatcher provider.
    /// </summary>
    /// <param name="provider">The <see cref="IDispatcherProvider"/> object to set as the current dispatcher provider.</param>
    /// <returns><see langword="true"/> if the current dispatcher was actually updated, otherwise <see langword="false"/>.</returns>
    public static bool SetCurrent(IDispatcherProvider? provider)
    {
        if (s_currentProvider == provider)
            return false;

        var old = s_currentProvider;
        s_currentProvider = provider;
        return old != null;
    }

    /// <inheritdoc/>
    public IDispatcher? GetForCurrentThread() =>
        s_dispatcherInstance ??= GetForCurrentThreadImplementation();

    static Dispatcher GetForCurrentThreadImplementation() =>
        new(System.Windows.Threading.Dispatcher.CurrentDispatcher);
}
