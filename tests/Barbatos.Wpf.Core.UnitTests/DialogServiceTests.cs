// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows;
using Barbatos.Wpf.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Core.UnitTests;

public class DialogServiceTests
{
    [Fact]
    public void FeatureIsNotRegisteredByDefault()
    {
        var wpfApp = WpfApp.CreateBuilder().Build();

        Assert.Null(wpfApp.Services.GetService<IDialogService>());
    }

    [Fact]
    public void FeatureIsRegisteredWhenConfigured()
    {
        var wpfApp = BuildApp();

        Assert.NotNull(wpfApp.Services.GetService<IDialogService>());
    }

    [Fact]
    public void CascadeCloseOwnedDialogsDefaultsToTrue()
    {
        Assert.True(new DialogOptions().CascadeCloseOwnedDialogs);
    }

    [Fact]
    public void ActiveWindowIsNullWithNoWindowsShown()
    {
        var wpfApp = BuildApp();
        var service = wpfApp.Services.GetRequiredService<IDialogService>();

        Assert.Null(service.ActiveWindow);
    }

    [Fact]
    public void ShowSetsTheExplicitOwner()
    {
        RunOnStaThread(service =>
        {
            var owner = NewWindow();
            owner.Show();

            var dialog = NewWindow();
            service.Show(dialog, owner: owner);

            Assert.Same(owner, dialog.Owner);

            dialog.Close();
            owner.Close();
        });
    }

    [Fact]
    public void ShowFallsBackToActiveWindowWhenNoOwnerIsGiven()
    {
        RunOnStaThread(service =>
        {
            var first = NewWindow();
            service.Show(first, key: "first");

            var second = NewWindow();
            service.Show(second, key: "second");

            // With no explicit owner, the second dialog should be owned by ActiveWindow
            // (whichever this service most recently saw activated) rather than being ownerless.
            Assert.NotNull(second.Owner);

            second.Close();
            first.Close();
        });
    }

    [Fact]
    public void ShowActivatesAnExistingDialogInsteadOfOpeningADuplicate()
    {
        RunOnStaThread(service =>
        {
            var dialog = NewWindow();

            var firstShow = service.Show(dialog, key: "duplicate-test");
            // Simulates a rapid double-click: the *same underlying* Show(dialog, ...) call
            // for the same key must not attempt to show the already-open window again.
            var secondShow = service.Show(dialog, key: "duplicate-test");

            Assert.True(firstShow);
            Assert.False(secondShow);
            Assert.True(service.IsOpen("duplicate-test"));

            dialog.Close();
        });
    }

    [Fact]
    public void ShowUsesTheWindowTypeAsTheDefaultKey()
    {
        RunOnStaThread(service =>
        {
            var first = NewWindow();
            var second = NewWindow();

            var firstShow = service.Show(first);
            var secondShow = service.Show(second);

            Assert.True(firstShow);
            // Both are plain System.Windows.Window instances, so they share the same default
            // key (the type's full name) - the second Show() must be treated as a duplicate.
            Assert.False(secondShow);
            Assert.Same(first, service.GetOpenDialog(typeof(Window).FullName!));

            first.Close();
        });
    }

    [Fact]
    public void ShowWithDifferentKeysAllowsMultipleInstancesOfTheSameType()
    {
        RunOnStaThread(service =>
        {
            var first = NewWindow();
            var second = NewWindow();

            var firstShow = service.Show(first, key: "entity-1");
            var secondShow = service.Show(second, key: "entity-2");

            Assert.True(firstShow);
            Assert.True(secondShow);
            Assert.True(service.IsOpen("entity-1"));
            Assert.True(service.IsOpen("entity-2"));

            first.Close();
            second.Close();
        });
    }

    [Fact]
    public void ShowWithCloseOthersClosesEveryOtherTrackedDialog()
    {
        RunOnStaThread(service =>
        {
            var first = NewWindow();
            service.Show(first, key: "first");

            var second = NewWindow();
            service.Show(second, key: "second", closeOthers: true);

            Assert.False(service.IsOpen("first"));
            Assert.True(service.IsOpen("second"));

            second.Close();
        });
    }

    [Fact]
    public void CloseAllRespectsAClosingVeto()
    {
        RunOnStaThread(service =>
        {
            var vetoing = NewWindow();
            var vetoActive = true;
            vetoing.Closing += (s, e) => e.Cancel = vetoActive;
            service.Show(vetoing, key: "vetoing");

            var normal = NewWindow();
            service.Show(normal, key: "normal");

            var allClosed = service.CloseAll();

            Assert.False(allClosed);
            Assert.True(service.IsOpen("vetoing"));
            Assert.False(service.IsOpen("normal"));

            // Let it actually close now so the test doesn't leak a real window.
            vetoActive = false;
            vetoing.Close();
        });
    }

    [Fact]
    public void CloseClosesOnlyTheDialogWithTheGivenKey()
    {
        RunOnStaThread(service =>
        {
            var first = NewWindow();
            service.Show(first, key: "first");

            // Explicit, unrelated owner: with no owner argument, "second" would default to
            // ActiveWindow - which, since "first" was just shown (and thus activated), would
            // be "first" itself. That would make "second" owned by "first", entangling this
            // test with the owner-cascade behavior covered separately below.
            var unrelatedOwner = NewWindow();
            unrelatedOwner.Show();
            var second = NewWindow();
            service.Show(second, owner: unrelatedOwner, key: "second");

            var closed = service.Close("first");

            Assert.True(closed);
            Assert.False(service.IsOpen("first"));
            Assert.True(service.IsOpen("second"));

            second.Close();
            unrelatedOwner.Close();
        });
    }

    [Fact]
    public void CloseReturnsTrueWhenNoDialogIsOpenUnderTheKey()
    {
        RunOnStaThread(service =>
        {
            Assert.True(service.Close("does-not-exist"));
        });
    }

    [Fact]
    public void ClosingAnOwnerCascadesToItsOwnedDialogs()
    {
        RunOnStaThread(service =>
        {
            var owner = NewWindow();
            service.Show(owner, key: "owner");

            var owned = NewWindow();
            service.Show(owned, owner: owner, key: "owned");

            service.Close("owner");

            // CascadeCloseOwnedDialogs defaults to true: closing the owner must close the
            // dialog it owns too, instead of leaving it open with a dead Owner reference.
            Assert.False(service.IsOpen("owned"));
        });
    }

    [Fact]
    public void ClosingAnOwnerRespectsAVetoOnAnOwnedDialog()
    {
        RunOnStaThread(service =>
        {
            var owner = NewWindow();
            service.Show(owner, key: "owner");

            var owned = NewWindow();
            var vetoActive = true;
            owned.Closing += (s, e) => e.Cancel = vetoActive;
            service.Show(owned, owner: owner, key: "owned");

            var ownerClosed = service.Close("owner");

            // Plain WPF Window.Owner semantics close owned windows unconditionally when their
            // owner closes - ignoring the owned window's own Closing veto entirely. That is
            // exactly the data-loss risk CascadeCloseOwnedDialogs (default: true) exists to
            // prevent: closing the owner must itself be blocked when an owned dialog vetoes,
            // rather than tearing the owned dialog down anyway.
            Assert.False(ownerClosed);
            Assert.True(service.IsOpen("owner"));
            Assert.True(service.IsOpen("owned"));

            vetoActive = false;
            owned.Close();
            owner.Close();
        });
    }

    [Fact]
    public void CascadeCloseCanBeDisabled()
    {
        RunOnStaThread(service =>
        {
            var owner = NewWindow();
            service.Show(owner, key: "owner");

            var owned = NewWindow();
            service.Show(owned, owner: owner, key: "owned");

            service.Close("owner");

            // With CascadeCloseOwnedDialogs = false, this service does not proactively close
            // owned dialogs - but plain WPF Window.Owner semantics still close them once the
            // owner actually closes (ignoring their own Closing veto), so "owned" ends up
            // closed here too, just via WPF's own mechanism rather than this service's.
            Assert.False(service.IsOpen("owned"));
        }, options => options.CascadeCloseOwnedDialogs = false);
    }

    [Fact]
    public void GetOpenDialogReturnsNullWhenNothingIsTrackedUnderTheKey()
    {
        var wpfApp = BuildApp();
        var service = wpfApp.Services.GetRequiredService<IDialogService>();

        Assert.Null(service.GetOpenDialog("does-not-exist"));
    }

    static Window NewWindow() => new() { ShowInTaskbar = false, Width = 1, Height = 1 };

    static WpfApp BuildApp(Action<DialogOptions>? configure = null)
    {
        var builder = WpfApp.CreateBuilder();
        builder.ConfigureDialogs(configure);
        return builder.Build();
    }

    /// <summary>
    /// Runs <paramref name="body"/> on a dedicated STA thread with a real <see cref="IDialogService"/>,
    /// inside an actual running <see cref="System.Windows.Threading.Dispatcher.Run()"/> loop
    /// (the test body itself runs as a dispatched callback). WPF windows can only be
    /// created/shown from an STA thread, and — unlike a bare STA thread with no message pump —
    /// a real running dispatcher is what makes <c>Window.Show()</c>/<c>Close()</c> settle
    /// synchronously the same way they do in a normal running app, instead of leaving
    /// Activated/Closed as queued-but-unprocessed work. Exceptions (including failed
    /// assertions) raised inside <paramref name="body"/> are captured and re-thrown on the
    /// calling thread so xUnit reports them correctly.
    /// </summary>
    static void RunOnStaThread(Action<IDialogService> body, Action<DialogOptions>? configure = null)
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;

            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    var wpfApp = BuildApp(configure);
                    var service = wpfApp.Services.GetRequiredService<IDialogService>();

                    body(service);
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
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
}
