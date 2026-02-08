using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NistParser.Constants;
using NistParser.Utilities;

namespace NistParser.Editing
{
    /// <summary>
    /// Provides validation for NIST files before parsing.
    /// Checks for structural integrity, field lengths, and content consistency.
    /// </summary>
    public class NistFileValidator
    {
        /// <summary>
        /// Validates NIST file data for structural integrity before parsing.
        /// Checks for correct separators, field lengths, and content consistency.
        /// </summary>
        /// <param name="fileData">The raw NIST file bytes to validate</param>
        /// <returns>A ValidationResults object containing any errors or warnings found</returns>
        public ValidationResults Validate(byte[] fileData)
        {
            var results = new ValidationResults();

            if (fileData == null || fileData.Length == 0)
            {
                results.Add(ValidationResult.Fail("File data is empty or null"));
                return results;
            }

            if (IsXml(fileData))
            {
                results.Add(ValidationResult.Fail("XML validation is not currently supported by NistFileValidator."));
                return results;
            }

            int position = 0;
            
            // 1. Validate Type-1 Record
             var (cntContent, type1Length) = ValidateType1(fileData, ref position, results);

            if (type1Length > 0 && type1Length != position)
            {
                 // position is now start of Type-2 (or end of Type-1 FS)
                 // If the parsed length (1.001) doesn't frame the actual found FS location, warn.
                 // Actually 1.001 is the length of the record in bytes.
                 // If we found FS at 'position-1', then actual length is 'position'.
                 if (type1Length != position) {
                     results.Add(ValidationResult.Fail($"Type-1 LEN field value ({type1Length}) does not match actual record size ({position})", "1.001"));
                 }
            }

            // 2. Validate Subsequent Records based on CNT
            if (cntContent != null)
            {
               foreach (var expectedRecord in cntContent)
               {
                   ValidateNextRecord(fileData, ref position, expectedRecord.RecordType, expectedRecord.IDC, results);
               }
            }
            else
            {
                 // If we failed to get CNT, we can try to blindly walk
                 results.Add(ValidationResult.Fail("Could not proceed with record validation due to missing Type-1 Content (1.003)"));
            }

             // Check if we are at EOF
             if (position < fileData.Length)
             {
                 results.Add(ValidationResult.Fail($"File has trailing data. Parsed {position} bytes, but file is {fileData.Length} bytes.", "EOF"));
             }
             else if (position > fileData.Length)
             {
                 // Should be caught earlier
                  results.Add(ValidationResult.Fail("Parsing went past end of file."));
             }

            return results;
        }

        private bool IsXml(byte[] fileData)
        {
             if (fileData.Length < 5) return false;
             string header = Encoding.UTF8.GetString(fileData, 0, Math.Min(200, fileData.Length)).TrimStart();
             return header.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
                    header.Contains("<itl:NISTBiometricInformationExchangePackage");
        }

        // Returns (Parsed Content records, Parsed LEN value)
        private (List<(int RecordType, string IDC)>?, int) ValidateType1(byte[] fileData, ref int position, ValidationResults results)
        {
            int startPos = position;
            
            // Must start with 1.
            if (position + 10 > fileData.Length) 
            {
                results.Add(ValidationResult.Fail("File too short for Type-1 header"));
                return (null, 0);
            }
            string header = Encoding.ASCII.GetString(fileData, position, 10);
            if (!header.StartsWith("1."))
            {
                 results.Add(ValidationResult.Fail("Type-1 record does not start with identifier '1.'", "1.001"));
            }

            int fsIndex = Array.IndexOf(fileData, SeparatorConstants.FS, position);
            if (fsIndex == -1)
            {
                 results.Add(ValidationResult.Fail("Type-1 record not terminated with FS separator"));
                 position = fileData.Length; // Fail safe
                 return (null, 0);
            }

            int actualLen = fsIndex - position + 1;
            
            // Extract fields strictly
            byte[] recordData = new byte[actualLen];
            Array.Copy(fileData, position, recordData, 0, actualLen);
            
            // Update position for caller
            position = fsIndex + 1; 

            return ValidateType1Fields(recordData, results);
        }

        private (List<(int, string)>?, int) ValidateType1Fields(byte[] data, ValidationResults results)
        {
            var separated = SplitFields(data);
            int lenFunc = 0;
            List<(int, string)>? content = null;

            bool foundLen = false;
            bool foundCnt = false;

            foreach(var fieldBytes in separated)
            {
                // Parse "X.YYY:data"
                // Find colon
                int colonIdx = Array.IndexOf(fieldBytes, SeparatorConstants.COLON);
                if (colonIdx == -1) 
                {
                     // Potentially valid if empty? NIST fields usually must have number.
                     // ignoring for now or reporting?
                     continue;
                }

                string tag = Encoding.ASCII.GetString(fieldBytes, 0, colonIdx);
                // Validate tag format
                if (!Utilities.FieldNumberFormatter.IsValid(tag))
                {
                    results.Add(ValidationResult.Fail($"Invalid field tag format: {tag}", tag));
                    continue;
                }
                
                string tagNorm = Utilities.FieldNumberFormatter.Normalize(tag); // e.g. "1.001"
                string value = Encoding.UTF8.GetString(fieldBytes, colonIdx + 1, fieldBytes.Length - colonIdx - 1); // rough decode

                if (Utilities.FieldNumberFormatter.AreEquivalent(tagNorm, "1.001"))
                {
                    foundLen = true;
                    if (int.TryParse(value, out int l))
                    {
                        lenFunc = l;
                    } 
                    else
                    {
                        results.Add(ValidationResult.Fail($"Invalid LEN value in Type-1: {value}", "1.001"));
                    }
                }
                else if (Utilities.FieldNumberFormatter.AreEquivalent(tagNorm, "1.003"))
                {
                    foundCnt = true;
                    content = ParseCntField(value, results);
                }
            }

            if (!foundLen) results.Add(ValidationResult.Fail("Missing mandatory field 1.001 (LEN)", "1.001"));
            if (!foundCnt) results.Add(ValidationResult.Fail("Missing mandatory field 1.003 (CNT)", "1.003"));

            return (content, lenFunc);
        }

        private List<(int, string)>? ParseCntField(string value, ValidationResults results)
        {
            // CNT format: "1.003:1<US>3<US>00<RS>2<US>00<GS>" - technically fields separators handled outside
            // raw string value: "1<US>3<US>00<RS>2<US>00"
            // Actually Subfields are Separated by RS (US in NIST terms often maps to RS in parser logic?? No.
            // Let's check Constants: US=0x1F, RS=0x1E, GS=0x1D, FS=0x1C
            // Hierarchy: File(FS) -> Record(GS) -> Field(RS) -> SubField(US) -> Item.
            // Wait. NistParser defines:
            // TransactionContent Parse: "SplitRecord(recordData, GS, FS)" -> returns fields.
            // Field 1.003: "Split(RS) -> subfields"?
            // NistField.Subfields usually split by RS (0x1E).
            // Items split by US (0x1F)?
            
            // Standard:
            // 1.003 (CNT): 1<US>Count <RS> Type <US> IDC <RS> Type <US> IDC ...
            // Subfield 1: 1 <US> CRC
            // Subfield 2..N: RecordType <US> IDC

            // Check how `value` is formed. In `ValidateType1Fields`, I split by GS/FS. 
            // So `value` here does NOT contain GS or FS. It contains RS and US.
            
            var list = new List<(int, string)>();
            
            // Split by RS (Subfields)
            var subfields = value.Split(new char[] { (char)SeparatorConstants.RS }, StringSplitOptions.None);
            
            if (subfields.Length == 0)
            {
                results.Add(ValidationResult.Fail("Empty CNT field", "1.003"));
                return null;
            }

            // First subfield: "1<US>Count"
            var firstParts = subfields[0].Split((char)SeparatorConstants.US);
            if (firstParts.Length < 2)
            {
                // results.Add(ValidationResult.Fail("CNT field missing record count", "1.003"));
                // Strict validation might fail here.
                // But let's look at Content records.
            }
            
            // Iterate subfields for contents
            // Skip index 0 if it is the header tuple
            // Standard 2011: 1st subfield = "1<US>CRC" (File Content Code <US> Content Record Count)

            // Remaining subfields: "Type<US>IDC"
            for (int i = 1; i < subfields.Length; i++)
            {
                 var box = subfields[i].Split((char)SeparatorConstants.US);
                 if (box.Length >= 2)
                 {
                     if (int.TryParse(box[0], out int type))
                     {
                         list.Add((type, box[1]));
                     }
                     else
                     {
                         results.Add(ValidationResult.Fail($"Invalid Record Type in CNT at index {i}: {box[0]}", "1.003"));
                     }
                 }
                 else if (box.Length == 1 && int.TryParse(box[0], out int type))
                 {
                      list.Add((type, "00")); // Default IDC
                 }
            }
            
            return list;
        }

        private void ValidateNextRecord(byte[] fileData, ref int position, int recordType, string expectedIdc, ValidationResults results)
        {
             if (position >= fileData.Length)
             {
                 results.Add(ValidationResult.Fail($"Unexpected End Of File. Expected Record Type-{recordType} IDC-{expectedIdc}"));
                 return;
             }

             // Determine encoding
             // Type-4, 7, 8 (?) -> Binary often.
             // Type-2, 9, 10, 13, 14, etc -> Tagged (mostly). 
             // But actually Type-14, 10, etc are "Mixed" (Tagged + field 999 binary).
             
             // We can check NistParser.RecordEncoding Extensions if available, or just heuristic.
             // Access `RecordEncodingExtensions`
             
             RecordEncoding encoding;
             RecordType rType = (RecordType)recordType;
             try
             {
                 encoding = RecordEncodingExtensions.GetEncoding(rType);
             }
             catch
             {
                 // Unknown record type or not mapped in encoding
                 // Default to Tagged? Or Mixed? Or just fail?
                 // If we don't know the encoding, we can't parse it reliably.
                 // But generic records are often Tagged.
                 // Let's assume Tagged if unknown, but log a warning/error?
                 // Or better, fail:
                  results.Add(ValidationResult.Fail($"Unknown Record Type: {recordType}"));
                  return;
             }
             
             switch (encoding)
             {
                 case RecordEncoding.Binary: // Type-4, 7, 8
                     ValidateBinaryRecord(fileData, ref position, recordType, results);
                     break;
                 case RecordEncoding.Mixed: // Type-14, etc
                     ValidateMixedRecord(fileData, ref position, recordType, expectedIdc, results);
                     break;
                 default: // Tagged (Type-2, 9, etc)
                     ValidateTaggedRecord(fileData, ref position, recordType, expectedIdc, results);
                     break;
             }
        }

        private void ValidateBinaryRecord(byte[] fileData, ref int position, int type, ValidationResults results)
        {
             if (position + 4 > fileData.Length)
             {
                 results.Add(ValidationResult.Fail($"Truncated Type-{type} record (missing length)", $"{type}.001"));
                 position = fileData.Length;
                 return;
             }
             
             // Read Length (Big Endian)
             int len = ReadInt32BigEndian(fileData, position);
             
             if (position + len > fileData.Length)
             {
                 results.Add(ValidationResult.Fail($"Type-{type} record length ({len}) exceeds file size", $"{type}.001"));
                 position = fileData.Length; 
                 return;
             }
             
             position += len;
        }

        private void ValidateTaggedRecord(byte[] fileData, ref int position, int type, string idc, ValidationResults results)
        {
            // Ends with FS.
            int fsIndex = Array.IndexOf(fileData, SeparatorConstants.FS, position);
            if (fsIndex == -1)
            {
                results.Add(ValidationResult.Fail($"Type-{type} record missing FS terminator"));
                position = fileData.Length;
                return;
            }
            
            int len = fsIndex - position + 1;
            
            // Check LEN field
            CheckRecordLengthField(fileData, position, len, type, results);
            
            // Check IDC matches? (Optional - deep parse required)

            position = fsIndex + 1;
        }

        private void ValidateMixedRecord(byte[] fileData, ref int position, int type, string idc, ValidationResults results)
        {
            // Starts with LEN field.
            // Parse X.001 tag.
            // Find first GS to get LEN field data.
            int gsIndex = Array.IndexOf(fileData, SeparatorConstants.GS, position);
             if (gsIndex == -1)
             {
                 results.Add(ValidationResult.Fail($"Type-{type} record Malformed (no separators)"));
                 position = fileData.Length;
                 return;
             }
             
             // Extract first field
             int fieldLen = gsIndex - position;
             string fieldStr = Encoding.ASCII.GetString(fileData, position, fieldLen);
             // Format: "14.001:1234"
             int colon = fieldStr.IndexOf(':');
             if (colon != -1 && int.TryParse(fieldStr.Substring(colon+1), out int recordLen))
             {
                  if (position + recordLen > fileData.Length)
                  {
                      results.Add(ValidationResult.Fail($"Type-{type} declared length {recordLen} exceeds file end", $"{type}.001"));
                      position = fileData.Length; 
                      return;
                  }
                  
                  // Verify that the record REALLY ends there (should be FS at end-1? No, FS is part of the binary data 999 usually? 
                  // No, 9.999 is data, but record terminator is still FS??
                  // WAIT. NistTransactionParser says:
                  // "Binary data ends before the FS separator (last byte)"
                  // So position + recordLen - 1 should be FS.
                  if (fileData[position + recordLen - 1] != SeparatorConstants.FS)
                  {
                       results.Add(ValidationResult.Fail($"Type-{type} record does not end with FS at expected byte {position + recordLen - 1}"));
                  }
                  
                  position += recordLen;
             }
             else
             {
                 results.Add(ValidationResult.Fail($"Could not parse Length from Type-{type} header: {fieldStr}"));
                 // Try to fallback to finding FS? Unreliable for mixed records (binary data might contain FS)
                 // But typically not. Field 999 is binary.
                 position = fileData.Length; // Abort
             }
        }

        private void CheckRecordLengthField(byte[] fileData, int position, int actualLen, int type, ValidationResults results)
        {
            // Try to find X.001 field in the first chunk
            int gsIndex = Array.IndexOf(fileData, SeparatorConstants.GS, position);
            int searchLim = (gsIndex != -1) ? gsIndex : position + actualLen;
            if (searchLim > position + 20) searchLim = position + 20; // Optimization
            
            string chunk = Encoding.ASCII.GetString(fileData, position, searchLim - position);
            // Looking for "{type}.001:{Val}"
            // Simplified check
            string valStr = "";
            if (chunk.StartsWith($"{type}.001:"))
            {
                valStr = chunk.Substring($"{type}.001:".Length);
            }
            else if (chunk.StartsWith($"{type}.01:")) // legacy
            {
                 valStr = chunk.Substring($"{type}.01:".Length);
            }
            
             if (int.TryParse(valStr, out int declared))
             {
                 if (declared != actualLen)
                 {
                     results.Add(ValidationResult.Fail($"Type-{type} Length Mismatch. Declared: {declared}, Actual: {actualLen}", $"{type}.001"));
                 }
             }
        }

        private List<byte[]> SplitFields(byte[] data)
        {
             var list = new List<byte[]>();
             int start = 0;
             for(int i=0; i<data.Length; i++)
             {
                 if (data[i] == SeparatorConstants.GS || data[i] == SeparatorConstants.FS)
                 {
                     if (i > start)
                     {
                         byte[] f = new byte[i - start];
                         Array.Copy(data, start, f, 0, i - start);
                         list.Add(f);
                     }
                     start = i + 1;
                 }
             }
             return list;
        }

        private static int ReadInt32BigEndian(byte[] data, int offset)
        {
            if (BitConverter.IsLittleEndian)
            {
                return (data[offset] << 24) | (data[offset + 1] << 16) |
                (data[offset + 2] << 8) | data[offset + 3];
            }
            else
            {
                return BitConverter.ToInt32(data, offset);
            }
        }
    }
}
