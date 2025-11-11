using NistParser.Records;

namespace NistParser.Core;

/// <summary>
/// Represents a complete ANSI/NIST-ITL transaction (a parsed NIST file).
/// A transaction contains a mandatory Type-1 header and zero or more additional records.
/// </summary>
public class NistTransaction
{
    /// <summary>
    /// Gets or sets the Type-1 Transaction Information record (MANDATORY)
    /// </summary>
    public Type1Record Header { get; set; } = null!;

    /// <summary>
    /// Gets or sets all records in the transaction (excluding Type-1)
    /// </summary>
    public List<NistRecord> Records { get; set; } = new();

    /// <summary>
    /// Gets or sets the raw file data (optional, for reference)
    /// </summary>
    public byte[]? RawData { get; set; }

    /// <summary>
    /// Gets the total number of records in the transaction (including Type-1)
    /// </summary>
    public int TotalRecordCount => Records.Count + 1;

    /// <summary>
    /// Gets the version of the ANSI/NIST-ITL standard used (from field 1.002)
    /// </summary>
    public string? Version => Header?.Version;

    /// <summary>
    /// Gets the Transaction Control Number (from field 1.009)
    /// </summary>
    public string? TransactionControlNumber => Header?.TransactionControlNumber;

    /// <summary>
    /// Gets the Originating Agency Identifier (from field 1.008)
    /// </summary>
    public string? OriginatingAgencyIdentifier => Header?.OriginatingAgencyIdentifier;

    /// <summary>
    /// Gets records of a specific type
    /// </summary>
    /// <typeparam name="T">The record type</typeparam>
    /// <returns>All records of the specified type</returns>
    public IEnumerable<T> GetRecords<T>() where T : NistRecord
    {
        return Records.OfType<T>();
    }

    /// <summary>
    /// Gets records with a specific IDC value
    /// </summary>
    /// <param name="idc">The IDC to filter by</param>
    /// <returns>All records with the specified IDC</returns>
    public IEnumerable<NistRecord> GetRecordsByIDC(string idc)
    {
        return Records.Where(r => r.IDC == idc);
    }

    /// <summary>
    /// Gets a record of a specific type with a specific IDC
    /// </summary>
    /// <typeparam name="T">The record type</typeparam>
    /// <param name="idc">The IDC value</param>
    /// <returns>The first matching record, or null if not found</returns>
    public T? GetRecord<T>(string idc) where T : NistRecord
    {
        return Records.OfType<T>().FirstOrDefault(r => r.IDC == idc);
    }

    /// <summary>
    /// Returns a summary of the transaction
    /// </summary>
    public override string ToString()
    {
        return $"NIST Transaction (Version {Version}, TCN: {TransactionControlNumber}), " +
               $"{TotalRecordCount} records";
    }

    /// <summary>
    /// Gets a detailed summary of all records in the transaction
    /// </summary>
    /// <returns>Formatted summary string</returns>
    public string GetDetailedSummary()
    {
        var summary = new System.Text.StringBuilder();
        summary.AppendLine($"ANSI/NIST-ITL Transaction");
        summary.AppendLine($"Version: {Version}");
        summary.AppendLine($"TCN: {TransactionControlNumber}");
        summary.AppendLine($"ORI: {OriginatingAgencyIdentifier}");
        summary.AppendLine($"Date: {Header?.Date}");
        summary.AppendLine($"Total Records: {TotalRecordCount}");
        summary.AppendLine();
        summary.AppendLine("Records:");
        summary.AppendLine($"  Type-1 (Header): {Header}");

        foreach (var record in Records)
        {
            summary.AppendLine($"  {record}");
        }

        return summary.ToString();
    }
}
