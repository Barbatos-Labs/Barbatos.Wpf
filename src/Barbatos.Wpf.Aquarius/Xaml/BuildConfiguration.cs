// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using System.Reflection;

namespace Barbatos.Wpf.Aquarius.Xaml;

/// <summary>
/// Reports whether the running application was built in Debug configuration - the XAML-first
/// counterpart of Vue/Vite's <c>import.meta.env.DEV</c>.
/// </summary>
/// <remarks>
/// <code>
/// &lt;Button Content="Reset local cache (debug only)"
///         aq:Directives.Show="{x:Static aq:BuildConfiguration.IsDebug}" /&gt;
/// </code>
/// XAML has no preprocessor: <c>#if DEBUG</c>/<c>#endif</c> only apply to <c>.cs</c> files
/// (including code-behind), never to <c>.xaml</c> markup itself, since XAML is parsed by the
/// markup compiler instead of the C# compiler. The traditional WPF answer is to declare the
/// element in XAML as usual and toggle it from code-behind wrapped in <c>#if DEBUG</c>;
/// <see cref="IsDebug"/> lets the same check happen directly in markup instead, composed with
/// <see cref="Directives.ShowProperty"/>/<see cref="If"/>/<see cref="Suspense"/> like any other
/// boolean.
/// <para>
/// This deliberately does not read Barbatos.Wpf.Aquarius's own build configuration - this
/// library can ship as a Release NuGet package while the application consuming it is still
/// built Debug, or vice versa. Instead <see cref="IsDebug"/> inspects the entry assembly's
/// <see cref="DebuggableAttribute"/>, which the C# compiler stamps according to the
/// *consuming application's own* configuration (Debug disables JIT optimizations, Release
/// does not) - the same signal tooling such as Visual Studio uses to tell a Debug build from a
/// Release one after the fact. That makes it a different, stronger signal than
/// <see cref="Debugger.IsAttached"/>, which only reports whether a debugger happens to be
/// attached right now: a Debug build launched by double-clicking its .exe has
/// <see cref="Debugger.IsAttached"/> = <see langword="false"/> but still counts as a Debug
/// build here.
/// </para>
/// </remarks>
public static class BuildConfiguration
{
    /// <summary>
    /// <see langword="true"/> when the running application's entry assembly was built Debug;
    /// <see langword="false"/> for Release, or if there is no entry assembly to inspect (e.g.
    /// under the XAML designer).
    /// </summary>
    public static readonly bool IsDebug = IsAssemblyDebugBuild(Assembly.GetEntryAssembly());

    /// <summary>
    /// Whether <paramref name="assembly"/> was built with JIT optimizations disabled (the
    /// signal a Debug configuration leaves behind) - the same check <see cref="IsDebug"/> runs
    /// against the entry assembly, exposed here in case a consumer needs to check a specific
    /// assembly instead.
    /// </summary>
    public static bool IsAssemblyDebugBuild(Assembly? assembly)
    {
        var attribute = assembly?.GetCustomAttribute<DebuggableAttribute>();
        return attribute is not null && attribute.IsJITOptimizerDisabled;
    }
}
