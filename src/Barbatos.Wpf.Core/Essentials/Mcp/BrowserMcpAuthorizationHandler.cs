// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Web;
using Barbatos.Wpf.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IMcpAuthorizationHandler"/>: opens the end user's own browser at the
/// authorization server's consent page and catches the redirect on a short-lived loopback HTTP
/// listener. This is the flow OAuth 2.1 / RFC 8252 prescribes for native apps - the user consents
/// in a real browser, where they can see the address bar and their existing session, and the
/// application never handles their password.
/// </summary>
/// <remarks>
/// The listener is bound only for the duration of one authorization, on <c>127.0.0.1</c> and a
/// port chosen per attempt unless <see cref="McpOAuthOptions.RedirectUri"/> pins one. It ignores
/// any incidental request the browser makes (a favicon fetch, say) and completes on the first one
/// carrying an OAuth <c>code</c> or <c>error</c> parameter.
/// </remarks>
public sealed class BrowserMcpAuthorizationHandler : IMcpAuthorizationHandler
{
    readonly ILauncher _launcher;
    readonly ILogger<BrowserMcpAuthorizationHandler>? _logger;

    /// <summary>
    /// Creates the handler. Resolved from the container by <c>ConfigureMcp</c>; construct it
    /// directly only to customise <see cref="Timeout"/> or <see cref="CompletionHtml"/> before
    /// registering it yourself.
    /// </summary>
    public BrowserMcpAuthorizationHandler(ILauncher launcher, ILogger<BrowserMcpAuthorizationHandler>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(launcher);

        _launcher = launcher;
        _logger = logger;
    }

    /// <summary>
    /// How long to wait for the user to finish consenting before giving up. Defaults to five
    /// minutes - long enough to sign in and read a consent screen, short enough that an abandoned
    /// attempt does not hold a port open for the rest of the session. The
    /// <see cref="CancellationToken"/> passed to <see cref="AuthorizeAsync"/> still cancels
    /// earlier.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The HTML shown in the browser tab once the redirect has been received, telling the user to
    /// go back to the app. Must be self-contained - the page is served by a listener that shuts
    /// down immediately afterwards, so it cannot load anything else.
    /// </summary>
    public string CompletionHtml { get; set; } =
        """
        <!doctype html>
        <html lang="en">
        <head><meta charset="utf-8"><title>Signed in</title></head>
        <body style="font-family:system-ui,sans-serif;text-align:center;padding:3rem;color:#1f2328">
          <h1 style="font-size:1.25rem">You're signed in</h1>
          <p style="color:#59636e">You can close this tab and go back to the application.</p>
        </body>
        </html>
        """;

    /// <inheritdoc />
    public Uri GetRedirectUri(McpServerDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        // An app-supplied URI wins: it is set precisely when the authorization server was told to
        // expect that exact value, so substituting a fresh port would break the flow.
        if (descriptor.OAuth?.RedirectUri is { } configured)
            return configured;

        // RFC 8252 section 7.3 prefers the loopback IP literal over the name "localhost", which
        // depends on name resolution the app does not control.
        return new Uri($"http://127.0.0.1:{FindFreeLoopbackPort()}/");
    }

    /// <inheritdoc />
    public async Task<McpAuthorizationResult> AuthorizeAsync(McpAuthorizationContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Listen at the root of the redirect's port rather than at its exact path: HttpListener
        // prefixes must end in '/' and match by path segment, so a prefix built from a path like
        // "/callback" would miss the very request being waited for. The port belongs to this flow
        // for its whole duration either way.
        var prefix = $"{context.RedirectUri.Scheme}://{context.RedirectUri.Host}:{context.RedirectUri.Port}/";

        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);

        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex)
        {
            throw new McpAuthorizationException(
                $"Could not listen for the OAuth redirect of '{context.Descriptor.Name}' on {prefix}. The port may already be in use by another application.",
                ex);
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(Timeout);

        try
        {
            _logger?.LogInformation(
                "Opening the browser to authorize the MCP server '{ServerName}'; awaiting the redirect on {RedirectUri}.",
                context.Descriptor.Name,
                context.RedirectUri);

            if (!await _launcher.OpenAsync(context.AuthorizationUri).ConfigureAwait(false))
            {
                throw new McpAuthorizationException(
                    $"No browser could be opened to authorize the MCP server '{context.Descriptor.Name}'.");
            }

            return await WaitForRedirectAsync(listener, context, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new McpAuthorizationException(
                $"Authorizing the MCP server '{context.Descriptor.Name}' timed out after {Timeout.TotalMinutes:0.#} minutes without a response from the browser.");
        }
        finally
        {
            // Stop() alone leaves the port bound until the listener is collected; the using block
            // disposes it, but do it eagerly so a retry can reuse the port immediately.
            listener.Close();
        }
    }

    async Task<McpAuthorizationResult> WaitForRedirectAsync(HttpListener listener, McpAuthorizationContext context, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // GetContextAsync has no cancellable overload; closing the listener is what unblocks
            // it, so cancellation is registered to do exactly that and the resulting exception is
            // translated back into a cancellation below.
            HttpListenerContext request;
            using (cancellationToken.Register(listener.Close))
            {
                try
                {
                    request = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException or InvalidOperationException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new McpAuthorizationException(
                        $"The OAuth redirect listener for '{context.Descriptor.Name}' stopped before a response arrived.",
                        ex);
                }
            }

            var query = HttpUtility.ParseQueryString(request.Request.Url?.Query ?? string.Empty);
            var code = query["code"];
            var error = query["error"];

            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(error))
            {
                // Not the redirect - a browser prefetch or a favicon probe. Turn it away and keep
                // waiting rather than failing the whole flow.
                await RespondAsync(request, HttpStatusCode.NotFound, "<!doctype html><title>Not found</title>").ConfigureAwait(false);
                continue;
            }

            if (!string.IsNullOrEmpty(error))
            {
                var description = query["error_description"];
                await RespondAsync(request, HttpStatusCode.OK, ErrorHtml(error, description)).ConfigureAwait(false);

                throw new McpAuthorizationException(
                    $"Authorization for the MCP server '{context.Descriptor.Name}' was refused: {description ?? error}.")
                {
                    Error = error,
                    ErrorDescription = description,
                };
            }

            var state = query["state"];
            if (string.IsNullOrEmpty(state))
            {
                await RespondAsync(request, HttpStatusCode.OK, ErrorHtml("invalid_request", "The redirect carried no state parameter.")).ConfigureAwait(false);

                // Without state the SDK cannot bind this response to the request that started it,
                // which is the whole point of the parameter - refuse rather than guess.
                throw new McpAuthorizationException(
                    $"The OAuth redirect for the MCP server '{context.Descriptor.Name}' carried no 'state' parameter, so it cannot be matched to the request that started it.")
                {
                    Error = "invalid_request",
                };
            }

            await RespondAsync(request, HttpStatusCode.OK, CompletionHtml).ConfigureAwait(false);

            _logger?.LogInformation("Received the OAuth redirect for the MCP server '{ServerName}'.", context.Descriptor.Name);

            // "iss" is optional; passing it through when present lets the SDK verify the response
            // came from the authorization server it expected (RFC 9207).
            return new McpAuthorizationResult(code!, state, query["iss"]);
        }
    }

    static async Task RespondAsync(HttpListenerContext context, HttpStatusCode statusCode, string html)
    {
        var body = Encoding.UTF8.GetBytes(html);

        try
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = body.Length;
            await context.Response.OutputStream.WriteAsync(body).ConfigureAwait(false);
        }
        catch (HttpListenerException)
        {
            // The browser hung up before the page was written. The authorization code was still
            // received, so this must not fail the flow.
        }
        finally
        {
            context.Response.Close();
        }
    }

    static string ErrorHtml(string error, string? description) =>
        $"""
        <!doctype html>
        <html lang="en">
        <head><meta charset="utf-8"><title>Not signed in</title></head>
        <body style="font-family:system-ui,sans-serif;text-align:center;padding:3rem;color:#1f2328">
          <h1 style="font-size:1.25rem">Not signed in</h1>
          <p style="color:#59636e">{HttpUtility.HtmlEncode(description ?? error)}</p>
          <p style="color:#59636e">You can close this tab and go back to the application.</p>
        </body>
        </html>
        """;

    static int FindFreeLoopbackPort()
    {
        // Binding to port 0 asks the OS for an unused one. There is an unavoidable gap before the
        // real listener claims it, which is what every native OAuth client does; a port taken in
        // between surfaces as a clear "port already in use" error rather than a silent hang.
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();

        try
        {
            return ((IPEndPoint)probe.LocalEndpoint).Port;
        }
        finally
        {
            probe.Stop();
        }
    }
}
