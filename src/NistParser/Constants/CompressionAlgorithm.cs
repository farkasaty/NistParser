namespace NistParser.Constants;

/// <summary>
/// Compression algorithms supported by ANSI/NIST-ITL standard for biometric images.
/// Different record types support different compression methods.
/// </summary>
public enum CompressionAlgorithm
{
    /// <summary>
    /// No compression - raw image data
    /// </summary>
    Uncompressed = 0,

    /// <summary>
    /// WSQ (Wavelet Scalar Quantization) Version 2.0
    /// Optimized for fingerprint compression, preserves ridge structure and minutiae
    /// Commonly used with Type-14 (Fingerprint) records
    /// </summary>
    WSQ = 1,

    /// <summary>
    /// JPEG (ISO/IEC 10918) - Lossy compression
    /// Used for facial images (Type-10) and other photographs
    /// </summary>
    JPEG_Lossy = 2,

    /// <summary>
    /// JPEG (ISO/IEC 10918) - Lossless compression
    /// Used for facial images (Type-10) when quality preservation is critical
    /// </summary>
    JPEG_Lossless = 3,

    /// <summary>
    /// JPEG 2000 (ISO/IEC 15444-1) - Lossy compression
    /// Superior to standard JPEG, used for fingerprints and facial images
    /// Supported by Type-10 and Type-14 records
    /// </summary>
    JPEG2000_Lossy = 4,

    /// <summary>
    /// JPEG 2000 (ISO/IEC 15444-1) - Lossless compression
    /// Best quality preservation, used for critical biometric data
    /// Supported by Type-10 and Type-14 records
    /// </summary>
    JPEG2000_Lossless = 5,

    /// <summary>
    /// PNG (Portable Network Graphics) - Lossless compression
    /// Used for lossless image storage in Type-10 records
    /// </summary>
    PNG = 6
}

/// <summary>
/// Extension methods for CompressionAlgorithm enum
/// </summary>
public static class CompressionAlgorithmExtensions
{
    /// <summary>
    /// Determines if the compression algorithm is lossless
    /// </summary>
    /// <param name="algorithm">The compression algorithm</param>
    /// <returns>True if lossless, false if lossy or uncompressed</returns>
    public static bool IsLossless(this CompressionAlgorithm algorithm) => algorithm switch
    {
        CompressionAlgorithm.Uncompressed => true,
        CompressionAlgorithm.JPEG_Lossless => true,
        CompressionAlgorithm.JPEG2000_Lossless => true,
        CompressionAlgorithm.PNG => true,
        _ => false
    };

    /// <summary>
    /// Gets a human-readable name for the compression algorithm
    /// </summary>
    /// <param name="algorithm">The compression algorithm</param>
    /// <returns>Display name</returns>
    public static string GetDisplayName(this CompressionAlgorithm algorithm) => algorithm switch
    {
        CompressionAlgorithm.Uncompressed => "Uncompressed",
        CompressionAlgorithm.WSQ => "WSQ v2.0",
        CompressionAlgorithm.JPEG_Lossy => "JPEG (Lossy)",
        CompressionAlgorithm.JPEG_Lossless => "JPEG (Lossless)",
        CompressionAlgorithm.JPEG2000_Lossy => "JPEG 2000 (Lossy)",
        CompressionAlgorithm.JPEG2000_Lossless => "JPEG 2000 (Lossless)",
        CompressionAlgorithm.PNG => "PNG (Lossless)",
        _ => $"Unknown ({(int)algorithm})"
    };
}
