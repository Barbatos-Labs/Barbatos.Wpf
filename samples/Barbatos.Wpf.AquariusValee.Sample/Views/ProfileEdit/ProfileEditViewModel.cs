using Barbatos.i18n.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.ProfileEdit;

/// <summary>
/// Drives its whole <see cref="Form"/> from <see cref="ProfileEditModel"/> via
/// <c>Form.FromModel</c> - no field is declared by hand here, every rule comes from the model's
/// own DataAnnotations attributes.
/// </summary>
public sealed partial class ProfileEditViewModel : ObservableObject
{
    public Form ProfileForm { get; }
    public Field<string> DisplayName { get; }
    public Field<int> Age { get; }
    public Field<string> Bio { get; }
    public Field<string?> Website { get; }
    public IAsyncRelayCommand SaveCommand { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ProfileEditViewModel(ICompositeStringLocalizer localizer)
    {
        ProfileForm = Form.FromModel(new ProfileEditModel());
        DisplayName = (Field<string>)ProfileForm.Fields["DisplayName"];
        Age = (Field<int>)ProfileForm.Fields["Age"];
        Bio = (Field<string>)ProfileForm.Fields["Bio"];
        Website = (Field<string?>)ProfileForm.Fields["Website"];

        SaveCommand = ProfileForm.HandleSubmit(async _ =>
        {
            StatusMessage = null;
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            StatusMessage = localizer["Profile.Success", ProfileForm.GetModel<ProfileEditModel>().DisplayName];
        });
    }
}
