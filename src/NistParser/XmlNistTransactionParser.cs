using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NistParser.Constants;
using NistParser.Core;
using NistParser.Exceptions;
using NistParser.Records;

namespace NistParser
{

    /// <summary>
    /// Parser for ANSI/NIST-ITL files in NIEM Conformant XML format.
    /// Implements XML parsing according to ANSI/NIST-ITL 1-2011 NIEM encoding.
    /// </summary>
    public static class XmlNistTransactionParser
    {
        // NIEM XML Namespaces
        private static readonly XNamespace itl = "http://biometrics.nist.gov/standard/2011";
        private static readonly XNamespace biom = "http://niem.gov/niem/biometrics/1.0";
        private static readonly XNamespace nc = "http://niem.gov/niem/niem-core/2.0";
        private static readonly XNamespace s = "http://niem.gov/niem/structures/2.0";

        /// <summary>
        /// Parses a NIEM XML ANSI/NIST-ITL file from a byte array
        /// </summary>
        /// <param name="xmlData">The raw XML file data</param>
        /// <returns>The parsed transaction</returns>
        /// <exception cref="MissingType1Exception">Thrown if Type-1 record is missing</exception>
        /// <exception cref="NistParserException">Thrown for other parsing errors</exception>
        public static NistTransaction Parse(byte[] xmlData)
        {
            if (xmlData == null || xmlData.Length == 0)
            {
                throw new NistParserException("XML data is null or empty");
            }

            try
            {
                using var stream = new MemoryStream(xmlData);
                XDocument doc = XDocument.Load(stream);
                return ParseXmlDocument(doc, xmlData);
            }
            catch (System.Xml.XmlException ex)
            {
                throw new NistParserException($"Failed to parse XML: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Parses a NIEM XML ANSI/NIST-ITL file from a file path
        /// </summary>
        /// <param name="xmlFilePath">Path to the XML file</param>
        /// <returns>The parsed transaction</returns>
        public static NistTransaction ParseFile(string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
            {
                throw new FileNotFoundException($"NIST XML file not found: {xmlFilePath}", xmlFilePath);
            }

            byte[] xmlData = File.ReadAllBytes(xmlFilePath);
            return Parse(xmlData);
        }

        /// <summary>
        /// Parses the XML document into a NistTransaction
        /// </summary>
        private static NistTransaction ParseXmlDocument(XDocument doc, byte[] rawData)
        {
            var transaction = new NistTransaction
            {
                RawData = rawData
            };

            XElement? root = doc.Root;
            if (root == null || root.Name != itl + "NISTBiometricInformationExchangePackage")
            {
                throw new NistParserException(
                "Invalid NIEM XML: root element must be <itl:NISTBiometricInformationExchangePackage>");
            }

            // Parse Type-1 record (mandatory, must be first)
            XElement? type1Element = root.Element(itl + "PackageInformationRecord");
            if (type1Element == null)
            {
                throw new MissingType1Exception(
                "Type-1 record (<itl:PackageInformationRecord>) not found in XML");
            }

            transaction.Header = ParseType1Record(type1Element);

            // Parse all other records
            foreach (XElement element in root.Elements())
            {
                // Skip Type-1 (already parsed)
                if (element.Name == itl + "PackageInformationRecord")
                continue;

                NistRecord? record = ParseRecordByElement(element);
                if (record != null)
                {
                    transaction.Records.Add(record);
                }
            }

            return transaction;
        }

        /// <summary>
        /// Parses a record element based on its XML element name
        /// </summary>
        private static NistRecord? ParseRecordByElement(XElement element)
        {
            if (element.Name == itl + "PackageDescriptiveTextRecord")
            {
                return ParseType2Record(element);
            }
            else if (element.Name == itl + "PackageFingerprintImageRecord")
            {
                return ParseType14Record(element);
            }
            else if (element.Name == itl + "PackageFacialAndSMTImageRecord")
            {
                return ParseGenericImageRecord(element, RecordType.Type10_FacialAndSMTImage);
            }
            else if (element.Name == itl + "PackageLatentImageRecord")
            {
                return ParseGenericImageRecord(element, RecordType.Type13_LatentImage);
            }
            else if (element.Name == itl + "PackageIrisImageRecord")
            {
                return ParseGenericImageRecord(element, RecordType.Type17_IrisImage);
            }
            else
            {
                // Unknown record type - create generic record
                return ParseGenericRecord(element);
            }
        }

        /// <summary>
        /// Parses Type-1 PackageInformationRecord
        /// </summary>
        private static Type1Record ParseType1Record(XElement element)
        {
            var record = new Type1Record();

            // Note: RecordCategoryCode is just a type indicator, not field 1.001
            // Field 1.001 (LEN) is the record length, which is calculated in traditional format
            // In XML format, we skip it as record boundaries are defined by XML structure

            XElement? transaction = element.Element(biom + "Transaction");
            if (transaction != null)
            {
                // Field 1.002 - Version (VER)
                ParseVersion(transaction, record);

                // Field 1.003 - Transaction Content (CNT)
                ParseTransactionContent(transaction, record);

                // Field 1.004 - Type of Transaction (TOT)
                XElement? transactionCategory = transaction.Element(biom + "TransactionCategoryCode");
                if (transactionCategory != null)
                {
                    AddField(record, "1.004", transactionCategory.Value);
                }

                // Field 1.005 - Date (DAT)
                XElement? transactionDate = transaction.Element(biom + "TransactionDate");
                if (transactionDate != null)
                {
                    XElement? date = transactionDate.Element(nc + "Date");
                    if (date != null)
                    {
                        // Convert from ISO format (YYYY-MM-DD) to NIST format (YYYYMMDD)
                        string dateValue = date.Value.Replace("-", "");
                        AddField(record, "1.005", dateValue);
                    }
                }

                // Field 1.006 - Priority (PRY)
                XElement? priority = transaction.Element(biom + "TransactionPriorityValue");
                if (priority != null)
                {
                    AddField(record, "1.006", priority.Value);
                }

                // Field 1.007 - Destination Agency Identifier (DAI)
                XElement? destOrg = transaction.Element(biom + "TransactionDestinationOrganization");
                if (destOrg != null)
                {
                    string? dai = GetOrganizationId(destOrg);
                    if (dai != null)
                    {
                        AddField(record, "1.007", dai);
                    }
                }

                // Field 1.008 - Originating Agency Identifier (ORI)
                XElement? origOrg = transaction.Element(biom + "TransactionOriginatingOrganization");
                if (origOrg != null)
                {
                    string? ori = GetOrganizationId(origOrg);
                    if (ori != null)
                    {
                        AddField(record, "1.008", ori);
                    }

                    // Field 1.017 - Originating Agency Name (OAN)
                    XElement? orgName = origOrg.Element(nc + "OrganizationName");
                    if (orgName != null)
                    {
                        AddField(record, "1.017", orgName.Value);
                    }
                }

                // Field 1.009 - Transaction Control Number (TCN)
                XElement? tcn = transaction.Element(biom + "TransactionControlIdentification");
                if (tcn != null)
                {
                    XElement? tcnId = tcn.Element(nc + "IdentificationID");
                    if (tcnId != null)
                    {
                        AddField(record, "1.009", tcnId.Value);
                    }
                }

                // Field 1.010 - Transaction Control Reference (TCR)
                XElement? tcr = transaction.Element(biom + "TransactionControlReferenceIdentification");
                if (tcr != null)
                {
                    XElement? tcrId = tcr.Element(nc + "IdentificationID");
                    if (tcrId != null)
                    {
                        AddField(record, "1.010", tcrId.Value);
                    }
                }

                // Field 1.011/1.012 - Resolution details
                ParseResolutionDetails(transaction, record);

                // Field 1.014 - GMT (UTC Date/Time)
                XElement? utcDate = transaction.Element(biom + "TransactionUTCDate");
                if (utcDate != null)
                {
                    XElement? dateTime = utcDate.Element(nc + "DateTime");
                    if (dateTime != null)
                    {
                        AddField(record, "1.014", dateTime.Value);
                    }
                }
            }

            return record;
        }

        /// <summary>
        /// Parses version fields (1.002)
        /// </summary>
        private static void ParseVersion(XElement transaction, Type1Record record)
        {
            XElement? major = transaction.Element(biom + "TransactionMajorVersionValue");
            XElement? minor = transaction.Element(biom + "TransactionMinorVersionValue");

            if (major != null && minor != null)
            {
                string version = $"{major.Value.PadLeft(2, '0')}{minor.Value.PadLeft(2, '0')}";
                AddField(record, "1.002", version);
            }
        }

        /// <summary>
        /// Parses transaction content summary (1.003 / CNT)
        /// </summary>
        private static void ParseTransactionContent(XElement transaction, Type1Record record)
        {
            XElement? contentSummary = transaction.Element(biom + "TransactionContentSummary");
            if (contentSummary == null)
            return;

            var content = new TransactionContent();

            // Parse record count (CRC)
            XElement? recordCount = contentSummary.Element(biom + "ContentRecordQuantity");
            if (recordCount != null && int.TryParse(recordCount.Value, out int count))
            {
                content.RecordCount = count;
            }

            // Parse each record summary
            foreach (XElement summary in contentSummary.Elements(biom + "ContentRecordSummary"))
            {
                XElement? recCat = summary.Element(biom + "RecordCategoryCode");
                XElement? imgRef = summary.Element(biom + "ImageReferenceIdentification");

                if (recCat != null && imgRef != null)
                {
                    XElement? idcElement = imgRef.Element(nc + "IdentificationID");
                    if (idcElement != null && int.TryParse(recCat.Value, out int recType))
                    {
                        content.Records.Add(new TransactionContent.RecordEntry
                        {
                            RecordType = recType,
                            IDC = idcElement.Value
                        });
                    }
                }
            }

            record.Content = content;

            // Build CNT field string representation
            // Format: recordCount<US>type<US>idc<RS>type<US>idc<RS>...
            var cntBuilder = new StringBuilder();
            cntBuilder.Append(content.RecordCount);
            foreach (var entry in content.Records)
            {
                cntBuilder.Append("|"); // Using | as placeholder for subfield separator
                cntBuilder.Append(entry.RecordType);
                cntBuilder.Append("^"); // Using ^ as placeholder for item separator
                cntBuilder.Append(entry.IDC);
            }

            AddField(record, "1.003", cntBuilder.ToString());
        }

        /// <summary>
        /// Parses resolution details (1.011, 1.012)
        /// </summary>
        private static void ParseResolutionDetails(XElement transaction, Type1Record record)
        {
            XElement? resolutionDetails = transaction.Element(biom + "TransactionImageResolutionDetails");
            if (resolutionDetails == null)
            return;

            // Field 1.011 - Native Scanning Resolution (NSR)
            XElement? nsr = resolutionDetails.Element(biom + "NativeScanningResolutionValue");
            if (nsr != null)
            {
                AddField(record, "1.011", nsr.Value);
            }

            // Field 1.012 - Nominal Transmitting Resolution (NTR)
            XElement? ntr = resolutionDetails.Element(biom + "NominalTransmittingResolutionValue");
            if (ntr != null)
            {
                AddField(record, "1.012", ntr.Value);
            }
        }

        /// <summary>
        /// Parses Type-2 PackageDescriptiveTextRecord
        /// </summary>
        private static NistRecord ParseType2Record(XElement element)
        {
            var record = new Type2Record();

            // Note: RecordCategoryCode is just a type indicator, NOT field 2.001
            // Field 2.001 (LEN) would be the record length in traditional format
            // We skip it in XML format as boundaries are defined by XML structure

            // Field 2.002 - IDC
            string? idc = GetImageReferenceId(element);
            if (idc != null)
            {
                record.IDC = idc;
                AddField(record, "2.002", idc);
            }

            // Parse any other child elements as generic fields
            ParseGenericFields(element, record, 2);

            return record;
        }

        /// <summary>
        /// Parses Type-14 PackageFingerprintImageRecord
        /// </summary>
        private static NistRecord ParseType14Record(XElement element)
        {
            var record = new Type14Record();

            // Note: RecordCategoryCode is just a type indicator, NOT field 14.001
            // Field 14.001 (LEN) would be the record length in traditional format
            // We skip it in XML format as boundaries are defined by XML structure

            // Field 14.002 - IDC
            string? idc = GetImageReferenceId(element);
            if (idc != null)
            {
                record.IDC = idc;
                AddField(record, "14.002", idc);
            }

            // Parse fingerprint image details
            XElement? fingerprintImage = element.Element(biom + "FingerprintImage");
            if (fingerprintImage != null)
            {
                // Field 14.003 - Impression Type (IMP)
                XElement? imp = fingerprintImage.Element(biom + "FingerprintImageImpressionCaptureCategoryCode");
                if (imp != null)
                {
                    AddField(record, "14.003", imp.Value);
                }

                // Field 14.004 - Source Agency (SRC)
                XElement? src = fingerprintImage.Element(biom + "FingerprintImageSourceCode");
                if (src != null)
                {
                    AddField(record, "14.004", src.Value);
                }

                // Field 14.013 - Friction Ridge Generalized Position (FGP)
                XElement? fgp = fingerprintImage.Element(biom + "FingerPositionCode");
                if (fgp != null)
                {
                    AddField(record, "14.013", fgp.Value);
                }

                // Parse image capture details (HLL, VLL, SLC, THPS)
                ParseImageCaptureDetails(fingerprintImage, record, 14);

                // Field 14.011 - Compression Algorithm (CGA)
                XElement? ca = fingerprintImage.Element(biom + "ImageCompressionAlgorithmCode");
                if (ca != null)
                {
                    AddField(record, "14.011", ca.Value);
                }
            }

            // Field 14.999 - Image Data (Base64 encoded)
            XElement? impressionImage = element.Element(biom + "FingerImpressionImage");
            if (impressionImage != null)
            {
                byte[]? imageData = ParseBase64ImageData(impressionImage);
                if (imageData != null)
                {
                    var imageField = new NistField("14.999")
                    {
                        BinaryData = imageData
                    };
                    record.AddField(imageField);
                }
            }

            return record;
        }

        /// <summary>
        /// Parses generic image record (Type-10, 13, 17, etc.)
        /// </summary>
        private static NistRecord ParseGenericImageRecord(XElement element, RecordType recordType)
        {
            var record = new GenericNistRecord(recordType);
            int typeNum = (int)recordType;

            // Note: RecordCategoryCode is just a type indicator, NOT field X.001
            // Field X.001 (LEN) would be the record length in traditional format
            // We skip it in XML format as boundaries are defined by XML structure

            // Field X.002 - IDC
            string? idc = GetImageReferenceId(element);
            if (idc != null)
            {
                record.IDC = idc;
                AddField(record, $"{typeNum}.002", idc);
            }

            // Try to parse image data (field X.999)
            byte[]? imageData = FindAndParseImageData(element);
            if (imageData != null)
            {
                var imageField = new NistField($"{typeNum}.999")
                {
                    BinaryData = imageData
                };
                record.AddField(imageField);
            }

            // Parse other generic fields
            ParseGenericFields(element, record, typeNum);

            return record;
        }

        /// <summary>
        /// Parses completely unknown/generic record
        /// </summary>
        private static NistRecord ParseGenericRecord(XElement element)
        {
            // Try to determine record type from element name or category code
            XElement? categoryCode = element.Element(biom + "RecordCategoryCode");
            int recordTypeNum = 99; // Default to Type-99

            if (categoryCode != null && int.TryParse(categoryCode.Value, out int type))
            {
                recordTypeNum = type;
            }

            var record = new GenericNistRecord((RecordType)recordTypeNum);

            // Parse all fields generically
            ParseGenericFields(element, record, recordTypeNum);

            return record;
        }

        /// <summary>
        /// Parses image capture details (resolution, dimensions)
        /// </summary>
        private static void ParseImageCaptureDetails(XElement imageElement, NistRecord record, int recordType)
        {
            XElement? captureDetail = imageElement.Element(biom + "ImageCaptureDetail");
            if (captureDetail == null)
            return;

            // Field X.006 - Horizontal Line Length (HLL)
            XElement? hll = captureDetail.Element(biom + "CaptureHorizontalLineLengthValue");
            if (hll != null)
            {
                AddField(record, $"{recordType}.006", hll.Value);
            }

            // Field X.007 - Vertical Line Length (VLL)
            XElement? vll = captureDetail.Element(biom + "CaptureVerticalLineLengthValue");
            if (vll != null)
            {
                AddField(record, $"{recordType}.007", vll.Value);
            }

            // Field X.008 - Scale Units (SLC) and X.009 - Resolution (THPS)
            XElement? captureResolution = captureDetail.Element(biom + "CaptureResolution");
            if (captureResolution != null)
            {
                XElement? resUnitCode = captureResolution.Element(biom + "ResolutionUnitCode");
                if (resUnitCode != null)
                {
                    AddField(record, $"{recordType}.008", resUnitCode.Value);
                }

                XElement? resValue = captureResolution.Element(biom + "ResolutionValue");
                if (resValue != null)
                {
                    AddField(record, $"{recordType}.009", resValue.Value);
                }
            }
        }

        /// <summary>
        /// Finds and parses Base64 image data from various possible element locations
        /// </summary>
        private static byte[]? FindAndParseImageData(XElement element)
        {
            // Try common image element names
            string[] imageElementNames = {
                "FingerImpressionImage",
                "FaceImage",
                "LatentImage",
                "IrisImage",
                "PalmImage"
            };

            foreach (string name in imageElementNames)
            {
                XElement? imageElement = element.Element(biom + name);
                if (imageElement != null)
                {
                    byte[]? data = ParseBase64ImageData(imageElement);
                    if (data != null)
                    return data;
                }
            }

            return null;
        }

        /// <summary>
        /// Parses Base64 encoded image data
        /// </summary>
        private static byte[]? ParseBase64ImageData(XElement imageElement)
        {
            XElement? base64Element = imageElement.Element(nc + "BinaryBase64Object");
            if (base64Element == null)
            return null;

            try
            {
                string base64String = base64Element.Value.Trim();
                return Convert.FromBase64String(base64String);
            }
            catch (FormatException)
            {
                return null;
            }
        }

        /// <summary>
        /// Parses generic fields from an element (for extensibility)
        /// </summary>
        private static void ParseGenericFields(XElement element, NistRecord record, int recordType)
        {
            // Load configuration to get XML element name mappings
            Models.Type2FieldConfigurationRoot? config = null;
            Dictionary<string, Models.Type2FieldDefinition> xmlElementToFieldDef = new Dictionary<string, Models.Type2FieldDefinition>();

            if (recordType == 2)
            {
                try
                {
                    config = Configuration.Type2FieldConfigurationLoader.LoadConfiguration();

                    // Build reverse lookup: XML element name -> field definition
                    foreach (var fieldDef in config.Type2Fields)
                    {
                        if (!string.IsNullOrWhiteSpace(fieldDef.XmlElementName))
                        {
                            xmlElementToFieldDef[fieldDef.XmlElementName] = fieldDef;
                        }
                    }
                }
                catch
                {
                    // If config loading fails, we still try to parse UserDefinedDescriptiveText elements
                }
            }

            // Parse UserDefinedDescriptiveText elements (simple key-value format)
            var userDefinedTexts = element.Elements(itl + "UserDefinedDescriptiveText");
            foreach (var userDefinedText in userDefinedTexts)
            {
                var fieldIdElement = userDefinedText.Element(itl + "UserDefinedFieldID");
                var descriptionTextElement = userDefinedText.Element(nc + "DescriptionText");

                if (fieldIdElement != null && descriptionTextElement != null)
                {
                    string fieldNumber = fieldIdElement.Value;
                    string value = descriptionTextElement.Value;

                    // Check if this field has metadata
                    var metadata = Editing.FieldMetadataProvider.GetFieldMetadata(fieldNumber);

                    if (metadata != null)
                    {
                        AddField(record, fieldNumber, value);
                    }
                    else
                    {
                        // Unknown field - add to UnknownFields
                        record.SetUnknownField(fieldNumber, value);
                    }
                }
            }

            // Parse domain-specific XML elements (if configured)
            if (xmlElementToFieldDef.Count > 0)
            {
                foreach (var childElement in element.Elements())
                {
                    // Skip already-processed elements
                    if (childElement.Name == biom + "RecordCategoryCode" ||
                    childElement.Name == biom + "ImageReferenceIdentification" ||
                    childElement.Name == itl + "UserDefinedDescriptiveText")
                    {
                        continue;
                    }

                    // Check if this element matches a configured field
                    string elementName = $"{childElement.Name.NamespaceName.Split('/').Last()}:{childElement.Name.LocalName}";

                    // Try to find by full namespace:localname
                    Models.Type2FieldDefinition? fieldDef = null;
                    foreach (var kvp in xmlElementToFieldDef)
                    {
                        if (childElement.Name.LocalName == kvp.Key.Split(':').Last())
                        {
                            fieldDef = kvp.Value;
                            break;
                        }
                    }

                    if (fieldDef != null)
                    {
                        string value = childElement.Value;
                        var metadata = Editing.FieldMetadataProvider.GetFieldMetadata(fieldDef.FieldNumber);

                        if (metadata != null)
                        {
                            AddField(record, fieldDef.FieldNumber, value);
                        }
                        else
                        {
                            record.SetUnknownField(fieldDef.FieldNumber, value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets organization ID from organization element
        /// </summary>
        private static string? GetOrganizationId(XElement orgElement)
        {
            XElement? orgId = orgElement.Element(nc + "OrganizationIdentification");
            if (orgId != null)
            {
                XElement? id = orgId.Element(nc + "IdentificationID");
                return id?.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets Image Reference ID (IDC) from element
        /// </summary>
        private static string? GetImageReferenceId(XElement element)
        {
            XElement? imgRef = element.Element(biom + "ImageReferenceIdentification");
            if (imgRef != null)
            {
                XElement? id = imgRef.Element(nc + "IdentificationID");
                return id?.Value;
            }
            return null;
        }

        /// <summary>
        /// Helper method to add a simple field to a record
        /// </summary>
        private static void AddField(NistRecord record, string fieldNumber, string value)
        {
            var field = new NistField(fieldNumber);
            var subfield = new NistSubfield();
            subfield.Items.Add(value);
            field.Subfields.Add(subfield);
            record.AddField(field);
        }
    }
}
