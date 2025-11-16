using System.Linq;
using NistParser.Constants;
using NistParser.Core;

namespace NistParser.Records
{

    /// <summary>
    /// Type-2 User-Defined Descriptive Text Record
    /// Contains biographic and descriptive information (user-defined fields).
    /// Common fields include name, date of birth, demographics, etc.
    /// </summary>
    public class Type2Record : NistRecord
    {
        /// <summary>
        /// Initializes a new instance of the Type2Record class
        /// </summary>
        public Type2Record()
        {
            RecordType = RecordType.Type2_UserDefinedText;
        }

        // Note: Type-2 fields are user-defined and vary by implementation.
        // Below are common fields, but agencies may use different field numbers.

        /// <summary>
        /// Common Field 2.004 - Surname / Last Name
        /// </summary>
        public string? Surname => GetFieldValue("2.004");

        /// <summary>
        /// Common Field 2.005 - Given Name / First Name
        /// </summary>
        public string? GivenName => GetFieldValue("2.005");

        /// <summary>
        /// Common Field 2.006 - Middle Name
        /// </summary>
        public string? MiddleName => GetFieldValue("2.006");

        /// <summary>
        /// Common Field 2.007 - Date of Birth
        /// Format may vary by implementation (often YYYY-MM-DD or YYYYMMDD)
        /// </summary>
        public string? DateOfBirth => GetFieldValue("2.007");

        /// <summary>
        /// Common Field 2.018 - Place of Birth
        /// </summary>
        public string? PlaceOfBirth => GetFieldValue("2.018");

        /// <summary>
        /// Common Field 2.024 - Sex / Gender
        /// Common values: "M" (Male), "F" (Female), "U" (Unknown)
        /// </summary>
        public string? Sex => GetFieldValue("2.024");

        /// <summary>
        /// Common Field 2.025 - Race / Ethnicity
        /// </summary>
        public string? Race => GetFieldValue("2.025");

        /// <summary>
        /// Gets the full name (combination of given, middle, and surname)
        /// </summary>
        public string? FullName
        {
            get
            {
                var parts = new string[] { GivenName, MiddleName, Surname }
                .Where(s => !string.IsNullOrWhiteSpace(s));
                return parts.Any() ? string.Join(" ", parts) : null;
            }
        }

        /// <summary>
        /// Returns a string representation of this Type-2 record
        /// </summary>
        public override string ToString()
        {
            var name = FullName ?? "Unknown";
            var dob = DateOfBirth ?? "Unknown DOB";
            return $"Type-2 Record (IDC: {IDC}) - {name}, DOB: {dob}";
        }
    }
}
