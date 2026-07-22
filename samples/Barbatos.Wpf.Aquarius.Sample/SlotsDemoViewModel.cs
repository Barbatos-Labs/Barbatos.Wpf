using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// DataContext for <see cref="SlotsDemoView"/> - free-form named (<see cref="Barbatos.Wpf.Aquarius.Xaml.Slot"/>)
/// and list-scoped (<c>ItemsControl.ItemTemplate</c>) slots, fully self-contained.
/// </summary>
public sealed partial class SlotsDemoViewModel : ObservableObject
{
    /// <summary>Fetched once here, styled entirely by the consumer's ItemTemplate - see SlotsDemoView.xaml.</summary>
    public ObservableCollection<Post> Posts { get; } =
    [
        new Post("ada", "Reactivity is just ObservableObject with a nicer name.", 42),
        new Post("grace", "Teleport is basically an Adorner you don't have to write yourself.", 17),
        new Post("linus", "If you think you need KeepAlive, you probably just need a TabControl.", 99),
    ];
}

/// <summary>A single feed item - the "slot props" the FancyList-style ItemTemplate in SlotsDemoView.xaml receives.</summary>
public sealed record Post(string Username, string Body, int Likes);
