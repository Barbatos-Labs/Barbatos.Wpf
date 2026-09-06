---
name: barbatos-valee-form
description: Build or fix a validated form with Barbatos.Wpf.AquariusValee - Form and Field<T>, schema mode via Form.FromModel and DataAnnotations vs ad-hoc DefineField, FieldMeta touched/dirty/valid/pending, HandleSubmit, validation triggers, cross-field rules (DependsOn, Compare, PropertyRangeAttribute, IDependsOnProperties), async rules with cancellation, and the aqv: markup (Valee.Field/PasswordField/Form/For/Mask/Numeric, FieldTemplateSelector, SafeMessageConverter). Use this for any input validation - required fields, email or phone format, password confirmation, digits-only or masked input, numeric range clamping, checking a username against the server while typing, localized or i18n validation messages, showing a server-returned field error, disabling or gating a submit button, and "the error message does not show up". Vietnamese prompts land here too: form nhập liệu, validate, kiểm tra dữ liệu, bắt buộc nhập, sai định dạng email hoặc số điện thoại, xác nhận mật khẩu, chỉ cho nhập số, thông báo lỗi, đa ngôn ngữ.
---

# Barbatos.Wpf.AquariusValee

vee-validate's shape for WPF, with `System.ComponentModel.DataAnnotations` as the schema
mechanism and `INotifyDataErrorInfo` as the error channel.

Full docs: [`src/Barbatos.Wpf.AquariusValee/README.md`](../../src/Barbatos.Wpf.AquariusValee/README.md) ·
[`API-REFERENCE.md`](../../src/Barbatos.Wpf.AquariusValee/API-REFERENCE.md).
Reference app: [`samples/Barbatos.Wpf.AquariusValee.Sample`](../../samples/Barbatos.Wpf.AquariusValee.Sample)
— Login (ad-hoc), Signup (both modes side by side), ProfileEdit (schema), CustomRules, Showcase.

```xml
xmlns:aqv="http://schemas.barbatos.co/aquariusvalee/2026/xaml"
```

---

## Choose the mode first — they are not interchangeable

**Schema mode** — you already have (or can write) a model class:

```csharp
public sealed class SignupModel
{
    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.InvalidEmail")]
    public string Email { get; set; } = "";

    [Required, MinLength(8)]
    public string Password { get; set; } = "";

    [Required]
    [Compare(nameof(Password), ErrorMessage = "Validation.PasswordsMustMatch")]
    public string ConfirmPassword { get; set; } = "";
}
```

```csharp
SchemaForm = Form.FromModel(new SignupModel());
SchemaEmail = (Field<string>)SchemaForm.Fields["Email"];
```

**Ad-hoc mode** — no model class:

```csharp
public Form LoginForm { get; } = new();

Email = LoginForm.DefineField("Email", "",
    new RequiredAttribute { ErrorMessage = "Validation.Required" },
    new EmailAddressAttribute { ErrorMessage = "Validation.InvalidEmail" });
```

**`[Compare]`, `[PropertyRange]`, and `IValidatableObject` only work in schema mode** — they
reflect `ValidationContext.ObjectInstance`, which an ad-hoc field does not have. Conversely,
`Field<T>.AddRule(ValidationAttribute)` has **no effect** on a `FromModel`-built field (its
validation is driven entirely by the live model property's own attributes) — declare the
attribute on the model instead. `AddRule(ValidationRule<T>)` and `AddRule(AsyncValidationRule<T>)`
work in both modes.

`GetModel<TModel>()` throws unless the form was built via `FromModel<TModel>()`.

---

## Submitting

```csharp
LogInCommand = LoginForm.HandleSubmit(async values =>
{
    await Task.Delay(...);
    if ((string)values["Password"]! != expected)
    {
        LoginForm.SetFieldError("Password", "Login.InvalidCredentials");  // server-driven
        return;
    }
});
```

Stage order (identical to vee-validate):

1. Touch **every** field, regardless of what the user edited.
2. `IsSubmitting = true`, `SubmitCount++`.
3. Validate every field (plus `IValidatableObject` in schema mode).
4. `onValid` if the whole form is valid, else `onInvalid(values, errors)`.
5. `IsSubmitting = false`, in a `finally`.

**`CanExecute` is gated only on `!IsSubmitting`, never on `Meta.Valid`.** Clicking Submit on an
empty form is *supposed* to touch every field and surface every error — do not "fix" this by
binding the button's enablement to validity.

`Form.SubmitInvalid` fires with `GetFirstInvalidField()` on an invalid submit. `Valee.Form`
subscribes automatically and focuses + `BringIntoView()`s the offending control — so **focus
management is free wherever `Valee.Form` is set**. Without it, subscribe to the plain headless
event yourself.

---

## Field state

| `FieldMeta` | Meaning |
| --- | --- |
| `Touched` | flips only via `HandleBlur()` or a submit's touch-all step |
| `Validated` | a real (non-`Silent`) pass has run at least once; gates whether a `ValidatedOnly` pass may write errors |
| `Pending` | an async rule is in flight; a sync-only field never raises it |
| `Valid` | updated on every pass regardless of write mode |
| `Dirty` | on-demand comparison of current vs initial — never cached |

`FormMeta`'s four members are all on-demand getters too, never cached; their `PropertyChanged`
is batched through `NextTick`. Every setter is `internal` — read-only from outside the package.

Triggers are `[Flags] ValidateTrigger { None, OnChange = 1, OnBlur = 2, Default = OnChange | OnBlur }`.
`OnChange` fires from `HandleChange` whatever WPF event caused it; `OnBlur` fires from
`Valee.Field`'s `LostFocus` hook even if nothing changed. Set per field:
`Email.Triggers = ValidateTrigger.OnBlur;`.

Two behaviors worth knowing:

- Every failing rule contributes its message — rules are **not** bailed after the first — with
  one BCL exception: `Validator` special-cases `RequiredAttribute` to run first and short-circuit,
  so `[Required, MinLength(3)]` on an empty value reports only the Required message.
- `Errors`/`ErrorMessage` notifications fire **only when the list actually changes**. Validation
  re-runs per keystroke and usually produces the identical result; notifying regardless would make
  WPF re-evaluate every adorner on every character.

---

## Cross-field validation — three mechanisms

**Ad-hoc:** `DependsOn` + a closure reading the other field.

```csharp
ConfirmPassword.AddRule(value => value == Password.Value ? null : "Signup.PasswordMismatch");
ConfirmPassword.DependsOn(Password);
```

Call `DependsOn` once per source for a dynamic range:

```csharp
Price = form.DefineField(nameof(Price), 50,
    v => v < MinPrice.Value ? $"Must be at least {MinPrice.Value}." : null,
    v => v > MaxPrice.Value ? $"Must be at most {MaxPrice.Value}." : null);
Price.DependsOn(MinPrice);
Price.DependsOn(MaxPrice);
```

`[Range(MinPrice, MaxPrice)]` cannot express this — attribute arguments must be compile-time
constants. `DependsOn` re-validates in `ValidatedOnly` mode so a sibling change never surfaces an
error on an untouched field, and it is built on `Watch.On` (not `Watch.Effect`) specifically so
that a validation pass's own `Errors`/`Meta` changes cannot cause an infinite loop.

**Schema, one sibling:** `[Compare(nameof(Password))]`. It gives the right verdict when the
confirm field is validated, but **does not auto-wire re-validation** — add
`ConfirmPassword.DependsOn(Password)` yourself or fixing the password leaves a stale error.

**Schema, dynamic bounds:** `[PropertyRange(nameof(MinPrice), nameof(MaxPrice))]`. It implements
`IDependsOnProperties`, which `Form.FromModel` scans for after every field exists, so
`DependsOn` **is** wired automatically. Any custom `ValidationAttribute` can opt into the same
auto-wiring by implementing that interface.

`PropertyRangeAttribute` gives up one thing: **its `ErrorMessage` cannot be an i18n key**
(`{0}` = display name, `{1}`/`{2}` = live bounds). An attribute has no DI access. Pick ad-hoc
`DependsOn` when the message must be localized.

---

## Async rules

```csharp
Username.AddRule(async (value, cancellationToken) =>
{
    var available = await service.IsAvailableAsync(value, cancellationToken);
    return available ? null : "Validation.UsernameTaken";
});
```

**Always forward the token.** A per-field `CancellationTokenSource` is cancelled and replaced on
every new pass — forwarding is what turns a superseded call into genuinely cancelled work rather
than a discarded result. `Field<T>.Dispose()` cancels in-flight work and clears `Pending`, so a
cancelled field cannot strand its form as permanently pending.

---

## XAML

```xml
<StackPanel aqv:Valee.Form="{Binding LoginForm}">
    <TextBox aqv:Valee.For="Email" />
    <TextBlock Text="{Binding Email.ErrorMessage}" Foreground="Red" />

    <PasswordBox aqv:Valee.PasswordField="{Binding Password}" />
    <TextBlock Text="{Binding Password.ErrorMessage}" Foreground="Red" />

    <Button Content="Log in" Command="{Binding LogInCommand}" />
</StackPanel>
```

- `Valee.Field` binds the control's natural value property, wires `LostFocus` → `HandleBlur`, and
  sets `UpdateSourceTrigger=PropertyChanged` (WPF's default for `TextBox.Text` is `LostFocus`,
  which would make `OnChange` only fire on blur).
- `Valee.PasswordField` is `PasswordBox`-only and **never creates a `Binding` on `Password`**
  (not a DP, for security). Consequence: **no native adorner** — always show a `PasswordBox`'s
  error with a bound `TextBlock`.
- `Valee.For="Email"` resolves the ambient `Valee.Form` by a one-shot ancestor walk at `Loaded`.
  **The `Form` must already be non-null by load time** — construct it synchronously in the
  ViewModel's constructor. There is no retry.
- `Valee.Form` also suppresses WPF's own native adorner **on that panel only** (`Form` implements
  `INotifyDataErrorInfo`, so the whole panel would otherwise get a red border whenever any field
  is invalid). `Validation.HasError`/`GetErrors` stay intact for a form-level banner.
- Error display is **already native** — a plain `TextBlock` bound to `ErrorMessage`, plus WPF's
  red adorner from `Field<T>`'s own `INotifyDataErrorInfo`. No `ErrorMessage` control ships.

### Input helpers

```xml
<TextBox aqv:Valee.Field="{Binding Sku}"   aqv:Valee.Mask=">AAA-0000" />
<TextBox aqv:Valee.Field="{Binding Age}"   aqv:Valee.Numeric="Integer" />
<TextBox aqv:Valee.Field="{Binding Price}" aqv:Valee.Numeric="Decimal" aqv:Valee.Minimum="0" />
```

`Valee.Mask` (`TextBox` only): `0` digit, `A` letter, `*` alphanumeric, `>`/`<` casing, `\`
escape, anything else a literal auto-inserted once preceding slots fill. Backspace deletes
through separators, paste is filtered, length is capped. The **formatted** text reaches the field.
**A mask guides, an attribute decides** — a mask is deliberately permissive, so pair it with
`PatternAttribute`.

`Valee.Numeric` (`TextBox` only): filters typing and paste to a valid numeric prefix — `-`, `.`,
`-.` all pass as legal intermediate states. Clamps **on `LostFocus`, never while typing**. Bounds
are optional and independent; unset, they come from `IField.Minimum`/`Maximum`, which
`Form.FromModel` fills from `[Range]`. A `Minimum >= 0` also removes the minus key.

`FieldTemplateSelector` picks a `DataTemplate` by `IField.UIHint` → `DataTypeHint` enum name →
a resource named `"Default"`, so a whole form can render from
`ItemsSource="{Binding Form.Fields.Values}"`. `[Display(Prompt = "...")]` → `IField.Prompt`
(guidance text) is a separate concern from `[UIHint]`/`[DataType]` (which control). `[Editable(false)]`
→ `IField.IsReadOnly` is a hint your template must honor — Valee does not enforce it.

`SafeMessageConverter` wraps a localizing `IValueConverter` and falls back to the raw value when
the inner one returns nothing or throws — so an untranslated key or a deliberately literal
message stays visible instead of rendering as empty text:

```xml
<aqv:SafeMessageConverter x:Key="LocalizeConverter">
    <i18n:LocalizeConverter />
</aqv:SafeMessageConverter>
```

Set `ErrorMessage` to a **lookup key**, never pre-resolved text — the converter resolves it at
display time, so a language switch is reflected. After a language change call
`Form.RevalidateValidatedAsync()` to refresh already-displayed messages without touching pristine
fields.

---

## Rules: prefer the BCL

Check `System.ComponentModel.DataAnnotations` first. Shipped here only for confirmed gaps:

| Attribute | Why it exists |
| --- | --- |
| `PatternAttribute` | `[RegularExpression]` has no `RegexOptions` property; this adds `Options`, `MustMatchEntireValue` (default true), `MatchTimeoutInMilliseconds` (default 2000, a timeout fails the field) |
| `PropertyRangeAttribute` | dynamic bounds from sibling properties + `IDependsOnProperties` auto-wiring |
| `AlphaAttribute` / `AlphaNumeric` / `AlphaDash` / `AlphaSpaces` | with an `AsciiOnly` switch |
| `DigitsAttribute` / `NumericString` / `IntegerString` | exact-count / ASCII-digit checks |
| `OneOfAttribute` / `NotOneOfAttribute` | **loose** cross-type matching (`1` matches `"1"`). Prefer the BCL's strict `AllowedValues`/`DeniedValues` (.NET 8+) unless you specifically want loose |

No `ExactLength` ships — `new LengthAttribute(n, n)` (.NET 8+) already covers it. All of these
except `PropertyRange` treat null/empty as valid; pair with `[Required]`.

Modeling note: make an optional string field `string?` with a `null` default, not `""`.
`[Url]`/`[EmailAddress]` treat `null` as valid but `""` as a format failure, so a `""` default
makes an optional field permanently invalid until typed into.
