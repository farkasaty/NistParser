using System;
using System.IO;
using System.Linq;
using NistParser;
using NistParser.Writing;

class TestDataLoss
{
    static void Main()
    {
        string origFile = @"example files\hibas\orig_SIS_MMS_C_P25M.nist";

        Console.WriteLine("=== TEST: Parse -> Write -> Parse ===");

        // Parse original
        var tx1 = NistTransactionParser.Parse(File.ReadAllBytes(origFile));
        var type9_1 = tx1.Records.FirstOrDefault(r => (int)r.RecordType == 9);
        var field9137_1 = type9_1?.GetField("9.137");

        if (field9137_1 != null)
        {
            Console.WriteLine($"\nORIGINAL Field 9.137:");
            Console.WriteLine($"  Subfields: {field9137_1.Subfields.Count}");
            if (field9137_1.Subfields.Count > 0)
            {
                Console.WriteLine($"  Subfield[0] Items: {field9137_1.Subfields[0].Items.Count}");
                Console.WriteLine($"  First 5 items: [{string.Join(", ", field9137_1.Subfields[0].Items.Take(5))}]");
            }
            Console.WriteLine($"  FirstValue: '{field9137_1.FirstValue}'");
        }

        // Write and re-parse
        byte[] newBytes = NistTransactionWriter.WriteTraditional(tx1);
        var tx2 = NistTransactionParser.Parse(newBytes);
        var type9_2 = tx2.Records.FirstOrDefault(r => (int)r.RecordType == 9);
        var field9137_2 = type9_2?.GetField("9.137");

        if (field9137_2 != null)
        {
            Console.WriteLine($"\nAFTER ROUNDTRIP Field 9.137:");
            Console.WriteLine($"  Subfields: {field9137_2.Subfields.Count}");
            if (field9137_2.Subfields.Count > 0)
            {
                Console.WriteLine($"  Subfield[0] Items: {field9137_2.Subfields[0].Items.Count}");
                Console.WriteLine($"  First 5 items: [{string.Join(", ", field9137_2.Subfields[0].Items.Take(5))}]");
            }
            Console.WriteLine($"  FirstValue: '{field9137_2.FirstValue}'");
        }

        // Check file sizes
        long origSize = new FileInfo(origFile).Length;
        Console.WriteLine($"\nFILE SIZES:");
        Console.WriteLine($"  Original: {origSize} bytes");
        Console.WriteLine($"  After roundtrip: {newBytes.Length} bytes");
        Console.WriteLine($"  Difference: {newBytes.Length - origSize} bytes");
    }
}
