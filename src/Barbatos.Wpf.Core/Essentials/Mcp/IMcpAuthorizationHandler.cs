// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Drives the interactive half of an MCP server's OAuth flow: sends the end user to the
/// authorization server's consent page and returns whatever it redirects back with. Everything
/// around it - discovering the authorization server, PKCE, dynamic client registration, exchanging
/// the code, refreshing - is handled by the MCP SDK; this is only the part that needs a human and
/// a UI, which is why it is the part a WPF app may want to replace.
/// </summary>
/// <remarks>
/// The default implementation (<see cref="BrowserMcpAuthorizationHandler"/>) opens the user's real
/// browser and catches the redirect on a loopback listener, which is the flow OAuth 2.1 / RFC 8252
/// prescribes for native apps: the user consents in a browser they can inspect, and the app never
/// sees their password. Register your own implementation before calling <c>ConfigureMcp</c> to
/// swap in something else - an embedded WebView2 window, for instance - the same way
/// <see cref="IAiApiKeyProvider"/> can be replaced.
/// </remarks>
public interface IMcpAuthorizationHandler
{
    /// <summary>
    /// Returns the redirect URL this handler will be able to receive an authorization response
    /// on, for the given server. Called once per connection attempt, before the flow starts,
    /// because the URL has to be registered with the authorization server up front.
    /// </summary>
    /// <param name="descriptor">The server about to be connected. Honour
    /// <c>descriptor.OAuth.RedirectUri</c> when it is set - the app set it because the server
    /// requires that exact value.</param>
    Uri GetRedirectUri(McpServerDescriptor descriptor);

    /// <summary>
    /// Sends the user to <see cref="McpAuthorizationContext.AuthorizationUri"/> and returns the
    /// result the authorization server redirects back with.
    /// </summary>
    /// <exception cref="McpAuthorizationException">The user denied consent, the flow timed out, or
    /// the redirect could not be received.</exception>
    Task<McpAuthorizationResult> AuthorizeAsync(McpAuthorizationContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// What <see cref="IMcpAuthorizationHandler.AuthorizeAsync"/> is asked to do.
/// </summary>
public sealed class McpAuthorizationContext
{
    /// <summary>
    /// Creates a context. Constructed by <see cref="IMcpServerRegistry"/>; exposed publicly so a
    /// custom handler can be unit-tested without a live server.
    /// </summary>
    public McpAuthorizationContext(McpServerDescriptor descriptor, Uri authorizationUri, Uri redirectUri)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(authorizationUri);
        ArgumentNullException.ThrowIfNull(redirectUri);

        Descriptor = descriptor;
        AuthorizationUri = authorizationUri;
        RedirectUri = redirectUri;
    }

    /// <summary>
    /// The server being authorized. Useful for telling the user *which* server is asking, when an
    /// app connects to several.
    /// </summary>
    public McpServerDescriptor Descriptor { get; }

    /// <summary>
    /// The authorization server's consent page. Send the user here.
    /// </summary>
    public Uri AuthorizationUri { get; }

    /// <summary>
    /// Where the authorization server will redirect once the user answers - the same URL
    /// <see cref="IMcpAuthorizationHandler.GetRedirectUri"/> returned for this attempt.
    /// </summary>
    public Uri RedirectUri { get; }
}

/// <summary>
/// The query parameters an authorization server sends back to
/// <see cref="McpAuthorizationContext.RedirectUri"/>.
/// </summary>
public sealed class McpAuthorizationResult
{
    /// <summary>
    /// Creates a result from the redirect's query parameters.
    /// </summary>
    /// <param name="code">The <c>code</c> parameter - the authorization code to exchange.</param>
    /// <param name="state">The <c>state</c> parameter. Pass it through unchanged: the SDK requires
    /// an exact match against the value it sent, which is what binds the response to the request
    /// that started it and blocks CSRF.</param>
    /// <param name="issuer">The <c>iss</c> parameter, when the server sent one. Pass it through
    /// too - it lets the SDK verify the response came from the authorization server it expected
    /// (RFC 9207), which is what defends against mix-up attacks.</param>
    public McpAuthorizationResult(string code, string state, string? issuer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        Code = code;
        State = state;
        Issuer = issuer;
    }

    /// <summary>The authorization code to exchange for tokens.</summary>
    public string Code { get; }

    /// <summary>The <c>state</c> value echoed back by the authorization server.</summary>
    public string State { get; }

    /// <summary>The issuer identifier, or <see langword="null"/> if the server sent none.</summary>
    public string? Issuer { get; }
}

/// <summary>
/// Thrown when an MCP server's OAuth flow could not be completed - the user closed the browser or
/// clicked "Deny", the flow timed out, or the redirect could not be received.
/// </summary>
public class McpAuthorizationException : Exception
{
    /// <summary>Creates an exception with the given message.</summary>
    public McpAuthorizationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception with the given message and cause.</summary>
    public McpAuthorizationException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// The OAuth <c>error</c> code the authorization server redirected back with (for example
    /// <c>access_denied</c> when the user clicked Deny), or <see langword="null"/> if the flow
    /// failed before any response arrived.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// The authorization server's own <c>error_description</c>, when it sent one.
    /// </summary>
    public string? ErrorDescription { get; init; }
}
