using System.Windows.Controls;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// The suite's only genuinely compiled XAML fixture (everywhere else uses a plain C# class) -
/// exists specifically to prove <c>aq:Setup.Enable</c>'s real BAML-loader-driven
/// <c>DataContext</c> resolution (not just the synthetic <c>BeginInit</c>/<c>EndInit</c>
/// simulation Aquarius's own test suite otherwise relies on) composes correctly with
/// <see cref="Router"/>'s own construction/guard-checking pipeline end to end.
/// </summary>
public partial class RoutedProbeView : UserControl
{
    public RoutedProbeView() => InitializeComponent();
}
