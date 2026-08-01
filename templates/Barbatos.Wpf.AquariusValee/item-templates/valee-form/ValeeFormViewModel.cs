using System.ComponentModel.DataAnnotations;
using Barbatos.Wpf.AquariusValee.Validation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ValeeTemplateNamespace;

/// <summary>
/// Picked up automatically by <c>ValeeFormView.xaml</c>'s <c>aq:Setup.Enable="True"</c> -
/// "ValeeFormView" -> "ValeeFormViewModel" by name.
/// </summary>
public sealed partial class ValeeFormViewModel : ObservableObject
{
    /// <summary>The whole form's aggregate state - bound once, on the container, as <c>aqv:Valee.Form</c>.</summary>
    public Form Form { get; }

    public Field<string> Email { get; }
    public Field<string> Password { get; }
    public Field<string> ConfirmPassword { get; }

    public IAsyncRelayCommand SubmitCommand { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ValeeFormViewModel()
    {
        // Schema mode: the rules live on the model, and validation runs against this live
        // instance - which is what lets a cross-field attribute like [Compare] read its sibling
        // property at all. The alternative is ad-hoc mode (Form.DefineField per field, no model
        // class), where [Compare] has nothing to reflect into and a hand-written rule plus
        // Field<T>.DependsOn does the same job; see the package README for the full comparison.
        Form = Form.FromModel(new ValeeFormModel());

        Email = (Field<string>)Form.Fields[nameof(ValeeFormModel.Email)];
        Password = (Field<string>)Form.Fields[nameof(ValeeFormModel.Password)];
        ConfirmPassword = (Field<string>)Form.Fields[nameof(ValeeFormModel.ConfirmPassword)];

        // [Compare] gives the right verdict whenever ConfirmPassword is validated, but nothing
        // re-validates it when the *other* field changes - only attributes implementing
        // IDependsOnProperties (e.g. Rules.PropertyRangeAttribute) get wired up automatically by
        // FromModel. Without this line, fixing the password leaves a stale "don't match" error
        // sitting under the confirm box until it is edited too.
        ConfirmPassword.DependsOn(Password);

        // CanExecute is gated only on IsSubmitting, never on validity: clicking Submit on an
        // untouched form is exactly what's supposed to touch every field, surface its errors,
        // and focus the first invalid control (that last part comes from aqv:Valee.Form).
        SubmitCommand = Form.HandleSubmit(
            onValid: values =>
            {
                // Or Form.GetModel<ValeeFormModel>() for the populated model instance itself.
                StatusMessage = $"Submitted {values[nameof(ValeeFormModel.Email)]}.";
                return Task.CompletedTask;
            },
            onInvalid: (_, errors) => StatusMessage = $"{errors.Count} field(s) still need attention.");
    }
}

/// <summary>
/// The schema <see cref="ValeeFormViewModel.Form"/> is built from. Plain
/// <c>System.ComponentModel.DataAnnotations</c> - Valee adds its own attributes (in
/// <c>Barbatos.Wpf.AquariusValee.Rules</c>) only where the BCL genuinely has a gap.
/// </summary>
public sealed class ValeeFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "That doesn't look like an email address.")]
    [Display(Name = "Email", Prompt = "you@example.com")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm the password.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords don't match.")]
    public string ConfirmPassword { get; set; } = "";
}
