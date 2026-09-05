// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Aquarius.Composition;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// One switchable section inside the KeepAlive demo - reports every mount/activate hook it
/// receives, and holds a scrap of state so you can see for yourself that switching away and back
/// did not throw it away.
/// </summary>
/// <remarks>
/// Reading the activity log while clicking between sections is the whole demo: the <b>first</b>
/// visit to a section logs <c>OnMounted</c> then <c>OnActivated</c>, and every visit after that
/// logs only <c>OnActivated</c>. <c>OnUnmounted</c> appears only when a section is genuinely
/// dropped - evicted past <c>Max</c>, or named in <c>Exclude</c> - and that is exactly when the
/// <see cref="Draft"/> text comes back empty.
/// </remarks>
public abstract partial class KeepAliveSectionViewModel : ObservableObject,
    IOnMounted, IOnUnmounted, IOnActivated, IOnDeactivated
{
    private readonly KeepAliveActivityLog _log;

    protected KeepAliveSectionViewModel(KeepAliveActivityLog log, string title)
    {
        _log = log;
        Title = title;
    }

    /// <summary>The section's display name - taken from the concrete type, so each section is distinguishable in the log.</summary>
    public string Title { get; }

    /// <summary>Type something here, switch sections, and come back: this is the state keep-alive exists to preserve.</summary>
    [ObservableProperty]
    private string _draft = "";

    public void OnMounted() => _log.Log($"{Title}: OnMounted (built for the first time)");

    public void OnUnmounted() => _log.Log($"{Title}: OnUnmounted - state discarded");

    public void OnActivated() => _log.Log($"{Title}: OnActivated");

    public void OnDeactivated() => _log.Log($"{Title}: OnDeactivated (still alive, just hidden)");
}

/// <summary>The <c>profile</c> section.</summary>
public sealed class ProfileSectionViewModel(KeepAliveActivityLog log) : KeepAliveSectionViewModel(log, "Profile");

/// <summary>The <c>orders</c> section.</summary>
public sealed class OrdersSectionViewModel(KeepAliveActivityLog log) : KeepAliveSectionViewModel(log, "Orders");

/// <summary>The <c>report</c> section - the one the demo's Exclude box is pre-filled to name.</summary>
public sealed class ReportSectionViewModel(KeepAliveActivityLog log) : KeepAliveSectionViewModel(log, "Report");
