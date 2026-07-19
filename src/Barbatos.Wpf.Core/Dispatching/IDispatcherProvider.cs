// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Dispatching;

/// <summary>
/// Provides a way to get the <see cref="IDispatcher"/> for the current thread.
/// </summary>
public interface IDispatcherProvider
{
    /// <summary>
    /// Gets the <see cref="IDispatcher"/> for the current thread.
    /// </summary>
    IDispatcher? GetForCurrentThread();
}
