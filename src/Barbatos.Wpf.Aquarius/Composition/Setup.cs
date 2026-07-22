// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.Concurrent;
using System.Windows;

namespace Barbatos.Wpf.Aquarius.Composition;

/// <summary>
/// Resolves and assigns a <see cref="FrameworkElement.DataContext"/> from a Type, so a View
/// never needs a code-behind constructor line like <c>DataContext = new XyzViewModel(...)</c>
/// - the closest Aquarius counterpart to how a Vue single-file component's own
/// <c>&lt;script&gt;</c>/<c>&lt;script setup&gt;</c> block is simply *there*, with no separate
/// wiring step a caller has to remember.
/// </summary>
/// <remarks>
/// Two ways to opt in, from a View's own XAML root:
/// <code>
/// &lt;!-- 1. Say the exact type - always wins, works for any naming/assembly layout --&gt;
/// &lt;UserControl aq:Setup.ViewModel="{x:Type vm:SomeViewModel}" ... /&gt;
///
/// &lt;!-- 2. Just say "figure it out" - guesses "XyzView" -&gt; "XyzViewModel" by name --&gt;
/// &lt;UserControl aq:Setup.Enable="True" ... /&gt;
/// </code>
/// Resolution happens once, at <see cref="FrameworkElement.Initialized"/> - not
/// <see cref="FrameworkElement.Loaded"/> - specifically so <see cref="Lifecycle"/> (which
/// does a best-effort hook check at <c>Initialized</c> and a guaranteed one at <c>Loaded</c>)
/// always observes the resolved <c>DataContext</c> by the time its own guaranteed check
/// runs, regardless of which attached property's changed-callback happens to fire first here.
/// <para>
/// <b>Where the instance comes from</b>: <see cref="ServiceProvider"/> first if set (so
/// constructor-injected dependencies just work, exactly like resolving any other DI-registered
/// service), falling back to <see cref="Activator.CreateInstance(Type)"/> - which requires a
/// public parameterless constructor. A ViewModel with required constructor arguments and no
/// DI configured needs either a registration, a parameterless constructor, or to skip this
/// feature entirely and keep setting <c>DataContext</c> by hand.
/// </para>
/// <para>
/// <b>The naming convention</b> (see <see cref="Resolver"/>) strips a trailing <c>"View"</c>
/// from the View's own type name and appends <c>"ViewModel"</c> - <c>ReactivityDemoView</c> to
/// <c>ReactivityDemoViewModel</c>, matching this library's own sample app throughout. It checks
/// the View's own assembly first, then falls back to scanning every currently-loaded assembly
/// (<see cref="AppDomain.GetAssemblies"/>) for a same-named type, so a ViewModel living in a
/// separate assembly from its View is found too - as long as that assembly is already loaded
/// by the time this runs (one only ever reached via lazy/plugin loading may not be yet;
/// reference the type once anywhere, or use the explicit <see cref="ViewModelProperty"/>
/// override, to guarantee it's loaded). Results are cached per View type, not recomputed per
/// instance. Replace <see cref="Resolver"/> entirely to use a different convention (a
/// different suffix, a "Views"/"ViewModels" namespace swap, whatever this app's own layout
/// calls for).
/// </para>
/// </remarks>
public static class Setup
{
    /// <summary>
    /// Explicit override: the exact ViewModel <see cref="Type"/> to resolve and assign as
    /// this element's <c>DataContext</c>. Wins over <see cref="EnableProperty"/>'s naming
    /// convention if both are set on the same element.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.RegisterAttached(
            "ViewModel",
            typeof(Type),
            typeof(Setup),
            new PropertyMetadata(null, OnRelevantPropertyChanged));

    /// <summary>
    /// Turns on convention-based ViewModel resolution (see <see cref="Resolver"/>) for a
    /// <see cref="FrameworkElement"/> that doesn't need <see cref="ViewModelProperty"/>'s
    /// explicit override.
    /// </summary>
    public static readonly DependencyProperty EnableProperty =
        DependencyProperty.RegisterAttached(
            "Enable",
            typeof(bool),
            typeof(Setup),
            new PropertyMetadata(false, OnRelevantPropertyChanged));

    private static readonly DependencyProperty WiredProperty =
        DependencyProperty.RegisterAttached("Wired", typeof(bool), typeof(Setup), new PropertyMetadata(false));

    /// <summary>Sets <see cref="ViewModelProperty"/>.</summary>
    public static void SetViewModel(DependencyObject element, Type? value) => element.SetValue(ViewModelProperty, value);

    /// <summary>Gets <see cref="ViewModelProperty"/>.</summary>
    public static Type? GetViewModel(DependencyObject element) => (Type?)element.GetValue(ViewModelProperty);

    /// <summary>Sets <see cref="EnableProperty"/>.</summary>
    public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);

    /// <summary>Gets <see cref="EnableProperty"/>.</summary>
    public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);

    /// <summary>
    /// Consulted first when resolving a ViewModel instance - set this once at app startup
    /// (e.g. to a <c>Microsoft.Extensions.DependencyInjection</c> <c>IServiceProvider</c>,
    /// such as a <c>Barbatos.Wpf.Core</c> <c>WpfAppBuilder</c>'s <c>Services</c>) so
    /// constructor-injected dependencies resolve the same way any other DI-registered service
    /// would. Left <see langword="null"/> (the default), every ViewModel is instead
    /// constructed fresh via <see cref="Activator.CreateInstance(Type)"/>, which requires a
    /// public parameterless constructor.
    /// </summary>
    public static IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// The naming convention <see cref="EnableProperty"/> uses to guess a View's ViewModel
    /// type - replace it to use a different convention app-wide. Returning
    /// <see langword="null"/> means "no match" (see <see cref="ThrowOnUnresolved"/>).
    /// </summary>
    public static Func<Type, Type?> Resolver { get; set; } = DefaultResolver;

    /// <summary>
    /// When <see langword="true"/>, a <see cref="FrameworkElement"/> with
    /// <see cref="EnableProperty"/> set but no ViewModel found by <see cref="Resolver"/>
    /// throws immediately instead of silently leaving <c>DataContext</c> untouched. Default
    /// <see langword="false"/> so this feature can be adopted incrementally without every
    /// existing View that doesn't yet follow the naming convention suddenly failing at
    /// runtime - same opt-into-strict shape as <see cref="Xaml.Expr.ThrowOnUnresolvedIdentifiers"/>.
    /// </summary>
    public static bool ThrowOnUnresolved { get; set; }

    private static readonly ConcurrentDictionary<Type, Type?> ConventionCache = new();

    private static void OnRelevantPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            throw new InvalidOperationException("Setup.ViewModel/Enable can only be set on a FrameworkElement.");

        // Both properties funnel here, and XAML doesn't guarantee which of two attached
        // properties on the same element has its changed-callback run first - deferring the
        // actual read to Initialized (guaranteed to fire only once every XAML-set property on
        // this element has already been applied) means whichever callback runs first only
        // has to make sure Initialized ends up wired exactly once, never decide anything itself.
        if ((bool)element.GetValue(WiredProperty))
            return;

        element.SetValue(WiredProperty, true);
        element.Initialized += OnInitialized;
    }

    private static void OnInitialized(object? sender, EventArgs e)
    {
        var element = (FrameworkElement)sender!;
        element.Initialized -= OnInitialized;

        var explicitType = GetViewModel(element);
        var viewModelType = explicitType ?? (GetEnable(element) ? Resolver(element.GetType()) : null);

        if (viewModelType is null)
        {
            if (ThrowOnUnresolved && GetEnable(element))
                throw new InvalidOperationException($"Setup: no ViewModel type could be found by convention for '{element.GetType()}'. Set Setup.ViewModel explicitly, or provide a custom Setup.Resolver.");

            return;
        }

        element.DataContext = Resolve(viewModelType);
    }

    private static object Resolve(Type viewModelType) =>
        ServiceProvider?.GetService(viewModelType) ?? Activator.CreateInstance(viewModelType)!;

    private static Type? DefaultResolver(Type viewType) =>
        ConventionCache.GetOrAdd(viewType, static t =>
        {
            var name = t.FullName;
            if (name is null || !name.EndsWith("View", StringComparison.Ordinal))
                return null;

            var candidateName = string.Concat(name.AsSpan(0, name.Length - "View".Length), "ViewModel");

            if (t.Assembly.GetType(candidateName) is { } sameAssembly)
                return sameAssembly;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(candidateName) is { } found)
                    return found;
            }

            return null;
        });
}
