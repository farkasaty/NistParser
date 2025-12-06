using Xunit;
using NistParser.Core;
using NistParser.Records;
using NistParser.Writing;
using System.Text;

namespace NistParser.Tests
{
    public class DebugXmlTest
    {
        [Fact]
        public void Debug_XmlRoundtrip_CheckFieldNumbers()
        {
            // Arrange
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

            // Check that field exists before XML conversion
            var versionBefore = type1.GetFieldValue("1.002");
            Assert.Equal("0502", versionBefore);

            // Act - Serialize to XML
            var xmlBytes = NistTransactionWriter.WriteXml(transaction);
            var xmlString = Encoding.UTF8.GetString(xmlBytes);

            // Debug: Save XML to file
            System.IO.File.WriteAllText("debug_output.xml", xmlString);

            // Act - Parse back from XML
            var parsed = NistTransactionParser.Parse(xmlBytes);

            // Assert
            Assert.NotNull(parsed);
            Assert.NotNull(parsed.Header);

            // Debug: Check what fields are present
            var fieldCount = parsed.Header.Fields.Count;
            var fieldNumbers = string.Join(", ", parsed.Header.Fields.Keys);

            // Check field directly
            var versionField = parsed.Header.GetField("1.002");

            // If null, provide debug info
            if (versionField == null)
            {
                throw new System.Exception($"Field 1.002 not found. Fields present: {fieldNumbers} (Count: {fieldCount})");
            }

            // Check value
            var versionValue = parsed.Header.GetFieldValue("1.002");
            Assert.Equal("0502", versionValue);

            // Check via property
            Assert.Equal("0502", parsed.Header.Version);
        }
    }
}
