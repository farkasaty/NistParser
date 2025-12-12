using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Records;
using NistParser.Utilities;

namespace NistParser.Writing
{

    /// <summary>
    /// Writes NIST transactions in Traditional binary format (ANSI/NIST-ITL)
    /// </summary>
    public static class TraditionalFormatWriter
    {
        /// <summary>
        /// Writes a NistTransaction to Traditional binary format
        /// </summary>
        public static byte[] Write(NistTransaction transaction)
        {
            if (transaction.Header == null)
            {
                throw new InvalidOperationException("Transaction must have a Type-1 header record");
            }

            using var ms = new MemoryStream();

            // Update Type-1 CNT field before writing
            UpdateTransactionContent(transaction);

            // Write Type-1 record
            WriteRecord(ms, transaction.Header);

            // Write all other records
            foreach (var record in transaction.Records)
            {
                WriteRecord(ms, record);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Updates the Type-1 CNT field (1.003) to reflect current transaction content
        /// </summary>
        private static void UpdateTransactionContent(NistTransaction transaction)
        {
            // Count records by type and build CNT field
            var recordCounts = new Dictionary<int, List<string>>();

            foreach (var record in transaction.Records)
            {
                int recordType = (int)record.RecordType;
                if (!recordCounts.ContainsKey(recordType))
                {
                    recordCounts[recordType] = new List<string>();
                }
                recordCounts[recordType].Add(record.IDC);
            }

            // Build CNT field structure
            // Format: record_count<US>type_1<US>idc_1<RS>type_2<US>idc_2<RS>...
            // This means:
            //   Subfield[0]: count, type_1, idc_1 (3 items separated by US)
            //   Subfield[1]: type_2, idc_2 (2 items separated by US)
            //   etc.

            int totalRecords = transaction.Records.Count;

            // Get or create the CNT field
            var cntField = transaction.Header.GetField("1.003");
            if (cntField == null)
            {
                cntField = new NistField("1.003");
                transaction.Header.AddField(cntField);
            }

            // Clear existing subfields
            cntField.Subfields.Clear();

            // Build subfields properly
            var allRecordPairs = recordCounts
                .OrderBy(k => k.Key)
                .SelectMany(kvp => kvp.Value.Select(idc => new { RecordType = kvp.Key, IDC = idc }))
                .ToList();

            // First subfield: FRC (1) + CRC (count)
            var firstSubfield = new NistSubfield();
            firstSubfield.Items.Add("1"); // FRC is always 1
            firstSubfield.Items.Add(totalRecords.ToString()); // CRC
            cntField.Subfields.Add(firstSubfield);

            // Remaining subfields: record type + IDC
            foreach (var pair in allRecordPairs)
            {
                var subfield = new NistSubfield();
                subfield.Items.Add(pair.RecordType.ToString());
                // Ensure IDC is 2 digits (e.g., "0" -> "00")
                subfield.Items.Add(pair.IDC);
                cntField.Subfields.Add(subfield);
            }

            // Also update the TransactionContent property
            transaction.Header.Content = new TransactionContent
            {
                RecordCount = totalRecords,
                Records = recordCounts
                .SelectMany(kvp => kvp.Value.Select(idc => new TransactionContent.RecordEntry
                {
                    RecordType = kvp.Key,
                    IDC = idc
                }))
                .ToList()
            };
        }

        /// <summary>
        /// Writes a single record to the stream
        /// </summary>
        private static void WriteRecord(MemoryStream ms, NistRecord record)
        {
            using var recordStream = new MemoryStream();

            // Determine if this is a binary record (has field 999)
            bool hasBinaryData = record.HasField($"{(int)record.RecordType}.999");

            // Get sorted field numbers (excluding LEN which is field 001)
            var fieldNumbers = record.Fields.Keys
            .Where(fn => !fn.EndsWith(".001")) // Exclude LEN field, we'll calculate and add it first
            .OrderBy(ParseFieldNumber)
            .ToList();

            // Also get unknown fields and sort them
            var unknownFieldNumbers = record.UnknownFields.Keys
            .Where(fn => !fn.EndsWith(".001")) // Exclude LEN field
            .OrderBy(ParseFieldNumber)
            .ToList();

            // Write each field (we'll write LEN field first after calculating)
            var fieldData = new List<byte[]>();

            for (int i = 0; i < fieldNumbers.Count; i++)
            {
                var fieldNumber = fieldNumbers[i];
                var field = record.GetField(fieldNumber);
                if (field != null)
                {
                    // Check if this is the last non-binary field
                    bool isLastField = (i == fieldNumbers.Count - 1) && unknownFieldNumbers.Count == 0 && !hasBinaryData;
                    var data = WriteField(field, hasBinaryData, isLastField);
                    fieldData.Add(data);
                }
            }

            // Write unknown fields
            // Parse the string value to preserve any separator characters (US/RS) as structure
            for (int i = 0; i < unknownFieldNumbers.Count; i++)
            {
                var fieldNumber = unknownFieldNumbers[i];
                var value = record.GetUnknownField(fieldNumber);
                if (value != null)
                {
                    // Create a NistField and parse the value to preserve separators as structure
                    var tempField = new NistField(fieldNumber);

                    // Parse the value string as field data to preserve US/RS separator structure
                    var valueBytes = Encoding.UTF8.GetBytes(value);
                    tempField.Subfields = FieldParser.ParseFieldData(valueBytes);

                    // Check if this is the last non-binary field
                    bool isLastField = (i == unknownFieldNumbers.Count - 1) && !hasBinaryData;
                    var data = WriteField(tempField, hasBinaryData, isLastField);
                    fieldData.Add(data);
                }
            }

            // Calculate total record length
            int recordLength = CalculateRecordLength(record, fieldData, hasBinaryData);

            // Update the LEN field
            record.RecordLength = recordLength;
            var lenFieldNumber = $"{(int)record.RecordType}.001";
            record.UpdateField(lenFieldNumber, recordLength.ToString());

            // Write LEN field first - create it if it doesn't exist (defensive programming)
            var lenField = record.GetField(lenFieldNumber);
            if (lenField == null)
            {
                // Create new LEN field
                lenField = new NistField(lenFieldNumber);
                lenField.SetValue(recordLength.ToString());
                record.AddField(lenField);
            }

            bool isLenOnlyField = fieldData.Count == 0 && !hasBinaryData;
            var lenData = WriteField(lenField, hasBinaryData, isLenOnlyField);
            recordStream.Write(lenData, 0, lenData.Length);

            // Write all other fields
            foreach (var data in fieldData)
            {
                recordStream.Write(data, 0, data.Length);
            }

            // Write FS separator after record (except for the last record, but we'll add it anyway for consistency)
            recordStream.WriteByte(SeparatorConstants.FS);

            // Write to main stream
            var recordBytes = recordStream.ToArray();
            ms.Write(recordBytes, 0, recordBytes.Length);
        }

        /// <summary>
        /// Calculates the total length of a record including all fields
        /// </summary>
        private static int CalculateRecordLength(NistRecord record, List<byte[]> fieldData, bool hasBinaryData)
        {
            // Start with LEN field itself
            var lenFieldNumber = $"{(int)record.RecordType}.001";
            
            // Check if LEN field will have a GS separator
            bool isLenOnlyField = fieldData.Count == 0 && !hasBinaryData;
            int gsSize = isLenOnlyField ? 0 : 1;

            // Calculate payload length (all fields + FS)
            int payloadLength = 1; // Start with FS separator
            foreach (var data in fieldData)
            {
                payloadLength += data.Length;
            }

            // Iteratively calculate total length until stable
            // Initial guess using current payload length
            int totalLength = payloadLength + lenFieldNumber.Length + 1 + payloadLength.ToString().Length + gsSize;
            
            // Max iterations to prevent infinite loop (though unlikely)
            for (int i = 0; i < 5; i++)
            {
                string lenValue = totalLength.ToString();
                int currentLenFieldSize = lenFieldNumber.Length + 1 + lenValue.Length + gsSize; // field_num + : + length_value + GS
                
                int newTotalLength = payloadLength + currentLenFieldSize;
                
                if (newTotalLength == totalLength)
                {
                    return totalLength;
                }
                
                totalLength = newTotalLength;
            }

            // Fallback (should ideally not be reached)
            return totalLength;
        }

        /// <summary>
        /// Writes a single field
        /// </summary>
        private static byte[] WriteField(NistField field, bool isBinaryRecord, bool isLastField)
        {
            using var ms = new MemoryStream();

            // Write field number
            var fieldNumBytes = Encoding.UTF8.GetBytes(field.FieldNumber);
            ms.Write(fieldNumBytes, 0, fieldNumBytes.Length);

            // Write colon separator
            ms.WriteByte(SeparatorConstants.COLON);

            // Check if this is the binary data field (999)
            if (field.IsBinary && field.BinaryData != null)
            {
                ms.Write(field.BinaryData, 0, field.BinaryData.Length);
            }
            else
            {
                // Write subfields
                for (int i = 0; i < field.Subfields.Count; i++)
                {
                    if (i > 0)
                    {
                        ms.WriteByte(SeparatorConstants.RS); // Separate subfields
                    }

                    var subfield = field.Subfields[i];
                    for (int j = 0; j < subfield.Items.Count; j++)
                    {
                        if (j > 0)
                        {
                            ms.WriteByte(SeparatorConstants.US); // Separate items
                        }

                        var itemBytes = Encoding.UTF8.GetBytes(subfield.Items[j]);
                        ms.Write(itemBytes, 0, itemBytes.Length);
                    }
                }
            }

            // Write GS separator (except for binary field 999 or if this is the last field in the record)
            if (!field.FieldNumber.EndsWith(".999") && !isLastField)
            {
                ms.WriteByte(SeparatorConstants.GS);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Parses a field number to get the field portion for sorting
        /// </summary>
        private static int ParseFieldNumber(string fieldNumber)
        {
            var parts = fieldNumber.Split('.');
            if (parts.Length == 2 && int.TryParse(parts[1], out int fieldNum))
            {
                return fieldNum;
            }
            return 0;
        }
    }
}
