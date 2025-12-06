using Xunit;
using System.Text;
using NistParser.Constants;

namespace NistParser.Tests
{
    public class FlexibleParsingTests
    {
        [Theory]
        [InlineData("1.001:")]
        [InlineData("1.01:")]
        [InlineData("1.1:")]
        public void Parse_Type1WithVariousFieldFormats_ShouldSucceed(string fieldPrefix)
        {
            // Arrange: Create minimal Type-1 record with various field number formats
            var data = CreateMinimalType1Data(fieldPrefix);

            // Act
            var transaction = NistTransactionParser.Parse(data);

            // Assert
            Assert.NotNull(transaction);
            Assert.NotNull(transaction.Header);
        }

        [Fact]
        public void Parse_Type1With1_1Format_ParsesAllFields()
        {
            // Arrange: Create Type-1 with "1.1" format
            var sb = new StringBuilder();
            sb.Append("1.1:150");  // LEN field with 1.1 format
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.002:0502");  // VER field
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.003:1");
            sb.Append((char)SeparatorConstants.US);
            sb.Append("0");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.004:TEST");  // TOT
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.005:20250101");  // DAT
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.007:DEST");  // DAI
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.008:ORIG");  // ORI
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.009:TCN123");  // TCN
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.011:19.69");  // NSR
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.012:19.69");  // NTR
            sb.Append((char)SeparatorConstants.FS);

            var data = Encoding.UTF8.GetBytes(sb.ToString());

            // Act
            var transaction = NistTransactionParser.Parse(data);

            // Assert
            Assert.NotNull(transaction);
            Assert.NotNull(transaction.Header);

            // Fields should be accessible with any format
            Assert.NotNull(transaction.Header.GetField("1.001"));
            Assert.NotNull(transaction.Header.GetField("1.1"));  // Same field
            Assert.NotNull(transaction.Header.GetField("1.01")); // Same field
        }

        [Fact]
        public void RoundTrip_NonNormalizedInput_ProducesNormalizedOutput()
        {
            // Arrange: Create Type-1 with "1.01" format
            var input = CreateMinimalType1Data("1.01:");

            // Act: Parse
            var transaction = NistTransactionParser.Parse(input);
            Assert.NotNull(transaction);

            // Act: Write
            var output = Writing.NistTransactionWriter.WriteTraditional(transaction);

            // Assert: Output uses normalized format "1.001:"
            var outputText = Encoding.UTF8.GetString(output);
            Assert.StartsWith("1.001:", outputText);

            // Assert: Re-parse succeeds
            var reParsed = NistTransactionParser.Parse(output);
            Assert.NotNull(reParsed);
        }

        [Fact]
        public void DictionaryLookup_VariousFormats_FindsSameField()
        {
            // Arrange: Create Type-1 record
            var data = CreateMinimalType1Data("1.001:");
            var transaction = NistTransactionParser.Parse(data);

            // Act & Assert: Should find the field with any format
            Assert.NotNull(transaction.Header.GetField("1.001"));
            Assert.NotNull(transaction.Header.GetField("1.01"));
            Assert.NotNull(transaction.Header.GetField("1.1"));

            // All should return the same field
            var field1 = transaction.Header.GetField("1.001");
            var field2 = transaction.Header.GetField("1.01");
            var field3 = transaction.Header.GetField("1.1");

            Assert.Same(field1, field2);
            Assert.Same(field2, field3);
        }

        [Fact]
        public void UpdateField_NonNormalizedFormat_WorksCorrectly()
        {
            // Arrange: Create Type-1 record
            var data = CreateMinimalType1Data("1.001:");
            var transaction = NistTransactionParser.Parse(data);

            // Act: Update field using non-normalized format
            transaction.Header.UpdateField("1.4", "NEW_VALUE");

            // Assert: Should be accessible with any format
            Assert.Equal("NEW_VALUE", transaction.Header.GetFieldValue("1.004"));
            Assert.Equal("NEW_VALUE", transaction.Header.GetFieldValue("1.04"));
            Assert.Equal("NEW_VALUE", transaction.Header.GetFieldValue("1.4"));
        }

        /// <summary>
        /// Creates a minimal valid Type-1 record for testing
        /// </summary>
        private byte[] CreateMinimalType1Data(string lenFieldPrefix)
        {
            var sb = new StringBuilder();
            sb.Append(lenFieldPrefix);
            sb.Append("200");  // Approximate length
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.002:0502");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.003:1");
            sb.Append((char)SeparatorConstants.US);
            sb.Append("0");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.004:TEST");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.005:20250101");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.007:DEST");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.008:ORIG");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.009:TCN123");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.011:19.69");
            sb.Append((char)SeparatorConstants.GS);
            sb.Append("1.012:19.69");
            sb.Append((char)SeparatorConstants.FS);

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
    }
}
