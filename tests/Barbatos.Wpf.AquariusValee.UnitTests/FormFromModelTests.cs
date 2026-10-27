namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class FormFromModelTests
{
    private sealed class SignupModel : IValidatableObject
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(8)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = "";

        public bool AcceptedTerms { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!AcceptedTerms)
                yield return new ValidationResult("You must accept the terms.", [nameof(AcceptedTerms)]);
        }
    }

    [Fact]
    public void FromModelCreatesOneFieldPerPublicSettableProperty()
    {
        var form = Form.FromModel(new SignupModel());

        Assert.Equal(4, form.Fields.Count);
        Assert.Contains("Email", form.Fields.Keys);
        Assert.Contains("Password", form.Fields.Keys);
        Assert.Contains("ConfirmPassword", form.Fields.Keys);
        Assert.Contains("AcceptedTerms", form.Fields.Keys);
    }

    [Fact]
    public void FromModelReflectsDisplayNameAttributeAsLabel()
    {
        var form = Form.FromModel(new SignupModel());

        Assert.Equal("Password", form.Fields["Password"].Label);
    }

    [Fact]
    public async Task FromModelFieldValidatesUsingTheModelsOwnDeclaredAttributes()
    {
        var form = Form.FromModel(new SignupModel());
        form.SetFieldValue("Email", "not-an-email");

        await form.Fields["Email"].ValidateAsync();

        Assert.NotEmpty(form.Fields["Email"].Errors);
    }

    [Fact]
    public async Task CompareAttributeWorksInFromModelModeBecauseObjectInstanceIsTheRealLiveModel()
    {
        var form = Form.FromModel(new SignupModel());
        form.SetFieldValue("Password", "secret123");
        form.SetFieldValue("ConfirmPassword", "different");

        await form.Fields["ConfirmPassword"].ValidateAsync();
        Assert.NotEmpty(form.Fields["ConfirmPassword"].Errors);

        form.SetFieldValue("ConfirmPassword", "secret123");
        await form.Fields["ConfirmPassword"].ValidateAsync();
        Assert.Empty(form.Fields["ConfirmPassword"].Errors);
    }

    [Fact]
    public async Task IValidatableObjectCrossFieldRuleIsAppliedAsPartOfValidateAllAsync()
    {
        var form = Form.FromModel(new SignupModel());
        form.SetFieldValue("Email", "user@example.com");
        form.SetFieldValue("Password", "secret123");
        form.SetFieldValue("ConfirmPassword", "secret123");
        // AcceptedTerms left false.

        await form.ValidateAllAsync();

        Assert.Contains("You must accept the terms.", form.Fields["AcceptedTerms"].Errors);
    }

    [Fact]
    public async Task AnEmptyRequiredFieldReportsItsMessageOnceAfterAWholeFormValidation()
    {
        // The model-level pass used Validator.TryValidateObject(..., validateAllProperties: false),
        // which still checks every [Required] property and returns each failure under the
        // property's name - appended on top of the identical message the field's own pass had
        // just written, so every empty required field listed it twice after a submit.
        var form = Form.FromModel(new SignupModel());

        await form.ValidateAllAsync();

        Assert.Single(form.Fields["Email"].Errors);
        Assert.Single(form.Fields["Password"].Errors);
        Assert.Single(form.Fields["ConfirmPassword"].Errors);
    }

    [Fact]
    public async Task IValidatableObjectRunsEvenWhileARequiredFieldIsStillEmpty()
    {
        // TryValidateObject stops after a failing [Required], so Validate never ran until every
        // required field was filled - the terms error appeared only on a second submit.
        var form = Form.FromModel(new SignupModel());

        await form.ValidateAllAsync();

        Assert.NotEmpty(form.Fields["Email"].Errors);
        Assert.Contains("You must accept the terms.", form.Fields["AcceptedTerms"].Errors);
    }

    // Public: CustomValidationAttribute refuses a validator type that is not visible.
    [CustomValidation(typeof(ClassLevelRuleModel), nameof(ClassLevelRuleModel.ValidateRange))]
    public sealed class ClassLevelRuleModel : IValidatableObject
    {
        private bool _validateWasCalled;

        // Get-only, so FromModel (which takes every readable and writable property) leaves it out.
        public bool ValidateWasCalled => _validateWasCalled;

        public int Start { get; set; } = 10;

        public int End { get; set; } = 5;

        public static ValidationResult? ValidateRange(ClassLevelRuleModel model) =>
            model.End < model.Start
                ? new ValidationResult("End must not be before Start.", [nameof(End)])
                : ValidationResult.Success;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            _validateWasCalled = true;
            yield break;
        }
    }

    [Fact]
    public async Task AClassLevelValidationAttributeIsStillAppliedBeforeIValidatableObject()
    {
        // The model-level pass is now done by hand rather than through TryValidateObject; this
        // pins that it keeps the BCL's remaining steps and their order - class-level attributes
        // first, and Validate only once they pass.
        var model = new ClassLevelRuleModel();
        var form = Form.FromModel(model);

        await form.ValidateAllAsync();

        Assert.Contains("End must not be before Start.", form.Fields["End"].Errors);
        Assert.False(model.ValidateWasCalled);

        form.SetFieldValue("End", 20);
        await form.ValidateAllAsync();

        Assert.Empty(form.Fields["End"].Errors);
        Assert.True(model.ValidateWasCalled);
    }

    [Fact]
    public void FieldChangesWriteBackLiveOntoTheModelInstance()
    {
        var model = new SignupModel();
        var form = Form.FromModel(model);

        form.SetFieldValue("Email", "user@example.com");

        Assert.Equal("user@example.com", model.Email);
        Assert.Same(model, form.GetModel<SignupModel>());
    }

    [Fact]
    public void GetModelThrowsForAFormNotBuiltViaFromModel()
    {
        var form = new Form();

        Assert.Throws<InvalidOperationException>(form.GetModel<SignupModel>);
    }

    private sealed class HintedModel
    {
        [UIHint("MultilineText")]
        public string Notes { get; set; } = "";

        [DataType(DataType.CreditCard)]
        public string CardNumber { get; set; } = "";

        [Editable(false)]
        public string ReadOnlyField { get; set; } = "fixed";

        [Display(Prompt = "e.g. jane@example.com")]
        public string Email { get; set; } = "";

        public string PlainField { get; set; } = "";
    }

    [Fact]
    public void FromModelReflectsUIHintAttributeAsUIHint()
    {
        var form = Form.FromModel(new HintedModel());

        Assert.Equal("MultilineText", form.Fields["Notes"].UIHint);
        Assert.Null(form.Fields["PlainField"].UIHint);
    }

    [Fact]
    public void FromModelReflectsDisplayPromptAttributeAsPrompt()
    {
        var form = Form.FromModel(new HintedModel());

        Assert.Equal("e.g. jane@example.com", form.Fields["Email"].Prompt);
        Assert.Null(form.Fields["PlainField"].Prompt);
    }

    [Fact]
    public void FromModelReflectsDataTypeAttributeAsDataTypeHint()
    {
        var form = Form.FromModel(new HintedModel());

        Assert.Equal(DataType.CreditCard, form.Fields["CardNumber"].DataTypeHint);
        Assert.Null(form.Fields["PlainField"].DataTypeHint);
    }

    [Fact]
    public void FromModelReflectsEditableFalseAsIsReadOnly()
    {
        var form = Form.FromModel(new HintedModel());

        Assert.True(form.Fields["ReadOnlyField"].IsReadOnly);
        Assert.False(form.Fields["PlainField"].IsReadOnly);
    }

    private sealed class PriceRangeModel
    {
        public int MinPrice { get; set; } = 10;
        public int MaxPrice { get; set; } = 100;

        [PropertyRange(nameof(MinPrice), nameof(MaxPrice))]
        public int Price { get; set; } = 50;
    }

    [Fact]
    public async Task PropertyRangeAttributeValidatesAgainstSiblingFieldsCurrentValuesInFromModelMode()
    {
        var form = Form.FromModel(new PriceRangeModel());

        await form.Fields["Price"].ValidateAsync();
        Assert.Empty(form.Fields["Price"].Errors);

        form.SetFieldValue("Price", 5);
        await form.Fields["Price"].ValidateAsync();
        Assert.NotEmpty(form.Fields["Price"].Errors);

        form.SetFieldValue("Price", 500);
        await form.Fields["Price"].ValidateAsync();
        Assert.NotEmpty(form.Fields["Price"].Errors);
    }

    [Fact]
    public async Task FromModelAutoWiresDependsOnForIDependsOnPropertiesAttributesWithNoManualWiring()
    {
        var form = Form.FromModel(new PriceRangeModel());
        await form.Fields["Price"].ValidateAsync();
        Assert.Empty(form.Fields["Price"].Errors);

        // Price itself is never touched here - only its declared dependencies (MinPrice/MaxPrice)
        // change, proving Form.FromModel wired Field.DependsOn automatically from the attribute.
        form.SetFieldValue("MinPrice", 60);
        await Task.Delay(20);
        Assert.NotEmpty(form.Fields["Price"].Errors);

        form.SetFieldValue("MinPrice", 10);
        await Task.Delay(20);
        Assert.Empty(form.Fields["Price"].Errors);

        form.SetFieldValue("MaxPrice", 40);
        await Task.Delay(20);
        Assert.NotEmpty(form.Fields["Price"].Errors);
    }

    private sealed class BadDependencyModel
    {
        [PropertyRange("ThisPropertyDoesNotExist", "NeitherDoesThisOne")]
        public int Price { get; set; } = 50;
    }

    [Fact]
    public void FromModelThrowsImmediatelyWhenAnIDependsOnPropertiesAttributeNamesAnUnknownProperty()
    {
        // Fails at FromModel-call time (typically ViewModel construction), not lazily on first
        // validation - the earliest point that can catch a hand-typed (non-nameof) typo.
        Assert.Throws<InvalidOperationException>(() => Form.FromModel(new BadDependencyModel()));
    }

    [Fact]
    public async Task DependsOnTriggeredRevalidationOnAFromModelFieldSeesTheSourcesAlreadyWrittenBackModelValue()
    {
        // Field.HandleChange used to set its own reactive _value BEFORE calling WriteBackToModel -
        // harmless for an untouched field, but DependsOn's watcher fires synchronously the instant
        // _value changes, so a dependent field re-validating (by reflecting into the live model,
        // like PropertyRangeAttribute/CompareAttribute do) would still see the model's OLD value.
        // Regression coverage for that exact ordering bug, independent of PropertyRange's own
        // comparison logic.
        var model = new PriceRangeModel();
        var form = Form.FromModel(model);
        var observedMinPrice = -1;

        ((Field<int>)form.Fields["Price"]).AddRule(_ =>
        {
            observedMinPrice = model.MinPrice;
            return null;
        });

        await form.Fields["Price"].ValidateAsync();
        form.SetFieldValue("MinPrice", 60);

        Assert.Equal(60, observedMinPrice);
    }

    [Fact]
    public void ResetWritesTheResetValueBackOntoTheModelToo()
    {
        var model = new PriceRangeModel();
        var form = Form.FromModel(model);

        form.SetFieldValue("Price", 999);
        Assert.Equal(999, model.Price);

        ((Field<int>)form.Fields["Price"]).Reset();

        Assert.Equal(50, model.Price);
    }
}
