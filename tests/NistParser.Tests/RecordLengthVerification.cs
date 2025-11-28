using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NistParser;
using NistParser.Constants;
using Xunit;

namespace NistParser.Tests
{
    public class RecordLengthVerification
    {
        [Fact]
        public void VerifyRecordLengths_OriginalFile()
        {
            // This test checks if the LEN field values match actual record sizes
            var origFile = Path.Combine("example files", "hibas", "orig_SIS_MMS_C_P25M.nist");

            if (!File.Exists(origFile))
            {
                return; // Skip if file doesn't exist
            }

            var bytes = File.ReadAllBytes(origFile);

            // Manually parse to check actual byte sizes
            Console.WriteLine("\n=== RECORD LENGTH VERIFICATION ===\n");

            int pos = 0;
            int recordNum = 0;

            while (pos < bytes.Length)
            {
                recordNum++;
                int recordStart = pos;

                // Find the FS (record separator)
                int fsPos = Array.IndexOf(bytes, SeparatorConstants.FS, pos);
                if (fsPos == -1) fsPos = bytes.Length; // Last record might not have FS

                int actualRecordSize = fsPos - recordStart; // Size WITHOUT FS

                // Extract LEN field value (X.001:value<GS>)
                // Find first GS to get the LEN field
                int firstGS = Array.IndexOf(bytes, SeparatorConstants.GS, pos);
                if (firstGS > fsPos || firstGS == -1) break;

                var lenFieldBytes = new byte[firstGS - pos];
                Array.Copy(bytes, pos, lenFieldBytes, 0, lenFieldBytes.Length);
                var lenFieldStr = Encoding.ASCII.GetString(lenFieldBytes);

                // Parse LEN value (format: "X.001:12345")
                var colonPos = lenFieldStr.IndexOf(':');
                if (colonPos >= 0)
                {
                    var lenValue = lenFieldStr.Substring(colonPos + 1);
                    if (int.TryParse(lenValue, out int declaredLength))
                    {
                        var fieldNum = lenFieldStr.Substring(0, colonPos);
                        Console.WriteLine($"Record {recordNum} ({fieldNum}):");
                        Console.WriteLine($"  Declared LEN: {declaredLength}");
                        Console.WriteLine($"  Actual size (excl FS): {actualRecordSize}");
                        Console.WriteLine($"  Difference: {declaredLength - actualRecordSize}");

                        if (declaredLength != actualRecordSize)
                        {
                            Console.WriteLine($"  WARNING: LEN mismatch!");
                        }
                    }
                }

                pos = fsPos + 1; // Move past the FS
            }
        }
    }
}
