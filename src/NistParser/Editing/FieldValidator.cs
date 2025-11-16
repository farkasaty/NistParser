using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using NistParser.Core;

namespace NistParser.Editing
{

    /// <summary>
    /// Validates NIST field values according to field metadata and ANSI/NIST-ITL specifications
    /// </summary>
    public static class FieldValidator
    {
        /// <summary>
        /// Validates a field value according to its metadata
        /// </summary>
        public static ValidationResult ValidateField(string fieldNumber, string? value)
        {
            var metadata = FieldMetadataProvider.GetFieldMetadata(fieldNumber);
            if (metadata == null)
            {
                return ValidationResult.Success(); // Unknown fields are allowed (user-defined)
            }

            return ValidateField(metadata, value);
        }

        /// <summary>
        /// Validates a field value according to provided metadata
        /// </summary>
        public static ValidationResult ValidateField(FieldMetadata metadata, string? value)
        {
            // Check required fields
            if (metadata.IsRequired && string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Fail($"{metadata.DisplayName} is required", metadata.FieldNumber);
            }

            // If value is empty and field is not required, it's valid
            if (string.IsNullOrWhiteSpace(value))
            {
                return ValidationResult.Success();
            }

            // Check minimum length
            if (metadata.MinLength.HasValue && value.Length < metadata.MinLength.Value)
            {
                return ValidationResult.Fail(
                $"{metadata.DisplayName} must be at least {metadata.MinLength.Value} characters",
                metadata.FieldNumber);
            }

            // Check maximum length
            if (metadata.MaxLength.HasValue && value.Length > metadata.MaxLength.Value)
            {
                return ValidationResult.Fail(
                $"{metadata.DisplayName} must not exceed {metadata.MaxLength.Value} characters",
                metadata.FieldNumber);
            }

            // Check validation pattern
            if (!string.IsNullOrEmpty(metadata.ValidationPattern))
            {
                if (!Regex.IsMatch(value, metadata.ValidationPattern))
                {
                    var message = metadata.ValidationMessage ?? $"{metadata.DisplayName} has invalid format";
                    return ValidationResult.Fail(message, metadata.FieldNumber);
                }
            }

            // Check enumeration values
            if (metadata.DataType == FieldDataType.Enumeration && metadata.EnumValues != null)
            {
                if (!metadata.EnumValues.ContainsKey(value))
                {
                    return ValidationResult.Fail(
                    $"{metadata.DisplayName} must be one of: {string.Join(", ", metadata.EnumValues.Keys)}",
                    metadata.FieldNumber);
                }
            }

            // Validate date format
            if (metadata.DataType == FieldDataType.Date)
            {
                if (!ValidateDate(value))
                {
                    return ValidationResult.Fail(
                    $"{metadata.DisplayName} must be a valid date in YYYYMMDD format",
                    metadata.FieldNumber);
                }
            }

            // Validate numeric format
            if (metadata.DataType == FieldDataType.Numeric)
            {
                if (!double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    return ValidationResult.Fail(
                    $"{metadata.DisplayName} must be a valid number",
                    metadata.FieldNumber);
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates all fields in a record
        /// </summary>
        public static ValidationResults ValidateRecord(NistRecord record)
        {
            var results = new ValidationResults();

            // Get all editable fields for this record type
            var editableFields = FieldMetadataProvider.GetEditableFields(record.RecordType);

            // Check all required fields
            foreach (var metadata in editableFields.Where(m => m.IsRequired))
            {
                var field = record.GetField(metadata.FieldNumber);
                var value = field?.FirstValue;

                var result = ValidateField(metadata, value);
                results.Add(result);
            }

            // Validate existing field values
            foreach (var field in record.Fields.Values)
            {
                var metadata = FieldMetadataProvider.GetFieldMetadata(field.FieldNumber);
                if (metadata != null && metadata.IsEditable)
                {
                    var result = ValidateField(metadata, field.FirstValue);
                    results.Add(result);
                }
            }

            return results;
        }

        /// <summary>
        /// Validates a date string in YYYYMMDD format
        /// </summary>
        private static bool ValidateDate(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString) || dateString.Length != 8)
            return false;

            if (!int.TryParse(dateString.Substring(0, 4), out int year) ||
            !int.TryParse(dateString.Substring(4, 2), out int month) ||
            !int.TryParse(dateString.Substring(6, 2), out int day))
            {
                return false;
            }

            // Check if it's a valid date
            try
            {
                var date = new DateTime(year, month, day);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates a new field can be added to a record
        /// </summary>
        public static ValidationResult ValidateNewField(NistRecord record, string fieldNumber, string? value)
        {
            // Check if field already exists
            if (record.HasField(fieldNumber))
            {
                return ValidationResult.Fail($"Field {fieldNumber} already exists in this record", fieldNumber);
            }

            // Validate the field value
            return ValidateField(fieldNumber, value);
        }

        /// <summary>
        /// Validates that a field can be deleted from a record
        /// </summary>
        public static ValidationResult ValidateDeleteField(string fieldNumber)
        {
            if (!FieldMetadataProvider.CanDeleteField(fieldNumber))
            {
                var metadata = FieldMetadataProvider.GetFieldMetadata(fieldNumber);
                if (metadata?.IsRequired == true)
                {
                    return ValidationResult.Fail($"Cannot delete required field: {metadata.DisplayName}", fieldNumber);
                }
                return ValidationResult.Fail($"Cannot delete field {fieldNumber}", fieldNumber);
            }

            return ValidationResult.Success();
        }
    }
}
