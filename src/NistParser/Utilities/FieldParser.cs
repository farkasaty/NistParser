using NistParser.Constants;
using NistParser.Core;
using NistParser.Exceptions;
using System.Text;

namespace NistParser.Utilities;

/// <summary>
/// Utility class for parsing ANSI/NIST-ITL tagged fields.
/// Handles the hierarchical structure of fields, subfields, and information items.
/// </summary>
public static class FieldParser
{
    /// <summary>
    /// Parses a single tagged field from raw bytes
    /// Format: "X.YYY:data<separator>"
    /// </summary>
    /// <param name="fieldData">The raw field data (including field number and colon)</param>
    /// <returns>The parsed NistField</returns>
    public static NistField ParseTaggedField(byte[] fieldData)
    {
        // Find the colon separator between field number and data
        int colonIndex = Array.IndexOf(fieldData, SeparatorConstants.COLON);
        if (colonIndex == -1)
        {
            throw new InvalidFieldFormatException("Unknown",
                "Field does not contain colon separator");
        }

        // Extract field number (e.g., "1.003")
        string fieldNumber = Encoding.ASCII.GetString(fieldData, 0, colonIndex);

        // Extract field data (everything after the colon)
        int dataLength = fieldData.Length - colonIndex - 1;
        byte[] data = new byte[dataLength];
        Array.Copy(fieldData, colonIndex + 1, data, 0, dataLength);

        // Create the field object
        var field = new NistField(fieldNumber);

        // Parse the data into subfields and items
        field.Subfields = ParseFieldData(data);

        return field;
    }

    /// <summary>
    /// Parses field data into subfields and information items
    /// Handles RS (subfield separator) and US (item separator)
    /// </summary>
    /// <param name="data">The field data (after the colon)</param>
    /// <returns>List of subfields</returns>
    public static List<NistSubfield> ParseFieldData(byte[] data)
    {
        var subfields = new List<NistSubfield>();

        if (data.Length == 0)
        {
            return subfields;
        }

        // Split on RS (Record Separator) to get subfields
        var subfieldChunks = SplitBytes(data, SeparatorConstants.RS);

        foreach (var subfieldChunk in subfieldChunks)
        {
            if (subfieldChunk.Length == 0)
                continue;

            var subfield = new NistSubfield();

            // Split on US (Unit Separator) to get information items
            var itemChunks = SplitBytes(subfieldChunk, SeparatorConstants.US);

            foreach (var itemChunk in itemChunks)
            {
                if (itemChunk.Length > 0)
                {
                    string item = Encoding.UTF8.GetString(itemChunk);
                    subfield.Items.Add(item);
                }
            }

            if (subfield.Items.Count > 0)
            {
                subfields.Add(subfield);
            }
        }

        // If no subfields were created (no RS separator), treat entire data as one subfield
        if (subfields.Count == 0)
        {
            var subfield = new NistSubfield();
            var itemChunks = SplitBytes(data, SeparatorConstants.US);

            foreach (var itemChunk in itemChunks)
            {
                if (itemChunk.Length > 0)
                {
                    string item = Encoding.UTF8.GetString(itemChunk);
                    subfield.Items.Add(item);
                }
            }

            if (subfield.Items.Count > 0)
            {
                subfields.Add(subfield);
            }
        }

        return subfields;
    }

    /// <summary>
    /// Splits a byte array by a delimiter byte
    /// </summary>
    /// <param name="data">The data to split</param>
    /// <param name="delimiter">The delimiter byte</param>
    /// <returns>List of byte arrays</returns>
    public static List<byte[]> SplitBytes(byte[] data, byte delimiter)
    {
        var result = new List<byte[]>();
        int start = 0;

        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] == delimiter)
            {
                // Found delimiter, extract the chunk
                int length = i - start;
                if (length > 0)
                {
                    byte[] chunk = new byte[length];
                    Array.Copy(data, start, chunk, 0, length);
                    result.Add(chunk);
                }
                else
                {
                    // Empty chunk (consecutive delimiters)
                    result.Add(Array.Empty<byte>());
                }

                start = i + 1;
            }
        }

        // Add the last chunk (after the last delimiter or entire array if no delimiter found)
        if (start < data.Length)
        {
            int length = data.Length - start;
            byte[] chunk = new byte[length];
            Array.Copy(data, start, chunk, 0, length);
            result.Add(chunk);
        }
        else if (start == data.Length && data.Length > 0 && data[data.Length - 1] == delimiter)
        {
            // Trailing delimiter
            result.Add(Array.Empty<byte>());
        }

        // If no chunks were created, add the entire array as one chunk
        if (result.Count == 0 && data.Length > 0)
        {
            result.Add(data);
        }

        return result;
    }

    /// <summary>
    /// Extracts the field number from a field's raw data
    /// </summary>
    /// <param name="fieldData">The field data</param>
    /// <returns>The field number (e.g., "1.003") or null if not found</returns>
    public static string? ExtractFieldNumber(byte[] fieldData)
    {
        int colonIndex = Array.IndexOf(fieldData, SeparatorConstants.COLON);
        if (colonIndex == -1)
            return null;

        return Encoding.ASCII.GetString(fieldData, 0, colonIndex);
    }

    /// <summary>
    /// Finds the index of a specific byte in a byte array, starting from a given position
    /// </summary>
    /// <param name="data">The data to search</param>
    /// <param name="value">The byte to find</param>
    /// <param name="startIndex">The starting position</param>
    /// <returns>The index of the byte, or -1 if not found</returns>
    public static int IndexOf(byte[] data, byte value, int startIndex = 0)
    {
        for (int i = startIndex; i < data.Length; i++)
        {
            if (data[i] == value)
                return i;
        }
        return -1;
    }
}
