using System.Windows.Controls;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class SuspenseTests
{
    [Fact]
    public void DefaultsToNotPendingAndShowsChild()
    {
        StaThread.Run(() =>
        {
            var child = new Border();

            var suspense = new Suspense { Child = child };

            Assert.Same(child, suspense.Content);
        });
    }

    [Fact]
    public void PendingShowsFallbackInsteadOfChild()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var fallback = new Border();

            var suspense = new Suspense { Child = child, Fallback = fallback, IsPending = true };

            Assert.Same(fallback, suspense.Content);
        });
    }

    [Fact]
    public void TogglingIsPendingSwapsBetweenFallbackAndChild()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var fallback = new Border();
            var suspense = new Suspense { Child = child, Fallback = fallback };

            suspense.IsPending = true;
            Assert.Same(fallback, suspense.Content);

            suspense.IsPending = false;
            Assert.Same(child, suspense.Content);
        });
    }

    [Fact]
    public void ChangingChildWhileNotPendingUpdatesContentImmediately()
    {
        StaThread.Run(() =>
        {
            var suspense = new Suspense { Child = new Border() };
            var replacement = new Border();

            suspense.Child = replacement;

            Assert.Same(replacement, suspense.Content);
        });
    }

    [Fact]
    public void ChangingChildWhilePendingDoesNotChangeVisibleContent()
    {
        StaThread.Run(() =>
        {
            var fallback = new Border();
            var suspense = new Suspense { Child = new Border(), Fallback = fallback, IsPending = true };

            suspense.Child = new Border();

            Assert.Same(fallback, suspense.Content);
        });
    }
}
