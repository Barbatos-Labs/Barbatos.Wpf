// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.Aquarius.Composition;

namespace Barbatos.Wpf.AquariusRouter.UnitTests;

/// <summary>
/// Pins how a View finds its ViewModel when the two do not follow the tidy same-name,
/// same-namespace layout - the case a real app hits as soon as ViewModels live in their own
/// assembly or carry a different suffix.
/// </summary>
public class CrossAssemblyViewModelTests
{
    [Fact]
    public void ConventionResolvesWhenTheNamespaceQualifiedNameLinesUp()
    {
        Assert.Equal(typeof(global::Matching.Ns.WidgetViewModel), Setup.Resolver(typeof(global::Matching.Ns.WidgetView)));
    }

    /// <summary>
    /// The convention matches on the <b>namespace-qualified</b> name, so a ViewModel sitting in a
    /// different namespace is not found even though its short name lines up perfectly - the exact
    /// reason a cross-assembly pairing usually needs the explicit override.
    /// </summary>
    [Fact]
    public void ConventionDoesNotResolveAcrossADifferentNamespace()
    {
        Assert.Null(Setup.Resolver(typeof(global::Matching.Ns.GadgetView)));
    }

    [Fact]
    public void ConventionDoesNotResolveANonConventionalName()
    {
        Assert.Null(Setup.Resolver(typeof(global::Matching.Ns.ReportView)));
    }

    /// <summary>
    /// And when the convention finds nothing, the default is to leave <c>DataContext</c> alone
    /// rather than throw - worth knowing, because a mistyped pairing then fails as an empty View
    /// rather than an error. <c>Setup.ThrowOnUnresolved</c> opts into the loud version.
    /// </summary>
    [Fact]
    public void AnUnresolvedConventionLeavesDataContextNullByDefault()
    {
        StaThread.Run(() =>
        {
            var view = new global::Matching.Ns.ReportView();
            Setup.SetEnable(view, true);
            view.BeginInit();
            view.EndInit();

            Assert.Null(view.DataContext);
        });
    }

    /// <summary>
    /// The real cross-assembly answer: naming the type outright bypasses the convention entirely,
    /// and <see cref="Router"/> is none the wiser - it only ever constructs the View type and then
    /// reads back whatever <c>DataContext</c> that View resolved for itself, so route params and
    /// the navigation hooks reach a differently-named ViewModel exactly as they would a
    /// conventionally-named one.
    /// </summary>
    [Fact]
    public void AnExplicitlyNamedViewModelStillReceivesRouteParamsThroughTheRouter()
    {
        StaThread.Run(() =>
        {
            var original = Setup.ServiceProvider;
            var provider = new FakeServiceProvider();

            // The View resolves its DataContext by explicit type, exactly like the sample's
            // aq:Setup.ViewModel="{x:Type crossAssembly:AccountSummaryPresenter}".
            var presenter = new global::Matching.Ns.ReportPresenter();
            var view = new global::Matching.Ns.ReportView();
            Setup.SetViewModel(view, typeof(global::Matching.Ns.ReportPresenter));

            provider.Register(typeof(global::Matching.Ns.ReportPresenter), presenter);
            provider.Register(typeof(global::Matching.Ns.ReportView), view);
            Setup.ServiceProvider = provider;

            try
            {
                view.BeginInit();
                view.EndInit();
                Assert.Same(presenter, view.DataContext);

                var router = new Router([
                    new RouteRecord { Path = "/accounts/:id", Name = "account", View = typeof(global::Matching.Ns.ReportView) },
                ]);

                Assert.Null(router.Start("/accounts/SA-1234").GetAwaiter().GetResult());

                Assert.Equal("SA-1234", presenter.SeenAccountId);
            }
            finally
            {
                Setup.ServiceProvider = original;
            }
        });
    }
}
