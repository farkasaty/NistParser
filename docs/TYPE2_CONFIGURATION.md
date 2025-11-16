# Type-2 Record Field Configuration

## Overview

Type-2 records in ANSI/NIST-ITL are "User-Defined Descriptive Text" records, meaning the fields from 2.003 onwards are implementation-specific. This library supports external configuration of Type-2 fields through a JSON configuration file, allowing you to define which fields are used in your implementation without modifying code.

## Configuration File Location

The configuration file should be named `type2-fields.json` and placed in one of the following locations (searched in order):

1. `config/type2-fields.json` (recommended)
2. `type2-fields.json` (root directory)

If no configuration file is found, the library will log a warning and use a minimal configuration with only the required fields (2.001 LEN and 2.002 IDC).

## Configuration File Structure

### Basic Structure

```json
{
  "type2Fields": [
    {
      "fieldNumber": "2.004",
      "name": "Surname",
      "dataType": "text",
      "required": false,
      "maxLength": 100,
      "xmlElementName": "nc:PersonSurName"
    }
  ],
  "schemaVersion": "1.0",
  "defaultNamespaces": {
    "nc": "http://niem.gov/niem/niem-core/2.0"
  }
}
```

### Field Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `fieldNumber` | string | Yes | Field number (must start with "2.") |
| `name` | string | Yes | Field name (displayed in UI) |
| `dataType` | string | Yes | Data type: `"text"`, `"numeric"`, `"date"`, or `"enum"` |
| `required` | boolean | Yes | Whether this field is required (usually `false` for Type-2) |

### Data Type-Specific Properties

#### Text Fields (`dataType: "text"`)

```json
{
  "fieldNumber": "2.004",
  "name": "Surname",
  "dataType": "text",
  "required": false,
  "maxLength": 100,
  "validationPattern": "^[A-Za-z\\s-']+$",
  "xmlElementName": "nc:PersonSurName"
}
```

| Property | Type | Description |
|----------|------|-------------|
| `maxLength` | number | Maximum character length (optional) |
| `validationPattern` | string | Regex pattern for validation (optional) |

#### Numeric Fields (`dataType: "numeric"`)

```json
{
  "fieldNumber": "2.022",
  "name": "Height",
  "dataType": "numeric",
  "required": false,
  "minValue": 50,
  "maxValue": 300,
  "xmlElementName": "nc:PersonHeightMeasure"
}
```

| Property | Type | Description |
|----------|------|-------------|
| `minValue` | number | Minimum allowed value (optional) |
| `maxValue` | number | Maximum allowed value (optional) |

#### Date Fields (`dataType": "date"`)

```json
{
  "fieldNumber": "2.007",
  "name": "Date of Birth",
  "dataType": "date",
  "required": false,
  "format": "YYYYMMDD",
  "xmlElementName": "nc:PersonBirthDate",
  "xmlDateFormat": "YYYY-MM-DD"
}
```

| Property | Type | Description |
|----------|------|-------------|
| `format` | string | Date format for traditional format (e.g., "YYYYMMDD") |
| `xmlDateFormat` | string | Date format for XML (e.g., "YYYY-MM-DD") |

#### Enum Fields (`dataType: "enum"`)

```json
{
  "fieldNumber": "2.024",
  "name": "Sex",
  "dataType": "enum",
  "required": false,
  "enumValues": [
    { "code": "M", "description": "Male" },
    { "code": "F", "description": "Female" },
    { "code": "U", "description": "Unknown" }
  ],
  "xmlElementName": "nc:PersonSexCode"
}
```

| Property | Type | Description |
|----------|------|-------------|
| `enumValues` | array | Array of `{code, description}` objects (required for enum type) |

### XML-Specific Properties

For XML format parsing and writing:

| Property | Type | Description |
|----------|------|-------------|
| `xmlElementName` | string | XML element name with namespace prefix (e.g., `"nc:PersonSurName"`) |
| `xmlParentPath` | string | Parent element for nested structures (optional, e.g., `"nc:PersonName"`) |

## Unknown Fields

Fields that exist in a NIST file but are not defined in the configuration are treated as "unknown fields":

- **Parsing**: Unknown fields are stored in `NistRecord.UnknownFields` as simple string key-value pairs
- **Display**: WPF application shows them as "Unknown Field (not defined in configuration)"
- **Editing**: Can be added, updated, or removed via the `NistTransactionEditor` API
- **Writing**: Unknown fields are written back to the file in both Traditional and XML formats

### Working with Unknown Fields via API

```csharp
var editor = new NistTransactionEditor(transaction);
var record = transaction.Records.OfType<Type2Record>().First();

// Add an unknown field
editor.AddUnknownField(record, "2.999", "Custom value");

// Update an unknown field
editor.UpdateUnknownField(record, "2.999", "Updated value");

// Remove an unknown field
editor.RemoveUnknownField(record, "2.999");

// Access unknown fields directly
string value = record.GetUnknownField("2.999");
bool exists = record.HasUnknownField("2.999");
```

## Complete Example

See [config/type2-fields.json](../config/type2-fields.json) for a complete example configuration with all common Type-2 fields including:

- 2.003: System Information
- 2.004-2.006: Name fields (Surname, Given Name, Middle Name)
- 2.007: Date of Birth
- 2.018: Place of Birth
- 2.020: Social Security Number
- 2.022: Height
- 2.024: Sex (enum)
- 2.025: Race (enum)
- 2.026: Eye Color (enum)
- 2.027: Hair Color (enum)
- 2.030: Citizenship
- 2.040: Occupation

## Validation

The configuration file is validated when loaded:

- Field numbers must start with "2."
- Field names are required
- Data type must be one of: `text`, `numeric`, `date`, `enum`
- Enum fields must have `enumValues` defined
- Enum values must have both `code` and `description`

If validation fails, the library will throw an `InvalidOperationException` with details about the errors.

## Best Practices

1. **Use descriptive field names**: The `name` property is displayed in the UI
2. **Define XML element names**: Even if you primarily use Traditional format, defining XML element names ensures compatibility
3. **Validation patterns**: Use regex patterns for text fields that have specific formats (SSN, phone numbers, etc.)
4. **Keep it organized**: Order fields by field number for easier maintenance
5. **Comment your JSON**: The loader supports comments (// and /* */) in the JSON file
6. **Version your config**: Update `schemaVersion` when making significant changes

## Migration from Hardcoded Fields

If you were using the previous hardcoded Type-2 field definitions:

1. The provided `config/type2-fields.json` contains all previously hardcoded fields
2. Simply use this configuration file to maintain backward compatibility
3. Customize as needed for your specific implementation

## Troubleshooting

**Configuration file not found:**
```
Info: No Type-2 field configuration file found. Searched locations:
  - C:\path\to\app\config\type2-fields.json
  - C:\path\to\app\type2-fields.json
Using minimal configuration (only required fields 2.001, 2.002)
```

Solution: Create a `type2-fields.json` file in one of the searched locations.

**Validation error:**
```
Configuration validation failed:
  - Field 2.024: enum dataType requires enumValues
```

Solution: Add the required `enumValues` property to the field definition.

**Unknown fields appearing:**

If you see fields marked as "Unknown Field" in the UI, they exist in your NIST file but aren't defined in the configuration. You can either:
- Add them to your `type2-fields.json` configuration
- Leave them as unknown fields (they will still be preserved when saving)
