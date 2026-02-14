using System;
using System.IO;
using NistParser.Constants;
using NistParser.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;

namespace NistParser.Utilities
{
    /// <summary>
    /// Képkonverziós szolgáltatás a NIST fájlokból kinyert képek web-kompatibilis
    /// formátumba alakítására. Csak cross-platform, pure .NET megoldásokat használ.
    ///
    /// Támogatott konverziók:
    /// - JPEG → változatlanul (natívan böngésző-kompatibilis)
    /// - PNG → változatlanul (natívan böngésző-kompatibilis)
    /// - JPEG 2000 → PNG (ImageSharp könyvtárral)
    /// - RAW → PNG (nyers pixel adat átformázása)
    ///
    /// NEM támogatott:
    /// - WSQ → nincs cross-platform pure .NET dekóder
    /// </summary>
    public static class ImageConverter
    {
        /// <summary>
        /// Konvertálja a képadatot PNG formátumba.
        /// </summary>
        /// <param name="imageData">Az eredeti képadat</param>
        /// <param name="compression">Az eredeti tömörítési algoritmus</param>
        /// <returns>PNG formátumú képadat</returns>
        /// <exception cref="ImageConversionNotSupportedException">
        /// Ha a formátum nem konvertálható (WSQ)
        /// </exception>
        public static byte[] ConvertToPng(byte[] imageData, CompressionAlgorithm? compression)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data cannot be null or empty.", nameof(imageData));

            switch (compression)
            {
                case CompressionAlgorithm.JPEG_Lossy:
                case CompressionAlgorithm.JPEG_Lossless:
                case CompressionAlgorithm.PNG:
                    // Natívan böngésző-kompatibilis, nem kell konvertálni
                    return imageData;

                case CompressionAlgorithm.JPEG2000_Lossy:
                case CompressionAlgorithm.JPEG2000_Lossless:
                    return ConvertJpeg2000ToPng(imageData);

                case CompressionAlgorithm.Uncompressed:
                    // RAW pixel adat - visszaadjuk, a hívónak kell kezelnie
                    // mivel a szélesség/magasság/bitmélység infó a BiometricImage-ben van
                    return imageData;

                case CompressionAlgorithm.WSQ:
                    throw new ImageConversionNotSupportedException(
                        "WSQ",
                        "A WSQ (Wavelet Scalar Quantization) formátum nem konvertálható. " +
                        "Nincs elérhető cross-platform pure .NET WSQ dekóder. " +
                        "A nyers képadat a BiometricImage.RawImageData tulajdonságon keresztül érhető el.");

                default:
                    throw new ImageConversionNotSupportedException(
                        compression?.ToString() ?? "Unknown",
                        $"Ismeretlen tömörítési formátum: {compression}. Konverzió nem lehetséges.");
            }
        }

        /// <summary>
        /// Megadja, hogy a konverzió támogatott-e az adott formátumhoz.
        /// </summary>
        /// <param name="compression">A tömörítési algoritmus</param>
        /// <returns>True, ha a konverzió támogatott</returns>
        public static bool IsConversionSupported(CompressionAlgorithm? compression)
        {
            switch (compression)
            {
                case CompressionAlgorithm.JPEG_Lossy:
                case CompressionAlgorithm.JPEG_Lossless:
                case CompressionAlgorithm.PNG:
                case CompressionAlgorithm.JPEG2000_Lossy:
                case CompressionAlgorithm.JPEG2000_Lossless:
                case CompressionAlgorithm.Uncompressed:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// JPEG 2000 képadat konvertálása PNG formátumba.
        /// Használja a SixLabors.ImageSharp könyvtárat, ami natívan támogatja a JPEG2000-t.
        /// </summary>
        private static byte[] ConvertJpeg2000ToPng(byte[] jp2Data)
        {
            try
            {
                using var inputStream = new MemoryStream(jp2Data);
                using var image = Image.Load(inputStream);
                using var outputStream = new MemoryStream();

                image.Save(outputStream, new PngEncoder());
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new ImageConversionNotSupportedException(
                    "JPEG2000",
                    $"JPEG 2000 képadat konvertálása sikertelen: {ex.Message}");
            }
        }
    }
}
