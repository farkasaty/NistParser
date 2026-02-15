using System;
using System.Collections.Generic;
using System.Linq;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Models;

namespace NistParser.Utilities
{
    /// <summary>
    /// Statikus szolgáltatás biometrikus képek kinyerésére NIST tranzakciókból.
    /// A különböző rekordtípusokból (Type-4, Type-10, Type-13, Type-14, Type-15, stb.)
    /// egységes BiometricImage objektumokba alakítja a képadatokat és metaadatokat.
    /// </summary>
    public static class ImageExtractor
    {
        /// <summary>
        /// Kinyeri az összes biometrikus képet a tranzakcióból.
        /// </summary>
        /// <param name="transaction">A feldolgozott NIST tranzakció</param>
        /// <returns>Az összes kinyert kép metaadatokkal</returns>
        public static IEnumerable<BiometricImage> ExtractAllImages(NistTransaction transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            foreach (var record in transaction.Records)
            {
                if (IsImageRecord(record.RecordType))
                {
                    var image = ExtractImageFromRecord(record);
                    if (image != null)
                    {
                        yield return image;
                    }
                }
            }
        }

        /// <summary>
        /// Kinyeri a megadott típusú képeket a tranzakcióból.
        /// </summary>
        /// <param name="transaction">A feldolgozott NIST tranzakció</param>
        /// <param name="imageType">A kívánt képtípus (pl. Fingerprint, Face)</param>
        /// <returns>A megadott típusú képek</returns>
        public static IEnumerable<BiometricImage> ExtractImages(
            NistTransaction transaction,
            BiometricImageType imageType)
        {
            return ExtractAllImages(transaction).Where(img => img.ImageType == imageType);
        }

        /// <summary>
        /// Kinyeri a megadott IDC-hez tartozó képeket.
        /// </summary>
        /// <param name="transaction">A feldolgozott NIST tranzakció</param>
        /// <param name="idc">Information Designation Character</param>
        /// <returns>Az adott IDC-hez tartozó képek</returns>
        public static IEnumerable<BiometricImage> ExtractImagesByIDC(
            NistTransaction transaction,
            string idc)
        {
            return ExtractAllImages(transaction).Where(img => img.IDC == idc);
        }

        /// <summary>
        /// Meghatározza, hogy a rekordtípus tartalmaz-e képadatot.
        /// </summary>
        private static bool IsImageRecord(RecordType recordType)
        {
            switch (recordType)
            {
                case RecordType.Type3_LowResGrayscaleFingerprint:
                case RecordType.Type4_HighResGrayscaleFingerprint:
                case RecordType.Type5_LowResBinaryFingerprint:
                case RecordType.Type6_HighResBinaryFingerprint:
                case RecordType.Type7_UserDefinedImage:
                case RecordType.Type8_SignatureImage:
                case RecordType.Type10_FacialAndSMTImage:
                case RecordType.Type13_LatentImage:
                case RecordType.Type14_FingerprintImage:
                case RecordType.Type15_PalmPrintImage:
                case RecordType.Type17_IrisImage:
                case RecordType.Type19_PlantarImage:
                case RecordType.Type20_SourceRepresentation:
                case RecordType.Type21_AssociatedContext:
                case RecordType.Type22_NonPhotographicImagery:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Kinyeri a képadatokat és metaadatokat egy NIST rekordból.
        /// </summary>
        private static BiometricImage? ExtractImageFromRecord(NistRecord record)
        {
            var imageData = GetImageData(record);
            if (imageData == null || imageData.Length == 0)
                return null;

            var image = new BiometricImage
            {
                IDC = record.IDC,
                RecordType = record.RecordType,
                ImageType = DetermineImageType(record),
                RawImageData = imageData
            };

            // Rekordtípus-specifikus metaadatok kinyerése
            ExtractMetadata(record, image);

            return image;
        }

        /// <summary>
        /// Meghatározza a kép biometrikus típusát a rekordtípus alapján.
        /// </summary>
        private static BiometricImageType DetermineImageType(NistRecord record)
        {
            switch (record.RecordType)
            {
                case RecordType.Type3_LowResGrayscaleFingerprint:
                case RecordType.Type4_HighResGrayscaleFingerprint:
                case RecordType.Type5_LowResBinaryFingerprint:
                case RecordType.Type6_HighResBinaryFingerprint:
                case RecordType.Type14_FingerprintImage:
                    return BiometricImageType.Fingerprint;

                case RecordType.Type10_FacialAndSMTImage:
                    return DetermineType10ImageType(record);

                case RecordType.Type13_LatentImage:
                    return BiometricImageType.LatentFingerprint;

                case RecordType.Type15_PalmPrintImage:
                    return BiometricImageType.PalmPrint;

                case RecordType.Type17_IrisImage:
                    return BiometricImageType.Iris;

                case RecordType.Type19_PlantarImage:
                    return BiometricImageType.Footprint;

                case RecordType.Type8_SignatureImage:
                    return BiometricImageType.Signature;

                default:
                    return BiometricImageType.Other;
            }
        }

        /// <summary>
        /// Meghatározza a Type-10 rekord pontos képtípusát az IMT (Image Type) mező alapján.
        /// </summary>
        private static BiometricImageType DetermineType10ImageType(NistRecord record)
        {
            // Field 10.003 - Image Type (IMT)
            var imt = record.GetFieldValue("10.003");
            if (imt == null)
                return BiometricImageType.Face; // Default for Type-10

            var imtUpper = imt.Trim().ToUpperInvariant();
            if (imtUpper == "FACE")
                return BiometricImageType.Face;
            if (imtUpper == "SCAR" || imtUpper == "MARK" || imtUpper == "TATTOO")
                return BiometricImageType.SMT;

            return BiometricImageType.Face; // Default
        }

        /// <summary>
        /// Kinyeri a képadatot a rekordból.
        /// Mixed rekordoknál a Field X.999-ből, bináris rekordoknál a fix struktúrából.
        /// </summary>
        private static byte[]? GetImageData(NistRecord record)
        {
            // Prefix megállapítása a rekordtípusszám alapján
            var typeNumber = (int)record.RecordType;
            var imageFieldNumber = $"{typeNumber}.999";

            // Mixed rekordok: Field X.999 tartalmazza a képadatot
            var imageField = record.GetField(imageFieldNumber);
            if (imageField?.BinaryData != null)
            {
                return imageField.BinaryData;
            }

            // Pure binary rekordok (Type-3 through Type-8): 
            // a képadat az utolsó mező/komplett bináris blokk
            // Próbáljuk az UnknownFields-ből kinyerni, ha ott van bináris adat
            if (IsPureBinaryRecordType(record.RecordType))
            {
                return ExtractBinaryRecordImageData(record);
            }

            return null;
        }

        /// <summary>
        /// Meghatározza, hogy a rekordtípus pure binary (nem mixed) rekord-e.
        /// </summary>
        private static bool IsPureBinaryRecordType(RecordType recordType)
        {
            switch (recordType)
            {
                case RecordType.Type3_LowResGrayscaleFingerprint:
                case RecordType.Type4_HighResGrayscaleFingerprint:
                case RecordType.Type5_LowResBinaryFingerprint:
                case RecordType.Type6_HighResBinaryFingerprint:
                case RecordType.Type7_UserDefinedImage:
                case RecordType.Type8_SignatureImage:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Kinyeri a képadatot pure binary rekordokból.
        /// Ezeknél a rekordoknál a képadat a fix fejléc után következik.
        /// </summary>
        private static byte[]? ExtractBinaryRecordImageData(NistRecord record)
        {
            // Pure binary rekordoknál a képadat a 999-es mezőben lehet,
            // ha a parser oda tette
            var typeNumber = (int)record.RecordType;
            var imageFieldNumber = $"{typeNumber}.999";

            // Check known fields
            var field = record.GetField(imageFieldNumber);
            if (field?.BinaryData != null)
                return field.BinaryData;

            // Check if there's any binary field data at all
            foreach (var kvp in record.Fields)
            {
                if (kvp.Value.IsBinary && kvp.Value.BinaryData != null)
                    return kvp.Value.BinaryData;
            }

            return null;
        }

        /// <summary>
        /// Kinyeri a metaadatokat a rekordból és beállítja a BiometricImage tulajdonságait.
        /// A kinyert metaadatok a rekordtípustól függenek.
        /// </summary>
        private static void ExtractMetadata(NistRecord record, BiometricImage image)
        {
            var typeNumber = (int)record.RecordType;

            // Közös mezők mixed rekordoknál
            ExtractCommonMixedMetadata(record, image, typeNumber);

            // Rekordtípus-specifikus metaadatok
            switch (record.RecordType)
            {
                case RecordType.Type14_FingerprintImage:
                    ExtractType14Metadata(record, image);
                    break;
                case RecordType.Type10_FacialAndSMTImage:
                    ExtractType10Metadata(record, image);
                    break;
                case RecordType.Type13_LatentImage:
                    ExtractType13Metadata(record, image);
                    break;
                case RecordType.Type15_PalmPrintImage:
                    ExtractType15Metadata(record, image);
                    break;
                case RecordType.Type17_IrisImage:
                    ExtractType17Metadata(record, image);
                    break;
                case RecordType.Type4_HighResGrayscaleFingerprint:
                    ExtractType4Metadata(record, image);
                    break;
            }
        }

        /// <summary>
        /// Kinyeri a közös metaadatokat, amelyek a legtöbb mixed rekordtípusban megtalálhatók.
        /// </summary>
        private static void ExtractCommonMixedMetadata(NistRecord record, BiometricImage image, int typeNumber)
        {
            // Source Agency (X.004)
            image.SourceAgency = record.GetFieldValue($"{typeNumber}.004");

            // Horizontal Line Length / Width (X.006)
            var hll = record.GetFieldValue($"{typeNumber}.006");
            if (int.TryParse(hll, out int width))
                image.Width = width;

            // Vertical Line Length / Height (X.007)
            var vll = record.GetFieldValue($"{typeNumber}.007");
            if (int.TryParse(vll, out int height))
                image.Height = height;

            // Scale Units (X.008)
            image.ResolutionUnit = record.GetFieldValue($"{typeNumber}.008");

            // Scanning Resolution / THPS (X.009)
            var sres = record.GetFieldValue($"{typeNumber}.009");
            if (decimal.TryParse(sres, out decimal resolution))
                image.Resolution = resolution;

            // Compression Algorithm / CGA (X.011)
            var compressionCode = record.GetFieldValue($"{typeNumber}.011");
            image.CompressionCode = compressionCode;
            if (compressionCode != null)
            {
                image.Compression = ParseCompressionCode(compressionCode);
            }
        }

        /// <summary>
        /// Parse-olja a compression code-ot, amely lehet szám vagy string formátumban.
        /// Példák: "1" → WSQ, "WSQ" → WSQ, "WSQ20" → WSQ, "JPEGB" → JPEG_Lossy
        /// </summary>
        private static CompressionAlgorithm? ParseCompressionCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            code = code.Trim().ToUpperInvariant();

            // Először próbáljuk szám formátumban
            if (int.TryParse(code, out int numericCode) && Enum.IsDefined(typeof(CompressionAlgorithm), numericCode))
            {
                return (CompressionAlgorithm)numericCode;
            }

            // String formátumú compression code-ok kezelése
            return code switch
            {
                "WSQ" or "WSQ20" => CompressionAlgorithm.WSQ,
                "JPEGB" or "JPEG" or "JPG" => CompressionAlgorithm.JPEG_Lossy,
                "JPEGL" => CompressionAlgorithm.JPEG_Lossless,
                "JP2" => CompressionAlgorithm.JPEG2000_Lossy,
                "JP2L" => CompressionAlgorithm.JPEG2000_Lossless,
                "PNG" => CompressionAlgorithm.PNG,
                "NONE" or "RAW" => CompressionAlgorithm.Uncompressed,
                _ => null
            };
        }

        /// <summary>
        /// Type-14 (Fingerprint) specifikus metaadatok.
        /// Per ANSI/NIST-ITL 1-2011 Update:2015 specification:
        ///   14.003=IMP, 14.004=SRC, 14.005=FCD, 14.006=HLL, 14.007=VLL,
        ///   14.008=SLC, 14.009=THPS, 14.010=TVPS, 14.011=CGA, 14.012=BPX, 14.013=FGP
        /// </summary>
        private static void ExtractType14Metadata(NistRecord record, BiometricImage image)
        {
            // Field 14.003 - Impression Type (IMP)
            image.ImpressionType = record.GetFieldValue("14.003");

            // Field 14.004 - Source Agency (SRC) - may override common
            var src = record.GetFieldValue("14.004");
            if (!string.IsNullOrEmpty(src))
                image.SourceAgency = src;

            // Field 14.005 - Fingerprint Capture Date (FCD)
            var captureDate = record.GetFieldValue("14.005");
            if (!string.IsNullOrEmpty(captureDate))
                image.AdditionalMetadata["CaptureDate"] = captureDate!;

            // Field 14.006 - Horizontal Line Length (HLL) - Width
            var hll = record.GetFieldValue("14.006");
            if (int.TryParse(hll, out int width))
                image.Width = width;

            // Field 14.007 - Vertical Line Length (VLL) - Height
            var vll = record.GetFieldValue("14.007");
            if (int.TryParse(vll, out int height))
                image.Height = height;

            // Field 14.008 - Scale Units (SLC)
            var scaleUnits = record.GetFieldValue("14.008");
            if (!string.IsNullOrEmpty(scaleUnits))
                image.ResolutionUnit = scaleUnits;

            // Field 14.009 - Transmitted Horizontal Pixel Scale (THPS) - Resolution
            var sres = record.GetFieldValue("14.009");
            if (decimal.TryParse(sres, out decimal resolution))
                image.Resolution = resolution;

            // Field 14.011 - Compression Algorithm (CGA)
            var compression = record.GetFieldValue("14.011");
            if (!string.IsNullOrEmpty(compression))
            {
                image.CompressionCode = compression;
                image.Compression = ParseCompressionCode(compression);
            }

            // Field 14.013 - Friction Ridge Generalized Position (FGP)
            image.FingerPosition = record.GetFieldValue("14.013");
        }

        /// <summary>
        /// Type-10 (Face / SMT) specifikus metaadatok.
        /// </summary>
        private static void ExtractType10Metadata(NistRecord record, BiometricImage image)
        {
            // Field 10.003 - Image Type
            var imt = record.GetFieldValue("10.003");
            image.FaceImageType = imt;
            if (!string.IsNullOrEmpty(imt))
            {
                image.FaceImageTypeDescription = GetFaceImageTypeDescription(imt!);
            }

            // Field 10.004 - Source Agency
            var src = record.GetFieldValue("10.004");
            if (!string.IsNullOrEmpty(src))
                image.SourceAgency = src;

            // Field 10.006 - HLL (Width)
            var hll = record.GetFieldValue("10.006");
            if (int.TryParse(hll, out int width))
                image.Width = width;

            // Field 10.007 - VLL (Height)
            var vll = record.GetFieldValue("10.007");
            if (int.TryParse(vll, out int height))
                image.Height = height;

            // Field 10.008 - Scale Units
            var scaleUnits = record.GetFieldValue("10.008");
            if (!string.IsNullOrEmpty(scaleUnits))
                image.ResolutionUnit = scaleUnits;

            // Field 10.009 - Scanning Resolution
            var sres = record.GetFieldValue("10.009");
            if (decimal.TryParse(sres, out decimal resolution))
                image.Resolution = resolution;

            // Field 10.011 - Compression Algorithm
            var compression = record.GetFieldValue("10.011");
            if (!string.IsNullOrEmpty(compression))
            {
                image.CompressionCode = compression;
                image.Compression = ParseCompressionCode(compression);
            }

            // Field 10.020 - Subject Description (SMD)
            image.PhotoDescription = record.GetFieldValue("10.020");
        }

        /// <summary>
        /// Type-13 (Latent) specifikus metaadatok.
        /// </summary>
        private static void ExtractType13Metadata(NistRecord record, BiometricImage image)
        {
            // Field 13.003 - Impression Type
            image.ImpressionType = record.GetFieldValue("13.003");

            // Field 13.004 - Source Agency
            var src = record.GetFieldValue("13.004");
            if (!string.IsNullOrEmpty(src))
                image.SourceAgency = src;

            // Field 13.005 - Latent Conditions (if available)
            var conditions = record.GetFieldValue("13.005");
            if (!string.IsNullOrEmpty(conditions))
                image.AdditionalMetadata["LatentConditions"] = conditions!;

            // Field 13.013 - Finger Position
            image.FingerPosition = record.GetFieldValue("13.013");

            // Compression (13.011)
            var compression = record.GetFieldValue("13.011");
            if (!string.IsNullOrEmpty(compression))
            {
                image.CompressionCode = compression;
                image.Compression = ParseCompressionCode(compression);
            }
        }

        /// <summary>
        /// Type-15 (Palm Print) specifikus metaadatok.
        /// </summary>
        private static void ExtractType15Metadata(NistRecord record, BiometricImage image)
        {
            // Field 15.003 - Impression Type
            image.ImpressionType = record.GetFieldValue("15.003");

            // Field 15.004 - Source Agency
            var src = record.GetFieldValue("15.004");
            if (!string.IsNullOrEmpty(src))
                image.SourceAgency = src;

            // Field 15.013 - Palm Position
            var palmPosition = record.GetFieldValue("15.013");
            if (!string.IsNullOrEmpty(palmPosition))
                image.AdditionalMetadata["PalmPosition"] = palmPosition!;

            // Compression
            var compression = record.GetFieldValue("15.011");
            if (!string.IsNullOrEmpty(compression))
            {
                image.CompressionCode = compression;
                image.Compression = ParseCompressionCode(compression);
            }
        }

        /// <summary>
        /// Type-17 (Iris) specifikus metaadatok.
        /// </summary>
        private static void ExtractType17Metadata(NistRecord record, BiometricImage image)
        {
            // Field 17.003 - Feature Identifier
            var featureId = record.GetFieldValue("17.003");
            if (!string.IsNullOrEmpty(featureId))
                image.AdditionalMetadata["FeatureIdentifier"] = featureId!;

            // Field 17.004 - Source Agency
            var src = record.GetFieldValue("17.004");
            if (!string.IsNullOrEmpty(src))
                image.SourceAgency = src;

            // Field 17.005 - Eye Color
            var eyeColor = record.GetFieldValue("17.005");
            if (!string.IsNullOrEmpty(eyeColor))
                image.AdditionalMetadata["EyeColor"] = eyeColor!;

            // Compression (17.011)
            var compression = record.GetFieldValue("17.011");
            if (!string.IsNullOrEmpty(compression))
            {
                image.CompressionCode = compression;
                image.Compression = ParseCompressionCode(compression);
            }
        }

        /// <summary>
        /// Type-4 (High-Res Grayscale Fingerprint) specifikus metaadatok.
        /// Pure binary rekord, fix fejléc struktúrával.
        /// </summary>
        private static void ExtractType4Metadata(NistRecord record, BiometricImage image)
        {
            // Type-4 has specific field positions in binary format
            // Try to use parsed fields if available
            image.FingerPosition = record.GetFieldValue("4.004");
            image.ImpressionType = record.GetFieldValue("4.003");

            var hll = record.GetFieldValue("4.006");
            if (int.TryParse(hll, out int width))
                image.Width = width;

            var vll = record.GetFieldValue("4.007");
            if (int.TryParse(vll, out int height))
                image.Height = height;

            // Type-4 is typically WSQ compressed
            var compression = record.GetFieldValue("4.008");
            if (!string.IsNullOrEmpty(compression))
            {
                image.CompressionCode = compression;
                image.Compression = ParseCompressionCode(compression);
            }
        }

        /// <summary>
        /// Visszaadja a Type-10 IMAGE TYPE mező emberi olvasható leírását.
        /// </summary>
        private static string GetFaceImageTypeDescription(string imt)
        {
            var imtUpper = imt.Trim().ToUpperInvariant();
            switch (imtUpper)
            {
                case "FACE": return "Arckép / Face";
                case "SCAR": return "Heg / Scar";
                case "MARK": return "Jegy / Mark";
                case "TATTOO": return "Tetoválás / Tattoo";
                case "PHOTO": return "Fénykép / Photograph";
                default: return imt;
            }
        }
    }
}
