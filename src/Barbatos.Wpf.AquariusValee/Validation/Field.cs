// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Barbatos.Wpf.Aquarius.Reactivity;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// One field's reactive value and validation state - the WPF/C# counterpart of vee-validate's
/// <c>useField</c>.
/// </summary>
/// <remarks>
/// <para>
/// Usable standalone (<c>new Field&lt;string&gt;(...)</c>, e.g. a single validated search box
/// with no surrounding form concept) or via <see cref="Form"/>'s <c>DefineField</c>,
/// which additionally tracks it for form-level aggregation (<see cref="Form.Meta"/>) and submit
/// gating.
/// </para>
/// <para>
/// <b>Rules run in three possible shapes</b>, all of which can be combined on the same field via
/// <see cref="AddRule(ValidationAttribute)"/>/<see cref="AddRule(ValidationRule{T})"/>/
/// <see cref="AddRule(AsyncValidationRule{T})"/>: <see cref="ValidationAttribute"/>s (executed via
/// <see cref="System.ComponentModel.DataAnnotations.Validator"/>, the primary "schema" mechanism -
/// or, for a field created via <see cref="Form.FromModel{TModel}"/>, the model property's own
/// declared attributes, evaluated via <see cref="Validator.TryValidateProperty"/> against the
/// live model instance so attributes like <see cref="CompareAttribute"/> resolve correctly),
/// <see cref="ValidationRule{T}"/> delegates, and <see cref="AsyncValidationRule{T}"/> delegates.
/// Every rule that fails contributes its own message to <see cref="Errors"/> - unlike
/// vee-validate's default <c>bails: true</c> (stop at the first failure), delegate rules are
/// always all evaluated and every message collected. The attribute path mostly matches this too
/// (confirmed empirically against the real BCL, not assumed), with one native BCL exception worth
/// knowing: <see cref="Validator.TryValidateValue"/>/<see cref="Validator.TryValidateProperty"/>
/// both special-case <see cref="RequiredAttribute"/> to run first - regardless of where it sits
/// in the list - and short-circuit every other attribute the instant it fails, so a
/// <c>[Required, MinLength(3)]</c> pair on an empty value reports only <see cref="RequiredAttribute"/>'s
/// own message, not both. This is the BCL's own long-standing, deliberate behavior (checking a
/// format/length rule on a value that's simply missing is rarely useful) - inherited as-is here
/// rather than worked around, since it already matches the sensible outcome, not fought.
/// </para>
/// <para>
/// Implements <see cref="INotifyDataErrorInfo"/> directly, so a plain
/// <c>&lt;TextBox Text="{Binding SomeField.Value}"/&gt;</c> already gets WPF's native red-border
/// validation adorner with zero extra XAML - see <see cref="Xaml.Valee"/> for the attached
/// properties that additionally wire touched-tracking and live (<c>PropertyChanged</c>-trigger)
/// binding, which a plain <see cref="System.Windows.Data.Binding"/> does not give you for free
/// (WPF's own default <c>UpdateSourceTrigger</c> for <see cref="System.Windows.Controls.TextBox.Text"/>
/// is <see cref="System.Windows.Data.UpdateSourceTrigger.LostFocus"/>, not <c>PropertyChanged</c>).
/// </para>
/// </remarks>
public sealed class Field<T> : ObservableObject, IField, IDisposable
{
    private readonly List<ValidationAttribute> _attributeRules = [];
    private readonly List<ValidationRule<T>> _syncRules = [];
    private readonly List<AsyncValidationRule<T>> _asyncRules = [];
    private readonly List<string> _errors = [];
    private readonly List<IDisposable> _dependencyHandles = [];
    private Ref<T> _value = null!;
    private T _initialValue = default!;

    // Deliberately Cancel()'d, never Dispose()'d - see ValidateInternal's own comment for why
    // disposing it here would race an older, still-in-flight call reading cts.Token elsewhere.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213", Justification =
        "Cancel() alone is safe to call concurrently with an in-flight reader's cts.Token access; " +
        "Dispose() is not, and this token holds no Register()'d callbacks/timers, so never " +
        "disposing it (left for the GC) is a deliberate, harmless trade favoring correctness.")]
    private CancellationTokenSource? _validationCts;

    // Set only by Form.FromModel's internal constructor below - null for a standalone/ad-hoc field.
    internal Func<T, IReadOnlyList<string>>? ModelAttributeValidator { get; private init; }
    internal Action<T>? WriteBackToModel { get; private init; }

    /// <summary>Creates a field with no validation rules.</summary>
    public Field(string name, T initialValue)
    {
        Initialize(name, initialValue);
        _ = ValidateInternal(ValidationWriteMode.Silent);
    }

    /// <summary>Creates a field validated by one or more <see cref="ValidationAttribute"/>s (the same attributes usable on a DataAnnotations model property).</summary>
    public Field(string name, T initialValue, params ValidationAttribute[] rules)
    {
        Initialize(name, initialValue);
        _attributeRules.AddRange(rules);
        _ = ValidateInternal(ValidationWriteMode.Silent);
    }

    /// <summary>Creates a field validated by one or more synchronous, schema-less rule delegates.</summary>
    public Field(string name, T initialValue, params ValidationRule<T>[] rules)
    {
        Initialize(name, initialValue);
        _syncRules.AddRange(rules);
        _ = ValidateInternal(ValidationWriteMode.Silent);
    }

    /// <summary>Creates a field validated by one or more asynchronous, schema-less rule delegates.</summary>
    public Field(string name, T initialValue, params AsyncValidationRule<T>[] rules)
    {
        Initialize(name, initialValue);
        _asyncRules.AddRange(rules);
        _ = ValidateInternal(ValidationWriteMode.Silent);
    }

    /// <summary>Used only by <see cref="Form.FromModel{TModel}"/> to back a field with a real model property's own declared attributes.</summary>
    internal Field(
        string name,
        T initialValue,
        string? label,
        string? prompt,
        string? uiHint,
        DataType? dataTypeHint,
        bool isReadOnly,
        Func<T, IReadOnlyList<string>> modelAttributeValidator,
        Action<T> writeBackToModel)
    {
        Initialize(name, initialValue);
        Label = label;
        Prompt = prompt;
        UIHint = uiHint;
        DataTypeHint = dataTypeHint;
        IsReadOnly = isReadOnly;
        ModelAttributeValidator = modelAttributeValidator;
        WriteBackToModel = writeBackToModel;
        _ = ValidateInternal(ValidationWriteMode.Silent);
    }

    private void Initialize(string name, T initialValue)
    {
        Name = name;
        _initialValue = initialValue;
        _value = new Ref<T>(initialValue);
        Meta = new FieldMeta(() => !EqualityComparer<T>.Default.Equals(_value.Value, _initialValue));
    }

    /// <inheritdoc/>
    public string Name { get; private set; } = "";

    /// <summary>A friendly display name for error messages - mirrors <c>[Display(Name = "...")]</c> when this field was built via <see cref="Form.FromModel{TModel}"/>. Settable directly on an ad-hoc field too.</summary>
    public string? Label { get; set; }

    /// <inheritdoc cref="IField.Prompt"/>
    public string? Prompt { get; set; }

    /// <inheritdoc/>
    public string? UIHint { get; set; }

    /// <inheritdoc/>
    public DataType? DataTypeHint { get; set; }

    /// <inheritdoc/>
    public bool IsReadOnly { get; set; }

    /// <inheritdoc/>
    public FieldMeta Meta { get; private set; } = null!;

    /// <inheritdoc/>
    public IReadOnlyList<string> Errors => _errors;

    /// <inheritdoc/>
    public string? ErrorMessage => _errors.Count > 0 ? _errors[0] : null;

    object? IField.RawValue => Value;

    /// <summary>Which events automatically re-validate this field. Defaults to <see cref="ValidateTrigger.Default"/>.</summary>
    public ValidateTrigger Triggers { get; set; } = ValidateTrigger.Default;

    /// <summary>The field's current value. Setting it is equivalent to calling <see cref="HandleChange"/> with <c>shouldValidate: true</c>.</summary>
    public T Value
    {
        get => _value.Value;
        set => HandleChange(value);
    }

    /// <summary>
    /// Updates the value, mirroring vee-validate's <c>handleChange</c>. A no-op (including no
    /// re-validation) if <paramref name="value"/> equals the current value. Re-validation, when
    /// it does run, uses <see cref="ValidationWriteMode.ValidatedOnly"/> - it only becomes visible
    /// if this field has already been <see cref="FieldMeta.Validated"/> once (by a prior blur,
    /// submit, or explicit <see cref="ValidateAsync"/> call), so typing into a pristine field
    /// doesn't surface an error before the user has had a chance to finish.
    /// </summary>
    public void HandleChange(T value, bool shouldValidate = true)
    {
        if (EqualityComparer<T>.Default.Equals(_value.Value, value))
            return;

        _value.Value = value;
        WriteBackToModel?.Invoke(value);
        OnPropertyChanged(nameof(Value));
        Meta.NotifyDirtyChanged();

        if (shouldValidate && Triggers.HasFlag(ValidateTrigger.OnChange))
            _ = ValidateInternal(ValidationWriteMode.ValidatedOnly);
    }

    /// <summary>Marks the field <see cref="FieldMeta.Touched"/> and, if <see cref="Triggers"/> includes <see cref="ValidateTrigger.OnBlur"/>, runs a <see cref="ValidationWriteMode.Force"/> validation pass.</summary>
    public void HandleBlur()
    {
        Meta.Touched = true;

        if (Triggers.HasFlag(ValidateTrigger.OnBlur))
            _ = ValidateInternal(ValidationWriteMode.Force);
    }

    /// <inheritdoc/>
    public void SetError(string? message)
    {
        Meta.Validated = true;
        Meta.Valid = message is null;
        ReplaceErrors(message is null ? [] : [message]);
    }

    /// <inheritdoc/>
    public void AppendError(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Meta.Validated = true;
        Meta.Valid = false;
        ReplaceErrors([.. _errors, message]);
    }

    /// <inheritdoc/>
    public Task ValidateAsync() => ValidateInternal(ValidationWriteMode.Force);

    /// <summary>Resets the field to its original initial value, clears touched/validated/errors, and re-runs a silent pass.</summary>
    public void Reset() => ResetCore(_initialValue, rebaseInitial: false);

    /// <summary>Resets the field to <paramref name="value"/>, which also becomes the new initial value for future <see cref="FieldMeta.Dirty"/> comparisons - mirroring vee-validate's <c>resetField({ value })</c>.</summary>
    public void Reset(T value) => ResetCore(value, rebaseInitial: true);

    void IField.Reset() => Reset();

    /// <summary>
    /// Re-validates this field whenever <paramref name="source"/>'s value changes, mirroring
    /// vee-validate's <c>@fieldName</c> cross-field dependency locator. Uses
    /// <see cref="ValidationWriteMode.ValidatedOnly"/>, so a sibling field changing doesn't
    /// suddenly surface an error on an untouched dependent field. Dispose the returned handle
    /// (or dispose this field) to stop watching.
    /// </summary>
    /// <remarks>
    /// Built on <see cref="Watch.On{TSource}(Ref{TSource}, Action{TSource, TSource}, bool, bool, bool, WatchFlush)"/>,
    /// not <c>Watch.Effect</c> - <c>Watch.On</c> only reacts to <paramref name="source"/>'s own
    /// value actually changing, whereas <c>Watch.Effect</c> reacts to <i>any</i> property change
    /// on its dependency (including <see cref="Errors"/>/<see cref="Meta"/> changes triggered by
    /// validation itself) - two fields that depend on each other with <c>Watch.Effect</c> would
    /// infinite-loop (validating A changes A's errors, which fires B's watcher, which validates B
    /// and changes B's errors, which fires A's watcher, forever).
    /// </remarks>
    public IDisposable DependsOn<TSource>(Field<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var handle = Watch.On(source._value, (_, _) => _ = ValidateInternal(ValidationWriteMode.ValidatedOnly));
        _dependencyHandles.Add(handle);
        return handle;
    }

    /// <summary>Adds an additional <see cref="ValidationAttribute"/> rule. For a field created via <see cref="Form.FromModel{TModel}"/>, prefer declaring the attribute on the model property instead - an attribute added here has no effect on such a field, since its validation is driven entirely by the live model property's own reflected attributes.</summary>
    public Field<T> AddRule(ValidationAttribute rule)
    {
        _attributeRules.Add(rule);
        return this;
    }

    /// <summary>Adds an additional synchronous rule delegate - works for every field, including one created via <see cref="Form.FromModel{TModel}"/>.</summary>
    public Field<T> AddRule(ValidationRule<T> rule)
    {
        _syncRules.Add(rule);
        return this;
    }

    /// <summary>Adds an additional asynchronous rule delegate - works for every field, including one created via <see cref="Form.FromModel{TModel}"/>.</summary>
    public Field<T> AddRule(AsyncValidationRule<T> rule)
    {
        _asyncRules.Add(rule);
        return this;
    }

    /// <inheritdoc/>
    public bool HasErrors => _errors.Count > 0;

    /// <inheritdoc/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc/>
    public IEnumerable GetErrors(string? propertyName) => _errors;

    /// <summary>Cancels any in-flight async validation and stops watching every <see cref="DependsOn{TSource}"/> dependency.</summary>
    public void Dispose()
    {
        _validationCts?.Cancel();
        _validationCts = null;

        foreach (var handle in _dependencyHandles)
            handle.Dispose();

        _dependencyHandles.Clear();
    }

    private void ResetCore(T value, bool rebaseInitial)
    {
        // See ValidateInternal's own comment on Cancel() vs Dispose() - the same race applies here.
        _validationCts?.Cancel();
        _validationCts = null;

        if (rebaseInitial)
            _initialValue = value;

        _value.Value = value;
        OnPropertyChanged(nameof(Value));
        Meta.NotifyDirtyChanged();
        Meta.Touched = false;
        Meta.Validated = false;
        Meta.Pending = false;
        ReplaceErrors([]);

        _ = ValidateInternal(ValidationWriteMode.Silent);
    }

    private async Task ValidateInternal(ValidationWriteMode mode)
    {
        // Deliberately Cancel(), never Dispose(), the CancellationTokenSource being replaced here:
        // an older, still-in-flight call further down this same method may still be reading
        // cts.Token (which throws ObjectDisposedException once its source is disposed) - only
        // Dispose() is genuinely unsafe to call out from under a concurrent reader, Cancel() is not.
        _validationCts?.Cancel();
        var cts = new CancellationTokenSource();
        _validationCts = cts;

        if (mode != ValidationWriteMode.Silent)
            Meta.Pending = true;

        List<string> messages;

        try
        {
            var currentValue = _value.Value;
            messages = [.. RunAttributeRules(currentValue)];

            foreach (var rule in _syncRules)
            {
                if (cts.Token.IsCancellationRequested)
                    return;

                if (rule(currentValue) is { } message)
                    messages.Add(message);
            }

            foreach (var rule in _asyncRules)
            {
                if (cts.Token.IsCancellationRequested)
                    return;

                var message = await rule(currentValue, cts.Token);

                if (cts.Token.IsCancellationRequested || !ReferenceEquals(cts, _validationCts))
                    return;

                if (message is not null)
                    messages.Add(message);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cts.Token.IsCancellationRequested || !ReferenceEquals(cts, _validationCts))
            return;

        if (mode != ValidationWriteMode.Silent)
            Meta.Pending = false;

        // Updated on every pass regardless of mode - this is what lets Meta.Valid stay accurate
        // even while a Silent/ValidatedOnly pass withholds the visible Errors themselves.
        Meta.Valid = messages.Count == 0;

        if (mode == ValidationWriteMode.Silent)
            return;

        if (mode == ValidationWriteMode.ValidatedOnly && !Meta.Validated)
            return;

        Meta.Validated = true;
        ReplaceErrors(messages);
    }

    private IReadOnlyList<string> RunAttributeRules(T value)
    {
        if (ModelAttributeValidator is not null)
            return ModelAttributeValidator(value);

        if (_attributeRules.Count == 0)
            return [];

        var context = new ValidationContext(this) { MemberName = Name, DisplayName = Label ?? Name };
        var results = new List<ValidationResult>();
        Validator.TryValidateValue(value!, context, results, _attributeRules);
        return [.. results.Select(r => r.ErrorMessage ?? "The value is invalid.")];
    }

    private void ReplaceErrors(IReadOnlyList<string> newErrors)
    {
        _errors.Clear();
        _errors.AddRange(newErrors);
        OnPropertyChanged(nameof(Errors));
        OnPropertyChanged(nameof(ErrorMessage));
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Value)));
    }
}
