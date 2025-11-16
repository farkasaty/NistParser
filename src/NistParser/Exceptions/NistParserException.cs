using System;

namespace NistParser.Exceptions
{

    /// <summary>
    /// Base exception for all NIST parser errors
    /// </summary>
    public class NistParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the NistParserException class
        /// </summary>
        public NistParserException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the NistParserException class with a message
        /// </summary>
        /// <param name="message">The error message</param>
        public NistParserException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the NistParserException class with a message and inner exception
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="innerException">The inner exception</param>
        public NistParserException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a required Type-1 record is missing or invalid
    /// </summary>
    public class MissingType1Exception : NistParserException
    {
        /// <summary>
        /// Initializes a new instance of the MissingType1Exception class
        /// </summary>
        public MissingType1Exception() : base("Type-1 record is mandatory but was not found or is invalid")
        {
        }

        /// <summary>
        /// Initializes a new instance of the MissingType1Exception class with a message
        /// </summary>
        /// <param name="message">The error message</param>
        public MissingType1Exception(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when field format is invalid
    /// </summary>
    public class InvalidFieldFormatException : NistParserException
    {
        /// <summary>
        /// Gets the field number that caused the error
        /// </summary>
        public string? FieldNumber { get; }

        /// <summary>
        /// Initializes a new instance of the InvalidFieldFormatException class
        /// </summary>
        /// <param name="fieldNumber">The field number</param>
        /// <param name="message">The error message</param>
        public InvalidFieldFormatException(string fieldNumber, string message)
        : base($"Invalid format for field {fieldNumber}: {message}")
        {
            FieldNumber = fieldNumber;
        }
    }

    /// <summary>
    /// Exception thrown when record length is invalid or mismatched
    /// </summary>
    public class InvalidRecordLengthException : NistParserException
    {
        /// <summary>
        /// Gets the expected length
        /// </summary>
        public int? ExpectedLength { get; }

        /// <summary>
        /// Gets the actual length
        /// </summary>
        public int? ActualLength { get; }

        /// <summary>
        /// Initializes a new instance of the InvalidRecordLengthException class
        /// </summary>
        /// <param name="message">The error message</param>
        public InvalidRecordLengthException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InvalidRecordLengthException class
        /// </summary>
        /// <param name="expected">Expected length</param>
        /// <param name="actual">Actual length</param>
        public InvalidRecordLengthException(int expected, int actual)
        : base($"Record length mismatch: expected {expected} bytes, but found {actual} bytes")
        {
            ExpectedLength = expected;
            ActualLength = actual;
        }
    }

    /// <summary>
    /// Exception thrown when file is truncated or corrupted
    /// </summary>
    public class TruncatedFileException : NistParserException
    {
        /// <summary>
        /// Initializes a new instance of the TruncatedFileException class
        /// </summary>
        /// <param name="message">The error message</param>
        public TruncatedFileException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a validation check fails
    /// </summary>
    public class ValidationException : NistParserException
    {
        /// <summary>
        /// Initializes a new instance of the ValidationException class
        /// </summary>
        /// <param name="message">The error message</param>
        public ValidationException(string message) : base(message)
        {
        }
    }
}
