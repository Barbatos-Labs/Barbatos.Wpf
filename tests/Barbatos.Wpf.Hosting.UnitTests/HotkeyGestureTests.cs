// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf.Core Contributors.
// All Rights Reserved.

using System.Windows.Input;
using Barbatos.Wpf.Hotkeys;

namespace Barbatos.Wpf.Hosting.UnitTests;

public class HotkeyGestureTests
{
    [Theory]
    [InlineData("Control+Alt+Space", ModifierKeys.Control | ModifierKeys.Alt, Key.Space)]
    [InlineData("Ctrl+Alt+Space", ModifierKeys.Control | ModifierKeys.Alt, Key.Space)]
    [InlineData("ctrl+shift+K", ModifierKeys.Control | ModifierKeys.Shift, Key.K)]
    [InlineData("Win+D", ModifierKeys.Windows, Key.D)]
    [InlineData("Windows+F12", ModifierKeys.Windows, Key.F12)]
    [InlineData("Control+Esc", ModifierKeys.Control, Key.Escape)]
    [InlineData("Control+1", ModifierKeys.Control, Key.D1)]
    [InlineData("Alt+Enter", ModifierKeys.Alt, Key.Enter)]
    [InlineData("F5", ModifierKeys.None, Key.F5)]
    [InlineData(" Control + Alt + Space ", ModifierKeys.Control | ModifierKeys.Alt, Key.Space)]
    public void CanParseValidGestures(string text, ModifierKeys expectedModifiers, Key expectedKey)
    {
        var gesture = HotkeyGesture.Parse(text);

        Assert.Equal(expectedModifiers, gesture.Modifiers);
        Assert.Equal(expectedKey, gesture.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Control+")]
    [InlineData("Control+Alt")]
    [InlineData("NotAKey+X")]
    [InlineData("Space+Control")]
    [InlineData("Control+Space+K")]
    public void TryParseReturnsFalseForInvalidGestures(string? text)
    {
        Assert.False(HotkeyGesture.TryParse(text, out var gesture));
        Assert.Null(gesture);
    }

    [Fact]
    public void ParseThrowsForInvalidGesture()
    {
        Assert.Throws<FormatException>(() => HotkeyGesture.Parse("Control+"));
    }

    [Fact]
    public void ConstructorRequiresAKey()
    {
        Assert.Throws<ArgumentException>(() => new HotkeyGesture(ModifierKeys.Control, Key.None));
    }

    [Theory]
    [InlineData("Control+Alt+Space")]
    [InlineData("Control+Shift+K")]
    [InlineData("Windows+F12")]
    public void ToStringRoundTrips(string text)
    {
        var gesture = HotkeyGesture.Parse(text);

        Assert.Equal(text, gesture.ToString());
        Assert.Equal(gesture, HotkeyGesture.Parse(gesture.ToString()));
    }

    [Fact]
    public void GesturesWithSameKeysAreEqual()
    {
        var gesture1 = HotkeyGesture.Parse("Ctrl+Alt+Space");
        var gesture2 = HotkeyGesture.Parse("Control+Alt+Space");
        var gesture3 = HotkeyGesture.Parse("Control+Alt+K");

        Assert.Equal(gesture1, gesture2);
        Assert.Equal(gesture1.GetHashCode(), gesture2.GetHashCode());
        Assert.NotEqual(gesture1, gesture3);
    }
}
