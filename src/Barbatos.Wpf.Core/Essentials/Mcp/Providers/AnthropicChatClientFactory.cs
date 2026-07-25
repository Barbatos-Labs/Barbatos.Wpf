// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Anthropic;
using Microsoft.Extensions.AI;

namespace Barbatos.Wpf.Mcp.Providers;

/// <summary>
/// Builds an <see cref="IChatClient"/> over the official Anthropic .NET SDK - used when
/// <see cref="AiProviderOptions.Provider"/> case-insensitively equals <c>"anthropic"</c>.
/// </summary>
/// <remarks>
/// Anthropic's own SDK documentation notes this package is still versioned 10+ but currently in
/// beta, with possible breaking changes in minor/patch releases - this repository pins an exact
/// version in <c>Directory.Packages.props</c> (no floating ranges), so such a change is only ever
/// absorbed when this repository deliberately bumps that pin.
/// </remarks>
internal static class AnthropicChatClientFactory
{
    public static IChatClient Create(string apiKey, string model) =>
        new AnthropicClient { ApiKey = apiKey }.AsIChatClient(model);
}
