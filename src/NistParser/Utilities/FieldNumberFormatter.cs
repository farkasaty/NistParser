using System;

namespace NistParser.Utilities
{
    /// <summary>
    /// Utility class for field number normalization and validation.
    /// Provides methods to normalize field numbers to a consistent format (minimum 3 digits)
    /// and compare field numbers for equivalence.
    /// </summary>
    public static class FieldNumberFormatter
    {
        /// <summary>
        /// Normalizes a field number to minimum 3-digit format.
        /// Examples: "1.1" -> "1.001", "1.01" -> "1.001", "1.001" -> "1.001", "14.999" -> "14.999"
        /// </summary>
        /// <param name="fieldNumber">The field number to normalize (e.g., "1.1", "1.01", "1.001")</param>
        /// <returns>Normalized field number with minimum 3 digits after the dot</returns>
        public static string Normalize(string fieldNumber)
        {
            var parts = fieldNumber.Split('.');
            if (parts.Length != 2)
            {
                // Invalid format, return as-is (defensive programming)
                return fieldNumber;
            }

            if (!int.TryParse(parts[0], out int recordType))
            {
                // Record type is not a number, return as-is
                return fieldNumber;
            }

            if (!int.TryParse(parts[1], out int field))
            {
                // Field number is not a number, return as-is
                return fieldNumber;
            }

            // Format with minimum 3 digits (D3 = decimal with zero-padding to 3 digits)
            return $"{recordType}.{field:D3}";
        }

        /// <summary>
        /// Checks if two field numbers are equivalent after normalization.
        /// Examples: "1.1" == "1.001" returns true, "1.1" == "1.10" returns false
        /// </summary>
        /// <param name="fieldNumber1">First field number</param>
        /// <param name="fieldNumber2">Second field number</param>
        /// <returns>True if the field numbers are equivalent after normalization</returns>
        public static bool AreEquivalent(string fieldNumber1, string fieldNumber2)
        {
            return Normalize(fieldNumber1) == Normalize(fieldNumber2);
        }

        /// <summary>
        /// Validates that a field number has the correct format (X.YYY where both parts are integers).
        /// </summary>
        /// <param name="fieldNumber">The field number to validate</param>
        /// <returns>True if the field number has valid format</returns>
        public static bool IsValid(string fieldNumber)
        {
            var parts = fieldNumber.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
        }

        /// <summary>
        /// Creates a normalized field number from record type and field number components.
        /// </summary>
        /// <param name="recordType">The record type (1-99)</param>
        /// <param name="field">The field number (1-999)</param>
        /// <returns>Normalized field number string (e.g., "1.001")</returns>
        public static string Create(int recordType, int field)
        {
            return $"{recordType}.{field:D3}";
        }
    }
}
