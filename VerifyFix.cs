using System;
using System.IO;
using System.Linq;
using NistParser;

class VerifyFix
{
    static void Main()
    {
        var origBytes = File.ReadAllBytes("orig_SIS_MMS_C_P25M.nist");
        var modBytes = File.ReadAllBytes("orig_SIS_MMS_C_P25M_modified.nist");

        Console.WriteLine("=== ORIGINAL FILE ===");
        var tx1 = NistTransactionParser.Parse(origBytes);
        var type9_1 = tx1.Records.FirstOrDefault(r => (int)r.RecordType == 9);
        var field9137_1 = type9_1?.GetField("9.137");

        if (field9137_1 != null)
        {
            Console.WriteLine($"Field 9.137:");
            Console.WriteLine($"  Subfields: {field9137_1.Subfields.Count}");
            if (field9137_1.Subfields.Count > 0)
            {
                Console.WriteLine($"  Subfield[0] Items: {field9137_1.Subfields[0].Items.Count}");
                Console.WriteLine($"  First 5 items: [{string.Join(", ", field9137_1.Subfields[0].Items.Take(5))}]");
            }
        }

        Console.WriteLine("\n=== MODIFIED FILE ===");
        var tx2 = NistTransactionParser.Parse(modBytes);
        var type9_2 = tx2.Records.FirstOrDefault(r => (int)r.RecordType == 9);
        var field9137_2 = type9_2?.GetField("9.137");

        if (field9137_2 != null)
        {
            Console.WriteLine($"Field 9.137:");
            Console.WriteLine($"  Subfields: {field9137_2.Subfields.Count}");
            if (field9137_2.Subfields.Count > 0)
            {
                Console.WriteLine($"  Subfield[0] Items: {field9137_2.Subfields[0].Items.Count}");
                Console.WriteLine($"  First 5 items: [{string.Join(", ", field9137_2.Subfields[0].Items.Take(5))}]");
            }
        }

        Console.WriteLine($"\n=== FILE SIZE COMPARISON ===");
        Console.WriteLine($"Original: {origBytes.Length} bytes");
        Console.WriteLine($"Modified: {modBytes.Length} bytes");
        Console.WriteLine($"Difference: {modBytes.Length - origBytes.Length} bytes");

        if (field9137_1 != null && field9137_2 != null)
        {
            bool itemCountMatch = field9137_1.Subfields[0].Items.Count == field9137_2.Subfields[0].Items.Count;
            Console.WriteLine($"\n=== RESULT ===");
            Console.WriteLine(itemCountMatch ? "✓ Field 9.137 structure PRESERVED!" : "✗ Field 9.137 structure LOST!");
        }
    }
}
