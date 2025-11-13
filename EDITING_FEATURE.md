# NIST File Editing Feature

## Overview

The NistParser library now includes comprehensive editing capabilities for Type-1 (Transaction Information) and Type-2 (User-Defined Text) records. This feature enables programmatic and GUI-based modification of NIST biometric data interchange files while maintaining file integrity and format compliance.

## Features

### Core Capabilities

- **Edit existing fields**: Modify values in Type-1 and Type-2 records
- **Add new fields**: Insert optional fields that don't currently exist
- **Delete optional fields**: Remove non-required fields from records
- **Validation**: Comprehensive field validation with clear error messages
- **Format support**: Automatic format detection and preservation (Binary/XML)
- **Roundtrip guarantee**: Parse → Edit → Write → Parse maintains data integrity

### Supported Record Types

- **Type-1 (Transaction Information)**: All standard fields including version, date, TCN, agencies, resolution, etc.
- **Type-2 (User-Defined Text)**: Common biographical fields (name, DOB, sex, race, eye color, hair color, etc.)

## Architecture

### Core Components

1. **FieldMetadata** (`NistParser.Editing`)
   - Defines field properties: data type, validation rules, enumerations
   - Provides human-readable display names and help text

2. **FieldValidator** (`NistParser.Editing`)
   - Validates field values against metadata rules
   - Supports: required fields, patterns, enumerations, date formats, numeric values

3. **NistTransactionWriter** (`NistParser.Writing`)
   - Serializes transactions back to byte arrays
   - Supports both Traditional binary and NIEM XML formats
   - Auto-calculates record lengths and transaction content (CNT field)

4. **NistTransactionEditor** (`NistParser.Editing`)
   - High-level API for editing operations
   - Enforces validation before saving
   - Maintains original file format unless overridden

## Usage Examples

### Programmatic Editing (Library API)

```csharp
using NistParser;
using NistParser.Editing;

// Load and parse NIST file
var editor = NistTransactionEditor.FromFile("sample.nist");

// Edit Type-1 field
var result = editor.SetFieldValue(
    editor.Transaction.Header,
    "1.009",  // Transaction Control Number
    "NEW-TCN-12345"
);

if (result.IsValid)
{
    Console.WriteLine("Field updated successfully");
}

// Add optional field
editor.AddField(
    editor.Transaction.Header,
    "1.017",  // Originating Agency Name
    "Federal Bureau of Investigation"
);

// Validate all changes
var validationResults = editor.ValidateAllChanges();
if (validationResults.IsValid)
{
    // Save (overwrites original file)
    editor.SaveToFile("sample.nist");
}
else
{
    Console.WriteLine($"Validation errors:\n{validationResults}");
}
```

### GUI Editing (WPF Application)

1. Open a NIST file using "📂 Open NIST File"
2. Select a Type-1 or Type-2 record from the left panel
3. Click "✏️ Edit Record" button in the record details section
4. In the edit window:
   - **Modify existing fields**: Edit values directly in text boxes or dropdowns
   - **Add new fields**: Select from "Add New Field" dropdown, enter value, click "Add Field"
   - **Delete optional fields**: Click "Delete" button next to any optional field
   - **Validate**: Real-time validation with visual feedback
5. Click "Save Changes" to commit edits
6. File is automatically saved and reloaded

## Field Types and Controls

### Text Fields
- Standard text input
- Optional pattern validation (regex)
- Max/min length constraints

### Numeric Fields
- Numeric-only validation
- Used for resolution, dimensions, etc.

### Date Fields
- YYYYMMDD format enforced
- Date validity checking

### Enumeration Fields
- Dropdown selection
- Predefined values only
- Examples: Transaction Type (CRM, CIV, AMN), Sex (M, F, U), Priority (1-9)

### Calculated Fields (Read-only)
- LEN (Logical Record Length) - Auto-calculated
- CNT (Transaction Content) - Auto-updated based on records

## Validation Rules

### Required Fields (Type-1)
- 1.001 - LEN (auto-calculated)
- 1.002 - VER (Version Number)
- 1.003 - CNT (auto-calculated)
- 1.004 - TOT (Type of Transaction)
- 1.005 - DAT (Date)
- 1.007 - DAI (Destination Agency ID)
- 1.008 - ORI (Originating Agency ID)
- 1.009 - TCN (Transaction Control Number)

### Field-Specific Validations

| Field | Validation Rule |
|-------|----------------|
| 1.002 (VER) | Exactly 4 digits (e.g., "0502") |
| 1.004 (TOT) | Must be: CRM, CIV, AMN, or CAR |
| 1.005 (DAT) | Valid date in YYYYMMDD format |
| 1.006 (PRY) | Number 1-9 |
| 2.024 (Sex) | M, F, U, or X |
| 2.025 (Race) | W, B, A, I, or U |

## Serialization Details

### Traditional Binary Format

- Hierarchical separators: US (0x1F), RS (0x1E), GS (0x1D), FS (0x1C)
- Tagged field format: "X.YYY:value"
- Automatic LEN field calculation for each record
- Automatic CNT field regeneration

### NIEM XML Format

- NIEM namespace compliance
- Element-based field representation
- Base64 encoding for binary data
- Preserves all metadata

## API Reference

### NistTransactionEditor

```csharp
// Factory methods
static NistTransactionEditor FromFile(string filePath)
static NistTransactionEditor FromBytes(byte[] fileData)

// Editing operations
ValidationResult SetFieldValue(NistRecord record, string fieldNumber, string value)
ValidationResult AddField(NistRecord record, string fieldNumber, string value)
ValidationResult RemoveField(NistRecord record, string fieldNumber)

// Validation
ValidationResults ValidateAllChanges()

// Saving
byte[] SaveToBytes(bool? useXmlFormat = null)
void SaveToFile(string filePath, bool? useXmlFormat = null)
```

### FieldMetadataProvider

```csharp
// Query metadata
static FieldMetadata? GetFieldMetadata(string fieldNumber)
static List<FieldMetadata> GetEditableFields(RecordType recordType)
static List<FieldMetadata> GetAvailableFieldsToAdd(RecordType recordType, IEnumerable<string> existingFields)
static bool CanDeleteField(string fieldNumber)
```

### FieldValidator

```csharp
// Validation methods
static ValidationResult ValidateField(string fieldNumber, string? value)
static ValidationResult ValidateField(FieldMetadata metadata, string? value)
static ValidationResults ValidateRecord(NistRecord record)
static ValidationResult ValidateNewField(NistRecord record, string fieldNumber, string? value)
static ValidationResult ValidateDeleteField(string fieldNumber)
```

## Error Handling

### Validation Errors
- Clear, user-friendly error messages
- Field-specific error indicators
- Prevents save when validation fails

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| "Field is required" | Missing required field value | Provide a value |
| "Must be exactly 4 digits" | Invalid version format | Use format like "0502" |
| "Must be a valid date in YYYYMMDD format" | Invalid date | Use 8-digit date like "20250115" |
| "Cannot delete required field" | Attempting to delete mandatory field | Only delete optional fields |
| "Field already exists" | Adding duplicate field | Edit existing field instead |

## Testing

The editing feature includes comprehensive test coverage:

- **FieldValidatorTests**: 12 tests covering all validation scenarios
- **SerializationRoundtripTests**: 7 tests ensuring data preservation
- **NistTransactionEditorTests**: 11 tests for editing workflows

Run tests:
```bash
dotnet test
```

## Performance Considerations

- **In-memory operations**: All editing happens in memory
- **Lazy serialization**: File written only on explicit save
- **Format preservation**: Original format (Binary/XML) maintained unless overridden
- **Efficient recalculation**: Only modified records trigger length recalculation

## Limitations

### Current Scope
- Only Type-1 and Type-2 records are editable
- Type-14 (Fingerprint), Type-10 (Face), and other image records are read-only
- Binary image data (field 999) cannot be edited through GUI

### Future Enhancements
- Support for editing additional record types
- Batch editing capabilities
- Undo/redo functionality
- Field-level diff viewer
- Import/export individual records

## Security Considerations

- **No authentication bypass**: Editing doesn't modify security fields
- **Validation enforcement**: Strict validation prevents malformed files
- **No data injection**: All inputs validated and sanitized
- **Format compliance**: Output files remain ANSI/NIST-ITL compliant

## Compliance

The editing feature maintains full compliance with:
- ANSI/NIST-ITL 1-2011 standard
- Traditional binary format specifications
- NIEM Conformant XML format specifications

## Support

For issues or questions:
- GitHub Issues: [NistParser Issues](https://github.com/anthropics/NistParser/issues)
- Documentation: See README.md and TECHNICAL_SPECIFICATION.md

## Version History

- **v1.1.0** (2025-01-13)
  - Initial editing feature release
  - Type-1 and Type-2 editing support
  - WPF GUI integration
  - Comprehensive validation
  - Roundtrip serialization
