# Barbatos.Wpf.AquariusValee API Reference

A member-by-member reference for every public type in `Barbatos.Wpf.AquariusValee`. See the
[README](README.md) for a concept-first walkthrough with worked examples - this document is for
looking up a specific member's exact signature/behavior.

## Namespaces

| Namespace | Contents |
| --- | --- |
| [`Barbatos.Wpf.AquariusValee.Validation`](#namespace-barbatoswpfaquariusvaleevalidation) | `Field<T>`, `IField`, `FieldMeta`, `Form`, `FormMeta`, `IDependsOnProperties`, the rule delegates, `ValidateTrigger`. |
| [`Barbatos.Wpf.AquariusValee.Rules`](#namespace-barbatoswpfaquariusvaleerules) | Custom `ValidationAttribute` subclasses, for confirmed gaps in `System.ComponentModel.DataAnnotations`. |
| [`Barbatos.Wpf.AquariusValee.Xaml`](#namespace-barbatoswpfaquariusvaleexaml) | `Valee` (the attached-property family connecting `Field`/`Form` to real WPF controls), `NumericInput`, `FieldTemplateSelector`, and `SafeMessageConverter`. |

All three are mapped onto one XAML namespace, `http://schemas.barbatos.co/aquariusvalee/2026/xaml`
(prefix `aqv`), via `[assembly: XmlnsDefinition]` in `AssemblyInfo.cs`.

---

## Namespace: `Barbatos.Wpf.AquariusValee.Validation`

### `IField` (interface)

The non-generic view of a `Field<T>` - lets `Form` hold a heterogeneous field collection, and
lets `Valee.For` resolve a field by name without knowing its value type at compile time. Extends
`INotifyPropertyChanged`/`INotifyDataErrorInfo`.

| Member | Type | Meaning |
| --- | --- | --- |
| `Name` | `string` | This field's name. |
| `Label` | `string?` | A friendly display name - mirrors `[Display(Name = "...")]` for a `FromModel`-built field. |
| `Prompt` | `string?` | Short guidance text (e.g. an expected format/example) - mirrors `[Display(Prompt = "...")]`. Distinct from `UIHint`/`DataTypeHint`, which pick a control, not text; not rendered by Valee itself. |
| `UIHint` | `string?` | Names which template/control should present this field - mirrors `[UIHint("...")]`. See `Xaml.FieldTemplateSelector`. |
| `DataTypeHint` | `DataType?` | The semantic kind of data this field holds - mirrors `[DataType(...)]`. A closed fallback for `Xaml.FieldTemplateSelector` when `UIHint` is unset. |
| `IsReadOnly` | `bool` | Whether the user should be able to edit this field - mirrors `[Editable(false)]`. Defaults `true`; not enforced by Valee itself. |
| `Minimum` / `Maximum` | `double?` | Numeric bounds, reflected from `[Range]` (by `Form.FromModel`, or from a `RangeAttribute` passed to a `Field<T>` constructor). Input-layer metadata for [`Valee.Numeric`](#namespace-barbatoswpfaquariusvaleexaml) - **not** used by validation, `[Range]` itself remains the verdict. `null` when unbounded, or when the range is non-numeric (e.g. the `DateTime` overload). |
| `Meta` | `FieldMeta` | This field's reactive validation state. |
| `Errors` | `IReadOnlyList<string>` | Every current validation error. Empty when valid. |
| `ErrorMessage` | `string?` | The first entry in `Errors`, or `null`. |
| `RawValue` | `object?` | The current value, boxed - a non-generic escape hatch for `Form.GetValues()`. |
| `ValidateAsync()` | `Task` | Runs every rule and writes the result (a `Force`-mode pass). |
| `HandleBlur()` | `void` | Sets `Meta.Touched = true`; validates too if triggers say so. |
| `SetError(string? message)` | `void` | Manual/server-driven error, bypasses rule execution. |
| `AppendError(string message)` | `void` | Adds an error without clearing existing ones. |
| `Reset()` | `void` | Resets to the original initial value; clears touched/validated/errors. |

### `FieldMeta` (sealed class, `ObservableObject`)

| Member | Type | Meaning |
| --- | --- | --- |
| `Touched` | `bool` | Flips only via `HandleBlur()` or a submit's touch-all-fields step. |
| `Validated` | `bool` | Whether a real (non-`Silent`) pass has run at least once - gates whether a `ValidatedOnly` pass may write `Errors`. Independent of `Touched`. |
| `Pending` | `bool` | `true` while an async rule is in flight. A field with only synchronous rules never raises it at all (the pass completes before anyone could observe it), and `Dispose` clears it so a field cancelled mid-flight cannot strand its form as permanently pending. |
| `Valid` | `bool` | Defaults `true`. Updated on every pass regardless of write mode. |
| `Dirty` | `bool` | On-demand getter: live comparison of current vs. initial value - never cached. |

All setters are `internal` - read-only from outside this package; only `Field<T>` mutates them.

### `Field<T>` (sealed class) : `ObservableObject`, `IField`, `IDisposable`

| Member | Signature | Meaning |
| --- | --- | --- |
| Constructor | `Field(string name, T initialValue)` | No validation rules. |
| Constructor | `Field(string name, T initialValue, params ValidationAttribute[] rules)` | |
| Constructor | `Field(string name, T initialValue, params ValidationRule<T>[] rules)` | |
| Constructor | `Field(string name, T initialValue, params AsyncValidationRule<T>[] rules)` | |
| `Name` | `string { get; }` | |
| `Label` | `string? { get; set; }` | |
| `Prompt` | `string? { get; set; }` | |
| `UIHint` | `string? { get; set; }` | |
| `DataTypeHint` | `DataType? { get; set; }` | |
| `IsReadOnly` | `bool { get; set; }` | |
| `Minimum` / `Maximum` | `double? { get; set; }` | Settable directly on an ad-hoc field; auto-filled from a `RangeAttribute` passed to the constructor, or by `Form.FromModel`. |
| `Meta` | `FieldMeta { get; }` | |
| `Errors` | `IReadOnlyList<string> { get; }` | |
| `ErrorMessage` | `string? { get; }` | |
| `Triggers` | `ValidateTrigger { get; set; }` | Default `ValidateTrigger.Default`. |
| `Value` | `T { get; set; }` | The setter is equivalent to `HandleChange(value)`. |
| `HandleChange` | `void HandleChange(T value, bool shouldValidate = true)` | No-op (including no re-validation) if `value` equals the current value. Re-validation, when it runs, uses `ValidatedOnly` mode. |
| `HandleBlur` | `void HandleBlur()` | Always sets `Touched = true`; validates (`Force` mode) if `Triggers` includes `OnBlur`. |
| `SetError` | `void SetError(string? message)` | |
| `AppendError` | `void AppendError(string message)` | |
| `ValidateAsync` | `Task ValidateAsync()` | `Force` mode. |
| `Reset` | `void Reset()` | Resets to the original initial value. |
| `Reset` | `void Reset(T value)` | Resets to `value`, which also becomes the new initial value. |
| `DependsOn` | `IDisposable DependsOn<TSource>(Field<TSource> source)` | Re-validates (`ValidatedOnly`) this field whenever `source`'s value changes. Built on `Watch.On`, not `Watch.Effect` - see the README's [Cross-field validation](README.md#cross-field-validation) section for why that distinction matters. Call once per source to depend on several fields at once (e.g. a dynamic min/max range), not just one. |
| `AddRule` | `Field<T> AddRule(ValidationAttribute rule)` | Returns `this` for chaining. No effect on a `FromModel`-built field (its validation is driven entirely by the live model property's own reflected attributes) - declare the attribute on the model property instead. |
| `AddRule` | `Field<T> AddRule(ValidationRule<T> rule)` | Works for every field. |
| `AddRule` | `Field<T> AddRule(AsyncValidationRule<T> rule)` | Works for every field. |
| `HasErrors` | `bool { get; }` | `INotifyDataErrorInfo`. |
| `ErrorsChanged` | `event EventHandler<DataErrorsChangedEventArgs>?` | `INotifyDataErrorInfo`. |
| `GetErrors` | `IEnumerable GetErrors(string? propertyName)` | `INotifyDataErrorInfo`; ignores `propertyName`, always returns `Errors`. |
| `Dispose` | `void Dispose()` | Cancels in-flight async validation and stops watching every `DependsOn` dependency. |

### `IDependsOnProperties` (interface)

| Member | Signature | Meaning |
| --- | --- | --- |
| `DependsOnPropertyNames` | `IEnumerable<string> { get; }` | Names of sibling properties on the same model whose changes should re-validate the property this attribute is declared on. |

Implemented by a `ValidationAttribute` that needs live re-validation whenever one or more sibling
model properties change - the declarative counterpart to a hand-wired `Field<T>.DependsOn<TSource>`
call. `Form.FromModel<TModel>` scans every property's attributes for this interface, after every
field already exists, and calls `DependsOn` automatically for each name returned here - see
[`PropertyRangeAttribute`](#namespace-barbatoswpfaquariusvaleerules), the one built-in attribute
that implements it, and the README's [Cross-field validation](README.md#cross-field-validation)
section for the full picture.

Every rule that fails contributes its own message to `Errors` - rules are not "bailed" after the
first failure, with one native BCL exception: `Validator.TryValidateValue`/`TryValidateProperty`
both special-case `RequiredAttribute` to run first and short-circuit every other attribute the
instant it fails (confirmed empirically), so `[Required, MinLength(3)]` on an empty value reports
only `RequiredAttribute`'s own message.

`Errors`/`ErrorMessage`'s `PropertyChanged` and `ErrorsChanged` fire only when the error list
actually **changes**. Validation re-runs on every keystroke and usually produces the identical
result; notifying regardless would make WPF re-evaluate the native validation adorner for every
binding on the field, and `Form` re-raise it at form level, once per character typed.

### `Form` (sealed class) : `ObservableObject`, `INotifyDataErrorInfo`, `IDisposable`

| Member | Signature | Meaning |
| --- | --- | --- |
| Constructor | `Form()` | An empty form. |
| `Meta` | `FormMeta { get; }` | |
| `IsSubmitting` | `bool { get; }` | |
| `SubmitCount` | `int { get; }` | |
| `Fields` | `IReadOnlyDictionary<string, IField> { get; }` | |
| `DefineField` | `Field<T> DefineField<T>(string name, T initialValue)` | |
| `DefineField` | `Field<T> DefineField<T>(string name, T initialValue, params ValidationAttribute[] rules)` | |
| `DefineField` | `Field<T> DefineField<T>(string name, T initialValue, params ValidationRule<T>[] rules)` | |
| `DefineField` | `Field<T> DefineField<T>(string name, T initialValue, params AsyncValidationRule<T>[] rules)` | |
| `FromModel` | `static Form FromModel<TModel>(TModel instance) where TModel : notnull` | Reflects `TModel`'s public settable properties into fields, then auto-wires `Field<T>.DependsOn` for every `IDependsOnProperties` attribute found (e.g. `PropertyRangeAttribute`); see README's [Schema mode](README.md#schema-mode-formfrommodel). |
| `GetModel` | `TModel GetModel<TModel>() where TModel : notnull` | Throws `InvalidOperationException` if this form was not built via `FromModel<TModel>()`. |
| `HandleSubmit` | `IAsyncRelayCommand HandleSubmit(Func<IReadOnlyDictionary<string, object?>, Task> onValid, Action<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, IReadOnlyList<string>>>? onInvalid = null)` | See README's [`HandleSubmit`](README.md#handlesubmit) for the exact stage order. `CanExecute` is gated only on `!IsSubmitting`. |
| `ValidateAllAsync` | `Task<bool> ValidateAllAsync()` | Runs a `Force` pass on every field (plus `IValidatableObject`, for a `FromModel`-built form); returns whether the whole form is valid. |
| `RevalidateValidatedAsync` | `Task RevalidateValidatedAsync()` | Re-runs validation on only the fields already `Meta.Validated`, leaving pristine fields untouched. For a `FromModel`-built form it re-runs `IValidatableObject` too (restricted to those same fields), since a field's own pass replaces its errors and would otherwise drop the model-level message. Intended for refreshing already-displayed messages after a UI language change - see the README's [localization section](README.md#refreshing-messages-after-a-language-change). |
| `SetFieldValue` | `void SetFieldValue<T>(string name, T value, bool shouldValidate = true)` | Throws `ArgumentException` if no field named `name` of type `T` exists. |
| `SetFieldError` | `void SetFieldError(string name, string? message)` | |
| `ResetForm` | `void ResetForm()` | Resets every field; clears `SubmitCount`. |
| `GetValues` | `IReadOnlyDictionary<string, object?> GetValues()` | Every field's current value, boxed, by name. |
| `GetErrors` | `IReadOnlyDictionary<string, IReadOnlyList<string>> GetErrors()` | Every field that currently has at least one error, by name. |
| `GetFirstInvalidField` | `IField? GetFirstInvalidField()` | The first invalid field, in `DefineField`/`FromModel`-reflection definition order, or `null` if the whole form is valid. |
| `SubmitInvalid` | `event Action<IField>?` | Raised by `HandleSubmit` (never a bare `ValidateAllAsync()` call) with `GetFirstInvalidField()`'s result whenever a submit turns out invalid. Headless - no WPF dependency; `Xaml.Valee.FormProperty` subscribes to this automatically to move keyboard focus. |
| `HasErrors` | `bool { get; }` | `INotifyDataErrorInfo`. |
| `ErrorsChanged` | `event EventHandler<DataErrorsChangedEventArgs>?` | `INotifyDataErrorInfo`. |
| `GetErrors` | `IEnumerable GetErrors(string? propertyName)` | `INotifyDataErrorInfo` overload - all fields' errors if `propertyName` is null/empty, else that one field's. |
| `Dispose` | `void Dispose()` | Disposes every field. |

### `FormMeta` (sealed class, `ObservableObject`)

| Member | Type | Meaning |
| --- | --- | --- |
| `Touched` | `bool { get; }` | Any field touched. |
| `Dirty` | `bool { get; }` | Any field dirty. |
| `Pending` | `bool { get; }` | Any field pending. |
| `Valid` | `bool { get; }` | Every field valid and no stray form-level error. |

All four are plain on-demand getters, computed fresh on every read - never cached (see README for
why this matters for `HandleSubmit`'s correctness). `PropertyChanged` notification for all four is
batched via `NextTick` (mirrors `Lifecycle`'s own `IOnUpdated` batching) - this only affects how
often the UI is told to re-read them, never the values themselves.

### `ValidateTrigger` (`[Flags]` enum)

| Member | Value | Meaning |
| --- | --- | --- |
| `None` | `0` | Never re-validate automatically. |
| `OnChange` | `1` | Re-validate whenever `Field<T>.Value` changes. |
| `OnBlur` | `2` | Re-validate when the bound control loses focus. |
| `Default` | `OnChange \| OnBlur` | The default. |

### `ValidationRule<T>` (delegate)

`string? ValidationRule<in T>(T value)` - `null` = valid, a message = invalid.

### `AsyncValidationRule<T>` (delegate)

`Task<string?> AsyncValidationRule<in T>(T value, CancellationToken cancellationToken)` - always
forward the token to whatever you `await` inside it (see README's
[Async rules](README.md#async-rules-and-metapending)).

---

## Namespace: `Barbatos.Wpf.AquariusValee.Rules`

Every attribute here **except**
[`PropertyRangeAttribute`](#propertyrangeattribute-sealed-class--validationattribute-idependsonproperties)
shares one shape: `[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]`,
a `null`/empty value treated as valid, and the simple `IsValid(object?)` override.
`PropertyRangeAttribute` is a different kind of attribute entirely - property-only, and
`ValidationContext`-aware.

| Attribute | Constructor | `AsciiOnly`? | Notes |
| --- | --- | --- | --- |
| `AlphaAttribute` | `()` | Yes | Letters only. |
| `AlphaNumericAttribute` | `()` | Yes | Letters + digits. |
| `AlphaDashAttribute` | `()` | Yes | Letters + digits + `_`/`-`. |
| `AlphaSpacesAttribute` | `()` | Yes | Letters + whitespace. |
| `OneOfAttribute` | `(params object?[] allowedValues)` | - | `AllowedValues` property exposes the constructor argument. **Loose**, cross-type matching (`1`/`"1"` match) - prefer the BCL's own `AllowedValuesAttribute` (.NET 8+, strict-equality) unless you specifically want this looser behavior. |
| `NotOneOfAttribute` | `(params object?[] disallowedValues)` | - | `DisallowedValues` property. Prefer the BCL's own `DeniedValuesAttribute` (.NET 8+) for the strict case. |
| `DigitsAttribute` | `(int length)` | - | `Length` property. ASCII digits only, exact count. |
| `NumericStringAttribute` | `()` | - | ASCII digits only, no sign, one or more characters. |
| `IntegerStringAttribute` | `()` | - | An optional leading `-` followed by ASCII digits only. |
| `PatternAttribute` | `(string pattern)` | - | `AllowMultiple = true`. Init-only `Options` (`RegexOptions`, the gap the BCL's `RegularExpressionAttribute` cannot fill - it has no such property), `MustMatchEntireValue` (default `true`, anchors end to end), `MatchTimeoutInMilliseconds` (default `2000`; a timeout fails the field rather than throwing). Throws `ArgumentException`/`ArgumentNullException` on a null/empty pattern. |

No `ExactLengthAttribute` is shipped - `new LengthAttribute(n, n)` (.NET 8+, in the BCL itself)
already covers exact-length for both strings and collections, confirmed empirically.

### `PropertyRangeAttribute` (sealed class) : `ValidationAttribute`, `IDependsOnProperties`

`[AttributeUsage(AttributeTargets.Property)]`.

| Member | Signature | Meaning |
| --- | --- | --- |
| Constructor | `PropertyRangeAttribute(string minPropertyName, string maxPropertyName)` | Names two sibling properties (typically via `nameof`) holding the current lower/upper bound. |
| `DependsOnPropertyNames` | `IEnumerable<string> { get; }` | `IDependsOnProperties` - yields both constructor arguments. |
| `ErrorMessage` | `string { get; set; }` (inherited) | Defaults to `"The {0} field must be between {1} and {2}."` - `{0}` = display name, `{1}`/`{2}` = the min/max bound's *current* value. Not an i18n-key lookup - see the README's [schema-mode cross-field section](README.md#the-schema-mode-equivalent-propertyrangeattribute-and-idependsonproperties) for why. |

Validates that a value stays between two **sibling properties'** own current values, reflected via
`ValidationContext.ObjectInstance` - the same trick `CompareAttribute` uses - so it only works on a
property validated via `Form.FromModel<TModel>`; an ad-hoc `Field<T>` has no such backing instance.
Throws `InvalidOperationException` (from `Form.FromModel`, at call time - not lazily on first
validation) if either named property does not exist on the model.

See the README's [full DataAnnotations review](README.md#a-full-review-of-systemcomponentmodeldataannotations---whats-native-whats-custom-whats-doesnt-apply)
for every other type in `System.ComponentModel.DataAnnotations` - which map directly onto
existing BCL attributes, which are hint/metadata-only (`UIHintAttribute`/`DataTypeAttribute`/
`EditableAttribute`/`DisplayFormatAttribute`), and which don't apply to form validation at all
(the EF/scaffolding-only attributes).

---

## Namespace: `Barbatos.Wpf.AquariusValee.Xaml`

### `Valee` (static partial class)

| Member | Type | Meaning |
| --- | --- | --- |
| `FieldProperty` / `Get/SetField` | `DependencyProperty` / `object?` | Binds a control's natural value property to an `IField`. Throws for `PasswordBox`. |
| `PasswordFieldProperty` / `Get/SetPasswordField` | `DependencyProperty` / `Field<string>?` | `PasswordBox`-only; never creates a real `Binding` on `Password`. |
| `FormProperty` / `Get/SetForm` | `DependencyProperty` / `Form?` | Set once on an ancestor container. Also subscribes to the bound `Form`'s `SubmitInvalid` event and focuses + `BringIntoView()`s whichever control is bound to the first invalid field, and suppresses WPF's own native validation adorner on this element itself (an empty `Validation.ErrorTemplate` - `Form` implements `INotifyDataErrorInfo`, so without this the whole panel would get a red-border adorner whenever any field is invalid, not just the offending control; `Validation.HasError`/`GetErrors` are left intact, only the visual is suppressed). |
| `ForProperty` / `Get/SetFor` | `DependencyProperty` / `string?` | A field name, resolved against the nearest ancestor `FormProperty` at `Loaded`. |
| `NumericProperty` / `Get/SetNumeric` | `DependencyProperty` / `NumericInput` | `TextBox`-only (throws otherwise). Filters typing and paste to a valid numeric prefix - half-finished states (`-`, `.`, `-.`) pass, anything that can never be numeric does not. Clamps to the resolved bounds on `LostFocus`, never while typing. Default `NumericInput.None` leaves the box unrestricted. |
| `MinimumProperty` / `Get/SetMinimum` | `DependencyProperty` / `double?` | Lower bound for a `Numeric` box, overriding the bound field's `IField.Minimum`. Optional and independent of `Maximum`. A value of zero or above also removes the negative sign from the accepted characters. |
| `MaximumProperty` / `Get/SetMaximum` | `DependencyProperty` / `double?` | Upper bound, overriding `IField.Maximum`. Optional and independent of `Minimum`. |
| `MaskProperty` / `Get/SetMask` | `DependencyProperty` / `string?` | `TextBox`-only. Formats as the user types from a positional mask (`0` digit, `A` letter, `*` alphanumeric, `>`/`<` casing, `\` escape, anything else a literal). Auto-inserts literals, swallows characters that fit no slot before they render, deletes through a separator on backspace, filters paste, and caps length at the mask. The formatted text is what reaches the bound field. Guides typing only - it does not validate; pair it with `Rules.PatternAttribute`. Setting `null`/`""` detaches it. |

See the README's [XAML integration](README.md#xaml-integration) section for full examples,
the `UpdateSourceTrigger`/native-adorner behavior, the "`Form` must be non-null by load time"
constraint, and [`Valee.Mask`](README.md#valeemask---guiding-what-gets-typed)'s mask-character
table and its "a mask guides, an attribute decides" split.

### `NumericInput` (enum)

| Member | Meaning |
| --- | --- |
| `None` | The default - not a numeric box, typing unrestricted. |
| `Integer` | Digits, plus a leading negative sign where the bounds allow one. |
| `Decimal` | As `Integer`, plus a single decimal separator. |

### `FieldTemplateSelector` (sealed class) : `DataTemplateSelector`

| Member | Signature | Meaning |
| --- | --- | --- |
| `SelectTemplate` | `DataTemplate? SelectTemplate(object? item, DependencyObject container)` | Looks up a `DataTemplate` resource keyed on `item`'s (an `IField`) `UIHint`, then `DataTypeHint`'s enum name, then `"Default"`, via `container.TryFindResource`. Falls back to `base.SelectTemplate` if `item`/`container` aren't the expected types or no matching resource exists (and no `"Default"` one either). |

See the README's [`FieldTemplateSelector`](README.md#fieldtemplateselector---auto-selecting-a-control-per-field)
section for a full worked example.

### `SafeMessageConverter` (sealed class) : `IValueConverter`

`[ContentProperty(nameof(Inner))]`.

| Member | Signature | Meaning |
| --- | --- | --- |
| `Inner` | `IValueConverter? { get; set; }` | The localizing converter to try first; the XAML content property, so it can simply be nested inside the element. When `null`, this converter is a pass-through. |
| `Convert` | `object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)` | Returns `Inner`'s result only if it produced a non-empty string; otherwise returns `value` unchanged (or `string.Empty` when `value` is `null`). An `Inner` that throws is treated the same as one that produced nothing. |
| `ConvertBack` | `object ConvertBack(...)` | Always throws `NotSupportedException` - these bindings are one-way. |

Guards against the silent failure mode of localized validation messages: a localizing converter
typically returns `null` for an unrecognised key, and `null` bound to `TextBlock.Text` renders as
nothing at all, so one typo or one deliberately-literal message becomes invisible. Nothing here is
tied to a particular localization package - `Inner` is an ordinary `IValueConverter`. See the
README's [`SafeMessageConverter`](README.md#safemessageconverter---localization-without-becoming-dependent-on-a-localizer)
section.
