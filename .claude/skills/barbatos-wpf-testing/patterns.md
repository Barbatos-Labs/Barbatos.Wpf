# Test patterns, copied from the suites

Every snippet is trimmed from a real test; the file named under each heading is the one to open
for the full version. Paths are relative to `tests/`.

- [A WPF object on its own STA thread](#a-wpf-object-on-its-own-sta-thread)
- [Deferred work and a real window](#deferred-work-and-a-real-window)
- [Time-based behaviour](#time-based-behaviour)
- [A running dispatcher for Core windows](#a-running-dispatcher-for-core-windows)
- [A deferred failure through DispatcherUnhandledException](#a-deferred-failure-through-dispatcherunhandledexception)
- [Modal windows](#modal-windows)
- [Real XAML strings](#real-xaml-strings)
- [Leak test trio](#leak-test-trio)
- [Leak test with a long-lived owner](#leak-test-with-a-long-lived-owner)
- [Core feature with a fake platform and configuration](#core-feature-with-a-fake-platform-and-configuration)
- [Aquarius and Router DI with restored statics](#aquarius-and-router-di-with-restored-statics)
- [Headless async with gates](#headless-async-with-gates)
- [Per-user store keys](#per-user-store-keys)
- [Validate() on descriptors](#validate-on-descriptors)

## A WPF object on its own STA thread

`Barbatos.Wpf.Aquarius.UnitTests/DirectivesShowTests.cs`:

```csharp
[Fact]
public void FalseCollapsesTheElement()
{
    StaThread.Run(() =>
    {
        var element = new Border();

        Directives.SetShow(element, false);

        Assert.Equal(Visibility.Collapsed, element.Visibility);
    });
}
```

Valee's copy of `StaThread` adds `T Run<T>(Func<T> body)` for bringing a value back to the test
thread to assert on.

## Deferred work and a real window

`Barbatos.Wpf.Aquarius.UnitTests/WatchTests.cs` - assert the deferral, pump, assert the effect:

```csharp
StaThread.Run(() =>
{
    var count = new Ref<int>(1);
    var invocations = 0;
    using var stop = Watch.On(count, (_, _) => invocations++, flush: WatchFlush.Post);

    count.Value = 2;
    count.Value = 3;

    Assert.Equal(0, invocations); // deferred, hasn't run yet

    StaThread.PumpDispatcher();

    Assert.Equal(1, invocations);
});
```

A real tree (`AquariusDependencyInjectionTests.cs`): show, pump, act, pump, assert, and always
close in `finally`.

```csharp
var window = new Window { Content = keepAlive, Width = 200, Height = 200 };
try
{
    window.Show();
    StaThread.PumpDispatcher();

    component.Is = typeof(KeepAliveProbeViewA);
    StaThread.PumpDispatcher();

    Assert.Same(provider.GetRequiredService<KeepAliveProbeViewA>(), keepAlive.ActiveView);
}
finally
{
    window.Close();
    StaThread.PumpDispatcher();
}
```

Synthetic lifecycle with no window (`LifecycleTests.cs`, `AsyncLifecycleHookTests.cs`):

```csharp
var element = new ContentControl { DataContext = vm };
Lifecycle.SetEnable(element, true);
element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, element));
// ...assert...
element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, element));
```

`Setup` resolves on initialization, so simulate the XAML loader with
`((ISupportInitialize)element).BeginInit(); ((ISupportInitialize)element).EndInit();`
(`SetupTests.cs`).

## Time-based behaviour

`PumpDispatcher()` does not let time pass. For a `Storyboard`, use a bounded loop
(`Barbatos.Wpf.Aquarius.UnitTests/TransitionTests.cs`):

```csharp
private static void PumpFor(TimeSpan duration)
{
    var deadline = DateTime.UtcNow + duration;

    while (DateTime.UtcNow < deadline)
    {
        Thread.Sleep(15);
        StaThread.PumpDispatcher();
    }
}
```

The same file has `PumpUntil(Func<bool> condition, TimeSpan timeout)` for waiting on a condition
with a deadline. Core tests with dispatcher timers (`InputChordTests.cs`,
`InputSystemScopeTests.cs`) end a `DispatcherFrame` from a one-shot timer instead - this works on
the plain xUnit thread, no STA needed:

```csharp
static void PumpFor(TimeSpan duration)
{
    var dispatcher = Barbatos.Wpf.Dispatching.Dispatcher.GetForCurrentThread()!;
    var pumpTimer = dispatcher.CreateTimer();
    pumpTimer.Interval = duration;
    pumpTimer.IsRepeating = false;

    var frame = new DispatcherFrame();
    pumpTimer.Tick += (_, _) => frame.Continue = false;
    pumpTimer.Start();

    System.Windows.Threading.Dispatcher.PushFrame(frame);
}
```

Pump comfortably past the timeout under test (`InputChordTests` pumps 300 ms for a 50 ms
`StepTimeout`), and pump - not sleep - when proving something does *not* fire, so a wrongly
firing timer actually gets the chance to run.

## A running dispatcher for Core windows

Core has no shared helpers. When `Window.Show()`/`Close()`/activation/focus must settle
synchronously, run the body as a dispatched callback inside `Dispatcher.Run()`
(`Barbatos.Wpf.Core.UnitTests/DialogServiceTests.cs`; `InputSystemScopeTests.cs` adds a window
and a `FakeHotkeyPlatform`). `SplashScreenTests.cs` uses the plain capture-and-rethrow STA thread.

```csharp
static void RunOnStaThread(Action<IDialogService> body, Action<DialogOptions>? configure = null)
{
    Exception? failure = null;

    var thread = new Thread(() =>
    {
        var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

        dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                var builder = WpfApp.CreateBuilder();
                builder.ConfigureDialogs(configure);
                body(builder.Build().Services.GetRequiredService<IDialogService>());
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                dispatcher.InvokeShutdown();
            }
        }));

        System.Windows.Threading.Dispatcher.Run();
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
        throw failure;
}
```

## A deferred failure through DispatcherUnhandledException

`Barbatos.Wpf.AquariusRouter.UnitTests/RouterViewTests.cs` (and the twin in
`Barbatos.Wpf.AquariusValee.UnitTests/ValeeXamlTests.cs`). A control that fails from `Loaded`
must defer the throw with `Dispatcher.BeginInvoke` - thrown synchronously from WPF's render pipeline it took
the whole test host down, past any `try/catch` and past `DispatcherUnhandledException`. The test
needs `TestApplication` because only its continuously running dispatcher ever executes the
deferred callback, and it waits **outside** `Invoke`, because waiting on the shared dispatcher's
own thread deadlocks.

```csharp
Exception? observed = null;
using var signal = new ManualResetEventSlim();

void OnUnhandled(object? sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
{
    observed = e.Exception;
    e.Handled = true;
    signal.Set();
}

TestApplication.Invoke(() =>
{
    Application.Current.DispatcherUnhandledException += OnUnhandled;
    var window = new Window { Content = new RouterView() };
    window.Show();
    window.Close();
});

try
{
    Assert.True(signal.Wait(TimeSpan.FromSeconds(5)), "Expected the deferred failure to reach DispatcherUnhandledException.");
    Assert.IsType<InvalidOperationException>(observed);
}
finally
{
    TestApplication.Invoke(() => Application.Current.DispatcherUnhandledException -= OnUnhandled);
}
```

## Modal windows

`ShowDialog()` blocks, so assert from inside it and close it there
(`Barbatos.Wpf.AquariusRouter.UnitTests/RouterDialogWindowTests.cs`). Hook the **inner**
element's `Loaded`: WPF broadcasts `Loaded` root-first, so at `Window.Loaded` the outlet has not
resolved anything yet.

```csharp
var outlet = new RouterView();
var dialog = new Window { Content = outlet, Width = 200, Height = 200 };
RouterScope.SetRouter(dialog, dialogRouter);

object? shown = null;
outlet.Loaded += (_, _) =>
{
    shown = outlet.Content;
    dialog.Close();
};

dialog.ShowDialog();
StaThread.PumpDispatcher();

Assert.IsType<RecordingViewB>(shown);
```

For a modeless owned window, `Show()` the owner before assigning `dialog.Owner` - WPF requires it.

## Real XAML strings

`Barbatos.Wpf.Aquarius.UnitTests/KeepAliveXamlTests.cs` (also `SlotXamlTests.cs`,
`ExprXamlTests.cs`). Use this when the behaviour is a XAML-loader fact - a content property, a
binding that resolves through the logical tree, a `ControlTemplate` with `TemplatedParent`
bindings - which building the graph in C# cannot reproduce.

```csharp
var xaml = """
    <Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:aq="http://schemas.barbatos.co/aquarius/2026/xaml"
            Width="200" Height="200">
        <aq:KeepAlive x:Name="Host" Max="2" Exclude="Border">
            <aq:Component Is="{Binding Current.Value}" />
        </aq:KeepAlive>
    </Window>
    """;

var window = (Window)XamlReader.Parse(xaml);
var keepAlive = (KeepAlive)window.FindName("Host");
window.DataContext = new SectionViewModel();
window.Show();
StaThread.PumpDispatcher();
```

A compiled `UserControl` with `x:Class` works too (`Barbatos.Wpf.AquariusRouter.UnitTests/RoutedProbeView.xaml`).

## Leak test trio

`Barbatos.Wpf.Aquarius.UnitTests/LifecycleLeakTests.cs` - baseline, regression, control. The
subject is built and dropped inside a non-inlined helper so the test's own frame never roots it.

```csharp
[Fact]
public void ATeleportIsCollectableAfterUnloading() =>
    StaThread.Run(() => GcProbe.AssertCollected(RunTeleport(loadedCount: 1), "the Teleport"));

[Fact]
public void ATeleportThatSawASecondLoadedIsStillCollectableAfterUnloading() =>
    StaThread.Run(() => GcProbe.AssertCollected(RunTeleport(loadedCount: 2), "the Teleport"));

[Fact]
public void ATeleportStillLoadedIsDeliberatelyNotCollectable() =>
    StaThread.Run(() => GcProbe.AssertAlive(RunTeleport(loadedCount: 1, unload: false), "a still-loaded Teleport"));

[MethodImpl(MethodImplOptions.NoInlining)]
private static WeakReference RunTeleport(int loadedCount, bool unload = true)
{
    var teleport = new Teleport { To = $"Host-{Guid.NewGuid()}", Content = new Border() };

    for (var i = 0; i < loadedCount; i++)
        teleport.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, teleport));

    if (unload)
        teleport.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, teleport));

    return new WeakReference(teleport);
}
```

When the live tree must stay up for the comparison (a `KeepAlive` cache), the helper returns the
still-open `Window` alongside the weak references and the test closes it after asserting
(`RunComponentSwitch` in the same file).

## Leak test with a long-lived owner

`Barbatos.Wpf.AquariusRouter.UnitTests/RouterLeakTests.cs`. Keep the app-lifetime owner rooted
across the assertion; otherwise owner, orphaned watch and subject form one unreachable island and
the test passes with the leak present.

```csharp
StaThread.Run(() =>
{
    var router = RootedRouter();
    GcProbe.AssertCollected(RunOutlet(router, loadedCount: 2), "the RouterView");
    GC.KeepAlive(router);
});
```

`RunOutlet` loads, unloads, sets `outlet.Content = null` and then
`router.Push("/other")`: the router holds the current route's View in `RouteRecord.Instances`,
and that View's logical parent is the outlet, so the outlet is legitimately reachable until the
route changes.

## Core feature with a fake platform and configuration

`Barbatos.Wpf.Core.UnitTests/RunOnStartupFeatureTests.cs`. The fake goes in before
`ConfigureXxx`, whose `TryAddSingleton` then leaves it in place.

```csharp
static IRunOnStartupService BuildService(FakeStartupRegistrar registrar, Action<RunOnStartupOptions>? configure = null)
{
    var builder = WpfApp.CreateBuilder();
    builder.Services.AddSingleton<IStartupRegistrar>(registrar);
    builder.ConfigureRunOnStartup(configure);
    var wpfApp = builder.Build();

    return wpfApp.Services.GetRequiredService<IRunOnStartupService>();
}

[Fact]
public void ConfigurationOverridesCodeValues()
{
    var registrar = new FakeStartupRegistrar();

    var builder = WpfApp.CreateBuilder();
    builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Barbatos:RunOnStartup:EntryName"] = "FromConfiguration",
    });
    builder.Services.AddSingleton<IStartupRegistrar>(registrar);
    builder.ConfigureRunOnStartup(options =>
    {
        options.Enabled = true;
        options.EntryName = "FromCode";
    });
    builder.Build();

    Assert.True(registrar.IsRegistered("FromConfiguration"));
    Assert.False(registrar.IsRegistered("FromCode"));
}
```

Pair it with `FeatureIsNotRegisteredByDefault`
(`Assert.Null(WpfApp.CreateBuilder().Build().Services.GetService<IRunOnStartupService>())`).

## Aquarius and Router DI with restored statics

`Barbatos.Wpf.Aquarius.UnitTests/AquariusDependencyInjectionTests.cs` - `UseAquarius()` writes
process-wide statics, so every test saves and restores them:

```csharp
private static void WithRestoredStatics(Action body)
{
    var provider = Setup.ServiceProvider;
    var resolver = Setup.Resolver;
    var throwOnUnresolved = Setup.ThrowOnUnresolved;
    var throwOnExpr = Expr.ThrowOnUnresolvedIdentifiers;

    try
    {
        body();
    }
    finally
    {
        Setup.ServiceProvider = provider;
        Setup.Resolver = resolver;
        Setup.ThrowOnUnresolved = throwOnUnresolved;
        Expr.ThrowOnUnresolvedIdentifiers = throwOnExpr;
    }
}
```

`AquariusRouterDependencyInjectionTests.cs` does the same for `Router.Current` inside
`StaThread.Run` (`WithRestoredCurrent`), and shows the `TryAdd` precedence check: register
`services.AddSingleton<IRouterHistory>(history)` before `AddAquariusRouter(...)`, then assert
`history.Location` after `router.Start("/")`.

## Headless async with gates

`Barbatos.Wpf.AquariusValee.UnitTests/AsyncValidationRaceTests.cs` - real async needs no STA;
hold each call open with a `TaskCompletionSource` and release it when the test chooses:

```csharp
[Fact]
public async Task TheLatestValidationCallsResultWinsOverAnOlderStillInFlightCall()
{
    var firstGate = new TaskCompletionSource();
    var callIndex = 0;

    var field = new Field<string>("Username", "a", async (value, ct) =>
    {
        if (++callIndex == 1)
        {
            await firstGate.Task;
            return "First (stale) result";
        }

        return "Second (fresh) result";
    });

    var firstValidate = field.ValidateAsync();
    var secondValidate = field.ValidateAsync();

    firstGate.SetResult();
    await Task.WhenAll(firstValidate, secondValidate);

    Assert.Contains("Second (fresh) result", field.Errors);
    Assert.DoesNotContain("First (stale) result", field.Errors);
}
```

## Per-user store keys

`Barbatos.Wpf.Core.UnitTests/PreferencesTests.cs` (same helper in `SecureStorageTests.cs`):

```csharp
[Fact]
public void StringRoundTrips()
{
    var key = UniqueKey();

    Preferences.Set(key, "hello");

    Assert.Equal("hello", Preferences.Get(key, (string?)null));

    Preferences.Remove(key);
}

static string UniqueKey() => $"Test.{Guid.NewGuid():N}";
```

`Preferences.Clear(sharedName)` appears only with a `sharedName` that is itself a `UniqueKey()`.

## Validate() on descriptors

`Barbatos.Wpf.Core.UnitTests/McpFeatureTests.cs`:

```csharp
var exception = Assert.Throws<InvalidOperationException>(() =>
    new AiProviderDescriptor { Provider = "openai" }.Validate());
Assert.Contains(nameof(AiProviderDescriptor.Key), exception.Message);

Assert.Null(Record.Exception(() =>
    new McpServerDescriptor { Name = "a", TransportKind = McpTransportKind.Stdio, Command = "dotnet" }.Validate()));
```
