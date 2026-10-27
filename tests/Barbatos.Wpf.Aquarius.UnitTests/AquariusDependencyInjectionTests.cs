// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Aquarius.UnitTests;

/// <summary>
/// <c>services.AddAquarius()</c> / <c>provider.UseAquarius()</c> - the DI-shaped way to configure
/// Aquarius, replacing the loose static assignments an app used to have to remember after
/// <c>Build()</c>.
/// </summary>
/// <remarks>
/// The statics these end up writing (<see cref="Setup.ServiceProvider"/> and friends) are
/// process-wide, so every test here saves and restores them - the same discipline
/// <c>SetupTests</c> already follows.
/// </remarks>
public class AquariusDependencyInjectionTests
{
    [Fact]
    public void UseAquariusPointsMarkupDrivenResolutionAtTheContainer()
    {
        WithRestoredStatics(() =>
        {
            var services = new ServiceCollection();
            services.AddAquarius();
            services.AddSingleton<SetupProbeViewModel>();

            using var provider = services.BuildServiceProvider();
            Assert.Same(provider, provider.UseAquarius());

            // The whole point: a type named in XAML (aq:Setup.ViewModel, Component.Is)
            // now comes out of the app's own container.
            Assert.Same(provider.GetRequiredService<SetupProbeViewModel>(), Setup.Resolve(typeof(SetupProbeViewModel)));
        });
    }

    [Fact]
    public void OptionsConfiguredAtRegistrationAreAppliedByUseAquarius()
    {
        WithRestoredStatics(() =>
        {
            var services = new ServiceCollection();
            services.AddAquarius(options =>
            {
                options.ThrowOnUnresolvedViewModels = true;
                options.ThrowOnUnresolvedExprIdentifiers = true;
                options.ViewModelResolver = _ => typeof(SetupProbeViewModel);
            });

            using var provider = services.BuildServiceProvider();
            provider.UseAquarius();

            Assert.True(Setup.ThrowOnUnresolved);
            Assert.True(Expr.ThrowOnUnresolvedIdentifiers);
            Assert.Equal(typeof(SetupProbeViewModel), Setup.Resolver(typeof(ContentControl)));
        });
    }

    [Fact]
    public void UseAquariusStillWiresTheProviderWhenAddAquariusWasNeverCalled()
    {
        // An app with nothing to configure should still be able to opt into DI-resolved
        // ViewModels with one call, so the options lookup is optional rather than required.
        WithRestoredStatics(() =>
        {
            var services = new ServiceCollection();
            services.AddSingleton<SetupProbeViewModel>();

            using var provider = services.BuildServiceProvider();
            provider.UseAquarius();

            Assert.Same(provider, Setup.ServiceProvider);
        });
    }

    [Fact]
    public void AComponentsViewComesOutOfTheContainer()
    {
        WithRestoredStatics(() => StaThread.Run(() =>
        {
            var services = new ServiceCollection();
            services.AddAquarius();
            services.AddSingleton<KeepAliveProbeViewA>();

            using var provider = services.BuildServiceProvider();
            provider.UseAquarius();

            var component = new Component();
            var keepAlive = new KeepAlive { Child = component };
            var window = new System.Windows.Window { Content = keepAlive, Width = 200, Height = 200 };
            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                component.Is = typeof(KeepAliveProbeViewA);
                StaThread.PumpDispatcher();

                Assert.Same(provider.GetRequiredService<KeepAliveProbeViewA>(), keepAlive.ActiveView);
            }
            finally
            {
                window.Close();
                StaThread.PumpDispatcher();
            }
        }));
    }

    private static void WithRestoredStatics(Action body)
    {
        var provider = Setup.ServiceProvider;
        var resolver = Setup.Resolver;
        var throwOnUnresolved = Setup.ThrowOnUnresolved;
        var throwOnExpr = Expr.ThrowOnUnresolvedIdentifiers;

        try
        {
            body();
        }
        finally
        {
            Setup.ServiceProvider = provider;
            Setup.Resolver = resolver;
            Setup.ThrowOnUnresolved = throwOnUnresolved;
            Expr.ThrowOnUnresolvedIdentifiers = throwOnExpr;
        }
    }
}
