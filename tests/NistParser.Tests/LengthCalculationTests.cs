using System;
using System.Linq;
using NistParser.Core;
using NistParser.Records;
using NistParser.Writing;
using Xunit;

namespace NistParser.Tests
{
    public class LengthCalculationTests
    {
        [Fact]
        public void Type2Record_WithOnlyLenField_HasCorrectLength()
        {
            // Arrange
            var type2 = new Type2Record();
            // No fields added. LEN is added automatically during write.

            var transaction = new NistTransaction();
            transaction.Header = new Type1Record();
            transaction.Records.Add(type2);

            // Act
            var bytes = TraditionalFormatWriter.Write(transaction);

            // Assert
            // Find start of Type-2
            // Type-1 ends with FS (0x1C).
            int endOfType1 = Array.IndexOf(bytes, (byte)0x1C);
            Assert.True(endOfType1 >= 0, "Type-1 record not found");

            // Type-2 starts at endOfType1 + 1
            int startOfType2 = endOfType1 + 1;
            int endOfType2 = Array.IndexOf(bytes, (byte)0x1C, startOfType2);
            Assert.True(endOfType2 >= 0, "Type-2 record not found");

            int actualLength = endOfType2 - startOfType2 + 1; // Include FS

            // Parse LEN field from bytes
            // Format: 2.001:LENGTH<GS/FS>
            // But if it's the only field, it might be 2.001:LENGTH<FS>
            
            // Extract the field content
            int colonIndex = Array.IndexOf(bytes, (byte)':', startOfType2);
            Assert.True(colonIndex > startOfType2);
            
            // Find separator (GS=0x1D or FS=0x1C if last)
            int sepIndex = -1;
            for (int i = colonIndex; i <= endOfType2; i++)
            {
                if (bytes[i] == 0x1D || bytes[i] == 0x1C)
                {
                    sepIndex = i;
                    break;
                }
            }
            Assert.True(sepIndex > colonIndex);

            string lenString = System.Text.Encoding.UTF8.GetString(bytes, colonIndex + 1, sepIndex - (colonIndex + 1));
            int statedLength = int.Parse(lenString);

            Assert.Equal(actualLength, statedLength);
        }

        [Fact]
        public void Type2Record_WithOneField_HasCorrectLength()
        {
            // Arrange
            var type2 = new Type2Record();
            type2.UpdateField("2.002", "00"); // IDC

            var transaction = new NistTransaction();
            transaction.Header = new Type1Record();
            transaction.Records.Add(type2);

            // Act
            var bytes = TraditionalFormatWriter.Write(transaction);

            // Assert
            // Find start of Type-2
            int endOfType1 = Array.IndexOf(bytes, (byte)0x1C);
            int startOfType2 = endOfType1 + 1;
            int endOfType2 = Array.IndexOf(bytes, (byte)0x1C, startOfType2);
            int actualLength = endOfType2 - startOfType2 + 1;

            // Parse LEN field
            int colonIndex = Array.IndexOf(bytes, (byte)':', startOfType2);
            int sepIndex = -1;
            for (int i = colonIndex; i <= endOfType2; i++)
            {
                if (bytes[i] == 0x1D || bytes[i] == 0x1C)
                {
                    sepIndex = i;
                    break;
                }
            }

            string lenString = System.Text.Encoding.UTF8.GetString(bytes, colonIndex + 1, sepIndex - (colonIndex + 1));
            int statedLength = int.Parse(lenString);

            Assert.Equal(actualLength, statedLength);
        }
        [Fact]
        public void Type2Record_LengthCalculation_IsCorrect_AroundBoundary()
        {
            // Test lengths around the 100-byte boundary (where length string changes from 2 to 3 digits)
            // This regression test addresses the issue where the estimated length field size calculation 
            // would be incorrect when the length crossed a digit boundary (e.g., 99 -> 100).
            
            // We iterate through a range of content sizes to ensure we cover the transition
            for (int i = 0; i < 50; i++)
            {
                // Arrange
                var type2 = new Type2Record();
                type2.UpdateField("2.002", "00"); // Standard IDC field

                // Add a variable length field
                // 2.003 field overhead is 5 chars (tag) + 1 (colon) + 1 (GS) = 7 bytes
                // Plus the content length
                string variableContent = new string('X', 50 + i); 
                type2.UpdateField("2.003", variableContent);

                var transaction = new NistTransaction();
                transaction.Header = new Type1Record();
                transaction.Records.Add(type2);

                // Act
                var bytes = TraditionalFormatWriter.Write(transaction);

                // Assert
                VerifyRecordLength(bytes, 2);
            }
        }

        private void VerifyRecordLength(byte[] bytes, int recordType)
        {
            // Find start of the record
            // Type-1 ends with FS (0x1C)
            int endOfType1 = Array.IndexOf(bytes, (byte)0x1C);
            Assert.True(endOfType1 >= 0, "Type-1 record not found");

            int startOfRecord = -1;
            if (recordType == 1)
            {
                startOfRecord = 0;
            }
            else
            {
                // Assuming we are testing the first record after Type-1
                startOfRecord = endOfType1 + 1;
            }

            // Find end of the record (FS)
            int endOfRecord = Array.IndexOf(bytes, (byte)0x1C, startOfRecord);
            Assert.True(endOfRecord >= 0, $"Record Type-{recordType} end not found");

            int actualLength = endOfRecord - startOfRecord + 1; // Include FS

            // Parse LEN field from bytes
            // Format: T.001:LENGTH<GS> (or <FS> if it's the only field, though rarely for Type-2)
            
            // Extract the field content
            int colonIndex = Array.IndexOf(bytes, (byte)':', startOfRecord);
            Assert.True(colonIndex > startOfRecord, "LEN field separator not found");
            
            // Find separator (GS=0x1D or FS=0x1C)
            int sepIndex = -1;
            for (int k = colonIndex; k <= endOfRecord; k++)
            {
                if (bytes[k] == 0x1D || bytes[k] == 0x1C)
                {
                    sepIndex = k;
                    break;
                }
            }
            Assert.True(sepIndex > colonIndex, "LEN field terminator not found");

            string lenString = System.Text.Encoding.UTF8.GetString(bytes, colonIndex + 1, sepIndex - (colonIndex + 1));
            int statedLength = int.Parse(lenString);

            Assert.Equal(actualLength, statedLength);
        }
    }
}
