using Barbatos.i18n.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.CustomRules;

/// <summary>
/// A single standalone <see cref="Field{T}"/> - no <see cref="Form"/>, no model class, no
/// <see cref="System.ComponentModel.DataAnnotations.ValidationAttribute"/> at all. Two
/// synchronous <see cref="ValidationRule{T}"/> delegates plus one asynchronous
/// <see cref="AsyncValidationRule{T}"/> (a simulated "is this username available?" API call, via
/// <see cref="UsernameAvailabilityService"/>) that exercises <see cref="FieldMeta.Pending"/>.
/// Also demonstrates a validation BOUND driven by other fields, not a fixed constant -
/// <see cref="Price"/>'s valid range is <see cref="MinPrice"/>/<see cref="MaxPrice"/>'s own
/// current values. <c>[Range(13, 120)]</c>-style attributes cannot express this at all (C#
/// attribute constructor arguments must be compile-time constants - there is no
/// <c>[Range(MinPrice, MaxPrice)]</c> referencing another property), so this is exactly what
/// <see cref="Field{T}.DependsOn{TSource}"/> plus a hand-written rule is for - the same mechanism
/// Signup's ad-hoc confirm-password panel uses, just with two dependencies and a range check
/// instead of one dependency and an equality check.
/// </summary>
public sealed partial class CustomRulesViewModel : ObservableObject
{
    public Field<string> Username { get; }

    public Field<int> MinPrice { get; }
    public Field<int> MaxPrice { get; }
    public Field<int> Price { get; }

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

        MinPrice = new Field<int>("MinPrice", 10);
        MaxPrice = new Field<int>("MaxPrice", 100);
        Price = new Field<int>("Price", 50,
            value => value < MinPrice.Value ? localizer["CustomRules.PriceTooLow", MinPrice.Value].Value : null,
            value => value > MaxPrice.Value ? localizer["CustomRules.PriceTooHigh", MaxPrice.Value].Value : null);
        Price.DependsOn(MinPrice);
        Price.DependsOn(MaxPrice);
    }

    /// <summary>
    /// Re-runs validation on whichever of this tab's fields have already been validated, so a
    /// message that is currently on screen switches to the newly selected language.
    /// </summary>
    /// <remarks>
    /// Needed because every rule here resolves its text <i>eagerly</i> through
    /// <see cref="ICompositeStringLocalizer"/> rather than returning a lookup key for a converter
    /// to resolve at display time - unavoidable for these particular messages, since they
    /// interpolate a runtime value ("<c>{0}</c> is already taken", "must be at least
    /// <c>{0}</c>") that a plain <c>{Binding ..., Converter=...}</c> has no way to supply. Once
    /// resolved, the string is just text and knows nothing about a later culture switch. The other
    /// tabs store lookup keys instead and get this for free when their bindings re-read.
    /// <para>
    /// This is the standalone-<see cref="Field{T}"/> counterpart of
    /// <see cref="Form.RevalidateValidatedAsync"/> - the same <c>Meta.Validated</c> gate, done by
    /// hand because this tab deliberately has no <see cref="Form"/> at all.
    /// </para>
    /// </remarks>
    public Task RefreshLocalizedMessagesAsync() =>
        Task.WhenAll(
            new IField[] { Username, MinPrice, MaxPrice, Price }
                .Where(field => field.Meta.Validated)
                .Select(field => field.ValidateAsync()));
}
