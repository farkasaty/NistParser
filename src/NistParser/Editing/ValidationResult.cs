using System;
using System.Collections.Generic;
using System.Linq;

namespace NistParser.Editing
{

    /// <summary>
    /// Represents the result of a validation operation
    /// </summary>
    public class ValidationResult
    {
        /// <summary>
        /// Gets whether the validation succeeded
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the field number that failed validation (if applicable)
        /// </summary>
        public string? FieldNumber { get; }

        /// <summary>
        /// Initializes a new instance representing a successful validation
        /// </summary>
        private ValidationResult()
        {
            IsValid = true;
        }

        /// <summary>
        /// Initializes a new instance representing a failed validation
        /// </summary>
        private ValidationResult(string errorMessage, string? fieldNumber = null)
        {
            IsValid = false;
            ErrorMessage = errorMessage;
            FieldNumber = fieldNumber;
        }

        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationResult Success() => new ValidationResult();

        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static ValidationResult Fail(string errorMessage, string? fieldNumber = null)
        => new ValidationResult(errorMessage, fieldNumber);

        /// <summary>
        /// Returns a string representation of this validation result
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
            return "Validation succeeded";

            return FieldNumber != null
            ? $"Field {FieldNumber}: {ErrorMessage}"
            : ErrorMessage ?? "Validation failed";
        }
    }

    /// <summary>
    /// Represents the results of multiple validation operations
    /// </summary>
    public class ValidationResults
    {
        /// <summary>
        /// Gets the list of validation results
        /// </summary>
        public List<ValidationResult> Results { get; } = new List<ValidationResult>();

        /// <summary>
        /// Gets whether all validations succeeded
        /// </summary>
        public bool IsValid => Results.All(r => r.IsValid);

        /// <summary>
        /// Gets the error messages from failed validations
        /// </summary>
        public IEnumerable<string> ErrorMessages => Results
        .Where(r => !r.IsValid && r.ErrorMessage != null)
        .Select(r => r.ErrorMessage!);

        /// <summary>
        /// Adds a validation result
        /// </summary>
        public void Add(ValidationResult result)
        {
            Results.Add(result);
        }

        /// <summary>
        /// Adds multiple validation results
        /// </summary>
        public void AddRange(IEnumerable<ValidationResult> results)
        {
            Results.AddRange(results);
        }

        /// <summary>
        /// Returns a string representation of all validation results
        /// </summary>
        public override string ToString()
        {
            if (IsValid)
            return "All validations succeeded";

            return string.Join(Environment.NewLine, ErrorMessages);
        }
    }
}
