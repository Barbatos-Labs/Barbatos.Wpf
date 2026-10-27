// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Devices;

/// <summary>
/// Represents information about the device screen.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>IDeviceDisplay</c>.</remarks>
public interface IDeviceDisplay
{
    /// <summary>
    /// Gets or sets if the screen should be kept on.
    /// </summary>
    /// <remarks>
    /// On Windows this is a <c>SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED)</c>
    /// request held by a background thread of its own for as long as the property is
    /// <see langword="true"/>, so it can be set and cleared from any thread. Because Windows
    /// combines every thread's request, it coexists with the keep-awake feature
    /// (<c>ConfigureKeepAwake</c>, which holds <c>ES_SYSTEM_REQUIRED</c> on the UI thread) and
    /// with any request the app makes itself: turning one off no longer cancels the other.
    /// Earlier versions applied the request on the calling thread, where it replaced - and on
    /// release cleared - whatever that thread already held.
    /// </remarks>
    bool KeepScreenOn { get; set; }

    /// <summary>
    /// Gets the main screen's display info.
    /// </summary>
    DisplayInfo MainDisplayInfo { get; }

    /// <summary>
    /// Occurs when the main display's info changes.
    /// </summary>
    event EventHandler<DisplayInfoChangedEventArgs> MainDisplayInfoChanged;
}

/// <summary>
/// Main display information event arguments.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DisplayInfoChangedEventArgs</c>.</remarks>
public class DisplayInfoChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisplayInfoChangedEventArgs"/> class.
    /// </summary>
    /// <param name="displayInfo">The display info associated to this event.</param>
    public DisplayInfoChangedEventArgs(DisplayInfo displayInfo) =>
        DisplayInfo = displayInfo;

    /// <summary>
    /// Gets the current display info for the main display associated to this event.
    /// </summary>
    public DisplayInfo DisplayInfo { get; }
}

/// <summary>
/// Represents information about the device screen.
/// </summary>
/// <remarks>This is the WPF counterpart of .NET MAUI's <c>DeviceDisplay</c>.</remarks>
public static class DeviceDisplay
{
    /// <inheritdoc cref="IDeviceDisplay.KeepScreenOn" />
    public static bool KeepScreenOn
    {
        get => Current.KeepScreenOn;
        set => Current.KeepScreenOn = value;
    }

    /// <inheritdoc cref="IDeviceDisplay.MainDisplayInfo" />
    public static DisplayInfo MainDisplayInfo => Current.MainDisplayInfo;

    /// <inheritdoc cref="IDeviceDisplay.MainDisplayInfoChanged" />
    public static event EventHandler<DisplayInfoChangedEventArgs> MainDisplayInfoChanged
    {
        add => Current.MainDisplayInfoChanged += value;
        remove => Current.MainDisplayInfoChanged -= value;
    }

    internal const float BaseLogicalDpi = 96.0f;

    static IDeviceDisplay? currentImplementation;

    /// <summary>
    /// Provides the default implementation for static usage of this API.
    /// </summary>
    public static IDeviceDisplay Current =>
        currentImplementation ??= new DeviceDisplayImplementation();

    internal static void SetCurrent(IDeviceDisplay? implementation) =>
        currentImplementation = implementation;
}

/// <summary>
/// The event-diffing shell of <see cref="IDeviceDisplay"/>, ported verbatim from .NET MAUI's
/// abstract <c>DeviceDisplayImplementationBase</c>. The platform half implements
/// <see cref="GetMainDisplayInfo"/>, <see cref="GetKeepScreenOn"/>,
/// <see cref="SetKeepScreenOn"/> and the listener start/stop methods.
/// </summary>
abstract class DeviceDisplayImplementationBase : IDeviceDisplay
{
    event EventHandler<DisplayInfoChangedEventArgs>? MainDisplayInfoChangedInternal;

    DisplayInfo _currentMetrics;

    public DisplayInfo MainDisplayInfo => GetMainDisplayInfo();

    public bool KeepScreenOn
    {
        get => GetKeepScreenOn();
        set => SetKeepScreenOn(value);
    }

    public event EventHandler<DisplayInfoChangedEventArgs> MainDisplayInfoChanged
    {
        add
        {
            if (MainDisplayInfoChangedInternal is null)
            {
                SetCurrent(MainDisplayInfo);
                StartScreenMetricsListeners();
            }
            MainDisplayInfoChangedInternal += value;
        }
        remove
        {
            var wasStopped = MainDisplayInfoChangedInternal is null;
            MainDisplayInfoChangedInternal -= value;
            if (!wasStopped && MainDisplayInfoChangedInternal is null)
                StopScreenMetricsListeners();
        }
    }

    void SetCurrent(DisplayInfo metrics) =>
        _currentMetrics = new DisplayInfo(
            metrics.Width, metrics.Height,
            metrics.Density,
            metrics.Orientation,
            metrics.Rotation,
            metrics.RefreshRate);

    protected void OnMainDisplayInfoChanged(DisplayInfoChangedEventArgs e)
    {
        if (!_currentMetrics.Equals(e.DisplayInfo))
        {
            SetCurrent(e.DisplayInfo);
            MainDisplayInfoChangedInternal?.Invoke(null, e);
        }
    }

    protected void OnMainDisplayInfoChanged()
    {
        var metrics = GetMainDisplayInfo();
        OnMainDisplayInfoChanged(new DisplayInfoChangedEventArgs(metrics));
    }

    protected abstract DisplayInfo GetMainDisplayInfo();

    protected abstract bool GetKeepScreenOn();

    protected abstract void SetKeepScreenOn(bool keepScreenOn);

    protected abstract void StartScreenMetricsListeners();

    protected abstract void StopScreenMetricsListeners();
}
