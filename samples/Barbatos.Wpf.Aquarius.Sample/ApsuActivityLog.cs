// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// The single shared sink every Apsu interceptor in this sample reports to - registered once in
/// <see cref="WpfProgram.CreateWpfApp"/> and handed to each interceptor's own constructor, so a
/// consumer (<see cref="MainViewModel"/>/<see cref="ApsuInteropDemoViewModel"/>) only ever
/// depends on this one type, never on the interceptors themselves. This is what keeps
/// interceptors invisible to the ViewModel layer, matching how a Pinia plugin is registered once
/// with <c>pinia.use(plugin)</c> and never imported into a component - if a plugin needs to
/// surface something to the UI, it writes into its own shared, injectable state instead of the
/// component reaching into the plugin directly.
/// </summary>
public sealed class ApsuActivityLog
{
    public event Action<string>? Logged;

    public void Log(string message) => Logged?.Invoke(message);
}
