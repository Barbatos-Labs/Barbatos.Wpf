// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Dispatching;

/// <summary>
/// The default service provider does not support a single service type for
/// BOTH a singleton (for the root app) AND a scoped (for the window scope).
/// This is a small wrapper so we can do the same thing.
/// This mirrors .NET MAUI's <c>ApplicationDispatcher</c>.
/// </summary>
internal class ApplicationDispatcher
{
    public IDispatcher Dispatcher { get; }

    public ApplicationDispatcher(IDispatcher dispatcher)
    {
        Dispatcher = dispatcher;
    }
}
