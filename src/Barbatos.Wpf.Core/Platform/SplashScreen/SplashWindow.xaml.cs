// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Barbatos.Wpf.ApplicationModel;
using Image = System.Windows.Controls.Image;

namespace Barbatos.Wpf.Hosting;

/// <summary>
/// The built-in splash screen window, populated from a <see cref="SplashScreenOptions"/>
/// instance. Shown automatically by <see cref="WpfApplication"/> when
/// <see cref="WpfApplication.GetSplashScreenOptions"/> is overridden; override
/// <see cref="WpfApplication.CreateSplashScreen"/> instead for full control over the UI.
/// </summary>
/// <remarks>
/// The named elements (<see cref="LogoImage"/>, <see cref="AppNameText"/>, ...) are public so a
/// subclass - or a test - can inspect or further restyle them after construction.
/// </remarks>
public partial class SplashWindow : Window, ISplashScreen
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SplashWindow"/> class, populated from the
    /// given <paramref name="options"/>.
    /// </summary>
    public SplashWindow(SplashScreenOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        InitializeComponent();

        MinimumDisplayDuration = options.MinimumDisplayDuration;

        if (options.Background is { } background)
            Background = background;

        AppNameText.Text = options.AppName ?? AppInfo.Name;

        SetOptionalText(TaglineText, options.Tagline);
        SetOptionalImage(LogoImage, options.LogoSource);

        ProgressIndicator.Visibility = options.ShowProgressIndicator ? Visibility.Visible : Visibility.Collapsed;

        PopulateSponsorLogos(options.SponsorLogos);
        PopulateRelatedLinks(options.RelatedLinks);
    }

    /// <inheritdoc />
    public TimeSpan MinimumDisplayDuration { get; }

    void PopulateSponsorLogos(ICollection<SplashScreenLogo> logos)
    {
        if (logos.Count == 0)
        {
            SponsorLogosControl.Visibility = Visibility.Collapsed;
            return;
        }

        foreach (var logo in logos)
        {
            var image = new Image
            {
                Source = TryLoadImage(logo.ImageSource),
                Height = 32,
                Margin = new Thickness(6, 0, 6, 0),
                Stretch = Stretch.Uniform,
                ToolTip = logo.Tooltip,
                Cursor = logo.LinkUrl is not null ? Cursors.Hand : Cursors.Arrow,
            };

            if (logo.LinkUrl is { } url)
                image.MouseLeftButtonUp += (_, _) => _ = Launcher.TryOpenAsync(url);

            SponsorLogosControl.Items.Add(image);
        }
    }

    void PopulateRelatedLinks(ICollection<SplashScreenLink> links)
    {
        if (links.Count == 0)
        {
            RelatedLinksSeparator.Visibility = Visibility.Collapsed;
            RelatedLinksControl.Visibility = Visibility.Collapsed;
            return;
        }

        foreach (var link in links)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };

            var title = new TextBlock
            {
                Text = link.Title,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = link.Url is not null ? Cursors.Hand : Cursors.Arrow,
            };
            if (link.Url is { } url)
            {
                title.TextDecorations = TextDecorations.Underline;
                title.MouseLeftButtonUp += (_, _) => _ = Launcher.TryOpenAsync(url);
            }
            panel.Children.Add(title);

            if (!string.IsNullOrEmpty(link.Description))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = link.Description,
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                });
            }

            RelatedLinksControl.Items.Add(panel);
        }
    }

    static void SetOptionalText(TextBlock textBlock, string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            textBlock.Visibility = Visibility.Collapsed;
            return;
        }

        textBlock.Text = text;
        textBlock.Visibility = Visibility.Visible;
    }

    static void SetOptionalImage(Image image, string? source)
    {
        var bitmap = string.IsNullOrEmpty(source) ? null : TryLoadImage(source);
        if (bitmap is null)
        {
            image.Visibility = Visibility.Collapsed;
            return;
        }

        image.Source = bitmap;
        image.Visibility = Visibility.Visible;
    }

    static BitmapImage? TryLoadImage(string source)
    {
        try
        {
            return new BitmapImage(new Uri(source, UriKind.RelativeOrAbsolute));
        }
        catch (Exception ex) when (ex is UriFormatException or NotSupportedException or IOException)
        {
            return null;
        }
    }
}
