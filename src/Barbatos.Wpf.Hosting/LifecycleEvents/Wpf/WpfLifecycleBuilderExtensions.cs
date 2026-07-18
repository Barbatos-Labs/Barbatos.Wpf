// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Hosting Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.LifecycleEvents;

public static class WpfLifecycleBuilderExtensions
{
    public static IWpfLifecycleBuilder OnStartup(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnStartup del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnExit(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnExit del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnActivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnActivated del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnDeactivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnDeactivated del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnSessionEnding(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnSessionEnding del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnDispatcherUnhandledException(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnDispatcherUnhandledException del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnWindowCreated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowCreated del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnWindowLoaded(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowLoaded del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnWindowActivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowActivated del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnWindowDeactivated(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowDeactivated del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnWindowStateChanged(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowStateChanged del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnWindowClosing(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowClosing del) => lifecycle.OnEvent(del);
    public static IWpfLifecycleBuilder OnWindowClosed(this IWpfLifecycleBuilder lifecycle, WpfLifecycle.OnWindowClosed del) => lifecycle.OnEvent(del);
}
