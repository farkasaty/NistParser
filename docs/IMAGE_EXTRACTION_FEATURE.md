# NIST Képkinyerési Funkció - Műszaki Specifikáció

## Tartalomjegyzék

1. [Összefoglaló](#1-összefoglaló)
2. [Üzleti háttér](#2-üzleti-háttér)
3. [Funkcionális követelmények](#3-funkcionális-követelmények)
4. [Technikai háttér](#4-technikai-háttér)
5. [Architektúra](#5-architektúra)
6. [Adatmodell](#6-adatmodell)
7. [API Specifikáció](#7-api-specifikáció)
8. [Implementációs részletek](#8-implementációs-részletek)
9. [Használati példák](#9-használati-példák)
10. [Korlátozások és megjegyzések](#10-korlátozások-és-megjegyzések)
11. [Web-kompatibilis képformátum](#11-web-kompatibilis-képformátum)

---

## 1. Összefoglaló

### 1.1 Cél

A NistParser könyvtár kibővítése egy olyan funkcióval, amely képes **kinyerni a biometrikus képeket** (ujjlenyomatok, arcképek, tenyérnyomatok, stb.) a NIST fájlokból, **teljes metaadat-készlettel** együtt.

### 1.2 Főbb jellemzők

| Jellemző | Leírás |
|----------|--------|
| **Támogatott képtípusok** | Ujjlenyomat, arckép, tenyérnyomat, írisz, lábnyom, aláírás |
| **Metaadatok** | Ujjpozíció, felbontás, méret, tömörítés, forrás, és több |
| **Formátum támogatás** | Hagyományos bináris és NIEM XML formátum |
| **Kimeneti formátum** | Egységesített `BiometricImage` objektum |

### 1.3 Célközönség

- **Rendszerszervezők**: A 2-6. fejezetek áttekintése ajánlott
- **Fejlesztők**: A 6-9. fejezetek tartalmazzák a technikai részleteket

---

## 2. Üzleti háttér

### 2.1 Mi az a NIST fájl?

A **NIST fájl** (ANSI/NIST-ITL standard) egy nemzetközi szabvány szerinti biometrikus adatcsere formátum. Világszerte használják:

- Bűnügyi nyilvántartásokban
- Határellenőrzési rendszerekben
- Személyazonosítási rendszerekben
- INTERPOL adatcserében

### 2.2 Milyen képek vannak egy NIST fájlban?

```
┌─────────────────────────────────────────────────────────────┐
│                     NIST Fájl (Tranzakció)                  │
├─────────────────────────────────────────────────────────────┤
│  Type-1: Tranzakció fejléc (kötelező)                       │
│  Type-2: Személyes adatok (név, születési dátum, stb.)      │
│  Type-10: Arckép #1 ─────────────────────── 📷 JPEG kép     │
│  Type-14: Ujjlenyomat #1 (jobb hüvelyk) ─── 🖐️ WSQ kép     │
│  Type-14: Ujjlenyomat #2 (jobb mutató) ──── 🖐️ WSQ kép     │
│  Type-14: Ujjlenyomat #3 (jobb középső) ─── 🖐️ WSQ kép     │
│  ...                                                        │
│  Type-15: Tenyérnyomat ─────────────────── 🖐️ JPEG2000 kép │
└─────────────────────────────────────────────────────────────┘
```

### 2.3 Miért van szükség erre a funkcióra?

Az új funkció lehetővé teszi:

1. **Képek megjelenítését** a felhasználói felületen
2. **Képek exportálását** külön fájlokba
3. **Automatikus feldolgozást** (pl. minőségellenőrzés)
4. **Metaadatok lekérdezését** (melyik ujj, milyen felbontás)

---

## 3. Funkcionális követelmények

### 3.1 Alapfunkciók

| Kód | Funkció | Leírás |
|-----|---------|--------|
| F01 | Összes kép lekérése | A tranzakcióban lévő összes kép kinyerése |
| F02 | Típus szerinti szűrés | Csak ujjlenyomatok VAGY csak arcképek lekérése |
| F03 | IDC szerinti szűrés | Adott személyhez tartozó képek lekérése |
| F04 | Metaadatok visszaadása | Minden képhez tartozó információk lekérése |

### 3.2 Visszaadandó metaadatok

#### Általános metaadatok (minden képtípushoz)

| Metaadat | Leírás | Példa |
|----------|--------|-------|
| IDC | Azonosító karakter | "00", "01" |
| ImageType | Képtípus | Fingerprint, Face |
| RecordType | Rekord típus | Type-14, Type-10 |
| RawImageData | Nyers képadat | byte[] |
| Width | Szélesség (pixel) | 800 |
| Height | Magasság (pixel) | 750 |
| Resolution | Felbontás | 500 |
| ResolutionUnit | Felbontás egysége | "ppi" |
| Compression | Tömörítési algoritmus | WSQ, JPEG |
| SourceAgency | Forrás ügynökség | "AGENCY001" |

#### Ujjlenyomat-specifikus metaadatok

| Metaadat | Leírás | Példa érték | Olvasható forma |
|----------|--------|-------------|-----------------|
| FingerPosition | Ujj pozíció kód | "2" | - |
| FingerPositionDescription | Ujj neve | - | "Jobb mutatóujj" |
| ImpressionType | Lenyomat típus kód | "1" | - |
| ImpressionTypeDescription | Lenyomat típus | - | "Live-scan rolled" |

#### Ujjpozíció kódok

| Kód | Jelentés (magyar) | Jelentés (angol) |
|-----|-------------------|------------------|
| 0 | Ismeretlen | Unknown |
| 1 | Jobb hüvelykujj | Right thumb |
| 2 | Jobb mutatóujj | Right index |
| 3 | Jobb középső ujj | Right middle |
| 4 | Jobb gyűrűs ujj | Right ring |
| 5 | Jobb kisujj | Right little |
| 6 | Bal hüvelykujj | Left thumb |
| 7 | Bal mutatóujj | Left index |
| 8 | Bal középső ujj | Left middle |
| 9 | Bal gyűrűs ujj | Left ring |
| 10 | Bal kisujj | Left little |
| 11 | Sima jobb hüvelykujj | Plain right thumb |
| 12 | Sima bal hüvelykujj | Plain left thumb |
| 13 | Sima jobb kéz 4 ujja | Plain right four fingers |
| 14 | Sima bal kéz 4 ujja | Plain left four fingers |
| 15 | Sima bal és jobb hüvelykujj | Plain left and right thumbs |

#### Arckép-specifikus metaadatok

| Metaadat | Leírás | Példa |
|----------|--------|-------|
| FaceImageType | Kép típus kód | "FACE", "SCAR" |
| FaceImageTypeDescription | Típus leírás | "Arcfotó", "Sebhely" |
| PhotoDescription | Fénykép leírás | "Frontális arckép" |

---

## 4. Technikai háttér

### 4.1 Hol találhatók a képek a NIST fájlban?

A NIST fájlban a képadatok **rekordtípusonként** eltérő helyen találhatók:

#### Kevert rekordok (Mixed) - Modern formátum

```
Rekord felépítése:
┌────────────────────────────────────────────────────────────┐
│ 14.001:12345<GS>  ← Rekord hossz                           │
│ 14.002:00<GS>     ← IDC azonosító                          │
│ 14.003:1<GS>      ← Lenyomat típus (IMP)                    │
│ 14.005:20250101<GS> ← Ujjlenyomat rögzítés dátuma (FCD)    │
│ 14.006:800<GS>    ← Szélesség (HLL)                        │
│ 14.007:750<GS>    ← Magasság (VLL)                         │
│ 14.008:1<GS>      ← Felbontás egysége (SLC, 1=ppi)        │
│ 14.011:WSQ20<GS>  ← Tömörítés (CGA)                       │
│ 14.013:2<GS>      ← Ujjpozíció (FGP, 2=jobb mutató)       │
│ ...               ← További metaadatok                     │
│ 14.999:[BINÁRIS KÉPADAT]<FS>  ← A tényleges kép!          │
└────────────────────────────────────────────────────────────┘
```

- A **999-es mező** tartalmazza a képadatot
- A formátum: `X.999:[bináris adat]`
- A többi mező ASCII szövegként tartalmazza a metaadatokat

#### Rekordtípusok és képtartalom

| Rekord típus | Tartalom | Mező a képadatnak |
|--------------|----------|-------------------|
| Type-4 | Ujjlenyomat (régi) | Bináris struktúra |
| Type-10 | Arckép, sebhely, tetoválás | 10.999 |
| Type-13 | Latens ujjlenyomat | 13.999 |
| Type-14 | Ujjlenyomat | 14.999 |
| Type-15 | Tenyérnyomat | 15.999 |
| Type-17 | Írisz | 17.999 |
| Type-19 | Lábnyom | 19.999 |

### 4.2 Szeparátor karakterek

| Karakter | Kód | Funkció |
|----------|-----|---------|
| GS | 0x1D | Mezők elválasztása |
| FS | 0x1C | Rekordok elválasztása |
| RS | 0x1E | Almezők elválasztása |
| US | 0x1F | Információs elemek elválasztása |

### 4.3 Tömörítési formátumok

| Kód | Algoritmus | Jellemző használat |
|-----|------------|-------------------|
| NONE | Tömörítetlen | Ritka |
| WSQ | Wavelet Scalar Quantization | Ujjlenyomatok (FBI szabvány) |
| JPEGB | JPEG Baseline | Arcképek |
| JPEGL | JPEG Lossless | Minőségkritikus képek |
| JP2 | JPEG 2000 | Modern formátum |
| JP2L | JPEG 2000 Lossless | Veszteségmentes |
| PNG | PNG | Veszteségmentes |

> ⚠️ **Fontos**: A könyvtár **nem végez dekompressziót**! A visszaadott képadatok az eredeti tömörített formátumban vannak.

---

## 5. Architektúra

### 5.1 Komponens diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      NistParser Library                      │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐    ┌────────────────────────────────┐ │
│  │ NistTransaction  │───▶│     ImageExtractor (ÚJ)        │ │
│  │   Parser         │    │  - ExtractAllImages()          │ │
│  └──────────────────┘    │  - ExtractImages(type)         │ │
│           │              │  - ExtractImagesByIDC(idc)     │ │
│           ▼              └────────────────────────────────┘ │
│  ┌──────────────────┐                  │                    │
│  │  NistTransaction │                  ▼                    │
│  │  - Header        │    ┌────────────────────────────────┐ │
│  │  - Records[]     │    │     BiometricImage (ÚJ)        │ │
│  │  + GetAllImages()│───▶│  - RawImageData                │ │
│  │  + GetImages()   │    │  - FingerPosition              │ │
│  └──────────────────┘    │  - Width, Height               │ │
│                          │  - Compression                 │ │
│                          │  - AdditionalMetadata          │ │
│                          └────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 5.2 Osztálydiagram

```
┌─────────────────────────────────────────┐
│          BiometricImage                 │
├─────────────────────────────────────────┤
│ + IDC: string                           │
│ + ImageType: BiometricImageType         │
│ + RecordType: RecordType                │
│ + RawImageData: byte[]                  │
│ + Width: int?                           │
│ + Height: int?                          │
│ + Resolution: decimal?                  │
│ + ResolutionUnit: string?               │
│ + Compression: CompressionAlgorithm?    │
│ + CompressionDescription: string?       │
│ + SourceAgency: string?                 │
│ + FingerPosition: string?               │
│ + FingerPositionDescription: string?    │
│ + ImpressionType: string?               │
│ + ImpressionTypeDescription: string?    │
│ + FaceImageType: string?                │
│ + FaceImageTypeDescription: string?     │
│ + AdditionalMetadata: Dictionary        │
├─────────────────────────────────────────┤
│ + GetImageSizeDescription(): string     │
│ + ToString(): string                    │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│       «enumeration»                     │
│       BiometricImageType                │
├─────────────────────────────────────────┤
│ Fingerprint                             │
│ LatentFingerprint                       │
│ Face                                    │
│ SMT (Scar, Mark, Tattoo)                │
│ PalmPrint                               │
│ Iris                                    │
│ Footprint                               │
│ Signature                               │
│ Other                                   │
└─────────────────────────────────────────┘
```

---

## 6. Adatmodell

### 6.1 BiometricImage osztály

```csharp
namespace NistParser.Models
{
    /// <summary>
    /// Egy biometrikus kép reprezentációja metaadatokkal.
    /// </summary>
    public class BiometricImage
    {
        // === ALAPVETŐ AZONOSÍTÓK ===
        
        /// <summary>
        /// Information Designation Character - összekapcsolja a kapcsolódó rekordokat
        /// </summary>
        public string IDC { get; set; } = string.Empty;
        
        /// <summary>
        /// A kép típusa (ujjlenyomat, arckép, stb.)
        /// </summary>
        public BiometricImageType ImageType { get; set; }
        
        /// <summary>
        /// A forrás rekord típusa (Type-10, Type-14, stb.)
        /// </summary>
        public RecordType RecordType { get; set; }
        
        // === KÉPADAT ===
        
        /// <summary>
        /// A nyers, tömörített képadat bájt tömbként.
        /// FIGYELEM: Ez az adat tömörített formátumban van (WSQ, JPEG, stb.)!
        /// </summary>
        public byte[] RawImageData { get; set; } = Array.Empty<byte>();
        
        // === MÉRET ÉS FELBONTÁS ===
        
        /// <summary>Kép szélessége pixelben</summary>
        public int? Width { get; set; }
        
        /// <summary>Kép magassága pixelben</summary>
        public int? Height { get; set; }
        
        /// <summary>Képfelbontás (tipikusan 500 vagy 1000)</summary>
        public decimal? Resolution { get; set; }
        
        /// <summary>Felbontás mértékegysége ("ppi" vagy "ppcm")</summary>
        public string? ResolutionUnit { get; set; }
        
        // === TÖMÖRÍTÉS ===
        
        /// <summary>Tömörítési algoritmus enum értéke</summary>
        public CompressionAlgorithm? Compression { get; set; }
        
        /// <summary>Tömörítési algoritmus emberi olvasható neve</summary>
        public string? CompressionDescription { get; set; }
        
        // === FORRÁS ===
        
        /// <summary>Forrás ügynökség azonosítója</summary>
        public string? SourceAgency { get; set; }
        
        // === UJJLENYOMAT-SPECIFIKUS ===
        
        /// <summary>Ujjpozíció kód (1-13)</summary>
        public string? FingerPosition { get; set; }
        
        /// <summary>Ujjpozíció emberi olvasható formában (pl. "Jobb mutatóujj")</summary>
        public string? FingerPositionDescription { get; set; }
        
        /// <summary>Lenyomat típus kód</summary>
        public string? ImpressionType { get; set; }
        
        /// <summary>Lenyomat típus emberi olvasható formában</summary>
        public string? ImpressionTypeDescription { get; set; }
        
        // === ARCKÉP-SPECIFIKUS ===
        
        /// <summary>Arckép típus kód (FACE, SCAR, MARK, TATTOO)</summary>
        public string? FaceImageType { get; set; }
        
        /// <summary>Arckép típus emberi olvasható formában</summary>
        public string? FaceImageTypeDescription { get; set; }
        
        /// <summary>Fénykép leírás</summary>
        public string? PhotoDescription { get; set; }
        
        // === EGYÉB ===
        
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
        public bool IsWebConvertible => Compression != CompressionAlgorithm.WSQ;
        
        /// <summary>
        /// Megadja, hogy a natív formátum közvetlenül megjeleníthető-e böngészőben
        /// (JPEG vagy PNG).
        /// </summary>
        public bool IsNativelyWebCompatible => 
            Compression == CompressionAlgorithm.JPEG ||
            Compression == CompressionAlgorithm.JPEG_Lossy ||
            Compression == CompressionAlgorithm.PNG ||
            Compression == CompressionAlgorithm.None;
        
        /// <summary>
        /// A web-kompatibilis formátum MIME típusa.
        /// </summary>
        public string WebCompatibleMimeType => IsNativelyWebCompatible && 
            (Compression == CompressionAlgorithm.JPEG || Compression == CompressionAlgorithm.JPEG_Lossy)
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
        /// </summary>
        /// <exception cref="ImageConversionNotSupportedException">
        /// Ha a formátum nem konvertálható (pl. WSQ)
        /// </exception>
        public byte[] GetWebCompatibleImage()
        {
            if (!IsWebConvertible)
                throw new ImageConversionNotSupportedException(
                    $"A {Compression} formátum nem konvertálható web-kompatibilis formátumra.");
            
            if (IsNativelyWebCompatible)
                return RawImageData;
            
            // JPEG 2000 és egyéb formátumok konvertálása PNG-re
            return ImageConverter.ConvertToPng(RawImageData, Compression);
        }
        
        /// <summary>
        /// Visszaadja a képet Base64 kódolásban, HTML img tag-be ágyazható formátumban.
        /// </summary>
        public string GetWebCompatibleImageAsBase64()
        {
            var imageData = GetWebCompatibleImage();
            var base64 = Convert.ToBase64String(imageData);
            return $"data:{WebCompatibleMimeType};base64,{base64}";
        }
        
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
```

### 6.2 BiometricImageType enum

```csharp
namespace NistParser.Constants
{
    /// <summary>
    /// A biometrikus képek típusai
    /// </summary>
    public enum BiometricImageType
    {
        /// <summary>Ujjlenyomat (Type-4, Type-14)</summary>
        Fingerprint,
        
        /// <summary>Latens (helyszíni) ujjlenyomat (Type-13)</summary>
        LatentFingerprint,
        
        /// <summary>Arckép (Type-10 - FACE)</summary>
        Face,
        
        /// <summary>Sebhely, jel, tetoválás (Type-10 - SCAR/MARK/TATTOO)</summary>
        SMT,
        
        /// <summary>Tenyérnyomat (Type-15)</summary>
        PalmPrint,
        
        /// <summary>Írisz kép (Type-17)</summary>
        Iris,
        
        /// <summary>Lábnyom (Type-19)</summary>
        Footprint,
        
        /// <summary>Aláírás (Type-8)</summary>
        Signature,
        
        /// <summary>Egyéb képtípus</summary>
        Other
    }
}
```

---

## 7. API Specifikáció

### 7.1 NistTransaction bővítés

```csharp
public class NistTransaction
{
    // Meglévő tulajdonságok és metódusok...
    
    /// <summary>
    /// Visszaadja az összes képet a tranzakcióból.
    /// </summary>
    /// <returns>Biometrikus képek listája metaadatokkal</returns>
    public IEnumerable<BiometricImage> GetAllImages();
    
    /// <summary>
    /// Visszaadja a megadott típusú képeket.
    /// </summary>
    /// <param name="imageType">Képtípus (pl. Fingerprint, Face)</param>
    /// <returns>A megadott típusú képek listája</returns>
    public IEnumerable<BiometricImage> GetImages(BiometricImageType imageType);
    
    /// <summary>
    /// Visszaadja a megadott IDC-hez tartozó képeket.
    /// </summary>
    /// <param name="idc">Information Designation Character</param>
    /// <returns>Az adott IDC-hez tartozó képek</returns>
    public IEnumerable<BiometricImage> GetImagesByIDC(string idc);
}
```

### 7.2 ImageExtractor szolgáltatás

```csharp
namespace NistParser.Utilities
{
    /// <summary>
    /// Statikus szolgáltatás a képek kinyerésére NIST tranzakciókból.
    /// </summary>
    public static class ImageExtractor
    {
        /// <summary>
        /// Kinyeri az összes képet a tranzakcióból.
        /// </summary>
        public static IEnumerable<BiometricImage> ExtractAllImages(
            NistTransaction transaction);
        
        /// <summary>
        /// Kinyeri a megadott típusú képeket.
        /// </summary>
        public static IEnumerable<BiometricImage> ExtractImages(
            NistTransaction transaction, 
            BiometricImageType imageType);
        
        /// <summary>
        /// Kinyeri a megadott IDC-hez tartozó képeket.
        /// </summary>
        public static IEnumerable<BiometricImage> ExtractImagesByIDC(
            NistTransaction transaction, 
            string idc);
    }
}
```

---

## 8. Implementációs részletek

### 8.1 Új fájlok

| Fájl | Hely | Leírás |
|------|------|--------|
| `BiometricImage.cs` | `src/NistParser/Models/` | Képmodell osztály |
| `BiometricImageType.cs` | `src/NistParser/Constants/` | Képtípus enum |
| `ImageExtractor.cs` | `src/NistParser/Utilities/` | Kinyerő szolgáltatás |

### 8.2 Módosított fájlok

| Fájl | Módosítás |
|------|-----------|
| `NistTransaction.cs` | Új metódusok: `GetAllImages()`, `GetImages()`, `GetImagesByIDC()` |

### 8.3 Képkinyerési logika

```
┌─────────────────────────────────────────────────────────────┐
│                    ExtractAllImages()                        │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│  Végigiterálunk a transaction.Records listán                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
           ┌───────────────┼───────────────┐
           ▼               ▼               ▼
    ┌──────────┐    ┌──────────┐    ┌──────────┐
    │ Type-10  │    │ Type-14  │    │ Type-15  │  ...
    │ (Face)   │    │ (Finger) │    │ (Palm)   │
    └────┬─────┘    └────┬─────┘    └────┬─────┘
         │               │               │
         ▼               ▼               ▼
    ┌─────────────────────────────────────────────┐
    │  Field X.999 kinyerése → RawImageData       │
    └─────────────────────────────────────────────┘
         │               │               │
         ▼               ▼               ▼
    ┌─────────────────────────────────────────────┐
    │  Metaadatok kinyerése az X.001-X.998 mezők- │
    │  ből, és lefordítása emberi olvasható       │
    │  formára a FieldDescriptions.cs segítségével│
    └─────────────────────────────────────────────┘
         │               │               │
         ▼               ▼               ▼
    ┌─────────────────────────────────────────────┐
    │         BiometricImage objektum létrehozása  │
    └─────────────────────────────────────────────┘
```

### 8.4 Mező-metaadat leképezés

| Rekord típus | Mező | Mnemonik | Célmező a BiometricImage-ben |
|--------------|------|----------|------------------------------|
| Type-14 | 14.003 | IMP | ImpressionType, ImpressionTypeDescription |
| Type-14 | 14.004 | SRC | SourceAgency |
| Type-14 | 14.005 | FCD | AdditionalMetadata["CaptureDate"] |
| Type-14 | 14.006 | HLL | Width |
| Type-14 | 14.007 | VLL | Height |
| Type-14 | 14.008 | SLC | ResolutionUnit |
| Type-14 | 14.009 | THPS | Resolution |
| Type-14 | 14.011 | CGA | Compression, CompressionDescription |
| Type-14 | 14.013 | FGP | FingerPosition, FingerPositionDescription |
| Type-14 | 14.999 | DATA | RawImageData |
| Type-10 | 10.003 | IMT | FaceImageType, FaceImageTypeDescription |
| Type-10 | 10.004 | SRC | SourceAgency |
| Type-10 | 10.006 | HLL | Width |
| Type-10 | 10.007 | VLL | Height |
| Type-10 | 10.011 | CGA | Compression, CompressionDescription |
| Type-10 | 10.999 | DATA | RawImageData |

---

## 9. Használati példák

### 9.1 Összes kép lekérése

```csharp
using NistParser;
using NistParser.Models;

// NIST fájl betöltése
var transaction = NistTransactionParser.ParseFile("szemely.nist");

// Összes kép lekérése
var images = transaction.GetAllImages();

Console.WriteLine($"Összesen {images.Count()} kép található a fájlban:");

foreach (var image in images)
{
    Console.WriteLine($"  - {image}");
}
```

**Kimenet:**
```
Összesen 12 kép található a fájlban:
  - Face, 480x640 px, JPEG Baseline, 45.2 KB
  - Fingerprint - Jobb hüvelykujj, 800x750 px, WSQ, 12.3 KB
  - Fingerprint - Jobb mutatóujj, 800x750 px, WSQ, 11.8 KB
  - ...
```

### 9.2 Csak ujjlenyomatok lekérése

```csharp
using NistParser.Constants;

// Csak ujjlenyomatok
var fingerprints = transaction.GetImages(BiometricImageType.Fingerprint);

foreach (var fp in fingerprints)
{
    Console.WriteLine($"Ujj: {fp.FingerPositionDescription}");
    Console.WriteLine($"  Típus: {fp.ImpressionTypeDescription}");
    Console.WriteLine($"  Méret: {fp.GetImageSizeDescription()}");
    Console.WriteLine($"  Felbontás: {fp.Resolution} {fp.ResolutionUnit}");
    Console.WriteLine($"  Tömörítés: {fp.CompressionDescription}");
    Console.WriteLine($"  Adat méret: {fp.GetDataSizeKB():F1} KB");
    Console.WriteLine();
}
```

**Kimenet:**
```
Ujj: Jobb hüvelykujj
  Típus: Live-scan rolled
  Méret: 800x750 px
  Felbontás: 500 ppi
  Tömörítés: WSQ - Wavelet Scalar Quantization
  Adat méret: 12.3 KB

Ujj: Jobb mutatóujj
  Típus: Live-scan rolled
  Méret: 800x750 px
  ...
```

### 9.3 Képek mentése fájlba

```csharp
// Összes kép mentése külön fájlokba
var images = transaction.GetAllImages();

foreach (var image in images)
{
    // Fájlnév generálása
    string filename = image.ImageType switch
    {
        BiometricImageType.Fingerprint => 
            $"fingerprint_{image.FingerPosition}_{image.IDC}",
        BiometricImageType.Face => 
            $"face_{image.IDC}",
        _ => 
            $"{image.ImageType}_{image.IDC}"
    };
    
    // Kiterjesztés a tömörítés alapján
    string extension = image.Compression switch
    {
        CompressionAlgorithm.WSQ => ".wsq",
        CompressionAlgorithm.JPEG or CompressionAlgorithm.JPEG_Lossy => ".jpg",
        CompressionAlgorithm.JPEG2000_Lossy or CompressionAlgorithm.JPEG2000_Lossless => ".jp2",
        CompressionAlgorithm.PNG => ".png",
        _ => ".dat"
    };
    
    // Mentés
    File.WriteAllBytes($"{filename}{extension}", image.RawImageData);
    Console.WriteLine($"Mentve: {filename}{extension}");
}
```

### 9.4 Arcképek lekérése

```csharp
var faces = transaction.GetImages(BiometricImageType.Face);

foreach (var face in faces)
{
    Console.WriteLine($"Típus: {face.FaceImageTypeDescription}");
    Console.WriteLine($"Leírás: {face.PhotoDescription}");
    Console.WriteLine($"Méret: {face.GetImageSizeDescription()}");
}
```

---

## 10. Korlátozások és megjegyzések

### 10.1 Fontos korlátozások

> ⚠️ **A könyvtár NEM végez képdekódolást!**

A visszaadott `RawImageData` az eredeti tömörített formátumban van. A képek megjelenítéséhez külső könyvtár szükséges:

| Formátum | Ajánlott könyvtár |
|----------|-------------------|
| WSQ | libfprint, NBIS |
| JPEG | System.Drawing, ImageSharp |
| JPEG 2000 | OpenJPEG, CSJ2K |
| PNG | Beépített .NET támogatás |

### 10.2 Támogatott és nem támogatott esetek

| Eset | Támogatott |
|------|------------|
| Hagyományos bináris NIST fájl | ✅ Igen |
| NIEM XML NIST fájl | ✅ Igen |
| Type-4 (régi bináris ujjlenyomat) | ⚠️ Korlátozott |
| Type-3, Type-5, Type-6 (deprecated) | ❌ Nem |
| Több kép egy rekordban | ✅ Igen (ha külön IDC) |

### 10.3 Teljesítmény megfontolások

- A képadatok a memóriában tárolódnak
- Nagy fájlok (sok nagy felbontású kép) sok memóriát igényelnek
- Javaslat: nagy fájlok esetén használjon lazy loading-ot vagy streamelést

---

## Függelék: Gyors referencia

### Rekordtípus → Képtípus leképezés

| RecordType | BiometricImageType |
|------------|-------------------|
| Type4_HighResGrayscaleFingerprint | Fingerprint |
| Type10_FacialAndSMTImage | Face / SMT |
| Type13_LatentImage | LatentFingerprint |
| Type14_FingerprintImage | Fingerprint |
| Type15_PalmPrintImage | PalmPrint |
| Type17_IrisImage | Iris |
| Type19_PlantarImage | Footprint |
| Type8_SignatureImage | Signature |

### Ujjpozíció gyors referencia

```
       BAL KÉZ                    JOBB KÉZ
    ┌─┬─┬─┬─┬─┐                ┌─┬─┬─┬─┬─┐
    │10│9│8│7│6│                │5│4│3│2│1│
    └─┴─┴─┴─┴─┘                └─┴─┴─┴─┴─┘
      K G K M H                  K G K M H
      i y ö u ü                  i y ö u ü
      s ű z t v                  s ű z t v
      u r é a e                  u r é a e
      j ű p t l                  j ű p t l
      j s s ó y                  j s s ó y
        ő ő                        ő ő
```

---

*Dokumentum verzió: 1.1*  
*Utolsó frissítés: 2026-02-08*

---

## 11. Web-kompatibilis képformátum

### 11.1 Probléma

A NIST fájlokban tárolt képek gyakran **speciális formátumokban** vannak, amelyeket a böngészők nem tudnak megjeleníteni:

| Formátum | Böngésző támogatás | Megjegyzés |
|----------|-------------------|------------|
| **JPEG** | ✅ Natív | Közvetlenül megjeleníthető |
| **PNG** | ✅ Natív | Közvetlenül megjeleníthető |
| **JPEG 2000** | ❌ Nem támogatott | Konverzió szükséges |
| **WSQ** | ❌ Nem támogatott | FBI-specifikus, konverzió szükséges |
| **RAW** | ❌ Nem támogatott | Konverzió szükséges |

**Tipikus helyzet:**
- Arcképek → JPEG formátum → ✅ Működik
- Ujjlenyomatok → WSQ formátum → ❌ Nem működik böngészőben

### 11.2 Megoldás: Kétféle kimeneti mód

A `BiometricImage` osztály **kétféle kimeneti módot** támogat:

```
┌─────────────────────────────────────────────────────────────┐
│                    BiometricImage                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  RawImageData ─────────────────► Natív formátum (WSQ, stb.) │
│       │                          - Eredeti minőség          │
│       │                          - Speciális szoftver kell  │
│       │                                                      │
│       ▼                                                      │
│  GetWebCompatibleImage() ──────► PNG formátum               │
│                                  - Böngészőben megjeleníthető│
│                                  - Base64 HTML-be ágyazható │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 11.3 API bővítés

#### BiometricImage új metódusai

```csharp
public class BiometricImage
{
    // Meglévő tulajdonságok...
    
    /// <summary>
    /// Visszaadja a képet web-kompatibilis formátumban (PNG).
    /// WSQ, JPEG2000 és RAW képeket konvertál, JPEG és PNG képeket változatlanul adja vissza.
    /// </summary>
    /// <returns>PNG formátumú képadat, vagy az eredeti JPEG/PNG</returns>
    /// <exception cref="ImageConversionException">Ha a konverzió sikertelen</exception>
    public byte[] GetWebCompatibleImage();
    
    /// <summary>
    /// Visszaadja a képet Base64 kódolásban, HTML img tag-be ágyazható formátumban.
    /// </summary>
    /// <returns>Base64 string data URI formátumban (pl. "data:image/png;base64,...")</returns>
    public string GetWebCompatibleImageAsBase64();
    
    /// <summary>
    /// Megadja, hogy a natív formátum közvetlenül megjeleníthető-e böngészőben.
    /// </summary>
    public bool IsNativelyWebCompatible { get; }
    
    /// <summary>
    /// Visszaadja a web-kompatibilis MIME típust.
    /// </summary>
    public string WebCompatibleMimeType { get; }
}
```

### 11.4 Konverziós logika

```
┌─────────────────────────────────────────────────────────────┐
│              GetWebCompatibleImage() logika                  │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           ▼
              ┌────────────────────────┐
              │  Mi az eredeti formátum? │
              └────────────┬───────────┘
                           │
     ┌─────────────────────┼─────────────────────┐
     ▼                     ▼                     ▼
┌─────────┐          ┌───────────┐          ┌─────────┐
│  JPEG   │          │ JPEG 2000 │          │   WSQ   │
│  PNG    │          │  (JP2)    │          │   RAW   │
└────┬────┘          └─────┬─────┘          └────┬────┘
     │                     │                     │
     ▼                     ▼                     ▼
┌───────────┐        ┌───────────┐        ┌───────────────┐
│ Változat- │        │ Konvertá- │        │ ❌ NEM        │
│ lanul     │        │ lás PNG   │        │ TÁMOGATOTT    │
│ visszaad  │        │ formátumra│        │ (kivétel)     │
└───────────┘        └───────────┘        └───────────────┘
     ✅                   ✅                     ❌
```


### 11.5 WSQ dekódolás - NEM TÁMOGATOTT

> [!CAUTION]
> **A WSQ formátum dekódolása NEM támogatott!**

A WSQ (Wavelet Scalar Quantization) az FBI által fejlesztett ujjlenyomat-tömörítési formátum. Sajnos **nincs elérhető pure .NET, cross-platform megoldás** a dekódolásához:

| Könyvtár | Probléma |
|----------|----------|
| AFIS.WSQ | Windows-only natív DLL függőség |
| Cognaxon | Kereskedelmi licenc szükséges |
| NBIS | C nyelven írt, natív fordítás szükséges |

**Következmény**: Ha egy kép WSQ formátumban van tárolva, a `GetWebCompatibleImage()` metódus **`ImageConversionNotSupportedException`** kivételt dob.

**Ajánlott megoldás**: 
- WSQ formátumú képeknél a `RawImageData` változatlanul elérhető
- A kliens oldalon speciális szoftverrel megjeleníthető
- Vagy a NIST fájl generálásakor kérje JPEG/PNG formátumban a képeket

```csharp
// WSQ kép kezelése
if (image.Compression == CompressionAlgorithm.WSQ)
{
    // Natív adat visszaadása - kliens oldalon kell dekódolni
    return image.RawImageData;
}
else
{
    // Konvertálható formátum
    return image.GetWebCompatibleImage();
}
```

### 11.6 JPEG 2000 dekódolás - TÁMOGATOTT ✅

A JPEG 2000 dekódolásához **pure C#, cross-platform** megoldás elérhető:

```xml
<!-- NistParser.csproj -->
<PackageReference Include="CoreJ2K" Version="1.0.0" />
```

**CoreJ2K jellemzői:**
- 100% managed C# (nincs natív függőség)
- Cross-platform: Windows, Linux, macOS
- .NET Standard 2.0 kompatibilis
- Nyílt forráskódú (GitHub)
- ISO/IEC 15444-1 kompatibilis

**Alternatíva:**
```xml
<PackageReference Include="CSJ2K" Version="2.0.0" />
```

A CSJ2K szintén pure C# implementáció, .NET Standard támogatással.

### 11.7 Támogatottsági mátrix

| Formátum | Dekódolás | Böngésző megjelenítés | Megjegyzés |
|----------|-----------|----------------------|------------|
| **JPEG** | ✅ Natív | ✅ Közvetlenül | Nincs konverzió szükséges |
| **PNG** | ✅ Natív | ✅ Közvetlenül | Nincs konverzió szükséges |
| **JPEG 2000** | ✅ CoreJ2K | ✅ PNG-re konvertálva | Pure C#, cross-platform |
| **WSQ** | ❌ Nem támogatott | ❌ Nem lehetséges | Nincs cross-platform megoldás |
| **RAW** | ⚠️ Korlátozott | ✅ PNG-re konvertálva | Pixel adat átformázása |

### 11.8 Használati példák

#### 11.8.1 Kép megjelenítése ASPX oldalon (WSQ kezeléssel)

```aspx
<%@ Page Language="C#" %>
<%@ Import Namespace="NistParser" %>
<%@ Import Namespace="NistParser.Models" %>
<%@ Import Namespace="NistParser.Constants" %>

<script runat="server">
    protected void Page_Load(object sender, EventArgs e)
    {
        // NIST fájl betöltése
        var transaction = NistTransactionParser.ParseFile(Server.MapPath("~/Data/szemely.nist"));
        
        // Csak konvertálható képek lekérése (WSQ kizárva)
        var images = transaction.GetAllImages()
            .Where(img => img.IsWebConvertible)
            .ToList();
        
        // Repeater-hez kötés
        rptImages.DataSource = images;
        rptImages.DataBind();
        
        // WSQ képek száma (nem megjeleníthető)
        var wsqCount = transaction.GetAllImages()
            .Count(img => img.Compression == CompressionAlgorithm.WSQ);
        if (wsqCount > 0)
        {
            lblWarning.Text = $"Figyelem: {wsqCount} WSQ formátumú kép nem jeleníthető meg!";
        }
    }
</script>

<html>
<body>
    <h1>Biometrikus képek</h1>
    <asp:Label ID="lblWarning" runat="server" ForeColor="Orange" />
    
    <asp:Repeater ID="rptImages" runat="server">
        <ItemTemplate>
            <div class="image-card">
                <h3><%# Eval("ImageType") %> - <%# Eval("FingerPositionDescription") %></h3>
                
                <!-- Kép megjelenítése Base64 formátumban -->
                <img src='<%# ((BiometricImage)Container.DataItem).GetWebCompatibleImageAsBase64() %>' 
                     alt="Biometrikus kép" />
                
                <p>Méret: <%# Eval("Width") %>x<%# Eval("Height") %> px</p>
                <p>Eredeti formátum: <%# Eval("CompressionDescription") %></p>
            </div>
        </ItemTemplate>
    </asp:Repeater>
</body>
</html>
```

#### 11.7.2 Kép visszaadása HTTP response-ként

```csharp
// ASP.NET WebForms - ASHX handler
public class ImageHandler : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        string nistFile = context.Request.QueryString["file"];
        string imageIndex = context.Request.QueryString["index"];
        
        var transaction = NistTransactionParser.ParseFile(nistFile);
        var image = transaction.GetAllImages().ElementAt(int.Parse(imageIndex));
        
        // Web-kompatibilis formátumban visszaadás
        byte[] webImage = image.GetWebCompatibleImage();
        
        context.Response.ContentType = image.WebCompatibleMimeType;
        context.Response.BinaryWrite(webImage);
    }
    
    public bool IsReusable => true;
}
```

**Használat HTML-ben:**
```html
<img src="ImageHandler.ashx?file=szemely.nist&index=0" alt="Arckép" />
<img src="ImageHandler.ashx?file=szemely.nist&index=1" alt="Ujjlenyomat" />
```

#### 11.7.3 Natív és web-kompatibilis formátum összehasonlítása

```csharp
var transaction = NistTransactionParser.ParseFile("szemely.nist");

foreach (var image in transaction.GetAllImages())
{
    Console.WriteLine($"Kép: {image.FingerPositionDescription ?? image.ImageType.ToString()}");
    Console.WriteLine($"  Eredeti formátum: {image.CompressionDescription}");
    Console.WriteLine($"  Eredeti méret: {image.RawImageData.Length / 1024.0:F1} KB");
    Console.WriteLine($"  Böngésző-kompatibilis: {image.IsNativelyWebCompatible}");
    
    if (!image.IsNativelyWebCompatible)
    {
        var webImage = image.GetWebCompatibleImage();
        Console.WriteLine($"  Konvertált méret: {webImage.Length / 1024.0:F1} KB (PNG)");
    }
    
    Console.WriteLine();
}
```

**Kimenet:**
```
Kép: Face
  Eredeti formátum: JPEG Baseline
  Eredeti méret: 45.2 KB
  Web-konvertálható: True
  Böngésző-kompatibilis: True

Kép: Jobb hüvelykujj
  Eredeti formátum: WSQ - Wavelet Scalar Quantization
  Eredeti méret: 12.3 KB
  Web-konvertálható: False  ⚠️ WSQ nem támogatott!

Kép: Tenyérnyomat
  Eredeti formátum: JPEG 2000
  Eredeti méret: 85.4 KB
  Web-konvertálható: True
  Konvertált méret: 312.5 KB (PNG)
```

### 11.9 Architektúra bővítés

```
┌─────────────────────────────────────────────────────────────┐
│                   NistParser Library                         │
│                  (Linux Docker kompatibilis)                 │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐    ┌────────────────────────────────┐ │
│  │  BiometricImage  │───▶│     ImageConverter (ÚJ)        │ │
│  │  - RawImageData  │    │  - ConvertToPng(byte[])        │ │
│  │  + GetWebCompat..│    │  - DecodeJpeg2000(byte[])      │ │
│  │  + IsWebConvert..│    │  - IsConversionSupported()     │ │
│  └──────────────────┘    └────────────────────────────────┘ │
│                                       │                      │
│                                       ▼                      │
│                          ┌────────────────────────────────┐ │
│                          │   Függőségek (Pure C#)         │ │
│                          │   - CoreJ2K (JPEG2000)         │ │
│                          │   - SkiaSharp (PNG kimenet)    │ │
│                          │   ❌ WSQ: NEM TÁMOGATOTT       │ │
│                          └────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 11.10 Új fájlok a konverzióhoz

| Fájl | Hely | Leírás |
|------|------|--------|
| `ImageConverter.cs` | `src/NistParser/Utilities/` | Képkonverziós szolgáltatás |
| `ImageConversionNotSupportedException.cs` | `src/NistParser/Exceptions/` | WSQ és egyéb nem támogatott formátum kivétel |

### 11.11 Teljesítmény megjegyzések

> [!TIP]
> **Javaslatok a teljesítmény optimalizálásához**

| Művelet | Tipikus idő | Megjegyzés |
|---------|-------------|------------|
| JPEG2000 → PNG | 30-80 ms | CPU-intenzív |
| JPEG visszaadás | < 1 ms | Nincs konverzió |
| PNG visszaadás | < 1 ms | Nincs konverzió |
| WSQ | N/A | ❌ Nem támogatott |

**Javaslatok:**
- Cache-elje a konvertált képeket (Redis, memória cache)
- Nagy mennyiségű kép esetén használjon aszinkron feldolgozást
- Fontolja meg a képek előre konvertálását és tárolását
- Docker környezetben allokáljon elegendő memóriát a konverzióhoz

### 11.12 Összefoglaló

| Funkció | Leírás |
|---------|--------|
| `RawImageData` | Eredeti formátum (WSQ, JPEG, stb.) - **mindig elérhető** |
| `IsWebConvertible` | Igaz, ha a formátum konvertálható (JPEG, PNG, JP2) |
| `GetWebCompatibleImage()` | PNG/JPEG formátum böngészőhöz - **ha konvertálható** |
| `GetWebCompatibleImageAsBase64()` | Base64 string HTML-hez - `data:image/png;base64,...` |
| `IsNativelyWebCompatible` | Igaz, ha az eredeti formátum böngésző-kompatibilis |

> [!IMPORTANT]
> **WSQ formátum NEM konvertálható!** A `GetWebCompatibleImage()` metódus `ImageConversionNotSupportedException` kivételt dob WSQ képeknél. Mindig ellenőrizze az `IsWebConvertible` tulajdonságot!

---

*Dokumentum verzió: 1.2*  
*Utolsó frissítés: 2026-02-08*  
*Változások: Linux Docker kompatibilitás, WSQ nem támogatott, CoreJ2K JPEG2000 dekóder*
