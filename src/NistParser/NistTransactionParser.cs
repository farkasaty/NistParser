using NistParser.Constants;
using NistParser.Core;
using NistParser.Exceptions;
using NistParser.Records;
using NistParser.Utilities;
using System.Text;

namespace NistParser;

/// <summary>
/// Main parser for ANSI/NIST-ITL transaction files.
/// Parses biometric data interchange files into structured NistTransaction objects.
/// </summary>
public class NistTransactionParser
{
    /// <summary>
    /// Parses an ANSI/NIST-ITL file from a byte array
    /// </summary>
    /// <param name="fileData">The raw file data</param>
    /// <returns>The parsed transaction</returns>
    /// <exception cref="MissingType1Exception">Thrown if Type-1 record is missing</exception>
    /// <exception cref="NistParserException">Thrown for other parsing errors</exception>
    public static NistTransaction Parse(byte[] fileData)
    {
        if (fileData == null || fileData.Length == 0)
        {
            throw new NistParserException("File data is null or empty");
        }

        var transaction = new NistTransaction
        {
            RawData = fileData
        };

        // Find and parse Type-1 record (mandatory)
        int position = 0;
        var type1Data = ExtractType1Record(fileData, ref position);
        if (type1Data == null)
        {
            throw new MissingType1Exception("Type-1 record not found at beginning of file");
        }

        transaction.Header = ParseType1Record(type1Data);

        // Parse remaining records based on Type-1 CNT field
        if (transaction.Header.Content != null)
        {
            foreach (var entry in transaction.Header.Content.Records)
            {
                if (position >= fileData.Length)
                {
                    throw new TruncatedFileException(
                        $"File ended prematurely. Expected Type-{entry.RecordType} record but reached end of file");
                }

                var record = ParseNextRecord(fileData, ref position, entry.RecordType, entry.IDC);
                if (record != null)
                {
                    transaction.Records.Add(record);
                }
            }
        }

        return transaction;
    }

    /// <summary>
    /// Parses an ANSI/NIST-ITL file from a file path
    /// </summary>
    /// <param name="filePath">Path to the NIST file</param>
    /// <returns>The parsed transaction</returns>
    public static NistTransaction ParseFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"NIST file not found: {filePath}", filePath);
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        return Parse(fileData);
    }

    /// <summary>
    /// Extracts the Type-1 record from the beginning of the file
    /// </summary>
    /// <param name="fileData">The file data</param>
    /// <param name="position">Current position in file (updated to end of Type-1)</param>
    /// <returns>Type-1 record data, or null if not found</returns>
    private static byte[]? ExtractType1Record(byte[] fileData, ref int position)
    {
        // Type-1 record should start with "1.001:"
        string header = Encoding.ASCII.GetString(fileData, 0, Math.Min(6, fileData.Length));
        if (!header.StartsWith("1.001:"))
        {
            return null;
        }

        // Find the FS (File Separator) that terminates the Type-1 record
        int fsIndex = Array.IndexOf(fileData, SeparatorConstants.FS, position);
        if (fsIndex == -1)
        {
            throw new NistParserException("Type-1 record not properly terminated with FS separator");
        }

        // Extract Type-1 data (including the FS terminator)
        int length = fsIndex - position + 1;
        byte[] type1Data = new byte[length];
        Array.Copy(fileData, position, type1Data, 0, length);

        position = fsIndex + 1; // Move past the FS

        return type1Data;
    }

    /// <summary>
    /// Parses Type-1 Transaction Information record
    /// </summary>
    /// <param name="recordData">The Type-1 record data</param>
    /// <returns>Parsed Type-1 record</returns>
    private static Type1Record ParseType1Record(byte[] recordData)
    {
        var record = new Type1Record();

        // Split on GS (Group Separator) to get individual fields
        // Note: Last separator is FS, not GS
        var fields = SplitRecord(recordData, SeparatorConstants.GS, SeparatorConstants.FS);

        foreach (var fieldData in fields)
        {
            if (fieldData.Length == 0)
                continue;

            var field = FieldParser.ParseTaggedField(fieldData);
            record.AddField(field);

            // Special handling for field 1.003 (Transaction Content / CNT)
            if (field.FieldNumber == "1.003")
            {
                record.Content = ParseTransactionContent(field);
            }
        }

        // Extract record length from field 1.001
        if (int.TryParse(record.GetFieldValue("1.001"), out int length))
        {
            record.RecordLength = length;
        }

        // Extract IDC from field 1.002 (though Type-1 is always "00")
        record.IDC = "00";

        return record;
    }

    /// <summary>
    /// Parses the Transaction Content field (1.003 / CNT)
    /// Format: record_count&lt;US&gt;record_type&lt;US&gt;IDC&lt;RS&gt;record_type&lt;US&gt;IDC&lt;RS&gt;...
    /// </summary>
    /// <param name="cntField">The CNT field</param>
    /// <returns>Parsed transaction content</returns>
    private static TransactionContent ParseTransactionContent(NistField cntField)
    {
        var content = new TransactionContent();

        if (cntField.Subfields.Count == 0)
        {
            return content;
        }

        // First subfield contains the record count
        var firstSubfield = cntField.Subfields[0];
        if (firstSubfield.Items.Count > 0 && int.TryParse(firstSubfield.Items[0], out int count))
        {
            content.RecordCount = count;
        }

        // Subsequent subfields contain record type and IDC pairs
        for (int i = 1; i < cntField.Subfields.Count; i++)
        {
            var subfield = cntField.Subfields[i];
            if (subfield.Items.Count >= 2 &&
                int.TryParse(subfield.Items[0], out int recordType))
            {
                string idc = subfield.Items[1];
                content.Records.Add(new TransactionContent.RecordEntry
                {
                    RecordType = recordType,
                    IDC = idc
                });
            }
        }

        return content;
    }

    /// <summary>
    /// Parses the next record in the file
    /// </summary>
    /// <param name="fileData">The file data</param>
    /// <param name="position">Current position (updated)</param>
    /// <param name="expectedRecordType">Expected record type number</param>
    /// <param name="expectedIDC">Expected IDC</param>
    /// <returns>Parsed record</returns>
    private static NistRecord? ParseNextRecord(byte[] fileData, ref int position,
        int expectedRecordType, string expectedIDC)
    {
        if (position >= fileData.Length)
        {
            return null;
        }

        // Determine record encoding type
        RecordType recordType = (RecordType)expectedRecordType;
        RecordEncoding encoding = RecordEncodingExtensions.GetEncoding(recordType);

        NistRecord? record = null;

        switch (encoding)
        {
            case RecordEncoding.TaggedASCII:
                record = ParseTaggedRecord(fileData, ref position, recordType);
                break;

            case RecordEncoding.Binary:
                record = ParseBinaryRecord(fileData, ref position, recordType);
                break;

            case RecordEncoding.Mixed:
                record = ParseMixedRecord(fileData, ref position, recordType);
                break;
        }

        if (record != null)
        {
            record.IDC = expectedIDC;
        }

        return record;
    }

    /// <summary>
    /// Parses a tagged ASCII record (Type-1, Type-2, Type-9, etc.)
    /// </summary>
    private static NistRecord ParseTaggedRecord(byte[] fileData, ref int position, RecordType recordType)
    {
        // Find the FS terminator
        int fsIndex = Array.IndexOf(fileData, SeparatorConstants.FS, position);
        if (fsIndex == -1)
        {
            throw new TruncatedFileException($"Type-{(int)recordType} record not properly terminated");
        }

        // Extract record data
        int length = fsIndex - position + 1;
        byte[] recordData = new byte[length];
        Array.Copy(fileData, position, recordData, 0, length);

        // Create record based on type
        NistRecord record = CreateRecord(recordType);

        // Parse fields
        var fields = SplitRecord(recordData, SeparatorConstants.GS, SeparatorConstants.FS);
        foreach (var fieldData in fields)
        {
            if (fieldData.Length == 0)
                continue;

            var field = FieldParser.ParseTaggedField(fieldData);
            record.AddField(field);
        }

        // Extract record length and IDC
        if (int.TryParse(record.GetFieldValue($"{(int)recordType}.001"), out int recordLength))
        {
            record.RecordLength = recordLength;
        }

        string? idc = record.GetFieldValue($"{(int)recordType}.002");
        if (!string.IsNullOrEmpty(idc))
        {
            record.IDC = idc;
        }

        position = fsIndex + 1;
        return record;
    }

    /// <summary>
    /// Parses a mixed record (tagged fields + binary field 999)
    /// </summary>
    private static NistRecord ParseMixedRecord(byte[] fileData, ref int position, RecordType recordType)
    {
        NistRecord record = CreateRecord(recordType);

        // Mixed records have tagged fields followed by field 999 with binary data
        // We need to parse tagged fields until we find field 999

        int startPosition = position;

        // Find all GS separators to parse tagged fields
        List<byte[]> fields = new List<byte[]>();
        int currentPos = position;
        int fieldStart = position;

        while (currentPos < fileData.Length)
        {
            if (fileData[currentPos] == SeparatorConstants.GS ||
                fileData[currentPos] == SeparatorConstants.FS)
            {
                // Extract field
                int fieldLength = currentPos - fieldStart;
                if (fieldLength > 0)
                {
                    byte[] fieldData = new byte[fieldLength];
                    Array.Copy(fileData, fieldStart, fieldData, 0, fieldLength);
                    fields.Add(fieldData);

                    // Check if this is field 999 (image data field)
                    string? fieldNumber = FieldParser.ExtractFieldNumber(fieldData);
                    if (fieldNumber != null && fieldNumber.EndsWith(".999"))
                    {
                        // Field 999 found - parse it as binary
                        var field = ParseField999(fieldData);
                        record.AddField(field);

                        // Move to next separator and exit
                        currentPos++;
                        if (fileData[currentPos - 1] == SeparatorConstants.FS)
                        {
                            position = currentPos;
                            return record;
                        }
                        fieldStart = currentPos;
                        continue;
                    }
                }

                // Check if we hit FS (end of record)
                if (fileData[currentPos] == SeparatorConstants.FS)
                {
                    position = currentPos + 1;
                    break;
                }

                fieldStart = currentPos + 1;
            }

            currentPos++;
        }

        // Parse all non-999 tagged fields
        foreach (var fieldData in fields)
        {
            if (fieldData.Length == 0)
                continue;

            string? fieldNumber = FieldParser.ExtractFieldNumber(fieldData);
            if (fieldNumber != null && !fieldNumber.EndsWith(".999"))
            {
                var field = FieldParser.ParseTaggedField(fieldData);
                record.AddField(field);
            }
        }

        // Extract record length and IDC
        if (int.TryParse(record.GetFieldValue($"{(int)recordType}.001"), out int recordLength))
        {
            record.RecordLength = recordLength;
        }

        string? idc = record.GetFieldValue($"{(int)recordType}.002");
        if (!string.IsNullOrEmpty(idc))
        {
            record.IDC = idc;
        }

        return record;
    }

    /// <summary>
    /// Parses field 999 (binary image data field)
    /// </summary>
    private static NistField ParseField999(byte[] fieldData)
    {
        int colonIndex = Array.IndexOf(fieldData, SeparatorConstants.COLON);
        if (colonIndex == -1)
        {
            throw new InvalidFieldFormatException("999", "No colon separator found");
        }

        string fieldNumber = Encoding.ASCII.GetString(fieldData, 0, colonIndex);

        // Everything after the colon is binary image data
        int dataLength = fieldData.Length - colonIndex - 1;
        byte[] imageData = new byte[dataLength];
        Array.Copy(fieldData, colonIndex + 1, imageData, 0, dataLength);

        var field = new NistField(fieldNumber)
        {
            BinaryData = imageData
        };

        return field;
    }

    /// <summary>
    /// Parses a pure binary record (Type-3 through Type-8)
    /// </summary>
    private static NistRecord ParseBinaryRecord(byte[] fileData, ref int position, RecordType recordType)
    {
        // Binary records have fixed-length fields
        // First 4 bytes are the record length (binary)
        if (position + 4 > fileData.Length)
        {
            throw new TruncatedFileException($"Not enough data for Type-{(int)recordType} record length");
        }

        int recordLength = BitConverter.ToInt32(fileData, position);
        if (position + recordLength > fileData.Length)
        {
            throw new TruncatedFileException(
                $"Type-{(int)recordType} record length ({recordLength}) exceeds available data");
        }

        // For now, create a generic record and store the raw binary data
        // A full implementation would parse the fixed-length fields based on record type
        var record = CreateRecord(recordType);
        record.RecordLength = recordLength;

        // Store the entire record as raw data
        byte[] recordData = new byte[recordLength];
        Array.Copy(fileData, position, recordData, 0, recordLength);

        // Create a field to hold the binary data
        var field = new NistField($"{(int)recordType}.999")
        {
            BinaryData = recordData
        };
        record.AddField(field);

        position += recordLength;
        return record;
    }

    /// <summary>
    /// Splits a record into fields based on field and record separators
    /// </summary>
    private static List<byte[]> SplitRecord(byte[] recordData, byte fieldSeparator, byte recordSeparator)
    {
        var fields = new List<byte[]>();
        int start = 0;

        for (int i = 0; i < recordData.Length; i++)
        {
            if (recordData[i] == fieldSeparator || recordData[i] == recordSeparator)
            {
                if (i > start)
                {
                    int length = i - start;
                    byte[] field = new byte[length];
                    Array.Copy(recordData, start, field, 0, length);
                    fields.Add(field);
                }

                start = i + 1;
            }
        }

        return fields;
    }

    /// <summary>
    /// Creates a record object based on record type
    /// </summary>
    private static NistRecord CreateRecord(RecordType recordType)
    {
        return recordType switch
        {
            RecordType.Type1_TransactionInformation => new Type1Record(),
            RecordType.Type2_UserDefinedText => new Type2Record(),
            RecordType.Type14_FingerprintImage => new Type14Record(),
            _ => new GenericNistRecord(recordType)
        };
    }
}
