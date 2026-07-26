using System.Collections.ObjectModel;

namespace Barbatos.Wpf.AquariusRouter.Sample.Services;

/// <summary>
/// The sample's global <c>Router.AfterEach</c> demo: records every navigation attempt - including
/// failed ones (Aborted/Cancelled/Duplicated) - into a visible, on-screen list (see
/// MainWindow.xaml's own history panel). The most direct way to actually *see* a guard's outcome
/// happen, rather than just trusting it happened.
/// </summary>
public sealed class NavigationHistoryLog
{
    private const int MaxEntries = 20;

    public ObservableCollection<string> Entries { get; } = [];

    public void Record(RouteLocation to, RouteLocation from, NavigationFailure? failure)
    {
        var outcome = failure is null ? "OK" : failure.Type.ToString();
        Entries.Insert(0, $"{DateTime.Now:HH:mm:ss}  {from.Path} → {to.Path}  [{outcome}]");

        while (Entries.Count > MaxEntries)
            Entries.RemoveAt(Entries.Count - 1);
    }
}
