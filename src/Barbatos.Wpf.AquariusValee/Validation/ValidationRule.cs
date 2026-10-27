// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// A synchronous, schema-less validation rule for a <see cref="Field{T}"/> - the WPF/C#
/// counterpart of a vee-validate function rule (<c>(value) =&gt; boolean | string</c>).
/// </summary>
/// <returns><see langword="null"/> if <paramref name="value"/> is valid, otherwise the error message.</returns>
/// <remarks>
/// Deliberately not vee-validate's <c>bool | string | string[]</c> return convention - a
/// nullable-string return (null = valid, a message = invalid) is unambiguous, idiomatic C#,
/// and needs no separate "how do I supply a custom message" mechanism the way a bare
/// <see langword="false"/> return does in JS.
/// </remarks>
public delegate string? ValidationRule<in T>(T value);

/// <summary>
/// An asynchronous, schema-less validation rule for a <see cref="Field{T}"/> - e.g. an
/// "is this username available?" API check. While one is in flight, the owning
/// <see cref="Field{T}"/>'s <see cref="FieldMeta.Pending"/> is <see langword="true"/>.
/// </summary>
/// <returns><see langword="null"/> if the value is valid, otherwise the error message.</returns>
/// <remarks>
/// <b>Always forward <paramref name="cancellationToken"/> to whatever you <see langword="await"/>
/// inside this delegate.</b> <see cref="Field{T}"/> cancels-and-replaces the token on every new
/// validation call (a "latest wins" race-safety strategy) - a deliberate upgrade over
/// vee-validate's own approach of letting a stale in-flight call finish and simply discarding
/// its result, since real cancellation is idiomatic .NET and wasn't available to vee-validate
/// (JS Promises aren't cancellable). Forwarding the token is what turns that into actually
/// cutting the stale work short, not just ignoring its result after the fact.
/// <para>
/// <b>Throwing is safe, but it is not a message.</b> <see cref="FieldMeta.Pending"/> is lowered
/// whichever way the rule ends, and the exception reaches whoever awaits the pass
/// (<see cref="Field{T}.ValidateAsync"/>, <see cref="Form.ValidateAllAsync"/>, a
/// <see cref="Form.HandleSubmit"/> command) or, for a pass nobody awaits (typing, blur,
/// <see cref="Field{T}.DependsOn{TSource}"/>), is rethrown on the UI thread's dispatcher - see
/// <see cref="Field{T}"/>'s remarks. Only the cancellation of a superseded pass is swallowed, so a
/// timeout surfacing as <see cref="TaskCanceledException"/> is not mistaken for one. When a failed
/// check (offline, a timeout) should simply show as an error under the field, catch it here and
/// return the message.
/// </para>
/// </remarks>
public delegate Task<string?> AsyncValidationRule<in T>(T value, CancellationToken cancellationToken);
