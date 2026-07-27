namespace Barbatos.Wpf.AquariusValee.Sample.Models;

/// <summary>
/// A plain DataAnnotations model - <c>ProfileEditViewModel</c> builds its <c>Form</c> from an
/// instance of this via <c>Form.FromModel</c>, rather than defining fields by hand. This is the
/// "I already have a model class" scenario the package's schema mode targets.
/// </summary>
public sealed class ProfileEditModel
{
    [Required(ErrorMessage = "Validation.Required")]
    [Display(Name = "Display name")]
    [MaxLength(40, ErrorMessage = "Validation.DisplayNameTooLong")]
    public string DisplayName { get; set; } = "Alex Rivera";

    [Range(13, 120, ErrorMessage = "Validation.AgeRange")]
    public int Age { get; set; } = 29;

    [MaxLength(280, ErrorMessage = "Validation.MaxLength280")]
    public string Bio { get; set; } = "";

    // Deliberately string? with a null default, not "" - confirmed empirically that
    // UrlAttribute/EmailAddressAttribute (unlike RequiredAttribute or the length-based
    // attributes) treat null as valid (defers emptiness to a separate [Required], the standard
    // "this is optional" convention) but treat an empty string as a genuine format failure. A
    // non-nullable string defaulting to "" here would make this field permanently invalid until
    // the user types something, even though it's meant to be optional.
    [Url(ErrorMessage = "Validation.InvalidUrl")]
    public string? Website { get; set; }
}
