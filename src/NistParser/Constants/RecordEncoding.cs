using System;

namespace NistParser.Constants
{
    /// <summary>
    /// Defines the encoding type for ANSI/NIST-ITL records.
    /// Different record types use different encoding strategies.
    /// </summary>
    public enum RecordEncoding
    {
        /// <summary>
        /// ASCII Tagged-Field encoding
        /// Fields are identified by "X.YYY:data" format with separator characters
        /// Used by: Type-1, Type-2, Type-9, Type-18, Type-98
        /// </summary>
        TaggedASCII,

        /// <summary>
        /// Pure Binary encoding
        /// Fixed-length fields in defined order, no field identifiers
        /// Field 999 contains image data
        /// Used by: Type-3, Type-4, Type-5, Type-6, Type-8
        /// </summary>
        Binary,

        /// <summary>
        /// Mixed encoding (Tagged ASCII + Binary)
        /// ASCII tagged fields (001-998) followed by binary field 999 (image data)
        /// Used by: Type-10, Type-13, Type-14, Type-15, Type-16, Type-17, Type-19, Type-20, Type-21, Type-22, Type-99
        /// </summary>
        Mixed
    }

    /// <summary>
    /// Extension methods for RecordEncoding enum
    /// </summary>
    public static class RecordEncodingExtensions
    {
        /// <summary>
        /// Gets the encoding type for a specific record type
        /// </summary>
        /// <param name="recordType">The record type</param>
        /// <returns>The encoding type used by that record</returns>
        public static RecordEncoding GetEncoding(RecordType recordType) => recordType switch
        {
            // ASCII Tagged-Field records
            RecordType.Type1_TransactionInformation => RecordEncoding.TaggedASCII,
            RecordType.Type2_UserDefinedText => RecordEncoding.TaggedASCII,
            RecordType.Type9_MinutiaeData => RecordEncoding.TaggedASCII,
            RecordType.Type18_DNAData => RecordEncoding.TaggedASCII,
            RecordType.Type98_InformationAssurance => RecordEncoding.TaggedASCII,

            // Pure Binary records
            RecordType.Type3_LowResGrayscaleFingerprint => RecordEncoding.Binary,
            RecordType.Type4_HighResGrayscaleFingerprint => RecordEncoding.Binary,
            RecordType.Type5_LowResBinaryFingerprint => RecordEncoding.Binary,
            RecordType.Type6_HighResBinaryFingerprint => RecordEncoding.Binary,
            RecordType.Type7_UserDefinedImage => RecordEncoding.Binary,
            RecordType.Type8_SignatureImage => RecordEncoding.Binary,

            // Mixed records (Tagged fields + Binary field 999)
            RecordType.Type10_FacialAndSMTImage => RecordEncoding.Mixed,
            RecordType.Type11_VoiceData => RecordEncoding.Mixed,
            RecordType.Type12_DentalRecords => RecordEncoding.Mixed,
            RecordType.Type13_LatentImage => RecordEncoding.Mixed,
            RecordType.Type14_FingerprintImage => RecordEncoding.Mixed,
            RecordType.Type15_PalmPrintImage => RecordEncoding.Mixed,
            RecordType.Type16_UserDefinedTestingImage => RecordEncoding.Mixed,
            RecordType.Type17_IrisImage => RecordEncoding.Mixed,
            RecordType.Type19_PlantarImage => RecordEncoding.Mixed,
            RecordType.Type20_SourceRepresentation => RecordEncoding.Mixed,
            RecordType.Type21_AssociatedContext => RecordEncoding.Mixed,
            RecordType.Type22_NonPhotographicImagery => RecordEncoding.Mixed,
            RecordType.Type99_CBEFFBiometricData => RecordEncoding.Mixed,

            _ => throw new ArgumentException($"Unknown record type: {recordType}", nameof(recordType))
        };

        /// <summary>
        /// Determines if the record type uses separator characters (US, RS, GS, FS)
        /// </summary>
        /// <param name="encoding">The record encoding</param>
        /// <returns>True if separators are meaningful, false for pure binary</returns>
        public static bool UsesSeparators(this RecordEncoding encoding) =>
        encoding == RecordEncoding.TaggedASCII || encoding == RecordEncoding.Mixed;

        /// <summary>
        /// Determines if the record type contains binary image data
        /// </summary>
        /// <param name="encoding">The record encoding</param>
        /// <returns>True if contains binary data, false if pure ASCII</returns>
        public static bool ContainsBinaryData(this RecordEncoding encoding) =>
        encoding == RecordEncoding.Binary || encoding == RecordEncoding.Mixed;
    }
}
