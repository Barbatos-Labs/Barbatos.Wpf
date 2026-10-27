// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// A handle to one registered <see cref="IStoreActionInterceptor"/>, returned by the
/// <c>out InterceptorHandle</c> overloads of <see cref="StoreServiceCollectionExtensions"/>'s
/// <c>AddStoreInterceptor</c> methods - the <c>axios.interceptors.request.eject(id)</c>
/// counterpart. Call <see cref="Eject"/> to remove the interceptor from every chain it applies
/// to, from that point on.
/// </summary>
/// <remarks>
/// Ejecting does not shrink or rebuild any store's interceptor chain - the interceptor's slot
/// stays in place, but becomes a transparent pass-through straight to the next interceptor (or
/// the action itself), as if it had never been registered. A call already in flight when
/// <see cref="Eject"/> is invoked keeps running with the interceptor still active, since its
/// own pipeline was already built by the time <see cref="Eject"/> ran; every call that starts
/// afterward skips it.
/// <para>
/// Ejecting is one-way. There is no "re-attach", and registering the interceptor again is no
/// way back either: <see cref="Hosting.WpfAppBuilder.Build"/> makes the service collection
/// read-only, and each store reads its chain once, when it is first resolved - getting the
/// behavior back means building a new container. For behavior that should come and go at
/// runtime (a settings toggle), give the interceptor its own enabled flag and have
/// <see cref="IStoreActionInterceptor.InterceptAsync"/> return <c>next()</c> while it is off;
/// keep <see cref="Eject"/> for a permanent removal.
/// </para>
/// </remarks>
public sealed class InterceptorHandle
{
    volatile bool _ejected;

    /// <summary>Whether <see cref="Eject"/> has been called on this handle.</summary>
    public bool IsEjected => _ejected;

    /// <summary>Removes the associated interceptor from every chain it applies to. Idempotent -
    /// calling this more than once has no further effect.</summary>
    public void Eject() => _ejected = true;
}
