using NistParser.Constants;

namespace NistParser.Core;

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
    public Dictionary<string, NistField> Fields { get; set; } = new();

    /// <summary>
    /// Gets the encoding type for this record
    /// </summary>
    public RecordEncoding Encoding => RecordEncodingExtensions.GetEncoding(RecordType);

    /// <summary>
    /// Gets a field by field number
    /// </summary>
    /// <param name="fieldNumber">Field number (e.g., "1.003")</param>
    /// <returns>The field if found, null otherwise</returns>
    public NistField? GetField(string fieldNumber)
    {
        return Fields.TryGetValue(fieldNumber, out var field) ? field : null;
    }

    /// <summary>
    /// Gets the value of a field as a string (first item of first subfield)
    /// </summary>
    /// <param name="fieldNumber">Field number (e.g., "1.002")</param>
    /// <returns>The field value, or null if not found</returns>
    public string? GetFieldValue(string fieldNumber)
    {
        return GetField(fieldNumber)?.FirstValue;
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
    /// <param name="fieldNumber">Field number (e.g., "1.002")</param>
    /// <param name="value">The new value</param>
    public void UpdateField(string fieldNumber, string value)
    {
        var field = GetField(fieldNumber);
        if (field == null)
        {
            // Create new field
            field = new NistField(fieldNumber);
            AddField(field);
        }

        field.SetValue(value);
    }

    /// <summary>
    /// Removes a field from this record
    /// </summary>
    /// <param name="fieldNumber">Field number (e.g., "1.003")</param>
    /// <returns>True if the field was removed, false if it didn't exist</returns>
    public bool RemoveField(string fieldNumber)
    {
        return Fields.Remove(fieldNumber);
    }

    /// <summary>
    /// Checks if a field exists in this record
    /// </summary>
    /// <param name="fieldNumber">Field number (e.g., "1.003")</param>
    /// <returns>True if the field exists</returns>
    public bool HasField(string fieldNumber)
    {
        return Fields.ContainsKey(fieldNumber);
    }

    /// <summary>
    /// Returns a string representation of this record
    /// </summary>
    public override string ToString()
    {
        return $"Record Type {(int)RecordType} (IDC: {IDC}), {Fields.Count} fields, {RecordLength} bytes";
    }
}
