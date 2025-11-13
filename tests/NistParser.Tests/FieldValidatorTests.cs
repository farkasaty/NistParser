using FluentAssertions;
using NistParser.Editing;
using Xunit;

namespace NistParser.Tests;

public class FieldValidatorTests
{
    [Fact]
    public void ValidateField_RequiredFieldWithValue_ShouldPass()
    {
        // Arrange
        var metadata = FieldMetadata.Text("1.002", "Version", isRequired: true, pattern: @"^\d{4}$");

        // Act
        var result = FieldValidator.ValidateField(metadata, "0502");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateField_RequiredFieldWithoutValue_ShouldFail()
    {
        // Arrange
        var metadata = FieldMetadata.Text("1.002", "Version", isRequired: true);

        // Act
        var result = FieldValidator.ValidateField(metadata, null);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("required");
    }

    [Fact]
    public void ValidateField_OptionalFieldWithoutValue_ShouldPass()
    {
        // Arrange
        var metadata = FieldMetadata.Text("1.006", "Priority", isRequired: false);

        // Act
        var result = FieldValidator.ValidateField(metadata, null);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateField_NumericFieldWithValidNumber_ShouldPass()
    {
        // Arrange
        var metadata = FieldMetadata.Numeric("1.011", "NSR", isRequired: false);

        // Act
        var result = FieldValidator.ValidateField(metadata, "500");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateField_NumericFieldWithInvalidValue_ShouldFail()
    {
        // Arrange
        var metadata = FieldMetadata.Numeric("1.011", "NSR", isRequired: false);

        // Act
        var result = FieldValidator.ValidateField(metadata, "abc");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("number");
    }

    [Fact]
    public void ValidateField_DateFieldWithValidDate_ShouldPass()
    {
        // Arrange
        var metadata = FieldMetadata.Date("1.005", "Date", isRequired: true);

        // Act
        var result = FieldValidator.ValidateField(metadata, "20250115");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateField_DateFieldWithInvalidDate_ShouldFail()
    {
        // Arrange
        var metadata = FieldMetadata.Date("1.005", "Date", isRequired: true);

        // Act
        var result = FieldValidator.ValidateField(metadata, "20251399"); // Invalid month

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateField_EnumFieldWithValidValue_ShouldPass()
    {
        // Arrange
        var metadata = FieldMetadata.Enum("1.004", "TOT",
            new Dictionary<string, string> { { "CRM", "Criminal" }, { "CIV", "Civil" } },
            isRequired: true);

        // Act
        var result = FieldValidator.ValidateField(metadata, "CRM");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateField_EnumFieldWithInvalidValue_ShouldFail()
    {
        // Arrange
        var metadata = FieldMetadata.Enum("1.004", "TOT",
            new Dictionary<string, string> { { "CRM", "Criminal" }, { "CIV", "Civil" } },
            isRequired: true);

        // Act
        var result = FieldValidator.ValidateField(metadata, "INVALID");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("must be one of");
    }

    [Fact]
    public void ValidateField_PatternValidation_ShouldEnforcePattern()
    {
        // Arrange
        var metadata = FieldMetadata.Text("1.002", "Version", isRequired: true,
            pattern: @"^\d{4}$", maxLength: 4);

        // Act - valid pattern
        var validResult = FieldValidator.ValidateField(metadata, "0502");
        // Act - invalid pattern
        var invalidResult = FieldValidator.ValidateField(metadata, "05");

        // Assert
        validResult.IsValid.Should().BeTrue();
        invalidResult.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateField_MaxLengthValidation_ShouldEnforceMaxLength()
    {
        // Arrange
        var metadata = FieldMetadata.Text("1.008", "ORI", isRequired: true, maxLength: 10);

        // Act - within max length
        var validResult = FieldValidator.ValidateField(metadata, "12345");
        // Act - exceeds max length
        var invalidResult = FieldValidator.ValidateField(metadata, "12345678901");

        // Assert
        validResult.IsValid.Should().BeTrue();
        invalidResult.IsValid.Should().BeFalse();
        invalidResult.ErrorMessage.Should().Contain("must not exceed");
    }

    [Fact]
    public void ValidateDeleteField_RequiredField_ShouldFail()
    {
        // Act
        var result = FieldValidator.ValidateDeleteField("1.002");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot delete required field");
    }

    [Fact]
    public void ValidateDeleteField_OptionalField_ShouldPass()
    {
        // Act
        var result = FieldValidator.ValidateDeleteField("1.011");

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
