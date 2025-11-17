using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NistParser.Core;
using NistParser.Writing;
using Xunit;
using Xunit.Abstractions;

namespace NistParser.Tests
{
    public class FileComparisonTests
    {
        private readonly ITestOutputHelper _output;

        public FileComparisonTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CompareOriginalAndCopy_DetailedAnalysis()
        {
            // Arrange
            string baseDir = @"c:\Users\farka\source\repos\NistParser";
            string originalFile = Path.Combine(baseDir, "example files", "hibas", "orig_SIS_ATM_C_P25.nist");
            string copyFile = Path.Combine(baseDir, "example files", "hibas", "orig_SIS_ATM_C_P25 - Copy.nist");

            if (!File.Exists(originalFile) || !File.Exists(copyFile))
            {
                _output.WriteLine($"Files not found:");
                _output.WriteLine($"  Original: {originalFile}");
                _output.WriteLine($"  Copy: {copyFile}");
                return;
            }

            byte[] originalBytes = File.ReadAllBytes(originalFile);
            byte[] copyBytes = File.ReadAllBytes(copyFile);

            _output.WriteLine($"Original file size: {originalBytes.Length}");
            _output.WriteLine($"Copy file size: {copyBytes.Length}");
            _output.WriteLine($"Difference: {originalBytes.Length - copyBytes.Length} bytes");
            _output.WriteLine("");

            // Parse both files
            var originalTransaction = NistTransactionParser.Parse(originalBytes);
            var copyTransaction = NistTransactionParser.Parse(copyBytes);

            _output.WriteLine("=== ORIGINAL FILE ===");
            AnalyzeTransaction(originalTransaction);

            _output.WriteLine("");
            _output.WriteLine("=== COPY FILE ===");
            AnalyzeTransaction(copyTransaction);

            _output.WriteLine("");
            _output.WriteLine("=== COMPARISON ===");

            // Compare each record
            for (int i = 0; i < Math.Min(originalTransaction.Records.Count, copyTransaction.Records.Count); i++)
            {
                var origRec = originalTransaction.Records[i];
                var copyRec = copyTransaction.Records[i];

                if (origRec.RecordLength != copyRec.RecordLength)
                {
                    _output.WriteLine($"Record {i} Type-{(int)origRec.RecordType}:");
                    _output.WriteLine($"  Original length: {origRec.RecordLength}");
                    _output.WriteLine($"  Copy length: {copyRec.RecordLength}");
                    _output.WriteLine($"  Difference: {origRec.RecordLength - copyRec.RecordLength}");

                    // Check binary field
                    var origField999 = origRec.GetField($"{(int)origRec.RecordType}.999");
                    var copyField999 = copyRec.GetField($"{(int)copyRec.RecordType}.999");

                    if (origField999 != null)
                    {
                        _output.WriteLine($"  Original 999 field:");
                        _output.WriteLine($"    BinaryData length: {origField999.BinaryData?.Length ?? 0}");
                        _output.WriteLine($"    Subfields count: {origField999.Subfields.Count}");
                        foreach (var sf in origField999.Subfields)
                        {
                            _output.WriteLine($"      Subfield items: {sf.Items.Count}");
                            foreach (var item in sf.Items)
                            {
                                _output.WriteLine($"        Item length: {item.Length}, first 20 chars: '{(item.Length > 20 ? item.Substring(0, 20) : item)}'");
                            }
                        }
                    }

                    if (copyField999 != null)
                    {
                        _output.WriteLine($"  Copy 999 field:");
                        _output.WriteLine($"    BinaryData length: {copyField999.BinaryData?.Length ?? 0}");
                        _output.WriteLine($"    Subfields count: {copyField999.Subfields.Count}");
                        foreach (var sf in copyField999.Subfields)
                        {
                            _output.WriteLine($"      Subfield items: {sf.Items.Count}");
                            foreach (var item in sf.Items)
                            {
                                _output.WriteLine($"        Item length: {item.Length}, first 20 chars: '{(item.Length > 20 ? item.Substring(0, 20) : item)}'");
                            }
                        }
                    }
                }
            }

            // Now re-serialize the ORIGINAL and see if it matches
            _output.WriteLine("");
            _output.WriteLine("=== RE-SERIALIZATION TEST ===");
            byte[] reserializedBytes = NistTransactionWriter.WriteTraditional(originalTransaction);
            _output.WriteLine($"Original file size: {originalBytes.Length}");
            _output.WriteLine($"Re-serialized size: {reserializedBytes.Length}");
            _output.WriteLine($"Match: {originalBytes.Length == reserializedBytes.Length}");

            if (originalBytes.Length != reserializedBytes.Length)
            {
                _output.WriteLine($"MISMATCH! Difference: {originalBytes.Length - reserializedBytes.Length} bytes");

                // Find first difference
                int minLen = Math.Min(originalBytes.Length, reserializedBytes.Length);
                for (int i = 0; i < minLen; i++)
                {
                    if (originalBytes[i] != reserializedBytes[i])
                    {
                        _output.WriteLine($"First difference at byte {i} (0x{i:X}):");
                        _output.WriteLine($"  Original: 0x{originalBytes[i]:X2}");
                        _output.WriteLine($"  Re-serialized: 0x{reserializedBytes[i]:X2}");
                        break;
                    }
                }
            }
        }

        private void AnalyzeTransaction(NistTransaction transaction)
        {
            _output.WriteLine($"Header (Type-1) length: {transaction.Header.RecordLength}");
            _output.WriteLine($"Total records: {transaction.Records.Count}");

            foreach (var record in transaction.Records)
            {
                _output.WriteLine($"  Type-{(int)record.RecordType} IDC={record.IDC} Length={record.RecordLength}");

                var field999 = record.GetField($"{(int)record.RecordType}.999");
                if (field999 != null)
                {
                    _output.WriteLine($"    Has .999 field: BinaryData={field999.BinaryData?.Length ?? 0} bytes, Subfields={field999.Subfields.Count}");
                }
            }
        }
    }
}
