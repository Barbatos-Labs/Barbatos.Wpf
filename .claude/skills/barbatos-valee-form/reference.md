# AquariusValee reference

Detail behind [SKILL.md](SKILL.md). Every behaviour here is read from
[`src/Barbatos.Wpf.AquariusValee`](../../../src/Barbatos.Wpf.AquariusValee) and pinned by the named
tests in [`tests/Barbatos.Wpf.AquariusValee.UnitTests`](../../../tests/Barbatos.Wpf.AquariusValee.UnitTests).

Contents:

- [Field and form state in detail](#field-and-form-state-in-detail)
- [Attribute catalogue](#attribute-catalogue) - BCL first, then Valee's own `Rules`
- [What Form.FromModel reads from a model](#what-formfrommodel-reads-from-a-model)
- [Valee.Mask](#valeemask)
- [Valee.Numeric](#valeenumeric)
- [Template-driven forms](#template-driven-forms)

## Field and form state in detail

| `FieldMeta` | Meaning |
| --- | --- |
| `Touched` | set only by `HandleBlur()` or a submit's touch-all step, never by a value change |
| `Validated` | a pass that writes errors has run at least once (blur with `OnBlur`, `ValidateAsync`, submit, or `SetError`/`AppendError`) - a change-triggered pass on a pristine field does not set it; independent of `Touched` (`FieldMetaTests`) |
| `Valid` | written by every pass that completes, silent ones included (a pass whose rule throws writes nothing); defaults `true` - "no errors seen yet", also while an async silent pass is still running |
| `Pending` | an async rule is in flight in a non-silent pass; never raised by sync-only fields or the silent pass; lowered when that pass ends, also when a rule threw, but left to the newer pass when superseded (`FieldRuleExceptionTests`) |
| `Dirty` | computed on every read against the initial value, never latched |

- `HandleChange` (and the `Value` setter) with a value equal to the current one is a no-op,
  including no re-validation. `Errors`, `ErrorMessage` and `ErrorsChanged` are raised only when the
  error list actually changes (`ReviewRegressionTests`), so per-keystroke passes do not churn
  adorners.
- `SetError(message)` / `AppendError(message)` mark the field `Validated` and set `Valid`;
  `SetError(null)` clears. `Form.SetFieldError` and `IValidatableObject` results use them.
- `FormMeta.Touched`/`Pending`/`Dirty` are true if any field is, `Valid` only if every field is.
  They are computed on read, never cached, so `HandleSubmit` reads the true outcome right after
  awaiting validation (`FormMetaStalenessTests`). Only their `PropertyChanged` is coalesced through
  Aquarius `NextTick` (`DispatcherPriority.Background`).
- `Form.HasErrors`/`GetErrors(propertyName)` implement `INotifyDataErrorInfo` over all fields;
  `GetErrors()` (no argument) returns only fields that currently have errors, by name.
- Field names must be unique per form: a repeated `DefineField` name replaces the `Fields` entry,
  but the old field stays in the definition-order list that `Meta` and `GetFirstInvalidField()` walk.
- `GetModel<TModel>()` throws `InvalidOperationException` for a form not built by `FromModel`;
  `SetFieldValue<T>` throws `ArgumentException` for an unknown name or a different `T`.

## Attribute catalogue

### BCL attributes - reach for these first

Usable in both modes (pass to `DefineField`, or declare on a `FromModel` property): `Required`,
`MinLength`/`MaxLength`, `StringLength`, `Range`, `RegularExpression`, `EmailAddress`, `Phone`,
`Url`, `CreditCard`, `Base64String`, `EnumDataType`, `CustomValidation`, and on .NET 8+
`AllowedValues`/`DeniedValues` (strict equality) and `Length` (`[Length(n, n)]` is exact length,
for strings and collections). `Compare` is schema-only. `Views/Showcase/ShowcaseModel.cs` in the
sample exercises many of them.

BCL behaviour that bites:

- `[Required]` runs first and short-circuits the other attributes (`FieldTests`): an empty value
  reports only the Required message.
- `[Url]`, `[EmailAddress]`, `[Phone]` accept `null` but fail `""`. Optional ones go on a `string?`
  property defaulting to `null` (`Models/ProfileEditModel.cs`). That covers only the untouched
  state: `Valee.Field`'s binding writes `""` once the user clears the box, so a cleared optional
  field fails again. Use a rule that returns `null` for `""` there instead of the BCL attribute.
- `[Compare]` compares the typed values with `Equals`.
- `[RegularExpression]` has no `RegexOptions` property; case-insensitivity needs an inline `(?i)`.
  Use `[Pattern]` instead.
- `[Phone]`, `[CreditCard]`, `[EmailAddress]`, `[Url]` and `[EnumDataType]` derive from
  `DataTypeAttribute`, so they already set `IField.DataTypeHint`; adding `[DataType(...)]` beside
  one makes `Form.FromModel` throw `AmbiguousMatchException`, wrapped in a
  `TargetInvocationException` because the field is created through reflection.
- `[CustomValidation(typeof(ShowcaseModel), nameof(ValidateUsername))]` points at
  `public static ValidationResult? ValidateUsername(string username, ValidationContext context)`;
  return `new ValidationResult(message, [context.MemberName!])` or `ValidationResult.Success`.
- `[DisplayFormat]` is presentation only and not reflected.
- Default messages are English only - `System.ComponentModel.Annotations` ships no satellite
  resources - whatever `CurrentUICulture` is; localize with a key or a resx resource.
- `Validator.TryValidateObject(..., validateAllProperties: false)` still checks every
  `[Required]` property and stops before `IValidatableObject`; `Form` deliberately does not use
  it for its model-level pass.

### Valee `Rules` attributes

Namespace `Barbatos.Wpf.AquariusValee.Rules`. Each constructor passes a default message
(`{0}` = display name) to the base `ValidationAttribute(string)` constructor, as `[Required]` and
`[Range]` do: a named `ErrorMessage = "..."` replaces it, `ErrorMessageResourceType`/`ErrorMessageResourceName`
replace it with a resx string (not both - the BCL throws), and `ErrorMessage` reads `null` until
set, on .NET 8 and 10 alike (`RulesTests`) - unlike `[EmailAddress]`/`[Phone]`/`[Url]`, which use an
internal BCL setter.

| Attribute | Construction | Passes when | `null` / `""` | Non-string value |
| --- | --- | --- | --- | --- |
| `PatternAttribute` | `(string pattern)`; init `Options` (`RegexOptions`), `MustMatchEntireValue` (`true`), `MatchTimeoutInMilliseconds` (`2000`); `AllowMultiple` | regex matches - the whole value unless `MustMatchEntireValue = false`; a timeout fails the field | valid | matched against `ToString()` |
| `PropertyRangeAttribute` | `(string minPropertyName, string maxPropertyName)`, properties only, schema only | between the two siblings' current values (`IComparable`) | `null` valid | compared via `IComparable` |
| `AlphaAttribute` | `AsciiOnly` | `char.IsLetter` (ASCII letters with `AsciiOnly`) | valid | **always valid** |
| `AlphaNumericAttribute` | `AsciiOnly` | letters or digits | valid | always valid |
| `AlphaDashAttribute` | `AsciiOnly` | letters, digits, `_`, `-` | valid | always valid |
| `AlphaSpacesAttribute` | `AsciiOnly` | letters or whitespace (ASCII whitespace with `AsciiOnly`) | valid | always valid |
| `DigitsAttribute` | `(int length)`, `Length` | exactly `length` ASCII digits | valid | always valid |
| `NumericStringAttribute` | `()` | ASCII digits only, no sign | valid | always valid |
| `IntegerStringAttribute` | `()` | optional leading `-`, then ASCII digits | valid | always valid |
| `OneOfAttribute` | `(params object?[] allowedValues)`, `AllowedValues` | `Equals` or same `ToString()` (so `1` matches `"1"`) | `null` valid, **`""` invalid unless listed** | compared by `ToString()` |
| `NotOneOfAttribute` | `(params object?[] disallowedValues)`, `DisallowedValues` | neither `Equals` nor same `ToString()` | `null` valid, `""` valid unless listed | compared by `ToString()` |

- The character-class and digit attributes check only `string` values: `[Digits(6)]` on an `int`
  property passes everything. Keep such input as a string, or use `[Range]` on the number.
- `PatternAttribute` with a null or empty pattern throws at construction.
- `[Pattern]` plus `Valee.Mask` is the intended pair: the mask shapes typing, the pattern is the
  verdict, and it must match the **formatted** text the mask produces (`ShowcaseModel.Sku`).

## What Form.FromModel reads from a model

| On the model | Becomes |
| --- | --- |
| each public instance property with getter and setter (not an indexer) | a `Field<TProperty>` named after the property, initial value read from the instance |
| its `ValidationAttribute`s | validation via `Validator.TryValidateProperty` against the live instance |
| `[Display(Name = "...")]` | `Label`, and the display name used for `{0}` |
| `[Display(Prompt = "...")]` | `Prompt` - guidance text for your template; Valee never renders it |
| `[UIHint("...")]` | `UIHint` |
| `[DataType(...)]` or a `DataTypeAttribute` subclass | `DataTypeHint` |
| `[Editable(false)]` | `IsReadOnly = true` - a hint your template must honour; nothing enforces it |
| `[Range]` with a numeric operand type | `Minimum`/`Maximum` for `Valee.Numeric`; `null` for a non-numeric operand type such as `DateTime` |
| an attribute implementing `IDependsOnProperties` | `DependsOn` wired to every named sibling |
| `ValidationAttribute`s on the class itself (`[CustomValidation]`) | run by `ValidateAllAsync`, submit and `RevalidateValidatedAsync`, before `IValidatableObject` |
| `IValidatableObject` on the class | run by the same three once class-level attributes pass, even while property attributes fail |

A change writes back to the model **before** dependents re-validate, so a `[PropertyRange]` or
`[Compare]` on another property sees the new value (`FormFromModelTests`); `Reset` writes back too.
Ad-hoc fields get `Minimum`/`Maximum` from a `RangeAttribute` passed to `DefineField` or the
constructor, or by setting them directly.

## Valee.Mask

`TextBox` only (anything else throws `InvalidOperationException`); `null` or `""` detaches.

| Mask character | Meaning |
| --- | --- |
| `0` | a digit (`char.IsDigit`) |
| `A` | a letter (`char.IsLetter`) |
| `*` | a letter or digit |
| `>` / `<` | upper-case / lower-case everything after it |
| `\` | the next character is a literal (`\0` is a literal zero) |
| anything else | a literal, inserted automatically once the slots before it are filled |

Behaviour pinned by `ValeeMaskTests`:

- Typing `abc1234` into `>AAA-0000` gives `ABC-1234`; the dash appears by itself and the caret
  parks after it. Typing the separator yourself is accepted, not duplicated.
- A keystroke that fits no slot (or lands past the end) is swallowed in `PreviewTextInput`, so it
  never flashes on screen. Length is capped by the mask.
- Backspace over a separator deletes through it instead of letting it reappear.
- Paste is filtered through the mask rather than rejected.
- An initial value from the field is formatted as soon as the mask attaches; an empty box stays
  empty.
- The caret fix runs as a dispatcher operation at `Input` priority, so a test must pump at a
  strictly lower priority to observe it (`ValeeMaskTests` drains at `ContextIdle`).

Masks used in the repo: `>AAA-0000` (SKU), `0000 0000 0000 0000` (card), `>AA` (country code) in
`Views/Showcase/ShowcaseView.xaml`; `(000) 000-0000` in the project template's `SignUpView.xaml`.
In XAML, `<` inside an attribute value must be written `&lt;`.

## Valee.Numeric

`NumericInput { None, Integer, Decimal }`; `TextBox` only; `None` (the default) detaches.

- **Typing**: digits; one decimal separator in `Decimal`; a minus sign only at the front and only
  when the lower bound is unset or negative. Half-finished `-`, `.` and `-.` are allowed because
  they are on the way to a number.
- **Paste** is all-or-nothing: text that would make the box non-numeric is rejected whole, never
  stripped (a pasted `1.234,56` does not become `1.23456`).
- **Bounds**: `aqv:Valee.Minimum`/`aqv:Valee.Maximum` on the element win; otherwise the bound
  field's `IField.Minimum`/`IField.Maximum` - found only through `Valee.Field`/`Valee.For`. Each
  side is independent:

| Declared | On `LostFocus` |
| --- | --- |
| neither | nothing is rewritten, characters are still filtered |
| `Minimum` only | values below it are raised |
| `Maximum` only | values above it are lowered |
| both | the value is kept inside the range |

- **Never clamps while typing** (`50` into 10-100 would otherwise become `100`), and leaves
  unparseable text for the field's rules to judge.
- **Culture**: the separator comes from the element's `FrameworkElement.Language`, the culture
  WPF's binding parses with - `en-US` by default whatever the thread culture. Set `Language` for a
  comma separator; reading another culture would let the box accept text the binding then refuses.
- It corrects; it does not validate. Keep `[Range]` on the model.

## Template-driven forms

`FieldTemplateSelector` looks up, via `container.TryFindResource`, a `DataTemplate` keyed by the
field's `UIHint`, then by `DataTypeHint`'s enum name (`PhoneNumber`, `CreditCard`, `EmailAddress`,
`Url`, `Password`, `MultilineText`, ...), then `"Default"`; with none of those it defers to the base
selector. Adapted from the package README and `Views/Showcase/ShowcaseView.xaml`:

```xml
<UserControl.Resources>
    <aqv:FieldTemplateSelector x:Key="FieldTemplateSelector" />
    <DataTemplate x:Key="MultilineText">
        <TextBox aqv:Valee.Field="{Binding}" AcceptsReturn="True" TextWrapping="Wrap" Height="60" />
    </DataTemplate>
    <DataTemplate x:Key="Password">
        <PasswordBox aqv:Valee.PasswordField="{Binding}" />
    </DataTemplate>
    <DataTemplate x:Key="Default">
        <StackPanel>
            <TextBlock Text="{Binding Label}" FontWeight="SemiBold" />
            <TextBox aqv:Valee.Field="{Binding}" />
            <TextBlock Text="{Binding ErrorMessage}" Foreground="Red" />
        </StackPanel>
    </DataTemplate>
</UserControl.Resources>

<StackPanel aqv:Valee.Form="{Binding ShowcaseForm}">
    <ItemsControl ItemsSource="{Binding ShowcaseForm.Fields.Values}"
                  ItemTemplateSelector="{StaticResource FieldTemplateSelector}" />
</StackPanel>
```

- Inside each template the `DataContext` is the `IField`, so `{Binding}` feeds `Valee.Field` and
  `Label`, `Prompt`, `ErrorMessage`, `Value` bind directly.
- A `ComboBox` template binds `SelectedItem`, so its items must be the field's actual values
  (`PlanPicker` in `ShowcaseView.xaml` lists `sys:String` items matching `SubscriptionPlan` names).
- When `Label`/`Prompt` hold lookup keys, run them through the same `SafeMessageConverter` as the
  error text - Showcase does.
- `[Editable(false)]` arrives as `IsReadOnly`; bind `IsReadOnly`/`IsEnabled` in the template if the
  field must not be edited.
