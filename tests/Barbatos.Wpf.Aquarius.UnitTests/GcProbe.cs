namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// Asserts that an object really became unreachable - the only way to prove a leak fix, since
/// the leaks this suite covers are all "something still holds a strong reference" rather than
/// anything observable through the object's own API.
/// </summary>
/// <remarks>
/// Deliberately kept to the two operations that are actually deterministic on CoreCLR: a full
/// blocking, compacting collection, and a finalizer drain in between two of them (an object with
/// a finalizer survives the first collection, so one pass is not enough). The retry loop covers
/// the one genuinely non-deterministic part - WPF posts work to the dispatcher that can still be
/// holding the graph when the first pass runs - rather than papering over a real leak: a
/// genuinely-leaked object fails every attempt just as reliably as it fails one.
/// <para>
/// The object under test must never be reachable from the calling frame. Take the
/// <see cref="WeakReference"/> inside a separate non-inlined method (see the tests' own
/// <c>[MethodImpl(MethodImplOptions.NoInlining)]</c> local helpers) so the JIT cannot keep a
/// live reference on the caller's stack - in Debug builds especially, a local is rooted for the
/// whole method body, which would make every one of these assertions fail for the wrong reason.
/// </para>
/// </remarks>
internal static class GcProbe
{
    public static void AssertCollected(WeakReference reference, string what)
    {
        ArgumentNullException.ThrowIfNull(reference);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            Collect();

            if (!reference.IsAlive)
                return;

            // Let anything WPF posted for later (Loaded-priority connection work, a
            // BeginInvoke'd fault rethrow, ...) drain before trying again.
            StaThread.PumpDispatcher();
        }

        Assert.Fail($"{what} was still reachable after 5 full collections - something is still holding a strong reference to it.");
    }

    public static void AssertAlive(WeakReference reference, string what)
    {
        ArgumentNullException.ThrowIfNull(reference);

        Collect();

        Assert.True(reference.IsAlive, $"{what} was collected, but something is supposed to be keeping it alive.");
    }

    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }
}
