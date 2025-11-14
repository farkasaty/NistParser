# NistParser Quick Start Guide

NistParser is a .NET library for parsing, editing, and writing ANSI/NIST-ITL biometric data interchange files. This guide will help you get started quickly.

## Installation

### Build from Source
```bash
git clone https://github.com/farkasaty/NistParser.git
cd NistParser
dotnet build
```

## Basic Usage

### 1. Parsing a NIST File

```csharp
using NistParser;
using NistParser.Core;

// Parse a NIST file from disk
var parser = new NistTransactionParser();
NistTransaction transaction = parser.ParseFile("path/to/file.an2");

// Or parse from a byte array
byte[] data = File.ReadAllBytes("path/to/file.an2");
NistTransaction transaction = parser.Parse(data);

// Access transaction information
Console.WriteLine($"Version: {transaction.Version}");
Console.WriteLine($"Total Records: {transaction.Records.Count}");

// Iterate through records
foreach (var record in transaction.Records)
{
    Console.WriteLine($"Record Type: {record.RecordType}");
    Console.WriteLine($"Field Count: {record.Fields.Count}");
}
```

### 2. Reading Field Values

```csharp
// Get a specific record by type
var type1Record = transaction.GetRecordsByType(RecordType.Type1_TransactionInformation).First();

// Read field values
string version = type1Record.GetField("1.002")?.FirstValue; // Version number
string tot = type1Record.GetField("1.004")?.FirstValue;     // Type of transaction
string date = type1Record.GetField("1.005")?.FirstValue;    // Date

Console.WriteLine($"Version: {version}");
Console.WriteLine($"Transaction Type: {tot}");
Console.WriteLine($"Date: {date}");

// Get Type-2 records (user-defined text)
var type2Records = transaction.GetRecordsByType(RecordType.Type2_UserDefinedText);
foreach (var record in type2Records)
{
    string surname = record.GetField("2.004")?.FirstValue;
    string givenName = record.GetField("2.005")?.FirstValue;
    Console.WriteLine($"Name: {givenName} {surname}");
}
```

### 3. Editing NIST Data

```csharp
using NistParser.Editing;

// Create an editor
var editor = new NistTransactionEditor(transaction);

// Get a record to edit
var type1Record = transaction.GetRecordsByType(RecordType.Type1_TransactionInformation).First();

// Update a field value
var result = editor.UpdateField(type1Record, "1.008", "NEW_AGENCY_ID");
if (result.IsSuccess)
{
    Console.WriteLine("Field updated successfully");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}

// Add a new field
result = editor.AddField(type1Record, "1.017", "My Agency Name");
if (result.IsSuccess)
{
    Console.WriteLine("Field added successfully");
}

// Delete a field (only non-required fields)
result = editor.DeleteField(type1Record, "1.017");
if (result.IsSuccess)
{
    Console.WriteLine("Field deleted successfully");
}
```

### 4. Validating Fields

```csharp
using NistParser.Editing;

// Validate a single field
var validationResult = FieldValidator.ValidateField("1.002", "0502");
if (validationResult.IsValid)
{
    Console.WriteLine("Field is valid");
}
else
{
    Console.WriteLine($"Validation error: {validationResult.ErrorMessage}");
}

// Validate an entire record
var type1Record = transaction.GetRecordsByType(RecordType.Type1_TransactionInformation).First();
var results = FieldValidator.ValidateRecord(type1Record);

if (results.IsValid)
{
    Console.WriteLine("All fields are valid");
}
else
{
    foreach (var error in results.Errors)
    {
        Console.WriteLine($"Field {error.FieldNumber}: {error.ErrorMessage}");
    }
}
```

### 5. Writing NIST Files

```csharp
using NistParser.Writing;

// Write in traditional ANSI/NIST format
var writer = new NistTransactionWriter();
byte[] outputData = writer.Write(transaction, NistFormat.Traditional);
File.WriteAllBytes("output.an2", outputData);

// Write in NIEM XML format
byte[] xmlData = writer.Write(transaction, NistFormat.NiemXml);
File.WriteAllBytes("output.xml", xmlData);
```

## Common Scenarios

### Creating a New NIST Transaction

```csharp
using NistParser.Core;
using NistParser.Editing;

// Create a new transaction
var transaction = new NistTransaction();

// Create Type-1 record (required)
var type1 = new NistRecord(RecordType.Type1_TransactionInformation);
type1.AddField("1.002", "0502");  // Version
type1.AddField("1.004", "CRM");   // Type of transaction
type1.AddField("1.005", DateTime.Now.ToString("yyyyMMdd")); // Date
type1.AddField("1.007", "DESTINATION_AGENCY"); // DAI
type1.AddField("1.008", "ORIGIN_AGENCY");      // ORI
type1.AddField("1.009", "TCN_" + Guid.NewGuid().ToString()); // TCN

transaction.Records.Add(type1);

// Create Type-2 record (optional, for biographic data)
var type2 = new Type2Record();
type2.AddField("2.002", "00"); // IDC
type2.AddField("2.004", "Doe");   // Surname
type2.AddField("2.005", "John");  // Given name

transaction.Records.Add(type2);

// Write to file
var writer = new NistTransactionWriter();
byte[] data = writer.Write(transaction, NistFormat.Traditional);
File.WriteAllBytes("new_transaction.an2", data);
```

### Working with Type-2 Records (Biographic Data)

```csharp
// Parse existing file
var transaction = parser.ParseFile("sample.an2");

// Get all Type-2 records
var type2Records = transaction.GetRecordsByType(RecordType.Type2_UserDefinedText);

foreach (var record in type2Records)
{
    // Read common biographic fields
    string surname = record.GetField("2.004")?.FirstValue;
    string givenName = record.GetField("2.005")?.FirstValue;
    string middleName = record.GetField("2.006")?.FirstValue;
    string dob = record.GetField("2.007")?.FirstValue;
    string sex = record.GetField("2.024")?.FirstValue;
    string race = record.GetField("2.025")?.FirstValue;

    Console.WriteLine($"Subject: {givenName} {middleName} {surname}");
    Console.WriteLine($"DOB: {dob}");
    Console.WriteLine($"Sex: {sex}, Race: {race}");
}
```

### Working with Fingerprint Records (Type-14)

```csharp
// Get Type-14 fingerprint records
var fingerprintRecords = transaction.GetRecordsByType(RecordType.Type14_FingerprintImage);

foreach (Type14Record fpRecord in fingerprintRecords)
{
    // Access fingerprint metadata
    string idc = fpRecord.GetField("14.002")?.FirstValue;  // IDC
    string imp = fpRecord.GetField("14.003")?.FirstValue;  // Impression type
    string fgp = fpRecord.GetField("14.013")?.FirstValue;  // Finger position

    // Access image data
    byte[] imageData = fpRecord.ImageData;

    Console.WriteLine($"Fingerprint IDC: {idc}, Position: {fgp}");
    Console.WriteLine($"Image size: {imageData.Length} bytes");

    // Save image to file
    File.WriteAllBytes($"fingerprint_{idc}.jpg", imageData);
}
```

### Batch Processing Multiple Files

```csharp
var parser = new NistTransactionParser();
var filesDirectory = "path/to/nist/files";

foreach (var file in Directory.GetFiles(filesDirectory, "*.an2"))
{
    try
    {
        var transaction = parser.ParseFile(file);

        // Process the transaction
        var type1 = transaction.GetRecordsByType(RecordType.Type1_TransactionInformation).First();
        string tcn = type1.GetField("1.009")?.FirstValue;

        Console.WriteLine($"Processing file: {Path.GetFileName(file)}");
        Console.WriteLine($"TCN: {tcn}");
        Console.WriteLine($"Records: {transaction.Records.Count}");

        // Perform operations...
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing {file}: {ex.Message}");
    }
}
```

## Field Metadata

NistParser includes metadata for common NIST fields to help with validation and editing:

```csharp
using NistParser.Editing;

// Get metadata for a specific field
var metadata = FieldMetadataProvider.GetFieldMetadata("1.002");
if (metadata != null)
{
    Console.WriteLine($"Display Name: {metadata.DisplayName}");
    Console.WriteLine($"Required: {metadata.IsRequired}");
    Console.WriteLine($"Data Type: {metadata.DataType}");
    Console.WriteLine($"Help Text: {metadata.HelpText}");
}

// Get all editable fields for a record type
var editableFields = FieldMetadataProvider.GetEditableFields(RecordType.Type1_TransactionInformation);
foreach (var field in editableFields)
{
    Console.WriteLine($"{field.FieldNumber} - {field.DisplayName}");
}
```

## Supported Record Types

NistParser supports all ANSI/NIST-ITL record types:

- **Type-1**: Transaction Information (required)
- **Type-2**: User-Defined Descriptive Text
- **Type-4**: High-Resolution Grayscale Fingerprint
- **Type-7**: User-Defined Image
- **Type-8**: Signature Image
- **Type-9**: Minutiae Data
- **Type-10**: Facial and SMT Image
- **Type-13**: Friction Ridge Latent Image
- **Type-14**: Fingerprint Image
- **Type-15**: Palm Print Image
- **Type-17**: Iris Image
- **Type-18**: DNA Data
- **Type-19**: Plantar (Footprint) Image
- **Type-99**: CBEFF Biometric Data
- And more...

## Error Handling

```csharp
try
{
    var transaction = parser.ParseFile("file.an2");
}
catch (FileNotFoundException)
{
    Console.WriteLine("File not found");
}
catch (Exception ex)
{
    Console.WriteLine($"Error parsing file: {ex.Message}");
}

// Validation errors
var result = editor.UpdateField(record, "1.002", "invalid");
if (!result.IsValid)
{
    Console.WriteLine($"Validation failed: {result.ErrorMessage}");
}
```

## Format Support

NistParser supports reading and writing multiple formats:

### Traditional ANSI/NIST Format
- Binary format with GS/RS/US separators
- Standard for law enforcement and government agencies
```csharp
byte[] data = writer.Write(transaction, NistFormat.Traditional);
```

### NIEM XML Format
- XML-based format conforming to NIEM standards
- Human-readable and easier to debug
```csharp
byte[] xmlData = writer.Write(transaction, NistFormat.NiemXml);
```

## Additional Resources

- **GitHub Repository**: https://github.com/farkasaty/NistParser
- **ANSI/NIST-ITL Standard**: [NIST SP 500-290](https://www.nist.gov/itl/iad/image-group/ansinist-itl-standard-references)
- **Issue Tracker**: https://github.com/farkasaty/NistParser/issues

## License

This project is licensed under the MIT License - see the LICENSE file for details.
