using System.Text;
using System.Xml.Linq;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Records;

namespace NistParser.Writing;

/// <summary>
/// Writes NIST transactions in NIEM Conformant XML format
/// </summary>
internal static class XmlFormatWriter
{
    // NIEM XML Namespaces
    private static readonly XNamespace itl = "http://biometrics.nist.gov/standard/2011";
    private static readonly XNamespace biom = "http://niem.gov/niem/biometrics/1.0";
    private static readonly XNamespace nc = "http://niem.gov/niem/niem-core/2.0";
    private static readonly XNamespace s = "http://niem.gov/niem/structures/2.0";

    /// <summary>
    /// Writes a NistTransaction to NIEM XML format
    /// </summary>
    public static byte[] Write(NistTransaction transaction)
    {
        if (transaction.Header == null)
        {
            throw new InvalidOperationException("Transaction must have a Type-1 header record");
        }

        // Create XML document
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            CreateRootElement(transaction)
        );

        // Convert to bytes
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8);
        doc.Save(writer);
        writer.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Creates the root XML element
    /// </summary>
    private static XElement CreateRootElement(NistTransaction transaction)
    {
        var root = new XElement(itl + "NISTBiometricInformationExchangePackage",
            new XAttribute(XNamespace.Xmlns + "itl", itl.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "biom", biom.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "nc", nc.NamespaceName),
            new XAttribute(XNamespace.Xmlns + "s", s.NamespaceName)
        );

        // Add Type-1 record (PackageInformationRecord)
        root.Add(WriteType1Record(transaction.Header));

        // Add all other records
        foreach (var record in transaction.Records)
        {
            var element = WriteRecord(record);
            if (element != null)
            {
                root.Add(element);
            }
        }

        return root;
    }

    /// <summary>
    /// Writes Type-1 record as PackageInformationRecord
    /// </summary>
    private static XElement WriteType1Record(Type1Record record)
    {
        var element = new XElement(itl + "PackageInformationRecord");

        // Add record category code (Type-1 indicator)
        element.Add(new XElement(biom + "RecordCategoryCode", "1"));

        // Add common fields as XML elements
        AddFieldAsElement(element, record, "1.002", itl + "Transaction", itl + "TransactionVersion");
        AddFieldAsElement(element, record, "1.004", itl + "Transaction", itl + "TransactionCategoryCode");
        AddFieldAsElement(element, record, "1.005", itl + "Transaction", itl + "TransactionDate");
        AddFieldAsElement(element, record, "1.006", itl + "Transaction", itl + "TransactionPriorityCode");

        // Destination and Originating Agency
        AddFieldAsElement(element, record, "1.007", itl + "Transaction", itl + "DestinationAgencyIdentifier");
        AddFieldAsElement(element, record, "1.008", itl + "Transaction", itl + "OriginatingAgencyIdentifier");
        AddFieldAsElement(element, record, "1.009", itl + "Transaction", itl + "TransactionControlNumber");

        // Optional fields
        AddFieldAsElement(element, record, "1.011", itl + "Transaction", itl + "NativeScanningResolution");
        AddFieldAsElement(element, record, "1.012", itl + "Transaction", itl + "NominalTransmittingResolution");
        AddFieldAsElement(element, record, "1.013", itl + "Transaction", itl + "DomainName");
        AddFieldAsElement(element, record, "1.017", itl + "Transaction", itl + "OriginatingAgencyName");

        // Add CNT field as TransactionContentSummary
        if (record.Content != null)
        {
            var contentElement = new XElement(itl + "Transaction",
                new XElement(itl + "TransactionContentSummary",
                    new XElement(itl + "ContentRecordCount", record.Content.RecordCount)
                )
            );

            foreach (var entry in record.Content.Records)
            {
                contentElement.Element(itl + "TransactionContentSummary")?.Add(
                    new XElement(itl + "ContentRecordSummary",
                        new XElement(itl + "RecordCategoryCode", entry.RecordType),
                        new XElement(biom + "ImageReferenceIdentification",
                            new XElement(nc + "IdentificationID", entry.IDC)
                        )
                    )
                );
            }

            element.Add(contentElement);
        }

        return element;
    }

    /// <summary>
    /// Writes any record type
    /// </summary>
    private static XElement? WriteRecord(NistRecord record)
    {
        return record.RecordType switch
        {
            RecordType.Type2_UserDefinedText => WriteType2Record((Type2Record)record),
            RecordType.Type14_Fingerprint => WriteType14Record((Type14Record)record),
            _ => WriteGenericRecord(record)
        };
    }

    /// <summary>
    /// Writes Type-2 record as PackageDescriptiveTextRecord
    /// </summary>
    private static XElement WriteType2Record(Type2Record record)
    {
        var element = new XElement(itl + "PackageDescriptiveTextRecord");

        // Add record category code
        element.Add(new XElement(biom + "RecordCategoryCode", "2"));

        // Add IDC
        element.Add(new XElement(biom + "ImageReferenceIdentification",
            new XElement(nc + "IdentificationID", record.IDC)
        ));

        // Add all fields as UserDefinedDescriptiveText elements
        foreach (var field in record.Fields.Values.Where(f => !f.FieldNumber.EndsWith(".001") && !f.FieldNumber.EndsWith(".002")))
        {
            if (field.FirstValue != null)
            {
                element.Add(new XElement(itl + "UserDefinedDescriptiveText",
                    new XElement(itl + "UserDefinedFieldID", field.FieldNumber),
                    new XElement(nc + "DescriptionText", field.FirstValue)
                ));
            }
        }

        return element;
    }

    /// <summary>
    /// Writes Type-14 record as PackageFingerprintImageRecord
    /// </summary>
    private static XElement WriteType14Record(Type14Record record)
    {
        var element = new XElement(itl + "PackageFingerprintImageRecord");

        // Add record category code
        element.Add(new XElement(biom + "RecordCategoryCode", "14"));

        // Add IDC
        element.Add(new XElement(biom + "ImageReferenceIdentification",
            new XElement(nc + "IdentificationID", record.IDC)
        ));

        // Add common fields
        AddFieldAsElement(element, record, "14.003", itl + "FingerprintImage", biom + "ImpressionTypeCode");
        AddFieldAsElement(element, record, "14.004", itl + "FingerprintImage", biom + "SourceAgencyCode");
        AddFieldAsElement(element, record, "14.005", itl + "FingerprintImage", biom + "FingerPositionCode");

        // Add image properties
        AddFieldAsElement(element, record, "14.006", itl + "FingerprintImage", biom + "ImageScanningResolution");
        AddFieldAsElement(element, record, "14.007", itl + "FingerprintImage", biom + "ImageHorizontalLineLength");
        AddFieldAsElement(element, record, "14.008", itl + "FingerprintImage", biom + "ImageVerticalLineLength");
        AddFieldAsElement(element, record, "14.009", itl + "FingerprintImage", biom + "ImageCompressionAlgorithmCode");

        // Add binary data if present
        var dataField = record.GetField("14.999");
        if (dataField?.BinaryData != null)
        {
            var base64Data = Convert.ToBase64String(dataField.BinaryData);
            element.Add(new XElement(itl + "FingerprintImage",
                new XElement(nc + "BinaryBase64Object", base64Data)
            ));
        }

        return element;
    }

    /// <summary>
    /// Writes a generic record (for unsupported types)
    /// </summary>
    private static XElement WriteGenericRecord(NistRecord record)
    {
        var element = new XElement(itl + "PackageGenericRecord");

        // Add record category code
        element.Add(new XElement(biom + "RecordCategoryCode", ((int)record.RecordType).ToString()));

        // Add IDC
        element.Add(new XElement(biom + "ImageReferenceIdentification",
            new XElement(nc + "IdentificationID", record.IDC)
        ));

        // Add all fields
        foreach (var field in record.Fields.Values.Where(f => !f.FieldNumber.EndsWith(".001") && !f.FieldNumber.EndsWith(".002")))
        {
            if (field.FirstValue != null)
            {
                element.Add(new XElement(itl + "GenericFieldData",
                    new XElement(itl + "FieldID", field.FieldNumber),
                    new XElement(nc + "FieldValueText", field.FirstValue)
                ));
            }
        }

        return element;
    }

    /// <summary>
    /// Helper to add a field as an XML element
    /// </summary>
    private static void AddFieldAsElement(XElement parent, NistRecord record, string fieldNumber,
        XName containerName, XName elementName)
    {
        var field = record.GetField(fieldNumber);
        if (field?.FirstValue != null)
        {
            var container = parent.Elements(containerName).FirstOrDefault();
            if (container == null)
            {
                container = new XElement(containerName);
                parent.Add(container);
            }
            container.Add(new XElement(elementName, field.FirstValue));
        }
    }
}
