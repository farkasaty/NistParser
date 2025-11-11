using NistParser.Constants;
using NistParser.Core;
using NistParser.Records;
using Xunit;
using FluentAssertions;

namespace NistParser.Tests;

public class RecordTypeTests
{
    [Fact]
    public void Type1Record_ShouldHaveCorrectRecordType()
    {
        // Arrange & Act
        var record = new Type1Record();

        // Assert
        record.RecordType.Should().Be(RecordType.Type1_TransactionInformation);
        record.IDC.Should().Be("00");
    }

    [Fact]
    public void Type2Record_ShouldHaveCorrectRecordType()
    {
        // Arrange & Act
        var record = new Type2Record();

        // Assert
        record.RecordType.Should().Be(RecordType.Type2_UserDefinedText);
    }

    [Fact]
    public void Type14Record_ShouldHaveCorrectRecordType()
    {
        // Arrange & Act
        var record = new Type14Record();

        // Assert
        record.RecordType.Should().Be(RecordType.Type14_FingerprintImage);
    }

    [Theory]
    [InlineData(RecordType.Type1_TransactionInformation, RecordEncoding.TaggedASCII)]
    [InlineData(RecordType.Type2_UserDefinedText, RecordEncoding.TaggedASCII)]
    [InlineData(RecordType.Type9_MinutiaeData, RecordEncoding.TaggedASCII)]
    [InlineData(RecordType.Type4_HighResGrayscaleFingerprint, RecordEncoding.Binary)]
    [InlineData(RecordType.Type8_SignatureImage, RecordEncoding.Binary)]
    [InlineData(RecordType.Type10_FacialAndSMTImage, RecordEncoding.Mixed)]
    [InlineData(RecordType.Type14_FingerprintImage, RecordEncoding.Mixed)]
    [InlineData(RecordType.Type17_IrisImage, RecordEncoding.Mixed)]
    public void RecordEncoding_ShouldReturnCorrectEncodingForRecordType(
        RecordType recordType, RecordEncoding expectedEncoding)
    {
        // Act
        var encoding = RecordEncodingExtensions.GetEncoding(recordType);

        // Assert
        encoding.Should().Be(expectedEncoding);
    }

    [Fact]
    public void NistField_ParseFieldNumber_ShouldExtractComponents()
    {
        // Arrange & Act
        var field = new NistField("14.999");

        // Assert
        field.RecordTypeNumber.Should().Be(14);
        field.Field.Should().Be(999);
    }

    [Fact]
    public void NistField_AddSubfield_ShouldBeAccessible()
    {
        // Arrange
        var field = new NistField("1.003");
        var subfield = new NistSubfield("1", "2", "3");

        // Act
        field.Subfields.Add(subfield);

        // Assert
        field.Subfields.Should().HaveCount(1);
        field.FirstValue.Should().Be("1");
        field.GetAllItems().Should().BeEquivalentTo("1", "2", "3");
    }

    [Fact]
    public void CompressionAlgorithm_GetDisplayName_ShouldReturnCorrectNames()
    {
        // Arrange & Act & Assert
        CompressionAlgorithm.WSQ.GetDisplayName().Should().Be("WSQ v2.0");
        CompressionAlgorithm.JPEG_Lossy.GetDisplayName().Should().Be("JPEG (Lossy)");
        CompressionAlgorithm.JPEG2000_Lossless.GetDisplayName().Should().Be("JPEG 2000 (Lossless)");
        CompressionAlgorithm.PNG.GetDisplayName().Should().Be("PNG (Lossless)");
    }

    [Theory]
    [InlineData(CompressionAlgorithm.Uncompressed, true)]
    [InlineData(CompressionAlgorithm.WSQ, false)]
    [InlineData(CompressionAlgorithm.JPEG_Lossy, false)]
    [InlineData(CompressionAlgorithm.JPEG_Lossless, true)]
    [InlineData(CompressionAlgorithm.JPEG2000_Lossy, false)]
    [InlineData(CompressionAlgorithm.JPEG2000_Lossless, true)]
    [InlineData(CompressionAlgorithm.PNG, true)]
    public void CompressionAlgorithm_IsLossless_ShouldReturnCorrectValue(
        CompressionAlgorithm algorithm, bool expectedLossless)
    {
        // Act
        bool isLossless = algorithm.IsLossless();

        // Assert
        isLossless.Should().Be(expectedLossless);
    }
}
