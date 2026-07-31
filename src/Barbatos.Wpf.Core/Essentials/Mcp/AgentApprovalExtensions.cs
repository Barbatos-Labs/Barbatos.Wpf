// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Helpers for driving the tool-approval round trip from a WPF app when an agent built by
/// <see cref="IAiAgentFactory"/> runs under <see cref="AiToolApprovalMode.Always"/>.
/// </summary>
/// <remarks>
/// <para>
/// The protocol underneath is: a run that wants to call a tool comes back with one or more
/// <see cref="ToolApprovalRequestContent"/> items in its messages instead of a final answer. The
/// app asks the end user, then runs the agent <i>again on the same session</i>, passing a message
/// that carries the matching <see cref="ToolApprovalResponseContent"/> items. Only then does the
/// tool actually execute and the answer arrive. The two methods here are just the "read the
/// pending requests" and "write the answer message" halves of that, so app code never has to
/// hand-assemble <c>AIContent</c> lists:
/// </para>
/// <code>
/// var response = await agent.RunAsync(userText, session);
///
/// // Keep going as long as the agent is waiting on the end user rather than answering.
/// while (response.GetToolApprovalRequests() is { Count: > 0 } pending)
/// {
///     // ToolCall is typed as the ToolCallContent base; the name lives on FunctionCallContent,
///     // which is what every MCP tool call actually is.
///     var name = (pending[0].ToolCall as FunctionCallContent)?.Name ?? pending[0].ToolCall.CallId;
///
///     var approved = MessageBox.Show(
///         $"Let the assistant run '{name}'?",
///         "Tool approval",
///         MessageBoxButton.YesNo) == MessageBoxResult.Yes;
///
///     response = await agent.RunAsync(pending.CreateApprovalResponse(approved), session);
/// }
///
/// Transcript.Add(response.Text);
/// </code>
/// <para>
/// The loop is required, not a convenience: when a model asks for several tools at once the
/// agent surfaces them <b>one at a time</b>, queueing the rest, because that is what a modal
/// dialog can actually present. So a single approval does not necessarily mean the next response
/// is the final answer - keep going until <see cref="GetToolApprovalRequests"/> comes back empty.
/// </para>
/// <para>
/// Passing <c>alwaysApprove: true</c> to <see cref="CreateApprovalResponse"/> is what backs a
/// "Don't ask again for this tool" checkbox - the agent records a standing rule on the session
/// and stops prompting for that tool for the rest of the conversation.
/// </para>
/// </remarks>
public static class AgentApprovalExtensions
{
    /// <summary>
    /// Gets the tool calls this response is waiting on approval for, or an empty list if the
    /// agent produced an ordinary answer and needs nothing from the end user. A non-empty result
    /// means the run is paused: no tool has run yet, and
    /// <see cref="AgentResponse.Text"/> is not the final answer.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<ToolApprovalRequestContent> GetToolApprovalRequests(this AgentResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        List<ToolApprovalRequestContent>? requests = null;
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is ToolApprovalRequestContent request)
                    (requests ??= []).Add(request);
            }
        }

        return requests ?? (IReadOnlyList<ToolApprovalRequestContent>)Array.Empty<ToolApprovalRequestContent>();
    }

    /// <summary>
    /// Builds the message to pass to the agent's next run to answer every request in
    /// <paramref name="requests"/> the same way - typically the result of
    /// <see cref="GetToolApprovalRequests"/>, once the end user has clicked Allow or Deny.
    /// </summary>
    /// <param name="requests">The pending requests being answered.</param>
    /// <param name="approved">Whether the tool calls may run. Denying is not an error: the agent
    /// carries on and tells the model the call was refused, so it can reply some other way.</param>
    /// <param name="alwaysApprove">When <see langword="true"/> (and <paramref name="approved"/> is
    /// also <see langword="true"/>), records a standing rule so later calls to the same tool run
    /// without prompting again for the rest of the session - the "Don't ask again" answer. Ignored
    /// when <paramref name="approved"/> is <see langword="false"/>, since there is no such thing
    /// as a standing denial.</param>
    /// <param name="reason">An optional explanation passed back to the model, most useful when
    /// denying ("the user declined to share their filesystem").</param>
    /// <exception cref="ArgumentNullException"><paramref name="requests"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="requests"/> is empty.</exception>
    public static ChatMessage CreateApprovalResponse(
        this IEnumerable<ToolApprovalRequestContent> requests,
        bool approved,
        bool alwaysApprove = false,
        string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(requests);

        var contents = new List<AIContent>();
        foreach (var request in requests)
        {
            // CreateAlwaysApproveToolResponse produces a wrapper the UseToolApproval middleware
            // unwraps: it records the standing rule on the session, then forwards the plain
            // response inward. There is deliberately no denying counterpart - a standing rule only
            // ever widens what runs without asking - so a denial is always the plain response.
            contents.Add(approved && alwaysApprove
                ? request.CreateAlwaysApproveToolResponse(reason)
                : request.CreateResponse(approved, reason));
        }

        if (contents.Count == 0)
            throw new ArgumentException("At least one approval request is required to build a response message.", nameof(requests));

        return new ChatMessage(ChatRole.User, contents);
    }
}
