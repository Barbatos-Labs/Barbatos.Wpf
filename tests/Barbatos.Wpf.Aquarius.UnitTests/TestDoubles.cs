using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Records every lifecycle hook invocation, in order, for assertions shared across the
/// Lifecycle and If test suites.
/// </summary>
internal sealed partial class FakeLifecycleViewModel : ObservableObject,
    IOnBeforeMount, IOnMounted, IOnBeforeUpdate, IOnUpdated, IOnBeforeUnmount, IOnUnmounted,
    IOnActivated, IOnDeactivated, IOnErrorCaptured
{
    public List<string> Calls { get; } = [];

    public bool ShouldHandleError { get; set; }

    [ObservableProperty]
    private int _counter;

    public void OnBeforeMount() => Calls.Add("OnBeforeMount");

    public void OnMounted() => Calls.Add("OnMounted");

    public void OnBeforeUpdate() => Calls.Add("OnBeforeUpdate");

    public void OnUpdated() => Calls.Add("OnUpdated");

    public void OnBeforeUnmount() => Calls.Add("OnBeforeUnmount");

    public void OnUnmounted() => Calls.Add("OnUnmounted");

    public void OnActivated() => Calls.Add("OnActivated");

    public void OnDeactivated() => Calls.Add("OnDeactivated");

    public bool OnErrorCaptured(Exception exception)
    {
        Calls.Add($"OnErrorCaptured:{exception.Message}");
        return ShouldHandleError;
    }
}

internal enum Status
{
    Inactive,
    Active,
    Pending,
}

/// <summary>A second, unrelated enum type - only used to prove two different enum types compare as unequal rather than throwing.</summary>
internal enum Priority
{
    Low,
    High,
}

/// <summary>A small object graph covering every primitive kind (plus one level of nesting) for <see cref="Barbatos.Wpf.Xaml.Expr"/> tests.</summary>
internal sealed partial class ExprTestViewModel : ObservableObject
{
    [ObservableProperty]
    private double _a;

    [ObservableProperty]
    private double _b;

    [ObservableProperty]
    private double _c;

    [ObservableProperty]
    private bool _isActive;

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private Status _status;

    [ObservableProperty]
    private Status _otherStatus;

    [ObservableProperty]
    private Priority _priority;

    [ObservableProperty]
    private ExprTestOrder? _order;
}

internal sealed class ExprTestOrder
{
    public double Total { get; set; }
}
