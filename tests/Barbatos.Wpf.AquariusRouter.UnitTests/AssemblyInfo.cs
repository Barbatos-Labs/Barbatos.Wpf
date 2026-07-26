using System.Windows;
using Xunit;

// Mirrors Barbatos.Wpf.Aquarius.UnitTests's own AssemblyInfo.cs exactly, including the reasoning -
// this attribute does not carry across assemblies, and this suite has the same shape (many
// StaThread.Run-created Windows/dispatchers) that caused a real, confirmed test-host crash there
// under xUnit's default cross-class parallelism.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]
