// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.Apsu;

/// <summary>
/// Wraps another <see cref="IStoreActionInterceptor"/> so it can be ejected via
/// <see cref="InterceptorHandle"/> - registered in place of the real interceptor, so its slot in
/// the chain is fixed at store-creation time same as any other interceptor, but each call
/// re-checks <see cref="InterceptorHandle.IsEjected"/> before deciding whether to actually invoke
/// the wrapped interceptor or just forward straight to <c>next</c>.
/// </summary>
internal sealed class EjectableInterceptor : IStoreActionInterceptor
{
    readonly IStoreActionInterceptor _inner;
    readonly InterceptorHandle _handle;

    public EjectableInterceptor(IStoreActionInterceptor inner, InterceptorHandle handle)
    {
        _inner = inner;
        _handle = handle;
    }

    public Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next) =>
        _handle.IsEjected ? next() : _inner.InterceptAsync(context, next);
}
