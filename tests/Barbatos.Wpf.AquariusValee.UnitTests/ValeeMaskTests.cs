using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// Drives a real <see cref="TextBox"/> on a real STA thread through
/// <see cref="TextCompositionManager"/>, so every case here goes through the same
/// <c>PreviewTextInput</c> -> <c>TextChanged</c> -> caret-fix pipeline a genuine keystroke does.
/// Assigning <c>TextBox.SelectedText</c> instead would not - it leaves the inserted text selected
/// rather than advancing an empty caret past it, so it cannot catch caret regressions.
/// </summary>
public class ValeeMaskTests
{
    private const string SkuMask = ">AAA-0000";

    private sealed record Result(string Text, int CaretIndex);

    private static Result Type(string mask, string keystrokes, string initialText = "")
    {
        return StaThread.Run(() =>
        {
            var box = new TextBox { Text = initialText };
            Valee.SetMask(box, mask);

            foreach (var key in keystrokes)
                Press(box, key);

            Drain(box);
            return new Result(box.Text, box.CaretIndex);
        });
    }

    private static void Press(TextBox box, char key)
    {
        TextCompositionManager.StartComposition(
            new TextComposition(InputManager.Current, box, key.ToString()));

        Drain(box);
    }

    /// <summary>
    /// Lets the deferred caret fix run. It is queued at <c>Input</c> priority (5), so this has to
    /// pump at something strictly lower - the dispatcher runs the queue highest-first, and
    /// draining at <c>Loaded</c> (6) or above returns without ever reaching it, leaving the caret
    /// pinned at 0 and every keystroke inserting at the front.
    /// </summary>
    private static void Drain(TextBox box) =>
        box.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);

    [Fact]
    public void SeparatorsAppearOnTheirOwnSoTheUserNeverTypesThem()
    {
        var result = Type(SkuMask, "abc1234");

        Assert.Equal("ABC-1234", result.Text);
    }

    [Fact]
    public void TheCaretSitsPastAnAutoInsertedSeparatorSoTheNextKeyLandsInTheRightSlot()
    {
        // After the third letter the mask has emitted "ABC-"; the next digit must go after the
        // dash, not in front of it.
        var result = Type(SkuMask, "abc");

        Assert.Equal("ABC-", result.Text);
        Assert.Equal(4, result.CaretIndex);
    }

    [Fact]
    public void TypingTheSeparatorYourselfIsAcceptedRatherThanDuplicated()
    {
        var result = Type(SkuMask, "abc-1234");

        Assert.Equal("ABC-1234", result.Text);
    }

    [Theory]
    [InlineData("ab@c1234", "ABC-1234")]   // punctuation aimed at a letter slot
    [InlineData("abc12x34", "ABC-1234")]   // a letter aimed at a digit slot
    [InlineData("1abc1234", "ABC-1234")]   // a digit aimed at the very first letter slot
    public void CharactersThatCannotGoInTheNextSlotAreIgnoredEntirely(string keystrokes, string expected) =>
        Assert.Equal(expected, Type(SkuMask, keystrokes).Text);

    [Fact]
    public void TypingPastTheEndOfTheMaskDoesNothing()
    {
        var result = Type(SkuMask, "abc12345678");

        Assert.Equal("ABC-1234", result.Text);
    }

    [Fact]
    public void ACharacterThatWouldBeDiscardedIsSwallowedBeforeItCanFlashOnScreen()
    {
        StaThread.Run(() =>
        {
            var box = new TextBox();
            Valee.SetMask(box, SkuMask);

            // handledEventsToo, so this probe still runs after the mask has marked the event
            // handled - that flag is the whole assertion. Stripping the character later in
            // TextChanged would leave the same empty Text but a false flag here, which is exactly
            // the difference between "never appeared" and "appeared then vanished".
            var handled = false;
            box.AddHandler(
                UIElement.PreviewTextInputEvent,
                new TextCompositionEventHandler((_, e) => handled = e.Handled),
                handledEventsToo: true);

            Press(box, '1'); // a digit cannot open ">AAA-0000"

            Assert.True(handled);
            Assert.Equal("", box.Text);
        });
    }

    [Fact]
    public void DeletingASeparatorDeletesThroughItInsteadOfLettingItReappear()
    {
        var result = StaThread.Run(() =>
        {
            var box = new TextBox();
            Valee.SetMask(box, SkuMask);

            foreach (var key in "abc")
                Press(box, key);
            Drain(box);
            Assert.Equal("ABC-", box.Text);

            // A real Backspace, via the same editing command the key is bound to - not a wholesale
            // Text assignment, which reports a different change shape than the keystroke does.
            // Without the trailing-literal trim the mask re-adds the dash instantly and the key
            // looks dead.
            box.CaretIndex = box.Text.Length;
            System.Windows.Documents.EditingCommands.Backspace.Execute(null, box);
            Drain(box);

            return box.Text;
        });

        Assert.Equal("ABC", result);
    }

    [Fact]
    public void AnInitialValueFromTheBoundFieldIsLaidOutWithoutWaitingForAKeystroke()
    {
        var result = StaThread.Run(() =>
        {
            var box = new TextBox { Text = "abc1234" };
            Valee.SetMask(box, SkuMask);
            Drain(box);
            return box.Text;
        });

        Assert.Equal("ABC-1234", result);
    }

    [Fact]
    public void PastedTextIsFilteredThroughTheMaskRatherThanRejected()
    {
        // Paste bypasses PreviewTextInput, so TextChanged is what has to clean it up.
        var result = StaThread.Run(() =>
        {
            var box = new TextBox();
            Valee.SetMask(box, SkuMask);
            box.Text = "  abc / 1234  ";
            Drain(box);
            return box.Text;
        });

        Assert.Equal("ABC-1234", result);
    }

    [Theory]
    [InlineData(">AAA-0000", "abc1234", "ABC-1234")]
    [InlineData("<AAA-0000", "ABC1234", "abc-1234")]
    [InlineData("AAA-0000", "aBc1234", "aBc-1234")]   // no casing directive - left as typed
    [InlineData("000.000.000", "123456789", "123.456.789")]
    [InlineData("(000) 000-0000", "5551234567", "(555) 123-4567")]
    [InlineData("***-***", "a1b2c3", "a1b-2c3")]
    public void TheMaskLanguageCoversDigitsLettersAlphanumericsAndCasing(string mask, string keystrokes, string expected) =>
        Assert.Equal(expected, Type(mask, keystrokes).Text);

    [Fact]
    public void ABackslashEscapesAMaskCharacterIntoALiteral()
    {
        // "\0" is a literal zero, not a digit slot; the following 0s are real slots.
        var result = Type(@"\0-000", "123");

        Assert.Equal("0-123", result.Text);
    }

    [Fact]
    public void AnEmptyBoxStaysEmptyRatherThanShowingBareSeparators()
    {
        var result = Type("(000) 000", "");

        Assert.Equal("", result.Text);
    }

    [Fact]
    public void ClearingTheMaskDetachesItAndLeavesTheBoxAlone()
    {
        var result = StaThread.Run(() =>
        {
            var box = new TextBox();
            Valee.SetMask(box, SkuMask);
            Valee.SetMask(box, null);

            foreach (var key in "abc1234")
                Press(box, key);
            Drain(box);

            return box.Text;
        });

        Assert.Equal("abc1234", result);
    }
}
