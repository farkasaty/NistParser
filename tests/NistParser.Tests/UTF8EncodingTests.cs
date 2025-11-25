using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Records;
using NistParser.Writing;
using Xunit;

namespace NistParser.Tests
{
    public class UTF8EncodingTests
    {
        [Fact]
        public void TraditionalFormat_ShouldPreserveUTF8Characters()
        {
            // Arrange - Load the original file with UTF-8 characters
            string originalFile = Path.Combine("example files", "hibas", "orig_SIS_ATM_C_P25.nist");

            if (!File.Exists(originalFile))
            {
                // Skip test if file not found (CI environment)
                return;
            }

            byte[] originalBytes = File.ReadAllBytes(originalFile);

            // Act - Parse and reserialize
            var transaction = NistTransactionParser.Parse(originalBytes);
            byte[] reserializedBytes = NistTransactionWriter.WriteTraditional(transaction);

            // Assert - Check that UTF-8 characters are preserved
            var type2Record = transaction.Records.FirstOrDefault(r => r.RecordType == RecordType.Type2_UserDefinedText);
            type2Record.Should().NotBeNull();

            string value = type2Record!.GetFieldValue("2.013");

            // The value should contain the UTF-8 characters (éèàäüù)
            value.Should().Contain("UTF8");
            value.Should().Contain("é");
            value.Should().Contain("è");
            value.Should().Contain("à");
            value.Should().Contain("ä");
            value.Should().Contain("ü");
            value.Should().Contain("ù");

            // The value should NOT contain question marks replacing UTF-8 chars
            value.Should().NotContain("??????");

            // File size should be close (minor differences due to record length calculation fixes)
            // Note: After fixing the LEN field calculation to not include FS separator,
            // the reserialized file may be 1 byte smaller per record
            reserializedBytes.Length.Should().BeCloseTo(originalBytes.Length, 10, "file sizes should be approximately the same");

            // Verify the reserialized file can be parsed
            var reParsed = NistTransactionParser.Parse(reserializedBytes);
            var reParsedType2 = reParsed.Records.FirstOrDefault(r => r.RecordType == RecordType.Type2_UserDefinedText);
            string reParsedValue = reParsedType2!.GetFieldValue("2.013");

            // UTF-8 characters should still be there after round-trip
            reParsedValue.Should().Be(value, "UTF-8 characters should survive round-trip serialization");
        }

        [Fact]
        public void TraditionalFormat_UTF8Bytes_ShouldBeCorrect()
        {
            // Arrange - Create a transaction with UTF-8 characters
            var type1 = new Type1Record();
            type1.UpdateField("1.002", "0501");
            type1.UpdateField("1.004", "TEST");
            type1.UpdateField("1.005", "20241116");
            type1.UpdateField("1.007", "DAI");
            type1.UpdateField("1.008", "TEST");
            type1.UpdateField("1.009", "TEST");

            var transaction = new NistTransaction
            {
                Header = type1
            };

            var type2 = new Type2Record { IDC = "00" };

            // Set a field with UTF-8 characters
            string utf8Text = "Test with UTF-8: éèàäüù";
            type2.UpdateField("2.003", utf8Text);

            transaction.Records.Add(type2);

            // Act - Write to traditional format
            byte[] bytes = NistTransactionWriter.WriteTraditional(transaction);

            // Assert - Parse back and verify
            var parsed = NistTransactionParser.Parse(bytes);
            var parsedType2 = parsed.Records.FirstOrDefault(r => r.RecordType == RecordType.Type2_UserDefinedText);
            parsedType2.Should().NotBeNull();

            string parsedValue = parsedType2!.GetFieldValue("2.003");
            parsedValue.Should().Be(utf8Text, "UTF-8 characters should be preserved exactly");

            // Verify the bytes contain UTF-8 encoding, not ASCII question marks
            string bytesAsString = Encoding.UTF8.GetString(bytes);
            bytesAsString.Should().Contain("éèàäüù", "the UTF-8 characters should be in the output");

            // Check that we don't have the ASCII replacement character (?)
            // UTF-8 'é' is encoded as 0xC3 0xA9 in bytes
            bool hasUtf8Encoding = false;
            for (int i = 0; i < bytes.Length - 1; i++)
            {
                if (bytes[i] == 0xC3 && bytes[i + 1] == 0xA9)
                {
                    hasUtf8Encoding = true;
                    break;
                }
            }
            hasUtf8Encoding.Should().BeTrue("UTF-8 character 'é' (0xC3 0xA9) should be found in bytes");
        }
    }
}
