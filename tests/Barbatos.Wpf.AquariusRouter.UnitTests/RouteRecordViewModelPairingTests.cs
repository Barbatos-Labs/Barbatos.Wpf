// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Aquarius.Composition;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Covers pairing a View with a ViewModel from the route table (<see cref="RouteRecord.ViewModel"/>),
/// the only option when both ship inside libraries nobody is allowed to edit - no <c>aq:Setup</c>
/// in the View's XAML, no attribute or base class on the ViewModel, nothing linking the two except
/// the route record itself.
/// </summary>
public class RouteRecordViewModelPairingTests
{
    [Fact]
    public void PairsAPristineViewWithAnUnrelatedViewModel()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/accounts/:id",
                Name = "account",
                View = typeof(global::Sealed.Library.PristineView),
                ViewModel = typeof(global::Sealed.Library.UnrelatedPresenter),
            };
            var router = new Router([record]);

            Assert.Null(router.Start("/accounts/SA-1").GetAwaiter().GetResult());

            var view = record.Instances["default"];
            Assert.IsType<global::Sealed.Library.UnrelatedPresenter>(view.DataContext);
        });
    }

    /// <summary>The paired ViewModel is a first-class participant: its route hooks fire like any other.</summary>
    [Fact]
    public void ThePairedViewModelStillReceivesRouteParams()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/accounts/:id",
                Name = "account",
                View = typeof(global::Sealed.Library.PristineView),
                ViewModel = typeof(global::Sealed.Library.UnrelatedPresenter),
            };
            var router = new Router([record]);

            Assert.Null(router.Start("/accounts/SA-1234").GetAwaiter().GetResult());

            var presenter = (global::Sealed.Library.UnrelatedPresenter)record.Instances["default"].DataContext;
            Assert.Equal("SA-1234", presenter.SeenId);
        });
    }

    [Fact]
    public void ResolvesThePairedViewModelThroughTheServiceProviderWhenThereIsOne()
    {
        StaThread.Run(() =>
        {
            var original = Setup.ServiceProvider;
            var provider = new FakeServiceProvider();
            var injected = new global::Sealed.Library.UnrelatedPresenter();
            provider.Register(typeof(global::Sealed.Library.UnrelatedPresenter), injected);
            Setup.ServiceProvider = provider;

            try
            {
                var record = new RouteRecord
                {
                    Path = "/accounts/:id",
                    Name = "account",
                    View = typeof(global::Sealed.Library.PristineView),
                    ViewModel = typeof(global::Sealed.Library.UnrelatedPresenter),
                };
                var router = new Router([record]);

                Assert.Null(router.Start("/accounts/SA-1").GetAwaiter().GetResult());

                Assert.Same(injected, record.Instances["default"].DataContext);
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }

    [Fact]
    public void PairsPerNamedOutlet()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/dashboard",
                Name = "dashboard",
                Views = new Dictionary<string, Type>
                {
                    ["default"] = typeof(global::Sealed.Library.PristineView),
                    ["sidebar"] = typeof(global::Sealed.Library.PristineSidebarView),
                },
                ViewModels = new Dictionary<string, Type>
                {
                    ["default"] = typeof(global::Sealed.Library.UnrelatedPresenter),
                    ["sidebar"] = typeof(global::Sealed.Library.UnrelatedSidebarPresenter),
                },
            };
            var router = new Router([record]);

            Assert.Null(router.Start("/dashboard").GetAwaiter().GetResult());

            Assert.IsType<global::Sealed.Library.UnrelatedPresenter>(record.Instances["default"].DataContext);
            Assert.IsType<global::Sealed.Library.UnrelatedSidebarPresenter>(record.Instances["sidebar"].DataContext);
        });
    }

    /// <summary>Only some outlets need pairing - the rest keep whatever their own View resolved.</summary>
    [Fact]
    public void LeavesUnpairedOutletsAlone()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/dashboard",
                Name = "dashboard",
                Views = new Dictionary<string, Type>
                {
                    ["default"] = typeof(global::Sealed.Library.PristineView),
                    ["sidebar"] = typeof(global::Sealed.Library.PristineSidebarView),
                },
                ViewModels = new Dictionary<string, Type> { ["default"] = typeof(global::Sealed.Library.UnrelatedPresenter) },
            };
            var router = new Router([record]);

            Assert.Null(router.Start("/dashboard").GetAwaiter().GetResult());

            Assert.IsType<global::Sealed.Library.UnrelatedPresenter>(record.Instances["default"].DataContext);
            Assert.Null(record.Instances["sidebar"].DataContext);
        });
    }

    /// <summary>
    /// A route-declared pairing is the composition root's deliberate decision, so it takes
    /// precedence over whatever the View wired up for itself - which is the whole point when the
    /// View's own wiring points at a ViewModel this particular route doesn't want.
    /// </summary>
    [Fact]
    public void ARouteDeclaredViewModelOverridesTheViewsOwnSetupWiring()
    {
        StaThread.Run(() =>
        {
            var record = new RouteRecord
            {
                Path = "/report",
                Name = "report",
                // This View type resolves ReportPresenter for itself via Setup.ViewModel below.
                View = typeof(global::Matching.Ns.ReportView),
                ViewModel = typeof(global::Sealed.Library.UnrelatedPresenter),
            };

            var original = Setup.ServiceProvider;
            var provider = new FakeServiceProvider();
            var selfWired = new global::Matching.Ns.ReportView();
            Setup.SetViewModel(selfWired, typeof(global::Matching.Ns.ReportPresenter));
            provider.Register(typeof(global::Matching.Ns.ReportView), selfWired);
            Setup.ServiceProvider = provider;

            try
            {
                var router = new Router([record]);
                Assert.Null(router.Start("/report").GetAwaiter().GetResult());

                Assert.IsType<global::Sealed.Library.UnrelatedPresenter>(record.Instances["default"].DataContext);
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }

    [Fact]
    public void SettingBothViewModelAndViewModelsIsRejectedWhenRegistered()
    {
        var record = new RouteRecord
        {
            Path = "/a",
            Name = "a",
            View = typeof(global::Sealed.Library.PristineView),
            ViewModel = typeof(global::Sealed.Library.UnrelatedPresenter),
            ViewModels = new Dictionary<string, Type> { ["default"] = typeof(global::Sealed.Library.UnrelatedPresenter) },
        };

        Assert.Throws<InvalidOperationException>(() => new RouteMatcher([record]));
    }

    /// <summary>A ViewModel declared for an outlet the record never renders would silently do nothing.</summary>
    [Fact]
    public void AViewModelForAnOutletWithNoViewIsRejectedWhenRegistered()
    {
        var record = new RouteRecord
        {
            Path = "/a",
            Name = "a",
            View = typeof(global::Sealed.Library.PristineView),
            ViewModels = new Dictionary<string, Type> { ["sidebar"] = typeof(global::Sealed.Library.UnrelatedSidebarPresenter) },
        };

        Assert.Throws<InvalidOperationException>(() => new RouteMatcher([record]));
    }

    [Fact]
    public void AViewModelOnARecordWithNoViewAtAllIsRejectedWhenRegistered()
    {
        var record = new RouteRecord
        {
            Path = "/group",
            Name = "group",
            ViewModel = typeof(global::Sealed.Library.UnrelatedPresenter),
        };

        Assert.Throws<InvalidOperationException>(() => new RouteMatcher([record]));
    }
}
