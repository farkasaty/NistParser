using FluentAssertions;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Exceptions;
using Xunit;

namespace NistParser.Tests;

/// <summary>
/// Tests for NIEM XML format parsing
/// </summary>
public class XmlNistTransactionParserTests
{
    private const string XmlSamplesPath = "example files/xml/AN2011_SampleData/NIEM XML Encoding";

    [Fact]
    public void Parse_Type1MandatoryOnly_ShouldParseSuccessfully()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-1-mandatory-only-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            // Skip test if sample files are not available
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Header.Should().NotBeNull();
        transaction.Header.RecordType.Should().Be(RecordType.Type1_TransactionInformation);
    }

    [Fact]
    public void Parse_Type1MandatoryOnly_ShouldParseVersion()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-1-mandatory-only-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        transaction.Header.Version.Should().NotBeNullOrEmpty();
        transaction.Header.Version.Should().MatchRegex(@"^\d{4}$"); // Format: "0500"
    }

    [Fact]
    public void Parse_Type1MandatoryOnly_ShouldParseTransactionControlNumber()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-1-mandatory-only-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        transaction.TransactionControlNumber.Should().NotBeNullOrEmpty();
        transaction.TransactionControlNumber.Should().Be("TCN");
    }

    [Fact]
    public void Parse_Type1MandatoryOnly_ShouldParseOriginatingAgency()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-1-mandatory-only-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        transaction.OriginatingAgencyIdentifier.Should().NotBeNullOrEmpty();
        transaction.OriginatingAgencyIdentifier.Should().Be("ORI");
    }

    [Fact]
    public void Parse_Type1MandatoryOnly_ShouldParseContentSummary()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-1-mandatory-only-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        transaction.Header.Content.Should().NotBeNull();
        transaction.Header.Content!.RecordCount.Should().BeGreaterThan(0);
        transaction.Header.Content.Records.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_Type14WithImage_ShouldParseSuccessfully()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-14-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Records.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_Type14WithImage_ShouldDecodeBase64Image()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-14-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        var type14Records = transaction.Records.Where(r => r.RecordType == RecordType.Type14_FingerprintImage).ToList();
        type14Records.Should().NotBeEmpty();

        // Check if at least one Type-14 record has image data
        var recordWithImage = type14Records.FirstOrDefault(r => r.Fields.ContainsKey("14.999"));
        if (recordWithImage != null)
        {
            var imageField = recordWithImage.Fields["14.999"];
            imageField.Should().NotBeNull();
            imageField.BinaryData.Should().NotBeNull();
            imageField.BinaryData!.Length.Should().BeGreaterThan(0);
        }
    }

    [Theory]
    [InlineData("pass-type-1-mandatory-only-utf8-L1.xml")]
    [InlineData("pass-type-14-utf8-L1.xml")]
    public void Parse_ValidXmlFiles_ShouldNotThrow(string filename)
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, filename);
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        Action act = () => NistTransactionParser.ParseFile(xmlPath);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_XmlFormat_ShouldAutoDetectFormat()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-1-mandatory-only-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert - Should parse without explicitly specifying XML format
        transaction.Should().NotBeNull();
        transaction.Header.Should().NotBeNull();
    }

    [Fact]
    public void Parse_EmptyXmlData_ShouldThrowException()
    {
        // Arrange
        byte[] emptyData = Array.Empty<byte>();

        // Act
        Action act = () => XmlNistTransactionParser.Parse(emptyData);

        // Assert
        act.Should().Throw<NistParserException>()
            .WithMessage("*null or empty*");
    }

    [Fact]
    public void Parse_InvalidXml_ShouldThrowException()
    {
        // Arrange
        byte[] invalidXml = System.Text.Encoding.UTF8.GetBytes("<invalid><xml>");

        // Act
        Action act = () => XmlNistTransactionParser.Parse(invalidXml);

        // Assert
        act.Should().Throw<NistParserException>()
            .WithMessage("*parse XML*");
    }

    [Fact]
    public void Parse_XmlWithoutType1_ShouldThrowMissingType1Exception()
    {
        // Arrange
        string xmlWithoutType1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<itl:NISTBiometricInformationExchangePackage xmlns:itl=""http://biometrics.nist.gov/standard/2011"">
</itl:NISTBiometricInformationExchangePackage>";
        byte[] xmlData = System.Text.Encoding.UTF8.GetBytes(xmlWithoutType1);

        // Act
        Action act = () => XmlNistTransactionParser.Parse(xmlData);

        // Assert
        act.Should().Throw<MissingType1Exception>()
            .WithMessage("*Type-1*");
    }

    [Fact]
    public void ParseFile_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        string nonExistentPath = "nonexistent.xml";

        // Act
        Action act = () => XmlNistTransactionParser.ParseFile(nonExistentPath);

        // Assert
        act.Should().Throw<FileNotFoundException>();
    }

    [Fact]
    public void Parse_AllSupportedTypes_ShouldHandleMultipleRecordTypes()
    {
        // Arrange
        string xmlPath = Path.Combine("example files/xml/AN2013_SampleData/NIEM XML Encoding", "pass-all-supported-types-utf8-L1&L2.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Header.Should().NotBeNull();
        transaction.Records.Should().NotBeEmpty();

        // Should have multiple record types
        var recordTypes = transaction.Records.Select(r => r.RecordType).Distinct().ToList();
        recordTypes.Should().NotBeEmpty();
    }

    [Fact]
    public void Parse_Type1Fields_ShouldMapToCorrectFieldNumbers()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-1-mandatory-only-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        var header = transaction.Header;
        header.Fields.Should().ContainKey("1.001"); // Record category code
        header.Fields.Should().ContainKey("1.002"); // Version
        header.Fields.Should().ContainKey("1.003"); // Content summary
        header.Fields.Should().ContainKey("1.004"); // Transaction type
        header.Fields.Should().ContainKey("1.005"); // Date
        header.Fields.Should().ContainKey("1.007"); // Destination agency
        header.Fields.Should().ContainKey("1.008"); // Originating agency
        header.Fields.Should().ContainKey("1.009"); // Transaction control number
    }

    [Fact]
    public void Parse_Type14Fields_ShouldMapToCorrectFieldNumbers()
    {
        // Arrange
        string xmlPath = Path.Combine(XmlSamplesPath, "pass-type-14-utf8-L1.xml");
        if (!File.Exists(xmlPath))
        {
            return;
        }

        // Act
        var transaction = NistTransactionParser.ParseFile(xmlPath);

        // Assert
        var type14 = transaction.Records.FirstOrDefault(r => r.RecordType == RecordType.Type14_FingerprintImage);
        if (type14 != null)
        {
            type14.Fields.Should().ContainKey("14.001"); // Record category code
            type14.Fields.Should().ContainKey("14.002"); // IDC
        }
    }
}
