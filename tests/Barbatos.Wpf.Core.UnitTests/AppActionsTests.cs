// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using Barbatos.Wpf.ApplicationModel;

namespace Barbatos.Wpf.Core.UnitTests;

// AppActions.SetAsync/GetAsync are exercised end-to-end against the real taskbar Jump List
// in the sample app instead of here: System.Windows.Shell.JumpList.SetJumpList requires a
// running System.Windows.Application, which the xunit test host does not have.
public class AppActionsTests
{
    [Fact]
    public void CurrentIsCached()
    {
        Assert.NotNull(AppActions.Current);
        Assert.Same(AppActions.Current, AppActions.Current);
    }

    [Fact]
    public void IsSupportedIsTrueOnWindows()
    {
        Assert.True(AppActions.IsSupported);
    }

    [Fact]
    public void SubscribingAndUnsubscribingDoesNotThrow()
    {
        EventHandler<AppActionEventArgs> handler = (sender, args) => { };

        AppActions.OnAppAction += handler;
        AppActions.OnAppAction -= handler;
    }

    [Fact]
    public void ConstructorThrowsForNullId()
    {
        Assert.Throws<ArgumentNullException>(() => new AppAction(null!, "Title"));
    }

    [Fact]
    public void ConstructorThrowsForNullTitle()
    {
        Assert.Throws<ArgumentNullException>(() => new AppAction("id", null!));
    }

    [Fact]
    public void ConstructorSetsAllProperties()
    {
        var action = new AppAction("id", "Title", "Subtitle", "icon.ico");

        Assert.Equal("id", action.Id);
        Assert.Equal("Title", action.Title);
        Assert.Equal("Subtitle", action.Subtitle);
        Assert.Equal("icon.ico", action.Icon);
    }

    [Fact]
    public void EventArgsExposeTheGivenAction()
    {
        var action = new AppAction("id", "Title");
        var args = new AppActionEventArgs(action);

        Assert.Same(action, args.AppAction);
    }
}
