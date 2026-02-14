using System.Collections.Generic;

namespace NistParser.Constants
{
    /// <summary>
    /// ANSI/NIST-ITL ujjpozíció kódok és emberi olvasható leírásaik.
    /// A kódok a Type-14 (14.005) és Type-4 rekordokban használatosak.
    /// </summary>
    public static class FingerPositionCode
    {
        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            { "0", "Unknown / Ismeretlen" },
            { "1", "Right Thumb / Jobb hüvelykujj" },
            { "2", "Right Index / Jobb mutatóujj" },
            { "3", "Right Middle / Jobb középső ujj" },
            { "4", "Right Ring / Jobb gyűrűsujj" },
            { "5", "Right Little / Jobb kisujj" },
            { "6", "Left Thumb / Bal hüvelykujj" },
            { "7", "Left Index / Bal mutatóujj" },
            { "8", "Left Middle / Bal középső ujj" },
            { "9", "Left Ring / Bal gyűrűsujj" },
            { "10", "Left Little / Bal kisujj" },
            { "11", "Plain Right Thumb / Sík jobb hüvelykujj" },
            { "12", "Plain Left Thumb / Sík bal hüvelykujj" },
            { "13", "Plain Right Four Fingers / Sík jobb négy ujj" },
            { "14", "Plain Left Four Fingers / Sík bal négy ujj" },
            { "15", "Plain Both Thumbs / Sík mindkét hüvelykujj" }
        };

        /// <summary>
        /// Visszaadja az ujjpozíció emberi olvasható leírását.
        /// </summary>
        /// <param name="code">Az ujjpozíció kód (0-15)</param>
        /// <returns>Emberi olvasható leírás, vagy az eredeti kód ha ismeretlen</returns>
        public static string GetDescription(string? code)
        {
            if (code == null) return "Ismeretlen";
            // Trim whitespace and handle multi-value codes (e.g., "1" from "01")
            var trimmed = code.Trim().TrimStart('0');
            if (string.IsNullOrEmpty(trimmed)) trimmed = "0";

            return Descriptions.TryGetValue(trimmed, out var description)
                ? description
                : $"Unknown ({code})";
        }
    }

    /// <summary>
    /// ANSI/NIST-ITL Impression Type kódok.
    /// Megadja, hogyan készült az ujjlenyomat (Type-14, 14.003 mező).
    /// </summary>
    public static class ImpressionTypeCode
    {
        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>
        {
            { "0", "Live-scan plain" },
            { "1", "Live-scan rolled" },
            { "2", "Nonlive-scan plain" },
            { "3", "Nonlive-scan rolled" },
            { "4", "Latent impression" },
            { "5", "Latent tracing" },
            { "6", "Latent photo" },
            { "7", "Latent lift" },
            { "8", "Live-scan swipe" },
            { "9", "Live-scan vertical roll" },
            { "10", "Live-scan palm" },
            { "20", "Other" },
            { "21", "Contactless plain" },
            { "22", "Contactless rolled" },
            { "28", "Unknown live-scan" },
            { "29", "Unknown" }
        };

        /// <summary>
        /// Visszaadja az impression type emberi olvasható leírását.
        /// </summary>
        /// <param name="code">Az impression type kód</param>
        /// <returns>Emberi olvasható leírás</returns>
        public static string GetDescription(string? code)
        {
            if (code == null) return "Ismeretlen";
            var trimmed = code.Trim();

            return Descriptions.TryGetValue(trimmed, out var description)
                ? description
                : $"Unknown ({code})";
        }
    }
}
