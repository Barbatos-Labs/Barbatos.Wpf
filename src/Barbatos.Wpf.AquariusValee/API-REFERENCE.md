# Barbatos.Wpf.AquariusValee API Reference

A member-by-member reference for every public type in `Barbatos.Wpf.AquariusValee`. See the
[README](README.md) for a concept-first walkthrough with worked examples - this document is for
looking up a specific member's exact signature/behavior.

## Namespaces

| Namespace | Contents |
| --- | --- |
| [`Barbatos.Wpf.AquariusValee.Validation`](#namespace-barbatoswpfaquariusvaleevalidation) | `Field<T>`, `IField`, `FieldMeta`, `Form`, `FormMeta`, the rule delegates, `ValidateTrigger`. |
| [`Barbatos.Wpf.AquariusValee.Rules`](#namespace-barbatoswpfaquariusvaleerules) | Eight custom `ValidationAttribute` subclasses for gaps in `System.ComponentModel.DataAnnotations`. |
| [`Barbatos.Wpf.AquariusValee.Xaml`](#namespace-barbatoswpfaquariusvaleexaml) | `Valee` (the attached-property family connecting `Field`/`Form` to real WPF controls) and `FieldTemplateSelector`. |

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
| `Pending` | `bool` | `true` while an async rule is in flight. |
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
| `DependsOn` | `IDisposable DependsOn<TSource>(Field<TSource> source)` | Re-validates (`ValidatedOnly`) this field whenever `source`'s value changes. Built on `Watch.On`, not `Watch.Effect` - see the README's [Cross-field validation](README.md#cross-field-validation) section for why that distinction matters. |
| `AddRule` | `Field<T> AddRule(ValidationAttribute rule)` | Returns `this` for chaining. No effect on a `FromModel`-built field (its validation is driven entirely by the live model property's own reflected attributes) - declare the attribute on the model property instead. |
| `AddRule` | `Field<T> AddRule(ValidationRule<T> rule)` | Works for every field. |
| `AddRule` | `Field<T> AddRule(AsyncValidationRule<T> rule)` | Works for every field. |
| `HasErrors` | `bool { get; }` | `INotifyDataErrorInfo`. |
| `ErrorsChanged` | `event EventHandler<DataErrorsChangedEventArgs>?` | `INotifyDataErrorInfo`. |
| `GetErrors` | `IEnumerable GetErrors(string? propertyName)` | `INotifyDataErrorInfo`; ignores `propertyName`, always returns `Errors`. |
| `Dispose` | `void Dispose()` | Cancels in-flight async validation and stops watching every `DependsOn` dependency. |

Every rule that fails contributes its own message to `Errors` - rules are not "bailed" after the
first failure, with one native BCL exception: `Validator.TryValidateValue`/`TryValidateProperty`
both special-case `RequiredAttribute` to run first and short-circuit every other attribute the
instant it fails (confirmed empirically), so `[Required, MinLength(3)]` on an empty value reports
only `RequiredAttribute`'s own message.

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
| `FromModel` | `static Form FromModel<TModel>(TModel instance) where TModel : notnull` | Reflects `TModel`'s public settable properties into fields; see README's [Schema mode](README.md#schema-mode-formfrommodel). |
| `GetModel` | `TModel GetModel<TModel>() where TModel : notnull` | Throws `InvalidOperationException` if this form was not built via `FromModel<TModel>()`. |
| `HandleSubmit` | `IAsyncRelayCommand HandleSubmit(Func<IReadOnlyDictionary<string, object?>, Task> onValid, Action<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, IReadOnlyList<string>>>? onInvalid = null)` | See README's [`HandleSubmit`](README.md#handlesubmit) for the exact stage order. `CanExecute` is gated only on `!IsSubmitting`. |
| `ValidateAllAsync` | `Task<bool> ValidateAllAsync()` | Runs a `Force` pass on every field (plus `IValidatableObject`, for a `FromModel`-built form); returns whether the whole form is valid. |
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

Eight `ValidationAttribute` subclasses, each `[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]`, each treating a `null`/empty value as valid.

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

No `ExactLengthAttribute` is shipped - `new LengthAttribute(n, n)` (.NET 8+, in the BCL itself)
already covers exact-length for both strings and collections, confirmed empirically.

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
| `FormProperty` / `Get/SetForm` | `DependencyProperty` / `Form?` | Set once on an ancestor container. Also subscribes to the bound `Form`'s `SubmitInvalid` event and focuses + `BringIntoView()`s whichever control is bound to the first invalid field. |
| `ForProperty` / `Get/SetFor` | `DependencyProperty` / `string?` | A field name, resolved against the nearest ancestor `FormProperty` at `Loaded`. |

See the README's [XAML integration](README.md#xaml-integration) section for full examples,
the `UpdateSourceTrigger`/native-adorner behavior, and the "`Form` must be non-null by load time"
constraint.

### `FieldTemplateSelector` (sealed class) : `DataTemplateSelector`

| Member | Signature | Meaning |
| --- | --- | --- |
| `SelectTemplate` | `DataTemplate? SelectTemplate(object? item, DependencyObject container)` | Looks up a `DataTemplate` resource keyed on `item`'s (an `IField`) `UIHint`, then `DataTypeHint`'s enum name, then `"Default"`, via `container.TryFindResource`. Falls back to `base.SelectTemplate` if `item`/`container` aren't the expected types or no matching resource exists (and no `"Default"` one either). |

See the README's [`FieldTemplateSelector`](README.md#fieldtemplateselector---auto-selecting-a-control-per-field)
section for a full worked example.
