using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NistParser.Models
{

    /// <summary>
    /// Configuration for Type-2 record fields loaded from external JSON file
    /// </summary>
    public class Type2FieldConfigurationRoot
    {
        [JsonPropertyName("type2Fields")]
        public List<Type2FieldDefinition> Type2Fields { get; set; } = new List<Type2FieldDefinition>();

        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = "1.0";

        [JsonPropertyName("defaultNamespaces")]
        public Dictionary<string, string>? DefaultNamespaces { get; set; }
    }

    /// <summary>
    /// Definition of a single Type-2 field
    /// </summary>
    public class Type2FieldDefinition
    {
        /// <summary>
        /// Field number (e.g., "2.007")
        /// </summary>
        [JsonPropertyName("fieldNumber")]
        public string FieldNumber { get; set; } = string.Empty;

        /// <summary>
        /// Field name (e.g., "Date of Birth")
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Data type: "text", "numeric", "date", "enum"
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = "text";

        /// <summary>
        /// Whether this field is required
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;

        // Text-specific properties
        /// <summary>
        /// Maximum length for text fields
        /// </summary>
        [JsonPropertyName("maxLength")]
        public int? MaxLength { get; set; }

        /// <summary>
        /// Regex validation pattern for text fields
        /// </summary>
        [JsonPropertyName("validationPattern")]
        public string? ValidationPattern { get; set; }

        // Numeric-specific properties
        /// <summary>
        /// Minimum value for numeric fields
        /// </summary>
        [JsonPropertyName("minValue")]
        public double? MinValue { get; set; }

        /// <summary>
        /// Maximum value for numeric fields
        /// </summary>
        [JsonPropertyName("maxValue")]
        public double? MaxValue { get; set; }

        // Date-specific properties
        /// <summary>
        /// Date format for traditional format (e.g., "YYYYMMDD")
        /// </summary>
        [JsonPropertyName("format")]
        public string? Format { get; set; }

        /// <summary>
        /// Date format for XML (e.g., "YYYY-MM-DD")
        /// </summary>
        [JsonPropertyName("xmlDateFormat")]
        public string? XmlDateFormat { get; set; }

        // Enum-specific properties
        /// <summary>
        /// Allowed values for enum fields
        /// </summary>
        [JsonPropertyName("enumValues")]
        public List<EnumValue>? EnumValues { get; set; }

        // XML-specific properties
        /// <summary>
        /// XML element name with namespace (e.g., "nc:PersonBirthDate")
        /// </summary>
        [JsonPropertyName("xmlElementName")]
        public string? XmlElementName { get; set; }

        /// <summary>
        /// Parent XML element path for nested elements (e.g., "nc:PersonName")
        /// </summary>
        [JsonPropertyName("xmlParentPath")]
        public string? XmlParentPath { get; set; }
    }

    /// <summary>
    /// Enum value definition
    /// </summary>
    public class EnumValue
    {
        /// <summary>
        /// Enum code (e.g., "M")
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Enum description (e.g., "Male")
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
    }
}
