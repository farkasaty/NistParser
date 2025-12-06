using System.Collections.Generic;
using System.Linq;
using NistParser.Constants;

namespace NistParser.Core
{

    /// <summary>
    /// Represents a single field in an ANSI/NIST-ITL record.
    /// Fields are identified by a field number (e.g., "1.003") and contain one or more subfields.
    /// </summary>
    public class NistField
    {
        /// <summary>
        /// Gets or sets the complete field identifier (e.g., "1.003", "14.999")
        /// </summary>
        public string FieldNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the record type portion of the field number (e.g., "1" from "1.003")
        /// </summary>
        public int RecordTypeNumber { get; set; }

        /// <summary>
        /// Gets or sets the field portion of the field number (e.g., "003" from "1.003")
        /// </summary>
        public int Field { get; set; }

        /// <summary>
        /// Gets or sets the subfields contained in this field.
        /// Most fields have a single subfield, but some fields have multiple repeating subfields.
        /// </summary>
        public List<NistSubfield> Subfields { get; set; } = new List<NistSubfield>();

        /// <summary>
        /// Gets or sets the raw binary data for this field (used for binary fields like 999)
        /// </summary>
        public byte[]? BinaryData { get; set; }

        /// <summary>
        /// Indicates whether this field contains binary data
        /// </summary>
        public bool IsBinary => BinaryData != null;

        /// <summary>
        /// Gets the first subfield, or null if no subfields exist
        /// </summary>
        public NistSubfield? FirstSubfield => Subfields.FirstOrDefault();

        /// <summary>
        /// Gets the first information item from the first subfield, or null if none exist
        /// </summary>
        public string? FirstValue => FirstSubfield?.FirstItem;

        /// <summary>
        /// Gets a human-readable description of this field (e.g., "VER - Version Number")
        /// </summary>
        public string Description => FieldDescriptions.GetDescription(FieldNumber);

        /// <summary>
        /// Gets the short mnemonic for this field (e.g., "VER" for field 1.002)
        /// </summary>
        public string Mnemonic => FieldDescriptions.GetMnemonic(FieldNumber);

        /// <summary>
        /// Gets the long description for this field (e.g., "Version Number" for field 1.002)
        /// </summary>
        public string LongDescription => FieldDescriptions.GetLongDescription(FieldNumber);

        /// <summary>
        /// Gets a human-readable interpretation of the first value
        /// </summary>
        public string ValueInterpretation => FirstValue != null
        ? FieldDescriptions.GetValueInterpretation(FieldNumber, FirstValue)
        : string.Empty;

        /// <summary>
        /// Initializes a new instance of the NistField class
        /// </summary>
        public NistField()
        {
        }

        /// <summary>
        /// Initializes a new instance of the NistField class with a field number
        /// </summary>
        /// <param name="fieldNumber">The complete field identifier (e.g., "1.003", "1.01", or "1.1")</param>
        public NistField(string fieldNumber)
        {
            // Normalize field number to standard format (minimum 3 digits)
            // This ensures consistent dictionary keys and output format
            FieldNumber = Utilities.FieldNumberFormatter.Normalize(fieldNumber);
            ParseFieldNumber(FieldNumber);
        }

        /// <summary>
        /// Parses the field number into record type and field components
        /// </summary>
        /// <param name="fieldNumber">Field number in format "X.YYY"</param>
        private void ParseFieldNumber(string fieldNumber)
        {
            var parts = fieldNumber.Split('.');
            if (parts.Length == 2 &&
            int.TryParse(parts[0], out int recordType) &&
            int.TryParse(parts[1], out int field))
            {
                RecordTypeNumber = recordType;
                Field = field;
            }
        }

        /// <summary>
        /// Gets all information items from all subfields as a flat list
        /// </summary>
        /// <returns>All information items</returns>
        public IEnumerable<string> GetAllItems()
        {
            return Subfields.SelectMany(sf => sf.Items);
        }

        /// <summary>
        /// Sets the field value (creates a single subfield with single item)
        /// </summary>
        /// <param name="value">The value to set</param>
        public void SetValue(string value)
        {
            Subfields.Clear();
            Subfields.Add(new NistSubfield(value));
        }

        /// <summary>
        /// Sets the value of a specific subfield
        /// </summary>
        /// <param name="subfieldIndex">Index of the subfield (0-based)</param>
        /// <param name="value">The value to set</param>
        public void SetSubfieldValue(int subfieldIndex, string value)
        {
            // Ensure we have enough subfields
            while (Subfields.Count <= subfieldIndex)
            {
                Subfields.Add(new NistSubfield());
            }

            Subfields[subfieldIndex].Items.Clear();
            Subfields[subfieldIndex].Items.Add(value);
        }

        /// <summary>
        /// Sets multiple values as separate subfields
        /// </summary>
        /// <param name="values">The values to set</param>
        public void SetValues(params string[] values)
        {
            Subfields.Clear();
            foreach (var value in values)
            {
                Subfields.Add(new NistSubfield(value));
            }
        }

        /// <summary>
        /// Returns a string representation of this field
        /// </summary>
        public override string ToString()
        {
            if (IsBinary)
            {
                return $"{FieldNumber}: [Binary data, {BinaryData?.Length ?? 0} bytes]";
            }

            return $"{FieldNumber}: {string.Join(" | ", Subfields.Select(sf => sf.ToString()))}";
        }
    }

    /// <summary>
    /// Represents a subfield within a field.
    /// Subfields contain one or more information items and can repeat within a field.
    /// </summary>
    public class NistSubfield
    {
        /// <summary>
        /// Gets or sets the information items in this subfield
        /// </summary>
        public List<string> Items { get; set; } = new List<string>();

        /// <summary>
        /// Gets the first information item, or null if no items exist
        /// </summary>
        public string? FirstItem => Items.FirstOrDefault();

        /// <summary>
        /// Gets the number of information items in this subfield
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Initializes a new instance of the NistSubfield class
        /// </summary>
        public NistSubfield()
        {
        }

        /// <summary>
        /// Initializes a new instance of the NistSubfield class with items
        /// </summary>
        /// <param name="items">The information items</param>
        public NistSubfield(params string[] items)
        {
            Items = new List<string>(items);
        }

        /// <summary>
        /// Returns a string representation of this subfield
        /// </summary>
        public override string ToString()
        {
            return string.Join(", ", Items);
        }
    }
}
