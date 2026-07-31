// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.IO;
using System.Text.Json;
using Barbatos.Wpf.Storage;
using Microsoft.Agents.AI;

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// The default <see cref="IAiSessionStore"/>: one JSON file per session under
/// <c>{IFileSystem.AppDataDirectory}/ai-sessions</c>. See <see cref="IAiSessionStore"/>'s own
/// remarks for when this default is not enough - notably that these files are not encrypted.
/// </summary>
internal sealed class FileAiSessionStore : IAiSessionStore
{
    const string DirectoryName = "ai-sessions";
    const string FileExtension = ".json";

    readonly IFileSystem _fileSystem;

    public FileAiSessionStore(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task SaveAsync(string sessionId, AIAgent agent, AgentSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(session);

        var path = PathFor(sessionId);
        var json = await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken).ConfigureAwait(false);

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Written to a sibling temp file and moved into place so an interrupted save (the app
        // being killed mid-write is entirely normal on desktop) leaves the previous session
        // intact rather than a half-written file that then fails to parse on next launch.
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, json, cancellationToken: cancellationToken).ConfigureAwait(false);

        File.Move(tempPath, path, overwrite: true);
    }

    public async Task<AgentSession?> LoadAsync(string sessionId, AIAgent agent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var path = PathFor(sessionId);
        if (!File.Exists(path))
            return null;

        JsonElement json;
        await using (var stream = File.OpenRead(path))
            json = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return await agent.DeserializeSessionAsync(json, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        var directory = SessionDirectory;
        if (!Directory.Exists(directory))
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        var ids = Directory.EnumerateFiles(directory, "*" + FileExtension)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(ids!);
    }

    public Task<bool> DeleteAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var path = PathFor(sessionId);
        if (!File.Exists(path))
            return Task.FromResult(false);

        File.Delete(path);
        return Task.FromResult(true);
    }

    string SessionDirectory => Path.Combine(_fileSystem.AppDataDirectory, DirectoryName);

    string PathFor(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        // Rejected rather than sanitized: silently rewriting the id would let two different
        // conversations collapse onto the same file, and a separator would let an id escape the
        // session directory entirely. The caller picks these ids, so a clear failure is better
        // than a surprising one.
        if (sessionId.AsSpan().IndexOfAny(InvalidIdChars) >= 0)
        {
            throw new ArgumentException(
                $"Session id '{sessionId}' contains a character that cannot be used in a file name.",
                nameof(sessionId));
        }

        return Path.Combine(SessionDirectory, sessionId + FileExtension);
    }

    static readonly char[] InvalidIdChars =
        [.. Path.GetInvalidFileNameChars().Concat([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '.'])];
}
