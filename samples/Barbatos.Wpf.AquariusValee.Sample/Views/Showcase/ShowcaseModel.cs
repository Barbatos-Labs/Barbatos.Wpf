namespace Barbatos.Wpf.AquariusValee.Sample.Views.Showcase;

/// <summary>Which subscription tier - demonstrates <see cref="EnumDataTypeAttribute"/>.</summary>
public enum SubscriptionPlan
{
    Free,
    Pro,
    Enterprise,
}

/// <summary>
/// A wider tour of <see cref="System.ComponentModel.DataAnnotations"/> beyond what
/// <see cref="Barbatos.Wpf.AquariusValee.Sample.Views.Login.LoginViewModel"/>/
/// <see cref="Barbatos.Wpf.AquariusValee.Sample.Views.ProfileEdit.ProfileEditViewModel"/> already
/// show - <see cref="PhoneAttribute"/>, <see cref="CreditCardAttribute"/>,
/// <see cref="EnumDataTypeAttribute"/>, <see cref="AllowedValuesAttribute"/>/
/// <see cref="DeniedValuesAttribute"/> (.NET 8+), <see cref="LengthAttribute"/> (.NET 8+, exact
/// length via equal min/max), and <see cref="CustomValidationAttribute"/> (a reusable attribute
/// wrapping arbitrary custom logic - a middle ground between a full built-in attribute and a
/// schema-less <c>ValidationRule&lt;T&gt;</c> delegate). Also demonstrates <c>[UIHint]</c>/
/// <c>[DataType]</c> picking an entirely different control per field (a dropdown for
/// <see cref="Plan"/>/<see cref="Theme"/>, a multi-line box for <see cref="Notes"/>) via
/// <see cref="Xaml.FieldTemplateSelector"/> in <c>ShowcaseView.xaml</c> instead of hand-authored
/// per-field XAML, plus <c>[Display(Prompt = "...")]</c> as separate, plain guidance text shown
/// under every field regardless of which control renders it.
/// </summary>
public sealed class ShowcaseModel
{
    [Required(ErrorMessage = "Validation.Required")]
    [DeniedValues("admin", "root", "administrator", ErrorMessage = "Validation.UsernameNotAllowed")]
    [CustomValidation(typeof(ShowcaseModel), nameof(ValidateUsername))]
    [Display(Name = "Field.Username", Prompt = "Field.UsernamePrompt")]
    public string Username { get; set; } = "";

    [Phone(ErrorMessage = "Validation.InvalidPhone")]
    [Display(Name = "Field.PhoneNumber", Prompt = "Field.PhoneNumberPrompt")]
    public string? PhoneNumber { get; set; }

    [CreditCard(ErrorMessage = "Validation.InvalidCreditCard")]
    [Display(Name = "Field.CardNumber", Prompt = "Field.CardNumberPrompt")]
    public string? CardNumber { get; set; }

    // UIHint picks a genuinely different WIDGET here (a ComboBox, not just a resized TextBox) -
    // matches AllowedValues/EnumDataType's own closed set of legal values, so picking from the
    // dropdown can't produce an invalid value in the first place.
    [EnumDataType(typeof(SubscriptionPlan), ErrorMessage = "Validation.InvalidPlan")]
    [UIHint("PlanPicker")]
    [Display(Name = "Field.Plan", Prompt = "Field.PlanPrompt")]
    public string Plan { get; set; } = nameof(SubscriptionPlan.Free);

    [AllowedValues("Light", "Dark", "System", ErrorMessage = "Validation.InvalidTheme")]
    [UIHint("ThemePicker")]
    [Display(Name = "Field.Theme", Prompt = "Field.ThemePrompt")]
    public string Theme { get; set; } = "System";

    [Length(2, 2, ErrorMessage = "Validation.InvalidCountryCodeLength")]
    [Display(Name = "Field.CountryCode", Prompt = "Field.CountryCodePrompt")]
    public string CountryCode { get; set; } = "US";

    [MaxLength(280, ErrorMessage = "Validation.MaxLength280")]
    [UIHint("MultilineText")]
    [Display(Name = "Field.Notes", Prompt = "Field.NotesPrompt")]
    public string Notes { get; set; } = "";

    /// <summary>
    /// A <see cref="CustomValidationAttribute"/> target - arbitrary logic no single built-in
    /// attribute expresses, reusable as a real attribute anywhere else <see cref="Username"/>-shaped
    /// values need the same check. Returns a Barbatos.i18n lookup key, not literal text - like
    /// every other message here, resolved by LocalizeConverter in ShowcaseView.xaml.
    /// </summary>
    public static ValidationResult? ValidateUsername(string username, ValidationContext context) =>
        username.Length > 0 && char.IsDigit(username[0])
            ? new ValidationResult("Validation.UsernameStartsWithDigit", [context.MemberName!])
            : ValidationResult.Success;
}
