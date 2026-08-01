using System.ComponentModel.DataAnnotations;
using Barbatos.Wpf.AquariusValee.Rules;
using Barbatos.Wpf.AquariusValee.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BarbatosWpfApp;

// Picked up automatically by SignUpView.xaml's aq:Setup.Enable="True" - "SignUpView" ->
// "SignUpViewModel" by name - and resolved through the same DI container as everything else
// (see WpfProgram.cs's Setup.ServiceProvider), so a constructor dependency would just work.
//
// This is "ad-hoc mode": every field is declared here with DefineField. The other half of
// Barbatos.Wpf.AquariusValee is "schema mode" - Form.FromModel<T>(new T()), which reflects a
// plain model class's own [Required]/[Compare]/... attributes into the same Field objects.
// Cross-field attributes like [Compare] only work that way (they need a live model instance to
// read the sibling property from); see the package's README for the full comparison.
public sealed partial class SignUpViewModel : ObservableObject
{
    public Form SignUpForm { get; } = new();

    public Field<string> Email { get; }
    public Field<string> Password { get; }
    public Field<string> Phone { get; }
    public Field<int> Age { get; }

    public IAsyncRelayCommand SubmitCommand { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public SignUpViewModel()
    {
        // Plain System.ComponentModel.DataAnnotations attributes - Valee deliberately does not
        // invent its own rule vocabulary, it only adds the handful the BCL genuinely lacks
        // (Barbatos.Wpf.AquariusValee.Rules, e.g. PatternAttribute below).
        Email = SignUpForm.DefineField("Email", "",
            new RequiredAttribute { ErrorMessage = "Email is required." },
            new EmailAddressAttribute { ErrorMessage = "That doesn't look like an email address." });

        Password = SignUpForm.DefineField("Password", "",
            new RequiredAttribute { ErrorMessage = "Password is required." },
            new MinLengthAttribute(8) { ErrorMessage = "Use at least 8 characters." });

        // Optional (no [Required]): like every rule in Rules, PatternAttribute treats an empty
        // value as valid, so this only complains once something has actually been typed.
        // Valee.Mask in SignUpView.xaml guides the typing; this attribute is what decides.
        Phone = SignUpForm.DefineField("Phone", "",
            new PatternAttribute(@"\(\d{3}\) \d{3}-\d{4}") { ErrorMessage = "Enter the full number, e.g. (024) 555-0100." });

        // The RangeAttribute does double duty: it validates, and Valee copies its bounds onto
        // Field.Minimum/Maximum, which is what aqv:Valee.Numeric in the View clamps to on blur.
        Age = SignUpForm.DefineField("Age", 18,
            new RangeAttribute(13, 120) { ErrorMessage = "Age must be between 13 and 120." });

        // CanExecute is gated only on IsSubmitting, never on validity - clicking Submit on an
        // empty form is exactly what's supposed to touch every field and surface its errors
        // (and focus the first invalid control, via aqv:Valee.Form in the View).
        SubmitCommand = SignUpForm.HandleSubmit(values =>
        {
            StatusMessage = $"Signed up {values["Email"]}.";
            return Task.CompletedTask;
        });
    }
}
