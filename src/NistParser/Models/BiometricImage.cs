using System;
using System.Collections.Generic;
using NistParser.Constants;
using NistParser.Exceptions;

namespace NistParser.Models
{
    /// <summary>
    /// Egy biometrikus képet reprezentál a NIST tranzakcióból, teljes metaadat-készlettel.
    /// Ez az osztály egységesíti a különböző rekordtípusokból (Type-4, Type-10, Type-14, stb.)
    /// származó képadatokat egyetlen, könnyen használható objektumba.
    /// </summary>
    public class BiometricImage
    {
        // === ALAPADATOK ===

        /// <summary>
        /// Egyedi azonosító (Information Designation Character).
        /// A tranzakción belüli rekord-azonosító ("00"-"99").
        /// </summary>
        public string IDC { get; set; } = "00";

        /// <summary>
        /// A kép biometrikus típusa (ujjlenyomat, arckép, tenyérnyomat, stb.)
        /// </summary>
        public BiometricImageType ImageType { get; set; }

        /// <summary>
        /// A NIST rekord típusa, amelyből a kép származik (pl. Type-14, Type-10)
        /// </summary>
        public RecordType RecordType { get; set; }

        // === KÉPADAT ===

        /// <summary>
        /// Nyers (eredeti) képadat bájt tömbként, pontosan úgy, ahogy a NIST fájlban szerepel.
        /// A formátum a Compression tulajdonság által meghatározott tömörítési algoritmusnak megfelelő.
        /// </summary>
        public byte[] RawImageData { get; set; } = Array.Empty<byte>();

        // === MÉRET ÉS FELBONTÁS ===

        /// <summary>
        /// Kép szélessége pixelben (Field X.010 / HLL - Horizontal Line Length)
        /// </summary>
        public int? Width { get; set; }

        /// <summary>
        /// Kép magassága pixelben (Field X.011 / VLL - Vertical Line Length)
        /// </summary>
        public int? Height { get; set; }

        /// <summary>
        /// Felbontás értéke (ppi vagy ppmm, a ResolutionUnit-tól függően)
        /// </summary>
        public decimal? Resolution { get; set; }

        /// <summary>
        /// Felbontás mértékegysége ("1" = pixels per inch, "2" = pixels per centimeter)
        /// </summary>
        public string? ResolutionUnit { get; set; }

        /// <summary>
        /// Felbontás emberi olvasható formában (pl. "500 ppi")
        /// </summary>
        public string? ResolutionDescription
        {
            get
            {
                if (!Resolution.HasValue) return null;
                var unit = ResolutionUnit == "2" ? "ppcm" : "ppi";
                return $"{Resolution} {unit}";
            }
        }

        // === TÖMÖRÍTÉS ===

        /// <summary>
        /// A képadat tömörítési algoritmusa
        /// </summary>
        public CompressionAlgorithm? Compression { get; set; }

        /// <summary>
        /// A tömörítési algoritmus tömörítési kód (eredeti szám a NIST fájlból)
        /// </summary>
        public string? CompressionCode { get; set; }

        /// <summary>
        /// A tömörítési algoritmus emberi olvasható neve
        /// </summary>
        public string? CompressionDescription =>
            Compression?.GetDisplayName() ?? $"Unknown ({CompressionCode})";

        // === UJJLENYOMAT SPECIFIKUS ===

        /// <summary>
        /// Ujjpozíció kód (Field 14.005 / FP - Finger Position)
        /// Értékek: "1"-"10" (egyedi ujjak), "11"-"15" (csoportok)
        /// </summary>
        public string? FingerPosition { get; set; }

        /// <summary>
        /// Ujjpozíció emberi olvasható leírás (pl. "Right Thumb / Jobb hüvelykujj")
        /// </summary>
        public string? FingerPositionDescription =>
            string.IsNullOrEmpty(FingerPosition)
                ? null
                : FingerPositionCode.GetDescription(FingerPosition);

        /// <summary>
        /// Impression type kód (Field 14.003 / IMP)
        /// Megadja, hogyan készült az ujjlenyomat
        /// </summary>
        public string? ImpressionType { get; set; }

        /// <summary>
        /// Impression type emberi olvasható leírás (pl. "Live-scan rolled")
        /// </summary>
        public string? ImpressionTypeDescription =>
            string.IsNullOrEmpty(ImpressionType)
                ? null
                : ImpressionTypeCode.GetDescription(ImpressionType);

        // === ARCKÉP SPECIFIKUS ===

        /// <summary>
        /// Arckép típus kód (Field 10.003 / IMT - Image Type)
        /// Értékek: "FACE", "SCAR", "MARK", "TATTOO", stb.
        /// </summary>
        public string? FaceImageType { get; set; }

        /// <summary>
        /// Arckép típus emberi olvasható leírás
        /// </summary>
        public string? FaceImageTypeDescription { get; set; }

        /// <summary>
        /// Arckép leírás (Field 10.020 / SMD)
        /// </summary>
        public string? PhotoDescription { get; set; }

        // === FORRÁS ===

        /// <summary>
        /// Forrás ügynökség (Field X.004 / SRC - Source Agency/ORI)
        /// </summary>
        public string? SourceAgency { get; set; }

        // === EGYÉB METAADATOK ===

        /// <summary>
        /// További metaadatok kulcs-érték formában.
        /// Itt tárolódnak a rekord-specifikus mezők, amelyek nem kerültek külön tulajdonságba.
        /// </summary>
        public Dictionary<string, string> AdditionalMetadata { get; set; }
            = new Dictionary<string, string>();

        // === WEB KOMPATIBILITÁS ===

        /// <summary>
        /// Megadja, hogy a kép konvertálható-e web-kompatibilis formátumba.
        /// WSQ formátum NEM konvertálható (nincs cross-platform pure .NET dekóder).
        /// </summary>
        public bool IsWebConvertible =>
            Compression != CompressionAlgorithm.WSQ;

        /// <summary>
        /// Megadja, hogy a natív formátum közvetlenül megjeleníthető-e böngészőben
        /// (JPEG vagy PNG formátumok).
        /// </summary>
        public bool IsNativelyWebCompatible =>
            Compression == CompressionAlgorithm.JPEG_Lossy ||
            Compression == CompressionAlgorithm.JPEG_Lossless ||
            Compression == CompressionAlgorithm.PNG;

        /// <summary>
        /// A web-kompatibilis formátum MIME típusa.
        /// JPEG formátumú képeknél "image/jpeg", minden más esetben "image/png".
        /// </summary>
        public string WebCompatibleMimeType =>
            IsNativelyWebCompatible &&
            (Compression == CompressionAlgorithm.JPEG_Lossy || Compression == CompressionAlgorithm.JPEG_Lossless)
                ? "image/jpeg"
                : "image/png";

        // === SEGÉDMETÓDUSOK ===

        /// <summary>
        /// Visszaadja a kép méretét olvasható formában (pl. "800x750 px")
        /// </summary>
        public string GetImageSizeDescription()
        {
            if (Width.HasValue && Height.HasValue)
                return $"{Width}x{Height} px";
            return "Ismeretlen méret";
        }

        /// <summary>
        /// Visszaadja a képadat méretét KB-ban
        /// </summary>
        public double GetDataSizeKB() => RawImageData.Length / 1024.0;

        /// <summary>
        /// Visszaadja a képet web-kompatibilis formátumban (PNG vagy JPEG).
        /// Ha a kép már natívan böngésző-kompatibilis (JPEG, PNG), változatlanul visszaadja.
        /// JPEG 2000 formátumot PNG-re konvertálja.
        /// </summary>
        /// <exception cref="ImageConversionNotSupportedException">
        /// Ha a formátum nem konvertálható (pl. WSQ - nincs cross-platform pure .NET dekóder)
        /// </exception>
        public byte[] GetWebCompatibleImage()
        {
            if (!IsWebConvertible)
                throw new ImageConversionNotSupportedException(
                    Compression?.ToString() ?? "Unknown",
                    $"A(z) {CompressionDescription} formátum nem konvertálható web-kompatibilis formátumra. " +
                    "WSQ dekódoláshoz nincs elérhető cross-platform pure .NET megoldás.");

            if (IsNativelyWebCompatible)
                return RawImageData;

            // JPEG 2000 és RAW formátumok konvertálása PNG-re
            return Utilities.ImageConverter.ConvertToPng(RawImageData, Compression);
        }

        /// <summary>
        /// Visszaadja a képet Base64 kódolásban, HTML img tag-be ágyazható formátumban.
        /// Eredmény formátum: "data:image/png;base64,..." vagy "data:image/jpeg;base64,..."
        /// </summary>
        /// <exception cref="ImageConversionNotSupportedException">
        /// Ha a formátum nem konvertálható (pl. WSQ)
        /// </exception>
        public string GetWebCompatibleImageAsBase64()
        {
            var imageData = GetWebCompatibleImage();
            var base64 = Convert.ToBase64String(imageData);
            return $"data:{WebCompatibleMimeType};base64,{base64}";
        }

        /// <summary>
        /// Visszaadja a nyers képadatot Base64 kódolásban (konverzió nélkül).
        /// Közvetlenül az eredeti formátumban, ami nem feltétlenül böngésző-kompatibilis.
        /// </summary>
        public string GetRawImageAsBase64()
        {
            return Convert.ToBase64String(RawImageData);
        }

        /// <summary>
        /// String reprezentáció a kép fő jellemzőivel
        /// </summary>
        public override string ToString()
        {
            var fingerInfo = !string.IsNullOrEmpty(FingerPositionDescription)
                ? $" - {FingerPositionDescription}"
                : "";
            var webInfo = IsWebConvertible ? "" : " [WSQ - nem konvertálható]";
            return $"{ImageType}{fingerInfo}, {GetImageSizeDescription()}, " +
                   $"{CompressionDescription ?? "?"}, {GetDataSizeKB():F1} KB{webInfo}";
        }
    }
}
