using NistParser.Constants;
using NistParser.Core;

namespace NistParser.Records;

/// <summary>
/// Type-1 Transaction Information Record (MANDATORY)
/// Contains metadata about the entire transaction and lists all records in the file.
/// </summary>
public class Type1Record : NistRecord
{
    /// <summary>
    /// Initializes a new instance of the Type1Record class
    /// </summary>
    public Type1Record()
    {
        RecordType = RecordType.Type1_TransactionInformation;
        IDC = "00"; // Type-1 always has IDC of 00
    }

    /// <summary>
    /// Field 1.002 - Version Number (VER)
    /// Format: "XXYY" where XX=major, YY=minor (e.g., "0502" for version 5.02)
    /// </summary>
    public string? Version => GetFieldValue("1.002");

    /// <summary>
    /// Field 1.003 - Transaction Content (CNT)
    /// Lists the record types included in this transaction
    /// </summary>
    public TransactionContent? Content { get; set; }

    /// <summary>
    /// Field 1.004 - Type of Transaction (TOT)
    /// Identifies the transaction type (e.g., "CRM" for criminal)
    /// </summary>
    public string? TypeOfTransaction => GetFieldValue("1.004");

    /// <summary>
    /// Field 1.005 - Date (DAT)
    /// Transaction date in YYYYMMDD format
    /// </summary>
    public string? DateString => GetFieldValue("1.005");

    /// <summary>
    /// Gets the transaction date as a DateTime (parsed from field 1.005)
    /// </summary>
    public DateTime? Date
    {
        get
        {
            if (string.IsNullOrEmpty(DateString) || DateString.Length != 8)
                return null;

            if (int.TryParse(DateString.Substring(0, 4), out int year) &&
                int.TryParse(DateString.Substring(4, 2), out int month) &&
                int.TryParse(DateString.Substring(6, 2), out int day))
            {
                try
                {
                    return new DateTime(year, month, day);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Field 1.006 - Priority (PRY)
    /// Processing priority (1-9, where 1 is highest)
    /// </summary>
    public string? Priority => GetFieldValue("1.006");

    /// <summary>
    /// Field 1.007 - Destination Agency Identifier (DAI)
    /// Identifier of the receiving agency
    /// </summary>
    public string? DestinationAgencyIdentifier => GetFieldValue("1.007");

    /// <summary>
    /// Field 1.008 - Originating Agency Identifier (ORI)
    /// Identifier of the sending agency
    /// </summary>
    public string? OriginatingAgencyIdentifier => GetFieldValue("1.008");

    /// <summary>
    /// Field 1.009 - Transaction Control Number (TCN)
    /// Unique identifier for this transaction
    /// </summary>
    public string? TransactionControlNumber => GetFieldValue("1.009");

    /// <summary>
    /// Field 1.011 - Native Scanning Resolution (NSR)
    /// Resolution at which biometric data was originally captured
    /// </summary>
    public string? NativeScanningResolution => GetFieldValue("1.011");

    /// <summary>
    /// Field 1.012 - Nominal Transmitting Resolution (NTR)
    /// Resolution of the transmitted biometric data
    /// </summary>
    public string? NominalTransmittingResolution => GetFieldValue("1.012");

    /// <summary>
    /// Returns a string representation of this Type-1 record
    /// </summary>
    public override string ToString()
    {
        return $"Type-1 Record (Version {Version}, TCN: {TransactionControlNumber}, " +
               $"{Content?.RecordCount ?? 0} additional records)";
    }
}

/// <summary>
/// Represents the parsed Transaction Content (Field 1.003)
/// Lists all records in the transaction with their IDC values
/// </summary>
public class TransactionContent
{
    /// <summary>
    /// Gets or sets the total number of records (excluding Type-1)
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Gets or sets the list of record entries
    /// </summary>
    public List<RecordEntry> Records { get; set; } = new();

    /// <summary>
    /// Represents a single record entry in the CNT field
    /// </summary>
    public class RecordEntry
    {
        /// <summary>
        /// Gets or sets the record type number (2-99)
        /// </summary>
        public int RecordType { get; set; }

        /// <summary>
        /// Gets or sets the Information Designation Character
        /// </summary>
        public string IDC { get; set; } = "00";

        /// <summary>
        /// Returns a string representation
        /// </summary>
        public override string ToString() => $"Type-{RecordType} (IDC: {IDC})";
    }

    /// <summary>
    /// Returns a string representation
    /// </summary>
    public override string ToString()
    {
        return $"{RecordCount} records: {string.Join(", ", Records)}";
    }
}
