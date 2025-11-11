using NistParser.Constants;
using NistParser.Core;

namespace NistParser.Records;

/// <summary>
/// Generic NIST record for record types that don't have specialized implementations.
/// Can handle any record type (Type-3 through Type-99) using the base NistRecord functionality.
/// </summary>
public class GenericNistRecord : NistRecord
{
    /// <summary>
    /// Initializes a new instance of the GenericNistRecord class
    /// </summary>
    /// <param name="recordType">The record type</param>
    public GenericNistRecord(RecordType recordType)
    {
        RecordType = recordType;
    }

    /// <summary>
    /// Gets the binary image data if this is a mixed or binary record with field 999
    /// </summary>
    public byte[]? ImageData => GetField($"{(int)RecordType}.999")?.BinaryData;

    /// <summary>
    /// Returns a string representation of this generic record
    /// </summary>
    public override string ToString()
    {
        var imageInfo = ImageData != null ? $", Image: {ImageData.Length / 1024.0:F1} KB" : "";
        return $"Type-{(int)RecordType} Record (IDC: {IDC}), {Fields.Count} fields{imageInfo}";
    }
}
