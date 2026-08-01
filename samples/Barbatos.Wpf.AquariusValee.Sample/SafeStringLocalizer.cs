using System.Windows.Markup;
using Barbatos.i18n.Wpf;

namespace Barbatos.Wpf.AquariusValee.Sample;

/// <summary>
/// <c>{app:SafeStringLocalizer Text='Some.Key'}</c> - a drop-in replacement for
/// <c>{i18n:StringLocalizer}</c> that falls back to showing <see cref="Text"/> as written when the
/// key is not registered, instead of rendering nothing at all.
/// </summary>
/// <remarks>
/// <para>
/// The static-XAML counterpart of <see cref="Barbatos.Wpf.AquariusValee.Xaml.SafeMessageConverter"/>,
/// which does the same job for runtime-bound strings (<c>ErrorMessage</c>/<c>Label</c>/<c>Prompt</c>).
/// Two different code paths need the same safety net: verified empirically that
/// <c>{i18n:StringLocalizer Text='A key nobody registered'}</c> renders as an empty string once a
/// localization set exists for the current culture but lacks that particular key - so a single
/// typo, or a heading someone wrote as ordinary prose rather than as a key, disappears from the UI
/// with no error anywhere.
/// </para>
/// <para>
/// This wrapper is a sample-level file rather than part of AquariusValee itself because it is
/// specific to Barbatos.i18n's markup extension. <see cref="Xaml.SafeMessageConverter"/>, which
/// wraps an arbitrary <see cref="System.Windows.Data.IValueConverter"/>, is the reusable,
/// localization-system-agnostic half and does live in the library.
/// </para>
/// </remarks>
[MarkupExtensionReturnType(typeof(string))]
public sealed class SafeStringLocalizer : MarkupExtension
{
    /// <summary>Creates the extension with no key yet - set <see cref="Text"/> via the property syntax.</summary>
    public SafeStringLocalizer()
    {
    }

    /// <summary>Creates the extension with a lookup key, enabling the positional syntax <c>{app:SafeStringLocalizer Some.Key}</c>.</summary>
    public SafeStringLocalizer(string text) => Text = text;

    /// <summary>The lookup key, also used verbatim as the displayed text when the key is not registered.</summary>
    public string Text { get; set; } = "";

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        try
        {
            var inner = new StringLocalizerExtension { Text = Text };

            if (inner.ProvideValue(serviceProvider) is string { Length: > 0 } localized)
                return localized;
        }
        catch (Exception)
        {
            // Fall through to the raw text below - an unresolvable key must never blank the UI.
        }

        return Text;
    }
}
