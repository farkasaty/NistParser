# NistParser

A .NET 9 library for parsing ANSI/NIST-ITL biometric data interchange files.

## Overview

NistParser is a comprehensive library for reading and parsing ANSI/NIST-ITL (American National Standards Institute / National Institute of Standards and Technology - Information Technology Laboratory) biometric data files. These files are used worldwide for exchanging biometric information including fingerprints, facial images, iris scans, palm prints, DNA data, and more.

## Features

- ✅ **Full ANSI/NIST-ITL 1-2011 Support**: Parses all record types (Type-1 through Type-99)
- ✅ **Multiple Encoding Types**: Handles ASCII tagged-field, binary, and mixed records
- ✅ **Hierarchical Data Structure**: Properly parses fields, subfields, and information items
- ✅ **Type-Safe**: Strong typing for all data structures
- ✅ **Well-Documented**: Comprehensive XML documentation for IntelliSense
- ✅ **Extensible**: Easy to add support for custom record types
- ✅ **Performance**: Efficient binary parsing with minimal allocations

## Supported Record Types

| Record Type | Description | Encoding |
|-------------|-------------|----------|
| **Type-1** | Transaction Information (MANDATORY) | ASCII Tagged |
| **Type-2** | User-Defined Descriptive Text | ASCII Tagged |
| **Type-4** | High-Resolution Grayscale Fingerprint | Binary |
| **Type-9** | Minutiae Data | ASCII Tagged |
| **Type-10** | Facial & SMT Images | Mixed |
| **Type-13** | Latent Fingerprint Images | Mixed |
| **Type-14** | Fingerprint Images | Mixed |
| **Type-15** | Palm Print Images | Mixed |
| **Type-17** | Iris Images | Mixed |
| **Type-18** | DNA Data | ASCII Tagged |
| And more... | All record types (1-99) supported | Various |

## Installation

### NuGet Package

```bash
dotnet add package NistParser
```

### From Source

```bash
git clone https://github.com/farkasaty/NistParser.git
cd NistParser
dotnet build src/NistParser/NistParser.csproj
```

## Quick Start

### Basic Usage

```csharp
using NistParser;
using NistParser.Core;

// Parse a NIST file
var transaction = NistTransactionParser.ParseFile("path/to/file.nist");

// Access transaction metadata
Console.WriteLine($"Version: {transaction.Version}");
Console.WriteLine($"TCN: {transaction.TransactionControlNumber}");
Console.WriteLine($"ORI: {transaction.OriginatingAgencyIdentifier}");
Console.WriteLine($"Records: {transaction.TotalRecordCount}");

// Access Type-1 header
var header = transaction.Header;
Console.WriteLine($"Date: {header.Date}");
Console.WriteLine($"Transaction Type: {header.TypeOfTransaction}");
```

### Working with Biographic Data (Type-2)

```csharp
using NistParser.Records;

// Get Type-2 records
var type2Records = transaction.GetRecords<Type2Record>();

foreach (var record in type2Records)
{
    Console.WriteLine($"Name: {record.FullName}");
    Console.WriteLine($"DOB: {record.DateOfBirth}");
    Console.WriteLine($"Sex: {record.Sex}");
    Console.WriteLine($"IDC: {record.IDC}");
}
```

### Working with Fingerprint Images (Type-14)

```csharp
using NistParser.Records;

// Get Type-14 fingerprint records
var fingerprintRecords = transaction.GetRecords<Type14Record>();

foreach (var record in fingerprintRecords)
{
    Console.WriteLine($"Finger Position: {record.FingerPosition}");
    Console.WriteLine($"Resolution: {record.ScanningResolution} ppmm");
    Console.WriteLine($"Size: {record.Width}x{record.Height}");
    Console.WriteLine($"Compression: {record.Compression?.GetDisplayName()}");
    Console.WriteLine($"Image Data Size: {record.ImageDataSize} bytes");

    // Access the raw image data
    byte[]? imageData = record.ImageData;
    if (imageData != null)
    {
        // Save or process the image
        File.WriteAllBytes($"fingerprint_{record.IDC}.dat", imageData);
    }
}
```

### Linking Related Records by IDC

```csharp
// Get all records with a specific IDC
string idc = "00";
var relatedRecords = transaction.GetRecordsByIDC(idc);

foreach (var record in relatedRecords)
{
    Console.WriteLine($"Type-{(int)record.RecordType}: {record}");
}

// Or get a specific record type with an IDC
var fingerprint = transaction.GetRecord<Type14Record>("00");
if (fingerprint != null)
{
    Console.WriteLine($"Found fingerprint: {fingerprint}");
}
```

### Accessing Fields Directly

```csharp
using NistParser.Core;

// Access any field from any record
foreach (var record in transaction.Records)
{
    // Get a specific field
    NistField? field = record.GetField("14.009"); // Scanning resolution
    if (field != null)
    {
        Console.WriteLine($"Field {field.FieldNumber}: {field.FirstValue}");
    }

    // Iterate through all fields
    foreach (var kvp in record.Fields)
    {
        string fieldNumber = kvp.Key;
        NistField field = kvp.Value;

        if (field.IsBinary)
        {
            Console.WriteLine($"{fieldNumber}: [Binary data, {field.BinaryData?.Length} bytes]");
        }
        else
        {
            Console.WriteLine($"{fieldNumber}: {field.FirstValue}");
        }
    }
}
```

### Parsing from Byte Array

```csharp
byte[] fileData = File.ReadAllBytes("file.nist");
var transaction = NistTransactionParser.Parse(fileData);
```

### Error Handling

```csharp
using NistParser.Exceptions;

try
{
    var transaction = NistTransactionParser.ParseFile("file.nist");
}
catch (MissingType1Exception ex)
{
    Console.WriteLine("Type-1 record is missing or invalid");
}
catch (TruncatedFileException ex)
{
    Console.WriteLine($"File is incomplete: {ex.Message}");
}
catch (InvalidFieldFormatException ex)
{
    Console.WriteLine($"Invalid field format: {ex.Message}");
}
catch (NistParserException ex)
{
    Console.WriteLine($"Parser error: {ex.Message}");
}
```

## Advanced Usage

### Transaction Summary

```csharp
// Get a detailed summary of the transaction
string summary = transaction.GetDetailedSummary();
Console.WriteLine(summary);
```

Output:
```
ANSI/NIST-ITL Transaction
Version: 0502
TCN: ABC123456
ORI: AGENCY001
Date: 7/17/2025 12:00:00 AM
Total Records: 4

Records:
  Type-1 (Header): Type-1 Record (Version 0502, TCN: ABC123456, 3 additional records)
  Type-2 Record (IDC: 00) - John Doe, DOB: 1980-01-15
  Type-14 Record (IDC: 00) - Finger 1, 800x1000, JPEG 2000 (Lossless), 45.2 KB
  Type-9 Record (IDC: 00), 25 fields, 2048 bytes
```

### Custom Record Type Handling

For record types not specifically implemented, use `GenericNistRecord`:

```csharp
using NistParser.Records;

var genericRecords = transaction.GetRecords<GenericNistRecord>();

foreach (var record in genericRecords)
{
    Console.WriteLine($"Type-{(int)record.RecordType}");

    // Access fields generically
    foreach (var field in record.Fields.Values)
    {
        // Process field data
    }
}
```

## Architecture

```
NistParser/
├── Core/
│   ├── NistTransaction.cs     - Main transaction container
│   ├── NistRecord.cs           - Base class for all records
│   └── NistField.cs            - Field, subfield, and item structures
├── Constants/
│   ├── SeparatorConstants.cs  - ASCII separator definitions
│   ├── RecordType.cs           - Record type enumeration
│   ├── CompressionAlgorithm.cs - Image compression types
│   └── RecordEncoding.cs       - Encoding type definitions
├── Records/
│   ├── Type1Record.cs          - Transaction header
│   ├── Type2Record.cs          - Biographic data
│   ├── Type14Record.cs         - Fingerprint images
│   └── GenericNistRecord.cs    - Generic record handler
├── Utilities/
│   └── FieldParser.cs          - Field parsing utilities
├── Exceptions/
│   └── NistParserException.cs  - Custom exceptions
└── NistTransactionParser.cs    - Main parser class
```

## Data Model

### Hierarchy

```
NistTransaction
├── Header (Type1Record)
│   ├── Version
│   ├── TransactionControlNumber
│   ├── Content (list of records in file)
│   └── Fields (dictionary of NistField)
└── Records (List<NistRecord>)
    └── NistRecord
        ├── RecordType
        ├── IDC
        ├── Fields (dictionary of NistField)
        └── NistField
            ├── FieldNumber (e.g., "1.003")
            ├── Subfields (List<NistSubfield>)
            └── NistSubfield
                └── Items (List<string>)
```

## Technical Details

### Separator Characters

The ANSI/NIST-ITL standard uses four ASCII control characters:

- **US (0x1F)**: Unit Separator - Separates information items
- **RS (0x1E)**: Record Separator - Separates subfields
- **GS (0x1D)**: Group Separator - Separates fields
- **FS (0x1C)**: File Separator - Separates records

### Encoding Types

- **Tagged ASCII**: Fields identified as "X.YYY:data" (Type-1, Type-2, Type-9, etc.)
- **Binary**: Fixed-length binary fields (Type-3 through Type-8)
- **Mixed**: Tagged fields + binary field 999 for image data (Type-10, Type-14, etc.)

## Consuming the Library

### From Web API

```csharp
using Microsoft.AspNetCore.Mvc;
using NistParser;

[ApiController]
[Route("api/[controller]")]
public class BiometricController : ControllerBase
{
    [HttpPost("parse")]
    public IActionResult ParseNistFile(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);

        var transaction = NistTransactionParser.Parse(memoryStream.ToArray());

        return Ok(new
        {
            Version = transaction.Version,
            TCN = transaction.TransactionControlNumber,
            RecordCount = transaction.TotalRecordCount,
            Records = transaction.Records.Select(r => new
            {
                Type = (int)r.RecordType,
                IDC = r.IDC,
                Fields = r.Fields.Count
            })
        });
    }
}
```

### From WPF Application

```csharp
using System.Windows;
using NistParser;
using NistParser.Records;

public partial class MainWindow : Window
{
    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "NIST Files (*.nist)|*.nist|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var transaction = NistTransactionParser.ParseFile(dialog.FileName);

                // Display transaction info
                VersionTextBlock.Text = transaction.Version;
                TcnTextBlock.Text = transaction.TransactionControlNumber;

                // Display records in ListBox
                RecordsListBox.ItemsSource = transaction.Records;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing NIST file: {ex.Message}",
                    "Parse Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
```

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## License

This project is licensed under the MIT License.

## References

- [ANSI/NIST-ITL 1-2011 Standard](https://www.nist.gov/itl/iad/image-group/ansinist-itl-standard)
- [NIST Special Publication 500-290](https://www.nist.gov/publications/data-format-interchange-fingerprint-facial-other-biometric-information-ansinist-itl-1-1)
- [Technical Specification](./TECHNICAL_SPECIFICATION.md)
- [Implementation Summary](./IMPLEMENTATION_SUMMARY.md)

## Support

For questions or issues, please open an issue on GitHub or contact the maintainers.

---

**Note**: This library is for parsing ANSI/NIST-ITL files. Image decompression (WSQ, JPEG 2000, etc.) is not included. Use appropriate image processing libraries for decompressing biometric images.
