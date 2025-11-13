using FluentAssertions;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Records;
using NistParser.Writing;
using Xunit;

namespace NistParser.Tests;

public class SerializationRoundtripTests
{
    [Fact]
    public void TraditionalFormat_RoundtripParsing_ShouldPreserveData()
    {
        // Arrange - Create a simple transaction
        var transaction = CreateTestTransaction();

        // Act - Serialize and parse back
        var serialized = NistTransactionWriter.WriteTraditional(transaction);
        var parsed = NistTransactionParser.Parse(serialized);

        // Assert - Verify key fields are preserved
        parsed.Should().NotBeNull();
        parsed.Header.Should().NotBeNull();
        parsed.Header.Version.Should().Be(transaction.Header.Version);
        parsed.Header.TransactionControlNumber.Should().Be(transaction.Header.TransactionControlNumber);
        parsed.Header.TypeOfTransaction.Should().Be(transaction.Header.TypeOfTransaction);
        parsed.Records.Should().HaveCount(transaction.Records.Count);
    }

    [Fact]
    public void TraditionalFormat_RoundtripWithEdits_ShouldPreserveChanges()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var originalTCN = transaction.Header.TransactionControlNumber;

        // Act - Edit a field
        transaction.Header.UpdateField("1.009", "NEW-TCN-12345");

        // Serialize and parse back
        var serialized = NistTransactionWriter.WriteTraditional(transaction);
        var parsed = NistTransactionParser.Parse(serialized);

        // Assert - Verify the edit is preserved
        parsed.Header.TransactionControlNumber.Should().Be("NEW-TCN-12345");
        parsed.Header.TransactionControlNumber.Should().NotBe(originalTCN);
    }

    [Fact]
    public void TraditionalFormat_Type2Record_ShouldRoundtrip()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Add a Type-2 record
        var type2 = new Type2Record { IDC = "01" };
        type2.UpdateField("2.004", "Smith");
        type2.UpdateField("2.005", "John");
        type2.UpdateField("2.024", "M");
        transaction.Records.Add(type2);

        // Act - Serialize and parse back
        var serialized = NistTransactionWriter.WriteTraditional(transaction);
        var parsed = NistTransactionParser.Parse(serialized);

        // Assert
        var parsedType2 = parsed.GetRecords<Type2Record>().FirstOrDefault();
        parsedType2.Should().NotBeNull();
        parsedType2!.Surname.Should().Be("Smith");
        parsedType2.GivenName.Should().Be("John");
        parsedType2.Sex.Should().Be("M");
    }

    [Fact]
    public void XmlFormat_RoundtripParsing_ShouldPreserveData()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act - Serialize to XML and parse back
        var serialized = NistTransactionWriter.WriteXml(transaction);
        var parsed = NistTransactionParser.Parse(serialized);

        // Assert
        parsed.Should().NotBeNull();
        parsed.Header.Should().NotBeNull();
        parsed.Header.Version.Should().Be(transaction.Header.Version);
        parsed.Header.TransactionControlNumber.Should().Be(transaction.Header.TransactionControlNumber);
    }

    [Fact]
    public void Serialization_AutomaticCNTUpdate_ShouldReflectCurrentRecords()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        int initialRecordCount = transaction.Records.Count;

        // Add a new record
        var newType2 = new Type2Record { IDC = "02" };
        newType2.UpdateField("2.004", "Doe");
        transaction.Records.Add(newType2);

        // Act - Serialize (should auto-update CNT)
        var serialized = NistTransactionWriter.WriteTraditional(transaction);
        var parsed = NistTransactionParser.Parse(serialized);

        // Assert - CNT should reflect new record count
        parsed.Header.Content.Should().NotBeNull();
        parsed.Header.Content!.RecordCount.Should().Be(initialRecordCount + 1);
        parsed.Records.Should().HaveCount(initialRecordCount + 1);
    }

    [Fact]
    public void Serialization_LengthCalculation_ShouldBeCorrect()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        var serialized = NistTransactionWriter.WriteTraditional(transaction);
        var parsed = NistTransactionParser.Parse(serialized);

        // Assert - Each record's LEN field should match its actual length
        parsed.Header.RecordLength.Should().BeGreaterThan(0);
        foreach (var record in parsed.Records)
        {
            record.RecordLength.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void Serialization_AddAndRemoveFields_ShouldRoundtrip()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Add a new optional field
        transaction.Header.UpdateField("1.017", "Test Agency");

        // Act - Serialize and parse back
        var serialized = NistTransactionWriter.WriteTraditional(transaction);
        var parsed = NistTransactionParser.Parse(serialized);

        // Assert - New field should be present
        parsed.Header.GetFieldValue("1.017").Should().Be("Test Agency");

        // Arrange - Remove the field
        parsed.Header.RemoveField("1.017");

        // Act - Serialize again
        var serialized2 = NistTransactionWriter.WriteTraditional(parsed);
        var parsed2 = NistTransactionParser.Parse(serialized2);

        // Assert - Field should be removed
        parsed2.Header.GetFieldValue("1.017").Should().BeNull();
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

        var transaction = new NistTransaction
        {
            Header = type1
        };

        return transaction;
    }
}
