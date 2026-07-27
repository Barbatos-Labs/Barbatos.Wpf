using Barbatos.i18n.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusValee.Sample.Views.Showcase;

/// <summary>Drives its <see cref="Form"/> entirely from <see cref="ShowcaseModel"/>; the View renders it via <see cref="Xaml.FieldTemplateSelector"/> instead of hand-authored per-field XAML.</summary>
public sealed partial class ShowcaseViewModel : ObservableObject
{
    public Form ShowcaseForm { get; }
    public IAsyncRelayCommand SubmitCommand { get; }

    [ObservableProperty]
    private string? _statusMessage;

    public ShowcaseViewModel(ICompositeStringLocalizer localizer)
    {
        ShowcaseForm = Form.FromModel(new ShowcaseModel());

        SubmitCommand = ShowcaseForm.HandleSubmit(_ =>
        {
            StatusMessage = localizer["Showcase.Success"];
            return Task.CompletedTask;
        });
    }
}
