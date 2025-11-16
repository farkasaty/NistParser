using System;

namespace NistParser.Constants
{
    /// <summary>
    /// Provides human-readable descriptions and meanings for ANSI/NIST-ITL field numbers
    /// </summary>
    public static class FieldDescriptions
    {
        /// <summary>
        /// Gets a human-readable description for a field number
        /// </summary>
        /// <param name="fieldNumber">Field number (e.g., "1.002", "14.999")</param>
        /// <returns>Description string, or the field number if no description is available</returns>
        public static string GetDescription(string fieldNumber)
        {
            // First check FieldMetadataProvider for configurable fields (especially Type-2)
            var metadata = Editing.FieldMetadataProvider.GetFieldMetadata(fieldNumber);
            if (metadata != null && !string.IsNullOrWhiteSpace(metadata.DisplayName))
            {
                return metadata.DisplayName;
            }

            // Fallback to hardcoded descriptions
            return fieldNumber switch
            {
                // Type-1 Fields
                "1.001" => "LEN - Logical Record Length",
                "1.002" => "VER - Version Number",
                "1.003" => "CNT - Transaction Content",
                "1.004" => "TOT - Type of Transaction",
                "1.005" => "DAT - Date",
                "1.006" => "PRY - Priority",
                "1.007" => "DAI - Destination Agency Identifier",
                "1.008" => "ORI - Originating Agency Identifier",
                "1.009" => "TCN - Transaction Control Number",
                "1.010" => "TCR - Transaction Control Reference",
                "1.011" => "NSR - Native Scanning Resolution",
                "1.012" => "NTR - Nominal Transmitting Resolution",
                "1.013" => "DOM - Domain Name",
                "1.014" => "GMT - Greenwich Mean Time",
                "1.015" => "DCS - Directory of Character Sets",
                "1.016" => "APS - Application Profile Specifications",
                "1.017" => "OAN - Originating Agency Name",

                // Type-2 Fields
                "2.001" => "LEN - Logical Record Length",
                "2.002" => "IDC - Information Designation Character",
                // Type-2 can have many user-defined fields 2.003-2.999

                // Type-10 Fields (Face)
                "10.001" => "LEN - Logical Record Length",
                "10.002" => "IDC - Information Designation Character",
                "10.003" => "IMT - Image Type",
                "10.004" => "SRC - Source Agency",
                "10.005" => "PHD - Photo Description",
                "10.006" => "HLL - Horizontal Line Length",
                "10.007" => "VLL - Vertical Line Length",
                "10.008" => "SLC - Scale Units",
                "10.009" => "THPS - Transmitted Horizontal Pixel Scale",
                "10.010" => "TVPS - Transmitted Vertical Pixel Scale",
                "10.011" => "CGA - Compression Algorithm",
                "10.012" => "BPX - Bits Per Pixel",
                "10.020" => "SHPS - Scanned Horizontal Pixel Scale",
                "10.021" => "SVPS - Scanned Vertical Pixel Scale",
                "10.999" => "DATA - Image Data",

                // Type-13 Fields (Latent)
                "13.001" => "LEN - Logical Record Length",
                "13.002" => "IDC - Information Designation Character",
                "13.003" => "IMP - Impression Type",
                "13.004" => "SRC - Source Agency",
                "13.005" => "LCD - Latent Capture Date",
                "13.006" => "HLL - Horizontal Line Length",
                "13.007" => "VLL - Vertical Line Length",
                "13.008" => "SLC - Scale Units",
                "13.009" => "THPS - Transmitted Horizontal Pixel Scale",
                "13.010" => "TVPS - Transmitted Vertical Pixel Scale",
                "13.011" => "CGA - Compression Algorithm",
                "13.012" => "BPX - Bits Per Pixel",
                "13.999" => "DATA - Image Data",

                // Type-14 Fields (Fingerprint)
                "14.001" => "LEN - Logical Record Length",
                "14.002" => "IDC - Information Designation Character",
                "14.003" => "IMP - Impression Type",
                "14.004" => "SRC - Source Agency",
                "14.005" => "FGP - Finger Position",
                "14.006" => "ISR - Image Scanning Resolution",
                "14.007" => "HLL - Horizontal Line Length",
                "14.008" => "VLL - Vertical Line Length",
                "14.009" => "CA - Compression Algorithm",
                "14.010" => "BPX - Bits Per Pixel",
                "14.011" => "FGP2 - Finger Position(s)",
                "14.012" => "FQM - Finger Quality Metric",
                "14.013" => "ASEG - Alternate Finger Segment Position(s)",
                "14.014" => "NIST2 - NIST Quality Metric",
                "14.015" => "SQS - Segmentation Quality Score",
                "14.020" => "COM - Comment",
                "14.021" => "SEG - Finger Segment Position(s)",
                "14.022" => "NQM - NIST Quality Metric",
                "14.023" => "SQM - Segmentation Quality Metric",
                "14.024" => "FQM2 - Finger Quality Metric (newer version)",
                "14.030" => "DMM - Device Monitoring Mode",
                "14.999" => "DATA - Image Data",

                // Type-17 Fields (Iris)
                "17.001" => "LEN - Logical Record Length",
                "17.002" => "IDC - Information Designation Character",
                "17.003" => "FID - Feature Identifier",
                "17.999" => "DATA - Image Data",

                // Default
                _ => fieldNumber
            };
        }

        /// <summary>
        /// Gets the short mnemonic for a field number
        /// </summary>
        public static string GetMnemonic(string fieldNumber)
        {
            var description = GetDescription(fieldNumber);
            if (description == fieldNumber)
            return string.Empty;

            // Extract mnemonic (text before " - ")
            int dashIndex = description.IndexOf(" - ");
            return dashIndex > 0 ? description.Substring(0, dashIndex) : string.Empty;
        }

        /// <summary>
        /// Gets the long description for a field number (without mnemonic)
        /// </summary>
        public static string GetLongDescription(string fieldNumber)
        {
            var description = GetDescription(fieldNumber);
            if (description == fieldNumber)
            return string.Empty;

            // Extract long description (text after " - ")
            int dashIndex = description.IndexOf(" - ");
            return dashIndex > 0 ? description.Substring(dashIndex + 3) : description;
        }

        /// <summary>
        /// Gets a human-readable interpretation of a field value
        /// </summary>
        public static string GetValueInterpretation(string fieldNumber, string value)
        {
            return fieldNumber switch
            {
                // Version interpretation
                "1.002" when value.Length == 4 => $"Version {value.Substring(0, 2)}.{value.Substring(2, 2)}",

                // Transaction types (TOT - 1.004)
                "1.004" => value switch
                {
                    "CRM" => "Criminal (CRM)",
                    "CIV" => "Civil (CIV)",
                    "AMN" => "Administrative (AMN)",
                    "CAR" => "Applicant (CAR)",
                    _ => value
                },

                // Impression types (IMP - X.003)
                "13.003" or "14.003" => value switch
                {
                    "0" => "Live-scan plain (0)",
                    "1" => "Live-scan rolled (1)",
                    "2" => "Nonlive-scan plain (2)",
                    "3" => "Nonlive-scan rolled (3)",
                    "4" => "Latent impression (4)",
                    "5" => "Latent tracing (5)",
                    "6" => "Latent photo (6)",
                    "7" => "Latent lift (7)",
                    "8" => "Swipe (8)",
                    "9" => "Unknown (9)",
                    _ => value
                },

                // Finger positions (FGP - 14.005)
                "14.005" => value switch
                {
                    "0" => "Unknown (0)",
                    "1" => "Right thumb (1)",
                    "2" => "Right index (2)",
                    "3" => "Right middle (3)",
                    "4" => "Right ring (4)",
                    "5" => "Right little (5)",
                    "6" => "Left thumb (6)",
                    "7" => "Left index (7)",
                    "8" => "Left middle (8)",
                    "9" => "Left ring (9)",
                    "10" => "Left little (10)",
                    "11" => "Right four fingers (11)",
                    "12" => "Left four fingers (12)",
                    "13" => "Both thumbs (13)",
                    _ => value
                },

                // Compression algorithms (CA - 14.009, CGA - 10.011/13.011)
                "14.009" or "10.011" or "13.011" => value switch
                {
                    "NONE" => "Uncompressed (NONE)",
                    "WSQ" => "WSQ - Wavelet Scalar Quantization",
                    "JPEGB" => "JPEG Baseline",
                    "JPEGL" => "JPEG Lossless",
                    "JP2" => "JPEG 2000",
                    "JP2L" => "JPEG 2000 Lossless",
                    "PNG" => "PNG - Portable Network Graphics",
                    _ => value
                },

                // Scale units (SLC - X.008)
                "10.008" or "13.008" => value switch
                {
                    "0" => "None (0)",
                    "1" => "Pixels per inch (1)",
                    "2" => "Pixels per cm (2)",
                    _ => value
                },

                // Resolution interpretation (ISR, NSR, NTR)
                "14.006" or "1.011" or "1.012" when double.TryParse(value, out double res) && res > 0
                => $"{res} ppi",

                // Date interpretation (DAT - 1.005)
                "1.005" when value.Length == 8 && int.TryParse(value, out _)
                => $"{value.Substring(0, 4)}-{value.Substring(4, 2)}-{value.Substring(6, 2)}",

                // Priority (PRY - 1.006)
                "1.006" => value switch
                {
                    "1" => "Highest priority (1)",
                    "2" => "High (2)",
                    "3" => "Medium-high (3)",
                    "4" => "Medium (4)",
                    "5" => "Medium-low (5)",
                    "6" => "Low (6)",
                    "7" => "Lower (7)",
                    "8" => "Lowest (8)",
                    "9" => "Background (9)",
                    _ => value
                },

                // Image type (IMT - 10.003)
                "10.003" => value switch
                {
                    "FACE" => "Facial image (FACE)",
                    "SCAR" => "Scar (SCAR)",
                    "MARK" => "Mark (MARK)",
                    "TATTOO" => "Tattoo (TATTOO)",
                    _ => value
                },

                // Default - return value as-is
                _ => value
            };
        }
    }
}
