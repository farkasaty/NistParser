using System;

namespace NistParser.Exceptions
{
    /// <summary>
    /// Exception thrown when an image format cannot be converted to a web-compatible format.
    /// This is typically thrown for WSQ images, which require native platform-specific decoders
    /// that are not available as cross-platform pure .NET libraries.
    /// </summary>
    public class ImageConversionNotSupportedException : NistParserException
    {
        /// <summary>
        /// Gets the compression algorithm that could not be converted
        /// </summary>
        public string? CompressionFormat { get; }

        /// <summary>
        /// Initializes a new instance of the ImageConversionNotSupportedException class
        /// </summary>
        /// <param name="message">The error message</param>
        public ImageConversionNotSupportedException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ImageConversionNotSupportedException class
        /// </summary>
        /// <param name="compressionFormat">The unsupported compression format name</param>
        /// <param name="message">The error message</param>
        public ImageConversionNotSupportedException(string compressionFormat, string message)
            : base(message)
        {
            CompressionFormat = compressionFormat;
        }

        /// <summary>
        /// Initializes a new instance with a message and inner exception
        /// </summary>
        public ImageConversionNotSupportedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
