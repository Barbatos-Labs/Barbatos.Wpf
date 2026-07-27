namespace Barbatos.Wpf.AquariusValee.Sample.Views.Signup;

/// <summary>
/// Backs the "Schema mode" half of <see cref="SignupViewModel"/> via <c>Form.FromModel</c>.
/// <see cref="CompareAttribute"/> only works this way - reflecting into a real model instance -
/// not in the ad-hoc <c>DefineField</c> mode the other half of the same tab demonstrates; see
/// <see cref="SignupViewModel"/>'s own remarks.
/// </summary>
public sealed class SignupModel
{
    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.InvalidEmail")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Validation.Required")]
    [MinLength(8, ErrorMessage = "Validation.PasswordMinLength8")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Validation.Required")]
    [Compare(nameof(Password), ErrorMessage = "Validation.PasswordsMustMatch")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";
}
