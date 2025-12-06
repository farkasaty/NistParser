using Xunit;
using NistParser.Utilities;

namespace NistParser.Tests
{
    public class FieldNumberFormatterTests
    {
        [Theory]
        [InlineData("1.1", "1.001")]
        [InlineData("1.01", "1.001")]
        [InlineData("1.001", "1.001")]
        [InlineData("2.2", "2.002")]
        [InlineData("14.13", "14.013")]
        [InlineData("14.999", "14.999")]
        [InlineData("1.1234", "1.1234")]  // More than 3 digits preserved
        public void Normalize_VariousFormats_ReturnsMinimumThreeDigits(string input, string expected)
        {
            // Act
            var result = FieldNumberFormatter.Normalize(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1.1", "1.001", true)]
        [InlineData("1.01", "1.001", true)]
        [InlineData("1.001", "1.001", true)]
        [InlineData("1.1", "1.10", false)]  // 1.1 (field 1) != 1.10 (field 10)
        [InlineData("1.01", "1.10", false)]
        [InlineData("2.002", "2.2", true)]
        [InlineData("14.999", "14.999", true)]
        public void AreEquivalent_ComparesNormalized(string fieldNumber1, string fieldNumber2, bool expected)
        {
            // Act
            var result = FieldNumberFormatter.AreEquivalent(fieldNumber1, fieldNumber2);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("1.001", true)]
        [InlineData("1.1", true)]
        [InlineData("14.999", true)]
        [InlineData("invalid", false)]
        [InlineData("1", false)]
        [InlineData("1.abc", false)]
        [InlineData("abc.123", false)]
        [InlineData("1.2.3", false)]
        public void IsValid_ValidatesFormat(string fieldNumber, bool expected)
        {
            // Act
            var result = FieldNumberFormatter.IsValid(fieldNumber);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(1, 1, "1.001")]
        [InlineData(1, 2, "1.002")]
        [InlineData(14, 999, "14.999")]
        [InlineData(2, 10, "2.010")]
        public void Create_GeneratesNormalizedFieldNumber(int recordType, int field, string expected)
        {
            // Act
            var result = FieldNumberFormatter.Create(recordType, field);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Normalize_InvalidFormat_ReturnsAsIs()
        {
            // Arrange
            var invalid = "invalid.format.test";

            // Act
            var result = FieldNumberFormatter.Normalize(invalid);

            // Assert - defensive programming: returns input unchanged
            Assert.Equal(invalid, result);
        }
    }
}
