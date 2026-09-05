using System.Windows;

namespace Barbatos.Wpf.AquariusRouter.Sample.Views.Dialog;

/// <summary>
/// The dialog a <b>modal route</b> renders into - the answer to "can AquariusRouter navigate to a
/// component that lives inside a child Window?".
/// </summary>
/// <remarks>
/// <b>There is no wiring here, and that is the point.</b> No router, no owner, no seeding
/// navigation, no command, no event, no window factory. <c>AppRoutes</c> declares
/// <c>Window = typeof(InspectorWindow)</c> on two routes and <c>MainWindow</c> declares
/// <c>aqr:RouterWindows.Enable="True"</c> once; between them, this window is resolved from the
/// container, owned, shown, navigated and closed entirely by the current route.
/// <para>
/// The alternative shape - a dialog with a <see cref="Router"/> and history stack of its own,
/// independent of the app's - is <c>RouterScope.Router</c> instead; see the README's "Dialog
/// windows". This sample demonstrates the modal route because it is the one that behaves like a
/// route: one history, Back closes the dialog, and the same link syntax as every other nav entry.
/// </para>
/// </remarks>
public partial class InspectorWindow : Window
{
    public InspectorWindow()
    {
        InitializeComponent();
    }
}
