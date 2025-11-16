using System.Text.Json;
using NistParser.Models;

namespace NistParser.Configuration;

/// <summary>
/// Loads Type-2 field configuration from JSON file
/// </summary>
public class Type2FieldConfigurationLoader
{
    private const string DefaultConfigFileName = "type2-fields.json";
    private const string ConfigSubdirectory = "config";

    /// <summary>
    /// Loads Type-2 field configuration from the default location
    /// </summary>
    /// <returns>Configuration root, or empty configuration if file not found</returns>
    public static Type2FieldConfigurationRoot LoadConfiguration()
    {
        // Try multiple possible locations
        var possiblePaths = new[]
        {
            // 1. config/type2-fields.json (preferred)
            Path.Combine(ConfigSubdirectory, DefaultConfigFileName),
            // 2. type2-fields.json (root directory)
            DefaultConfigFileName,
            // 3. Absolute path in current directory
            Path.Combine(Directory.GetCurrentDirectory(), ConfigSubdirectory, DefaultConfigFileName),
            Path.Combine(Directory.GetCurrentDirectory(), DefaultConfigFileName)
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    return LoadConfigurationFromFile(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to load Type-2 configuration from '{path}': {ex.Message}");
                    Console.WriteLine("Falling back to empty configuration (only required fields 2.001, 2.002)");
                }
            }
        }

        // No config file found - log warning and return empty config
        Console.WriteLine($"Info: No Type-2 field configuration file found. Searched locations:");
        foreach (var path in possiblePaths)
        {
            Console.WriteLine($"  - {Path.GetFullPath(path)}");
        }
        Console.WriteLine("Using minimal configuration (only required fields 2.001, 2.002)");

        return new Type2FieldConfigurationRoot
        {
            Type2Fields = new List<Type2FieldDefinition>(),
            SchemaVersion = "1.0"
        };
    }

    /// <summary>
    /// Loads Type-2 field configuration from a specific file path
    /// </summary>
    /// <param name="filePath">Path to the JSON configuration file</param>
    /// <returns>Configuration root</returns>
    /// <exception cref="FileNotFoundException">If file doesn't exist</exception>
    /// <exception cref="JsonException">If JSON is invalid</exception>
    public static Type2FieldConfigurationRoot LoadConfigurationFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var jsonContent = File.ReadAllText(filePath);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var config = JsonSerializer.Deserialize<Type2FieldConfigurationRoot>(jsonContent, options);

        if (config == null)
        {
            throw new JsonException($"Failed to deserialize configuration from: {filePath}");
        }

        // Validate configuration
        ValidateConfiguration(config);

        Console.WriteLine($"Successfully loaded Type-2 field configuration from: {filePath}");
        Console.WriteLine($"  Schema version: {config.SchemaVersion}");
        Console.WriteLine($"  Defined fields: {config.Type2Fields.Count}");

        return config;
    }

    /// <summary>
    /// Validates the loaded configuration
    /// </summary>
    private static void ValidateConfiguration(Type2FieldConfigurationRoot config)
    {
        var errors = new List<string>();

        foreach (var field in config.Type2Fields)
        {
            // Validate field number format
            if (string.IsNullOrWhiteSpace(field.FieldNumber))
            {
                errors.Add("Field has empty fieldNumber");
            }
            else if (!field.FieldNumber.StartsWith("2."))
            {
                errors.Add($"Field {field.FieldNumber}: fieldNumber must start with '2.'");
            }

            // Validate name
            if (string.IsNullOrWhiteSpace(field.Name))
            {
                errors.Add($"Field {field.FieldNumber}: name is required");
            }

            // Validate data type
            var validDataTypes = new[] { "text", "numeric", "date", "enum" };
            if (!validDataTypes.Contains(field.DataType?.ToLower()))
            {
                errors.Add($"Field {field.FieldNumber}: dataType must be one of: {string.Join(", ", validDataTypes)}");
            }

            // Validate enum fields have enumValues
            if (field.DataType?.ToLower() == "enum" && (field.EnumValues == null || field.EnumValues.Count == 0))
            {
                errors.Add($"Field {field.FieldNumber}: enum dataType requires enumValues");
            }

            // Validate enum values
            if (field.EnumValues != null)
            {
                foreach (var enumValue in field.EnumValues)
                {
                    if (string.IsNullOrWhiteSpace(enumValue.Code))
                    {
                        errors.Add($"Field {field.FieldNumber}: enum value has empty code");
                    }
                    if (string.IsNullOrWhiteSpace(enumValue.Description))
                    {
                        errors.Add($"Field {field.FieldNumber}: enum value '{enumValue.Code}' has empty description");
                    }
                }
            }
        }

        if (errors.Any())
        {
            throw new InvalidOperationException(
                $"Configuration validation failed:\n  - {string.Join("\n  - ", errors)}");
        }
    }
}
