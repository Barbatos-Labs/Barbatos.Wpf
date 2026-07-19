// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// Configures the built-in <see cref="SplashWindow"/>. Returned from
/// <see cref="WpfApplication.GetSplashScreenOptions"/>.
/// </summary>
/// <remarks>
/// There is no .NET MAUI equivalent to port here: MAUI's splash screen is entirely a
/// build-time asset pipeline (<c>Microsoft.Maui.Resizetizer</c>, driven by the
/// <c>&lt;MauiSplashScreen&gt;</c> MSBuild item) with no cross-platform C# runtime logic, and it
/// does not apply at all to unpackaged Windows apps (the deployment model this library
/// targets) - the AppxManifest entry it relies on is stripped for those builds. This type
/// instead follows the same shape as MAUI's runtime-configurable Essentials/Features options
/// (a plain, directly-constructed settings object), since a splash screen has to be created
/// before the dependency injection container exists and therefore cannot go through the usual
/// <c>Configure...()</c> + config-binding pattern the other Features use.
/// </remarks>
public class SplashScreenOptions
{
    /// <summary>
    /// The minimum time the splash screen stays visible, regardless of how fast the rest of
    /// startup finishes. This avoids a jarring flash/flicker on a fast load, at the cost of the
    /// splash screen acting like a deliberate "ad slot" for at least this long. Defaults to 1.5
    /// seconds.
    /// </summary>
    public TimeSpan MinimumDisplayDuration { get; set; } = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// The app name shown on the splash screen. Defaults to
    /// <see cref="Barbatos.Wpf.ApplicationModel.AppInfo.Name"/> when not set.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// The app's own logo image, as any URI a WPF <see cref="System.Windows.Media.Imaging.BitmapImage"/>
    /// accepts - typically a pack URI such as <c>pack://application:,,,/Assets/logo.png</c>, or
    /// an absolute file path. Hidden entirely when not set.
    /// </summary>
    public string? LogoSource { get; set; }

    /// <summary>
    /// An optional short tagline/subtitle shown under the app name.
    /// </summary>
    public string? Tagline { get; set; }

    /// <summary>
    /// The splash screen's background brush. Defaults to the built-in window's own background
    /// (white) when not set.
    /// </summary>
    public System.Windows.Media.Brush? Background { get; set; }

    /// <summary>
    /// Whether to show an indeterminate progress indicator near the bottom of the splash
    /// screen. Defaults to <see langword="true"/>.
    /// </summary>
    public bool ShowProgressIndicator { get; set; } = true;

    /// <summary>
    /// Developer/sponsor logos shown in a row below the progress indicator - e.g. "Built with"
    /// or sponsor credits. Each one is clickable (opens <see cref="SplashScreenLogo.LinkUrl"/>
    /// via <c>Launcher</c>) when a URL is provided. Empty by default.
    /// </summary>
    public IList<SplashScreenLogo> SponsorLogos { get; } = new List<SplashScreenLogo>();

    /// <summary>
    /// Other things worth referencing while the app loads, e.g. other products from the same
    /// publisher. Each one is clickable (opens <see cref="SplashScreenLink.Url"/> via
    /// <c>Launcher</c>) when a URL is provided. Empty by default.
    /// </summary>
    public IList<SplashScreenLink> RelatedLinks { get; } = new List<SplashScreenLink>();
}

/// <summary>
/// A single logo shown on the splash screen. See <see cref="SplashScreenOptions.SponsorLogos"/>.
/// </summary>
public class SplashScreenLogo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SplashScreenLogo"/> class.
    /// </summary>
    /// <param name="imageSource">The logo image, as any URI a <see cref="System.Windows.Media.Imaging.BitmapImage"/> accepts.</param>
    /// <param name="tooltip">An optional tooltip shown on hover, e.g. the sponsor's name.</param>
    /// <param name="linkUrl">An optional URL opened (via <c>Launcher</c>) when the logo is clicked.</param>
    public SplashScreenLogo(string imageSource, string? tooltip = null, string? linkUrl = null)
    {
        ImageSource = imageSource ?? throw new ArgumentNullException(nameof(imageSource));
        Tooltip = tooltip;
        LinkUrl = linkUrl;
    }

    /// <summary>The logo image, as any URI a <see cref="System.Windows.Media.Imaging.BitmapImage"/> accepts.</summary>
    public string ImageSource { get; }

    /// <summary>An optional tooltip shown on hover, e.g. the sponsor's name.</summary>
    public string? Tooltip { get; }

    /// <summary>An optional URL opened (via <c>Launcher</c>) when the logo is clicked.</summary>
    public string? LinkUrl { get; }
}

/// <summary>
/// A single related-item entry shown on the splash screen. See
/// <see cref="SplashScreenOptions.RelatedLinks"/>.
/// </summary>
public class SplashScreenLink
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SplashScreenLink"/> class.
    /// </summary>
    /// <param name="title">The link's title, e.g. another product's name.</param>
    /// <param name="description">An optional short description.</param>
    /// <param name="url">An optional URL opened (via <c>Launcher</c>) when clicked.</param>
    public SplashScreenLink(string title, string? description = null, string? url = null)
    {
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description;
        Url = url;
    }

    /// <summary>The link's title, e.g. another product's name.</summary>
    public string Title { get; }

    /// <summary>An optional short description.</summary>
    public string? Description { get; }

    /// <summary>An optional URL opened (via <c>Launcher</c>) when clicked.</summary>
    public string? Url { get; }
}
