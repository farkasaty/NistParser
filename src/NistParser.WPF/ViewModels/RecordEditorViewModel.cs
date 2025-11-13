using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NistParser.Core;
using NistParser.Editing;

namespace NistParser.WPF.ViewModels;

/// <summary>
/// View model for the record editor window
/// </summary>
public class RecordEditorViewModel : INotifyPropertyChanged
{
    private readonly NistRecord _record;
    private readonly NistTransactionEditor _editor;
    private readonly Dictionary<string, string> _originalValues = new();
    private FieldMetadata? _selectedFieldToAdd;
    private string? _newFieldValue;

    /// <summary>
    /// Gets the record type
    /// </summary>
    public RecordType RecordType => _record.RecordType;

    /// <summary>
    /// Gets the record type display name
    /// </summary>
    public string RecordTypeDisplayName => $"Type-{(int)RecordType} Record";

    /// <summary>
    /// Gets the collection of editable fields
    /// </summary>
    public ObservableCollection<EditableFieldViewModel> EditableFields { get; } = new();

    /// <summary>
    /// Gets the list of fields that can be added (not currently present)
    /// </summary>
    public ObservableCollection<FieldMetadata> AvailableFieldsToAdd { get; } = new();

    /// <summary>
    /// Gets or sets the selected field to add
    /// </summary>
    public FieldMetadata? SelectedFieldToAdd
    {
        get => _selectedFieldToAdd;
        set
        {
            if (_selectedFieldToAdd != value)
            {
                _selectedFieldToAdd = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddField));
            }
        }
    }

    /// <summary>
    /// Gets or sets the value for the new field
    /// </summary>
    public string? NewFieldValue
    {
        get => _newFieldValue;
        set
        {
            if (_newFieldValue != value)
            {
                _newFieldValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanAddField));
            }
        }
    }

    /// <summary>
    /// Gets whether a new field can be added
    /// </summary>
    public bool CanAddField => SelectedFieldToAdd != null && !string.IsNullOrWhiteSpace(NewFieldValue);

    /// <summary>
    /// Gets whether the record has been modified
    /// </summary>
    public bool IsDirty
    {
        get
        {
            foreach (var field in EditableFields)
            {
                if (_originalValues.TryGetValue(field.FieldNumber, out var originalValue))
                {
                    if (field.FieldValue != originalValue)
                        return true;
                }
                else if (!string.IsNullOrEmpty(field.FieldValue))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Gets whether all fields are valid
    /// </summary>
    public bool IsValid => EditableFields.All(f => f.IsValid);

    /// <summary>
    /// Initializes a new instance of RecordEditorViewModel
    /// </summary>
    public RecordEditorViewModel(NistRecord record, NistTransactionEditor editor)
    {
        _record = record;
        _editor = editor;

        LoadEditableFields();
        LoadAvailableFieldsToAdd();
    }

    /// <summary>
    /// Loads the editable fields from the record
    /// </summary>
    private void LoadEditableFields()
    {
        EditableFields.Clear();
        _originalValues.Clear();

        var metadata = FieldMetadataProvider.GetEditableFields(RecordType);

        foreach (var fieldMeta in metadata)
        {
            var field = _record.GetField(fieldMeta.FieldNumber);
            var value = field?.FirstValue;

            _originalValues[fieldMeta.FieldNumber] = value ?? string.Empty;

            var viewModel = new EditableFieldViewModel(fieldMeta, value);
            viewModel.PropertyChanged += Field_PropertyChanged;
            EditableFields.Add(viewModel);
        }
    }

    /// <summary>
    /// Loads the list of fields that can be added
    /// </summary>
    private void LoadAvailableFieldsToAdd()
    {
        AvailableFieldsToAdd.Clear();

        var existingFieldNumbers = _record.Fields.Keys;
        var availableFields = FieldMetadataProvider.GetAvailableFieldsToAdd(RecordType, existingFieldNumbers);

        foreach (var field in availableFields)
        {
            AvailableFieldsToAdd.Add(field);
        }
    }

    /// <summary>
    /// Adds a new field to the record
    /// </summary>
    public ValidationResult AddNewField()
    {
        if (SelectedFieldToAdd == null || string.IsNullOrWhiteSpace(NewFieldValue))
        {
            return ValidationResult.Fail("Please select a field and enter a value");
        }

        var result = _editor.AddField(_record, SelectedFieldToAdd.FieldNumber, NewFieldValue);
        if (result.IsValid)
        {
            // Add to editable fields
            var viewModel = new EditableFieldViewModel(SelectedFieldToAdd, NewFieldValue);
            viewModel.PropertyChanged += Field_PropertyChanged;
            EditableFields.Add(viewModel);

            // Remove from available fields
            AvailableFieldsToAdd.Remove(SelectedFieldToAdd);

            // Clear selection
            SelectedFieldToAdd = null;
            NewFieldValue = null;

            OnPropertyChanged(nameof(IsDirty));
        }

        return result;
    }

    /// <summary>
    /// Removes a field from the record
    /// </summary>
    public ValidationResult RemoveField(EditableFieldViewModel fieldViewModel)
    {
        var result = _editor.RemoveField(_record, fieldViewModel.FieldNumber);
        if (result.IsValid)
        {
            fieldViewModel.PropertyChanged -= Field_PropertyChanged;
            EditableFields.Remove(fieldViewModel);

            // Add back to available fields if it's optional
            if (fieldViewModel.CanDelete)
            {
                AvailableFieldsToAdd.Add(fieldViewModel.Metadata);
            }

            OnPropertyChanged(nameof(IsDirty));
        }

        return result;
    }

    /// <summary>
    /// Saves all changes to the record
    /// </summary>
    public ValidationResults SaveChanges()
    {
        var results = new ValidationResults();

        // Update all modified fields
        foreach (var fieldViewModel in EditableFields)
        {
            if (_originalValues.TryGetValue(fieldViewModel.FieldNumber, out var originalValue))
            {
                if (fieldViewModel.FieldValue != originalValue)
                {
                    var result = _editor.SetFieldValue(_record, fieldViewModel.FieldNumber, fieldViewModel.FieldValue ?? string.Empty);
                    results.Add(result);
                }
            }
        }

        // Update original values if successful
        if (results.IsValid)
        {
            foreach (var field in EditableFields)
            {
                _originalValues[field.FieldNumber] = field.FieldValue ?? string.Empty;
            }
            OnPropertyChanged(nameof(IsDirty));
        }

        return results;
    }

    /// <summary>
    /// Resets all changes
    /// </summary>
    public void ResetChanges()
    {
        foreach (var field in EditableFields)
        {
            if (_originalValues.TryGetValue(field.FieldNumber, out var originalValue))
            {
                field.Reset(originalValue);
            }
        }

        OnPropertyChanged(nameof(IsDirty));
    }

    private void Field_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EditableFieldViewModel.FieldValue))
        {
            OnPropertyChanged(nameof(IsDirty));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
