// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Net.Http;
using Barbatos.Wpf.ApplicationModel;
using Barbatos.Wpf.Mcp;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class McpOAuthTests
{
    // ---- McpOAuthOptions / McpServerDescriptor validation -------------------------------------

    [Fact]
    public void EmptyOAuthOptionsAreValid()
    {
        // The intended common case: everything else is discovered from the server's own 401
        // challenge and its protected resource metadata.
        var descriptor = new McpServerDescriptor
        {
            Name = "hosted",
            TransportKind = McpTransportKind.Http,
            Endpoint = "https://example.com/mcp",
            OAuth = new McpOAuthOptions(),
        };

        Assert.Null(Record.Exception(descriptor.Validate));
    }

    [Fact]
    public void OAuthOnAStdioServerIsRejected()
    {
        var descriptor = new McpServerDescriptor
        {
            Name = "local",
            TransportKind = McpTransportKind.Stdio,
            Command = "dotnet",
            OAuth = new McpOAuthOptions(),
        };

        var exception = Assert.Throws<InvalidOperationException>(descriptor.Validate);
        Assert.Contains(nameof(McpServerDescriptor.OAuth), exception.Message);
    }

    [Fact]
    public void ClientSecretWithoutAClientIdIsRejected()
    {
        var options = new McpOAuthOptions { ClientSecret = "s3cret" };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains(nameof(McpOAuthOptions.ClientId), exception.Message);
    }

    [Theory]
    [InlineData("https://example.com/callback")] // not loopback, and not plain http
    [InlineData("http://example.com/callback")]  // not loopback
    [InlineData("myapp://callback")]             // a custom scheme the listener cannot bind
    public void ANonLoopbackRedirectUriIsRejected(string redirectUri)
    {
        var options = new McpOAuthOptions { RedirectUri = new Uri(redirectUri) };

        var exception = Assert.Throws<InvalidOperationException>(options.Validate);
        Assert.Contains(nameof(McpOAuthOptions.RedirectUri), exception.Message);
    }

    [Theory]
    [InlineData("http://127.0.0.1:8765/callback/")]
    [InlineData("http://localhost:8765/")]
    public void ALoopbackRedirectUriIsAccepted(string redirectUri)
    {
        var options = new McpOAuthOptions { RedirectUri = new Uri(redirectUri) };

        Assert.Null(Record.Exception(options.Validate));
    }

    // ---- BrowserMcpAuthorizationHandler ------------------------------------------------------

    [Fact]
    public void GetRedirectUriHonoursAConfiguredValue()
    {
        var handler = new BrowserMcpAuthorizationHandler(new FakeLauncher());
        var configured = new Uri("http://127.0.0.1:8765/callback/");

        var redirectUri = handler.GetRedirectUri(new McpServerDescriptor
        {
            Name = "hosted",
            TransportKind = McpTransportKind.Http,
            Endpoint = "https://example.com/mcp",
            OAuth = new McpOAuthOptions { RedirectUri = configured },
        });

        Assert.Equal(configured, redirectUri);
    }

    [Fact]
    public void GetRedirectUriPicksAFreeLoopbackPortWhenUnconfigured()
    {
        var handler = new BrowserMcpAuthorizationHandler(new FakeLauncher());
        var descriptor = HostedServer(new McpOAuthOptions());

        var first = handler.GetRedirectUri(descriptor);
        var second = handler.GetRedirectUri(descriptor);

        Assert.Equal("127.0.0.1", first.Host);
        Assert.Equal(Uri.UriSchemeHttp, first.Scheme);
        Assert.InRange(first.Port, 1024, 65535);
        // Each attempt asks the OS for its own port, so two concurrent authorizations can't
        // collide on one listener.
        Assert.NotEqual(first.Port, second.Port);
    }

    [Fact]
    public async Task AuthorizeAsyncReturnsTheCodeStateAndIssuerFromTheRedirect()
    {
        // Drives the real loopback listener end to end: the fake "browser" performs the redirect
        // the authorization server would, so this proves HttpListener can actually be bound on
        // 127.0.0.1 without elevation and that the query is parsed as expected.
        var result = await AuthorizeAsync(
            new FakeLauncher("?code=the-code&state=the-state&iss=https%3A%2F%2Fauth.example.com"));

        Assert.Equal("the-code", result.Code);
        Assert.Equal("the-state", result.State);
        Assert.Equal("https://auth.example.com", result.Issuer);
    }

    [Fact]
    public async Task AuthorizeAsyncIgnoresRequestsThatCarryNoOAuthParameters()
    {
        // Browsers routinely fetch /favicon.ico against the redirect origin. Treating the first
        // request to arrive as the redirect would fail the flow at random.
        var launcher = new FakeLauncher("?code=the-code&state=the-state") { PrefetchPath = "favicon.ico" };

        var result = await AuthorizeAsync(launcher);

        Assert.Equal("the-code", result.Code);
        Assert.True(launcher.PrefetchWasAnswered);
    }

    [Fact]
    public async Task AuthorizeAsyncSurfacesADeniedConsentAsAnAuthorizationException()
    {
        var launcher = new FakeLauncher("?error=access_denied&error_description=The+user+said+no&state=the-state");

        var exception = await Assert.ThrowsAsync<McpAuthorizationException>(() => AuthorizeAsync(launcher));

        Assert.Equal("access_denied", exception.Error);
        Assert.Equal("The user said no", exception.ErrorDescription);
        Assert.Contains("The user said no", exception.Message);
    }

    [Fact]
    public async Task AuthorizeAsyncRejectsARedirectWithNoState()
    {
        // Without state the SDK cannot bind the response to the request that started it, which is
        // the parameter's entire purpose - accepting it would defeat the CSRF protection.
        var exception = await Assert.ThrowsAsync<McpAuthorizationException>(() =>
            AuthorizeAsync(new FakeLauncher("?code=the-code")));

        Assert.Contains("state", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizeAsyncFailsWhenNoBrowserCanBeOpened()
    {
        var exception = await Assert.ThrowsAsync<McpAuthorizationException>(() =>
            AuthorizeAsync(new FakeLauncher { CanOpen = false }));

        Assert.Contains("browser", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizeAsyncTimesOutRatherThanWaitingForever()
    {
        // A user who closes the browser tab without answering must not leave the connection - and
        // the loopback port - pinned open for the rest of the session.
        var exception = await Assert.ThrowsAsync<McpAuthorizationException>(() =>
            AuthorizeAsync(new FakeLauncher(), handler => handler.Timeout = TimeSpan.FromMilliseconds(250)));

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizeAsyncObservesTheCallersCancellationToken()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AuthorizeAsync(new FakeLauncher(), cancellationToken: cancellation.Token));
    }

    // ---- IMcpTokenStore ----------------------------------------------------------------------

    [Fact]
    public async Task TokenStoreRoundTripsAndRemovesPerServer()
    {
        var store = BuildTokenStore();

        Assert.Null(await store.GetAsync("github"));
        Assert.False(await store.RemoveAsync("github"));

        await store.SetAsync("github", "{\"AccessToken\":\"at\"}");
        await store.SetAsync("linear", "{\"AccessToken\":\"other\"}");

        Assert.Equal("{\"AccessToken\":\"at\"}", await store.GetAsync("github"));

        Assert.True(await store.RemoveAsync("github"));
        Assert.Null(await store.GetAsync("github"));
        // Signing out of one server must not sign the user out of every other one.
        Assert.Equal("{\"AccessToken\":\"other\"}", await store.GetAsync("linear"));
    }

    [Fact]
    public async Task SignOutAsyncDiscardsTheStoredTokensForOneServer()
    {
        var secureStorage = new FakeSecureStorage();
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<Storage.ISecureStorage>(secureStorage);
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        var registry = wpfApp.Services.GetRequiredService<IMcpServerRegistry>();
        var store = wpfApp.Services.GetRequiredService<IMcpTokenStore>();

        Assert.False(await registry.SignOutAsync("github"));

        await store.SetAsync("github", "{\"AccessToken\":\"at\"}");

        Assert.True(await registry.SignOutAsync("github"));
        Assert.Null(await store.GetAsync("github"));
    }

    [Fact]
    public void OAuthServicesAreRegisteredByConfigureMcpAndAreReplaceable()
    {
        var builder = WpfApp.CreateBuilder();
        var custom = new FakeMcpAuthorizationHandler();
        builder.Services.AddSingleton<IMcpAuthorizationHandler>(custom);
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        // TryAddSingleton leaves an already-registered handler alone, the same precedence
        // IAiApiKeyProvider relies on - that is what lets an app swap in a WebView2 flow.
        Assert.Same(custom, wpfApp.Services.GetRequiredService<IMcpAuthorizationHandler>());
        Assert.NotNull(wpfApp.Services.GetService<IMcpTokenStore>());
    }

    [Fact]
    public async Task AConnectionFailsClearlyWhenOAuthIsConfiguredWithoutAHandler()
    {
        // The registry is constructible without a handler (its constructor parameters are
        // optional), so a server that needs one must say so rather than NullReferenceException.
        var registry = new McpServerRegistryProbe();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            registry.AddServerAsync(HostedServer(new McpOAuthOptions())));

        Assert.Contains(nameof(IMcpAuthorizationHandler), exception.Message);
    }

    // ---- helpers -----------------------------------------------------------------------------

    static McpServerDescriptor HostedServer(McpOAuthOptions? oauth = null) => new()
    {
        Name = "hosted",
        TransportKind = McpTransportKind.Http,
        Endpoint = "https://example.com/mcp",
        OAuth = oauth,
    };

    static async Task<McpAuthorizationResult> AuthorizeAsync(
        FakeLauncher launcher,
        Action<BrowserMcpAuthorizationHandler>? configure = null,
        CancellationToken cancellationToken = default)
    {
        var handler = new BrowserMcpAuthorizationHandler(launcher);
        configure?.Invoke(handler);

        var descriptor = HostedServer(new McpOAuthOptions());

        // The port is only known once the handler has picked it, so the fake browser is told
        // where to send its redirect the same way a real authorization server would be.
        var redirectUri = handler.GetRedirectUri(descriptor);
        launcher.RedirectUri = redirectUri;

        var context = new McpAuthorizationContext(
            descriptor,
            new Uri("https://auth.example.com/authorize?client_id=abc"),
            redirectUri);

        return await handler.AuthorizeAsync(context, cancellationToken);
    }

    static IMcpTokenStore BuildTokenStore()
    {
        var builder = WpfApp.CreateBuilder();
        builder.Services.AddSingleton<Storage.ISecureStorage>(new FakeSecureStorage());
        builder.ConfigureMcp();
        var wpfApp = builder.Build();

        return wpfApp.Services.GetRequiredService<IMcpTokenStore>();
    }

    /// <summary>
    /// Resolves a registry built with no OAuth services at all, to prove the "OAuth configured but
    /// nothing to drive it" path reports itself properly.
    /// </summary>
    sealed class McpServerRegistryProbe
    {
        readonly IMcpServerRegistry _registry;

        public McpServerRegistryProbe()
        {
            var builder = WpfApp.CreateBuilder();
            // Registered before ConfigureMcp so TryAddSingleton keeps these nulls-in-disguise
            // rather than installing the real browser handler.
            builder.Services.AddSingleton<IMcpAuthorizationHandler>(_ => null!);
            builder.ConfigureMcp();
            _registry = builder.Build().Services.GetRequiredService<IMcpServerRegistry>();
        }

        public Task AddServerAsync(McpServerDescriptor descriptor) => _registry.AddServerAsync(descriptor);
    }

    /// <summary>
    /// Stands in for the end user's browser: instead of launching one, it performs the very
    /// request the authorization server's redirect would, against the handler's own listener.
    /// </summary>
    sealed class FakeLauncher : ILauncher
    {
        readonly string? _redirectQuery;

        public FakeLauncher(string? redirectQuery = null)
        {
            _redirectQuery = redirectQuery;
        }

        /// <summary>Set to false to simulate a machine with no usable browser.</summary>
        public bool CanOpen { get; set; } = true;

        /// <summary>An extra path fetched before the redirect, mimicking a favicon probe.</summary>
        public string? PrefetchPath { get; set; }

        public bool PrefetchWasAnswered { get; private set; }

        public Uri? RedirectUri { get; set; }

        public Task<bool> CanOpenAsync(Uri uri) => Task.FromResult(CanOpen);

        public Task<bool> OpenAsync(Uri uri)
        {
            if (!CanOpen)
                return Task.FromResult(false);

            if (_redirectQuery is not null && RedirectUri is { } redirectUri)
                _ = RespondAsync(redirectUri);

            return Task.FromResult(true);
        }

        public Task<bool> OpenAsync(OpenFileRequest request) => Task.FromResult(false);

        public Task<bool> TryOpenAsync(Uri uri) => OpenAsync(uri);

        async Task RespondAsync(Uri redirectUri)
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            if (PrefetchPath is { } prefetch)
            {
                try
                {
                    using var prefetchResponse = await client.GetAsync(new Uri(redirectUri, prefetch));
                    PrefetchWasAnswered = prefetchResponse.StatusCode == System.Net.HttpStatusCode.NotFound;
                }
                catch (HttpRequestException)
                {
                    // The listener may already be gone; the assertion on the real redirect below
                    // is what actually matters.
                }
            }

            try
            {
                using var response = await client.GetAsync(new Uri(redirectUri.GetLeftPart(UriPartial.Path) + _redirectQuery));
            }
            catch (HttpRequestException)
            {
                // Handler already tore the listener down (a denial path closes early).
            }
        }
    }

    sealed class FakeMcpAuthorizationHandler : IMcpAuthorizationHandler
    {
        public Uri GetRedirectUri(McpServerDescriptor descriptor) => new("http://127.0.0.1:1/");

        public Task<McpAuthorizationResult> AuthorizeAsync(McpAuthorizationContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(new McpAuthorizationResult("code", "state"));
    }
}
