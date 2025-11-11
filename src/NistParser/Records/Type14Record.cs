using NistParser.Constants;
using NistParser.Core;

namespace NistParser.Records;

/// <summary>
/// Type-14 Fingerprint Image Record
/// Contains variable resolution fingerprint images (500/1000 ppi recommended).
/// This is a mixed record with ASCII tagged fields (001-998) and binary field 999 (image data).
/// </summary>
public class Type14Record : NistRecord
{
    /// <summary>
    /// Initializes a new instance of the Type14Record class
    /// </summary>
    public Type14Record()
    {
        RecordType = RecordType.Type14_FingerprintImage;
    }

    /// <summary>
    /// Field 14.003 - Impression Type (IMP)
    /// Indicates how the fingerprint was captured
    /// </summary>
    public string? ImpressionType => GetFieldValue("14.003");

    /// <summary>
    /// Field 14.004 - Source Agency / ORI
    /// Originating agency for this specific record
    /// </summary>
    public string? SourceAgency => GetFieldValue("14.004");

    /// <summary>
    /// Field 14.005 - Fingerprint Position
    /// Indicates which finger (e.g., "1" = right thumb)
    /// </summary>
    public string? FingerPosition => GetFieldValue("14.005");

    /// <summary>
    /// Field 14.009 - Image Scanning Resolution
    /// Resolution in pixels per millimeter (ppmm) or pixels per inch (ppi)
    /// </summary>
    public string? ScanningResolution => GetFieldValue("14.009");

    /// <summary>
    /// Gets the scanning resolution as a decimal value
    /// </summary>
    public decimal? ScanningResolutionValue
    {
        get
        {
            if (decimal.TryParse(ScanningResolution, out decimal value))
                return value;
            return null;
        }
    }

    /// <summary>
    /// Field 14.010 - Horizontal Line Length (HLL)
    /// Image width in pixels
    /// </summary>
    public string? HorizontalLineLength => GetFieldValue("14.010");

    /// <summary>
    /// Gets the image width as an integer
    /// </summary>
    public int? Width
    {
        get
        {
            if (int.TryParse(HorizontalLineLength, out int value))
                return value;
            return null;
        }
    }

    /// <summary>
    /// Field 14.011 - Vertical Line Length (VLL)
    /// Image height in pixels
    /// </summary>
    public string? VerticalLineLength => GetFieldValue("14.011");

    /// <summary>
    /// Gets the image height as an integer
    /// </summary>
    public int? Height
    {
        get
        {
            if (int.TryParse(VerticalLineLength, out int value))
                return value;
            return null;
        }
    }

    /// <summary>
    /// Field 14.012 - Scale Units
    /// Units for resolution (1 = pixels per inch, 2 = pixels per centimeter)
    /// </summary>
    public string? ScaleUnits => GetFieldValue("14.012");

    /// <summary>
    /// Field 14.013 - Compression Algorithm
    /// Code indicating the compression method used
    /// </summary>
    public string? CompressionAlgorithmCode => GetFieldValue("14.013");

    /// <summary>
    /// Gets the compression algorithm as an enum
    /// </summary>
    public CompressionAlgorithm? Compression
    {
        get
        {
            if (int.TryParse(CompressionAlgorithmCode, out int code) &&
                Enum.IsDefined(typeof(CompressionAlgorithm), code))
            {
                return (CompressionAlgorithm)code;
            }
            return null;
        }
    }

    /// <summary>
    /// Field 14.999 - Image Data (Binary)
    /// The actual fingerprint image data
    /// </summary>
    public byte[]? ImageData => GetField("14.999")?.BinaryData;

    /// <summary>
    /// Gets the size of the image data in bytes
    /// </summary>
    public int? ImageDataSize => ImageData?.Length;

    /// <summary>
    /// Returns a string representation of this Type-14 record
    /// </summary>
    public override string ToString()
    {
        var dimensions = (Width != null && Height != null) ? $"{Width}x{Height}" : "Unknown size";
        var compression = Compression?.GetDisplayName() ?? "Unknown compression";
        var size = ImageDataSize != null ? $"{ImageDataSize / 1024.0:F1} KB" : "No image data";

        return $"Type-14 Record (IDC: {IDC}) - Finger {FingerPosition}, {dimensions}, " +
               $"{compression}, {size}";
    }
}
