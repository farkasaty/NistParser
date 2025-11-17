using System;
using System.IO;
using System.Linq;
using NistParser.Core;
using NistParser.Constants;
using Xunit;
using Xunit.Abstractions;

namespace NistParser.Tests
{
    public class DebugBinaryParsing
    {
        private readonly ITestOutputHelper _output;

        public DebugBinaryParsing(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DebugWhereField999IsProcessed()
        {
            // Load the file
            string originalFile = Path.Combine("example files", "hibas", "orig_SIS_ATM_C_P25.nist");

            if (!File.Exists(originalFile))
            {
                _output.WriteLine("File not found");
                return;
            }

            byte[] bytes = File.ReadAllBytes(originalFile);

            // Manually find where Type-13 starts
            // Based on PowerShell: Type-1 ends at 191, Type-2 is 107 bytes, so Type-13 starts at 298
            int type13Start = 298;

            // Read the length field
            int lengthFieldEnd = -1;
            for (int i = type13Start; i < type13Start + 30; i++)
            {
                if (bytes[i] == 0x1D) // GS
                {
                    lengthFieldEnd = i;
                    break;
                }
            }

            _output.WriteLine($"Type-13 starts at: {type13Start}");
            _output.WriteLine($"First GS at: {lengthFieldEnd}");

            // Find where 13.999 starts
            int field999Start = -1;
            for (int i = type13Start; i < type13Start + 500; i++)
            {
                if (i + 6 < bytes.Length &&
                    bytes[i] == (byte)'1' && bytes[i+1] == (byte)'3' &&
                    bytes[i+2] == (byte)'.' && bytes[i+3] == (byte)'9' &&
                    bytes[i+4] == (byte)'9' && bytes[i+5] == (byte)'9' &&
                    bytes[i+6] == (byte)':')
                {
                    field999Start = i;
                    break;
                }
            }

            _output.WriteLine($"Field 13.999 starts at: {field999Start}");

            // Find the GS BEFORE 13.999
            int gsBefore999 = -1;
            for (int i = field999Start - 1; i > type13Start; i--)
            {
                if (bytes[i] == 0x1D) // GS
                {
                    gsBefore999 = i;
                    break;
                }
            }

            _output.WriteLine($"GS before 13.999 at: {gsBefore999}");
            _output.WriteLine($"So field 13.999 starts right after the GS");

            // Check if there's a GS AFTER 13.999:
            int gsAfter999 = -1;
            for (int i = field999Start + 7; i < bytes.Length && i < type13Start + 100000; i++)
            {
                if (bytes[i] == 0x1D) // GS
                {
                    gsAfter999 = i;
                    break;
                }
            }

            _output.WriteLine($"GS after 13.999 at: {gsAfter999} (should be -1, meaning NOT FOUND)");

            // Now parse with the actual parser
            var transaction = NistTransactionParser.Parse(bytes);
            var type13 = transaction.Records.FirstOrDefault(r => r.RecordType == RecordType.Type13_LatentImage);

            if (type13 != null)
            {
                var field999 = type13.GetField("13.999");
                if (field999 != null)
                {
                    _output.WriteLine($"\nParser result:");
                    _output.WriteLine($"  IsBinary: {field999.IsBinary}");
                    _output.WriteLine($"  BinaryData length: {field999.BinaryData?.Length ?? 0}");
                    _output.WriteLine($"  Subfields count: {field999.Subfields.Count}");
                }
            }
        }
    }
}
