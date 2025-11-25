using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Records;

namespace NistParser.Writing
{

    /// <summary>
    /// Writes NIST transactions in Traditional binary format (ANSI/NIST-ITL)
    /// </summary>
    internal static class TraditionalFormatWriter
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

            // Build CNT field value
            // Format: "record_count<US>type_1<US>idc_1<RS>type_2<US>idc_2<RS>..."
            var sb = new StringBuilder();

            // First item: total count
            int totalRecords = transaction.Records.Count;
            sb.Append(totalRecords);

            // Add each record type and IDC
            // Use US to separate count from first type, and between type and IDC
            // Use RS to separate record pairs
            bool isFirst = true;
            foreach (var kvp in recordCounts.OrderBy(k => k.Key))
            {
                int recordType = kvp.Key;
                foreach (var idc in kvp.Value)
                {
                    if (!isFirst)
                    {
                        sb.Append((char)SeparatorConstants.RS);
                    }
                    isFirst = false;

                    sb.Append((char)SeparatorConstants.US);
                    sb.Append(recordType);
                    sb.Append((char)SeparatorConstants.US);
                    sb.Append(idc);
                }
            }

            // Update the CNT field in Type-1 record
            transaction.Header.UpdateField("1.003", sb.ToString());

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

            // Combine all field numbers to determine which is last
            var allFieldNumbers = new List<string>(fieldNumbers);
            allFieldNumbers.AddRange(unknownFieldNumbers);

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

            // Write unknown fields as simple text fields
            for (int i = 0; i < unknownFieldNumbers.Count; i++)
            {
                var fieldNumber = unknownFieldNumbers[i];
                var value = record.GetUnknownField(fieldNumber);
                if (value != null)
                {
                    // Create a temporary NistField for writing
                    var tempField = new NistField(fieldNumber);
                    tempField.SetValue(value);

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
                // Check if LEN field is in UnknownFields (this can happen for some record types)
                var lenUnknownValue = record.GetUnknownField(lenFieldNumber);
                if (lenUnknownValue != null)
                {
                    // Move it from UnknownFields to Fields
                    lenField = new NistField(lenFieldNumber);
                    lenField.SetValue(recordLength.ToString());
                    record.AddField(lenField);
                    record.RemoveUnknownField(lenFieldNumber);
                }
                else
                {
                    // Create new LEN field
                    lenField = new NistField(lenFieldNumber);
                    lenField.SetValue(recordLength.ToString());
                    record.AddField(lenField);
                }
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
            int estimatedLenFieldSize = lenFieldNumber.Length + 1 + 10 + 1; // field_num + : + length_value + GS

            // Add all field data
            int totalLength = estimatedLenFieldSize;
            foreach (var data in fieldData)
            {
                totalLength += data.Length;
            }

            // Note: FS separator is NOT part of the record length per ANSI/NIST-ITL standard
            // The FS is a separator between records, not part of the record itself

            // Re-calculate with actual LEN field size
            string lenValue = totalLength.ToString();
            int actualLenFieldSize = lenFieldNumber.Length + 1 + lenValue.Length + 1;

            // Adjust if our estimate was wrong
            if (actualLenFieldSize != estimatedLenFieldSize)
            {
                totalLength = totalLength - estimatedLenFieldSize + actualLenFieldSize;
            }

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
