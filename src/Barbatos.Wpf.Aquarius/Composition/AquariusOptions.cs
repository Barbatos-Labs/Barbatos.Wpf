// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Aquarius.Composition;

/// <summary>
/// Options for the Aquarius feature set, applied by
/// <see cref="AquariusServiceProviderExtensions.UseAquarius"/>. Configured from code via
/// <see cref="AquariusServiceCollectionExtensions.AddAquarius"/>, and from configuration only if
/// the app binds <see cref="SectionName"/> itself.
/// </summary>
/// <remarks>
/// Every setting here has a static counterpart that has always been settable directly
/// (<see cref="Setup.ThrowOnUnresolved"/>, <see cref="Xaml.Expr.ThrowOnUnresolvedIdentifiers"/>,
/// <see cref="Setup.Resolver"/>) - XAML needs an ambient value it can reach without a container,
/// and those stay the mechanism. This type is the DI-shaped way to set them: declared once
/// alongside every other service registration rather than as a handful of loose static
/// assignments after <c>Build()</c>.
/// </remarks>
public sealed class AquariusOptions
{
    /// <summary>
    /// The configuration section this repository's <c>Barbatos:</c> convention reserves for these
    /// options. Nothing binds it automatically - see
    /// <see cref="AquariusServiceCollectionExtensions.AddAquarius"/> for the one line an app adds
    /// to read it from configuration.
    /// </summary>
    public const string SectionName = "Barbatos:Aquarius";

    /// <summary>
    /// Sets <see cref="Setup.ThrowOnUnresolved"/>: whether an element with
    /// <c>aq:Setup.Enable</c> whose ViewModel cannot be found by convention fails loudly instead
    /// of silently leaving <c>DataContext</c> alone. Worth turning on once a codebase follows the
    /// convention everywhere.
    /// </summary>
    public bool ThrowOnUnresolvedViewModels { get; set; }

    /// <summary>
    /// Sets <see cref="Xaml.Expr.ThrowOnUnresolvedIdentifiers"/>: whether an <c>{aq:Expr}</c>
    /// naming something the <c>DataContext</c> does not have fails loudly instead of evaluating
    /// as unset.
    /// </summary>
    public bool ThrowOnUnresolvedExprIdentifiers { get; set; }

    /// <summary>
    /// A replacement for <see cref="Setup.Resolver"/>, the View-to-ViewModel naming convention -
    /// for an app whose layout is not "XyzView" to "XyzViewModel" (a different suffix, a
    /// Views/ViewModels namespace swap). Left <see langword="null"/> keeps the default.
    /// </summary>
    public Func<Type, Type?>? ViewModelResolver { get; set; }
}
