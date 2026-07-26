using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Samples.Shared;

/// <summary>
/// A parameterless-constructor ViewModel with sensible defaults - required for
/// <see cref="Barbatos.Wpf.Aquarius.Composition.Setup"/>'s <c>Activator.CreateInstance</c>
/// fallback (no DI container is guaranteed to be listening wherever this component ends up), and
/// what makes <see cref="InfoCardView"/> genuinely usable with zero configuration.
/// </summary>
public sealed partial class InfoCardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Shared component";

    [ObservableProperty]
    private string _description = "Defined once in Barbatos.Wpf.Samples.Shared, reused unmodified from both Barbatos.Wpf.Aquarius.Sample and Barbatos.Wpf.AquariusRouter.Sample.";
}
