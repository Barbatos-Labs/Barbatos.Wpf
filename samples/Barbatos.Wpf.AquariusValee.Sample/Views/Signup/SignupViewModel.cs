using Barbatos.i18n.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.Signup;

/// <summary>
/// Two independent password-confirmation mini-forms, side by side, showing the two different
/// (and, for cross-field rules, non-interchangeable) ways to build a <see cref="Form"/>:
/// <list type="bullet">
/// <item><b>Schema mode</b> (<see cref="SchemaForm"/>) - <c>Form.FromModel&lt;SignupModel&gt;</c>.
/// <see cref="CompareAttribute"/> on the model's own <c>ConfirmPassword</c> property works
/// correctly here because validation reflects into the real, live model instance.</item>
/// <item><b>Ad-hoc mode</b> (<see cref="AdHocForm"/>) - plain <c>DefineField</c> calls with no
/// backing model class. <see cref="CompareAttribute"/> would not work here (there is no model
/// object for it to reflect a sibling property from) - the confirm-password field instead uses
/// <see cref="Field{T}.DependsOn{TSource}"/> plus a hand-written <see cref="ValidationRule{T}"/>
/// that reads the password field's value directly.</item>
/// </list>
/// </summary>
public sealed partial class SignupViewModel : ObservableObject
{
    public Form SchemaForm { get; }
    public Field<string> SchemaEmail { get; }
    public Field<string> SchemaPassword { get; }
    public Field<string> SchemaConfirmPassword { get; }
    public IAsyncRelayCommand SchemaSubmitCommand { get; }

    public Form AdHocForm { get; } = new();
    public Field<string> AdHocPassword { get; }
    public Field<string> AdHocConfirmPassword { get; }
    public IAsyncRelayCommand AdHocSubmitCommand { get; }

    [ObservableProperty]
    private string? _schemaStatusMessage;

    [ObservableProperty]
    private string? _adHocStatusMessage;

    public SignupViewModel(ICompositeStringLocalizer localizer)
    {
        SchemaForm = Form.FromModel(new SignupModel());
        SchemaEmail = (Field<string>)SchemaForm.Fields["Email"];
        SchemaPassword = (Field<string>)SchemaForm.Fields["Password"];
        SchemaConfirmPassword = (Field<string>)SchemaForm.Fields["ConfirmPassword"];
        // [Compare] gives the right verdict whenever ConfirmPassword is validated, but nothing
        // re-validates it when Password changes - FromModel auto-wires only IDependsOnProperties
        // attributes (such as PropertyRange). Without this, fixing the password leaves a stale
        // "must match" error under the confirmation box until that box is edited too.
        SchemaConfirmPassword.DependsOn(SchemaPassword);
        SchemaSubmitCommand = SchemaForm.HandleSubmit(values =>
        {
            SchemaStatusMessage = localizer["Signup.SchemaSuccess", values["Email"]!];
            return Task.CompletedTask;
        });

        AdHocPassword = AdHocForm.DefineField("Password", "",
            new RequiredAttribute { ErrorMessage = "Validation.Required" },
            new MinLengthAttribute(8) { ErrorMessage = "Validation.PasswordMinLength8" });
        AdHocConfirmPassword = AdHocForm.DefineField("ConfirmPassword", "",
            new RequiredAttribute { ErrorMessage = "Validation.Required" });
        // A lookup key, like every ValidationAttribute's ErrorMessage above - resolved by
        // SignupView.xaml's LocalizeConverter at display time, not eagerly here.
        AdHocConfirmPassword.AddRule(value =>
            value == AdHocPassword.Value ? null : "Signup.PasswordMismatch");
        AdHocConfirmPassword.DependsOn(AdHocPassword);

        AdHocSubmitCommand = AdHocForm.HandleSubmit(_ =>
        {
            AdHocStatusMessage = localizer["Signup.AdHocSuccess"];
            return Task.CompletedTask;
        });
    }
}
