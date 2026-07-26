// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Input;

namespace Barbatos.Wpf.AquariusRouter.Sample.Routing;

/// <summary>
/// A user's chosen replacement for one action's key binding. <see cref="Key"/>/<see cref="Modifiers"/>
/// are real enums, not strings - the standard configuration binder already parses a <c>[Flags]</c>
/// enum like <see cref="ModifierKeys"/> from a comma-separated value (e.g. <c>"Control, Shift"</c>)
/// with no custom parsing needed.
/// </summary>
/// <remarks>
/// Bound directly as <c>IOptionsMonitor&lt;Dictionary&lt;string, KeyBindingOverride&gt;&gt;</c>
/// (see <see cref="ShortcutRoutingInitializer"/>/<see cref="ShortcutSettingsStore"/>) - not wrapped
/// in an outer options class with an <c>Overrides</c> property. An earlier draft did wrap it, which
/// was a real bug: <c>ShortcutSettingsStore</c> writes each action's entry directly under
/// <see cref="ShortcutSettingsStore.SectionName"/> (e.g. <c>Barbatos:InputSystem:KeyBindingOverrides:GoToDashboard</c>),
/// but a wrapper class needs a matching <c>...:Overrides:GoToDashboard</c> - one extra nesting
/// level the file was never actually written with - so the dictionary silently bound empty every
/// time, live reload or not. Binding straight to the dictionary type removes the mismatch instead
/// of trying to keep two independently-written shapes in sync.
/// </remarks>
public sealed class KeyBindingOverride
{
    public Key Key { get; set; }

    public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;
}
