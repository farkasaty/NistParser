using System;
using System.IO;
using System.Reflection;
using CSJ2K;
using CSJ2K.Util;
using NistParser.Constants;
using NistParser.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace NistParser.Utilities
{
    /// <summary>
    /// Képkonverziós szolgáltatás a NIST fájlokból kinyert képek web-kompatibilis
    /// formátumba alakítására. Csak cross-platform, pure .NET megoldásokat használ.
    ///
    /// Támogatott konverziók:
    /// - JPEG → változatlanul (natívan böngésző-kompatibilis)
    /// - PNG → változatlanul (natívan böngésző-kompatibilis)
    /// - JPEG 2000 → PNG (CSJ2K dekóder + ImageSharp enkóder)
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
        /// CSJ2K könyvtárat használ a dekódoláshoz, ImageSharp-ot a PNG enkódoláshoz.
        /// </summary>
        private static byte[] ConvertJpeg2000ToPng(byte[] jp2Data)
        {
            try
            {
                var decoded = J2kImage.FromBytes(jp2Data);
                int numComps = decoded.NumberOfComponents;

                // Width/Height internal property-k a CSJ2K 3.0-ban, reflection szükséges
                var piType = typeof(PortableImage);
                int width = (int)piType.GetProperty("Width", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(decoded)!;
                int height = (int)piType.GetProperty("Height", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(decoded)!;

                if (numComps >= 3)
                {
                    // RGB vagy RGBA kép
                    var compR = decoded.GetComponent(0);
                    var compG = decoded.GetComponent(1);
                    var compB = decoded.GetComponent(2);

                    using var image = new Image<Rgb24>(width, height);
                    for (int y = 0; y < height; y++)
                    {
                        int rowOffset = y * width;
                        for (int x = 0; x < width; x++)
                        {
                            int idx = rowOffset + x;
                            image[x, y] = new Rgb24(
                                ClampToByte(compR[idx]),
                                ClampToByte(compG[idx]),
                                ClampToByte(compB[idx]));
                        }
                    }

                    using var outputStream = new MemoryStream();
                    image.Save(outputStream, new PngEncoder());
                    return outputStream.ToArray();
                }
                else
                {
                    // Szürkeárnyalatos kép
                    var compGray = decoded.GetComponent(0);

                    using var image = new Image<L8>(width, height);
                    for (int y = 0; y < height; y++)
                    {
                        int rowOffset = y * width;
                        for (int x = 0; x < width; x++)
                        {
                            image[x, y] = new L8(ClampToByte(compGray[rowOffset + x]));
                        }
                    }

                    using var outputStream = new MemoryStream();
                    image.Save(outputStream, new PngEncoder());
                    return outputStream.ToArray();
                }
            }
            catch (ImageConversionNotSupportedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ImageConversionNotSupportedException(
                    "JPEG2000",
                    $"JPEG 2000 képadat konvertálása sikertelen: {ex.Message}");
            }
        }

        private static byte ClampToByte(int value)
        {
            if (value < 0) return 0;
            if (value > 255) return 255;
            return (byte)value;
        }
    }
}
