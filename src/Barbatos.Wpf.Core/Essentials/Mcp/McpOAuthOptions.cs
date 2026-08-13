// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// OAuth settings for an <see cref="McpTransportKind.Http"/> server that requires authorization -
/// the hosted MCP servers a desktop app is most likely to want (GitHub, Linear, Notion, and the
/// like) all do. Set <see cref="McpServerDescriptor.OAuth"/> to an instance of this to turn the
/// flow on; leave it <see langword="null"/> for a server that needs no authorization, or one that
/// authenticates with a static token supplied through
/// <see cref="McpServerDescriptor.AdditionalHeaders"/> instead.
/// </summary>
/// <remarks>
/// Every property here is optional. An empty <c>new McpOAuthOptions()</c> is the intended common
/// case and is enough for any server that follows the MCP authorization spec: the client discovers
/// the authorization server from the server's own <c>401</c> challenge, registers itself
/// dynamically (no pre-provisioned client ID), asks for the scopes the server advertises, and
/// drives the user through consent via <see cref="IMcpAuthorizationHandler"/>. Fill properties in
/// only where a specific server needs you to.
/// </remarks>
public class McpOAuthOptions
{
    /// <summary>
    /// The pre-registered OAuth client ID, or <see langword="null"/> (the default) to register
    /// dynamically with the authorization server on first use. Set this only for a server that
    /// does not support dynamic client registration and made you create a client by hand in its
    /// developer console.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The client secret paired with <see cref="ClientId"/>, for the rare confidential-client
    /// server that requires one. Leave <see langword="null"/> otherwise.
    /// </summary>
    /// <remarks>
    /// A secret embedded in a desktop app is not actually secret - anyone can extract it from the
    /// shipped binary - which is why the OAuth 2.1 / RFC 8252 native-app guidance the MCP spec
    /// follows uses PKCE with a public client instead. Prefer leaving this unset; it exists for
    /// servers that give you no other option.
    /// </remarks>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Fallback scopes to request, used only when the server advertises none of its own. The MCP
    /// scope-selection order is the <c>WWW-Authenticate</c> challenge first, then the protected
    /// resource metadata's <c>scopes_supported</c>, then this list, then no scope parameter at
    /// all - so leaving this empty is correct for any server that publishes its own scopes.
    /// </summary>
    public IList<string> Scopes { get; } = new List<string>();

    /// <summary>
    /// The loopback URL the authorization server redirects back to, or <see langword="null"/> (the
    /// default) to let <see cref="IMcpAuthorizationHandler"/> pick a free port on
    /// <c>127.0.0.1</c> for this attempt. Set it explicitly only when the server made you register
    /// a fixed redirect URL up front, since a fixed port can already be in use on the end user's
    /// machine.
    /// </summary>
    public Uri? RedirectUri { get; set; }

    /// <summary>
    /// The client name sent during dynamic client registration, which is what the authorization
    /// server shows the end user on the consent screen ("<c>Contoso Desktop</c> wants to access
    /// your account"). Defaults to the application's own <c>IAppInfo.Name</c> when left unset.
    /// Ignored when <see cref="ClientId"/> is set, since no registration happens then.
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// Extra query parameters appended to the authorization request, for servers that require a
    /// non-standard one (an <c>audience</c>, a tenant hint, a <c>prompt</c> override).
    /// </summary>
    public IDictionary<string, string> AdditionalAuthorizationParameters { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Whether the tokens obtained for this server are persisted through
    /// <see cref="IMcpTokenStore"/> - DPAPI-encrypted <c>SecureStorage</c> by default - so the end
    /// user is not sent back through the browser on every app restart. Defaults to
    /// <see langword="true"/>. Set to <see langword="false"/> to keep tokens in memory only, which
    /// means re-consenting each run.
    /// </summary>
    public bool PersistTokens { get; set; } = true;

    /// <summary>
    /// Validates that these options are internally consistent, throwing
    /// <see cref="InvalidOperationException"/> if they are not. Called for you by
    /// <see cref="McpServerDescriptor.Validate"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException"><see cref="ClientSecret"/> is set without a
    /// <see cref="ClientId"/>, or <see cref="RedirectUri"/> is set to something the default
    /// handler cannot listen on.</exception>
    public void Validate()
    {
        if (!string.IsNullOrWhiteSpace(ClientSecret) && string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException(
                $"{nameof(McpOAuthOptions)}.{nameof(ClientSecret)} cannot be set without {nameof(ClientId)} - a secret only makes sense for a client that was registered by hand.");
        }

        if (RedirectUri is null)
            return;

        if (!RedirectUri.IsAbsoluteUri)
        {
            throw new InvalidOperationException(
                $"{nameof(McpOAuthOptions)}.{nameof(RedirectUri)} must be an absolute URL.");
        }

        // The default handler receives the redirect on a local HTTP listener, so anything other
        // than plain http on the loopback interface cannot work. Checked here rather than at
        // connect time so a settings UI can reject it while the user is still editing.
        if (RedirectUri.Scheme != Uri.UriSchemeHttp || !RedirectUri.IsLoopback)
        {
            throw new InvalidOperationException(
                $"{nameof(McpOAuthOptions)}.{nameof(RedirectUri)} must be an http:// loopback URL (for example http://127.0.0.1:8765/callback/), which is what the OAuth 2.1 native-app flow expects and what {nameof(IMcpAuthorizationHandler)} can listen on.");
        }
    }
}
