// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Core.UnitTests;

public class SplashScreenTests
{
    [Fact]
    public void OptionsDefaultToOneAndAHalfSeconds()
    {
        Assert.Equal(TimeSpan.FromSeconds(1.5), new SplashScreenOptions().MinimumDisplayDuration);
    }

    [Fact]
    public void OptionsShowProgressIndicatorDefaultsToTrue()
    {
        Assert.True(new SplashScreenOptions().ShowProgressIndicator);
    }

    [Fact]
    public void OptionsSponsorLogosAndRelatedLinksAreEmptyByDefault()
    {
        var options = new SplashScreenOptions();

        Assert.Empty(options.SponsorLogos);
        Assert.Empty(options.RelatedLinks);
    }

    [Fact]
    public void SplashScreenLogoThrowsOnNullImageSource()
    {
        Assert.Throws<ArgumentNullException>(() => new SplashScreenLogo(null!));
    }

    [Fact]
    public void SplashScreenLinkThrowsOnNullTitle()
    {
        Assert.Throws<ArgumentNullException>(() => new SplashScreenLink(null!));
    }

    [Fact]
    public void SplashWindowThrowsOnNullOptions()
    {
        RunOnStaThread(() => Assert.Throws<ArgumentNullException>(() => new SplashWindow(null!)));
    }

    [Fact]
    public void SplashWindowExposesMinimumDisplayDurationFromOptions()
    {
        RunOnStaThread(() =>
        {
            var options = new SplashScreenOptions { MinimumDisplayDuration = TimeSpan.FromSeconds(3) };

            var window = new SplashWindow(options);

            Assert.Equal(TimeSpan.FromSeconds(3), ((ISplashScreen)window).MinimumDisplayDuration);
        });
    }

    [Fact]
    public void SplashWindowFallsBackToAppInfoNameWhenAppNameIsNotSet()
    {
        RunOnStaThread(() =>
        {
            var window = new SplashWindow(new SplashScreenOptions());

            Assert.Equal(Barbatos.Wpf.ApplicationModel.AppInfo.Name, window.AppNameText.Text);
        });
    }

    [Fact]
    public void SplashWindowUsesTheGivenAppNameWhenSet()
    {
        RunOnStaThread(() =>
        {
            var window = new SplashWindow(new SplashScreenOptions { AppName = "Custom App Name" });

            Assert.Equal("Custom App Name", window.AppNameText.Text);
        });
    }

    [Fact]
    public void SplashWindowHidesTaglineWhenNotSet()
    {
        RunOnStaThread(() =>
        {
            var window = new SplashWindow(new SplashScreenOptions());

            Assert.Equal(System.Windows.Visibility.Collapsed, window.TaglineText.Visibility);
        });
    }

    [Fact]
    public void SplashWindowShowsTaglineWhenSet()
    {
        RunOnStaThread(() =>
        {
            var window = new SplashWindow(new SplashScreenOptions { Tagline = "A short tagline" });

            Assert.Equal(System.Windows.Visibility.Visible, window.TaglineText.Visibility);
            Assert.Equal("A short tagline", window.TaglineText.Text);
        });
    }

    [Fact]
    public void SplashWindowHidesTheProgressIndicatorWhenDisabled()
    {
        RunOnStaThread(() =>
        {
            var window = new SplashWindow(new SplashScreenOptions { ShowProgressIndicator = false });

            Assert.Equal(System.Windows.Visibility.Collapsed, window.ProgressIndicator.Visibility);
        });
    }

    [Fact]
    public void SplashWindowHidesSponsorLogosRowWhenEmpty()
    {
        RunOnStaThread(() =>
        {
            var window = new SplashWindow(new SplashScreenOptions());

            Assert.Equal(System.Windows.Visibility.Collapsed, window.SponsorLogosControl.Visibility);
            Assert.Empty(window.SponsorLogosControl.Items);
        });
    }

    [Fact]
    public void SplashWindowPopulatesOneItemPerSponsorLogo()
    {
        RunOnStaThread(() =>
        {
            var options = new SplashScreenOptions();
            options.SponsorLogos.Add(new SplashScreenLogo("pack://application:,,,/does-not-exist-1.png", "Sponsor 1"));
            options.SponsorLogos.Add(new SplashScreenLogo("pack://application:,,,/does-not-exist-2.png", "Sponsor 2", "https://example.com"));

            var window = new SplashWindow(options);

            Assert.Equal(System.Windows.Visibility.Visible, window.SponsorLogosControl.Visibility);
            Assert.Equal(2, window.SponsorLogosControl.Items.Count);
        });
    }

    [Fact]
    public void SplashWindowHidesRelatedLinksWhenEmpty()
    {
        RunOnStaThread(() =>
        {
            var window = new SplashWindow(new SplashScreenOptions());

            Assert.Equal(System.Windows.Visibility.Collapsed, window.RelatedLinksControl.Visibility);
            Assert.Equal(System.Windows.Visibility.Collapsed, window.RelatedLinksSeparator.Visibility);
        });
    }

    [Fact]
    public void SplashWindowPopulatesOneItemPerRelatedLink()
    {
        RunOnStaThread(() =>
        {
            var options = new SplashScreenOptions();
            options.RelatedLinks.Add(new SplashScreenLink("Other product", "A short description", "https://example.com"));

            var window = new SplashWindow(options);

            Assert.Equal(System.Windows.Visibility.Visible, window.RelatedLinksControl.Visibility);
            Assert.Single(window.RelatedLinksControl.Items);
        });
    }

    /// <summary>
    /// Runs <paramref name="body"/> on a dedicated STA thread, since WPF windows can only be
    /// created from an STA thread. Mirrors <c>DialogServiceTests.RunOnStaThread</c>.
    /// </summary>
    static void RunOnStaThread(Action body)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                body();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw failure;
    }
}
