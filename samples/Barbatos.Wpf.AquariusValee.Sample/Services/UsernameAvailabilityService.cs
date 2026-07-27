namespace Barbatos.Wpf.AquariusValee.Sample.Services;

/// <summary>
/// A fake "is this username taken" check - simulates a real API round-trip via
/// <see cref="Task.Delay(TimeSpan, CancellationToken)"/> so <c>CustomRulesViewModel</c>'s
/// <see cref="AsyncValidationRule{T}"/> has something genuine to await, exercising
/// <see cref="FieldMeta.Pending"/>.
/// </summary>
public sealed class UsernameAvailabilityService
{
    private static readonly string[] TakenUsernames = ["admin", "root", "test", "user"];

    public async Task<bool> IsAvailableAsync(string username, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        return !TakenUsernames.Contains(username.ToLowerInvariant());
    }
}
