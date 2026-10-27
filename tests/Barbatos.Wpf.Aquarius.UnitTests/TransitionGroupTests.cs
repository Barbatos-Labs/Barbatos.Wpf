// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Barbatos.Wpf.Aquarius.UnitTests;

public class TransitionGroupTests
{
    [Fact]
    public void EnterPlaysOnceForANewContainerAndNotAgainWhenThatContainerIsReloaded()
    {
        // The hook used to stay subscribed, so every container replayed Enter on each later
        // Loaded - every time the list's tab or If came back - for items that were not new.
        StaThread.Run(() =>
        {
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(50));
            Storyboard.SetTargetProperty(fade, new PropertyPath(UIElement.OpacityProperty));
            var enter = new CountingStoryboard();
            enter.Children.Add(fade);

            var items = new ObservableCollection<string>();
            var list = new ItemsControl { ItemsSource = items };
            TransitionGroup.SetEnter(list, enter);
            var window = new Window { Content = list, Width = 200, Height = 200 };

            try
            {
                window.Show();
                StaThread.PumpDispatcher();

                items.Add("first");
                StaThread.PumpDispatcher();

                Assert.Equal(1, enter.Plays);

                // A second Loaded on the same container, as a returning tab or If raises.
                var container = (FrameworkElement)list.ItemContainerGenerator.ContainerFromItem("first");
                container.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent, container));

                Assert.Equal(1, enter.Plays);
            }
            finally
            {
                window.Close();
                StaThread.PumpDispatcher();
            }
        });
    }

    /// <summary>
    /// Counts how many times it, or any clone of it, is begun - <c>TransitionGroup</c> plays a
    /// clone per container, so the count has to survive <c>Clone()</c>.
    /// </summary>
    /// <remarks>
    /// Counting clock creation needs no time to pass and no assumption about when an animated value
    /// becomes visible: every <c>Begin</c> builds exactly one root clock through
    /// <see cref="AllocateClock"/>.
    /// </remarks>
    private sealed class CountingStoryboard : Storyboard
    {
        private PlayCount _count = new();

        public int Plays => _count.Value;

        protected override Freezable CreateInstanceCore() => new CountingStoryboard();

        protected override void CloneCore(Freezable sourceFreezable)
        {
            base.CloneCore(sourceFreezable);
            _count = ((CountingStoryboard)sourceFreezable)._count;
        }

        protected override Clock AllocateClock()
        {
            _count.Value++;
            return base.AllocateClock();
        }

        private sealed class PlayCount
        {
            public int Value { get; set; }
        }
    }
}
