using NistParser.Constants;

namespace NistParser.Editing;

/// <summary>
/// Provides metadata for NIST fields, including Type-1 and Type-2 fields
/// </summary>
public static class FieldMetadataProvider
{
    private static readonly Dictionary<string, FieldMetadata> _metadata = new();

    static FieldMetadataProvider()
    {
        InitializeType1Metadata();
        InitializeType2Metadata();
    }

    /// <summary>
    /// Gets the metadata for a specific field
    /// </summary>
    public static FieldMetadata? GetFieldMetadata(string fieldNumber)
    {
        return _metadata.TryGetValue(fieldNumber, out var metadata) ? metadata : null;
    }

    /// <summary>
    /// Gets all editable fields for a specific record type
    /// </summary>
    public static List<FieldMetadata> GetEditableFields(RecordType recordType)
    {
        int recordTypeNumber = (int)recordType;
        return _metadata.Values
            .Where(m => m.FieldNumber.StartsWith($"{recordTypeNumber}.") && m.IsEditable)
            .OrderBy(m => m.FieldNumber)
            .ToList();
    }

    /// <summary>
    /// Gets all available fields that can be added to a record (not currently present)
    /// </summary>
    public static List<FieldMetadata> GetAvailableFieldsToAdd(RecordType recordType, IEnumerable<string> existingFieldNumbers)
    {
        int recordTypeNumber = (int)recordType;
        var existingSet = new HashSet<string>(existingFieldNumbers);

        return _metadata.Values
            .Where(m => m.FieldNumber.StartsWith($"{recordTypeNumber}.")
                     && m.IsEditable
                     && !m.IsRequired
                     && !existingSet.Contains(m.FieldNumber))
            .OrderBy(m => m.FieldNumber)
            .ToList();
    }

    /// <summary>
    /// Checks if a field can be deleted (not required)
    /// </summary>
    public static bool CanDeleteField(string fieldNumber)
    {
        var metadata = GetFieldMetadata(fieldNumber);
        return metadata != null && !metadata.IsRequired;
    }

    private static void InitializeType1Metadata()
    {
        // Type-1: Transaction Information Record

        // 1.001 - LEN (Calculated, not editable)
        _metadata["1.001"] = FieldMetadata.Calculated("1.001", "Logical Record Length");

        // 1.002 - VER (Required, 4-digit version)
        _metadata["1.002"] = FieldMetadata.Text("1.002", "Version Number", isRequired: true, pattern: @"^\d{4}$", maxLength: 4,
            helpText: "Version number in format XXYY (e.g., 0502 for version 5.02)",
            validationMessage: "Must be exactly 4 digits",
            defaultValue: "0502");

        // 1.003 - CNT (Calculated, not editable)
        _metadata["1.003"] = FieldMetadata.Calculated("1.003", "Transaction Content");

        // 1.004 - TOT (Required, text with validation)
        // Note: ANSI/NIST allows various TOT codes (CRM, CIV, AMN, CAR, SIT, etc.)
        // Using text field instead of enum to support all valid codes
        _metadata["1.004"] = FieldMetadata.Text("1.004", "Type of Transaction", isRequired: true,
            pattern: @"^[A-Z0-9]{1,4}$", maxLength: 4,
            helpText: "Type of transaction code (e.g., CRM=Criminal, CIV=Civil, AMN=Administrative, CAR=Applicant, SIT=Subject In Trouble)",
            validationMessage: "Must be 1-4 uppercase letters or digits",
            defaultValue: "CRM");

        // 1.005 - DAT (Required, date)
        _metadata["1.005"] = FieldMetadata.Date("1.005", "Date", isRequired: true,
            helpText: "Transaction date in YYYYMMDD format");

        // 1.006 - PRY (Optional, enumeration)
        _metadata["1.006"] = FieldMetadata.Enum("1.006", "Priority",
            new Dictionary<string, string>
            {
                { "1", "Highest priority (1)" },
                { "2", "High (2)" },
                { "3", "Medium-high (3)" },
                { "4", "Medium (4)" },
                { "5", "Medium-low (5)" },
                { "6", "Low (6)" },
                { "7", "Lower (7)" },
                { "8", "Lowest (8)" },
                { "9", "Background (9)" }
            }, isRequired: false,
            helpText: "Processing priority (1=highest, 9=lowest)",
            defaultValue: "4");

        // 1.007 - DAI (Required)
        _metadata["1.007"] = FieldMetadata.Text("1.007", "Destination Agency Identifier", isRequired: true, maxLength: 50,
            helpText: "Identifier of the receiving agency");

        // 1.008 - ORI (Required)
        _metadata["1.008"] = FieldMetadata.Text("1.008", "Originating Agency Identifier", isRequired: true, maxLength: 50,
            helpText: "Identifier of the sending agency");

        // 1.009 - TCN (Required)
        _metadata["1.009"] = FieldMetadata.Text("1.009", "Transaction Control Number", isRequired: true, maxLength: 50,
            helpText: "Unique identifier for this transaction");

        // 1.010 - TCR (Optional)
        _metadata["1.010"] = FieldMetadata.Text("1.010", "Transaction Control Reference", isRequired: false, maxLength: 50,
            helpText: "Reference to a related transaction");

        // 1.011 - NSR (Optional, numeric)
        _metadata["1.011"] = FieldMetadata.Numeric("1.011", "Native Scanning Resolution", isRequired: false,
            helpText: "Resolution at which biometric data was originally captured (in ppi)");

        // 1.012 - NTR (Optional, numeric)
        _metadata["1.012"] = FieldMetadata.Numeric("1.012", "Nominal Transmitting Resolution", isRequired: false,
            helpText: "Resolution of the transmitted biometric data (in ppi)");

        // 1.013 - DOM (Optional)
        _metadata["1.013"] = FieldMetadata.Text("1.013", "Domain Name", isRequired: false, maxLength: 100,
            helpText: "Domain name for this transaction");

        // 1.014 - GMT (Optional)
        _metadata["1.014"] = FieldMetadata.Text("1.014", "Greenwich Mean Time", isRequired: false,
            pattern: @"^\d{14}Z?$", maxLength: 15,
            helpText: "GMT timestamp in YYYYMMDDHHMMSSz format",
            validationMessage: "Must be in format YYYYMMDDHHMMSSz");

        // 1.015 - DCS (Optional)
        _metadata["1.015"] = FieldMetadata.Text("1.015", "Directory of Character Sets", isRequired: false,
            helpText: "Character set specification");

        // 1.016 - APS (Optional)
        _metadata["1.016"] = FieldMetadata.Text("1.016", "Application Profile Specifications", isRequired: false,
            helpText: "Application profile specifications");

        // 1.017 - OAN (Optional)
        _metadata["1.017"] = FieldMetadata.Text("1.017", "Originating Agency Name", isRequired: false, maxLength: 100,
            helpText: "Full name of the originating agency");
    }

    private static void InitializeType2Metadata()
    {
        // Type-2: User-Defined Descriptive Text Record

        // 2.001 - LEN (Calculated, not editable)
        _metadata["2.001"] = FieldMetadata.Calculated("2.001", "Logical Record Length");

        // 2.002 - IDC (Required for Type-2, but typically set programmatically)
        _metadata["2.002"] = FieldMetadata.Text("2.002", "Information Designation Character", isRequired: true,
            pattern: @"^\d{1,2}$", maxLength: 2,
            helpText: "Links related records (00-99)",
            validationMessage: "Must be a 1 or 2 digit number (00-99)");

        // Common Type-2 fields (user-defined, but commonly used)

        // 2.003 - System Information (optional, varies by implementation)
        _metadata["2.003"] = FieldMetadata.Text("2.003", "System Information", isRequired: false, maxLength: 200,
            helpText: "System or implementation-specific information");

        // 2.004 - Surname
        _metadata["2.004"] = FieldMetadata.Text("2.004", "Surname", isRequired: false, maxLength: 100,
            helpText: "Last name or family name");

        // 2.005 - Given Name
        _metadata["2.005"] = FieldMetadata.Text("2.005", "Given Name", isRequired: false, maxLength: 100,
            helpText: "First name");

        // 2.006 - Middle Name
        _metadata["2.006"] = FieldMetadata.Text("2.006", "Middle Name", isRequired: false, maxLength: 100,
            helpText: "Middle name or initial");

        // 2.007 - Date of Birth
        _metadata["2.007"] = FieldMetadata.Date("2.007", "Date of Birth", isRequired: false,
            helpText: "Date of birth in YYYYMMDD format");

        // 2.008 - Aliases (optional)
        _metadata["2.008"] = FieldMetadata.Text("2.008", "Aliases", isRequired: false, maxLength: 200,
            helpText: "Known aliases or alternate names");

        // 2.018 - Place of Birth
        _metadata["2.018"] = FieldMetadata.Text("2.018", "Place of Birth", isRequired: false, maxLength: 100,
            helpText: "City, state, or country of birth");

        // 2.020 - Social Security Number (optional)
        _metadata["2.020"] = FieldMetadata.Text("2.020", "Social Security Number", isRequired: false,
            pattern: @"^\d{9}$|^\d{3}-\d{2}-\d{4}$", maxLength: 11,
            helpText: "SSN in format XXXXXXXXX or XXX-XX-XXXX",
            validationMessage: "Must be 9 digits or in format XXX-XX-XXXX");

        // 2.022 - Height (optional)
        _metadata["2.022"] = FieldMetadata.Numeric("2.022", "Height", isRequired: false,
            helpText: "Height in centimeters");

        // 2.024 - Sex
        _metadata["2.024"] = FieldMetadata.Enum("2.024", "Sex",
            new Dictionary<string, string>
            {
                { "M", "Male" },
                { "F", "Female" },
                { "U", "Unknown" },
                { "X", "Unspecified" }
            }, isRequired: false,
            helpText: "Biological sex");

        // 2.025 - Race
        _metadata["2.025"] = FieldMetadata.Enum("2.025", "Race",
            new Dictionary<string, string>
            {
                { "W", "White" },
                { "B", "Black" },
                { "A", "Asian or Pacific Islander" },
                { "I", "American Indian or Alaskan Native" },
                { "U", "Unknown" }
            }, isRequired: false,
            helpText: "Race or ethnicity");

        // 2.026 - Eye Color (optional)
        _metadata["2.026"] = FieldMetadata.Enum("2.026", "Eye Color",
            new Dictionary<string, string>
            {
                { "BLK", "Black" },
                { "BLU", "Blue" },
                { "BRO", "Brown" },
                { "GRY", "Gray" },
                { "GRN", "Green" },
                { "HAZ", "Hazel" },
                { "MAR", "Maroon" },
                { "PNK", "Pink" },
                { "XXX", "Unknown" }
            }, isRequired: false,
            helpText: "Eye color");

        // 2.027 - Hair Color (optional)
        _metadata["2.027"] = FieldMetadata.Enum("2.027", "Hair Color",
            new Dictionary<string, string>
            {
                { "BAL", "Bald" },
                { "BLK", "Black" },
                { "BLN", "Blonde" },
                { "BRO", "Brown" },
                { "GRY", "Gray" },
                { "RED", "Red" },
                { "SDY", "Sandy" },
                { "WHI", "White" },
                { "XXX", "Unknown" }
            }, isRequired: false,
            helpText: "Hair color");

        // 2.030 - Citizenship (optional)
        _metadata["2.030"] = FieldMetadata.Text("2.030", "Citizenship", isRequired: false, maxLength: 50,
            helpText: "Country of citizenship");

        // 2.040 - Occupation (optional)
        _metadata["2.040"] = FieldMetadata.Text("2.040", "Occupation", isRequired: false, maxLength: 100,
            helpText: "Occupation or profession");
    }

    /// <summary>
    /// Checks if a field number is valid for a given record type
    /// </summary>
    public static bool IsValidFieldNumber(string fieldNumber)
    {
        return _metadata.ContainsKey(fieldNumber);
    }

    /// <summary>
    /// Gets all metadata entries
    /// </summary>
    public static IEnumerable<FieldMetadata> GetAllMetadata()
    {
        return _metadata.Values;
    }
}
