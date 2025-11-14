# ANSI/NIST-ITL Parser - Implementation Summary

## What You Need to Build

### Core Functionality
A .NET parser that can:
1. **Read** ANSI/NIST-ITL biometric data files (Traditional/Binary encoding)
2. **Parse** the hierarchical structure (Transaction → Records → Fields → Subfields → Items)
3. **Extract** data from both ASCII tagged-field records and binary records
4. **Validate** against the standard's requirements
5. **Provide** structured access to biometric and biographic data

---

## File Format at a Glance

### Structure
```
ANSI/NIST File (Transaction)
│
├── Type-1 Record (MANDATORY) ─ Transaction metadata
│   ├── Field 1.001: Record length
│   ├── Field 1.002: Version ("0502")
│   ├── Field 1.003: Content list (what records are in the file)
│   ├── Field 1.004: Transaction type
│   ├── Field 1.005: Date (YYYYMMDD)
│   └── ... (more fields)
│
├── Type-2 Record (optional) ─ Biographic data
│   └── User-defined text fields
│
├── Type-9 Record (optional) ─ Fingerprint minutiae
│   └── ASCII tagged fields
│
├── Type-14 Record (optional) ─ Fingerprint images
│   ├── Tagged fields (001-998)
│   └── Field 999: Binary image data
│
└── ... (more records as listed in Type-1 CNT field)
```

### Critical Separator Characters
```
0x1F (US) - Separates items within a field      →  item₁<US>item₂<US>item₃
0x1E (RS) - Separates subfields in a field      →  subfield₁<RS>subfield₂
0x1D (GS) - Separates fields in a record        →  field₁<GS>field₂<GS>field₃
0x1C (FS) - Separates records in transaction    →  record₁<FS>record₂<FS>
```

---

## Key Implementation Steps

### Step 1: File Reading
```csharp
// Read entire file as binary
byte[] fileData = File.ReadAllBytes(filePath);
```

### Step 2: Parse Type-1 Record (ASCII)
```
1. Find "1.001:" at start of file
2. Read fields separated by GS (0x1D)
3. Each field format: "X.YYY:data"
4. Parse field 1.003 (CNT) to know what records follow
5. Type-1 ends with FS (0x1C)
```

**Field 1.003 Example:**
```
1.003:1<US>3<RS>2<US>00<RS>14<US>00<RS>9<US>00<GS>
```
Means: Transaction contains Type-2 (IDC 00), Type-14 (IDC 00), Type-9 (IDC 00)

### Step 3: Parse Subsequent Records

**For ASCII/Tagged Records (Type-1, Type-2, Type-9, Type-18):**
```
1. Split on GS (0x1D) to get fields
2. For each field:
   - Parse "X.YYY:data" format
   - Split data on RS (0x1E) to get subfields
   - Split subfields on US (0x1F) to get items
3. Record ends with FS (0x1C)
```

**For Binary Records (Type-3, Type-4, Type-5, Type-6, Type-8):**
```
1. Read fixed-length fields in specified order
2. First field is always record length (binary)
3. Field 999 contains image data (rest of record)
4. NO separator characters are meaningful
```

**For Mixed Records (Type-10, Type-13, Type-14, Type-15, Type-17, Type-19-22, Type-99):**
```
1. Parse fields 001-998 as ASCII tagged fields
2. Field 999 contains binary image data
3. Image follows immediately after "999:"
```

---

## Data Structures You'll Need

### Transaction
```csharp
public class NistTransaction
{
    public Type1Record Header { get; set; }
    public List<NistRecord> Records { get; set; }
}
```

### Record (Base)
```csharp
public abstract class NistRecord
{
    public int RecordType { get; set; }
    public string IDC { get; set; }
    public Dictionary<string, NistField> Fields { get; set; }
}
```

### Field
```csharp
public class NistField
{
    public string FieldNumber { get; set; }  // e.g., "1.003"
    public List<Subfield> Subfields { get; set; }
}

public class Subfield
{
    public List<string> Items { get; set; }
}
```

### Specific Record Types
```csharp
public class Type1Record : NistRecord
{
    public string Version { get; set; }       // 1.002
    public TransactionContent Content { get; set; }  // 1.003 (parsed)
    public string TransactionType { get; set; }  // 1.004
    public DateTime Date { get; set; }        // 1.005
    public string ORI { get; set; }           // 1.008
    public string TCN { get; set; }           // 1.009
}

public class Type14Record : NistRecord  // Fingerprint image
{
    public int Resolution { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public CompressionAlgorithm Compression { get; set; }
    public byte[] ImageData { get; set; }  // Field 999
}
```

---

## Essential Parsing Functions

### 1. Parse Type-1 CNT Field
```csharp
public TransactionContent ParseCNT(string cntData)
{
    // Input: "1\x1F3\x1E2\x1F00\x1E14\x1F00\x1E9\x1F00"
    // Split on RS (0x1E) to get record entries
    // First item is count
    // Subsequent items are recordType<US>IDC pairs
}
```

### 2. Split Tagged Field
```csharp
public (string fieldNumber, string data) ParseTaggedField(string field)
{
    // Input: "1.003:data"
    // Split on ':' to get field number and data
    // Return ("1.003", "data")
}
```

### 3. Parse Subfields and Items
```csharp
public List<List<string>> ParseFieldData(string data)
{
    // Split on RS (0x1E) to get subfields
    // For each subfield, split on US (0x1F) to get items
}
```

### 4. Extract Binary Image Data
```csharp
public byte[] ExtractField999(byte[] recordData)
{
    // Find "999:" in byte array
    // Return all bytes after the colon until GS/FS
}
```

---

## Record Types Priority (Start with These)

### Phase 1: Essential
1. **Type-1** - Transaction header (MANDATORY)
2. **Type-2** - Biographic data (common)

### Phase 2: Core Biometrics
3. **Type-9** - Minutiae data (ASCII)
4. **Type-14** - Fingerprint images (mixed)

### Phase 3: Extended
5. **Type-10** - Facial images (mixed)
6. **Type-13** - Latent prints (mixed)
7. **Type-15** - Palm prints (mixed)
8. **Type-17** - Iris images (mixed)

### Phase 4: Advanced
9. **Type-4** - Legacy fingerprint (binary)
10. **Type-18** - DNA data (ASCII)
11. Other types as needed

---

## Validation Rules

### Critical Validations
- [ ] Type-1 record exists and is first
- [ ] Field 1.001 (length) matches actual Type-1 length
- [ ] Field 1.002 (version) is valid ("0500", "0502", etc.)
- [ ] Field 1.003 (CNT) lists all records in file
- [ ] All records listed in CNT are present
- [ ] IDC values link related records correctly
- [ ] Record lengths match actual sizes
- [ ] No truncated records

### Data Validations
- [ ] Date fields match "YYYYMMDD" format
- [ ] Numeric fields contain only digits
- [ ] IDC is two-digit numeric "00"-"99"
- [ ] Resolution values are within tolerance
- [ ] Compression codes are valid (0-6)
- [ ] Field numbers are valid for their record type

---

## Testing Strategy

### Test Files Needed
1. **Simple file**: Type-1 + Type-2 only
2. **Fingerprint file**: Type-1 + Type-14 (image)
3. **Minutiae file**: Type-1 + Type-9 + Type-14 (linked by IDC)
4. **Facial file**: Type-1 + Type-10
5. **Complex file**: Multiple record types with multiple IDCs

### Validation Approach
1. Parse sample files from NIST
2. Compare output with reference implementations:
   - node-nist (TypeScript)
   - NIST3 (Python)
   - NBIS tools
3. Use NIST conformance testing tools (BioCTS)

---

## Common Pitfalls to Avoid

❌ **Don't** interpret separator characters in binary records
❌ **Don't** assume all records are ASCII tagged
❌ **Don't** forget that Field 999 is binary in mixed records
❌ **Don't** parse field X.YYY as a decimal number (it's two separate numbers)
❌ **Don't** forget the FS terminator in record length calculation
❌ **Don't** use string parsing for binary image data

✅ **Do** handle separator characters correctly (US, RS, GS, FS)
✅ **Do** validate record lengths before parsing
✅ **Do** support both ASCII and binary records
✅ **Do** link records by IDC
✅ **Do** preserve image data as byte arrays
✅ **Do** implement proper error handling

---

## Example Parsing Workflow

```
┌─────────────────────────────────────────┐
│ 1. Read file as byte array              │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│ 2. Find Type-1 record                   │
│    - Search for "1.001:" at start       │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│ 3. Parse Type-1 fields                  │
│    - Split on GS (0x1D)                 │
│    - Extract each field "X.YYY:data"    │
│    - Parse 1.003 (CNT) to get record    │
│      list                               │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│ 4. For each record in CNT:              │
│    ├─ Determine record type             │
│    ├─ If ASCII: Parse tagged fields     │
│    ├─ If Binary: Read fixed fields      │
│    └─ If Mixed: Parse both types        │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│ 5. Build structured object model        │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│ 6. Validate transaction                 │
└──────────────┬──────────────────────────┘
               │
┌──────────────▼──────────────────────────┐
│ 7. Return parsed transaction            │
└─────────────────────────────────────────┘
```

---

## Recommended Architecture

```
NistParser/
├── src/
│   ├── Core/
│   │   ├── NistTransaction.cs
│   │   ├── NistRecord.cs
│   │   ├── NistField.cs
│   │   └── Enums.cs (RecordType, CompressionAlgorithm, etc.)
│   │
│   ├── Records/
│   │   ├── Type1Record.cs
│   │   ├── Type2Record.cs
│   │   ├── Type9Record.cs
│   │   ├── Type14Record.cs
│   │   └── ... (other record types)
│   │
│   ├── Parsers/
│   │   ├── IRecordParser.cs
│   │   ├── TaggedFieldParser.cs
│   │   ├── BinaryFieldParser.cs
│   │   ├── MixedRecordParser.cs
│   │   ├── Type1Parser.cs
│   │   └── ... (parsers for specific record types)
│   │
│   ├── Validators/
│   │   ├── TransactionValidator.cs
│   │   ├── RecordValidator.cs
│   │   └── FieldValidator.cs
│   │
│   └── Utilities/
│       ├── SeparatorConstants.cs
│       ├── FieldNumberParser.cs
│       └── ImageExtractor.cs
│
├── tests/
│   ├── ParserTests.cs
│   ├── ValidatorTests.cs
│   └── TestFiles/ (sample NIST files)
│
└── docs/
    ├── TECHNICAL_SPECIFICATION.md
    └── IMPLEMENTATION_SUMMARY.md (this file)
```

---

## Next Steps

1. **Set up project structure**
   - Create .NET solution
   - Add class library project
   - Add test project
   - Add necessary NuGet packages (image processing, etc.)

2. **Implement core classes**
   - Define data models (Transaction, Record, Field)
   - Create separator constants
   - Build basic file reader

3. **Implement Type-1 parser**
   - Parse tagged ASCII fields
   - Handle field 1.003 (CNT) specially
   - Validate Type-1 requirements

4. **Implement Type-2 parser**
   - Reuse tagged field parsing logic
   - Handle user-defined fields flexibly

5. **Add image record parsers**
   - Type-14 (fingerprints)
   - Type-10 (facial)
   - Extract binary field 999

6. **Add validation**
   - Transaction-level validation
   - Record-level validation
   - Field-level validation

7. **Testing**
   - Unit tests for each parser
   - Integration tests with sample files
   - Validation against reference implementations

8. **Documentation**
   - API documentation
   - Usage examples
   - Known limitations

---

## Resources for Sample Files

- **NIST Special Databases**: https://www.nist.gov/itl/iad/image-group/nist-special-databases
- **NIST Sample Files**: Request from ansinist@nist.gov
- **Open Source Implementations**: Use test files from node-nist, NIST3 repositories
- **Create Your Own**: Use reference implementations to generate simple test files

---

## Quick Reference Card

| Concept | Value/Format |
|---------|--------------|
| **Type-1 is mandatory** | Every file starts with Type-1 |
| **Field format** | `X.YYY:data<separator>` |
| **US separator** | `0x1F` (item separator) |
| **RS separator** | `0x1E` (subfield separator) |
| **GS separator** | `0x1D` (field separator) |
| **FS separator** | `0x1C` (record separator) |
| **Version field** | `1.002` (e.g., "0502") |
| **CNT field** | `1.003` (lists all records) |
| **Date format** | `YYYYMMDD` (e.g., "20250717") |
| **IDC format** | Two digits: "00" to "99" |
| **Image field** | Always `999` in mixed records |
| **Binary image in** | Type-4, Type-10, Type-13, Type-14, Type-15, Type-17 |

---

**You now have everything you need to start building your ANSI/NIST-ITL parser!**

Refer to `TECHNICAL_SPECIFICATION.md` for detailed technical information.
