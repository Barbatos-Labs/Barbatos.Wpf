using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// <c>services.AddAquariusRouter()</c> / <c>provider.UseAquariusRouter()</c> - composing the
/// router through the container instead of newing it up and assigning statics by hand.
/// </summary>
public class AquariusRouterDependencyInjectionTests
{
    [Fact]
    public void TheRouteTableIsBuiltFromTheContainer()
    {
        // The reason the routes parameter is a factory rather than a plain collection: a
        // per-route BeforeEnter guard usually needs to close over a DI-resolved service, and
        // nothing should have to be constructed by hand first just to get at it.
        WithRestoredCurrent(() =>
        {
            var services = new ServiceCollection();
            services.AddSingleton<AuthStateStub>();
            services.AddAquariusRouter(sp =>
            [
                new RouteRecord
                {
                    Path = "/admin",
                    Name = "admin",
                    View = typeof(RecordingViewA),
                    BeforeEnter = (_, _) => Task.FromResult(
                        sp.GetRequiredService<AuthStateStub>().IsSignedIn
                            ? NavigationGuardResult.Continue
                            : NavigationGuardResult.Cancel),
                },
            ]);

            using var provider = services.BuildServiceProvider();
            var router = provider.UseAquariusRouter();

            Assert.Same(router, Router.Current);
            Assert.Same(router, provider.GetRequiredService<Router>()); // a singleton, not rebuilt per resolve

            var failure = router.Push("/admin").GetAwaiter().GetResult();
            Assert.Equal(NavigationFailureType.Aborted, failure?.Type);

            provider.GetRequiredService<AuthStateStub>().IsSignedIn = true;
            Assert.Null(router.Push("/admin").GetAwaiter().GetResult());
        });
    }

    [Fact]
    public void OptionsFlowIntoTheConstructedRouter()
    {
        WithRestoredCurrent(() =>
        {
            var services = new ServiceCollection();
            services.AddAquariusRouter(
                _ => [new RouteRecord { Path = "/Home", Name = "home", View = typeof(RecordingViewA) }],
                options => options.PathOptions = new PathParserOptions { Sensitive = true });

            using var provider = services.BuildServiceProvider();
            var router = provider.UseAquariusRouter();

            // Sensitive really reached the matcher: "/home" must no longer match "/Home".
            Assert.Null(router.Start("/Home").GetAwaiter().GetResult());
            Assert.Throws<InvalidOperationException>(() => router.Resolve("/home"));
        });
    }

    [Fact]
    public void AnAppsOwnHistoryRegistrationWins()
    {
        // Registered with TryAdd, so an app that needs something else - a history that persists
        // across restarts, say - registers it first and keeps it.
        WithRestoredCurrent(() =>
        {
            var services = new ServiceCollection();
            var history = new MemoryRouterHistory();
            services.AddSingleton<IRouterHistory>(history);
            services.AddAquariusRouter(_ => [new RouteRecord { Path = "/", Name = "home", View = typeof(RecordingViewA) }]);

            using var provider = services.BuildServiceProvider();
            var router = provider.UseAquariusRouter();

            Assert.Null(router.Start("/").GetAwaiter().GetResult());
            Assert.Equal("/", history.Location);
        });
    }

    /// <summary>
    /// Runs on an STA thread (the router really constructs WPF Views) with
    /// <see cref="Router.Current"/> saved and restored, since <see cref="AquariusRouterServiceProviderExtensions.UseAquariusRouter"/>
    /// writes that process-wide static.
    /// </summary>
    private static void WithRestoredCurrent(Action body) => StaThread.Run(() =>
    {
        var original = Router.Current;

        try
        {
            body();
        }
        finally
        {
            Router.Current = original;
        }
    });

    /// <summary>Stands in for an app service a per-route guard needs to close over.</summary>
    private sealed class AuthStateStub
    {
        public bool IsSignedIn { get; set; }
    }
}
