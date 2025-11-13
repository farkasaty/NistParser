namespace NistParser.Editing;

/// <summary>
/// Defines the data type of a field for editing purposes
/// </summary>
public enum FieldDataType
{
    /// <summary>Text field (free-form)</summary>
    Text,

    /// <summary>Numeric field (integer or decimal)</summary>
    Numeric,

    /// <summary>Date field (YYYYMMDD format)</summary>
    Date,

    /// <summary>Enumeration field (predefined values)</summary>
    Enumeration,

    /// <summary>Binary data field (not editable)</summary>
    Binary,

    /// <summary>Calculated field (auto-generated, read-only)</summary>
    Calculated
}

/// <summary>
/// Metadata for a NIST field, providing information needed for editing and validation
/// </summary>
public class FieldMetadata
{
    /// <summary>
    /// Gets or sets the field number (e.g., "1.002", "2.004")
    /// </summary>
    public string FieldNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field data type
    /// </summary>
    public FieldDataType DataType { get; set; } = FieldDataType.Text;

    /// <summary>
    /// Gets or sets whether this field is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets whether this field is editable
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// Gets or sets the regular expression pattern for validation (optional)
    /// </summary>
    public string? ValidationPattern { get; set; }

    /// <summary>
    /// Gets or sets the validation error message
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// Gets or sets the maximum length for the field value
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the minimum length for the field value
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Gets or sets the enumeration values (for dropdown fields)
    /// Key = value, Value = display text
    /// </summary>
    public Dictionary<string, string>? EnumValues { get; set; }

    /// <summary>
    /// Gets or sets a help text for the field
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// Gets or sets the default value for new fields
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Initializes a new instance of the FieldMetadata class
    /// </summary>
    public FieldMetadata()
    {
    }

    /// <summary>
    /// Initializes a new instance of the FieldMetadata class with basic properties
    /// </summary>
    public FieldMetadata(string fieldNumber, string displayName, FieldDataType dataType, bool isRequired = false)
    {
        FieldNumber = fieldNumber;
        DisplayName = displayName;
        DataType = dataType;
        IsRequired = isRequired;
    }

    /// <summary>
    /// Creates a text field metadata
    /// </summary>
    public static FieldMetadata Text(string fieldNumber, string displayName, bool isRequired = false,
        string? pattern = null, int? maxLength = null)
    {
        return new FieldMetadata(fieldNumber, displayName, FieldDataType.Text, isRequired)
        {
            ValidationPattern = pattern,
            MaxLength = maxLength
        };
    }

    /// <summary>
    /// Creates a numeric field metadata
    /// </summary>
    public static FieldMetadata Numeric(string fieldNumber, string displayName, bool isRequired = false,
        string? pattern = null)
    {
        return new FieldMetadata(fieldNumber, displayName, FieldDataType.Numeric, isRequired)
        {
            ValidationPattern = pattern ?? @"^\d+(\.\d+)?$",
            ValidationMessage = "Must be a valid number"
        };
    }

    /// <summary>
    /// Creates a date field metadata
    /// </summary>
    public static FieldMetadata Date(string fieldNumber, string displayName, bool isRequired = false)
    {
        return new FieldMetadata(fieldNumber, displayName, FieldDataType.Date, isRequired)
        {
            ValidationPattern = @"^\d{8}$",
            ValidationMessage = "Must be a valid date in YYYYMMDD format",
            MinLength = 8,
            MaxLength = 8
        };
    }

    /// <summary>
    /// Creates an enumeration field metadata
    /// </summary>
    public static FieldMetadata Enum(string fieldNumber, string displayName,
        Dictionary<string, string> enumValues, bool isRequired = false)
    {
        return new FieldMetadata(fieldNumber, displayName, FieldDataType.Enumeration, isRequired)
        {
            EnumValues = enumValues
        };
    }

    /// <summary>
    /// Creates a calculated (read-only) field metadata
    /// </summary>
    public static FieldMetadata Calculated(string fieldNumber, string displayName)
    {
        return new FieldMetadata(fieldNumber, displayName, FieldDataType.Calculated, false)
        {
            IsEditable = false
        };
    }
}
