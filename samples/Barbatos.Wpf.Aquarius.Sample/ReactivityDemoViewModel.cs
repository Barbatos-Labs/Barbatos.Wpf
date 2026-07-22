using Barbatos.Wpf.Aquarius.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="ReactivityDemoView"/> - <see cref="Ref{T}"/>/<see cref="Computed{T}"/>
/// (including a writable one)/<see cref="Watch"/>, fully self-contained.
/// </summary>
public sealed partial class ReactivityDemoViewModel : ObservableObject
{
    private readonly Action<string> _log;

    /// <summary>A plain reactive value (<c>ref(0)</c>).</summary>
    public Ref<int> Count { get; } = new(0);

    /// <summary>Derived from <see cref="Count"/> (<c>computed(() =&gt; count.value * 2)</c>).</summary>
    public Computed<int> Doubled { get; }

    /// <summary>A writable computed (<c>computed({ get, set })</c>) - editing it splits back into <see cref="FirstName"/>/<see cref="LastName"/>.</summary>
    public Computed<string> FullName { get; }

    [ObservableProperty]
    private string _firstName = "Ada";

    [ObservableProperty]
    private string _lastName = "Lovelace";

    public ReactivityDemoViewModel(Action<string> log)
    {
        _log = log;

        Doubled = Computed<int>.From(() => Count.Value * 2, Count);

        FullName = Computed<string>.From(
            () => $"{FirstName} {LastName}",
            fullName =>
            {
                var parts = fullName.Split(' ', 2);
                FirstName = parts.Length > 0 ? parts[0] : "";
                LastName = parts.Length > 1 ? parts[1] : "";
            },
            this);

        Watch.On(Count, (newValue, oldValue) => _log($"Count changed {oldValue} -> {newValue}"));
    }

    [RelayCommand]
    private void Increment() => Count.Value++;
}
