// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

namespace Barbatos.Wpf.AquariusValee.UnitTests;

public class FieldTests
{
    [Fact]
    public void NewFieldWithNoRulesIsValidWithNoErrors()
    {
        var field = new Field<string>("Name", "hello");

        Assert.Equal("hello", field.Value);
        Assert.True(field.Meta.Valid);
        Assert.Empty(field.Errors);
        Assert.Null(field.ErrorMessage);
        Assert.False(field.Meta.Touched);
        Assert.False(field.Meta.Validated);
    }

    [Fact]
    public void HandleChangeUpdatesValueAndRaisesPropertyChangedOnlyWhenValueActuallyDiffers()
    {
        var field = new Field<string>("Name", "a");
        var raisedFor = new List<string>();
        field.PropertyChanged += (_, e) => raisedFor.Add(e.PropertyName!);

        field.Value = "b";
        Assert.Equal("b", field.Value);
        Assert.Contains(nameof(Field<string>.Value), raisedFor);

        raisedFor.Clear();
        field.Value = "b";
        Assert.DoesNotContain(nameof(Field<string>.Value), raisedFor);
    }

    [Fact]
    public void PristineFieldWithFailingRuleReportsInvalidButWithholdsVisibleErrorsUntilValidated()
    {
        var field = new Field<string>("Email", "someone@example.com", new RequiredAttribute());

        // Clearing a required field via a plain value change (OnChange trigger) uses
        // ValidatedOnly mode - Meta.Valid must already reflect the truth, but Errors stays
        // withheld because this field has never actually been Validated yet (no blur, no submit).
        field.Value = "";

        Assert.False(field.Meta.Valid);
        Assert.False(field.Meta.Validated);
        Assert.Empty(field.Errors);
    }

    [Fact]
    public async Task ExplicitValidateAsyncAlwaysWritesErrorsRegardlessOfTouchedOrValidated()
    {
        var field = new Field<string>("Email", "", new RequiredAttribute());

        await field.ValidateAsync();

        Assert.True(field.Meta.Validated);
        Assert.False(field.Meta.Touched);
        Assert.NotEmpty(field.Errors);
    }

    [Fact]
    public void HandleBlurAlwaysSetsTouchedEvenWhenOnBlurTriggerIsDisabled()
    {
        var field = new Field<string>("Name", "x") { Triggers = ValidateTrigger.None };

        field.HandleBlur();

        Assert.True(field.Meta.Touched);
    }

    [Fact]
    public void HandleBlurValidatesWhenOnBlurTriggerIsEnabled()
    {
        var field = new Field<string>("Email", "", new RequiredAttribute());

        field.HandleBlur();

        Assert.True(field.Meta.Validated);
        Assert.NotEmpty(field.Errors);
    }

    [Fact]
    public void DirtyIsALiveComparisonNotALatchedFlag()
    {
        var field = new Field<string>("Name", "original");

        Assert.False(field.Meta.Dirty);

        field.Value = "changed";
        Assert.True(field.Meta.Dirty);

        field.Value = "original";
        Assert.False(field.Meta.Dirty);
    }

    [Fact]
    public async Task SetErrorBypassesRulesAndIsSupersededByTheNextRealValidation()
    {
        var field = new Field<string>("Username", "validname", new RequiredAttribute());

        field.SetError("Username already taken");
        Assert.Equal("Username already taken", field.ErrorMessage);
        Assert.False(field.Meta.Valid);

        // A real validation pass (Force, via ValidateAsync) supersedes the manual error entirely.
        await field.ValidateAsync();
        Assert.True(field.Meta.Valid);
        Assert.Empty(field.Errors);
    }

    [Fact]
    public async Task AppendErrorAddsWithoutClearingExistingErrors()
    {
        var field = new Field<string>("Name", "", new RequiredAttribute());
        await field.ValidateAsync();
        Assert.Single(field.Errors);

        field.AppendError("Cross-field problem");

        Assert.Equal(2, field.Errors.Count);
        Assert.Contains("Cross-field problem", field.Errors);
    }

    [Fact]
    public void ResetRestoresInitialValueAndClearsTouchedValidatedAndErrors()
    {
        var field = new Field<string>("Email", "start@example.com", new RequiredAttribute());
        field.Value = "";
        field.HandleBlur();
        Assert.NotEmpty(field.Errors);

        field.Reset();

        Assert.Equal("start@example.com", field.Value);
        Assert.False(field.Meta.Touched);
        Assert.False(field.Meta.Validated);
        Assert.Empty(field.Errors);
        Assert.False(field.Meta.Dirty);
    }

    [Fact]
    public void ResetWithANewValueRebasesTheInitialValue()
    {
        var field = new Field<string>("Name", "original");
        field.Value = "changed";

        field.Reset("rebased");

        Assert.Equal("rebased", field.Value);
        Assert.False(field.Meta.Dirty);

        field.Value = "original";
        Assert.True(field.Meta.Dirty);
    }

    [Fact]
    public async Task EveryFailingRuleContributesItsOwnMessageRatherThanBailingAtTheFirst()
    {
        // Deliberately does NOT combine RequiredAttribute with another rule here - confirmed
        // empirically that System.ComponentModel.DataAnnotations.Validator itself special-cases
        // RequiredAttribute to run first and short-circuit every other attribute the moment it
        // fails (regardless of declaration order), so a [Required, MinLength] pair on an empty
        // value would only ever produce Required's own single message. Two ordinary (non-
        // Required) failing rules on the same non-empty value is what actually proves "collect
        // every failure, don't bail after the first" for the attribute path.
        var field = new Field<string>("Code", "ab",
            new System.ComponentModel.DataAnnotations.MinLengthAttribute(10),
            new RegularExpressionAttribute(@"^\d+$"));

        await field.ValidateAsync();

        Assert.Equal(2, field.Errors.Count);
    }

    [Fact]
    public async Task AddRuleAppendsAnAdditionalRuleOnTopOfTheConstructorRules()
    {
        var field = new Field<string>("Name", "ok", new RequiredAttribute());
        field.AddRule(value => value.Length > 10 ? null : "Too short");

        await field.ValidateAsync();

        Assert.Contains("Too short", field.Errors);
    }

    [Fact]
    public async Task HasErrorsAndGetErrorsImplementINotifyDataErrorInfo()
    {
        var field = new Field<string>("Email", "", new RequiredAttribute());
        await field.ValidateAsync();

        Assert.True(field.HasErrors);
        Assert.NotEmpty(field.GetErrors(nameof(Field<string>.Value)).Cast<string>());
    }
}
