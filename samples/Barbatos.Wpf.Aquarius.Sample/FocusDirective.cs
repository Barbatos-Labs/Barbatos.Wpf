using System.Windows;
using Barbatos.Wpf.Xaml;

namespace Barbatos.Wpf.Aquarius.Sample;

/// <summary>
/// The canonical Vue custom-directive example (<c>v-focus</c>) from Vue's own docs,
/// ported: focuses the target element as soon as it mounts. Registered as a resource and
/// attached with <c>aq:Directives.Use="{StaticResource AutoFocus}"</c>.
/// </summary>
public sealed class FocusDirective : Directive
{
    public override void Mounted(FrameworkElement element, DirectiveBinding binding) => element.Focus();
}
