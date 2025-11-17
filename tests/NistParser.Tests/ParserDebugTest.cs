using System;
using System.IO;
using System.Linq;
using NistParser.Core;
using NistParser.Constants;
using Xunit;
using Xunit.Abstractions;

namespace NistParser.Tests
{
    public class ParserDebugTest
    {
        private readonly ITestOutputHelper _output;

        public ParserDebugTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DebugType13BinaryField()
        {
            // Arrange
            string originalFile = Path.Combine("example files", "hibas", "orig_SIS_ATM_C_P25.nist");

            if (!File.Exists(originalFile))
            {
                _output.WriteLine("File not found, skipping test");
                return;
            }

            byte[] originalBytes = File.ReadAllBytes(originalFile);
            _output.WriteLine($"File size: {originalBytes.Length}");

            // Act - Parse the file
            var transaction = NistTransactionParser.Parse(originalBytes);

            // Assert - Check Type-13 record
            var type13 = transaction.Records.FirstOrDefault(r => r.RecordType == RecordType.Type13_LatentImage);

            if (type13 != null)
            {
                _output.WriteLine($"Type-13 RecordLength: {type13.RecordLength}");

                var field999 = type13.GetField("13.999");
                if (field999 != null)
                {
                    _output.WriteLine($"Field 999 found: BinaryData length = {field999.BinaryData?.Length ?? 0}");

                    // Expected: Binary data should start at position 485 and end at position 335731 (before FS at 335732)
                    // Expected length: 335731 - 485 + 1 = 335247 bytes
                    // But we're seeing 334517 bytes in the test output, which is 730 bytes less

                    int expectedBinaryLength = 335247;
                    int actualBinaryLength = field999.BinaryData?.Length ?? 0;
                    int difference = expectedBinaryLength - actualBinaryLength;

                    _output.WriteLine($"Expected binary length: {expectedBinaryLength}");
                    _output.WriteLine($"Actual binary length: {actualBinaryLength}");
                    _output.WriteLine($"Difference: {difference}");
                }
                else
                {
                    _output.WriteLine("Field 999 NOT found!");
                }

                // List all fields in Type-13
                _output.WriteLine("\nAll fields in Type-13:");
                foreach (var kvp in type13.Fields)
                {
                    var field = kvp.Value;
                    if (field.IsBinary)
                    {
                        _output.WriteLine($"  {field.FieldNumber}: [Binary, {field.BinaryData?.Length ?? 0} bytes]");
                    }
                    else
                    {
                        _output.WriteLine($"  {field.FieldNumber}: {field.FirstValue}");
                    }
                }
            }
            else
            {
                _output.WriteLine("Type-13 record not found!");
            }
        }
    }
}
