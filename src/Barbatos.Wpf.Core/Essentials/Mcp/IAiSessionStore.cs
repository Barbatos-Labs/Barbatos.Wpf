// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Agents.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Persists an agent's <see cref="AgentSession"/> so a conversation survives closing and
/// reopening the app - the desktop counterpart of the server-side "serialize the session into
/// Redis between HTTP requests" pattern the Agent Framework is built for.
/// </summary>
/// <remarks>
/// <para>
/// The Agent Framework already knows how to turn a session into JSON and back
/// (<c>AIAgent.SerializeSessionAsync</c>/<c>DeserializeSessionAsync</c>); what it deliberately
/// leaves to the app is <i>where</i> that JSON lives. This is that seam, with a file-backed
/// default under <see cref="Storage.IFileSystem.AppDataDirectory"/>. Register your own
/// implementation via <c>services.AddSingleton&lt;IAiSessionStore, MySessionStore&gt;()</c>
/// (before calling <c>ConfigureMcp</c>) to keep sessions in SQLite, a roaming profile, or
/// wherever else suits the app.
/// </para>
/// <para>
/// A session is tied to the agent that created it, so the same <see cref="AIAgent"/> shape must
/// be passed to <see cref="LoadAsync"/> as was passed to <see cref="SaveAsync"/>. Take the agent
/// from <see cref="IAiAgentFactory"/> both times and this holds automatically.
/// </para>
/// <para>
/// <b>A saved session contains the conversation itself</b> - whatever the end user typed and
/// whatever tools returned, which for a desktop assistant can easily include personal data. The
/// default implementation writes plain JSON into the app's own data directory, which is
/// per-user but not encrypted; if the conversations an app handles warrant more than that, back
/// this interface with <see cref="Storage.ISecureStorage"/> or your own encrypted store instead.
/// Restoring a session is also equivalent to accepting input: only load sessions this app wrote.
/// </para>
/// </remarks>
public interface IAiSessionStore
{
    /// <summary>
    /// Saves <paramref name="session"/> under <paramref name="sessionId"/>, replacing any
    /// previously saved session with that id.
    /// </summary>
    /// <param name="sessionId">The app's own name for this conversation. Any non-empty string;
    /// the default implementation maps it to a filename, so it may not contain characters that
    /// are invalid in one.</param>
    /// <param name="agent">The agent the session belongs to - the one that created it.</param>
    /// <param name="session">The session to save.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentException"><paramref name="sessionId"/> is null, whitespace, or
    /// contains characters the store cannot use.</exception>
    Task SaveAsync(string sessionId, AIAgent agent, AgentSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the session saved under <paramref name="sessionId"/>, or returns
    /// <see langword="null"/> if nothing has been saved under that id - so a "resume last
    /// conversation" path can fall back to <c>agent.CreateSessionAsync()</c> without
    /// catching anything.
    /// </summary>
    /// <param name="sessionId">The id the session was saved under.</param>
    /// <param name="agent">The agent to restore the session onto - see the interface remarks.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentException"><paramref name="sessionId"/> is null, whitespace, or
    /// contains characters the store cannot use.</exception>
    Task<AgentSession?> LoadAsync(string sessionId, AIAgent agent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the ids of every currently saved session, for a "recent conversations" UI. Ordering
    /// is unspecified.
    /// </summary>
    Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the session saved under <paramref name="sessionId"/>. Returns
    /// <see langword="false"/> if nothing was saved under that id.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="sessionId"/> is null, whitespace, or
    /// contains characters the store cannot use.</exception>
    Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default);
}
