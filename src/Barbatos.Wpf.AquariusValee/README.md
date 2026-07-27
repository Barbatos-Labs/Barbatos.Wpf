# Barbatos.Wpf.AquariusValee

![Barbatos.Wpf.AquariusValee logo](https://github.com/Barbatos-Labs/Barbatos.Wpf/blob/main/build/aquariusvalee-logo.svg?raw=true)

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
* **[Form](#form)**
  * [Schema mode: `Form.FromModel`](#schema-mode-formfrommodel)
  * [Ad-hoc mode: `Form.DefineField`](#ad-hoc-mode-formdefinefield)
  * [`HandleSubmit`](#handlesubmit)
* **[Validation triggers](#validation-triggers)**
* **[Cross-field validation](#cross-field-validation)**
* **[Async rules and `Meta.Pending`](#async-rules-and-metapending)**
* **[XAML integration](#xaml-integration)**
  * [`Valee.Field` / `Valee.PasswordField`](#valeefield--valeepasswordfield)
  * [`Valee.Form` / `Valee.For`](#valeeform--valeefor)
  * [Already native: error display](#already-native-error-display)
* **[Rules](#rules)**
* **[Deliberately Out of Scope](#deliberately-out-of-scope)**
* **[Ecosystem](#ecosystem)**
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
- **`ValidateAsync()`** - an explicit, always-visible validation pass.
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
| `Pending` | `true` while an asynchronous rule is in flight - see [Async rules](#async-rules-and-metapending). |

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
too - checked once per `HandleSubmit`/explicit `ValidateAllAsync()`, and any resulting message is
layered onto every field named in `ValidationResult.MemberNames`, on top of that field's own rule
errors.

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
  or `IValidatableObject.Validate(...)` for anything a single attribute can't express. Both are
  DataAnnotations' own native mechanisms, and both work because validation reflects into the
  real, live model instance.
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

All eight treat a `null`/empty value as valid (pair with `[Required]` if emptiness itself should
fail) - matching the `isEmpty`-short-circuit convention vee-validate's own equivalent rules use.

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
- **A `generateMessage`-style global message hook** - `ValidationAttribute` already has
  `ErrorMessageResourceType`/`ErrorMessageResourceName` for resx-based localization, and the
  BCL's own built-in attributes already localize their default text via `CurrentUICulture` with
  zero code. "Bulk-localize the whole app" is already solved beneath this package.

---

## Ecosystem

Ships as a **single package** - `Validation`, `Rules`, and `Xaml` are all included; there is
nothing else to install beyond Aquarius itself.

### Repository layout

- `src/Barbatos.Wpf.AquariusValee` - the library.
- `samples/Barbatos.Wpf.AquariusValee.Sample` - a complete sample application exercising every
  feature area above, localized into English/Vietnamese via `Barbatos.i18n` (see its own
  `WpfProgram.cs`/`App.xaml.cs` for the DI + startup-culture wiring).
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
