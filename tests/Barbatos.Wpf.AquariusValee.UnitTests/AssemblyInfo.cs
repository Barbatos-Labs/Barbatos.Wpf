using System.Windows;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

// Mirrors Barbatos.Wpf.Aquarius.UnitTests's/Barbatos.Wpf.AquariusRouter.UnitTests's own
// AssemblyInfo.cs exactly, including the reasoning - this attribute does not carry across
// assemblies, and ValeeXamlTests.cs uses the same StaThread.Run-created Windows/dispatchers
// shape that caused a real, confirmed test-host crash in the Aquarius suite under xUnit's
// default cross-class parallelism.
[assembly: Parallelization(Mode = ParallelMode.None)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]
