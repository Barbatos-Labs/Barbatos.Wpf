// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusRouter.Routing;

/// <summary>
/// An in-process navigation stack - a plain list of entries plus an integer cursor, with no
/// dependency on any real browser/URL/address bar. The WPF/C# counterpart of Vue Router's own
/// <c>createMemoryHistory()</c>, and (per its own docs) the mode Vue Router's own maintainers
/// reach for whenever there's no real browser environment to speak to - exactly a desktop app's
/// situation.
/// </summary>
/// <remarks>
/// <see cref="Router.Start(RouteLocationRaw)"/> must be called once at app startup to seed the
/// very first entry - unlike a browser, there is no ambient URL to read an initial location from.
/// </remarks>
public sealed class MemoryRouterHistory : IRouterHistory
{
    private readonly List<(string Location, object? State)> _entries = [("/", null)];
    private int _position;

    /// <inheritdoc/>
    public string Location => _entries[_position].Location;

    /// <inheritdoc/>
    public object? State => _entries[_position].State;

    /// <inheritdoc/>
    public void Push(string location, object? state = null)
    {
        _position++;
        if (_position < _entries.Count)
            _entries.RemoveRange(_position, _entries.Count - _position);
        _entries.Add((location, state));
    }

    /// <inheritdoc/>
    public void Replace(string location, object? state = null) =>
        _entries[_position] = (location, state);

    /// <inheritdoc/>
    public string? Peek(int delta)
    {
        var target = _position + delta;
        return target >= 0 && target < _entries.Count ? _entries[target].Location : null;
    }

    /// <inheritdoc/>
    public void Go(int delta) =>
        _position = Math.Clamp(_position + delta, 0, _entries.Count - 1);
}
