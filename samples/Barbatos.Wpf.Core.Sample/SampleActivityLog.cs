// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Core.Sample;

/// <summary>
/// The single shared sink <see cref="SampleActionLoggingInterceptor"/> and the
/// <c>OnDispatcherUnhandledException</c> lifecycle hook (see <see cref="WpfProgram.CreateWpfApp"/>)
/// both report into - so <see cref="MainViewModel"/> only ever depends on this one type instead
/// of the interceptor itself, which stays fully invisible to it. Mirrors
/// <c>Barbatos.Wpf.Aquarius.Sample</c>'s own <c>ApsuActivityLog</c>.
/// </summary>
public sealed class SampleActivityLog
{
    public event Action<string>? Logged;

    public void Log(string message) => Logged?.Invoke(message);
}
