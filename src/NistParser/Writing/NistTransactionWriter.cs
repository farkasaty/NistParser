using System.IO;
using NistParser.Core;

namespace NistParser.Writing
{

    /// <summary>
    /// Serializes a NistTransaction back to byte array format
    /// Supports both Traditional binary format and NIEM XML format
    /// </summary>
    public class NistTransactionWriter
    {
        /// <summary>
        /// Writes a NistTransaction to a byte array in Traditional binary format
        /// </summary>
        /// <param name="transaction">The transaction to write</param>
        /// <returns>The serialized byte array</returns>
        public static byte[] WriteTraditional(NistTransaction transaction)
        {
            return TraditionalFormatWriter.Write(transaction);
        }

        /// <summary>
        /// Writes a NistTransaction to a byte array in NIEM XML format
        /// </summary>
        /// <param name="transaction">The transaction to write</param>
        /// <returns>The serialized byte array</returns>
        public static byte[] WriteXml(NistTransaction transaction)
        {
            return XmlFormatWriter.Write(transaction);
        }

        /// <summary>
        /// Writes a NistTransaction to a byte array using the specified format
        /// </summary>
        /// <param name="transaction">The transaction to write</param>
        /// <param name="useXmlFormat">True to use XML format, false for traditional binary format</param>
        /// <returns>The serialized byte array</returns>
        public static byte[] Write(NistTransaction transaction, bool useXmlFormat = false)
        {
            return useXmlFormat ? WriteXml(transaction) : WriteTraditional(transaction);
        }

        /// <summary>
        /// Writes a NistTransaction to a file
        /// </summary>
        /// <param name="transaction">The transaction to write</param>
        /// <param name="filePath">The file path to write to</param>
        /// <param name="useXmlFormat">True to use XML format, false for traditional binary format</param>
        public static void WriteToFile(NistTransaction transaction, string filePath, bool useXmlFormat = false)
        {
            var data = Write(transaction, useXmlFormat);
            File.WriteAllBytes(filePath, data);
        }
    }
}
