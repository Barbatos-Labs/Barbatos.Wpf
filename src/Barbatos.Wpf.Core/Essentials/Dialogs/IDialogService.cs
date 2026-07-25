// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;

namespace Barbatos.Wpf.Dialogs;

/// <summary>
/// Centralizes showing and tracking child windows ("dialogs") so owner assignment, duplicate
/// prevention, and bulk-close behave consistently across the whole app, no matter where a
/// dialog is opened from. Registered by <c>ConfigureDialogs</c>.
/// </summary>
/// <remarks>
/// This has no .NET MAUI counterpart — MAUI's page-based navigation model has no equivalent
/// of WPF's multi-window/owner model. It exists to close three well-known WPF footguns:
/// <list type="bullet">
/// <item>Not setting <see cref="Window.Owner"/> explicitly lets the OS decide Z-order/activation,
/// which can put an unrelated foreground application "above" your dialog, or leave two
/// concurrently-shown dialogs fighting over which one visually owns the other.</item>
/// <item>Plain WPF already closes a window's owned dialogs when it closes, but does so
/// unconditionally — ignoring each owned dialog's own <c>Closing</c> veto, so in-progress work
/// can be force-closed and silently lost. <see cref="DialogOptions.CascadeCloseOwnedDialogs"/>
/// (default: on) closes owned dialogs itself first, ahead of WPF's own cascade, so each one
/// gets a real, respected veto — and if any refuse, the owner's own close is cancelled too.</item>
/// <item>Calling <see cref="Window.Show()"/> (not <see cref="Window.ShowDialog()"/>) from a
/// button's click handler opens a second instance on a rapid double-click, since
/// <c>Show()</c> doesn't block. <see cref="Show{TWindow}"/>/<see cref="ShowDialog{TWindow}"/>
/// key dialogs by their type (or an explicit <c>key</c> you provide) and activate the
/// existing instance instead of opening a duplicate.</item>
/// </list>
/// </remarks>
public interface IDialogService
{
    /// <summary>
    /// Gets the window this service currently considers the best default owner for a new
    /// dialog: the most recently activated window this service has seen (every dialog it has
    /// shown, plus <see cref="Application.MainWindow"/> once observed), falling back to
    /// <see cref="Application.MainWindow"/> directly, falling back to <see langword="null"/>
    /// when the application has no windows yet.
    /// </summary>
    Window? ActiveWindow { get; }

    /// <summary>
    /// Shows a modal dialog of type <typeparamref name="TWindow"/> (resolved from the
    /// dependency injection container) and blocks until it is closed.
    /// </summary>
    /// <param name="owner">The owner window, or <see langword="null"/> to use <see cref="ActiveWindow"/>.</param>
    /// <param name="key">
    /// Identifies this dialog's "slot", used to detect an already-open instance. Defaults to
    /// <c>typeof(TWindow).FullName</c>.
    /// </param>
    /// <param name="closeOthers">
    /// When <see langword="true"/>, every other dialog currently tracked by this service is
    /// closed first (gracefully — see <see cref="CloseAll"/>) before this one is shown.
    /// </param>
    /// <returns>
    /// The dialog's <c>DialogResult</c>; or <see langword="null"/> both in the normal WPF
    /// sense (closed without setting <c>DialogResult</c>) and when a dialog with the same
    /// <paramref name="key"/> was already open, in which case that instance was activated
    /// instead and nothing new was shown.
    /// </returns>
    bool? ShowDialog<TWindow>(Window? owner = null, string? key = null, bool closeOthers = false) where TWindow : Window;

    /// <summary>
    /// Shows an already-constructed window as a modal dialog and blocks until it is closed.
    /// </summary>
    /// <param name="dialog">An already-constructed window to show.</param>
    /// <param name="owner">The owner window, or <see langword="null"/> to use <see cref="ActiveWindow"/>.</param>
    /// <param name="key">
    /// Identifies this dialog's "slot", used to detect an already-open instance. Defaults to
    /// <c>dialog.GetType().FullName</c>.
    /// </param>
    /// <param name="closeOthers">
    /// When <see langword="true"/>, every other dialog currently tracked by this service is
    /// closed first (gracefully — see <see cref="CloseAll"/>) before this one is shown.
    /// </param>
    /// <returns>
    /// The dialog's <c>DialogResult</c>; or <see langword="null"/> both in the normal WPF
    /// sense (closed without setting <c>DialogResult</c>) and when a dialog with the same
    /// <paramref name="key"/> was already open, in which case that instance was activated
    /// instead and nothing new was shown.
    /// </returns>
    bool? ShowDialog(Window dialog, Window? owner = null, string? key = null, bool closeOthers = false);

    /// <summary>
    /// Shows a non-modal dialog of type <typeparamref name="TWindow"/> (resolved from the
    /// dependency injection container). If a dialog with the same <paramref name="key"/> is
    /// already tracked as open, that window is activated instead of a new one being shown —
    /// this is what makes a rapid double-click on a button that calls this method safe.
    /// </summary>
    /// <param name="owner">The owner window, or <see langword="null"/> to use <see cref="ActiveWindow"/>.</param>
    /// <param name="key">
    /// Identifies this dialog's "slot". Defaults to <c>typeof(TWindow).FullName</c>, so by
    /// default only one instance of a given window type can be open at a time; pass a more
    /// specific key (e.g. including an entity id) to allow several instances of the same
    /// window type open simultaneously, one per key.
    /// </param>
    /// <param name="closeOthers">
    /// When <see langword="true"/>, every other dialog currently tracked by this service is
    /// closed first (gracefully — see <see cref="CloseAll"/>) before this one is shown.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a new window was shown; <see langword="false"/> if an
    /// already-open dialog with the same <paramref name="key"/> was activated instead.
    /// </returns>
    bool Show<TWindow>(Window? owner = null, string? key = null, bool closeOthers = false) where TWindow : Window;

    /// <summary>
    /// Shows an already-constructed window as a non-modal dialog. If a dialog with the same
    /// <paramref name="key"/> is already tracked as open, that window is activated instead of
    /// showing this one.
    /// </summary>
    /// <param name="dialog">An already-constructed window to show.</param>
    /// <param name="owner">The owner window, or <see langword="null"/> to use <see cref="ActiveWindow"/>.</param>
    /// <param name="key">
    /// Identifies this dialog's "slot". Defaults to <c>dialog.GetType().FullName</c>, so by
    /// default only one instance of a given window type can be open at a time; pass a more
    /// specific key (e.g. including an entity id) to allow several instances of the same
    /// window type open simultaneously, one per key.
    /// </param>
    /// <param name="closeOthers">
    /// When <see langword="true"/>, every other dialog currently tracked by this service is
    /// closed first (gracefully — see <see cref="CloseAll"/>) before this one is shown.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a new window was shown; <see langword="false"/> if an
    /// already-open dialog with the same <paramref name="key"/> was activated instead.
    /// </returns>
    bool Show(Window dialog, Window? owner = null, string? key = null, bool closeOthers = false);

    /// <summary>
    /// Gets whether a dialog is currently tracked as open under the given key.
    /// </summary>
    bool IsOpen(string key);

    /// <summary>
    /// Gets the currently open dialog tracked under the given key, or <see langword="null"/>
    /// if none is open.
    /// </summary>
    Window? GetOpenDialog(string key);

    /// <summary>
    /// Gracefully closes every dialog currently tracked by this service. Each dialog still
    /// gets a chance to veto via its own <c>Closing</c> event (set <c>e.Cancel = true</c> to
    /// keep unsaved work from being silently discarded), so this can return with some dialogs
    /// still open.
    /// </summary>
    /// <returns><see langword="true"/> if every tracked dialog closed; <see langword="false"/> if one or more vetoed closing.</returns>
    bool CloseAll();

    /// <summary>
    /// Gracefully closes the dialog tracked under the given key, if one is open.
    /// </summary>
    /// <returns><see langword="true"/> if the dialog closed (or none was open under that key); <see langword="false"/> if it vetoed closing.</returns>
    bool Close(string key);
}
