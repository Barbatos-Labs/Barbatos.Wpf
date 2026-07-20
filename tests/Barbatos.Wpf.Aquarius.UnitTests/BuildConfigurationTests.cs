namespace Barbatos.Wpf.Aquarius.UnitTests;

public class BuildConfigurationTests
{
    [Fact]
    public void NullAssemblyIsNotADebugBuild()
    {
        Assert.False(BuildConfiguration.IsAssemblyDebugBuild(null));
    }

    [Fact]
    public void AFrameworkAssemblyShippedAsReleaseIsNotReportedAsDebug()
    {
        // System.Private.CoreLib is a Microsoft-shipped, Release-optimized framework assembly -
        // a reliable known-false case independent of how *this* test project happens to be
        // configured.
        Assert.False(BuildConfiguration.IsAssemblyDebugBuild(typeof(object).Assembly));
    }

    [Fact]
    public void ThisTestAssemblyBuiltDebugIsReportedAsDebug()
    {
        // This project is conventionally built/run via `dotnet test -c Debug`; the SDK's
        // default Debug configuration disables JIT optimizations, which is exactly the signal
        // IsAssemblyDebugBuild reads back out. Run under `-c Release` and this (correctly)
        // flips to false.
        Assert.True(BuildConfiguration.IsAssemblyDebugBuild(typeof(BuildConfigurationTests).Assembly));
    }
}
