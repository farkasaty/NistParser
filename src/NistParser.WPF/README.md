# NistParser.WPF - ANSI/NIST-ITL File Viewer

A Windows Presentation Foundation (WPF) application for viewing and analyzing ANSI/NIST-ITL biometric data files.

## Features

✅ **File Browser**: Easy-to-use file selection dialog for .nist and .an2 files
✅ **Transaction Summary**: Display key transaction metadata at a glance
✅ **Records List**: Browse all records in the file with summary information
✅ **Detailed View**: Examine all fields and data within each record
✅ **Binary Data Detection**: Identifies and displays binary image data
✅ **Type-Specific Details**: Shows specialized information for Type-2, Type-14, and other record types
✅ **User-Friendly UI**: Modern, clean interface with intuitive navigation

## Screenshots

### Main Window
The application displays:
- Transaction header information (Version, TCN, ORI, Date, etc.)
- A list of all records in the file
- Detailed field information for the selected record

## Usage

### Opening a NIST File

1. Launch the application
2. Click **"Open NIST File"** button
3. Browse to your NIST file (.nist or .an2 extension)
4. The file will be automatically parsed and displayed

### Viewing Transaction Information

The top panel shows key transaction metadata:
- **Version**: ANSI/NIST-ITL standard version
- **Date**: Transaction date
- **TCN**: Transaction Control Number
- **Transaction Type**: Type of transaction (e.g., "CRM" for criminal)
- **ORI**: Originating Agency Identifier
- **DAI**: Destination Agency Identifier
- **Total Records**: Number of records in the file
- **Resolution**: Native scanning resolution

### Browsing Records

The left panel shows all records in the file:
- **Type-1**: Transaction Information (always present)
- **Type-2**: Biographic data (name, DOB, etc.)
- **Type-14**: Fingerprint images
- **Other types**: All other record types with their IDC values

Click on any record to view its details.

### Viewing Record Details

The right panel shows detailed information for the selected record:
- **Record Type**: The type number and name
- **IDC**: Information Designation Character (links related records)
- **Record Length**: Size of the record in bytes
- **Fields**: All fields in the record with their values

### Field Display

Fields are shown with their complete field numbers (e.g., "1.002", "14.999"):
- **Simple Fields**: Display as plain text
- **Complex Fields**: Show multiple subfields and information items
- **Binary Fields**: Indicate binary data with size information
- **Image Fields**: Show compression type and dimensions (for Type-14, etc.)

## Understanding the Data

### Record Types

| Type | Description |
|------|-------------|
| Type-1 | Transaction header (always present) |
| Type-2 | Biographic/demographic data |
| Type-4 | High-resolution fingerprint (binary) |
| Type-9 | Minutiae data |
| Type-10 | Facial images and SMT |
| Type-13 | Latent fingerprint images |
| Type-14 | Fingerprint images (variable resolution) |
| Type-15 | Palm print images |
| Type-17 | Iris images |

### Information Designation Character (IDC)

The IDC links related records together. For example:
- A Type-14 fingerprint image with IDC "00"
- A Type-9 minutiae record with IDC "00"
- Both represent the same fingerprint

### Field Structure

Fields follow the format **X.YYY**:
- **X**: Record type number (e.g., "1", "14")
- **YYY**: Field number (e.g., "002", "999")

Example: **14.999** = Type-14 record, field 999 (image data)

## Error Handling

The application handles various error conditions:
- **Missing Type-1**: File is invalid (Type-1 is mandatory)
- **Truncated File**: File is incomplete or corrupted
- **Parse Errors**: Invalid field format or structure
- **Invalid Files**: Not a valid ANSI/NIST-ITL file

Error messages provide detailed information to help diagnose issues.

## Building the Application

### Prerequisites
- .NET 9 SDK or later
- Windows 10/11
- Visual Studio 2022 (optional)

### Build Instructions

```bash
# From the repository root
dotnet build src/NistParser.WPF/NistParser.WPF.csproj

# Run the application
dotnet run --project src/NistParser.WPF/NistParser.WPF.csproj
```

Or open `NistParser.sln` in Visual Studio and build/run the WPF project.

## Technical Details

### Architecture
- **MVVM Pattern**: Uses ViewModels for data binding
- **NistParser Library**: Leverages the core parser library
- **WPF Framework**: Modern Windows desktop UI
- **Data Binding**: Efficient UI updates with ObservableCollections

### Dependencies
- NistParser (core library)
- .NET 9 Windows Desktop Runtime

### Performance
- Lazy loading for large files
- Efficient memory management
- Responsive UI during parsing

## Sample NIST Files

To test the application, you can:
1. Obtain sample files from NIST: https://www.nist.gov/itl/iad/image-group
2. Request test files from biometric vendors
3. Generate test files using reference implementations

## Keyboard Shortcuts

- **Ctrl+O**: Open file dialog
- **Up/Down Arrows**: Navigate records list
- **Mouse Wheel**: Scroll record details

## Known Limitations

- Image decompression not included (binary data shown as-is)
- Very large files (>1GB) may take time to load
- Complex multi-page records may require scrolling

## Support

For issues or questions:
- Check the main [NistParser README](../../README.md)
- Review the [Technical Specification](../../TECHNICAL_SPECIFICATION.md)
- Open an issue on GitHub

## License

This application is part of the NistParser project and is licensed under the MIT License.

---

**Note**: This viewer displays the parsed structure of ANSI/NIST-ITL files. For image viewing, use appropriate image processing tools that support WSQ, JPEG 2000, and other biometric image formats.
