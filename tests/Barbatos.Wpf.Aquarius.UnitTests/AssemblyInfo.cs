using System.Windows;
using Xunit;

// This suite creates real Windows/dispatchers across many dedicated STA threads
// (StaThread.Run, one per test). xUnit's default behavior runs different test classes in
// parallel on the thread pool; under that concurrency, WPF's composition/render engine
// became unreliable enough to crash the test host process outright partway through a full
// run (confirmed via `dotnet test --blame-crash`, reproduced twice, and confirmed to stop
// reproducing once test-class parallelization was disabled) - specifically once a test
// attaches a brand-new, never-before-parented visual into an already-live template while
// many other STA/Window threads are concurrently active. Every test still runs correctly in
// isolation; this is about concurrent WPF UI-thread load within one process, not test logic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page,
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page,
    // app, or any theme specific resource dictionaries)
)]