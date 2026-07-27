namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class FormTests
{
    private static (Form form, Field<string> email, Field<string> password) BuildLoginForm()
    {
        var form = new Form();
        var email = form.DefineField("email", "", new RequiredAttribute(), new EmailAddressAttribute());
        var password = form.DefineField("password", "", new RequiredAttribute());
        return (form, email, password);
    }

    [Fact]
    public void DefineFieldReturnsATypedFieldAndTracksItOnTheForm()
    {
        var (form, email, _) = BuildLoginForm();

        Assert.Same(email, form.Fields["email"]);
        Assert.Equal(2, form.Fields.Count);
    }

    [Fact]
    public void FormMetaTouchedIsTrueIfAnyFieldIsTouched()
    {
        var (form, email, _) = BuildLoginForm();
        Assert.False(form.Meta.Touched);

        email.HandleBlur();

        Assert.True(form.Meta.Touched);
    }

    [Fact]
    public async Task FormMetaValidRequiresEveryFieldToBeValid()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        password.Value = "secret";
        await form.ValidateAllAsync();

        Assert.True(form.Meta.Valid);

        password.Value = "";
        await form.ValidateAllAsync();

        Assert.False(form.Meta.Valid);
    }

    [Fact]
    public async Task HandleSubmitTouchesEveryFieldEvenOnesNeverInteractedWith()
    {
        var (form, email, password) = BuildLoginForm();
        var command = form.HandleSubmit(_ => Task.CompletedTask);

        await command.ExecuteAsync(null);

        Assert.True(email.Meta.Touched);
        Assert.True(password.Meta.Touched);
    }

    [Fact]
    public async Task HandleSubmitCallsOnValidOnlyWhenTheWholeFormIsValid()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        password.Value = "secret";

        var onValidCalled = false;
        var onInvalidCalled = false;
        var command = form.HandleSubmit(
            _ => { onValidCalled = true; return Task.CompletedTask; },
            (_, _) => onInvalidCalled = true);

        await command.ExecuteAsync(null);

        Assert.True(onValidCalled);
        Assert.False(onInvalidCalled);
    }

    [Fact]
    public async Task HandleSubmitCallsOnInvalidWithCurrentValuesAndErrorsWhenTheFormIsInvalid()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        // password left empty - invalid.

        IReadOnlyDictionary<string, object?>? capturedValues = null;
        IReadOnlyDictionary<string, IReadOnlyList<string>>? capturedErrors = null;
        var command = form.HandleSubmit(
            _ => Task.CompletedTask,
            (values, errors) => { capturedValues = values; capturedErrors = errors; });

        await command.ExecuteAsync(null);

        Assert.NotNull(capturedValues);
        Assert.Equal("user@example.com", capturedValues!["email"]);
        Assert.NotNull(capturedErrors);
        Assert.True(capturedErrors!.ContainsKey("password"));
    }

    [Fact]
    public async Task HandleSubmitIncrementsSubmitCountOnEveryAttemptRegardlessOfOutcome()
    {
        var (form, _, _) = BuildLoginForm();
        var command = form.HandleSubmit(_ => Task.CompletedTask);

        Assert.Equal(0, form.SubmitCount);

        await command.ExecuteAsync(null);
        Assert.Equal(1, form.SubmitCount);

        await command.ExecuteAsync(null);
        Assert.Equal(2, form.SubmitCount);
    }

    [Fact]
    public async Task IsSubmittingSpansTheWholeCallbackNotJustValidation()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        password.Value = "secret";

        var gate = new TaskCompletionSource();
        var command = form.HandleSubmit(async _ =>
        {
            Assert.True(form.IsSubmitting);
            await gate.Task;
        });

        var executeTask = command.ExecuteAsync(null);
        Assert.True(form.IsSubmitting);

        gate.SetResult();
        await executeTask;

        Assert.False(form.IsSubmitting);
    }

    [Fact]
    public void CanExecuteIsGatedOnlyOnIsSubmittingNeverOnFormValidity()
    {
        var (form, _, _) = BuildLoginForm();
        // Both fields are empty/invalid, and untouched - a pristine, invalid form.
        var command = form.HandleSubmit(_ => Task.CompletedTask);

        // Matches vee-validate's own real behavior: clicking Submit on an invalid, untouched
        // form is exactly what's supposed to force every field touched and surface its errors -
        // the command itself must not refuse to run just because the form isn't valid yet.
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public void GetValuesReturnsEveryFieldsCurrentBoxedValueByName()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        password.Value = "secret";

        var values = form.GetValues();

        Assert.Equal("user@example.com", values["email"]);
        Assert.Equal("secret", values["password"]);
    }

    [Fact]
    public void SetFieldValueUpdatesTheNamedFieldThroughItsGenericType()
    {
        var (form, email, _) = BuildLoginForm();

        form.SetFieldValue("email", "new@example.com");

        Assert.Equal("new@example.com", email.Value);
    }

    [Fact]
    public void SetFieldValueThrowsForAWrongGenericTypeOrUnknownName()
    {
        var (form, _, _) = BuildLoginForm();

        Assert.Throws<ArgumentException>(() => form.SetFieldValue("email", 42));
        Assert.Throws<ArgumentException>(() => form.SetFieldValue("nope", "x"));
    }

    [Fact]
    public void SetFieldErrorDelegatesToTheNamedFieldsSetError()
    {
        var (form, email, _) = BuildLoginForm();

        form.SetFieldError("email", "Email already registered");

        Assert.Equal("Email already registered", email.ErrorMessage);
    }

    [Fact]
    public async Task ResetFormResetsEveryFieldAndSubmitCount()
    {
        var (form, email, _) = BuildLoginForm();
        email.Value = "user@example.com";
        email.HandleBlur();
        await form.HandleSubmit(_ => Task.CompletedTask).ExecuteAsync(null);

        form.ResetForm();

        Assert.Equal("", email.Value);
        Assert.False(email.Meta.Touched);
        Assert.Equal(0, form.SubmitCount);
    }

    [Fact]
    public async Task GetFirstInvalidFieldReturnsFieldsInRegistrationOrder()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        // password left empty - invalid.
        await form.ValidateAllAsync();

        Assert.Same(password, form.GetFirstInvalidField());
    }

    [Fact]
    public async Task GetFirstInvalidFieldReturnsNullWhenTheWholeFormIsValid()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        password.Value = "secret";
        await form.ValidateAllAsync();

        Assert.Null(form.GetFirstInvalidField());
    }

    [Fact]
    public async Task HandleSubmitRaisesSubmitInvalidWithTheFirstInvalidFieldOnFailure()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        // password left empty - invalid.

        IField? raised = null;
        form.SubmitInvalid += field => raised = field;

        await form.HandleSubmit(_ => Task.CompletedTask).ExecuteAsync(null);

        Assert.Same(password, raised);
    }

    [Fact]
    public async Task HandleSubmitDoesNotRaiseSubmitInvalidWhenTheWholeFormIsValid()
    {
        var (form, email, password) = BuildLoginForm();
        email.Value = "user@example.com";
        password.Value = "secret";

        var raised = false;
        form.SubmitInvalid += _ => raised = true;

        await form.HandleSubmit(_ => Task.CompletedTask).ExecuteAsync(null);

        Assert.False(raised);
    }
}
