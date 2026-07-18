// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.LifecycleEvents;

/// <summary>
/// The WPF counterpart of .NET MAUI's <c>WindowsLifecycleExtensions.AddWindows</c>.
/// </summary>
public static class WpfLifecycleExtensions
{
    public static ILifecycleBuilder AddWpf(this ILifecycleBuilder builder, Action<IWpfLifecycleBuilder> configureDelegate)
    {
        var wpf = new LifecycleBuilder(builder);

        configureDelegate?.Invoke(wpf);

        return builder;
    }

    class LifecycleBuilder : IWpfLifecycleBuilder
    {
        readonly ILifecycleBuilder _builder;

        public LifecycleBuilder(ILifecycleBuilder builder)
        {
            _builder = builder;
        }

        public void AddEvent<TDelegate>(string eventName, TDelegate action)
            where TDelegate : Delegate
        {
            _builder.AddEvent(eventName, action);
        }
    }
}
