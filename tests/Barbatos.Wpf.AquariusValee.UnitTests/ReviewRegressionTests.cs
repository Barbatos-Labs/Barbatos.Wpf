using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

// System.Windows.Controls has a ValidationResult of its own, unrelated to DataAnnotations'.
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Barbatos.Wpf.AquariusValee.UnitTests;

/// <summary>
/// Regression coverage for the defects found while reviewing the package as a whole - each test
/// here fails against the code as it stood before that review.
/// </summary>
public class ReviewRegressionTests
{
    private sealed class TermsModel : IValidatableObject
    {
        [Required]
        public string Email { get; set; } = "";

        public bool AcceptedTerms { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!AcceptedTerms)
                yield return new ValidationResult("You must accept the terms.", [nameof(AcceptedTerms)]);
        }
    }

    [Fact]
    public async Task RevalidatingKeepsModelLevelErrorsInsteadOfSilentlyDroppingThem()
    {
        // Each field's own pass REPLACES its errors, so re-running the fields without re-running
        // IValidatableObject wiped the cross-field message - the error vanished from the UI the
        // first time the user switched language.
        var form = Form.FromModel(new TermsModel());
        form.SetFieldValue("Email", "user@example.com");

        await form.ValidateAllAsync();
        Assert.Contains("You must accept the terms.", form.Fields["AcceptedTerms"].Errors);

        await form.RevalidateValidatedAsync();

        Assert.Contains("You must accept the terms.", form.Fields["AcceptedTerms"].Errors);
    }

    [Fact]
    public async Task RevalidatingStillDoesNotSurfaceAModelLevelErrorOnAnUntouchedField()
    {
        // The counterweight to the test above: re-running the model-level pass must not start
        // showing errors on a field the user has never interacted with.
        var form = Form.FromModel(new TermsModel());

        await form.Fields["Email"].ValidateAsync(); // only Email is validated; AcceptedTerms is pristine
        await form.RevalidateValidatedAsync();

        Assert.Empty(form.Fields["AcceptedTerms"].Errors);
    }

    [Fact]
    public async Task ARepeatedValidationPassWithAnUnchangedResultRaisesNoNotifications()
    {
        // Validation re-runs on every keystroke; firing ErrorsChanged each time makes WPF
        // re-evaluate the validation adorner for every binding on the field for no reason.
        var field = new Field<string>("Email", "", _ => "Required.");
        await field.ValidateAsync();

        var errorsChanged = 0;
        var propertyChanged = 0;
        field.ErrorsChanged += (_, _) => errorsChanged++;
        field.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(Field<string>.Errors) or nameof(Field<string>.ErrorMessage))
                propertyChanged++;
        };

        await field.ValidateAsync();
        await field.ValidateAsync();

        Assert.Equal(0, errorsChanged);
        Assert.Equal(0, propertyChanged);
    }

    [Fact]
    public async Task AGenuinelyChangedErrorStillNotifies()
    {
        // Guard against the optimisation above going too far and suppressing real changes.
        var message = "First.";
        var field = new Field<string>("Email", "", _ => message);
        await field.ValidateAsync();

        var errorsChanged = 0;
        field.ErrorsChanged += (_, _) => errorsChanged++;

        message = "Second.";
        await field.ValidateAsync();

        Assert.Equal(1, errorsChanged);
        Assert.Equal("Second.", field.ErrorMessage);
    }

    [Fact]
    public async Task ASynchronousOnlyFieldNeverReportsPending()
    {
        // Pending exists for in-flight async rules. Raising and lowering it around a purely
        // synchronous pass is two wasted notifications per keystroke, each propagating to FormMeta.
        var field = new Field<string>("Email", "", _ => null);

        var pendingChanges = 0;
        field.Meta.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(FieldMeta.Pending))
                pendingChanges++;
        };

        await field.ValidateAsync();

        Assert.Equal(0, pendingChanges);
        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public async Task AnAsyncFieldStillReportsPendingWhileItsRuleIsInFlight()
    {
        var gate = new TaskCompletionSource();
        var field = new Field<string>("Username", "taken");
        field.AddRule(async (_, _) => { await gate.Task; return "Taken."; });

        var validation = field.ValidateAsync();
        Assert.True(field.Meta.Pending);

        gate.SetResult();
        await validation;

        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public void DisposingAFieldMidFlightClearsPendingRatherThanStrandingItForever()
    {
        var field = new Field<string>("Username", "x");
        field.AddRule(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return null; });

        _ = field.ValidateAsync();
        Assert.True(field.Meta.Pending);

        field.Dispose();

        // The cancelled pass returns before its own "Pending = false", so Dispose has to do it -
        // otherwise the whole Form reports Pending forever too.
        Assert.False(field.Meta.Pending);
    }

    [Fact]
    public void ValeeForBindsEvenWhenSetOnAnAlreadyLoadedElement()
    {
        // Assigning from code-behind after load means Loaded has already fired and will not fire
        // again, so subscribing to it alone left the field silently unbound.
        StaThread.Run(() =>
        {
            var form = new Form();
            var email = form.DefineField("Email", "initial");

            var panel = new StackPanel();
            var box = new TextBox();
            panel.Children.Add(box);
            Valee.SetForm(panel, form);

            var window = new Window { Content = panel, Width = 100, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();
            Assert.True(box.IsLoaded);

            Valee.SetFor(box, "Email");
            StaThread.PumpDispatcher();

            Assert.Equal("initial", box.Text);

            box.Text = "typed";
            Assert.Equal("typed", email.Value);

            window.Close();
        });
    }

    [Fact]
    public void AnElementCarryingBothFieldAndForBlursItsFieldOnlyOnce()
    {
        // Both attached properties route through the same binding helper, which subscribed to
        // LostFocus without detaching first - so a single loss of focus ran HandleBlur twice.
        StaThread.Run(() =>
        {
            // HandleBlur runs a Force validation pass (the OnBlur trigger is on by default), so
            // counting rule invocations counts the handlers - unlike Meta.Touched, which latches
            // true and would hide a second call.
            var blurs = 0;
            var form = new Form();
            var email = form.DefineField<string>("Email", "", _ => { blurs++; return null; });
            blurs = 0; // discard the constructor's own silent pass

            var panel = new StackPanel();
            var box = new TextBox();
            panel.Children.Add(box);
            Valee.SetForm(panel, form);

            var window = new Window { Content = panel, Width = 100, Height = 100 };
            window.Show();
            StaThread.PumpDispatcher();

            Valee.SetField(box, email);
            Valee.SetFor(box, "Email");
            StaThread.PumpDispatcher();

            box.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent, box));

            Assert.Equal(1, blurs);

            window.Close();
        });
    }

    [Fact]
    public void MaskRefusesANonTextBoxInsteadOfSilentlyDoingNothing() =>
        StaThread.Run(() =>
            Assert.Throws<InvalidOperationException>(() => Valee.SetMask(new CheckBox(), "000")));
}
