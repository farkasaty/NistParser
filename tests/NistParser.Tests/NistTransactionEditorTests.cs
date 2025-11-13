using FluentAssertions;
using NistParser.Core;
using NistParser.Editing;
using NistParser.Records;
using Xunit;

namespace NistParser.Tests;

public class NistTransactionEditorTests
{
    [Fact]
    public void SetFieldValue_ValidValue_ShouldUpdateField()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);

        // Act
        var result = editor.SetFieldValue(transaction.Header, "1.009", "NEW-TCN");

        // Assert
        result.IsValid.Should().BeTrue();
        transaction.Header.GetFieldValue("1.009").Should().Be("NEW-TCN");
    }

    [Fact]
    public void SetFieldValue_InvalidValue_ShouldReturnError()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);

        // Act - Try to set invalid version (should be 4 digits)
        var result = editor.SetFieldValue(transaction.Header, "1.002", "05");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("4 digits");
    }

    [Fact]
    public void AddField_NewOptionalField_ShouldAddSuccessfully()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);

        // Act
        var result = editor.AddField(transaction.Header, "1.017", "Test Agency Name");

        // Assert
        result.IsValid.Should().BeTrue();
        transaction.Header.GetFieldValue("1.017").Should().Be("Test Agency Name");
    }

    [Fact]
    public void AddField_ExistingField_ShouldReturnError()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);

        // Act - Try to add a field that already exists
        var result = editor.AddField(transaction.Header, "1.002", "0502");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("already exists");
    }

    [Fact]
    public void RemoveField_OptionalField_ShouldRemoveSuccessfully()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.Header.UpdateField("1.011", "500");
        var editor = new NistTransactionEditor(transaction);

        // Act
        var result = editor.RemoveField(transaction.Header, "1.011");

        // Assert
        result.IsValid.Should().BeTrue();
        transaction.Header.HasField("1.011").Should().BeFalse();
    }

    [Fact]
    public void RemoveField_RequiredField_ShouldReturnError()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);

        // Act - Try to remove required field
        var result = editor.RemoveField(transaction.Header, "1.002");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Cannot delete required field");
    }

    [Fact]
    public void ValidateAllChanges_ValidTransaction_ShouldPass()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);

        // Act
        var results = editor.ValidateAllChanges();

        // Assert
        results.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateAllChanges_MissingRequiredField_ShouldFail()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.Header.RemoveField("1.002"); // Remove required version field
        var editor = new NistTransactionEditor(transaction);

        // Act
        var results = editor.ValidateAllChanges();

        // Assert
        results.IsValid.Should().BeFalse();
        results.ErrorMessages.Should().Contain(m => m.Contains("Version") && m.Contains("required"));
    }

    [Fact]
    public void SaveToBytes_ValidTransaction_ShouldSerialize()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);
        editor.SetFieldValue(transaction.Header, "1.009", "EDITED-TCN");

        // Act
        var bytes = editor.SaveToBytes();

        // Assert
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);

        // Verify roundtrip
        var parsed = NistTransactionParser.Parse(bytes);
        parsed.Header.TransactionControlNumber.Should().Be("EDITED-TCN");
    }

    [Fact]
    public void SaveToBytes_InvalidTransaction_ShouldThrowException()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.Header.RemoveField("1.002"); // Remove required field
        var editor = new NistTransactionEditor(transaction);

        // Act
        Action act = () => editor.SaveToBytes();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*validation errors*");
    }

    [Fact]
    public void FromBytes_ValidFile_ShouldCreateEditor()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var originalEditor = new NistTransactionEditor(transaction);
        var bytes = originalEditor.SaveToBytes();

        // Act
        var editor = NistTransactionEditor.FromBytes(bytes);

        // Assert
        editor.Should().NotBeNull();
        editor.Transaction.Should().NotBeNull();
        editor.Transaction.Header.Version.Should().Be("0502");
    }

    [Fact]
    public void CompleteEditWorkflow_ShouldWork()
    {
        // Arrange - Create initial transaction
        var transaction = CreateTestTransaction();
        var editor = new NistTransactionEditor(transaction);

        // Act - Make multiple edits
        editor.SetFieldValue(transaction.Header, "1.009", "MODIFIED-TCN");
        editor.AddField(transaction.Header, "1.011", "500");
        editor.AddField(transaction.Header, "1.012", "500");

        // Add Type-2 record
        var type2 = new Type2Record { IDC = "01" };
        type2.UpdateField("2.004", "Doe");
        type2.UpdateField("2.005", "Jane");
        type2.UpdateField("2.024", "F");
        transaction.Records.Add(type2);

        // Validate
        var validationResults = editor.ValidateAllChanges();
        validationResults.IsValid.Should().BeTrue();

        // Save
        var bytes = editor.SaveToBytes();

        // Parse back
        var parsed = NistTransactionParser.Parse(bytes);

        // Assert - Verify all changes were preserved
        parsed.Header.TransactionControlNumber.Should().Be("MODIFIED-TCN");
        parsed.Header.GetFieldValue("1.011").Should().Be("500");
        parsed.Header.GetFieldValue("1.012").Should().Be("500");

        var parsedType2 = parsed.GetRecords<Type2Record>().FirstOrDefault();
        parsedType2.Should().NotBeNull();
        parsedType2!.Surname.Should().Be("Doe");
        parsedType2.GivenName.Should().Be("Jane");
        parsedType2.Sex.Should().Be("F");
    }

    /// <summary>
    /// Creates a minimal valid test transaction
    /// </summary>
    private NistTransaction CreateTestTransaction()
    {
        var type1 = new Type1Record();
        type1.UpdateField("1.002", "0502");
        type1.UpdateField("1.004", "CRM");
        type1.UpdateField("1.005", "20250115");
        type1.UpdateField("1.007", "TEST-DAI");
        type1.UpdateField("1.008", "TEST-ORI");
        type1.UpdateField("1.009", "TEST-TCN-001");

        return new NistTransaction { Header = type1 };
    }
}
