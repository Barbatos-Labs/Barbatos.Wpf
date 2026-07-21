using System.Windows.Media;
using Barbatos.Wpf.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="ProvideInjectDemoView"/> - no prop-drilling through every
/// DataContext, fully self-contained. <see cref="ThemeColor"/> is what MainWindow.xaml's
/// own root <c>aq:Provide.Value</c> reaches into (via a nested Binding path,
/// <c>ProvideInjectDemo.ThemeColor.Value</c>) since <c>Provide</c>/<c>Inject</c> resolve by
/// walking the visual tree, so the declaration has to live on a real ancestor of wherever
/// <c>{aq:Inject}</c> is used - it can't move into this ViewModel's own View.
/// </summary>
public sealed partial class ProvideInjectDemoViewModel : ObservableObject
{
    private static readonly Brush[] ThemePalette =
    [
        Brushes.MediumPurple, Brushes.SeaGreen, Brushes.IndianRed, Brushes.SteelBlue,
    ];

    public Ref<Brush> ThemeColor { get; } = new(ThemePalette[0]);

    [RelayCommand]
    private void CycleThemeColor()
    {
        var index = Array.IndexOf(ThemePalette, ThemeColor.Value);
        ThemeColor.Value = ThemePalette[(index + 1) % ThemePalette.Length];
    }
}
