using Barbatos.i18n.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.CustomRules;

/// <summary>
/// A single standalone <see cref="Field{T}"/> - no <see cref="Form"/>, no model class, no
/// <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> at all. Two
/// synchronous <see cref="ValidationRule{T}"/> delegates plus one asynchronous
/// <see cref="AsyncValidationRule{T}"/> (a simulated "is this username available?" API call, via
/// <see cref="UsernameAvailabilityService"/>) that exercises <see cref="FieldMeta.Pending"/>.
/// </summary>
public sealed partial class CustomRulesViewModel : ObservableObject
{
    public Field<string> Username { get; }

    public CustomRulesViewModel(UsernameAvailabilityService availabilityService, ICompositeStringLocalizer localizer)
    {
        Username = new Field<string>(
            "Username",
            "",
            value => string.IsNullOrWhiteSpace(value) ? localizer["CustomRules.Required"].Value : null,
            value => value.Contains(' ') ? localizer["CustomRules.NoSpaces"].Value : null);

        Username.AddRule(async (value, cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var available = await availabilityService.IsAvailableAsync(value, cancellationToken);
            return available ? null : localizer["CustomRules.Taken", value].Value;
        });
    }
}
