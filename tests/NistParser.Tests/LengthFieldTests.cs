using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NistParser;
using Xunit;

namespace NistParser.Tests
{
    public class LengthFieldTests
    {
        [Fact]
        public void CompareLengthFields_OriginalVsModified()
        {
            // Arrange
            var origFile = Path.Combine("example files", "hibas", "orig_SIS_MMS_C_P25M.nist");
            var modFile = Path.Combine("example files", "hibas", "orig_SIS_MMS_C_P25M_modified.nist");

            if (!File.Exists(origFile) || !File.Exists(modFile))
            {
                // Skip if files don't exist
                return;
            }

            // Act
            var tx1 = NistTransactionParser.Parse(File.ReadAllBytes(origFile));
            var tx2 = NistTransactionParser.Parse(File.ReadAllBytes(modFile));

            // Output comparison
            Console.WriteLine("\n=== LENGTH FIELD COMPARISON ===\n");

            // Compare Type-1
            var len1_1 = tx1.Header.GetFieldValue("1.001");
            var len1_2 = tx2.Header.GetFieldValue("1.001");
            Console.WriteLine($"Type-1: {len1_1} → {len1_2} (diff: {int.Parse(len1_2 ?? "0") - int.Parse(len1_1 ?? "0")})");

            // Compare Type-2
            var type2_1 = tx1.Records.FirstOrDefault(r => (int)r.RecordType == 2);
            var type2_2 = tx2.Records.FirstOrDefault(r => (int)r.RecordType == 2);
            if (type2_1 != null && type2_2 != null)
            {
                var len2_1 = type2_1.GetFieldValue("2.001");
                var len2_2 = type2_2.GetFieldValue("2.001");
                Console.WriteLine($"Type-2: {len2_1} → {len2_2} (diff: {int.Parse(len2_2 ?? "0") - int.Parse(len2_1 ?? "0")})");
            }

            // Compare Type-9
            var type9_1 = tx1.Records.FirstOrDefault(r => (int)r.RecordType == 9);
            var type9_2 = tx2.Records.FirstOrDefault(r => (int)r.RecordType == 9);
            if (type9_1 != null && type9_2 != null)
            {
                var len9_1 = type9_1.GetFieldValue("9.001");
                var len9_2 = type9_2.GetFieldValue("9.001");
                Console.WriteLine($"Type-9: {len9_1} → {len9_2} (diff: {int.Parse(len9_2 ?? "0") - int.Parse(len9_1 ?? "0")})");

                // This is the issue the user reported - Type-9 wasn't modified but LEN decreased by 1
                var diff = int.Parse(len9_2 ?? "0") - int.Parse(len9_1 ?? "0");
                Console.WriteLine($"\nType-9 LEN difference: {diff} byte(s)");
            }
        }
    }
}
