using NistParser.Constants;
using NistParser.Utilities;
using Xunit;
using FluentAssertions;
using System.Text;

namespace NistParser.Tests;

public class FieldParserTests
{
    [Fact]
    public void ParseTaggedField_SimpleField_ShouldParseCorrectly()
    {
        // Arrange
        string fieldData = "1.002:0502";
        byte[] bytes = Encoding.ASCII.GetBytes(fieldData);

        // Act
        var field = FieldParser.ParseTaggedField(bytes);

        // Assert
        field.FieldNumber.Should().Be("1.002");
        field.Subfields.Should().HaveCount(1);
        field.FirstValue.Should().Be("0502");
    }

    [Fact]
    public void ParseTaggedField_WithMultipleItems_ShouldParseCorrectly()
    {
        // Arrange: Field with 3 items separated by US
        var fieldBytes = Encoding.ASCII.GetBytes("1.003:");
        var item1 = Encoding.ASCII.GetBytes("1");
        var item2 = Encoding.ASCII.GetBytes("2");
        var item3 = Encoding.ASCII.GetBytes("3");

        var fullField = fieldBytes
            .Concat(item1)
            .Concat(new[] { SeparatorConstants.US })
            .Concat(item2)
            .Concat(new[] { SeparatorConstants.US })
            .Concat(item3)
            .ToArray();

        // Act
        var field = FieldParser.ParseTaggedField(fullField);

        // Assert
        field.FieldNumber.Should().Be("1.003");
        field.Subfields.Should().HaveCount(1);
        field.Subfields[0].Items.Should().HaveCount(3);
        field.Subfields[0].Items[0].Should().Be("1");
        field.Subfields[0].Items[1].Should().Be("2");
        field.Subfields[0].Items[2].Should().Be("3");
    }

    [Fact]
    public void ParseTaggedField_WithMultipleSubfields_ShouldParseCorrectly()
    {
        // Arrange: Field with 2 subfields, each with 2 items
        // Format: "9.012:1<US>100<RS>2<US>200"
        var fieldBytes = Encoding.ASCII.GetBytes("9.012:");

        var data = Encoding.ASCII.GetBytes("1")
            .Concat(new[] { SeparatorConstants.US })
            .Concat(Encoding.ASCII.GetBytes("100"))
            .Concat(new[] { SeparatorConstants.RS })
            .Concat(Encoding.ASCII.GetBytes("2"))
            .Concat(new[] { SeparatorConstants.US })
            .Concat(Encoding.ASCII.GetBytes("200"))
            .ToArray();

        var fullField = fieldBytes.Concat(data).ToArray();

        // Act
        var field = FieldParser.ParseTaggedField(fullField);

        // Assert
        field.FieldNumber.Should().Be("9.012");
        field.Subfields.Should().HaveCount(2);

        // First subfield
        field.Subfields[0].Items.Should().HaveCount(2);
        field.Subfields[0].Items[0].Should().Be("1");
        field.Subfields[0].Items[1].Should().Be("100");

        // Second subfield
        field.Subfields[1].Items.Should().HaveCount(2);
        field.Subfields[1].Items[0].Should().Be("2");
        field.Subfields[1].Items[1].Should().Be("200");
    }

    [Fact]
    public void SplitBytes_WithDelimiter_ShouldSplitCorrectly()
    {
        // Arrange
        var data = Encoding.ASCII.GetBytes("item1")
            .Concat(new[] { SeparatorConstants.US })
            .Concat(Encoding.ASCII.GetBytes("item2"))
            .Concat(new[] { SeparatorConstants.US })
            .Concat(Encoding.ASCII.GetBytes("item3"))
            .ToArray();

        // Act
        var chunks = FieldParser.SplitBytes(data, SeparatorConstants.US);

        // Assert
        chunks.Should().HaveCount(3);
        Encoding.ASCII.GetString(chunks[0]).Should().Be("item1");
        Encoding.ASCII.GetString(chunks[1]).Should().Be("item2");
        Encoding.ASCII.GetString(chunks[2]).Should().Be("item3");
    }

    [Fact]
    public void ExtractFieldNumber_ValidField_ShouldReturnFieldNumber()
    {
        // Arrange
        byte[] fieldData = Encoding.ASCII.GetBytes("1.002:0502");

        // Act
        string? fieldNumber = FieldParser.ExtractFieldNumber(fieldData);

        // Assert
        fieldNumber.Should().Be("1.002");
    }

    [Fact]
    public void ExtractFieldNumber_NoColon_ShouldReturnNull()
    {
        // Arrange
        byte[] fieldData = Encoding.ASCII.GetBytes("1.002");

        // Act
        string? fieldNumber = FieldParser.ExtractFieldNumber(fieldData);

        // Assert
        fieldNumber.Should().BeNull();
    }
}
