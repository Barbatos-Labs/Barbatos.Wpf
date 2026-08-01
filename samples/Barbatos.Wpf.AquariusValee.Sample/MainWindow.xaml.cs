using System.ComponentModel;
using System.Windows;
using Barbatos.i18n.DependencyInjection;
using Barbatos.Wpf.AquariusValee.Sample.Views.CustomRules;
using Barbatos.Wpf.AquariusValee.Sample.Views.Login;
using Barbatos.Wpf.AquariusValee.Sample.Views.ProfileEdit;
using Barbatos.Wpf.AquariusValee.Sample.Views.Showcase;
using Barbatos.Wpf.AquariusValee.Sample.Views.Signup;

namespace Barbatos.Wpf.AquariusValee.Sample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly ICompositeStringLocalizer _localizer;

    public MainWindow(MainViewModel viewModel, ICompositeStringLocalizer localizer)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _localizer = localizer;
        DataContext = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Plain {i18n:StringLocalizer Text='Key'} bindings (no BindArg) only evaluate once, so
        // Title/TabItem.Header (set directly in MainWindow.xaml) and every tab's View content
        // are all refreshed by hand here to pick up the newly selected language - see
        // MainWindow.xaml's own remarks. Tab Views are rebuilt fresh against their existing
        // (state-preserving) ViewModels rather than mutated in place.
        if (e.PropertyName == nameof(MainViewModel.SelectedCulture))
        {
            Title = _localizer["MainWindow.Title"];
            LoginTabItem.Header = _localizer["MainWindow.TabLogin"];
            SignupTabItem.Header = _localizer["MainWindow.TabSignup"];
            ProfileTabItem.Header = _localizer["MainWindow.TabProfile"];
            CustomRulesTabItem.Header = _localizer["MainWindow.TabCustomRules"];
            ShowcaseTabItem.Header = _localizer["MainWindow.TabShowcase"];

            LoginTabItem.Content = new LoginView { DataContext = _viewModel.LoginDemo };
            SignupTabItem.Content = new SignupView { DataContext = _viewModel.SignupDemo };
            ProfileTabItem.Content = new ProfileEditView { DataContext = _viewModel.ProfileEditDemo };
            CustomRulesTabItem.Content = new CustomRulesView { DataContext = _viewModel.CustomRulesDemo };
            ShowcaseTabItem.Content = new ShowcaseView { DataContext = _viewModel.ShowcaseDemo };

            // Every other tab's ErrorMessage is a Barbatos.i18n lookup key resolved by
            // SafeMessageConverter at display time, so rebuilding the View above already refreshes
            // it to the new language for free. The Custom rules tab is the exception - its rule
            // delegates resolve real localized text eagerly (needed for messages that interpolate
            // a runtime value, which a converter has no way to supply) - so it re-validates
            // whatever is already on screen. The ViewModel owns that list rather than this handler
            // naming fields one by one, so adding a field there can't silently miss this path.
            _ = _viewModel.CustomRulesDemo.RefreshLocalizedMessagesAsync();
        }
    }
}
