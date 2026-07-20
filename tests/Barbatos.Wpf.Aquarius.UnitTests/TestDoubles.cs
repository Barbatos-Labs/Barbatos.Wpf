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
