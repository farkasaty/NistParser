namespace NistParser.Constants
{
    /// <summary>
    /// Defines the ASCII separator characters used in ANSI/NIST-ITL file format.
    /// These characters create a hierarchical structure: Transaction > Records > Fields > Subfields > Items
    /// </summary>
    public static class SeparatorConstants
    {
        /// <summary>
        /// Unit Separator (US) - Decimal 31, Hex 0x1F
        /// Separates individual information items within a field or subfield
        /// Lowest level of hierarchy
        /// </summary>
        public const byte US = 0x1F;

        /// <summary>
        /// Record Separator (RS) - Decimal 30, Hex 0x1E
        /// Separates repeated subfields within a field
        /// </summary>
        public const byte RS = 0x1E;

        /// <summary>
        /// Group Separator (GS) - Decimal 29, Hex 0x1D
        /// Separates fields within a logical record
        /// </summary>
        public const byte GS = 0x1D;

        /// <summary>
        /// File Separator (FS) - Decimal 28, Hex 0x1C
        /// Separates logical records within a transaction
        /// Highest level of hierarchy
        /// </summary>
        public const byte FS = 0x1C;

        /// <summary>
        /// Colon character used in tagged field format: "X.YYY:data"
        /// </summary>
        public const byte COLON = (byte)':';

        /// <summary>
        /// Period character used in field numbering: "X.YYY"
        /// </summary>
        public const byte PERIOD = (byte)'.';

        /// <summary>
        /// Gets the name of a separator character for display/logging purposes
        /// </summary>
        /// <param name="separator">The separator byte value</param>
        /// <returns>The name of the separator (e.g., "US", "RS", "GS", "FS")</returns>
        public static string GetSeparatorName(byte separator) => separator switch
        {
            US => "US (Unit Separator)",
            RS => "RS (Record Separator)",
            GS => "GS (Group Separator)",
            FS => "FS (File Separator)",
            _ => $"Unknown (0x{separator:X2})"
        };
    }
}
