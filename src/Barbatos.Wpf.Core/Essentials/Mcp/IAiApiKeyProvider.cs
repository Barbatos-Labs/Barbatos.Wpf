// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Mcp;

/// <summary>
/// Resolves and stores the end user's own API key for a given provider (see
/// <see cref="AiProviderOptions.Provider"/>) - the bring-your-own-key (BYOK) seam this whole
/// feature is built around: the application publisher never pays for or ships an LLM
/// subscription, the application's own end user supplies their key for whichever provider they
/// have an account with, typically entered once through a settings screen.
/// </summary>
/// <remarks>
/// The default implementation, <see cref="SecureStorageAiApiKeyProvider"/>, stores each
/// provider's key separately (keyed by the provider string's value, case-insensitively)
/// via <see cref="Storage.ISecureStorage"/> - so switching the configured provider back and
/// forth never loses a previously-entered key. Register your own implementation via
/// <c>services.AddSingleton&lt;IAiApiKeyProvider, MyKeyProvider&gt;()</c> (before calling
/// <c>ConfigureMcp</c>) to use a different secret store, e.g. Windows Credential Manager or an
/// enterprise key vault, instead of the DPAPI-backed default.
/// </remarks>
public interface IAiApiKeyProvider
{
    /// <summary>
    /// Gets the stored API key for <paramref name="provider"/>, or <see langword="null"/> if none
    /// has been set yet.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="provider"/> is null or whitespace.</exception>
    Task<string?> GetApiKeyAsync(string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores <paramref name="apiKey"/> as the API key for <paramref name="provider"/>, replacing
    /// any previous value. Typically called once, from a settings UI, when the end user pastes in
    /// their own key.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="provider"/> or <paramref name="apiKey"/>
    /// is null or whitespace.</exception>
    Task SetApiKeyAsync(string provider, string apiKey, CancellationToken cancellationToken = default);
}
