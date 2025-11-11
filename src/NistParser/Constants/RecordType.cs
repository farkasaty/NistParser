namespace NistParser.Constants;

/// <summary>
/// Defines the record types in ANSI/NIST-ITL standard.
/// Each transaction must have a Type-1 record, and may contain any combination of other record types.
/// </summary>
public enum RecordType
{
    /// <summary>
    /// Type-1: Transaction Information Record (MANDATORY)
    /// Contains transaction metadata and lists all records in the file
    /// </summary>
    Type1_TransactionInformation = 1,

    /// <summary>
    /// Type-2: User-Defined Descriptive Text Record
    /// Contains biographic and descriptive data (user-defined fields)
    /// </summary>
    Type2_UserDefinedText = 2,

    /// <summary>
    /// Type-3: Low-Resolution Grayscale Fingerprint (DEPRECATED)
    /// Binary record for low-resolution grayscale fingerprint images
    /// </summary>
    Type3_LowResGrayscaleFingerprint = 3,

    /// <summary>
    /// Type-4: High-Resolution Grayscale Fingerprint
    /// Binary record for high-resolution fingerprints (typically WSQ compressed)
    /// </summary>
    Type4_HighResGrayscaleFingerprint = 4,

    /// <summary>
    /// Type-5: Low-Resolution Binary Fingerprint (DEPRECATED)
    /// Binary record for low-resolution binary (black and white) fingerprints
    /// </summary>
    Type5_LowResBinaryFingerprint = 5,

    /// <summary>
    /// Type-6: High-Resolution Binary Fingerprint (DEPRECATED)
    /// Binary record for high-resolution binary fingerprints
    /// </summary>
    Type6_HighResBinaryFingerprint = 6,

    /// <summary>
    /// Type-7: User-Defined Image Record
    /// Binary record for user-defined grayscale images
    /// </summary>
    Type7_UserDefinedImage = 7,

    /// <summary>
    /// Type-8: Signature Image Record
    /// Binary record for digitized signature data
    /// </summary>
    Type8_SignatureImage = 8,

    /// <summary>
    /// Type-9: Minutiae Data Record
    /// ASCII tagged record containing fingerprint minutiae and extended feature sets
    /// </summary>
    Type9_MinutiaeData = 9,

    /// <summary>
    /// Type-10: Facial and SMT Image Record
    /// Mixed record for facial images, scars, marks, and tattoos
    /// </summary>
    Type10_FacialAndSMTImage = 10,

    /// <summary>
    /// Type-11: Voice Data Record (RESERVED)
    /// Reserved for voice recording data
    /// </summary>
    Type11_VoiceData = 11,

    /// <summary>
    /// Type-12: Dental Records (RESERVED)
    /// Reserved for dental biometric data
    /// </summary>
    Type12_DentalRecords = 12,

    /// <summary>
    /// Type-13: Friction Ridge Latent Image Record
    /// Mixed record for latent fingerprint images from crime scenes
    /// </summary>
    Type13_LatentImage = 13,

    /// <summary>
    /// Type-14: Fingerprint Image Record
    /// Mixed record for variable resolution fingerprint images (500/1000 ppi recommended)
    /// </summary>
    Type14_FingerprintImage = 14,

    /// <summary>
    /// Type-15: Palm Print Image Record
    /// Mixed record for palm print images (500/1000 ppi recommended)
    /// </summary>
    Type15_PalmPrintImage = 15,

    /// <summary>
    /// Type-16: User-Defined Testing Image Record
    /// Mixed record for testing purposes
    /// </summary>
    Type16_UserDefinedTestingImage = 16,

    /// <summary>
    /// Type-17: Iris Image Record
    /// Mixed record for iris biometric images
    /// </summary>
    Type17_IrisImage = 17,

    /// <summary>
    /// Type-18: DNA Data Record
    /// ASCII tagged record for DNA profile information
    /// </summary>
    Type18_DNAData = 18,

    /// <summary>
    /// Type-19: Plantar (Footprint) Image Record
    /// Mixed record for footprint images
    /// </summary>
    Type19_PlantarImage = 19,

    /// <summary>
    /// Type-20: Source Representation Image Record
    /// Mixed record for source representation data
    /// </summary>
    Type20_SourceRepresentation = 20,

    /// <summary>
    /// Type-21: Associated Context Image Record
    /// Mixed record for associated context images
    /// </summary>
    Type21_AssociatedContext = 21,

    /// <summary>
    /// Type-22: Non-Photographic Imagery Record
    /// Mixed record for non-photographic images
    /// </summary>
    Type22_NonPhotographicImagery = 22,

    /// <summary>
    /// Type-98: Information Assurance Record
    /// ASCII tagged record for digital signatures and hashing
    /// </summary>
    Type98_InformationAssurance = 98,

    /// <summary>
    /// Type-99: CBEFF Biometric Data Record
    /// Mixed record for Common Biometric Exchange Formats Framework data
    /// </summary>
    Type99_CBEFFBiometricData = 99
}
