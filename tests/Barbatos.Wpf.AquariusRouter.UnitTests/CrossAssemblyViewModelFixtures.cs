// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Controls;

// Deliberately in namespaces that do NOT line up, to pin what Setup's naming convention actually
// keys on. The convention builds "<View's full name minus 'View'>ViewModel" and looks that exact
// namespace-qualified name up - first in the View's own assembly, then across every loaded one.
namespace Matching.Ns
{
    public sealed class WidgetView : ContentControl;

    public sealed class WidgetViewModel
    {
        public string Origin => "matching-namespace";
    }

    /// <summary>Its ViewModel counterpart lives in <c>Other.Ns</c> - the convention will not find it.</summary>
    public sealed class GadgetView : ContentControl;

    /// <summary>Named "Presenter", so the convention has nothing to match even in the same namespace.</summary>
    public sealed class ReportView : ContentControl;

    public sealed class ReportPresenter : IOnRouteEnter
    {
        public string? SeenAccountId { get; private set; }

        public void OnRouteEnter(RouteLocation to, RouteLocation from) =>
            SeenAccountId = to.Params.TryGetValue("id", out var id) ? id as string : null;
    }
}

namespace Sealed.Library
{
    /// <summary>
    /// Stands in for a View shipped inside a component library: it carries no <c>aq:Setup</c>
    /// wiring of any kind and knows nothing about any ViewModel. Nothing here may be edited to
    /// pair it with one.
    /// </summary>
    public sealed class PristineView : ContentControl;

    /// <summary>A second pristine View, for the named-outlet case.</summary>
    public sealed class PristineSidebarView : ContentControl;

    /// <summary>
    /// And the ViewModel half, from a different (equally unmodifiable) library - no attribute, no
    /// base class, no interface tying it to <see cref="PristineView"/>.
    /// </summary>
    public sealed class UnrelatedPresenter : IOnRouteEnter
    {
        public string? SeenId { get; private set; }

        public void OnRouteEnter(RouteLocation to, RouteLocation from) =>
            SeenId = to.Params.TryGetValue("id", out var id) ? id as string : null;
    }

    public sealed class UnrelatedSidebarPresenter;
}

namespace Other.Ns
{
    public sealed class GadgetViewModel;
}
