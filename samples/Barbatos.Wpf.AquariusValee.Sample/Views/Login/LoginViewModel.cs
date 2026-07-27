using Barbatos.i18n.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.Login;

/// <summary>
/// Email + password, ad-hoc <c>DefineField</c> (no model class). Demonstrates
/// <c>Form.SetFieldError</c> for a server-driven failure and that the submit command's
/// <c>CanExecute</c> is gated only on <c>IsSubmitting</c>, never on validity - clicking "Log In"
/// on an empty form is exactly what's supposed to touch every field and surface its errors.
/// </summary>
public sealed partial class LoginViewModel : ObservableObject
{
    public Form LoginForm { get; } = new();
    public Field<string> Email { get; }
    public Field<string> Password { get; }
    public IAsyncRelayCommand LogInCommand { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public LoginViewModel(ICompositeStringLocalizer localizer)
    {
        Email = LoginForm.DefineField("Email", "",
            new RequiredAttribute { ErrorMessage = "Validation.Required" },
            new EmailAddressAttribute { ErrorMessage = "Validation.InvalidEmail" });
        Password = LoginForm.DefineField("Password", "",
            new RequiredAttribute { ErrorMessage = "Validation.Required" });

        LogInCommand = LoginForm.HandleSubmit(async values =>
        {
            StatusMessage = null;
            await Task.Delay(TimeSpan.FromSeconds(1));

            if ((string)values["Password"]! != "correct-password")
            {
                // A lookup key, like every ValidationAttribute's ErrorMessage above - resolved by
                // LoginView.xaml's LocalizeConverter, not eagerly here (so it reflects whichever
                // language is active when the error is actually displayed, not when it was set).
                LoginForm.SetFieldError("Password", "Login.InvalidCredentials");
                return;
            }

            StatusMessage = localizer["Login.WelcomeBack", values["Email"]!];
        });
    }
}
