using System.Net.Http;
using Barbatos.Wpf.Apsu;
using Barbatos.Wpf.Aquarius.Composition;
using Barbatos.Wpf.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// Composes the <see cref="WpfApp"/> host, mirroring .NET MAUI's <c>MauiProgram</c> pattern (and
/// <c>Barbatos.Wpf.Core.Sample</c>'s own <c>WpfProgram</c>).
/// </summary>
public static class WpfProgram
{
    public static WpfApp CreateWpfApp()
    {
        var builder = WpfApp.CreateBuilder();

        // Shared by UserStore/OrderStore for real network calls to a free fake REST API, instead
        // of Task.Delay simulations - a single long-lived HttpClient (not one per call/request),
        // the standard guidance for avoiding socket exhaustion.
        builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri("https://jsonplaceholder.typicode.com/") });

        // The single shared sink every interceptor below reports into (see its own remarks for
        // why) - DI-constructed (it has no dependencies of its own), so every interceptor below
        // just asks for it via the container instead of a local variable being threaded through
        // by hand.
        builder.Services.AddSingleton<ApsuActivityLog>();

        // The full 3-global + 3-local interceptor chain this session's own unit tests
        // (StoreActionInterceptorTests.SixChainedInterceptorsAllRunInOrderWhenEveryOneCallsNext /
        // AnyInterceptorBlockingInASixDeepChainPreventsTheActionAndEverythingAfterIt) verify in
        // the abstract - this is the same shape with real, distinct responsibilities. A store reads
        // its interceptors (and plugins) when it is first resolved, not when AddStore<T>() runs,
        // so these could just as well come after the AddStore calls below; only their order among
        // themselves matters, since that is the chain order. Only registered as
        // IStoreActionInterceptor/keyed - nothing outside this method ever needs an interceptor
        // instance directly, the same way a Pinia plugin is never imported into a component once
        // pinia.use(plugin) has registered it.
        //
        // Four of the six need nothing but ApsuActivityLog, so DI constructs them outright - no
        // `new` anywhere for these. The other two (timing warning, checkout cooldown) also need a
        // plain TimeSpan, which the container has no way to invent on its own - a factory is the
        // normal way any DI container handles "this one value comes from code, not a service",
        // not a limitation specific to this library.

        // Global #1-#3: every one wraps every action on every store, in this registration order
        // (outermost to innermost) - see each type for what it does.
        builder.Services.AddSingleton<IStoreActionInterceptor, ApsuActionLoggingInterceptor>();

        // 200ms: a single JSONPlaceholder round-trip is typically well under this, but
        // CheckoutAsync's own call also awaits UserStore.RefreshProfileAsync's separate
        // round-trip afterward, so the *composite* call crosses it more often than either alone.
        // Real (if fake-API) network latency now, not a fixed Task.Delay - the threshold is
        // measuring something genuinely variable.
        builder.Services.AddSingleton<IStoreActionInterceptor>(sp =>
            new ApsuTimingWarningInterceptor(TimeSpan.FromMilliseconds(200), sp.GetRequiredService<ApsuActivityLog>()));

        builder.Services.AddSingleton<IStoreActionInterceptor, ApsuAuditTrailInterceptor>();

        // Barbatos.Wpf.Apsu stores - see UserStore/OrderStore for the cross-store scenarios
        // (watch, explicit cross-store action calls, a getter combining both) they demonstrate.
        // OrderStore's constructor depends on UserStore, so both must be registered; which one
        // is registered first does not matter. UserStore is always resolved first - MainViewModel's
        // constructor takes it first, and OrderStore's constructor needs it anyway - so
        // UserSessionPersistencePlugin restores the last selected user before OrderStore exists
        // (see OrderStore's constructor for how it still loads that user's orders). Each
        // store's own local interceptors are declared right inside its own AddStore call, one
        // call total per store instead of a separate AddStoreInterceptor call per interceptor
        // plus a trailing AddStore.
        builder.Services.AddSingleton<IStorePlugin, UserSessionPersistencePlugin>();

        // Local: UserStore.SelectUser only.
        builder.Services.AddStore<UserStore>(i => i.Add<UserIdValidationInterceptor>());

        // Local: both wrap OrderStore.CheckoutAsync, stacked (cartGuard outer, checkoutCooldown
        // inner, per Add() order) on top of the 3 global ones above - local interceptors compose
        // with each other exactly like global ones do, not just with the global layer. cartGuard
        // deliberately added first: it checks a precondition and should run before
        // checkoutCooldown spends a cooldown "token" on a call that was going to be rejected
        // anyway - otherwise, repeatedly clicking Checkout on an empty cart would eventually get
        // blocked by the cooldown instead of the (more specific, more useful) empty-cart message.
        builder.Services.AddStore<OrderStore>(i => i
            .Add<NonEmptyCartGuardInterceptor>()
            .Add(sp => new CheckoutCooldownInterceptor(TimeSpan.FromSeconds(3), sp.GetRequiredService<ApsuActivityLog>())));

        // The KeepAlive demo. AddAquarius() is the DI entry point for the library's own options;
        // UseAquarius() below is what points its markup-driven type resolution (aq:Setup.Enable,
        // Component.Is) at this container.
        builder.Services.AddAquarius();
        builder.Services.AddSingleton<KeepAliveActivityLog>();

        // Transient, not Singleton: KeepAlive builds a section View the first time it is shown
        // and again after an eviction, and each of those genuinely wants its own fresh instance -
        // a Singleton registration would silently hand back the evicted one, state and all, which
        // is precisely the behavior the eviction demo is meant to show the absence of.
        builder.Services.AddTransient<ProfileSectionView>();
        builder.Services.AddTransient<OrdersSectionView>();
        builder.Services.AddTransient<ReportSectionView>();
        builder.Services.AddTransient<ProfileSectionViewModel>();
        builder.Services.AddTransient<OrdersSectionViewModel>();
        builder.Services.AddTransient<ReportSectionViewModel>();

        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        var app = builder.Build();
        app.Services.UseAquarius();

        return app;
    }
}
