using System;
using System.IO;
using System.Text;
using NistParser.Core;
using NistParser.Writing;

namespace NistParser.Editing
{

    /// <summary>
    /// Provides high-level API for editing NIST transactions
    /// Handles validation, field updates, and transaction serialization
    /// </summary>
    public class NistTransactionEditor
    {
        private readonly NistTransaction _transaction;
        private readonly bool _originalFormatWasXml;

        /// <summary>
        /// Gets the transaction being edited
        /// </summary>
        public NistTransaction Transaction => _transaction;

        /// <summary>
        /// Initializes a new instance of the NistTransactionEditor
        /// </summary>
        /// <param name="transaction">The transaction to edit</param>
        /// <param name="wasXmlFormat">Whether the original file was in XML format</param>
        public NistTransactionEditor(NistTransaction transaction, bool wasXmlFormat = false)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
            _originalFormatWasXml = wasXmlFormat;
        }

        /// <summary>
        /// Sets a field value in a specific record
        /// </summary>
        /// <param name="record">The record to update</param>
        /// <param name="fieldNumber">The field number (e.g., "1.002")</param>
        /// <param name="value">The new value</param>
        /// <returns>Validation result</returns>
        public ValidationResult SetFieldValue(NistRecord record, string fieldNumber, string value)
        {
            // Validate the field value
            var validationResult = FieldValidator.ValidateField(fieldNumber, value);
            if (!validationResult.IsValid)
            {
                return validationResult;
            }

            // Update the field
            record.UpdateField(fieldNumber, value);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Adds a new field to a record
        /// </summary>
        /// <param name="record">The record to update</param>
        /// <param name="fieldNumber">The field number (e.g., "2.004")</param>
        /// <param name="value">The field value</param>
        /// <returns>Validation result</returns>
        public ValidationResult AddField(NistRecord record, string fieldNumber, string value)
        {
            // Validate that the field can be added
            var validationResult = FieldValidator.ValidateNewField(record, fieldNumber, value);
            if (!validationResult.IsValid)
            {
                return validationResult;
            }

            // Add the field
            var field = new NistField(fieldNumber);
            field.SetValue(value);
            record.AddField(field);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Removes a field from a record
        /// </summary>
        /// <param name="record">The record to update</param>
        /// <param name="fieldNumber">The field number to remove</param>
        /// <returns>Validation result</returns>
        public ValidationResult RemoveField(NistRecord record, string fieldNumber)
        {
            // Validate that the field can be deleted
            var validationResult = FieldValidator.ValidateDeleteField(fieldNumber);
            if (!validationResult.IsValid)
            {
                return validationResult;
            }

            // Remove the field
            if (!record.RemoveField(fieldNumber))
            {
                return ValidationResult.Fail($"Field {fieldNumber} does not exist in record", fieldNumber);
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Adds an unknown field (field without metadata definition) to a record
        /// </summary>
        /// <param name="record">The record to update</param>
        /// <param name="fieldNumber">The field number (e.g., "2.999")</param>
        /// <param name="value">The field value as string</param>
        /// <returns>Validation result</returns>
        public ValidationResult AddUnknownField(NistRecord record, string fieldNumber, string value)
        {
            // Basic validation - check field number format
            if (string.IsNullOrWhiteSpace(fieldNumber))
            {
                return ValidationResult.Fail("Field number cannot be empty", fieldNumber);
            }

            // Check if field already exists (either in Fields or UnknownFields)
            if (record.HasField(fieldNumber) || record.HasUnknownField(fieldNumber))
            {
                return ValidationResult.Fail($"Field {fieldNumber} already exists in record", fieldNumber);
            }

            // Add to UnknownFields
            record.SetUnknownField(fieldNumber, value);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Updates an unknown field value
        /// </summary>
        /// <param name="record">The record to update</param>
        /// <param name="fieldNumber">The field number</param>
        /// <param name="value">The new value</param>
        /// <returns>Validation result</returns>
        public ValidationResult UpdateUnknownField(NistRecord record, string fieldNumber, string value)
        {
            // Check if field exists
            if (!record.HasUnknownField(fieldNumber))
            {
                return ValidationResult.Fail($"Unknown field {fieldNumber} does not exist in record", fieldNumber);
            }

            // Update the value
            record.SetUnknownField(fieldNumber, value);

            return ValidationResult.Success();
        }

        /// <summary>
        /// Removes an unknown field from a record
        /// </summary>
        /// <param name="record">The record to update</param>
        /// <param name="fieldNumber">The field number to remove</param>
        /// <returns>Validation result</returns>
        public ValidationResult RemoveUnknownField(NistRecord record, string fieldNumber)
        {
            // Remove the field
            if (!record.RemoveUnknownField(fieldNumber))
            {
                return ValidationResult.Fail($"Unknown field {fieldNumber} does not exist in record", fieldNumber);
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates all changes in the transaction
        /// </summary>
        /// <returns>Validation results</returns>
        public ValidationResults ValidateAllChanges()
        {
            var results = new ValidationResults();

            // Validate Type-1 record
            if (_transaction.Header != null)
            {
                var headerResults = FieldValidator.ValidateRecord(_transaction.Header);
                results.AddRange(headerResults.Results);
            }

            // Validate all other records
            foreach (var record in _transaction.Records)
            {
                var recordResults = FieldValidator.ValidateRecord(record);
                results.AddRange(recordResults.Results);
            }

            return results;
        }

        /// <summary>
        /// Saves the edited transaction to a byte array
        /// Uses the original format (XML or binary) unless specified otherwise
        /// </summary>
        /// <param name="useXmlFormat">Optional override for output format</param>
        /// <returns>The serialized transaction as a byte array</returns>
        public byte[] SaveToBytes(bool? useXmlFormat = null)
        {
            // Validate before saving
            var validationResults = ValidateAllChanges();
            if (!validationResults.IsValid)
            {
                throw new InvalidOperationException(
                $"Cannot save transaction with validation errors:{Environment.NewLine}{validationResults}");
            }

            // Determine format
            bool outputAsXml = useXmlFormat ?? _originalFormatWasXml;

            // Serialize
            return NistTransactionWriter.Write(_transaction, outputAsXml);
        }

        /// <summary>
        /// Saves the edited transaction to a file
        /// </summary>
        /// <param name="filePath">The file path to save to</param>
        /// <param name="useXmlFormat">Optional override for output format</param>
        public void SaveToFile(string filePath, bool? useXmlFormat = null)
        {
            var data = SaveToBytes(useXmlFormat);
            File.WriteAllBytes(filePath, data);
        }

        /// <summary>
        /// Creates a NistTransactionEditor from a file
        /// </summary>
        /// <param name="filePath">Path to the NIST file</param>
        /// <returns>A new NistTransactionEditor instance</returns>
        public static NistTransactionEditor FromFile(string filePath)
        {
            var fileData = File.ReadAllBytes(filePath);
            return FromBytes(fileData);
        }

        /// <summary>
        /// Creates a NistTransactionEditor from a byte array
        /// </summary>
        /// <param name="fileData">The NIST file data</param>
        /// <returns>A new NistTransactionEditor instance</returns>
        public static NistTransactionEditor FromBytes(byte[] fileData)
        {
            // Detect format
            bool isXml = IsXmlFormat(fileData);

            // Parse transaction
            var transaction = NistTransactionParser.Parse(fileData);

            // Create editor
            return new NistTransactionEditor(transaction, isXml);
        }

        /// <summary>
        /// Determines if the file data is in XML format
        /// </summary>
        private static bool IsXmlFormat(byte[] fileData)
        {
            if (fileData.Length < 5)
            return false;

            int sampleLength = Math.Min(200, fileData.Length);
            string header = System.Text.Encoding.UTF8.GetString(fileData, 0, sampleLength).TrimStart();

            return header.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
            header.Contains("<itl:NISTBiometricInformationExchangePackage");
        }
    }
}
