// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// <see cref="TeleportHost"/> with more than one UI thread - an app that runs a second window on
/// its own thread, or this suite, where every test mounts its controls on a fresh STA thread.
/// </summary>
/// <remarks>
/// The registry and its events used to be process-wide. A host loading on one thread then ran
/// every other thread's <see cref="Teleport"/> handler on the host's thread, where reading
/// <see cref="Teleport.To"/> throws. On CI the other thread was one that had already exited:
/// <c>LifecycleLeakTests</c> deliberately leaves a <see cref="Teleport"/> loaded, so it never
/// unsubscribed, and the next test's host threw from inside its <c>Loaded</c> handler and hung
/// the whole run.
/// </remarks>
public class TeleportThreadingTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(30);

    [Fact]
    public void AHostLoadingOnAnotherThreadNeitherThrowsNorReachesThisThreadsTeleport()
    {
        var hostName = $"Host-{Guid.NewGuid()}";
        using var teleportLoaded = new ManualResetEventSlim();
        using var hostLoaded = new ManualResetEventSlim();
        using var teleportChecked = new ManualResetEventSlim();

        Exception? otherThreadFailure = null;
        var stayedLocal = false;
        var sawTheOtherThreadsHost = true;

        // The Teleport waits on its own thread, subscribed to HostRegistered, for a host that
        // only ever loads on the test's thread.
        var otherThread = new Thread(() =>
        {
            try
            {
                var content = new Border();
                var teleport = new Teleport { To = hostName, Content = content };
                var window = new Window { Content = teleport, Width = 200, Height = 200 };
                window.Show();
                StaThread.PumpDispatcher();
                teleportLoaded.Set();

                hostLoaded.Wait(WaitLimit);
                StaThread.PumpDispatcher();
                stayedLocal = ReferenceEquals(teleport.Content, content);
                sawTheOtherThreadsHost = TeleportHost.TryGetHost(hostName, out _);
                teleportChecked.Set();

                window.Close();
                StaThread.PumpDispatcher();
            }
            catch (Exception ex)
            {
                otherThreadFailure = ex;
            }
            finally
            {
                teleportLoaded.Set();
                teleportChecked.Set();
            }
        });

        otherThread.SetApartmentState(ApartmentState.STA);
        otherThread.Start();

        try
        {
            Assert.True(teleportLoaded.Wait(WaitLimit));

            StaThread.Run(() =>
            {
                // Captured rather than left to escape: before the fix the cross-thread read threw
                // inside this window's Loaded, and an unhandled dispatcher exception takes the
                // test host down instead of failing this test.
                Exception? escaped = null;
                Dispatcher.CurrentDispatcher.UnhandledException += (_, e) =>
                {
                    escaped ??= e.Exception;
                    e.Handled = true;
                };

                var host = new StackPanel();
                TeleportHost.SetRegisterHost(host, hostName);
                var mine = new Border();
                var local = new Teleport { To = hostName, Content = mine };
                var window = new Window { Content = new StackPanel { Children = { host, local } }, Width = 200, Height = 200 };

                window.Show();
                StaThread.PumpDispatcher();

                hostLoaded.Set();
                Assert.True(teleportChecked.Wait(WaitLimit));

                // A Teleport on the host's own thread is unaffected.
                Assert.Same(mine, Assert.Single(host.Children));

                window.Close();
                StaThread.PumpDispatcher();

                Assert.Null(escaped);
            });
        }
        finally
        {
            hostLoaded.Set();
            Assert.True(otherThread.Join(WaitLimit));
        }

        Assert.Null(otherThreadFailure);
        Assert.True(stayedLocal);
        Assert.False(sawTheOtherThreadsHost);
    }

    [Fact]
    public void ATeleportLeftLoadedOnAThreadThatHasExitedDoesNotBreakTheNextHost()
    {
        var hostName = $"Host-{Guid.NewGuid()}";

        // The CI case exactly: never closed, so its Teleport stays subscribed to HostRegistered
        // after its thread is gone. (It stays pinned for the rest of the run too, which is the
        // price LifecycleLeakTests already pays for its AssertAlive control.)
        StaThread.Run(() =>
        {
            var window = new Window { Content = new Teleport { To = hostName, Content = new Border() }, Width = 200, Height = 200 };
            window.Show();
            StaThread.PumpDispatcher();
        });

        StaThread.Run(() =>
        {
            Exception? escaped = null;
            Dispatcher.CurrentDispatcher.UnhandledException += (_, e) =>
            {
                escaped ??= e.Exception;
                e.Handled = true;
            };

            var host = new Grid();
            TeleportHost.SetRegisterHost(host, hostName);
            var window = new Window { Content = host, Width = 200, Height = 200 };

            window.Show();
            StaThread.PumpDispatcher();
            window.Close();
            StaThread.PumpDispatcher();

            Assert.Null(escaped);
            Assert.Empty(host.Children);
        });
    }

    [Fact]
    public void TheSameHostNameOnTwoThreadsGivesEachThreadItsOwnHost()
    {
        var hostName = $"Host-{Guid.NewGuid()}";
        using var firstLoaded = new ManualResetEventSlim();
        using var secondDone = new ManualResetEventSlim();

        Exception? firstThreadFailure = null;
        Panel? firstThreadsHost = null;
        var firstStillFindsItsOwn = false;

        // Names are unique per thread now, like a DOM id per document: a second window on its
        // own thread reusing "Overlay" must not take the first thread's host away from it.
        var firstThread = new Thread(() =>
        {
            try
            {
                var host = new Grid();
                TeleportHost.SetRegisterHost(host, hostName);
                var window = new Window { Content = host, Width = 200, Height = 200 };
                window.Show();
                StaThread.PumpDispatcher();
                firstThreadsHost = host;
                firstLoaded.Set();

                secondDone.Wait(WaitLimit);
                firstStillFindsItsOwn = TeleportHost.TryGetHost(hostName, out var found) && ReferenceEquals(found, host);

                window.Close();
                StaThread.PumpDispatcher();
            }
            catch (Exception ex)
            {
                firstThreadFailure = ex;
            }
            finally
            {
                firstLoaded.Set();
            }
        });

        firstThread.SetApartmentState(ApartmentState.STA);
        firstThread.Start();

        try
        {
            Assert.True(firstLoaded.Wait(WaitLimit));

            StaThread.Run(() =>
            {
                var host = new Grid();
                TeleportHost.SetRegisterHost(host, hostName);
                var content = new Border();
                var teleport = new Teleport { To = hostName, Content = content };
                var window = new Window { Content = new StackPanel { Children = { host, teleport } }, Width = 200, Height = 200 };

                window.Show();
                StaThread.PumpDispatcher();

                Assert.True(TeleportHost.TryGetHost(hostName, out var found));
                Assert.Same(host, found);
                Assert.Same(content, Assert.Single(host.Children));

                window.Close();
                StaThread.PumpDispatcher();
            });
        }
        finally
        {
            secondDone.Set();
            Assert.True(firstThread.Join(WaitLimit));
        }

        Assert.Null(firstThreadFailure);
        Assert.NotNull(firstThreadsHost);
        Assert.True(firstStillFindsItsOwn);
    }
}
