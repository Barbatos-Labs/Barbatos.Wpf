# Barbatos.Wpf.AquariusValee

![Barbatos.Wpf.AquariusValee logo](https://raw.githubusercontent.com/Barbatos-Labs/Barbatos.Wpf/main/build/aquariusvalee-logo.svg)

### *A vee-validate-style form-validation system - for WPF*

**Reactive `Field<T>`/`Form` validation state, `System.ComponentModel.DataAnnotations` as the
primary schema mechanism, `INotifyDataErrorInfo`-native error display, and
`Valee.Field`/`Valee.Form`/`Valee.For` binding sugar - built directly on top of
[Barbatos.Wpf.Aquarius](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.Aquarius/README.md).**

[![NuGet](https://img.shields.io/nuget/v/Barbatos.Wpf.AquariusValee.svg)](https://www.nuget.org/packages/Barbatos.Wpf.AquariusValee)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Barbatos.Wpf.AquariusValee.svg)](https://www.nuget.org/packages/Barbatos.Wpf.AquariusValee)
[![GitHub stars](https://img.shields.io/github/stars/Barbatos-Labs/Barbatos.Wpf?style=social)](https://github.com/Barbatos-Labs/Barbatos.Wpf/stargazers)
[![License](https://img.shields.io/github/license/Barbatos-Labs/Barbatos.Wpf)](https://github.com/Barbatos-Labs/Barbatos.Wpf/tree/main/LICENSE.md)

---

## 📖 Documentation Menu

* **[Getting Started](#getting-started)**
  * [Introduction](#introduction)
  * [Quick Start](#quick-start)
* **[Field](#field)**
  * [`FieldMeta`](#fieldmeta)
* **[Form](#form)**
  * [Schema mode: `Form.FromModel`](#schema-mode-formfrommodel)
  * [Ad-hoc mode: `Form.DefineField`](#ad-hoc-mode-formdefinefield)
  * [`FormMeta`](#formmeta)
  * [`HandleSubmit`](#handlesubmit)
  * [Focusing the first invalid field on submit](#focusing-the-first-invalid-field-on-submit)
* **[Validation triggers](#validation-triggers)**
* **[Cross-field validation](#cross-field-validation)**
  * [The schema-mode equivalent: `PropertyRangeAttribute`](#the-schema-mode-equivalent-propertyrangeattribute-and-idependsonproperties)
* **[Async rules and `Meta.Pending`](#async-rules-and-metapending)**
* **[XAML integration](#xaml-integration)**
  * [`Valee.Field` / `Valee.PasswordField`](#valeefield--valeepasswordfield)
  * [`Valee.Form` / `Valee.For`](#valeeform--valeefor)
  * [`Valee.Mask` - guiding what gets typed](#valeemask---guiding-what-gets-typed)
  * [`Valee.Numeric` - number-only boxes](#valeenumeric---number-only-boxes-and-snapping-back-into-range)
  * [`FieldTemplateSelector` - auto-selecting a control per field](#fieldtemplateselector---auto-selecting-a-control-per-field)
  * [Already native: error display](#already-native-error-display)
  * [`SafeMessageConverter` - localization](#safemessageconverter---localization-without-becoming-dependent-on-a-localizer)
* **[Rules](#rules)**
  * [`PatternAttribute`](#patternattribute---regex-and-why-regularexpression-isnt-always-enough)
  * [A full review of `System.ComponentModel.DataAnnotations`](#a-full-review-of-systemcomponentmodeldataannotations---whats-native-whats-custom-what-doesnt-apply)
* **[Deliberately Out of Scope](#deliberately-out-of-scope)**
* **[Ecosystem](#ecosystem)**
  * [Repository layout](#repository-layout)
* **[API Reference](#api-reference)**
* **[Community](#community)**

---

## Getting Started

### Introduction

#### What is Barbatos.Wpf.AquariusValee?

AquariusValee ports [vee-validate](https://vee-validate.logaretm.com/v4/)'s form-validation
concepts to WPF: reactive per-field state (`touched`/`dirty`/`valid`/`validated`/`pending`),
form-level aggregation, submit gating, and cross-field validation - combined with
`System.ComponentModel.DataAnnotations` as the primary "schema" mechanism, since most of
vee-validate's own built-in rules already map directly onto existing `ValidationAttribute`s.

> **Prerequisites**
>
> The rest of this document assumes basic familiarity with C#, XAML, WPF data binding, and
> `System.ComponentModel.DataAnnotations`.
>
> **Familiar with vee-validate?** `Field`/`Form`, `Meta`, and the trigger/validation-mode
> vocabulary below are deliberately similar - a head start if you've used it before, though
> nothing here requires that background. Where this port genuinely diverges from vee-validate's
> real behavior, it's called out explicitly with the reason why.

### Quick Start

```powershell
dotnet add package Barbatos.Wpf.AquariusValee
```

```xml
<Window ...
        xmlns:aqv="http://schemas.barbatos.co/aquariusvalee/2026/xaml">
```

A hand-authored form, no model class:

```csharp
public sealed partial class LoginViewModel : ObservableObject
{
    public Form LoginForm { get; } = new();
    public Field<string> Email { get; }
    public Field<string> Password { get; }
    public IAsyncRelayCommand LogInCommand { get; }

    public LoginViewModel()
    {
        Email = LoginForm.DefineField(nameof(Email), "", new RequiredAttribute(), new EmailAddressAttribute());
        Password = LoginForm.DefineField(nameof(Password), "", new RequiredAttribute());

        LogInCommand = LoginForm.HandleSubmit(async values =>
        {
            await SignInAsync((string)values[nameof(Email)]!, (string)values[nameof(Password)]!);
        });
    }
}
```

```xml
<TextBox aqv:Valee.Field="{Binding Email}" />
<TextBlock Text="{Binding Email.ErrorMessage}" Foreground="Red" />

<PasswordBox aqv:Valee.PasswordField="{Binding Password}" />
<TextBlock Text="{Binding Password.ErrorMessage}" Foreground="Red" />

<Button Content="Log In" Command="{Binding LogInCommand}" />
```

See `samples/Barbatos.Wpf.AquariusValee.Sample` in the repo for a complete, working app: a login
form, a signup form demonstrating both cross-field validation styles side by side, a
`Form.FromModel`-driven profile editor, a schema-less custom-rules example with a real async
rule, and a wider "attribute showcase" tab tying together `UIHint`/`DataType`-driven templates
with the rest of the DataAnnotations review below. Every tab's static text, and every
`ValidationRule<T>`/`Form.SetFieldError` message the sample writes by hand, is also localized
into English and Vietnamese via [Barbatos.i18n](https://www.nuget.org/packages/Barbatos.i18n) -
purely a sample-level demonstration of the two packages working together side by side, not a
dependency AquariusValee itself takes on.

**Scaffolding a form**: the `aqv-form` `dotnet new` item template (see
[`templates/`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/templates/README.md#aqv-form---valee-form-view--viewmodel-item-template)
in the repo) generates a schema-mode `XyzView.xaml` (`aqv:Valee.Form` on the container, a bound
control and an error `TextBlock` per field, a submit button) / `XyzView.xaml.cs` /
`XyzViewModel.cs` (the `Form`, its typed `Field<T>`s, the `HandleSubmit` command and the
attribute-carrying model class) in one step:

```powershell
dotnet new install ./templates/Barbatos.Wpf.AquariusValee/item-templates/valee-form   # once
dotnet new aqv-form -n Checkout --namespace MyApp.Features.Checkout
```

---

## Field

`Barbatos.Wpf.AquariusValee.Validation.Field<T>` - one field's reactive value and validation
state, the WPF/C# counterpart of vee-validate's `useField`. Usable standalone
(`new Field<string>(...)`, e.g. a single validated search box with no surrounding form concept)
or via [`Form.DefineField`](#ad-hoc-mode-formdefinefield)/[`Form.FromModel`](#schema-mode-formfrommodel).

```csharp
public Field<string> Email { get; } = new("Email", "", new RequiredAttribute(), new EmailAddressAttribute());
```

- **`Value`** (`T`) - the current value. Setting it is equivalent to calling `HandleChange`.
- **`Meta`** (`FieldMeta`) - see below.
- **`Errors`** (`IReadOnlyList<string>`) / **`ErrorMessage`** (`string?`, the first error).
- **`HandleChange(T value, bool shouldValidate = true)`** - a no-op (including no re-validation)
  if `value` equals the current value.
- **`HandleBlur()`** - always sets `Meta.Touched = true`, regardless of trigger configuration;
  additionally validates if [`Triggers`](#validation-triggers) includes `OnBlur`.
- **`ValidateAsync()`** - an explicit, always-visible validation pass. An exception a rule
  throws comes out of the returned task - see
  [When a rule throws](#when-a-rule-throws).
- **`SetError(string? message)`** - a manual, server-driven error (e.g. "email already taken")
  that bypasses rule execution entirely; a subsequent real validation pass supersedes it.
- **`AppendError(string message)`** - adds an error without clearing existing ones.
- **`Reset()`** / **`Reset(T value)`** - the second overload also rebases the initial value (so
  `Meta.Dirty` becomes `false` against the new value, not the original one).
- **`AddRule(...)`** - three overloads (`ValidationAttribute`, `ValidationRule<T>`,
  `AsyncValidationRule<T>`) to layer an additional rule onto an already-constructed field.
- **`DependsOn<TSource>(Field<TSource> source)`** - see [Cross-field validation](#cross-field-validation).

### `FieldMeta`

| Property | Meaning |
| --- | --- |
| `Touched` | Flips to `true` only via `HandleBlur()` or a form submit's touch-all-fields step - never via a value change. |
| `Dirty` | A **live** comparison of the current value against the initial value, computed fresh on every read - not a flag latched the first time the value changes. Typing a value and then retyping the original value un-dirties the field again. |
| `Valid` | Defaults to `true` - "no errors yet" is not the same as "confirmed valid." Updated on *every* validation pass regardless of mode, so it stays accurate even while `Errors` itself is being withheld (see the next row). Combine with `Dirty`/`Touched` before showing any "this looks good" UI. |
| `Validated` | Whether a real (non-silent) validation pass has run at least once - **independent of `Touched`**, and what actually gates whether a background pass may *write* visible `Errors`. A field validated only via `ValidateAsync()` reports `Validated=true, Touched=false`; a blurred field with change-triggered validation off reports `Touched=true, Validated=false`. |
| `Pending` | `true` while an asynchronous rule is in flight, and lowered when that pass ends - with a result or with an exception - see [Async rules](#async-rules-and-metapending). |

This split (`Valid` always fresh, `Errors`/`Validated` gated) is what lets a freshly-constructed
field report accurate validity immediately without ever flashing a red error before the user has
interacted with anything.

---

## Form

`Barbatos.Wpf.AquariusValee.Validation.Form` - aggregates a set of `Field<T>`s, the WPF/C#
counterpart of vee-validate's `useForm`. Two ways to build one:

### Schema mode: `Form.FromModel`

Reflects an existing `ValidationAttribute`-decorated model class - one `Field<T>` per public
settable property, `[Display(Name = "...")]` becomes each field's `Label`. Each field's value
stays live-synced back onto the model instance as it changes; `Form.GetModel<TModel>()` returns
that same instance, always up to date.

```csharp
public sealed class SignupModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";
}

var form = Form.FromModel(new SignupModel());
```

`IValidatableObject` on the model (DataAnnotations' own native cross-field mechanism) is honored
too, as is any `ValidationAttribute` on the model class itself - checked once per
`HandleSubmit`/explicit `ValidateAllAsync()`, and any resulting message is layered onto every
field named in `ValidationResult.MemberNames`, on top of that field's own rule errors. It runs
whether or not the properties' own attributes pass, so a submit with required fields still empty
shows the model-level errors alongside them rather than one round later.

That last point is a deliberate departure from calling `Validator.TryValidateObject`, which
`Form` used to do. Its `validateAllProperties: false` mode reads as "skip the property
attributes" but does not: confirmed against the BCL, it still checks every property's
`[Required]`, reports each failure under the property's name, and then stops before the
class-level attributes and `IValidatableObject.Validate`. Every field had already reported that
same Required message itself, so it showed twice, and the cross-field rule never ran. The
model-level pass now runs only the two steps the fields do not - class-level attributes, then
`Validate` once those pass, in the BCL's own order.

The flip side: `Validate` is no longer shielded from property values that fail their own
attributes, as it is under `TryValidateObject`. Write it to tolerate an empty, `null` or
out-of-range property - skip a comparison whose operand is missing rather than dereference it -
since an exception from `Validate` ends the submit the way a throwing rule does (see
[When a rule throws](#when-a-rule-throws)).

**`CompareAttribute` (and anything else that reflects into a sibling property) only works this
way** - against a real model instance. `Validator.TryValidateProperty` resolves the "other
property" by reflecting `ValidationContext.ObjectInstance`, which only has something to find when
that instance is a genuine model object. See [Cross-field validation](#cross-field-validation) for
the ad-hoc-mode equivalent.

### Ad-hoc mode: `Form.DefineField`

No model class - build a form field by field. Each call also returns the strongly-typed
`Field<T>`, so a ViewModel keeps its own typed reference for use in code/XAML; there is no need
to subclass `Form` or look fields up by name in ordinary use.

```csharp
public Form LoginForm { get; } = new();
public Field<string> Email { get; }

public LoginViewModel()
{
    Email = LoginForm.DefineField(nameof(Email), "", new RequiredAttribute(), new EmailAddressAttribute());
}
```

Four overloads, matching `Field<T>`'s own constructors: no rules, `params ValidationAttribute[]`,
`params ValidationRule<T>[]`, `params AsyncValidationRule<T>[]`.

### `FormMeta`

Mirrors vee-validate's own aggregation exactly: `Touched`/`Pending` are true if *any* field is;
`Valid` requires *every* field to be valid (and no stray form-level error - see
`IValidatableObject` above); `Dirty` is true if *any* field is dirty. Every property is a plain
on-demand getter, computed fresh from the form's fields on every read - never cached, so a
synchronous read immediately after `await`-ing a validation pass is always accurate.

### `HandleSubmit`

```csharp
public IAsyncRelayCommand HandleSubmit(
    Func<IReadOnlyDictionary<string, object?>, Task> onValid,
    Action<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, IReadOnlyList<string>>>? onInvalid = null)
```

Returns a real `ICommand` (a `CommunityToolkit.Mvvm.Input.AsyncRelayCommand`), bindable directly
to `Button.Command`. Mirrors vee-validate's own `handleSubmit` stage order exactly:

1. Touch **every** field, regardless of which one the user actually edited.
2. `IsSubmitting = true`, `SubmitCount++`.
3. Validate every field (plus `IValidatableObject`, for a `FromModel`-built form).
4. Call `onValid` only if the whole form is valid; otherwise `onInvalid` with the current
   values/errors.
5. `IsSubmitting = false`, in a `finally`.

If a rule throws during step 3, neither callback runs: the submit ends with that exception
(after step 5), which faults the command's `ExecuteAsync` task and - for a command a `Button`
invoked - is rethrown on the UI thread by `AsyncRelayCommand`, the same route an exception from
`onValid` takes.

**`CanExecute` is gated only on `!IsSubmitting`, never on `Meta.Valid`.** Clicking Submit on an
invalid, untouched form is exactly what's supposed to force every field's `Touched` to `true` and
surface its errors - that's the whole point of a submit attempt, matching vee-validate's own real
behavior.

Other `Form` members: `SetFieldValue<T>`, `SetFieldError`, `ResetForm`, `GetValues`, `GetErrors`,
`ValidateAllAsync`, `Fields` (`IReadOnlyDictionary<string, IField>`, for name-based lookup -
`IField` is the non-generic view every `Field<T>` also implements),
`GetFirstInvalidField()` (in `DefineField`/`FromModel`-reflection definition order).

### Focusing the first invalid field on submit

`Form.SubmitInvalid` (`event Action<IField>?`) fires with `GetFirstInvalidField()`'s result
whenever a `HandleSubmit`-driven submit turns out invalid - `Form` itself has no WPF-control
dependency, so it cannot focus anything on its own. `Valee.Form` (see
[XAML integration](#xaml-integration) below) is what actually subscribes to this and moves
keyboard focus - so **this behavior is entirely automatic wherever `Valee.Form` is set somewhere
in the visual tree**, no extra code needed in the ViewModel or the View:

```xml
<StackPanel aqv:Valee.Form="{Binding LoginForm}">
    <TextBox aqv:Valee.For="Email" />
    ...
</StackPanel>
```

A form with no `Valee.Form` anywhere in its tree simply doesn't get this behavior - subscribe to
`Form.SubmitInvalid` yourself (it is a plain, headless C# event) if you need the same behavior
without using `Valee.Form`.

To find that control, `Valee` remembers the last control bound to each field - through a weak
reference, so a `Form` that outlives its View (one owned by a singleton ViewModel, say) does not
keep the closed View alive. A `ConditionalWeakTable` keyed by the field alone is not enough for
that: it keeps each value alive for as long as its key is, which is how an earlier version pinned
the last bound control, and its whole visual tree, to the form.

---

## Validation triggers

```csharp
[Flags]
public enum ValidateTrigger { None = 0, OnChange = 1, OnBlur = 2, Default = OnChange | OnBlur }
```

vee-validate has four independent booleans (`validateOnBlur`/`Change`/`Input`/`ModelUpdate`)
because a DOM `<input>` genuinely fires two separate native events - `input` (every keystroke)
and `change` (on commit/blur) - that a WPF `TextBox` does not have as two distinct native events
in the same way. Porting four booleans would cargo-cult a distinction WPF doesn't make. Instead:

- **`OnChange`** fires whenever `Field.Value` actually changes (`HandleChange`), regardless of
  *which* WPF event caused it - a field doesn't need to know whether the XAML side chose
  `UpdateSourceTrigger=PropertyChanged` (live, per-keystroke) or `LostFocus` (commit-on-blur).
- **`OnBlur`** fires purely from `Valee.Field`'s own `LostFocus` hook, independent of whether the
  value even changed.
- **`Default = OnChange | OnBlur`** matches vee-validate's own `<Field>`-component default.

Set per field: `Email.Triggers = ValidateTrigger.OnBlur;`.

---

## Cross-field validation

Two mechanisms, matching whichever `Form` mode you're in - **they are not interchangeable, pick
the one that matches your form**:

- **Schema mode** (`Form.FromModel`) - `[Compare(nameof(Password))]` on the model property,
  `[PropertyRange(nameof(Min), nameof(Max))]` for a dynamic bound (see below), or
  `IValidatableObject.Validate(...)` for anything a single attribute can't express. All three are
  DataAnnotations-native (the latter two ship with this package), and all work because validation
  reflects into the real, live model instance.
- **Ad-hoc mode** (`Form.DefineField`) - `Field<T>.DependsOn<TSource>(Field<TSource> source)` plus
  a hand-written rule that reads the other field's `.Value` directly:

  ```csharp
  ConfirmPassword = LoginForm.DefineField(nameof(ConfirmPassword), "", new RequiredAttribute());
  ConfirmPassword.AddRule(value => value == Password.Value ? null : "Passwords must match.");
  ConfirmPassword.DependsOn(Password);
  ```

  `DependsOn` re-validates the dependent field (using the `ValidatedOnly` write mode - see
  [`Field`](#field)) whenever the source field's value changes, so a sibling changing doesn't
  suddenly surface an error on a field the user hasn't touched yet. Built on Aquarius's
  `Watch.On` specifically (not `Watch.Effect`) - `Watch.On` only reacts to the source's *value*
  changing, never to `Errors`/`Meta` changes a validation pass itself causes, which is what keeps
  two mutually-dependent fields from infinite-looping.

  `DependsOn` isn't limited to one dependency or to equality checks - call it once per source to
  validate against a **dynamic bound driven by other fields**, e.g. a price that must stay between
  a min and a max the user can also edit:

  ```csharp
  Price = form.DefineField(nameof(Price), 50,
      value => value < MinPrice.Value ? $"Must be at least {MinPrice.Value}." : null,
      value => value > MaxPrice.Value ? $"Must be at most {MaxPrice.Value}." : null);
  Price.DependsOn(MinPrice);
  Price.DependsOn(MaxPrice);
  ```

  `[Range(MinPrice, MaxPrice)]` cannot express this at all - C# attribute constructor arguments
  must be compile-time constants - so for an **ad-hoc** field, `DependsOn` plus a closure like
  above is the only option. Changing either `MinPrice` or `MaxPrice` re-validates `Price` against
  the *current* value of both, even though `Price` itself wasn't touched. See the sample's
  "Custom rules" tab for a live demo.

### The schema-mode equivalent: `PropertyRangeAttribute` and `IDependsOnProperties`

For a `Form.FromModel` field, the declarative equivalent is
[`PropertyRangeAttribute`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.AquariusValee/API-REFERENCE.md#propertyrangeattribute-sealed-class--validationattribute-idependsonproperties),
which reflects two named sibling properties' *current* values the same way `[Compare]` reflects
one:

```csharp
public int MinPrice { get; set; } = 10;
public int MaxPrice { get; set; } = 100;

[PropertyRange(nameof(MinPrice), nameof(MaxPrice))]
public int Price { get; set; } = 50;
```

No manual `DependsOn` call is needed here - `PropertyRangeAttribute` implements
`IDependsOnProperties`, and `Form.FromModel` scans every property's attributes for it after every
field already exists, calling `Field<T>.DependsOn<TSource>` automatically for each named
dependency. Any custom `ValidationAttribute` can opt into the same auto-wiring by implementing
`IDependsOnProperties` itself - it is not specific to `PropertyRangeAttribute`.

Two things this attribute-based form gives up, both because a `ValidationAttribute` is plain
reflected metadata with no dependency-injection access, unlike a hand-written rule closure:

- **It only works in `Form.FromModel` mode.** It reflects `ValidationContext.ObjectInstance` - the
  real model - which an ad-hoc `Field<T>` never has (see
  [`Field<T>.AddRule(ValidationAttribute)`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.AquariusValee/API-REFERENCE.md#fieldt-sealed-class--observableobject-ifield-idisposable)'s
  notes).
- **Its message cannot reach a DI localizer.** `ErrorMessage` is a plain `string.Format`-style
  template (`{0}` = display name, `{1}`/`{2}` = the live min/max) formatted when the rule runs -
  there is no service container reachable from inside an attribute, and a lookup key stored there
  for a display-time converter loses the live bounds. What does work is DataAnnotations' own resx
  route: `ErrorMessageResourceType`/`ErrorMessageResourceName` name a resource string that is
  itself the template, so it is localized *and* keeps `{1}`/`{2}`. The sample's Showcase tab uses
  this attribute and deliberately leaves this one message in plain English, contrasting with
  "Custom rules"' fully-localized ad-hoc version of the identical Price/MinPrice/MaxPrice idea.

Pick ad-hoc `DependsOn` when the message needs an app localizer or DI; pick
`PropertyRangeAttribute` when the field already lives on a `FromModel` model and a plain-text or
resx message is acceptable.

---

## Async rules and `Meta.Pending`

```csharp
public Field<T> AddRule(AsyncValidationRule<T> rule);
// delegate Task<string?> AsyncValidationRule<in T>(T value, CancellationToken cancellationToken);
```

`Meta.Pending` is `true` while an async rule is in flight. Race-safety uses a per-field
`CancellationTokenSource`, cancelled and replaced on every new validation call - **always forward
the token to whatever you `await` inside the delegate**; that's what turns a superseded call into
genuinely cancelled work, not just a discarded result (an upgrade over vee-validate's own
"let the stale call finish, ignore its result" approach - real cancellation is idiomatic .NET and
wasn't available to vee-validate, since JS Promises aren't cancellable).

```csharp
Username.AddRule(async (value, cancellationToken) =>
{
    var available = await availabilityService.IsAvailableAsync(value, cancellationToken);
    return available ? null : "That username is already taken.";
});
```

#### When a rule throws

A rule of any shape may throw - a network failure, a timeout, a bug - and the field stays
consistent:

- **`Meta.Pending` is lowered** whichever way the pass ends, so neither the field nor its form is
  left "checking..." for good.
- **Nothing is written.** `Errors`, `Meta.Valid` and `Meta.Validated` keep what the previous pass
  wrote; a half-finished pass (the sync rules passed, the async one never answered) is not
  presented as a verdict.
- **The exception reaches whoever awaits the pass** - `ValidateAsync()`, `ValidateAllAsync()`,
  `RevalidateValidatedAsync()`, or the `HandleSubmit` command. A pass nobody awaits (typing, blur,
  a `DependsOn` source changing, the silent pass a constructor or `Reset` runs) rethrows it on the
  `Dispatcher` of the UI thread that started the pass, so it reaches
  `Application.DispatcherUnhandledException` like any other UI-thread failure instead of
  disappearing with a discarded `Task` - the same "observe the fault, rethrow through the
  Dispatcher" route Aquarius's async lifecycle hooks take. A pass started off a UI thread - on a
  thread-pool thread (a value set after `ConfigureAwait(false)`, say), or on a worker that is
  neither STA nor running a dispatcher operation - has no pumping dispatcher to go to, even if it
  owns one, so there the exception stays on the task, for `TaskScheduler.UnobservedTaskException`.
  That is the same test Aquarius's `NextTick` uses to decide whether the calling thread is a UI
  thread.
- **Only Valee's own cancellation is swallowed.** A pass superseded by a newer one ends quietly
  whatever its rule then throws, since its result would be discarded anyway (and not every client
  library reports cancellation as an `OperationCanceledException`). An
  `OperationCanceledException` from anywhere else is a real failure: `HttpClient` reports its own
  `Timeout` as a `TaskCanceledException`, and that is not mistaken for supersession.

So throwing is safe, but it is not a message. When a failed check should read as an error under
the field rather than an unhandled exception, catch it inside the rule and return that message:

```csharp
Username.AddRule(async (value, cancellationToken) =>
{
    try
    {
        var available = await availabilityService.IsAvailableAsync(value, cancellationToken);
        return available ? null : "That username is already taken.";
    }
    catch (HttpRequestException)
    {
        return "Could not check the username right now.";
    }
});
```

---

## XAML integration

### `Valee.Field` / `Valee.PasswordField`

```xml
<TextBox aqv:Valee.Field="{Binding Email}" />
<PasswordBox aqv:Valee.PasswordField="{Binding Password}" />
```

`Valee.Field` binds a control's natural value property (`TextBox.Text`,
`ToggleButton.IsChecked`, `Selector.SelectedItem`, `RangeBase.Value`) to an `IField`/`Field<T>`
and wires `LostFocus` to `HandleBlur` - something a plain `Binding` cannot express at all, since
WPF has no built-in "touched" concept. It also sets `UpdateSourceTrigger=PropertyChanged` -
worth knowing, since WPF's own default for `TextBox.Text` is `LostFocus`; without this attached
property, a plain `{Binding Field.Value}` would only push a value (and therefore only trigger
`OnChange` validation) on blur, not per keystroke.

Because `Field<T>` implements `INotifyDataErrorInfo` directly, this binding already gets WPF's
native red-border validation adorner for free - see
[Already native: error display](#already-native-error-display).

`Valee.PasswordField` is `PasswordBox`-specific: wires `PasswordChanged`/`LostFocus` but **never
creates a real `Binding` on `Password`**, mirroring Aquarius's own `Directives.Model` refusing to
bind it (`Password` is deliberately not a `DependencyProperty`, for security reasons). A direct
consequence: there's no `BindingExpression` for the native adorner to attach to here - show this
field's error with a plain bound `TextBlock` instead (`{Binding Password.ErrorMessage}`).

### `Valee.Form` / `Valee.For`

Sugar over repeating `Valee.Field="{Binding LoginForm.Fields[Email]}"` on every control:

```xml
<StackPanel aqv:Valee.Form="{Binding LoginForm}">
    <TextBox aqv:Valee.For="Email" />
    <PasswordBox aqv:Valee.PasswordField="{Binding LoginForm.Fields[Password]}" />
</StackPanel>
```

`Valee.For` resolves the ambient `Valee.Form` via a one-shot ancestor walk at
`FrameworkElement.Loaded`, mirroring `Barbatos.Wpf.AquariusRouter`'s own `RouterView` pattern.

**The bound `Form` must already be non-null by the time the View loads** - construct it
synchronously in the ViewModel's constructor, not populated later/asynchronously. There is no
retry if it's still `null` at that moment (the same documented constraint `Router.Current`
carries: "must be set before first navigation").

Setting `Valee.Form` also wires the automatic "focus the first invalid field on submit" behavior
- see [Focusing the first invalid field on submit](#focusing-the-first-invalid-field-on-submit).

`Form` itself implements `INotifyDataErrorInfo` (`Form.HasErrors` aggregates every field), and
`aqv:Valee.Form="{Binding LoginForm}"` is a real WPF `Binding` under the hood -
`ValidatesOnNotifyDataErrors` defaults to `true` for *any* `Binding`, not just ones on editing
controls. Left alone, that would make WPF's own native validation adorner (a red border) wrap the
*whole panel* the instant a single field anywhere in the form is invalid, not just the offending
control. Setting `Valee.Form` suppresses that adorner on its own element (an empty
`Validation.ErrorTemplate`) - `Validation.HasError`/`GetErrors` on that element are left intact for
anyone who wants them (e.g. a form-level "there are errors" banner), only the *visual* is
suppressed. Each individual field's own adorner (from `Valee.Field`/`Valee.PasswordField`, see
[error display](#already-native-error-display)) is unaffected.

### `Valee.Mask` - guiding what gets typed

A validation attribute gives a verdict *after the fact*. `Valee.Mask` shapes the value *as it is
being typed* - separators appear on their own, characters that cannot go in the next slot are
ignored, and the caret always sits where the next character will actually land:

```xml
<TextBox aqv:Valee.Field="{Binding Sku}" aqv:Valee.Mask=">AAA-0000" />
```

| Mask character | Meaning |
| --- | --- |
| `0` | a required digit |
| `A` | a required letter |
| `*` | a required letter or digit |
| `>` / `<` | upper-case / lower-case everything that follows |
| `\` | treat the next character as a literal (`\0` is a literal zero) |
| anything else | a literal, inserted automatically once the slots before it are filled |

Typing `abc1234` into `>AAA-0000` produces `A`, `AB`, `ABC-` (the dash arrives by itself, caret
after it), `ABC-1`, … `ABC-1234`. A `@` aimed at a letter slot, a letter aimed at a digit slot, or
anything past the end of the mask is swallowed before it can appear on screen. Backspacing over a
separator deletes through it rather than letting it reappear, and pasted text is filtered through
the mask instead of rejected. The **formatted** text is what reaches the bound `Field<T>`, so the
model sees `"ABC-1234"`.

#### A mask guides; an attribute decides

Keep the real rule on the model - a mask is deliberately permissive (a half-typed `"AB"` is a fine
intermediate state, and the user can always tab away early), so pair it with
[`PatternAttribute`](#patternattribute---regex-and-why-regularexpression-isnt-always-enough) or any
other attribute for the actual verdict:

```csharp
[Pattern(@"[A-Z]{3}-\d{4}", Options = RegexOptions.IgnoreCase)]
public string Sku { get; set; } = "";
```

**The two cannot be collapsed into one**, and it is worth being precise about why, because "just
use the regex to filter keystrokes" is the obvious first instinct:

- A regex is **not positional**. It cannot answer "what may I type at index 3?" - `[A-Z]{3}` and
  `[A-Z]+` give no fixed index for the dash that follows.
- Running the **full** pattern per keystroke rejects every prefix. Against `^[A-Z]{3}-\d{4}$`, the
  string `"A"` does not match, nor `"AB"`, nor `"ABC"` - so all eight keystrokes of `ABC-1234`
  would be blocked and the field could never be filled at all.
- .NET has no partial-match API to fix that with (Java's `Matcher.hitEnd()` has no `System.Text.RegularExpressions`
  equivalent).

This is the same split WinForms' `MaskedTextBox`, imask.js and Cleave.js all make, for the same
reason.

#### When a mask is the wrong tool

A mask needs one fixed layout. Where the layout genuinely varies - a phone number, whose length and
grouping differ by country - do not force one; a hand-written `PreviewTextInput`/`TextChanged` pair
that accepts digits and formats loosely is the honest answer. The sample's
`Views/Showcase/InputMask.cs` keeps exactly one such field for that reason, and uses `Valee.Mask`
for every other.

### `Valee.Numeric` - number-only boxes, and snapping back into range

```xml
<TextBox aqv:Valee.Field="{Binding Age}"   aqv:Valee.Numeric="Integer" />
<TextBox aqv:Valee.Field="{Binding Price}" aqv:Valee.Numeric="Decimal" aqv:Valee.Minimum="0" />
```

**Typing** is filtered per keystroke (and paste with it): letters, punctuation, a second decimal
separator, a minus sign anywhere but the front - all dropped before they appear. Half-finished
input is deliberately still allowed, because `-`, `.` and `-.` are all legal states on the way to
a real number and rejecting them would make the field impossible to fill. A number's grammar is
simple enough to decide prefix-by-prefix, which is exactly why this can filter typing where
[`PatternAttribute`](#valeemask---guiding-what-gets-typed) cannot.

**Bounds are optional and independent** - set only a floor, only a ceiling, or neither:

| Declared | Behavior |
| --- | --- |
| neither | characters are filtered, nothing is ever rewritten |
| `Minimum` only | anything below it is raised; no upper limit |
| `Maximum` only | anything above it is lowered; no lower limit |
| both | value is kept inside the range |

A `Minimum` of zero or above also removes the minus key entirely - no value that could satisfy it
is negative.

#### Bounds come from `[Range]` on their own

If you don't declare them, they are taken from the bound field's
`IField.Minimum`/`IField.Maximum`, which `Form.FromModel` fills in from the property's own
`[Range]`. A model that already says:

```csharp
[Range(13, 120, ErrorMessage = "Age must be between 13 and 120.")]
public int Age { get; set; } = 29;
```

needs nothing repeated in XAML - `aqv:Valee.Numeric="Integer"` alone will snap `150` down to
`120`. `Valee.Minimum`/`Valee.Maximum` override the discovered values when you do set them, which
is also how you bound a field that has no `[Range]` at all.

#### Two things this deliberately does *not* do

- **It never clamps while typing.** With a 10-100 range, snapping mid-keystroke would rewrite the
  first `5` of `50` into `10`, leaving the user with `100`. Correction waits for
  `LostFocus`, the way every numeric editor behaves.
- **It does not validate.** Clamping silently rewrites what was typed, so keep `[Range]` (or an
  equivalent rule) on the model regardless: that is what reports a genuinely bad value, and what
  still runs when a value arrives from anywhere other than this text box.

> **Culture note.** The accepted decimal separator is read from the element's
> `FrameworkElement.Language`, because that is the culture WPF's own binding will use to parse the
> text back. Reading it from `CultureInfo.CurrentCulture` instead would let the box accept a
> character the binding then refuses, and the value would silently stop updating. `Language`
> defaults to `en-US` regardless of thread culture - set it if you want comma separators.

### `FieldTemplateSelector` - auto-selecting a control per field

```xml
<Window.Resources>
    <aqv:FieldTemplateSelector x:Key="FieldTemplateSelector" />
    <DataTemplate x:Key="MultilineText">
        <TextBox aqv:Valee.Field="{Binding}" AcceptsReturn="True" TextWrapping="Wrap" Height="60" />
    </DataTemplate>
    <DataTemplate x:Key="Password">
        <PasswordBox aqv:Valee.PasswordField="{Binding}" />
    </DataTemplate>
    <DataTemplate x:Key="Default">
        <TextBox aqv:Valee.Field="{Binding}" />
    </DataTemplate>
</Window.Resources>

<ItemsControl ItemsSource="{Binding ProfileForm.Fields.Values}"
              ItemTemplateSelector="{StaticResource FieldTemplateSelector}" />
```

Picks a `DataTemplate` resource keyed on `IField.UIHint` first (mirrors `[UIHint("...")]` on a
`FromModel`-reflected property), then `IField.DataTypeHint`'s enum name (mirrors
`[DataType(...)]` - e.g. `DataType.Password`, `DataType.MultilineText`), then a resource literally
named `"Default"`. Only the resource-key *convention* is prescribed - which real control each key
maps to is entirely up to your own `DataTemplate`s, the same way ASP.NET Dynamic Data never
shipped opinions about what a "MultilineText" template should look like either. `[Editable(false)]`
becomes `IField.IsReadOnly` - a hint for your own templates to react to (e.g. bind `IsEnabled`);
Valee does not enforce it itself.

`UIHint`/`DataTypeHint` pick *which control* renders a field - a completely separate concern from
*guidance text* telling the user how to fill it in. For the latter, `[Display(Prompt = "...")]`
becomes `IField.Prompt` - a plain string, not rendered by Valee itself, for your own template to
show as a watermark, tooltip, or (as the sample does) a small helper line under the field:

```csharp
[Phone]
[Display(Name = "Phone number", Prompt = "e.g. +1 555 123 4567")]
public string? PhoneNumber { get; set; }
```

```xml
<TextBlock Text="{Binding Prompt}" FontSize="11" Foreground="Gray" />
```

### Already native: error display

A plain `<TextBlock Text="{Binding Email.ErrorMessage}" />` already shows a field's current error
- no dedicated `ErrorMessage` control is provided, matching this repo's own established "patterns
that are already native" precedent (see Aquarius's README). Likewise, a plain
`<TextBox Text="{Binding Email.Value}" />` (no `Valee.Field` at all) already gets the native red
adorner for free via `Field<T>`'s own `INotifyDataErrorInfo` - `Valee.Field`'s only genuinely
non-redundant job is the touched/trigger-timing wiring described above.

### `SafeMessageConverter` - localization, without becoming dependent on a localizer

**AquariusValee has no dependency on any localization library, and never requires one.** A plain
literal message is an ordinary, fully supported way to use it:

```csharp
[Required(ErrorMessage = "Enter a valid email address.")]
[Display(Name = "Email address", Prompt = "you@example.com")]
public string Email { get; set; } = "";
```

Localizing is an application choice layered on top: store lookup keys in `ErrorMessage`/
`[Display]` and resolve them at display time with your localizer's converter. That introduces one
sharp edge worth knowing about, because it is **silent**: a typical localizing converter returns
`null` for a key it does not recognise, and `null` bound to `TextBlock.Text` renders as *nothing at
all*. One typo, one key nobody registered, or one message deliberately written as ordinary prose
becomes an invisible error message - strictly worse than an untranslated one.

`SafeMessageConverter` is the guard rail. Wrap your localizing converter in it (it is the XAML
content property, so just nest it) and anything untranslatable is shown **as written** instead of
disappearing:

```xml
<UserControl.Resources>
    <aqv:SafeMessageConverter x:Key="Message">
        <i18n:LocalizeConverter />
    </aqv:SafeMessageConverter>
</UserControl.Resources>
...
<TextBlock Text="{Binding ErrorMessage, Converter={StaticResource Message}}" Foreground="Red" />
```

`Inner` is an ordinary `IValueConverter`, so this is not tied to any one localization package -
Barbatos.i18n, an `IStringLocalizer`-backed converter, ResX, or a hand-written one all work. An
inner converter that *throws* is treated the same as one that returned nothing. Leave `Inner`
unset and it is a pass-through, which is exactly right for an app that does not localize at all.

#### Refreshing messages after a language change

A message stored as a lookup key and resolved by a converter updates for free when the binding
re-reads. A rule that resolves its text **eagerly** - unavoidable when the message interpolates a
runtime value, e.g. `$"Must be at least {MinPrice.Value}."` - captured the old language at the
moment it ran, and no later culture switch re-runs it. Call
[`Form.RevalidateValidatedAsync()`](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.AquariusValee/API-REFERENCE.md#form-sealed-class--observableobject-inotifydataerrorinfo-idisposable)
after switching:

```csharp
localizationCultureManager.SetCulture(newCulture);
await myForm.RevalidateValidatedAsync();
```

It re-runs only fields already `Meta.Validated`, so nothing sprouts an error on a field the user
has never touched. vee-validate has the same characteristic and documents the same remedy.

---

## Rules

`Barbatos.Wpf.AquariusValee.Rules` - a small number of custom `ValidationAttribute` subclasses,
only for gaps confirmed (by direct comparison against vee-validate's real built-in rule library)
to have no `System.ComponentModel.DataAnnotations` equivalent:

| Attribute | Purpose |
| --- | --- |
| `AlphaAttribute` | Letters only. Unicode-aware via `char.IsLetter` by default (broader than vee-validate's own ~20 hand-picked locale tables, since .NET's Unicode category data already covers essentially every script at once) - set `AsciiOnly` to restrict to `a-z`/`A-Z`. |
| `AlphaNumericAttribute` | Letters and digits only. |
| `AlphaDashAttribute` | Letters, digits, `_`, and `-` only (slugs/usernames). |
| `AlphaSpacesAttribute` | Letters and whitespace only (a person's full name). |
| `OneOfAttribute` / `NotOneOfAttribute` | Value must (not) equal one of a fixed set of literals, using vee-validate's own **loose, cross-type** matching (`1` and `"1"` match each other) - see the next section for when to reach for these instead of the BCL's own, stricter equivalent. |
| `DigitsAttribute` | An exact-length numeric string (a PIN/OTP code). |
| `NumericStringAttribute` / `IntegerStringAttribute` | "Does this raw string look like a number/integer" - for input not yet parsed into a numeric type. |
| `PatternAttribute` | Regex matching **with a real `RegexOptions` property** - see below. |

All of them treat a `null` value as valid (pair with `[Required]` if emptiness itself should
fail) - matching the `isEmpty`-short-circuit convention vee-validate's own equivalent rules use -
and all but one treat an empty string as valid too. Two differences are worth knowing:

- **`OneOfAttribute` rejects `""`** unless `""` is one of the allowed values: an empty string is
  a value like any other there, not "nothing entered". (`NotOneOfAttribute` accepts it unless it
  is listed.)
- **The character-class and digit attributes check strings only.** `Alpha`, `AlphaNumeric`,
  `AlphaDash`, `AlphaSpaces`, `Digits`, `NumericString` and `IntegerString` pass *any* non-string
  value, so `[Digits(6)]` on an `int` property accepts everything - keep that input as a `string`,
  or use `[Range]` on the number. `PatternAttribute` instead matches a non-string value's
  `ToString()`.

Every one of them, `PropertyRangeAttribute` included, supplies its default message the way
`[Required]`, `[Range]` and `[Compare]` do - through the protected `ValidationAttribute`
constructor - so a named `ErrorMessage` replaces it, and
`ErrorMessageResourceType`/`ErrorMessageResourceName` work for resx-based localization (setting
`ErrorMessage` *and* a resource name is rejected, as on any BCL attribute). `ErrorMessage` itself
therefore reads `null` until you set it, exactly as `new RequiredAttribute().ErrorMessage` does
(verified on .NET 8 and .NET 10). `[EmailAddress]`, `[Phone]` and `[Url]` do read their default
back through `ErrorMessage`, but only through an internal setter a third-party attribute cannot
use.

`PropertyRangeAttribute` (documented under
[Cross-field validation](#the-schema-mode-equivalent-propertyrangeattribute-and-idependsonproperties))
lives in this namespace too, but is a different shape - property-only and `ValidationContext`-aware.

### `PatternAttribute` - regex, and why `[RegularExpression]` isn't always enough

The BCL's `RegularExpressionAttribute` covers the ordinary case and needs no extra package - **use
it whenever a plain, case-sensitive pattern is all you need**. It has one hard limitation, though,
confirmed by inspection: it exposes **no `RegexOptions` property at all**. `IgnoreCase`,
`Multiline`, `CultureInvariant`, `IgnorePatternWhitespace` and friends are reachable only by
embedding inline flags like `(?i)` into the pattern string - workable, but easy to typo and
invisible to anyone skimming the attribute.

```csharp
[Pattern(@"[A-Z]{3}-\d{4}",
    Options = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
    ErrorMessage = "Use the format ABC-1234.")]
public string Sku { get; set; } = "";
```

Two more differences from the BCL attribute worth knowing:

- **Anchored by default.** `MustMatchEntireValue` defaults to `true`, so `[Pattern("[a-z]+")]`
  rejects `abc123` - no forgotten `^...$` trap. Set it to `false` for a "contains a match" search.
- **A regex timeout fails the field instead of throwing.** `MatchTimeoutInMilliseconds` (2000 by
  default, the same bound the BCL attribute uses) means a catastrophically backtracking
  pattern/input pair degrades into an invalid field rather than an exception on the UI thread.

### A full review of `System.ComponentModel.DataAnnotations` - what's native, what's custom, what doesn't apply

Every public type in the namespace was reviewed (not just the ones vee-validate happens to have
an equivalent for) to decide whether it's a validation concern this package should surface at
all. Three buckets:

**Directly usable as-is - no Barbatos wrapper needed, just pass it to `DefineField`/put it on a
`FromModel` property**: `RequiredAttribute`, `MinLengthAttribute`/`MaxLengthAttribute`,
`RangeAttribute`, `RegularExpressionAttribute`, `StringLengthAttribute`, `CompareAttribute`
(cross-field, `FromModel` mode only - see [Cross-field validation](#cross-field-validation)),
`EmailAddressAttribute`, `PhoneAttribute`, `UrlAttribute`, `CreditCardAttribute`,
`Base64StringAttribute` (well-formed Base64 text), `EnumDataTypeAttribute` (a string/underlying
value matches one of a given enum's members - confirmed via `new EnumDataTypeAttribute(typeof(DayOfWeek))`),
`CustomValidationAttribute` (points at your own `public static ValidationResult? Method(object value, ValidationContext context)`
- a genuine middle ground between a full DataAnnotations attribute and a `ValidationRule<T>`
delegate, useful when you want the exact same check reusable as a real attribute elsewhere too),
and, **since .NET 8**, `AllowedValuesAttribute`/`DeniedValuesAttribute` (strict-equality
`one_of`/`not_one_of` - confirmed empirically stricter than this package's own `OneOfAttribute`/
`NotOneOfAttribute`: `1` and `"1"` do *not* match there) and `LengthAttribute` (min **and** max in
one attribute - `new LengthAttribute(n, n)` is the exact-length case, confirmed to already cover
both strings and collections correctly, which is why this package ships no `ExactLengthAttribute`
of its own).

**Two confirmed, non-obvious BCL gotchas worth knowing before you rely on any of the above:**
- `UrlAttribute`/`EmailAddressAttribute`/`PhoneAttribute` (and, by the same `DataTypeAttribute`-derived
  family resemblance, presumably others in that family) treat a `null` value as valid (deferring
  emptiness to a separate `[Required]`) but treat an **empty string** as a genuine format
  failure - different from `RequiredAttribute` itself, and different from the length-based
  attributes. A `string` property with an optional `[Url]`/`[EmailAddress]`/`[Phone]` and a
  non-nullable `""` default will read as permanently invalid until the user types something -
  give it a `string?` property with a `null` default instead if the field is meant to be
  optional (see `ProfileEditModel.Website` in the sample for a worked example).
- `Validator.TryValidateValue`/`TryValidateProperty` special-case `RequiredAttribute` to run
  first and short-circuit every other attribute the instant it fails (see [Field](#field)'s own
  remarks) - `confirmed`→`CompareAttribute` also has one documented caveat of its own:
  `CompareAttribute` compares typed values via `Equals`, while vee-validate's `confirmed` coerces
  both sides through `String()` first.

**A separate hint/metadata system, not validation - reflected for [UIHint/DataType hints](#fieldtemplateselector---auto-selecting-a-control-per-field), not for pass/fail rules**:
`DisplayAttribute` (→ `Label`), `UIHintAttribute` (→ `IField.UIHint`), `DataTypeAttribute` (→
`IField.DataTypeHint`), `EditableAttribute` (→ `IField.IsReadOnly`). `DisplayFormatAttribute` is
purely presentation (`DataFormatString`/`NullDisplayText`) with no validation role at all - not
reflected by this package; apply it with an `IValueConverter` in your own template if needed.

**Deliberately not applicable to form validation at all** - these describe an *entity/ORM*
(Entity Framework) or *scaffolding* (ASP.NET Dynamic Data) concern, not a value's validity, and
none of them are even `ValidationAttribute` subclasses (so `Validator` never looks at them
regardless): `KeyAttribute`, `ConcurrencyCheckAttribute`, `TimestampAttribute`,
`AssociationAttribute`, `MetadataTypeAttribute`, `AssociatedMetadataTypeTypeDescriptionProvider`,
`ScaffoldColumnAttribute`, `DisplayColumnAttribute`, `FilterUIHintAttribute`. Left alone rather
than forced into this package's scope.

---

## Deliberately Out of Scope

- **File-upload rules** (vee-validate's `ext`/`mimes`/`size`/`image`/`dimensions`) - no BCL or
  WPF-native equivalent; would need `OpenFileDialog`/`FileInfo`/`BitmapDecoder`-specific code, a
  meaningfully different problem from the rest of this package.
- **A dedicated field-array/repeating-section helper** (vee-validate's `useFieldArray`) - use
  `ObservableCollection<Form>` (one real `Form` per row) instead. vee-validate's own `FieldArray`
  is complex specifically because it addresses fields by a string path that shifts identity on
  every insert/remove/reorder - a problem that doesn't exist here, since each row can just be a
  real C# object with real identity.
- **A `generateMessage`-style global message hook** - DataAnnotations already gives every
  attribute two localization routes: a lookup key in `ErrorMessage` resolved at display time (see
  [`SafeMessageConverter`](#safemessageconverter---localization-without-becoming-dependent-on-a-localizer)),
  and `ErrorMessageResourceType`/`ErrorMessageResourceName` for resx - which Valee's own `Rules`
  attributes support too. What it does **not** give you is localized *default* text: the shared
  framework that contains `System.ComponentModel.Annotations` ships no satellite resource
  assemblies, so `[Required]`'s "The {0} field is required." stays English whatever
  `CurrentUICulture` is (checked on .NET 8 and .NET 10 under `fr-FR`, `de-DE`, `vi-VN` and
  `ja-JP`). Give every attribute whose message a user can see an explicit key or resource.

---

## Ecosystem

Ships as a **single package** - `Validation`, `Rules`, and `Xaml` are all included; there is
nothing else to install beyond Aquarius itself.

### Repository layout

- `src/Barbatos.Wpf.AquariusValee` - the library.
- `samples/Barbatos.Wpf.AquariusValee.Sample` - a complete sample application exercising every
  feature area above, localized into English/Vietnamese via `Barbatos.i18n` (see its own
  `WpfProgram.cs`/`App.xaml.cs` for the DI + startup-culture wiring).
- `templates/Barbatos.Wpf.AquariusValee/item-templates/valee-form` - the `aqv-form` item template.
- `tests/Barbatos.Wpf.AquariusValee.UnitTests` - the unit test suite.

---

## API Reference

Due to the extensive nature of the library's interfaces, classes, and properties, the
full API Reference has been moved to a dedicated document modeled after Microsoft's
official .NET documentation format.

👉 **[Read the Full API Reference](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/src/Barbatos.Wpf.AquariusValee/API-REFERENCE.md)** 👈

---

## Community

See the [root README](https://github.com/Barbatos-Labs/Barbatos.Wpf#community) for
maintainers, support, and license information - shared across every package in this
repository.
