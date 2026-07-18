// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using System.Runtime.InteropServices;
using Barbatos.Wpf.Hosting;

namespace Barbatos.Wpf.Power;

/// <summary>
/// Abstraction over the OS mechanism used to keep the computer awake.
/// </summary>
public interface IPowerManager
{
    void SetKeepAwake(bool keepAwake, bool keepDisplayOn);
}

/// <summary>
/// The default <see cref="IPowerManager"/> that uses the Win32
/// <c>SetThreadExecutionState</c> API.
/// </summary>
internal sealed class Win32PowerManager : IPowerManager
{
    const uint ES_CONTINUOUS = 0x80000000;
    const uint ES_SYSTEM_REQUIRED = 0x00000001;
    const uint ES_DISPLAY_REQUIRED = 0x00000002;

    readonly IServiceProvider _services;

    public Win32PowerManager(IServiceProvider services)
    {
        _services = services;
    }

    public void SetKeepAwake(bool keepAwake, bool keepDisplayOn)
    {
        // ES_CONTINUOUS is a per-thread state, so always apply it on the same (stable)
        // thread — the application dispatcher thread when one is available.
        var dispatcher = _services.GetOptionalApplicationDispatcher();
        if (dispatcher is not null && dispatcher.IsDispatchRequired)
            dispatcher.Dispatch(() => SetState(keepAwake, keepDisplayOn));
        else
            SetState(keepAwake, keepDisplayOn);
    }

    static void SetState(bool keepAwake, bool keepDisplayOn)
    {
        var flags = ES_CONTINUOUS;

        if (keepAwake)
        {
            flags |= ES_SYSTEM_REQUIRED;

            if (keepDisplayOn)
                flags |= ES_DISPLAY_REQUIRED;
        }

        NativeMethods.SetThreadExecutionState(flags);
    }

    static class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern uint SetThreadExecutionState(uint esFlags);
    }
}
