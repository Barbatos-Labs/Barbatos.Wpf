using Barbatos.Wpf.AquariusRouter.Routing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.Samples.Shared.CrossAssembly;

/// <summary>
/// The "ViewModel" half of a deliberately awkward pairing: its View
/// (<c>Barbatos.Wpf.AquariusRouter.Sample.Views.CrossAssembly.AccountSummaryView</c>) lives in a
/// <b>different assembly</b>, under a <b>different namespace</b>, and this type's name doesn't
/// follow the <c>XxxView</c> -&gt; <c>XxxViewModel</c> convention at all (<c>Presenter</c>, not
/// <c>ViewModel</c>). It is still resolved, still gets its route params, and still participates in
/// the navigation pipeline.
/// </summary>
/// <remarks>
/// <b>Neither half is wired to the other.</b> This type names no View; the View carries no
/// <c>aq:Setup</c> attribute and names no ViewModel. They stand in for types shipped inside
/// libraries under strict change control, where "just add an attribute to the View" is not an
/// option. The pairing is declared once in the route table instead:
/// <code>
/// new RouteRecord
/// {
///     Path = "/accounts/:id", Name = "account-summary",
///     View = typeof(AccountSummaryView),
///     ViewModel = typeof(AccountSummaryPresenter),
/// }
/// </code>
/// <c>Router</c> constructs the View, then assigns this type as its <c>DataContext</c> - resolving
/// it through the app's own container when there is one, so constructor injection works normally.
/// Everything downstream is unchanged: this type implements <see cref="IOnRouteEnter"/> and
/// receives <c>/accounts/:id</c>'s own <c>id</c> param exactly like a ViewModel the View had wired
/// up itself.
/// <para>
/// <b>Why the naming convention can't do this job</b>: <c>aq:Setup.Enable="True"</c> matches on
/// the <i>namespace-qualified</i> name (<c>Some.Ns.FooView</c> -&gt; <c>Some.Ns.FooViewModel</c>),
/// checking the View's own assembly first and then every loaded assembly. A different assembly
/// alone is fine - a different <i>namespace</i> or a different <i>name</i> is not, and by default
/// that failure is silent (<c>DataContext</c> is simply left null; set
/// <c>Setup.ThrowOnUnresolved = true</c> to make it loud). This pairing breaks it on both counts,
/// and neither file may be edited to say so.
/// </para>
/// </remarks>
public sealed partial class AccountSummaryPresenter : ObservableObject, IOnRouteEnter
{
    [ObservableProperty]
    private string _accountId = "";

    [ObservableProperty]
    private string _resolvedFrom = "";

    public void OnRouteEnter(RouteLocation to, RouteLocation from)
    {
        AccountId = to.Params.TryGetValue("id", out var id) ? id as string ?? "" : "";

        // Proves at runtime - not just in a comment - that this instance really did come from a
        // different assembly than the View that is displaying it.
        ResolvedFrom = GetType().Assembly.GetName().Name ?? "unknown";
    }
}
