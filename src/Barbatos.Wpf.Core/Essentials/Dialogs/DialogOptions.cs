// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Dialogs;

/// <summary>
/// Options for the dialog service feature. Can be configured from code via
/// <c>ConfigureDialogs</c> and/or from configuration files using the
/// <see cref="SectionName"/> section (file values override code values).
/// </summary>
public class DialogOptions
{
    /// <summary>
    /// The configuration section the options are bound from.
    /// </summary>
    public const string SectionName = "Barbatos:Dialogs";

    /// <summary>
    /// Whether closing a window that owns other tracked dialogs proactively closes those
    /// dialogs <em>first</em> — before WPF's own native behavior would. Defaults to
    /// <see langword="true"/>.
    /// </summary>
    /// <remarks>
    /// Plain WPF already closes a window's owned windows when it closes — but does so
    /// unconditionally, ignoring each owned window's own <c>Closing</c> veto (confirmed:
    /// setting <c>e.Cancel = true</c> in an owned window's <c>Closing</c> handler does not
    /// stop it from being force-closed once its owner closes). That is exactly the kind of
    /// silent data loss this option exists to prevent: when <see langword="true"/>, this
    /// service closes a window's owned dialogs itself, ahead of WPF's own cascade, so each one
    /// gets a real, respected chance to veto (recursively, through however many owned dialogs
    /// deep) — and if any of them do, the window's own close is cancelled too, so the whole
    /// chain stays open together rather than tearing down partway through. When
    /// <see langword="false"/>, this service does not do that proactive check, and WPF's own
    /// veto-ignoring cascade applies as usual.
    /// </remarks>
    public bool CascadeCloseOwnedDialogs { get; set; } = true;
}
