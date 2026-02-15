using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Records;

namespace NistParser.Writing
{

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

            // Create the biom:Transaction container
            var transaction = new XElement(biom + "Transaction");

            // Field 1.002 - Version (split into major and minor)
            var versionField = record.GetField("1.002");
            if (versionField?.FirstValue != null && versionField.FirstValue.Length == 4)
            {
                string major = versionField.FirstValue.Substring(0, 2);
                string minor = versionField.FirstValue.Substring(2, 2);
                transaction.Add(new XElement(biom + "TransactionMajorVersionValue", major));
                transaction.Add(new XElement(biom + "TransactionMinorVersionValue", minor));
            }

            // Field 1.004 - Type of Transaction (TOT)
            var totField = record.GetField("1.004");
            if (totField?.FirstValue != null)
            {
                transaction.Add(new XElement(biom + "TransactionCategoryCode", totField.FirstValue));
            }

            // Field 1.005 - Date (DAT) - convert from YYYYMMDD to YYYY-MM-DD
            var dateField = record.GetField("1.005");
            if (dateField?.FirstValue != null && dateField.FirstValue.Length == 8)
            {
                string isoDate = $"{dateField.FirstValue.Substring(0, 4)}-{dateField.FirstValue.Substring(4, 2)}-{dateField.FirstValue.Substring(6, 2)}";
                transaction.Add(new XElement(biom + "TransactionDate",
                    new XElement(nc + "Date", isoDate)));
            }

            // Field 1.006 - Priority (PRY)
            var priorityField = record.GetField("1.006");
            if (priorityField?.FirstValue != null)
            {
                transaction.Add(new XElement(biom + "TransactionPriorityValue", priorityField.FirstValue));
            }

            // Field 1.007 - Destination Agency Identifier (DAI)
            var daiField = record.GetField("1.007");
            if (daiField?.FirstValue != null)
            {
                transaction.Add(new XElement(biom + "TransactionDestinationOrganization",
                    new XElement(nc + "OrganizationIdentification",
                        new XElement(nc + "IdentificationID", daiField.FirstValue))));
            }

            // Field 1.008 - Originating Agency Identifier (ORI)
            var oriField = record.GetField("1.008");
            var oanField = record.GetField("1.017"); // Originating Agency Name
            if (oriField?.FirstValue != null || oanField?.FirstValue != null)
            {
                var origOrg = new XElement(biom + "TransactionOriginatingOrganization");

                if (oriField?.FirstValue != null)
                {
                    origOrg.Add(new XElement(nc + "OrganizationIdentification",
                        new XElement(nc + "IdentificationID", oriField.FirstValue)));
                }

                if (oanField?.FirstValue != null)
                {
                    origOrg.Add(new XElement(nc + "OrganizationName", oanField.FirstValue));
                }

                transaction.Add(origOrg);
            }

            // Field 1.009 - Transaction Control Number (TCN)
            var tcnField = record.GetField("1.009");
            if (tcnField?.FirstValue != null)
            {
                transaction.Add(new XElement(biom + "TransactionControlIdentification",
                    new XElement(nc + "IdentificationID", tcnField.FirstValue)));
            }

            // Field 1.010 - Transaction Control Reference (TCR)
            var tcrField = record.GetField("1.010");
            if (tcrField?.FirstValue != null)
            {
                transaction.Add(new XElement(biom + "TransactionControlReferenceIdentification",
                    new XElement(nc + "IdentificationID", tcrField.FirstValue)));
            }

            // Field 1.011/1.012 - Resolution details
            var nsrField = record.GetField("1.011");
            var ntrField = record.GetField("1.012");
            if (nsrField?.FirstValue != null || ntrField?.FirstValue != null)
            {
                var resolutionDetails = new XElement(biom + "TransactionImageResolutionDetails");

                if (nsrField?.FirstValue != null)
                {
                    resolutionDetails.Add(new XElement(biom + "NativeScanningResolutionValue", nsrField.FirstValue));
                }

                if (ntrField?.FirstValue != null)
                {
                    resolutionDetails.Add(new XElement(biom + "NominalTransmittingResolutionValue", ntrField.FirstValue));
                }

                transaction.Add(resolutionDetails);
            }

            // Field 1.013 - Domain Name (DOM)
            var domField = record.GetField("1.013");
            if (domField?.FirstValue != null)
            {
                transaction.Add(new XElement(biom + "TransactionDomain",
                    new XElement(biom + "DomainVersionNumberIdentification",
                        new XElement(nc + "IdentificationID", domField.FirstValue))));
            }

            // Field 1.003 - Transaction Content (CNT)
            if (record.Content != null)
            {
                var contentSummary = new XElement(biom + "TransactionContentSummary",
                    new XElement(biom + "ContentRecordCount", record.Content.RecordCount));

                foreach (var entry in record.Content.Records)
                {
                    contentSummary.Add(new XElement(biom + "ContentRecordSummary",
                        new XElement(itl + "RecordCategoryCode", entry.RecordType),
                        new XElement(biom + "ImageReferenceIdentification",
                            new XElement(nc + "IdentificationID", entry.IDC))));
                }

                transaction.Add(contentSummary);
            }

            // Add the Transaction element to the PackageInformationRecord
            element.Add(transaction);

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
                RecordType.Type14_FingerprintImage => WriteType14Record((Type14Record)record),
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

            // Add all defined fields as UserDefinedDescriptiveText elements
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

            // Add all unknown fields as UserDefinedDescriptiveText elements
            foreach (var unknownField in record.UnknownFields)
            {
                if (!string.IsNullOrEmpty(unknownField.Value))
                {
                    element.Add(new XElement(itl + "UserDefinedDescriptiveText",
                    new XElement(itl + "UserDefinedFieldID", unknownField.Key),
                    new XElement(nc + "DescriptionText", unknownField.Value)
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
            AddFieldAsElement(element, record, "14.013", itl + "FingerprintImage", biom + "FingerPositionCode");

            // Add image properties (per ANSI/NIST-ITL 1-2011 Update:2015)
            AddFieldAsElement(element, record, "14.006", itl + "FingerprintImage", biom + "ImageHorizontalLineLength");
            AddFieldAsElement(element, record, "14.007", itl + "FingerprintImage", biom + "ImageVerticalLineLength");
            AddFieldAsElement(element, record, "14.008", itl + "FingerprintImage", biom + "ImageScaleUnitsCode");
            AddFieldAsElement(element, record, "14.009", itl + "FingerprintImage", biom + "ImageHorizontalPixelScale");
            AddFieldAsElement(element, record, "14.011", itl + "FingerprintImage", biom + "ImageCompressionAlgorithmCode");

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
}
