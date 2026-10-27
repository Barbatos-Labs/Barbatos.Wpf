// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Diagnostics;
using Barbatos.Wpf.Apsu;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// A second <b>global</b> <see cref="IStoreActionInterceptor"/> (see <see cref="ApsuActionLoggingInterceptor"/>
/// for the first) - registered right after it in <see cref="WpfProgram.CreateWpfApp"/>, so it
/// wraps every action on every store too, one layer further in. Deliberately a separate,
/// narrowly-focused interceptor rather than folded into the logging one, the same way a real app
/// would keep "log everything" and "warn on slow calls" as independent, composable concerns.
/// </summary>
public sealed class ApsuTimingWarningInterceptor : IStoreActionInterceptor
{
    readonly TimeSpan _warnThreshold;
    readonly ApsuActivityLog _activityLog;

    public ApsuTimingWarningInterceptor(TimeSpan warnThreshold, ApsuActivityLog activityLog)
    {
        _warnThreshold = warnThreshold;
        _activityLog = activityLog;
    }

    public async Task<object?> InterceptAsync(StoreActionContext context, StoreActionDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await next();

        if (stopwatch.Elapsed > _warnThreshold)
        {
            // BaseType: context.Store is the generated proxy subclass, not the store's own type.
            _activityLog.Log(
                $"[Global interceptor #2 - timing] {context.Store.GetType().BaseType!.Name}.{context.ActionName}() took " +
                $"{stopwatch.Elapsed.TotalMilliseconds:0}ms (over the {_warnThreshold.TotalMilliseconds:0}ms warning threshold)");
        }

        return result;
    }
}
