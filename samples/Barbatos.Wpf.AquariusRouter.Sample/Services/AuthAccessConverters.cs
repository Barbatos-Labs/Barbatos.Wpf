using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Barbatos.Wpf.AquariusRouter.Sample.Services;

/// <summary>
/// <see cref="Visibility"/>: <see cref="Visibility.Collapsed"/> while <see cref="AuthState.IsLoggedIn"/>
/// is <see langword="false"/>, <see cref="Visibility.Visible"/> otherwise - regardless of
/// <see cref="AuthState.IsPremium"/>. Bound onto every <c>RouterLink</c> in <c>MainWindow.xaml</c>'s
/// nav bar: nothing is even visible until logged in; a premium-only link then stays visible but
/// disabled (see <see cref="PremiumAccessConverter"/>) rather than vanishing, which is what
/// actually tells a free-plan user the feature exists at all.
/// </summary>
public sealed class LoggedInVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(LoggedInVisibilityConverter)} is one-way.");
}

/// <summary>
/// The inverse of <see cref="LoggedInVisibilityConverter"/>: visible only while logged out. Bound
/// onto the one nav entry that must stay reachable precisely when every other <c>RouterLink</c> is
/// hidden - "Log In" - so there is still a way to reach <c>/login</c> instead of a dead end.
/// </summary>
public sealed class LoggedOutVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(LoggedOutVisibilityConverter)} is one-way.");
}

/// <summary>
/// <see langword="bool"/> <c>IsEnabled</c> for a premium-only <c>RouterLink</c>: <see langword="true"/>
/// only once both bound values - <see cref="AuthState.IsLoggedIn"/> and <see cref="AuthState.IsPremium"/> -
/// are <see langword="true"/>. A <see cref="MultiBinding"/> rather than two separate converters
/// since disabling correctly needs both checked together, not either alone - a logged-out user
/// never reaches this at all (see <see cref="LoggedInVisibilityConverter"/> hiding the whole
/// button first), but a click that raced past that (e.g. a saved deep link) still lands on the
/// real, server-side-equivalent guard (<c>AppNavigationGuards</c>'s <c>requiresPremium</c> check)
/// either way - this converter is a UX hint, not the actual enforcement.
/// </summary>
public sealed class PremiumAccessConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [true, true];

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(PremiumAccessConverter)} is one-way.");
}
