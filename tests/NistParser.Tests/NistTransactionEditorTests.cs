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
    public void SetFieldValue_UnknownField_ShouldUpdateExistingFieldNotCreateDuplicate()
    {
        // Arrange - Create Type-2 record with unknown field (2.003 is not in type2-fields.json)
        // Manually add to UnknownFields to simulate parsing behavior
        var transaction = CreateTestTransaction();
        var type2 = new Type2Record { IDC = "01" };
        type2.UpdateField("2.002", "01");
        type2.SetUnknownField("2.003", "ORIGINAL-VALUE"); // Add to UnknownFields (simulates parsing)
        transaction.Records.Add(type2);

        var editor = new NistTransactionEditor(transaction);

        // Act - Try to edit the unknown field
        var result = editor.SetFieldValue(type2, "2.003", "MODIFIED-VALUE");

        // Assert
        result.IsValid.Should().BeTrue();
        type2.GetFieldValue("2.003").Should().Be("MODIFIED-VALUE");

        // Verify the field is in UnknownFields and was updated there
        type2.UnknownFields.Should().ContainKey("2.003");
        type2.GetUnknownField("2.003").Should().Be("MODIFIED-VALUE");

        // Verify the field is NOT in Fields (should stay in UnknownFields)
        type2.Fields.Should().NotContainKey("2.003", "unknown field should not be moved to Fields");

        // Verify no duplicate - should exist in exactly ONE collection
        var inFields = type2.Fields.ContainsKey("2.003");
        var inUnknownFields = type2.UnknownFields.ContainsKey("2.003");
        (inFields ^ inUnknownFields).Should().BeTrue("field should be in exactly one collection (XOR)");
    }

    [Fact]
    public void SetFieldValue_UnknownField_RoundTripShouldPreserveEdit()
    {
        // Arrange - Create Type-2 record with unknown field
        var transaction = CreateTestTransaction();
        var type2 = new Type2Record { IDC = "01" };
        type2.UpdateField("2.002", "01");
        type2.SetUnknownField("2.003", "ORIGINAL-VALUE"); // Add to UnknownFields (simulates parsing)
        type2.UpdateField("2.004", "Doe"); // Known field
        transaction.Records.Add(type2);

        var editor = new NistTransactionEditor(transaction);

        // Act - Edit unknown field and save
        editor.SetFieldValue(type2, "2.003", "MODIFIED-VALUE");
        var bytes = editor.SaveToBytes();

        // Parse back
        var parsed = NistTransactionParser.Parse(bytes);
        var parsedType2 = parsed.GetRecords<Type2Record>().FirstOrDefault();

        // Assert - Verify the edit was preserved
        parsedType2.Should().NotBeNull();
        parsedType2!.GetFieldValue("2.003").Should().Be("MODIFIED-VALUE");

        // Verify no duplicates - field should exist in exactly one collection
        var inFields = parsedType2.Fields.ContainsKey("2.003");
        var inUnknownFields = parsedType2.UnknownFields.ContainsKey("2.003");
        var totalCount = (inFields ? 1 : 0) + (inUnknownFields ? 1 : 0);
        totalCount.Should().Be(1, "after round-trip, field 2.003 should exist in exactly one collection");

        // The serialized and re-parsed file should not have duplicates in the raw bytes either
        var bytesAsString = System.Text.Encoding.UTF8.GetString(bytes);
        var occurrences = System.Text.RegularExpressions.Regex.Matches(bytesAsString, "2\\.003:").Count;
        occurrences.Should().Be(1, "the serialized file should contain field 2.003 exactly once");
    }

    [Fact]
    public void SetFieldValue_MultipleUnknownFields_ShouldNotCreateDuplicates()
    {
        // Arrange - Create record with multiple unknown fields
        var transaction = CreateTestTransaction();
        var type2 = new Type2Record { IDC = "01" };
        type2.UpdateField("2.002", "01");
        type2.SetUnknownField("2.003", "VALUE-A"); // Add to UnknownFields
        type2.SetUnknownField("2.099", "VALUE-B"); // Add to UnknownFields
        type2.SetUnknownField("2.100", "VALUE-C"); // Add to UnknownFields
        transaction.Records.Add(type2);

        var editor = new NistTransactionEditor(transaction);

        // Act - Edit multiple unknown fields
        editor.SetFieldValue(type2, "2.003", "MODIFIED-A");
        editor.SetFieldValue(type2, "2.099", "MODIFIED-B");
        editor.SetFieldValue(type2, "2.100", "MODIFIED-C");

        // Assert - Verify values were updated
        type2.GetFieldValue("2.003").Should().Be("MODIFIED-A");
        type2.GetFieldValue("2.099").Should().Be("MODIFIED-B");
        type2.GetFieldValue("2.100").Should().Be("MODIFIED-C");

        // Verify all fields stayed in UnknownFields
        type2.UnknownFields.Should().ContainKey("2.003");
        type2.UnknownFields.Should().ContainKey("2.099");
        type2.UnknownFields.Should().ContainKey("2.100");

        // Verify fields were NOT moved to Fields
        type2.Fields.Should().NotContainKey("2.003");
        type2.Fields.Should().NotContainKey("2.099");
        type2.Fields.Should().NotContainKey("2.100");

        // Verify no duplicates across both collections
        var totalFieldCount =
            (type2.Fields.ContainsKey("2.003") ? 1 : 0) + (type2.UnknownFields.ContainsKey("2.003") ? 1 : 0) +
            (type2.Fields.ContainsKey("2.099") ? 1 : 0) + (type2.UnknownFields.ContainsKey("2.099") ? 1 : 0) +
            (type2.Fields.ContainsKey("2.100") ? 1 : 0) + (type2.UnknownFields.ContainsKey("2.100") ? 1 : 0);
        totalFieldCount.Should().Be(3, "each field should exist in exactly one collection");
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
        type2.UpdateField("2.002", "01"); // Add IDC field
        type2.UpdateField("2.004", "Doe");
        type2.UpdateField("2.005", "Jane");
        type2.UpdateField("2.024", "F");
        transaction.Records.Add(type2);

        // Validate
        var validationResults = editor.ValidateAllChanges();
        validationResults.IsValid.Should().BeTrue($"Validation failed: {validationResults}");

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
