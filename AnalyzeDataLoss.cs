using System;
using System.IO;
using System.Linq;
using System.Text;
using NistParser;

class AnalyzeDataLoss
{
    static void Main()
    {
        string origFile = @"example files\hibas\orig_SIS_MMS_C_P25M.nist";
        string modFile = @"example files\hibas\orig_SIS_MMS_C_P25M_modified.nist";

        Console.WriteLine("=== FILE SIZES ===");
        var origBytes = File.ReadAllBytes(origFile);
        var modBytes = File.ReadAllBytes(modFile);
        Console.WriteLine($"Original: {origBytes.Length} bytes");
        Console.WriteLine($"Modified: {modBytes.Length} bytes");
        Console.WriteLine($"Loss: {origBytes.Length - modBytes.Length} bytes\n");

        Console.WriteLine("=== PARSING ORIGINAL ===");
        var origTx = NistTransactionParser.Parse(origBytes);
        AnalyzeField(origTx, "1.003", "CNT");
        AnalyzeField(origTx, "9.137", "Type-9 field");

        Console.WriteLine("\n=== PARSING MODIFIED ===");
        var modTx = NistTransactionParser.Parse(modBytes);
        AnalyzeField(modTx, "1.003", "CNT");
        AnalyzeField(modTx, "9.137", "Type-9 field");

        Console.WriteLine("\n=== RE-SERIALIZING ORIGINAL (should match) ===");
        var reserBytes = NistParser.Writing.NistTransactionWriter.WriteTraditional(origTx);
        Console.WriteLine($"Re-serialized: {reserBytes.Length} bytes");
        Console.WriteLine($"Match: {reserBytes.Length == origBytes.Length}");

        if (reserBytes.Length != origBytes.Length)
        {
            Console.WriteLine($"MISMATCH! Difference: {origBytes.Length - reserBytes.Length} bytes");

            // Re-parse to see what changed
            var reParsed = NistTransactionParser.Parse(reserBytes);
            Console.WriteLine("\n=== AFTER RE-SERIALIZATION ===");
            AnalyzeField(reParsed, "9.137", "Type-9 field");
        }
    }

    static void AnalyzeField(NistTransaction tx, string fieldNum, string description)
    {
        Console.WriteLine($"\n{description} ({fieldNum}):");

        NistParser.Core.NistField field = null;

        if (fieldNum.StartsWith("1."))
        {
            field = tx.Header.GetField(fieldNum);
        }
        else
        {
            int recordType = int.Parse(fieldNum.Split('.')[0]);
            var record = tx.Records.FirstOrDefault(r => (int)r.RecordType == recordType);
            if (record != null)
            {
                field = record.GetField(fieldNum);
            }
        }

        if (field == null)
        {
            Console.WriteLine("  FIELD NOT FOUND!");
            return;
        }

        Console.WriteLine($"  Subfields: {field.Subfields.Count}");
        for (int i = 0; i < Math.Min(5, field.Subfields.Count); i++)
        {
            var sf = field.Subfields[i];
            Console.WriteLine($"    Subfield[{i}]: {sf.Items.Count} items");
            for (int j = 0; j < Math.Min(10, sf.Items.Count); j++)
            {
                var item = sf.Items[j];
                var display = item.Length > 30 ? item.Substring(0, 30) + "..." : item;
                Console.WriteLine($"      [{j}] = '{display}'");
            }
        }

        if (field.Subfields.Count > 5)
        {
            Console.WriteLine($"    ... and {field.Subfields.Count - 5} more subfields");
        }
    }
}
