using System.Collections.Generic;
using NistParser.Constants;

namespace NistParser.Core
{

    /// <summary>
    /// Base class for all ANSI/NIST-ITL record types.
    /// A record is a logical grouping of related fields within a transaction.
    /// </summary>
    public abstract class NistRecord
    {
        /// <summary>
        /// Gets or sets the record type (1-99)
        /// </summary>
        public RecordType RecordType { get; set; }

        /// <summary>
        /// Gets or sets the Information Designation Character (IDC)
        /// Links related records in a transaction (e.g., image and its minutiae)
        /// Format: Two-digit numeric "00" through "99"
        /// </summary>
        public string IDC { get; set; } = "00";

        /// <summary>
        /// Gets or sets the logical record length in bytes
        /// Field X.001 in all records
        /// </summary>
        public int RecordLength { get; set; }

        /// <summary>
        /// Gets or sets all fields in this record, keyed by field number (e.g., "1.003")
        /// </summary>
        public Dictionary<string, NistField> Fields { get; set; } = new Dictionary<string, NistField>();

        /// <summary>
        /// Gets or sets unknown fields (fields without metadata definition) as simple key-value pairs.
        /// These are fields that exist in the file but are not defined in the field configuration.
        /// Key is the field number (e.g., "2.999"), value is the field content as string.
        /// </summary>
        public Dictionary<string, string> UnknownFields { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Gets the encoding type for this record
        /// </summary>
        public RecordEncoding Encoding => RecordEncodingExtensions.GetEncoding(RecordType);

        /// <summary>
        /// Gets a field by field number
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "1.003", "1.01", or "1.1")</param>
        /// <returns>The field if found, null otherwise</returns>
        public NistField? GetField(string fieldNumber)
        {
            // Normalize field number for consistent lookup
            string normalized = Utilities.FieldNumberFormatter.Normalize(fieldNumber);
            return Fields.TryGetValue(normalized, out var field) ? field : null;
        }

        /// <summary>
        /// Gets the value of a field as a string (first item of first subfield)
        /// Checks both Fields (known fields) and UnknownFields collections
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "1.002")</param>
        /// <returns>The field value, or null if not found</returns>
        public string? GetFieldValue(string fieldNumber)
        {
            // Check Fields first (known fields with metadata)
            var field = GetField(fieldNumber);
            if (field != null)
            {
                return field.FirstValue;
            }

            // Check UnknownFields (fields without metadata)
            return GetUnknownField(fieldNumber);
        }

        /// <summary>
        /// Adds a field to this record
        /// </summary>
        /// <param name="field">The field to add</param>
        public void AddField(NistField field)
        {
            Fields[field.FieldNumber] = field;
        }

        /// <summary>
        /// Updates a field value (or adds it if it doesn't exist)
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "1.002", "1.01", or "1.1")</param>
        /// <param name="value">The new value</param>
        public void UpdateField(string fieldNumber, string value)
        {
            // Normalize field number for consistent operations
            string normalized = Utilities.FieldNumberFormatter.Normalize(fieldNumber);

            // Check if this field exists in UnknownFields first
            // Unknown fields are stored as simple string values
            if (UnknownFields.ContainsKey(normalized))
            {
                // Update the value directly in UnknownFields
                UnknownFields[normalized] = value;
                return;
            }

            // Check if this field exists in Fields (known fields with metadata)
            var field = GetField(normalized);
            if (field == null)
            {
                // Create new field and add to Fields
                // NistField constructor will normalize the field number
                field = new NistField(normalized);
                AddField(field);
            }

            field.SetValue(value);
        }

        /// <summary>
        /// Removes a field from this record
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "1.003", "1.01", or "1.1")</param>
        /// <returns>True if the field was removed, false if it didn't exist</returns>
        public bool RemoveField(string fieldNumber)
        {
            // Normalize field number for consistent operations
            string normalized = Utilities.FieldNumberFormatter.Normalize(fieldNumber);
            return Fields.Remove(normalized);
        }

        /// <summary>
        /// Checks if a field exists in this record
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "1.003", "1.01", or "1.1")</param>
        /// <returns>True if the field exists</returns>
        public bool HasField(string fieldNumber)
        {
            // Normalize field number for consistent operations
            string normalized = Utilities.FieldNumberFormatter.Normalize(fieldNumber);
            return Fields.ContainsKey(normalized);
        }

        /// <summary>
        /// Gets an unknown field value by field number
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "2.999", "2.99", or "2.9")</param>
        /// <returns>The field value, or null if not found</returns>
        public string? GetUnknownField(string fieldNumber)
        {
            // Normalize field number for consistent operations
            string normalized = Utilities.FieldNumberFormatter.Normalize(fieldNumber);
            return UnknownFields.TryGetValue(normalized, out var value) ? value : null;
        }

        /// <summary>
        /// Sets an unknown field value (or adds it if it doesn't exist)
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "2.999", "2.99", or "2.9")</param>
        /// <param name="value">The field value</param>
        public void SetUnknownField(string fieldNumber, string value)
        {
            // Normalize field number for consistent operations
            string normalized = Utilities.FieldNumberFormatter.Normalize(fieldNumber);
            UnknownFields[normalized] = value;
        }

        /// <summary>
        /// Removes an unknown field
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "2.999", "2.99", or "2.9")</param>
        /// <returns>True if the field was removed, false if it didn't exist</returns>
        public bool RemoveUnknownField(string fieldNumber)
        {
            // Normalize field number for consistent operations
            string normalized = Utilities.FieldNumberFormatter.Normalize(fieldNumber);
            return UnknownFields.Remove(normalized);
        }

        /// <summary>
        /// Checks if an unknown field exists
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "2.999")</param>
        /// <returns>True if the field exists in UnknownFields</returns>
        public bool HasUnknownField(string fieldNumber)
        {
            return UnknownFields.ContainsKey(fieldNumber);
        }

        /// <summary>
        /// Returns a string representation of this record
        /// </summary>
        public override string ToString()
        {
            var totalFields = Fields.Count + UnknownFields.Count;
            var unknownInfo = UnknownFields.Count > 0 ? $" ({UnknownFields.Count} unknown)" : "";
            return $"Record Type {(int)RecordType} (IDC: {IDC}), {totalFields} fields{unknownInfo}, {RecordLength} bytes";
        }
    }
}
