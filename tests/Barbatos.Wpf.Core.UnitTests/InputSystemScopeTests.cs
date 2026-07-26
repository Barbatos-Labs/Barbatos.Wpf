// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Barbatos.Wpf.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

/// <summary>
/// The one place InputSystem needs a real, focused <see cref="Window"/>: <see cref="InputActionMap.Scope"/>-based
/// gating, and confirming a <see cref="GlobalKeyBinding"/> still fires while the app itself is
/// genuinely focused (see <see cref="GlobalBindingFiresEvenWhileTheAppIsFocused"/> and
/// <see cref="InputSystemService"/>'s own remarks for why that's not obvious - an earlier draft
/// of this suite asserted the opposite, based on a wrong assumption about how
/// <c>RegisterHotKey</c> actually interacts with a focused window, until real testing showed
/// otherwise). Uses the same file-local <c>RunOnStaThread</c> helper shape as
/// <c>DialogServiceTests.cs</c>.
/// </summary>
public class InputSystemScopeTests
{
    [Fact]
    public void ScopedKeyBindingOnlyFiresWhileTheScopeElementHasFocus()
    {
        RunOnStaThread((service, window) =>
        {
            var focusTarget = new TextBox();
            var otherElement = new TextBox();
            var panel = new StackPanel();
            panel.Children.Add(focusTarget);
            panel.Children.Add(otherElement);
            window.Content = panel;
            window.Show();

            var map = new InputActionMap("Scoped", scope: focusTarget);
            map.AddAction("Save").AddKeyBinding(Key.S, ModifierKeys.Control);
            service.AddActionMap(map);
            var action = service.FindAction("Save")!;

            var performed = 0;
            action.Performed += (_, _) => performed++;

            otherElement.Focus();
            service.ProcessKeyDown(Key.S, ModifierKeys.Control);
            Assert.Equal(0, performed);

            focusTarget.Focus();
            service.ProcessKeyDown(Key.S, ModifierKeys.Control);
            Assert.Equal(1, performed);
        });
    }

    [Fact]
    public void UnscopedActionMapFiresRegardlessOfWhichElementHasFocus()
    {
        RunOnStaThread((service, window) =>
        {
            var element = new TextBox();
            window.Content = element;
            window.Show();
            element.Focus();

            var map = new InputActionMap("Global"); // no Scope.
            map.AddAction("Save").AddKeyBinding(Key.S, ModifierKeys.Control);
            service.AddActionMap(map);
            var action = service.FindAction("Save")!;

            var performed = 0;
            action.Performed += (_, _) => performed++;

            service.ProcessKeyDown(Key.S, ModifierKeys.Control);

            Assert.Equal(1, performed);
        });
    }

    [Fact]
    public void GlobalBindingFiresEvenWhileTheAppIsFocused()
    {
        RunOnStaThread((service, window, platform) =>
        {
            window.Content = new TextBox();
            window.Show();
            window.Activate();

            var map = new InputActionMap("Test");
            map.AddAction("QuickEntry")
                .AddKeyBinding(Key.F8, ModifierKeys.Control)
                .AddGlobalBinding(Key.F8, ModifierKeys.Control);
            service.AddActionMap(map);
            var action = service.FindAction("QuickEntry")!;

            var performedCount = 0;
            action.Performed += (_, _) => performedCount++;

            // The real WM_HOTKEY notification fires regardless of focus - simulated here via the
            // fake platform, exactly as it would while this window is the genuinely focused one.
            // Confirmed empirically (a real running sample app): once RegisterHotKey claims a
            // chord, Windows routes it into WM_HOTKEY exclusively, focused or not - the ordinary
            // WM_KEYDOWN a KeyBinding sibling for the identical chord relies on never arrives at
            // all while that registration holds, so there is no double-fire to guard against here.
            platform.RaiseHotkeyPressed(0);

            Assert.Equal(1, performedCount);
        });
    }

    [Fact]
    public void ScopedChordIsIgnoredWhileUnreachableAndSelfHealsViaItsOwnTimeout()
    {
        RunOnStaThread((service, window) =>
        {
            var focusTarget = new TextBox();
            var otherElement = new TextBox();
            var panel = new StackPanel();
            panel.Children.Add(focusTarget);
            panel.Children.Add(otherElement);
            window.Content = panel;
            window.Show();

            var map = new InputActionMap("Scoped", scope: focusTarget);
            map.AddAction("Chord").AddChordBinding(TimeSpan.FromMilliseconds(50), (Key.K, ModifierKeys.Control), (Key.C, ModifierKeys.Control));
            service.AddActionMap(map);
            var action = service.FindAction("Chord")!;

            var started = 0;
            var performed = 0;
            var canceled = 0;
            action.Started += (_, _) => started++;
            action.Performed += (_, _) => performed++;
            action.Canceled += (_, _) => canceled++;

            focusTarget.Focus();
            service.ProcessKeyDown(Key.K, ModifierKeys.Control); // progress -> 1, schedules a timeout.
            Assert.Equal(1, started);

            otherElement.Focus(); // the map is no longer reachable.
            service.ProcessKeyDown(Key.C, ModifierKeys.Control); // ignored entirely - the map is skipped.
            Assert.Equal(0, performed);

            // The timeout scheduled before focus moved away is not reachability-gated (see
            // InputSystemService's own design remarks) - it still fires and resets the stuck
            // progress on its own, even though focusTarget never got focus back.
            PumpFor(TimeSpan.FromMilliseconds(300));

            Assert.Equal(1, canceled);
        });
    }

    /// <summary>Pumps the current thread's real dispatcher for at least <paramref name="duration"/> - same pattern as <c>DispatchingTests.NonRepeatingTimerStopsAfterFirstTick</c>, safe to nest inside <see cref="RunOnStaThread(Action{IInputSystemService,Window})"/>'s own already-running dispatcher loop (an ordinary, supported nested <see cref="DispatcherFrame"/>, the same mechanism a modal dialog uses).</summary>
    static void PumpFor(TimeSpan duration)
    {
        var dispatcher = Barbatos.Wpf.Dispatching.Dispatcher.GetForCurrentThread()!;
        var pumpTimer = dispatcher.CreateTimer();
        pumpTimer.Interval = duration;
        pumpTimer.IsRepeating = false;

        var frame = new DispatcherFrame();
        pumpTimer.Tick += (_, _) => frame.Continue = false;
        pumpTimer.Start();

        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    /// <summary>
    /// Runs <paramref name="body"/> on a dedicated STA thread with a real, running
    /// <see cref="System.Windows.Threading.Dispatcher"/> - see <c>DialogServiceTests.RunOnStaThread</c>
    /// for why a real running dispatcher (not just an STA thread) is what's needed for
    /// <see cref="Window.Show"/>/focus to settle synchronously.
    /// </summary>
    static void RunOnStaThread(Action<IInputSystemService, Window, FakeHotkeyPlatform> body)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            dispatcher.BeginInvoke(new Action(() =>
            {
                Window? window = null;
                try
                {
                    var platform = new FakeHotkeyPlatform();
                    var builder = WpfApp.CreateBuilder();
                    builder.Services.AddSingleton<IHotkeyPlatform>(platform);
                    builder.ConfigureInputSystem();
                    var wpfApp = builder.Build();
                    var service = wpfApp.Services.GetRequiredService<IInputSystemService>();

                    window = new Window { ShowInTaskbar = false, Width = 200, Height = 200 };
                    body(service, window, platform);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    window?.Close();
                    dispatcher.InvokeShutdown();
                }
            }));

            System.Windows.Threading.Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
            throw failure;
    }

    static void RunOnStaThread(Action<IInputSystemService, Window> body) =>
        RunOnStaThread((service, window, _) => body(service, window));
}
