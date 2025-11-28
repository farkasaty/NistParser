using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NistParser;
using NistParser.Writing;
using Xunit;

namespace NistParser.Tests
{
    public class Field9137Tests
    {
        [Fact]
        public void Field9137_ShouldPreserveDataAfterRoundtrip()
        {
            // Arrange
            string origFile = Path.Combine("example files", "hibas", "orig_SIS_MMS_C_P25M.nist");
            if (!File.Exists(origFile))
            {
                // Skip test if file not found
                return;
            }

            byte[] origBytes = File.ReadAllBytes(origFile);

            // Act - Parse
            var transaction = NistTransactionParser.Parse(origBytes);
            var type9 = transaction.Records.FirstOrDefault(r => (int)r.RecordType == 9);
            type9.Should().NotBeNull();

            var field9137 = type9!.GetField("9.137");
            field9137.Should().NotBeNull();

            // Capture original structure
            int origSubfieldCount = field9137!.Subfields.Count;
            int origFirstSubfieldItemCount = field9137.Subfields[0].Items.Count;
            string origFirstItem = field9137.Subfields[0].Items[0];

            // Act - Write
            byte[] newBytes = NistTransactionWriter.WriteTraditional(transaction);

            // Act - Re-parse
            var transaction2 = NistTransactionParser.Parse(newBytes);
            var type9_2 = transaction2.Records.FirstOrDefault(r => (int)r.RecordType == 9);
            var field9137_2 = type9_2!.GetField("9.137");

            // Assert
            field9137_2.Should().NotBeNull();
            field9137_2!.Subfields.Count.Should().Be(origSubfieldCount, "subfield count should be preserved");
            field9137_2.Subfields[0].Items.Count.Should().Be(origFirstSubfieldItemCount, "item count should be preserved");
            field9137_2.Subfields[0].Items[0].Should().Be(origFirstItem, "first item value should be preserved");
        }
    }
}
