using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class TransitionTests
{
    [Fact]
    public void InitialChildIsShownWithoutPlayingEnter()
    {
        StaThread.Run(() =>
        {
            var child = new Border();

            var transition = new Transition { Child = child }; // Show defaults to true

            Assert.Same(child, transition.Content);
        });
    }

    [Fact]
    public void WithoutLeaveDetachesImmediatelyLikeIf()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var transition = new Transition { Show = true, Child = child };

            transition.Show = false;

            Assert.Null(transition.Content);
        });
    }

    [Fact]
    public void ShowFalseWithLeaveDefersDetachUntilTheStoryboardCompletes()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var transition = new Transition { Child = child, Leave = CreateFadeStoryboard(TimeSpan.FromMilliseconds(150)) };

            var window = new Window { Content = transition, Width = 100, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            transition.Show = false;

            // Still attached right after toggling - the storyboard hasn't had time to run yet.
            Assert.Same(child, transition.Content);

            PumpUntil(() => transition.Content is null, TimeSpan.FromSeconds(3));

            Assert.Null(transition.Content);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ReTogglingMidLeaveStopsTheInFlightAnimationAndKeepsContentMounted()
    {
        StaThread.Run(() =>
        {
            var child = new Border();
            var transition = new Transition { Child = child, Leave = CreateFadeStoryboard(TimeSpan.FromMilliseconds(150)) };

            var window = new Window { Content = transition, Width = 100, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            transition.Show = false; // starts playing Leave
            transition.Show = true; // immediately re-toggled, before Leave had any chance to progress

            Assert.Same(child, transition.Content);

            // Give it time to prove the stopped Leave animation doesn't fire a delayed Completed
            // and null out Content anyway.
            PumpFor(TimeSpan.FromMilliseconds(300));

            Assert.Same(child, transition.Content);

            // ...and that it really stopped. Begun non-controllable, Leave ignored the Stop, ran
            // to the end and held Opacity at 0: content mounted, but invisible.
            Assert.Equal(1.0, transition.Opacity);

            window.Close();
            StaThread.PumpDispatcher();
        });
    }

    [Fact]
    public void ReShowingAfterACompletedLeaveRestoresWhatTheLeaveAnimated()
    {
        // A finished Storyboard keeps holding its end value (FillBehavior.HoldEnd). With no
        // Enter animating Opacity back, the content used to come back still at Opacity 0.
        StaThread.Run(() =>
        {
            var child = new Border();
            var transition = new Transition { Child = child, Leave = CreateFadeStoryboard(TimeSpan.FromMilliseconds(100)) };
            var window = new Window { Content = transition, Width = 100, Height = 100 };

            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                transition.Show = false;
                PumpUntil(() => transition.Content is null, TimeSpan.FromSeconds(3));

                Assert.Null(transition.Content);
                Assert.Equal(0.0, transition.Opacity); // the hold this test is about

                transition.Show = true;

                // Removing a storyboard takes effect on the next animation tick, so wait for one -
                // bounded, since with the bug the value never comes back at all.
                PumpUntil(() => transition.Opacity == 1.0, TimeSpan.FromSeconds(3));

                Assert.Same(child, transition.Content);
                Assert.Equal(1.0, transition.Opacity);
            }
            finally
            {
                window.Close();
                StaThread.PumpDispatcher();
            }
        });
    }

    private static Storyboard CreateFadeStoryboard(TimeSpan duration)
    {
        var animation = new DoubleAnimation(1, 0, duration);
        Storyboard.SetTargetProperty(animation, new PropertyPath(UIElement.OpacityProperty));

        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        return storyboard;
    }

    private static void PumpUntil(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (!condition() && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(15);
            StaThread.PumpDispatcher();
        }
    }

    private static void PumpFor(TimeSpan duration)
    {
        var deadline = DateTime.UtcNow + duration;

        while (DateTime.UtcNow < deadline)
        {
            Thread.Sleep(15);
            StaThread.PumpDispatcher();
        }
    }
}
