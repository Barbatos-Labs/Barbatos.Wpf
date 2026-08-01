using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Covers <see cref="RouteTo"/> both as a plain object (its parsing rules) and through a real
/// <see cref="XamlReader"/> parse - the latter is what actually proves the <c>{aqr:RouteTo ...}</c>
/// syntax in its own docs works, including that the assembly's <c>XmlnsDefinition</c> really
/// exposes it under the <c>aqr</c> namespace.
/// </summary>
public class RouteToTests
{
    private static object ParseXaml(string innerXaml)
    {
        var xaml =
            $"""
             <Button xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                     xmlns:aqr="http://schemas.barbatos.co/aquariusrouter/2026/xaml"
                     {innerXaml} />
             """;

        return XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)));
    }

    [Fact]
    public void ProvidesANamedTarget()
    {
        var raw = (RouteLocationRaw)new RouteTo { Name = "user" }.ProvideValue(null!);

        Assert.Equal("user", raw.Name);
        Assert.Null(raw.Path);
        Assert.Null(raw.Params);
    }

    [Fact]
    public void ProvidesALiteralPathPositionally()
    {
        var raw = (RouteLocationRaw)new RouteTo("/about").ProvideValue(null!);

        Assert.Equal("/about", raw.Path);
        Assert.Null(raw.Name);
    }

    [Fact]
    public void ParsesSeveralSemicolonSeparatedParams()
    {
        var raw = (RouteLocationRaw)new RouteTo { Name = "user", Params = "id=42;tab=billing" }.ProvideValue(null!);

        Assert.Equal("42", raw.Params!["id"]);
        Assert.Equal("billing", raw.Params["tab"]);
    }

    [Fact]
    public void ACommaSeparatedParamValueBecomesTheListARepeatableParamExpects()
    {
        var raw = (RouteLocationRaw)new RouteTo { Name = "files", Params = "segments=docs,2026,report" }.ProvideValue(null!);

        Assert.Equal(["docs", "2026", "report"], Assert.IsAssignableFrom<IReadOnlyList<string>>(raw.Params!["segments"]));
    }

    [Fact]
    public void ParsesQueryAndHashAndFlags()
    {
        var raw = (RouteLocationRaw)new RouteTo
        {
            Name = "search",
            Query = "ref=nav;page=2",
            Hash = "results",
            Replace = true,
            Force = true,
        }.ProvideValue(null!);

        Assert.Equal("nav", raw.Query!["ref"]);
        Assert.Equal("2", raw.Query["page"]);
        Assert.Equal("results", raw.Hash);
        Assert.True(raw.Replace);
        Assert.True(raw.Force);
    }

    [Fact]
    public void SurroundingWhitespaceAroundPairsIsIgnored()
    {
        var raw = (RouteLocationRaw)new RouteTo { Name = "user", Params = " id = 42 ; tab = billing " }.ProvideValue(null!);

        Assert.Equal("42", raw.Params!["id"]);
        Assert.Equal("billing", raw.Params["tab"]);
    }

    [Fact]
    public void SettingNeitherNameNorPathThrows()
    {
        Assert.Throws<InvalidOperationException>(() => new RouteTo().ProvideValue(null!));
    }

    [Fact]
    public void SettingBothNameAndPathThrowsRatherThanSilentlyIgnoringParams()
    {
        Assert.Throws<InvalidOperationException>(() => new RouteTo { Name = "user", Path = "/users/42" }.ProvideValue(null!));
    }

    [Theory]
    [InlineData("id")]
    [InlineData("=42")]
    public void AMalformedPairThrows(string malformed)
    {
        Assert.Throws<FormatException>(() => new RouteTo { Name = "user", Params = malformed }.ProvideValue(null!));
    }

    [Fact]
    public void ResolvesThroughTheRouterToTheNamedRoutesRealPath()
    {
        var router = new Router([
            new RouteRecord
            {
                Path = "/settings",
                Name = "settings",
                View = typeof(RecordingViewA),
                Children = [new RouteRecord { Path = "security", Name = "settings-security", View = typeof(RecordingViewB) }],
            },
        ]);

        var raw = (RouteLocationRaw)new RouteTo { Name = "settings-security" }.ProvideValue(null!);

        Assert.Equal("/settings/security", router.Resolve(raw).Path);
    }

    [Fact]
    public void WorksAsARouterLinkTargetParsedFromRealXaml()
    {
        StaThread.Run(() =>
        {
            var button = (Button)ParseXaml("aqr:RouterLink.To=\"{aqr:RouteTo Name=user, Params='id=42'}\"");

            var raw = Assert.IsType<RouteLocationRaw>(RouterLink.GetTo(button));
            Assert.Equal("user", raw.Name);
            Assert.Equal("42", raw.Params!["id"]);
        });
    }

    [Fact]
    public void WorksAsARouterLinkTargetInItsPositionalPathFormFromRealXaml()
    {
        StaThread.Run(() =>
        {
            var button = (Button)ParseXaml("aqr:RouterLink.To=\"{aqr:RouteTo /about}\"");

            var raw = Assert.IsType<RouteLocationRaw>(RouterLink.GetTo(button));
            Assert.Equal("/about", raw.Path);
        });
    }
}
