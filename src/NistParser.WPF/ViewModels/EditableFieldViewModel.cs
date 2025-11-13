using System.ComponentModel;
using System.Runtime.CompilerServices;
using NistParser.Editing;

namespace NistParser.WPF.ViewModels;

/// <summary>
/// View model for an editable field in the record editor
/// </summary>
public class EditableFieldViewModel : INotifyPropertyChanged
{
    private string? _fieldValue;
    private bool _isValid = true;
    private string? _validationMessage;

    /// <summary>
    /// Gets the field metadata
    /// </summary>
    public FieldMetadata Metadata { get; }

    /// <summary>
    /// Gets the field number (e.g., "1.002")
    /// </summary>
    public string FieldNumber => Metadata.FieldNumber;

    /// <summary>
    /// Gets the display name for the field
    /// </summary>
    public string DisplayName => Metadata.DisplayName;

    /// <summary>
    /// Gets the help text for the field
    /// </summary>
    public string? HelpText => Metadata.HelpText;

    /// <summary>
    /// Gets whether this field is required
    /// </summary>
    public bool IsRequired => Metadata.IsRequired;

    /// <summary>
    /// Gets whether this field is editable
    /// </summary>
    public bool IsEditable => Metadata.IsEditable;

    /// <summary>
    /// Gets the field data type
    /// </summary>
    public FieldDataType FieldType => Metadata.DataType;

    /// <summary>
    /// Gets the enumeration values for dropdown fields
    /// </summary>
    public Dictionary<string, string>? EnumValues => Metadata.EnumValues;

    /// <summary>
    /// Gets or sets the field value
    /// </summary>
    public string? FieldValue
    {
        get => _fieldValue;
        set
        {
            if (_fieldValue != value)
            {
                _fieldValue = value;
                OnPropertyChanged();
                Validate();
            }
        }
    }

    /// <summary>
    /// Gets whether the field value is valid
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        private set
        {
            if (_isValid != value)
            {
                _isValid = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets the validation message (if validation failed)
    /// </summary>
    public string? ValidationMessage
    {
        get => _validationMessage;
        private set
        {
            if (_validationMessage != value)
            {
                _validationMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets whether this field can be deleted
    /// </summary>
    public bool CanDelete => !IsRequired;

    /// <summary>
    /// Initializes a new instance of EditableFieldViewModel
    /// </summary>
    public EditableFieldViewModel(FieldMetadata metadata, string? initialValue = null)
    {
        Metadata = metadata;
        _fieldValue = initialValue;
        Validate();
    }

    /// <summary>
    /// Validates the current field value
    /// </summary>
    public void Validate()
    {
        var result = FieldValidator.ValidateField(Metadata, FieldValue);
        IsValid = result.IsValid;
        ValidationMessage = result.ErrorMessage;
    }

    /// <summary>
    /// Resets the field to its original value
    /// </summary>
    public void Reset(string? originalValue)
    {
        FieldValue = originalValue;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
