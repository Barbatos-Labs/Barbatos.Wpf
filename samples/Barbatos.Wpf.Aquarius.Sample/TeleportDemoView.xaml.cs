using System.Windows;
using System.Windows.Controls;
using Barbatos.Wpf.Aquarius.Reactivity;

namespace Barbatos.Wpf.Aquarius.Sample;

public partial class TeleportDemoView : UserControl
{
    private DockableToolDialog? _dockableDialog;
    private bool _wired;

    public TeleportDemoView()
    {
        InitializeComponent();

        // DataContextChanged (rather than Loaded) is what reliably fires exactly once
        // DataContext actually resolves to TeleportDemoViewModel - a TabControl realizes
        // every tab's content upfront (see KeepAliveTests in the library's own test
        // project), so Loaded can fire for this tab's content before it is ever selected,
        // and WPF can raise Loaded again later without an intervening Unloaded when the
        // tab finally is selected. Unsubscribing after a single Loaded (as this used to)
        // risked giving up before DataContext was actually ready.
        DataContextChanged += OnDataContextChanged;
        TryWireDockTargetWatch();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) => TryWireDockTargetWatch();

    private void TryWireDockTargetWatch()
    {
        if (_wired || DataContext is not TeleportDemoViewModel viewModel)
            return;

        _wired = true;
        DataContextChanged -= OnDataContextChanged;

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
                // Window.GetWindow(this) is resolved fresh here (not captured earlier) so
                // it reflects this control's actual owning Window at the moment it's needed.
                _dockableDialog = new DockableToolDialog { Owner = Window.GetWindow(this), DataContext = viewModel };
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
