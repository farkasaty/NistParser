using System;
using System.IO;
using System.Linq;
using NistParser;
using NistParser.Core;

class QuickTest
{
    static void Main()
    {
        // Test: What happens when we parse a field with US separators?
        var tx = NistTransactionParser.Parse(File.ReadAllBytes(@"example files\hibas\orig_SIS_MMS_C_P25M.nist"));

        var type9 = tx.Records.FirstOrDefault(r => (int)r.RecordType == 9);
        var field9137 = type9?.GetField("9.137");

        if (field9137 != null)
        {
            Console.WriteLine("=== ORIGINAL 9.137 ===");
            Console.WriteLine($"Subfields: {field9137.Subfields.Count}");
            foreach (var sf in field9137.Subfields.Take(3))
            {
                Console.WriteLine($"  Items: {sf.Items.Count}: [{string.Join(", ", sf.Items)}]");
            }

            Console.WriteLine("\n=== AFTER CALLING UpdateField (simulating modification) ===");
            // Simulate what happens when user modifies a field
            var currentValue = field9137.FirstValue ?? "";
            Console.WriteLine($"Current FirstValue: '{currentValue}'");

            // This is what UpdateField does internally:
            type9.UpdateField("9.137", currentValue);

            // Check what happened:
            field9137 = type9.GetField("9.137");
            Console.WriteLine($"\nAfter UpdateField:");
            Console.WriteLine($"Subfields: {field9137.Subfields.Count}");
            foreach (var sf in field9137.Subfields.Take(3))
            {
                Console.WriteLine($"  Items: {sf.Items.Count}: [{string.Join(", ", sf.Items.Take(10))}]");
            }
        }
    }
}
