using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Records every lifecycle hook invocation, in order, for assertions shared across the
/// Lifecycle and If test suites.
/// </summary>
internal sealed partial class FakeLifecycleViewModel : ObservableObject,
    IOnBeforeCreate, IOnCreated, IOnBeforeMount, IOnMounted, IOnBeforeUpdate, IOnUpdated,
    IOnBeforeUnmount, IOnUnmounted, IOnActivated, IOnDeactivated, IOnErrorCaptured
{
    public List<string> Calls { get; } = [];

    public bool ShouldHandleError { get; set; }

    [ObservableProperty]
    private int _counter;

    public void OnBeforeCreate() => Calls.Add("OnBeforeCreate");

    public void OnCreated() => Calls.Add("OnCreated");

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

/// <summary>
/// Records every async lifecycle hook invocation - deliberately a *separate* type from
/// <see cref="FakeLifecycleViewModel"/> rather than adding the *Async interfaces to it: if
/// the same DataContext implemented both, every existing exact-sequence sync test would
/// suddenly need to account for interleaved async entries too. <see cref="OnMountedAsync"/>
/// returns <see cref="MountedGate"/>'s Task instead of completing immediately, so tests can
/// prove a pending async hook does not block anything else - complete it explicitly when a
/// test needs to observe what happens afterward.
/// </summary>
internal sealed class FakeAsyncLifecycleViewModel :
    IOnBeforeCreateAsync, IOnCreatedAsync, IOnBeforeMountAsync, IOnMountedAsync,
    IOnBeforeUnmountAsync, IOnUnmountedAsync, IOnActivatedAsync, IOnDeactivatedAsync
{
    public List<string> Calls { get; } = [];

    public TaskCompletionSource MountedGate { get; } = new();

    public Task OnBeforeCreateAsync()
    {
        Calls.Add("OnBeforeCreateAsync");
        return Task.CompletedTask;
    }

    public Task OnCreatedAsync()
    {
        Calls.Add("OnCreatedAsync");
        return Task.CompletedTask;
    }

    public Task OnBeforeMountAsync()
    {
        Calls.Add("OnBeforeMountAsync");
        return Task.CompletedTask;
    }

    public Task OnMountedAsync()
    {
        Calls.Add("OnMountedAsync:started");
        return MountedGate.Task;
    }

    public Task OnBeforeUnmountAsync()
    {
        Calls.Add("OnBeforeUnmountAsync");
        return Task.CompletedTask;
    }

    public Task OnUnmountedAsync()
    {
        Calls.Add("OnUnmountedAsync");
        return Task.CompletedTask;
    }

    public Task OnActivatedAsync()
    {
        Calls.Add("OnActivatedAsync");
        return Task.CompletedTask;
    }

    public Task OnDeactivatedAsync()
    {
        Calls.Add("OnDeactivatedAsync");
        return Task.CompletedTask;
    }
}

/// <summary>
/// A <see cref="IOnMountedAsync"/> that genuinely faults after yielding once (so
/// <see cref="Lifecycle"/> observes the fault through the deferred <c>ContinueWith</c> path,
/// not the already-completed fast path) - paired with <see cref="IOnErrorCaptured"/> on the
/// same instance to prove a faulted async hook's exception reaches it through the existing
/// <see cref="System.Windows.Application.DispatcherUnhandledException"/> route, with no
/// dedicated async error hook needed. <see cref="ErrorCapturedSignal"/> lets a test wait
/// for that cross-thread routing to actually land instead of racing a dispatcher pump
/// against a thread-pool continuation.
/// </summary>
internal sealed class FakeThrowingAsyncLifecycleViewModel : IOnMountedAsync, IOnErrorCaptured
{
    public List<string> Calls { get; } = [];

    public ManualResetEventSlim ErrorCapturedSignal { get; } = new();

    public async Task OnMountedAsync()
    {
        await Task.Yield();
        throw new InvalidOperationException("boom-async");
    }

    public bool OnErrorCaptured(Exception exception)
    {
        Calls.Add($"OnErrorCaptured:{exception.Message}");
        ErrorCapturedSignal.Set();
        return true;
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

    [ObservableProperty]
    private ExprTestOrder? _otherOrder;
}

internal sealed class ExprTestOrder
{
    public double Total { get; set; }
}

/// <summary>
/// A real <c>ContentControl</c> subclass literally named "...View" so
/// <see cref="Composition"/>'s default naming-convention resolver has something to
/// match against - the resolver inspects the element's own <see cref="Type"/>, so a plain
/// unnamed <c>ContentControl</c> can't stand in for this the way it can for every other test
/// in this suite.
/// </summary>
internal sealed class CompositionProbeView : System.Windows.Controls.ContentControl
{
}

/// <summary>Same-assembly convention target for <see cref="CompositionProbeView"/>.</summary>
internal sealed class CompositionProbeViewModel
{
}

/// <summary>A minimal <see cref="IServiceProvider"/> for proving <see cref="Composition.ServiceProvider"/> is consulted before the <see cref="Activator.CreateInstance(Type)"/> fallback.</summary>
internal sealed class FakeServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _registrations = [];

    public void Register(Type serviceType, object instance) => _registrations[serviceType] = instance;

    public object? GetService(Type serviceType) => _registrations.GetValueOrDefault(serviceType);
}
