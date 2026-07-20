using System.Windows;
using Barbatos.Wpf.Reactivity;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private DockableToolDialog? _dockableDialog;

    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainViewModel();
        DataContext = viewModel;

        // Opening/closing an actual Window is a View concern, not a ViewModel one - the
        // ViewModel only ever expresses *intent* via DockTarget, it has no idea a dialog
        // Window exists. Watch.On is what reacts to that intent here.
        Watch.On(viewModel.DockTarget, (target, _) =>
        {
            if (target == "FloatingDock")
            {
                if (_dockableDialog is not null)
                    return;

                // DataContext must be set explicitly here - it does NOT travel with the
                // teleported content. The Border/TextBox moving into this dialog's host
                // still inherit DataContext from *this* dialog's own root, same as any
                // other WPF Window; without this line their {Binding DockableNotes} would
                // resolve against a null DataContext and render empty, even though
                // Teleport itself moved the exact same TextBox instance correctly.
                _dockableDialog = new DockableToolDialog { Owner = this, DataContext = viewModel };
                _dockableDialog.Closed += (_, _) =>
                {
                    _dockableDialog = null;
                    // Closed via its own [X], not via Redock - HostUnregistered already
                    // brought the panel content home by now; this just keeps DockTarget
                    // itself consistent so clicking Undock again works correctly.
                    viewModel.DockTarget.Value = "MainDock";
                };
                _dockableDialog.Show();
            }
            else
            {
                _dockableDialog?.Close();
            }
        });
    }
}
