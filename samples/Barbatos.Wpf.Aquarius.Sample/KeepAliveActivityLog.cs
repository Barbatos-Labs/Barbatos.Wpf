// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// The shared sink the KeepAlive demo's section ViewModels report their lifecycle hooks into -
/// registered once in <see cref="WpfProgram.CreateWpfApp"/>, exactly like
/// <see cref="ApsuActivityLog"/>.
/// </summary>
/// <remarks>
/// It exists because those section ViewModels are resolved through the container by each section
/// View's own <c>aq:Setup.ViewModel</c>, so there is nowhere to hand them a callback by hand the
/// way <see cref="MainViewModel"/> does for every other demo.
/// Depending on one injectable sink instead is what makes them ordinary DI services - and is the
/// point of the demo: a cached View's ViewModel gets its constructor dependencies like any other.
/// </remarks>
public sealed class KeepAliveActivityLog
{
    public event Action<string>? Logged;

    public void Log(string message) => Logged?.Invoke(message);
}
