// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Wpf Contributors.
// All Rights Reserved.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Barbatos.Wpf.AquariusValee.Validation;

/// <summary>
/// Aggregates a set of <see cref="Field{T}"/>s into one form - the WPF/C# counterpart of
/// vee-validate's <c>useForm</c>.
/// </summary>
/// <remarks>
/// <para>
/// Two ways to build one: hand-author it field by field via <c>DefineField</c>
/// (each call also returns the strongly-typed <see cref="Field{T}"/>, so a ViewModel keeps its
/// own typed reference for use in code/XAML - there is no need to subclass <see cref="Form"/>
/// or look fields up by name in ordinary use):
/// <code>
/// public Form LoginForm { get; } = new();
/// public Field&lt;string&gt; Email { get; }
///
/// public LoginViewModel()
/// {
///     Email = LoginForm.DefineField(nameof(Email), "", new RequiredAttribute(), new EmailAddressAttribute());
/// }
/// </code>
/// - or reflect an existing <see cref="ValidationAttribute"/>-decorated model class via
/// <see cref="FromModel{TModel}"/>, the "schema" mode equivalent of vee-validate's yup/zod
/// support.
/// </para>
/// <para>
/// <see cref="HandleSubmit"/>'s returned command's <c>CanExecute</c> is gated <b>only</b> on
/// <c>!IsSubmitting</c>, never on <see cref="FormMeta.Valid"/> - clicking Submit on an invalid,
/// untouched form is exactly what forces every field's <see cref="FieldMeta.Touched"/> to
/// <see langword="true"/> and surfaces its errors, matching vee-validate's own real behavior.
/// </para>
/// </remarks>
public sealed class Form : ObservableObject, INotifyDataErrorInfo, IDisposable
{
    private static readonly MethodInfo CreateModelFieldMethod =
        typeof(Form).GetMethod(nameof(CreateModelField), BindingFlags.NonPublic | BindingFlags.Static)!;

    private readonly Dictionary<string, IField> _fields = new();
    // Dictionary<TKey,TValue> enumeration order happens to match insertion order in the current
    // BCL implementation, but that is explicitly documented as an implementation detail, not a
    // contract - GetFirstInvalidField()/auto-focus needs a real guarantee, so definition order is
    // tracked here explicitly instead of relying on _fields' own enumeration order.
    private readonly List<IField> _fieldOrder = [];
    private object? _model;
    private bool _isSubmitting;
    private int _submitCount;

    /// <summary>Creates an empty form - add fields via <c>DefineField</c>.</summary>
    public Form() =>
        Meta = new FormMeta(AnyFieldTouched, AnyFieldDirty, AnyFieldPending, EveryFieldValid);

    // Plain loops over the ordered list, not LINQ over Dictionary.Values: all four are recomputed
    // on every read (never cached - see FormMeta's own remarks on why), and every read through
    // Enumerable.Any/All boxes the enumerator. These run on each of the four batched notifications
    // and again for each binding that re-reads them, so keeping them allocation-free matters more
    // here than the terser call would be worth.
    private bool AnyFieldTouched()
    {
        foreach (var field in _fieldOrder)
        {
            if (field.Meta.Touched)
                return true;
        }

        return false;
    }

    private bool AnyFieldDirty()
    {
        foreach (var field in _fieldOrder)
        {
            if (field.Meta.Dirty)
                return true;
        }

        return false;
    }

    private bool AnyFieldPending()
    {
        foreach (var field in _fieldOrder)
        {
            if (field.Meta.Pending)
                return true;
        }

        return false;
    }

    private bool EveryFieldValid()
    {
        foreach (var field in _fieldOrder)
        {
            if (!field.Meta.Valid)
                return false;
        }

        return true;
    }

    /// <summary>This form's aggregated reactive state.</summary>
    public FormMeta Meta { get; }

    /// <summary>Whether a <see cref="HandleSubmit"/>-returned command is currently executing.</summary>
    public bool IsSubmitting
    {
        get => _isSubmitting;
        private set => SetProperty(ref _isSubmitting, value);
    }

    /// <summary>How many times a <see cref="HandleSubmit"/>-returned command has been invoked, successful or not.</summary>
    public int SubmitCount
    {
        get => _submitCount;
        private set => SetProperty(ref _submitCount, value);
    }

    /// <summary>Every field currently registered on this form, keyed by name.</summary>
    public IReadOnlyDictionary<string, IField> Fields => _fields;

    /// <summary>Defines a field with no validation rules and tracks it for form-level aggregation.</summary>
    public Field<T> DefineField<T>(string name, T initialValue) =>
        Register(name, new Field<T>(name, initialValue));

    /// <summary>Defines a field validated by one or more <see cref="ValidationAttribute"/>s and tracks it for form-level aggregation.</summary>
    public Field<T> DefineField<T>(string name, T initialValue, params ValidationAttribute[] rules) =>
        Register(name, new Field<T>(name, initialValue, rules));

    /// <summary>Defines a field validated by one or more synchronous rule delegates and tracks it for form-level aggregation.</summary>
    public Field<T> DefineField<T>(string name, T initialValue, params ValidationRule<T>[] rules) =>
        Register(name, new Field<T>(name, initialValue, rules));

    /// <summary>Defines a field validated by one or more asynchronous rule delegates and tracks it for form-level aggregation.</summary>
    public Field<T> DefineField<T>(string name, T initialValue, params AsyncValidationRule<T>[] rules) =>
        Register(name, new Field<T>(name, initialValue, rules));

    /// <summary>
    /// Builds a <see cref="Form"/> by reflecting over <typeparamref name="TModel"/>'s public
    /// settable properties - one <see cref="Field{T}"/> per property, validated via
    /// <see cref="Validator.TryValidateProperty"/> against the real, live <paramref name="instance"/>
    /// (so attributes like <see cref="CompareAttribute"/> that reflect into sibling properties
    /// resolve correctly - see <see cref="Field{T}.AddRule(ValidationAttribute)"/>'s remarks for
    /// why this differs from an ad-hoc <c>DefineField</c>
    /// field). Each field's value stays live-synced back onto <paramref name="instance"/> as it
    /// changes - <see cref="GetModel{TModel}"/> returns that same instance, always up to date.
    /// <see cref="IValidatableObject"/> on <typeparamref name="TModel"/> (DataAnnotations' own
    /// native cross-field mechanism) is honored too, checked once per <see cref="HandleSubmit"/>/
    /// explicit whole-form validate rather than per keystroke.
    /// </summary>
    /// <remarks>
    /// A property with no settable get/set pair, or an indexer, is skipped. <c>[Display(Name = "...")]</c>
    /// becomes each field's <see cref="Field{T}.Label"/>; <c>[Display(Prompt = "...")]</c> becomes
    /// <see cref="IField.Prompt"/>; <c>[UIHint("...")]</c> becomes <see cref="IField.UIHint"/>;
    /// <c>[DataType(...)]</c> becomes <see cref="IField.DataTypeHint"/>; <c>[Editable(false)]</c>
    /// becomes <see cref="IField.IsReadOnly"/>. See <see cref="Xaml.FieldTemplateSelector"/> for
    /// using <c>UIHint</c>/<c>DataTypeHint</c> to auto-select the right control per field.
    /// Any attribute implementing <see cref="IDependsOnProperties"/> (e.g.
    /// <see cref="Rules.PropertyRangeAttribute"/>) is auto-wired via
    /// <see cref="Field{T}.DependsOn{TSource}"/> once every field exists, so declaration order does
    /// not matter and application code never has to wire this by hand.
    /// </remarks>
    public static Form FromModel<TModel>(TModel instance) where TModel : notnull
    {
        ArgumentNullException.ThrowIfNull(instance);

        var form = new Form { _model = instance };
        var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .ToArray();

        foreach (var property in properties)
        {
            var generic = CreateModelFieldMethod.MakeGenericMethod(property.PropertyType);
            var field = (IField)generic.Invoke(null, [instance, property])!;
            form.Register(property.Name, field);
        }

        foreach (var property in properties)
            form.WireDeclaredDependencies(typeof(TModel), property);

        return form;
    }

    /// <summary>
    /// Auto-wires <see cref="Field{T}.DependsOn{TSource}"/> for every property attribute
    /// implementing <see cref="IDependsOnProperties"/>, so a dynamic-bound field (e.g. a price
    /// range) re-validates the instant its declared dependency changes, with no hand-written
    /// wiring. Runs as its own pass, after every field already exists, so declaration order within
    /// the model does not matter.
    /// </summary>
    private void WireDeclaredDependencies(Type modelType, PropertyInfo property)
    {
        var dependencyNames = property.GetCustomAttributes(inherit: true)
            .OfType<IDependsOnProperties>()
            .SelectMany(a => a.DependsOnPropertyNames)
            .Distinct();

        foreach (var sourceName in dependencyNames)
        {
            var sourceProperty = modelType.GetProperty(sourceName, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProperty is null)
                throw new InvalidOperationException(
                    $"A validation attribute on '{property.Name}' declares a dependency on '{sourceName}', but '{modelType.Name}' has no such property.");

            var dependentField = _fields[property.Name];
            var sourceField = _fields[sourceName];

            var dependsOnMethod = typeof(Field<>).MakeGenericType(property.PropertyType)
                .GetMethod(nameof(Field<object>.DependsOn))!
                .MakeGenericMethod(sourceProperty.PropertyType);

            dependsOnMethod.Invoke(dependentField, [sourceField]);
        }
    }

    private static Field<TProp> CreateModelField<TProp>(object model, PropertyInfo property)
    {
        var propertyName = property.Name;
        var display = property.GetCustomAttribute<DisplayAttribute>();
        var label = display?.GetName();
        var prompt = display?.GetPrompt();
        var uiHint = property.GetCustomAttribute<UIHintAttribute>()?.UIHint;
        var dataTypeHint = property.GetCustomAttribute<DataTypeAttribute>()?.DataType;
        var isReadOnly = property.GetCustomAttribute<EditableAttribute>() is { AllowEdit: false };
        var initialValue = (TProp)property.GetValue(model)!;

        IReadOnlyList<string> Validate(TProp value)
        {
            var context = new ValidationContext(model) { MemberName = propertyName, DisplayName = label ?? propertyName };
            var results = new List<ValidationResult>();
            Validator.TryValidateProperty(value, context, results);

            if (results.Count == 0)
                return [];

            var messages = new string[results.Count];

            for (var i = 0; i < results.Count; i++)
                messages[i] = results[i].ErrorMessage ?? Field<TProp>.InvalidValueFallbackMessage;

            return messages;
        }

        void WriteBack(TProp value) => property.SetValue(model, value);

        var field = new Field<TProp>(propertyName, initialValue, label, prompt, uiHint, dataTypeHint, isReadOnly, Validate, WriteBack);

        // Set after construction rather than threaded through the constructor: these are metadata
        // for the input layer, never consulted by validation (the property's own [Range] still
        // does that), so the silent first pass above does not need them.
        (field.Minimum, field.Maximum) = RangeMetadata.From(property.GetCustomAttribute<RangeAttribute>());

        return field;
    }

    /// <summary>Returns the model instance this form was built from via <see cref="FromModel{TModel}"/> - always reflects the current field values, since each field writes back to it live.</summary>
    public TModel GetModel<TModel>() where TModel : notnull =>
        _model is TModel model
            ? model
            : throw new InvalidOperationException($"This form was not created via {nameof(Form)}.{nameof(FromModel)}<{typeof(TModel).Name}>().");

    /// <summary>
    /// Returns a command that, when executed: touches every field, sets <see cref="IsSubmitting"/>,
    /// increments <see cref="SubmitCount"/>, validates every field (plus, for a
    /// <see cref="FromModel{TModel}"/>-built form, <see cref="IValidatableObject"/>), then calls
    /// <paramref name="onValid"/> only if the whole form is valid - otherwise
    /// <paramref name="onInvalid"/> with the current values/errors (and raises
    /// <see cref="SubmitInvalid"/> with the first invalid field) - and finally clears
    /// <see cref="IsSubmitting"/>. Mirrors vee-validate's <c>handleSubmit</c> stage order exactly.
    /// </summary>
    public IAsyncRelayCommand HandleSubmit(
        Func<IReadOnlyDictionary<string, object?>, Task> onValid,
        Action<IReadOnlyDictionary<string, object?>, IReadOnlyDictionary<string, IReadOnlyList<string>>>? onInvalid = null)
    {
        ArgumentNullException.ThrowIfNull(onValid);

        AsyncRelayCommand? command = null;
        command = new AsyncRelayCommand(
            async () =>
            {
                foreach (var field in _fields.Values)
                    field.Meta.Touched = true;

                IsSubmitting = true;
                SubmitCount++;
                command!.NotifyCanExecuteChanged();

                try
                {
                    var isValid = await ValidateAllAsync();

                    if (isValid)
                    {
                        await onValid(GetValues());
                    }
                    else
                    {
                        onInvalid?.Invoke(GetValues(), GetErrors());

                        if (GetFirstInvalidField() is { } firstInvalid)
                            SubmitInvalid?.Invoke(firstInvalid);
                    }
                }
                finally
                {
                    IsSubmitting = false;
                    command!.NotifyCanExecuteChanged();
                }
            },
            () => !IsSubmitting);

        return command;
    }

    /// <summary>Runs a <see cref="ValidationWriteMode.Force"/> pass on every field (plus, for a <see cref="FromModel{TModel}"/>-built form, <see cref="IValidatableObject"/>) and returns whether the whole form is valid.</summary>
    public async Task<bool> ValidateAllAsync()
    {
        await Task.WhenAll(_fields.Values.Select(f => f.ValidateAsync()));

        if (_model is not null)
            RunModelLevelValidation();

        return Meta.Valid;
    }

    /// <summary>
    /// Re-runs validation on only the fields that have already been validated at least once,
    /// leaving pristine fields untouched - so no error appears on a field the user has never
    /// interacted with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The intended use is <b>refreshing already-displayed messages after the UI language
    /// changes</b>. A rule that resolves its text eagerly - anything that returns real display
    /// text rather than a lookup key, which is unavoidable when the message interpolates a runtime
    /// value - captured that text in the caller's language at the moment it ran, and nothing about
    /// a later culture switch re-runs it. vee-validate has exactly the same characteristic and
    /// documents the same remedy: change the locale, then re-validate.
    /// </para>
    /// <para>
    /// Unnecessary for a message that is stored as a lookup key and resolved at display time by a
    /// converter - re-reading the binding already picks up the new language for those.
    /// </para>
    /// </remarks>
    public async Task RevalidateValidatedAsync()
    {
        var revalidated = _fieldOrder.Where(field => field.Meta.Validated).ToArray();

        await Task.WhenAll(revalidated.Select(field => field.ValidateAsync()));

        // A field's own pass REPLACES its errors, which would wipe any message
        // IValidatableObject had layered on top - so the model-level pass has to run again too,
        // or a cross-field error silently disappears the first time the language is switched.
        // Restricted to the fields that were already showing errors, so re-running this cannot
        // surface a model-level message on a field the user has never touched.
        if (_model is not null)
            RunModelLevelValidation(revalidated);
    }

    /// <summary>Sets a field's value directly, bypassing whatever control it may be bound to.</summary>
    public void SetFieldValue<T>(string name, T value, bool shouldValidate = true)
    {
        if (_fields.TryGetValue(name, out var field) && field is Field<T> typed)
            typed.HandleChange(value, shouldValidate);
        else
            throw new ArgumentException($"No field named '{name}' of type '{typeof(T)}' exists on this form.", nameof(name));
    }

    /// <summary>Sets a manual, server-driven error on a named field - see <see cref="Field{T}.SetError"/>.</summary>
    public void SetFieldError(string name, string? message)
    {
        if (_fields.TryGetValue(name, out var field))
            field.SetError(message);
        else
            throw new ArgumentException($"No field named '{name}' exists on this form.", nameof(name));
    }

    /// <summary>Resets every field to its initial value and clears <see cref="SubmitCount"/>.</summary>
    public void ResetForm()
    {
        foreach (var field in _fields.Values)
            field.Reset();

        SubmitCount = 0;
    }

    /// <summary>Every field's current value, boxed and keyed by name.</summary>
    public IReadOnlyDictionary<string, object?> GetValues() =>
        _fields.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.RawValue);

    /// <summary>Every field that currently has at least one error, keyed by name.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> GetErrors() =>
        _fields.Where(kvp => kvp.Value.Errors.Count > 0).ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Errors);

    /// <summary>The first invalid field, in <c>DefineField</c>/<c>FromModel</c>-reflection definition order, or <see langword="null"/> if the whole form is currently valid.</summary>
    public IField? GetFirstInvalidField() => _fieldOrder.FirstOrDefault(f => !f.Meta.Valid);

    /// <summary>
    /// Raised by <see cref="HandleSubmit"/> - never by a bare <see cref="ValidateAllAsync"/> call -
    /// with <see cref="GetFirstInvalidField"/>'s result whenever a submit attempt turns out
    /// invalid. This type itself has no WPF-control dependency (see this class's own remarks);
    /// <see cref="Xaml.Valee.FormProperty"/> is what actually subscribes to this and moves
    /// keyboard focus, so the "focus the first invalid field" behavior is automatic wherever
    /// <c>Valee.Form</c> is used and requires no wiring from application code at all.
    /// </summary>
    public event Action<IField>? SubmitInvalid;

    /// <inheritdoc/>
    public bool HasErrors => _fields.Values.Any(f => f.HasErrors);

    /// <inheritdoc/>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    /// <inheritdoc/>
    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _fields.Values.SelectMany(f => f.Errors);

        return _fields.TryGetValue(propertyName, out var field) ? field.Errors : [];
    }

    /// <summary>Disposes every field.</summary>
    public void Dispose()
    {
        foreach (var field in _fields.Values)
            if (field is IDisposable disposable)
                disposable.Dispose();
    }

    private Field<T> Register<T>(string name, Field<T> field)
    {
        Register(name, (IField)field);
        return field;
    }

    private void Register(string name, IField field)
    {
        _fields[name] = field;
        _fieldOrder.Add(field);
        field.Meta.PropertyChanged += (_, _) => Meta.NotifyMayHaveChanged();
        field.ErrorsChanged += (_, _) => ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(name));
        Meta.NotifyMayHaveChanged();
    }

    /// <summary>
    /// Runs <see cref="IValidatableObject"/> (if implemented) against the live model instance -
    /// DataAnnotations' own native cross-field mechanism - and layers any resulting message onto
    /// every named field it references, on top of (not replacing) that field's own rule errors.
    /// <c>validateAllProperties: false</c> deliberately skips per-property attribute re-checking
    /// here, since <see cref="ValidateAllAsync"/> already ran each field's own attributes
    /// individually just above.
    /// </summary>
    /// <param name="only">
    /// When non-<see langword="null"/>, restricts which fields may receive a message - used by
    /// <see cref="RevalidateValidatedAsync"/> so re-running this cannot surface a model-level
    /// error on a field the user has never touched. <see langword="null"/> means every field, the
    /// right behavior for <see cref="ValidateAllAsync"/>, which has just touched them all anyway.
    /// </param>
    private void RunModelLevelValidation(IReadOnlyCollection<IField>? only = null)
    {
        var context = new ValidationContext(_model!);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(_model!, context, results, validateAllProperties: false);

        foreach (var result in results)
        {
            var message = result.ErrorMessage ?? "The form is invalid.";

            foreach (var memberName in result.MemberNames)
            {
                if (_fields.TryGetValue(memberName, out var field) && (only is null || only.Contains(field)))
                    field.AppendError(message);
            }
        }
    }
}
