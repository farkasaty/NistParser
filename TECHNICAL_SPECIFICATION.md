# ANSI/NIST-ITL File Format - Comprehensive Technical Specification

## Document Purpose
This document provides detailed technical specifications for implementing a parser for ANSI/NIST-ITL (American National Standards Institute / National Institute of Standards and Technology - Information Technology Laboratory) biometric data interchange files.

---

## 1. OVERVIEW

### 1.1 Standard Information
- **Full Name**: ANSI/NIST-ITL 1-2011 "Data Format for the Interchange of Fingerprint, Facial & Other Biometric Information"
- **Also Known As**: NIST Special Publication 500-290 (Edition 3)
- **Current Version**: ANSI/NIST-ITL 1-2011 Update:2015
- **Purpose**: Worldwide standard for exchanging biometric and biographic data

### 1.2 Use Cases
- Law enforcement agencies
- Homeland security
- Identity management systems
- Criminal justice administrations
- Automated biometric identification systems (AFIS)
- International data exchange (used by INTERPOL and worldwide)

### 1.3 Supported Biometric Modalities
- Fingerprints
- Facial images
- Iris scans
- Palm prints
- DNA data
- Footprints (plantars)
- Latent prints
- Scars, Marks, and Tattoos (SMT)
- Voice data
- Dental records
- Signatures

---

## 2. FILE STRUCTURE

### 2.1 Transaction Structure
An ANSI/NIST-ITL file is called a **Transaction** and consists of:
- One mandatory **Type-1 record** (Transaction Information)
- Zero or more additional logical records (Type-2 through Type-99)
- All records in a transaction are related and identified by a Transaction Control Number (TCN)

### 2.2 File Organization
```
[Type-1 Record] <FS> [Type-2 Record] <FS> [Type-X Record] <FS> ... [Type-N Record] <FS>
```

### 2.3 Maximum File Size
- Typical implementation limit: 1-4 GB (based on buffer limitations)
- No explicit standard-defined limit, but Field 1.001 (record length) is ASCII numeric

---

## 3. ENCODING TYPES

### 3.1 Traditional Encoding (Binary)
- **Most Common Format**
- Mix of ASCII tagged-field records and binary records
- Used for backward compatibility
- Character encoding: 7-bit ASCII for Type-1 and Type-2 records
- Binary encoding for image data

### 3.2 XML Encoding (NIEM-Conformant)
- Introduced in 2008
- XML structure for the same data
- Not covered in detail in this specification (focus on Traditional Encoding)

---

## 4. SEPARATOR CHARACTERS

### 4.1 ASCII Control Characters
The standard uses four special ASCII control characters as delimiters:

| Character | Name | Decimal | Hex | Usage |
|-----------|------|---------|-----|-------|
| **US** | Unit Separator | 31 | 0x1F | Separates individual information items within a field/subfield |
| **RS** | Record Separator | 30 | 0x1E | Separates repeated subfields within a field |
| **GS** | Group Separator | 29 | 0x1D | Separates fields within a logical record |
| **FS** | File Separator | 28 | 0x1C | Separates logical records in the transaction |

### 4.2 Hierarchical Usage
```
Transaction
├── Record <FS> Record <FS> Record
    ├── Field <GS> Field <GS> Field
        ├── Subfield <RS> Subfield <RS> Subfield
            ├── Item <US> Item <US> Item
```

### 4.3 Important Rules
- **ASCII Text Records**: Separators are meaningful delimiters
- **Binary Records**: Separators are just binary data (no special meaning)
- The FS character terminates each record in ASCII/tagged records
- The FS character is **included** in the record length calculation
- US, RS, GS, FS must remain as 7-bit ASCII without base-64 encoding

---

## 5. RECORD TYPES

### 5.1 Complete Record Type List

| Type | Name | Encoding | Purpose |
|------|------|----------|---------|
| **Type-1** | Transaction Information | ASCII Tagged | **MANDATORY** - Header for entire transaction |
| **Type-2** | User-Defined Descriptive Text | ASCII Tagged | Biographic/descriptive data (user-defined) |
| **Type-3** | Low-Resolution Grayscale Fingerprint | Binary | **DEPRECATED** |
| **Type-4** | High-Resolution Grayscale Fingerprint | Binary | WSQ compressed fingerprints |
| **Type-5** | Low-Resolution Binary Fingerprint | Binary | **DEPRECATED** |
| **Type-6** | High-Resolution Binary Fingerprint | Binary | **DEPRECATED** |
| **Type-7** | User-Defined Image | Binary | Grayscale images |
| **Type-8** | Signature Image | Binary | Digitized signatures |
| **Type-9** | Minutiae Data | ASCII Tagged | Fingerprint minutiae and Extended Feature Sets |
| **Type-10** | Facial/SMT Image | Mixed | Facial images, scars, marks, tattoos |
| **Type-11** | Voice Data | Reserved | Reserved for voice recordings |
| **Type-12** | Dental Records | Reserved | Reserved for dental data |
| **Type-13** | Friction Ridge Latent Image | Mixed | Latent fingerprint images |
| **Type-14** | Fingerprint Image | Mixed | Variable resolution finger images (500/1000 ppi) |
| **Type-15** | Palm Print Image | Mixed | Palm images (500/1000 ppi) |
| **Type-16** | User-Defined Testing Image | Mixed | Testing purposes |
| **Type-17** | Iris Image | Mixed | Iris biometric images |
| **Type-18** | DNA Data | ASCII Tagged | DNA profile information |
| **Type-19** | Plantar (Footprint) Image | Mixed | Footprint images |
| **Type-20** | Source Representation | Mixed | Source representation data |
| **Type-21** | Associated Context | Mixed | Associated context images |
| **Type-22** | Non-Photographic Imagery | Mixed | Non-photographic images |
| **Type-23 to Type-97** | Reserved | - | Reserved for future use |
| **Type-98** | Information Assurance | ASCII Tagged | Digital signatures, hashing |
| **Type-99** | CBEFF Biometric Data | Mixed | Common Biometric Exchange Formats Framework |

### 5.2 Record Encoding Categories

**ASCII Tagged-Field Records:**
- Type-1 (Transaction Information)
- Type-2 (User-Defined Text)
- Type-9 (Minutiae)
- Type-18 (DNA)
- Type-98 (Information Assurance)

**Pure Binary Records:**
- Type-3, Type-4, Type-5, Type-6, Type-8 (legacy fingerprint/signature images)

**Mixed Records (Tagged Fields + Binary Field 999):**
- Type-10, Type-13, Type-14, Type-15, Type-17, Type-19, Type-20, Type-21, Type-22, Type-99
- These contain ASCII tagged fields followed by field 999 containing binary image data

---

## 6. FIELD STRUCTURE

### 6.1 Field Naming Convention
Fields are identified as: **X.YYY**

Where:
- **X** = Record Type number (1-99)
- **YYY** = Field number (can be 1 to 9 digits, typically 3 digits)

Examples:
- `1.001` = Type-1, Field 1 (Logical Record Length)
- `1.002` = Type-1, Field 2 (Version Number)
- `9.010` = Type-9, Field 10
- `17.031` = Type-17, Field 31

**Note**: `1.1` is synonymous with `1.001`, but `1.1` ≠ `1.10`

### 6.2 Tagged Field Format (ASCII Records)
```
X.YYY:data<GS>
```

Where:
- **X** = Record type
- **.** = Period (literal)
- **YYY** = Field number (1-9 digits)
- **:** = Colon (literal)
- **data** = The actual field data
- **<GS>** = Group Separator (0x1D) - or FS for last field in record

### 6.3 Field Structure Examples

**Simple Field (single value):**
```
1.002:0502<GS>
```
- Field 1.002 (Version)
- Value: "0502" (version 5.02)

**Field with Multiple Information Items:**
```
1.003:1<US>2<US>3<GS>
```
- Field 1.003 (Transaction Content)
- Information items: "1", "2", "3" separated by US

**Field with Subfields (Repeating Groups):**
```
9.012:1<US>150<US>200<RS>2<US>180<US>220<RS>3<US>195<US>245<GS>
```
- Field 9.012 (Minutiae)
- Three subfields (three minutiae points)
- Each subfield has three information items (finger number, X coord, Y coord)

### 6.4 Hierarchical Data Structure

```
Field 1.003:1<US>0<RS>2<US>5<GS>
           └─┬─┘  │ └─┬─┘ │ └─┬─┘
             │    │   │   │   │
     Subfield 1 ──┘   │   │   │
       Item 1: "1"    │   │   │
       Item 2: "0" ───┘   │   │
     Subfield 2 ──────────┘   │
       Item 1: "2"            │
       Item 2: "5" ───────────┘
```

---

## 7. TYPE-1 RECORD (MANDATORY)

### 7.1 Purpose
- **ALWAYS REQUIRED** - Every transaction must have exactly one Type-1 record
- Contains metadata about the transaction
- Lists all records included in the file
- Identifies originating agency and transaction control

### 7.2 Mandatory Type-1 Fields

| Field | Name | Description | Format |
|-------|------|-------------|--------|
| **1.001** | LEN (Logical Record Length) | Total length of Type-1 record in bytes | Numeric ASCII |
| **1.002** | VER (Version Number) | Version of ANSI/NIST-ITL standard | "0500" = v5.0, "0502" = v5.02 |
| **1.003** | CNT (File Content) | List of record types in transaction | Complex (see below) |
| **1.004** | TOT (Type of Transaction) | Transaction type code | Alphanumeric (e.g., "CRM") |
| **1.005** | DAT (Date) | Transaction date | "YYYYMMDD" (e.g., "20250717") |
| **1.007** | DAI (Destination Agency ID) | Receiving agency identifier | Alphanumeric |
| **1.008** | ORI (Originating Agency ID) | Sending agency identifier | Alphanumeric |
| **1.009** | TCN (Transaction Control Number) | Unique transaction identifier | Alphanumeric |
| **1.011** | NSR (Native Scanning Resolution) | Scanning resolution | Numeric (ppmm or ppi) |
| **1.012** | NTR (Nominal Transmitting Resolution) | Transmission resolution | Numeric (ppmm or ppi) |

### 7.3 Field 1.003 (CNT) - Transaction Content

**Purpose**: Lists the count and type of all records in the transaction

**Format**:
```
1.003:record_count<US>record_type<US>IDC<RS>record_type<US>IDC<RS>...<GS>
```

**Example**:
```
1.003:1<US>3<RS>2<US>00<RS>9<US>00<RS>13<US>00<GS>
```

Interpretation:
- Total count: 1 (number of records after Type-1)
- Record Type 2, IDC 00
- Record Type 9, IDC 00
- Record Type 13, IDC 00

**Note**: The first information item is a count of the subsequent record entries (not including Type-1 itself)

### 7.4 Information Designation Character (IDC)

- **Purpose**: Links related records in a transaction
- **Format**: Two-digit numeric (00-99)
- Records representing the same subject matter have the same IDC
- Example: Fingerprint image (Type-13) and its minutiae (Type-9) share the same IDC

### 7.5 Character Encoding
- Type-1 record **MUST** use 7-bit ASCII encoding
- All field numbers, colons, and separator characters are ASCII
- Data values are ASCII encoded

---

## 8. TYPE-2 RECORD (USER-DEFINED)

### 8.1 Purpose
- Contains biographic and descriptive text information
- **User-Defined**: Organizations can define their own fields
- Often contains subject demographics, case information, etc.

### 8.2 Common Type-2 Fields (Example - varies by implementation)

| Field | Name | Description |
|-------|------|-------------|
| **2.001** | LEN | Record length in bytes |
| **2.002** | IDC | Information Designation Character |
| **2.004** | Surname | Subject's last name |
| **2.005** | Given Name | Subject's first name |
| **2.007** | Date of Birth | DOB in YYYY-MM-DD format |
| **2.018** | Place of Birth | Birth location |
| **2.024** | Sex | Gender (M/F/U) |

### 8.3 Standardization Through Profiles
- Organizations can subscribe to "Application Profiles"
- Profiles define which Type-2 fields are mandatory/optional
- NIEM (National Information Exchange Model) elements can be used
- Field numbering and meanings can be registered for consistency

---

## 9. BINARY vs TAGGED-FIELD RECORDS

### 9.1 Pure Binary Records (Type-3 through Type-8)

**Structure:**
```
[Field 1: Fixed Length] [Field 2: Fixed Length] ... [Field 999: Variable Length - Image Data]
```

**Characteristics:**
- All fields are binary encoded (not ASCII)
- Field order and size are fixed by the standard
- No field identifiers (e.g., "4.001:") in the data
- Last field is always numbered "999" and contains the image data
- NO separator characters (US, RS, GS, FS) are interpreted as separators
- FS character does NOT terminate the record
- Record length is determined by the first field

**Example Binary Record Structure (Type-4):**
1. Logical record length (4 bytes binary)
2. Image designation character (IDC) (1 byte)
3. Impression type (1 byte)
4. Finger position (6 bytes)
5. Image scanning resolution (1 byte)
6. Horizontal line length (2 bytes)
7. Vertical line length (2 bytes)
8. Compression algorithm (1 byte)
9. Image data (variable length, remainder of record)

### 9.2 Tagged-Field Records (Type-1, Type-2, Type-9, etc.)

**Characteristics:**
- Each field has an identifier: "X.YYY:"
- Variable-length fields
- ASCII encoded
- Separator characters (US, RS, GS, FS) are meaningful delimiters
- FS character terminates the record
- Record length includes all separators and the terminating FS

### 9.3 Mixed Records (Type-10, Type-13, Type-14, Type-15, Type-17, etc.)

**Structure:**
```
X.001:length<GS>
X.002:IDC<GS>
...
X.999:binary_image_data<GS or FS>
```

**Characteristics:**
- Begin with ASCII tagged fields (001-998)
- Field 999 contains binary image data
- Binary data in 999 follows immediately after "999:"
- The last field (999) is terminated by GS or FS
- Combines flexibility of tagged fields with efficiency of binary image storage

**Example Type-14 Structure:**
```
14.001:12345<GS>             (Record length)
14.002:00<GS>                (IDC)
14.003:500<GS>               (Image resolution)
14.004:1<GS>                 (Impression type)
14.005:600<GS>               (Horizontal line length)
14.006:800<GS>               (Vertical line length)
14.007:7<GS>                 (Compression: JPEG 2000)
14.999:[binary image data]<FS>
```

---

## 10. IMAGE COMPRESSION METHODS

### 10.1 Supported Compression Algorithms

| Algorithm | Code | Usage |
|-----------|------|-------|
| Uncompressed | 0 | No compression |
| WSQ v2.0 | 1 | Fingerprints (Type-14) |
| JPEG (Lossy) | 2 | Facial images (Type-10) |
| JPEG (Lossless) | 3 | Facial images (Type-10) |
| JPEG 2000 (Lossy) | 4 | Fingerprints, faces (Type-10, Type-14) |
| JPEG 2000 (Lossless) | 5 | Fingerprints, faces (Type-10, Type-14) |
| PNG | 6 | Lossless images (Type-10) |

### 10.2 Compression by Record Type

**Type-10 (Facial/SMT):**
- JPEG lossy
- JPEG lossless
- JPEG 2000 lossy
- JPEG 2000 lossless
- PNG lossless

**Type-14 (Fingerprints):**
- WSQ (optimized for fingerprints, preserves ridge structure)
- JPEG 2000 lossy
- JPEG 2000 lossless
- Uncompressed

**Type-15 (Palm Prints):**
- Similar to Type-14

### 10.3 WSQ Algorithm
- **Wavelet Scalar Quantization**
- Specifically designed for fingerprint compression
- Preserves ridge structure and minutiae
- Recommended for 500 ppi fingerprint images
- Focuses on retaining lower frequency components (ridge patterns)

---

## 11. RESOLUTION REQUIREMENTS

### 11.1 Units of Measurement
- **ppi** = Pixels Per Inch
- **ppmm** = Pixels Per Millimeter
- **Conversion**: 1000 ppi = 39.37 ppmm

### 11.2 Fingerprint Resolution Standards

| Resolution | ppi | ppmm | Usage |
|------------|-----|------|-------|
| **Standard (Legacy)** | 500 ± 5 | 19.69 ± 0.20 | Traditional fingerprint interchange |
| **Recommended** | 1000 ± 1% | 39.37 ± 1% | Current recommendation |
| **High Resolution** | 2000 ± 1% | 78.74 ± 1% | Advanced applications |

### 11.3 Palm Print Resolution
- **Minimum**: 500 ppi ± 5 ppi (19.69 ppmm ± 0.20 ppmm)
- **Recommended**: 1000 ppi (39.37 ppmm)

### 11.4 Resolution Fields
- **Field 1.011** (NSR): Native Scanning Resolution - Resolution at which image was captured
- **Field 1.012** (NTR): Nominal Transmitting Resolution - Resolution of transmitted image
- Image-specific resolution fields in each image record type

---

## 12. DATA TYPES AND VALIDATION

### 12.1 Common Data Types

| Type | Description | Example |
|------|-------------|---------|
| **Numeric (N)** | Digits 0-9 only | "500", "12345" |
| **Alphanumeric (AN)** | Letters and digits | "CRM", "ABC123" |
| **Date (YYYYMMDD)** | 8-digit date | "20250717" |
| **Date-Time** | ISO format | "2025-07-17T14:30:00Z" |
| **Binary (B)** | Raw binary data | Image bytes, etc. |
| **Decimal (D)** | Decimal number | "39.37" |

### 12.2 Field Cardinality
- **Mandatory (M)**: Field must be present
- **Optional (O)**: Field may be omitted
- **Conditional (C)**: Required based on other fields/conditions

### 12.3 Length Constraints
- Fields have minimum and maximum lengths defined by the standard
- Type-1 fields are typically limited (e.g., 15 characters for ORI)
- User-defined Type-2 fields can have implementation-specific limits

### 12.4 Character Encoding Constraints
- Type-1: **7-bit ASCII only** (0x00-0x7F)
- Type-2: Can support extended character sets (UTF-8 common)
- Binary fields: No encoding constraints

### 12.5 Validation Rules Examples
- **Date Format**: Must match "YYYYMMDD" exactly
- **Version**: "0500", "0502", etc. (4 digits)
- **IDC**: Two-digit numeric "00" through "99"
- **Resolution**: Numeric with tolerance (e.g., 500 ± 5)

---

## 13. PARSING ALGORITHM OVERVIEW

### 13.1 High-Level Parsing Workflow

```
1. Read entire file into buffer
2. Locate first record (Type-1) by finding "1.001:"
3. Parse Type-1 record:
   a. Read field 1.001 to get Type-1 length
   b. Parse Type-1 fields until FS character
   c. Extract field 1.003 (CNT) to get list of records
4. For each subsequent record in CNT:
   a. Determine record type (from CNT)
   b. If ASCII/Tagged record:
      - Parse fields by splitting on GS
      - Extract field number and data from "X.YYY:data"
      - Split subfields on RS
      - Split information items on US
   c. If Binary record:
      - Read fixed-length fields in defined order
      - Read field 999 (image data) as remaining bytes
   d. If Mixed record:
      - Parse tagged fields (001-998) as ASCII
      - Read field 999 as binary data
5. Build structured representation of transaction
6. Validate against standard requirements
```

### 13.2 Record Boundary Detection

**For ASCII Records:**
- Records are separated by FS (0x1C)
- Type-1 field 1.001 contains the record length (including FS)

**For Binary Records:**
- First 4 bytes contain record length
- Use length to skip to next record (no FS separator)

### 13.3 Field Parsing Pattern (ASCII)

```regex
Pattern: (\d+\.\d+):([^\x1D\x1C]+)[\x1D\x1C]
         └─ Field ID ─┘└── Data ──┘└─ Separator ─┘
```

### 13.4 Subfield and Information Item Parsing

```
Given field data: "1\x1F2\x1E3\x1F4"
1. Split on RS (0x1E) → ["1\x1F2", "3\x1F4"]
2. For each subfield, split on US (0x1F):
   - Subfield 1: ["1", "2"]
   - Subfield 2: ["3", "4"]
```

### 13.5 IDC Linking

- Extract IDC from field X.002 in each record
- Group records with the same IDC
- Example: Type-14 (image) with IDC "00" links to Type-9 (minutiae) with IDC "00"

---

## 14. IMPLEMENTATION CONSIDERATIONS

### 14.1 Error Handling

**Critical Errors (Stop Parsing):**
- Missing Type-1 record
- Invalid Type-1 structure
- Corrupted record length fields
- File truncated before all records read

**Non-Critical Errors (Log and Continue):**
- Unknown optional fields
- Non-critical field validation failures
- Missing optional records

### 14.2 Memory Management

**Considerations:**
- Files can be large (hundreds of MB to GB)
- High-resolution images consume significant memory
- Consider streaming parsers for large files
- Parse on-demand rather than loading entire structure

### 14.3 Performance Optimization

**Strategies:**
- Use efficient binary readers for binary records
- Cache parsed Type-1 CNT to avoid re-parsing
- Use byte arrays for image data (avoid string conversion)
- Implement lazy loading for image field 999

### 14.4 Extensibility

**Design for:**
- New record types (Type-23 through Type-97 reserved)
- New compression algorithms
- New field types in existing records
- Version differences (2000, 2007, 2011, future)

### 14.5 Testing Strategy

**Test Data:**
- Obtain sample ANSI/NIST files from NIST
- Test with various record type combinations
- Test binary, ASCII, and mixed records
- Test different compression formats

**Validation:**
- Compare against reference implementations (NBIS, node-nist)
- Use NIST conformance testing tools (BioCTS)
- Validate against known good files

### 14.6 Security Considerations

**Threats:**
- Malformed length fields causing buffer overflows
- Malicious image data exploiting decompression libraries
- Injection attacks via text fields
- Excessive file sizes causing DoS

**Mitigations:**
- Validate all length fields before allocation
- Implement maximum file size limits
- Sanitize text field input
- Use safe image decompression libraries
- Implement timeout mechanisms for parsing

---

## 15. REFERENCE IMPLEMENTATIONS

### 15.1 Open Source Libraries

| Library | Language | Standard Version | Notes |
|---------|----------|------------------|-------|
| **NBIS** | C | ANSI/NIST-ITL 1-2007 | NIST reference implementation |
| **node-nist** | TypeScript/Node.js | 1-2011 (Update 2015) | Low-level encoding/decoding |
| **NIST3** | Python 3 | 1-2013 | Type-1 through Type-99 |
| **python-nistitl** | Python | 1-2011 | Pure Python parser |

### 15.2 Commercial Tools

- **Aware NISTPack**: Full-featured SDK for ANSI/NIST processing
- **Cognaxon NIST Library**: Windows-based library
- Various vendor-specific implementations

### 15.3 NIST Tools

- **BioCTS**: Biometric Conformance Test Software
- **NIST Extractor**: Field and record extraction utility
- **NIST Viewer**: Visual inspection tool

---

## 16. QUICK REFERENCE

### 16.1 Separator Characters

```
US = 0x1F (31 decimal) - Information item separator
RS = 0x1E (30 decimal) - Subfield separator
GS = 0x1D (29 decimal) - Field separator
FS = 0x1C (28 decimal) - Record separator
```

### 16.2 Essential Type-1 Fields

```
1.001 - Logical Record Length (LEN)
1.002 - Version Number (VER) - e.g., "0502"
1.003 - File Content (CNT) - Lists all records
1.004 - Type of Transaction (TOT)
1.005 - Date (DAT) - "YYYYMMDD"
1.007 - Destination Agency ID (DAI)
1.008 - Originating Agency ID (ORI)
1.009 - Transaction Control Number (TCN)
```

### 16.3 Common Record Types

```
Type-1  - Transaction header (MANDATORY)
Type-2  - Biographic data (user-defined)
Type-9  - Minutiae data (ASCII)
Type-10 - Face/SMT images (mixed)
Type-13 - Latent fingerprints (mixed)
Type-14 - Fingerprint images (mixed)
Type-15 - Palm prints (mixed)
Type-17 - Iris images (mixed)
```

### 16.4 Field Structure Syntax

```
Record.Field:Data<Separator>
  │     │    │        │
  │     │    │        └─ GS (between fields) or FS (end of record)
  │     │    └─ Field data (may contain US and RS)
  │     └─ Field number (1-999)
  └─ Record type (1-99)
```

---

## 17. STANDARDS DOCUMENTS

### 17.1 Official Specifications

- **NIST SP 500-290**: "Data Format for the Interchange of Fingerprint, Facial & Other Biometric Information"
- **ANSI/NIST-ITL 1-2011 Update:2015**: Current standard version
- **INTERPOL Implementation**: Subset for international law enforcement

### 17.2 Related Standards

- **ISO/IEC 19794**: Biometric data interchange formats
- **CBEFF (INCITS 398)**: Common Biometric Exchange Formats Framework
- **WSQ Gray-scale Fingerprint Image Compression**: FBI specification

### 17.3 Online Resources

- NIST ITL Image Group: https://www.nist.gov/itl/iad/image-group
- ANSI/NIST-ITL Standard References: https://www.nist.gov/itl/iad/image-group/ansinist-itl-standard-references
- BioCTS Conformance Testing: Available from NIST

---

## 18. GLOSSARY

| Term | Definition |
|------|------------|
| **AFIS** | Automated Fingerprint Identification System |
| **CBEFF** | Common Biometric Exchange Formats Framework |
| **CNT** | File Content (Field 1.003) |
| **DAI** | Destination Agency Identifier (Field 1.007) |
| **EFS** | Extended Feature Set (minutiae markup) |
| **FGP** | Friction Ridge Generalized Position (finger position) |
| **FS/GS/RS/US** | File/Group/Record/Unit Separator characters |
| **IDC** | Information Designation Character (links related records) |
| **ITL** | Information Technology Laboratory (NIST division) |
| **LEN** | Logical Record Length (Field X.001) |
| **NIEM** | National Information Exchange Model |
| **ORI** | Originating Agency Identifier (Field 1.008) |
| **ppi** | Pixels Per Inch (resolution unit) |
| **ppmm** | Pixels Per Millimeter (resolution unit) |
| **SMT** | Scars, Marks, and Tattoos |
| **TCN** | Transaction Control Number (Field 1.009) |
| **TOT** | Type of Transaction (Field 1.004) |
| **VER** | Version Number (Field 1.002) |
| **WSQ** | Wavelet Scalar Quantization (compression for fingerprints) |

---

## 19. REVISION HISTORY

| Date | Version | Notes |
|------|---------|-------|
| 2025-11-11 | 1.0 | Initial comprehensive technical specification |

---

## 20. CONTACT AND SUPPORT

For official standard information:
- **NIST ITL Image Group**: https://www.nist.gov/itl/iad/image-group
- **Email**: ansinist@nist.gov

For implementation questions:
- Consult official NIST publications
- Review open-source implementations
- Contact biometric standards working groups

---

**END OF TECHNICAL SPECIFICATION**
