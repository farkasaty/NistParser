# Eltérések az ANSI/NIST-ITL 1-2011 Update:2015 hivatalos specifikáció és a projekt dokumentációja/forráskódja között

> **Forrás PDF**: `docs/ANSI NIST ITL 1 - 20160901.pdf` (NIST SP 500-290 Edition 3, 615 oldal)
>
> **Összehasonlítva**: A `docs/` mappában lévő md fájlok és a `src/NistParser/Constants/` forráskód fájlok
>
> **Készült**: 2026-02-14

---

## 1. KRITIKUS: Type-14 mezőszámok teljesen hibásak

**Érintett fájlok**:
- `docs/TECHNICAL_SPECIFICATION.md` (9. szekció, Type-14 példa)
- `docs/IMAGE_EXTRACTION_FEATURE.md` (8.4 szekció, mező-metaadat leképezés)
- `src/NistParser/Constants/FieldDescriptions.cs` (83-105. sorok)
- `docs/NIEM_XML_IMPLEMENTATION.md` (Type-14 mező mapping)

### A hiba

A Type-14 rekord mezőszámai a dokumentációban és forráskódban **nem egyeznek** a hivatalos specifikációval. A mezők el vannak csúszva, mert a 14.005-ös mező (FCD - Fingerprint Capture Date) hiányzik, és emiatt az összes további mező egy pozícióval el van tolva.

### Összehasonlító táblázat

| Mező# | Hivatalos PDF (Table, p.359-368) | Dokumentáció/Forráskód | Státusz |
|-------|--------------------------------|----------------------|---------|
| 14.001 | LEN - Record Header | LEN - Logical Record Length | OK |
| 14.002 | IDC - Information Designation Character | IDC | OK |
| 14.003 | IMP - Impression Type | IMP - Impression Type | OK |
| 14.004 | SRC - Source Agency | SRC - Source Agency | OK |
| **14.005** | **FCD - Fingerprint Capture Date** | **FGP - Finger Position** | **HIBAS** |
| **14.006** | **HLL - Horizontal Line Length** | **ISR - Image Scanning Resolution** | **HIBAS** |
| **14.007** | **VLL - Vertical Line Length** | **HLL - Horizontal Line Length** | **HIBAS** |
| **14.008** | **SLC - Scale Units** | **VLL - Vertical Line Length** | **HIBAS** |
| **14.009** | **THPS - Transmitted Horizontal Pixel Scale** | **CA - Compression Algorithm** | **HIBAS** |
| **14.010** | **TVPS - Transmitted Vertical Pixel Scale** | **BPX - Bits Per Pixel** | **HIBAS** |
| **14.011** | **CGA - Compression Algorithm** | **FGP2 - Finger Position(s)** | **HIBAS** |
| **14.012** | **BPX - Bits Per Pixel** | **FQM - Finger Quality Metric** | **HIBAS** |
| **14.013** | **FGP - Friction Ridge Generalized Position** | **ASEG - Alternate Finger Segment** | **HIBAS** |
| 14.014 | PPD - Print Position Descriptors | NIST2 - NIST Quality Metric | HIBAS |
| 14.015 | PPC - Print Position Coordinates | SQS - Segmentation Quality Score | HIBAS |
| 14.016 | SHPS - Scanned Horizontal Pixel Scale | - | - |
| 14.017 | SVPS - Scanned Vertical Pixel Scale | - | - |
| 14.018 | AMP - Amputated or Bandaged | - | - |
| 14.020 | COM - Comment | COM - Comment | OK |
| 14.021 | SEG - Finger Segment Position | SEG | OK |
| 14.022 | NQM - NIST Quality Metric | NQM | OK |
| 14.023 | SQM - Segmentation Quality Metric | SQM | OK |
| 14.024 | FQM - Fingerprint Quality Metric | FQM2 | Mnemonik eltér |
| 14.025 | ASEG - Alternate Finger Segment Position(s) | - | - |
| 14.030 | DMM - Device Monitoring Mode | DMM | OK |
| 14.031 | FAP - Subject Acquisition Profile | - | Hiányzik |
| 14.999 | DATA - Fingerprint Image | DATA - Image Data | OK |

### Hatás

Ez azt jelenti, hogy ha a parser a 14.005-ös mezőt olvassa, azt Finger Position-nek értelmezi, holott az valójában a Fingerprint Capture Date. A valódi Finger Position a 14.013-as mezőben van.

### A `TECHNICAL_SPECIFICATION.md` hibás Type-14 példája (9.3 szekció)

**Jelenlegi (HIBÁS)**:
```
14.003:500               (Image resolution)
14.004:1                 (Impression type)
14.005:600               (Horizontal line length)
14.006:800               (Vertical line length)
14.007:7                 (Compression: JPEG 2000)
```

**Helyes (PDF alapján)**:
```
14.003:0                 (Impression type / IMP)
14.004:SRC_AGENCY        (Source agency / SRC)
14.005:20250717           (Fingerprint capture date / FCD)
14.006:600               (Horizontal line length / HLL)
14.007:800               (Vertical line length / VLL)
14.008:1                 (Scale units / SLC)
14.009:500               (Transmitted horizontal pixel scale / THPS)
14.010:500               (Transmitted vertical pixel scale / TVPS)
14.011:WSQ20             (Compression algorithm / CGA)
14.012:8                 (Bits per pixel / BPX)
14.013:1                 (Friction ridge generalized position / FGP)
```

---

## 2. KRITIKUS: Kompressziós algoritmus kódok kettős rendszere

**Érintett fájlok**:
- `src/NistParser/Constants/CompressionAlgorithm.cs`
- `src/NistParser/Constants/FieldDescriptions.cs` (203-213. sorok)
- `docs/TECHNICAL_SPECIFICATION.md` (10. szekció)
- `docs/IMAGE_EXTRACTION_FEATURE.md` (4.3 szekció)

### A hiba

A hivatalos specifikáció (Table 19, p.101) **kétféle azonosítót** definiál a tömörítési algoritmusokhoz:

| Numerikus kód | Szöveges label | Algoritmus | Hűség |
|---------------|---------------|------------|-------|
| 0 | NONE | Uncompressed | Lossless |
| 1 | WSQ20 | WSQ (Wavelet Scalar Quantization) | Lossy |
| 2 | JPEGB | JPEG Baseline | Lossy |
| 3 | JPEGL | JPEG Lossless | Lossless |
| 4 | JP2 | JPEG 2000 | Lossy |
| 5 | JP2L | JPEG 2000 Lossless | Lossless |
| 6 | PNG | PNG | Lossless |

**A szabvány szerint** (p.102):
> "Type-4 records use the **code** [numerikus]. Other record types use the **label** [szöveges]."

### Problémák:

1. **A `CompressionAlgorithm.cs` enum** csak numerikus kódokat használ (0-6), ami a Type-4 bináris rekordokra vonatkozik. A tagged field rekordok (Type-10, 13, 14, 15, 17) a **szöveges label**-eket használják.

2. **A `FieldDescriptions.cs`** helyesen szöveges kódokat használ a `GetValueInterpretation`-ben, DE a WSQ label "WSQ"-ként van feltüntetve, holott a helyes label **"WSQ20"**.

3. **A `TECHNICAL_SPECIFICATION.md`** (10.1 szekció) a kompressziós kódokat egyszerű számokként (0-6) listázza, nem említi a szöveges label-eket.

4. **Az `IMAGE_EXTRACTION_FEATURE.md`** (4.3 szekció) a kódokat is szövegesként listázza (NONE, WSQ, JPEGB, stb.), de "WSQ" helyett "WSQ20" kellene.

---

## 3. FONTOS: Type-1 mezők kötelezőségi besorolása

**Érintett fájlok**:
- `docs/TECHNICAL_SPECIFICATION.md` (7.2 szekció)
- `src/NistParser/Editing/FieldMetadataProvider.cs` (132-138. sorok)

### A hiba

A hivatalos specifikáció szerint (Table 34, p.121) az 1.011 (NSR) és 1.012 (NTR) mezők **MANDATORY (kötelezők)**, nem opcionálisak.

| Mező | PDF specifikáció | FieldMetadataProvider.cs | TECHNICAL_SPECIFICATION.md |
|------|-----------------|------------------------|---------------------------|
| 1.011 NSR | **M** (Mandatory) | `isRequired: false` | Mandatory listában (**OK**) |
| 1.012 NTR | **M** (Mandatory) | `isRequired: false` | Mandatory listában (**OK**) |

A TECHNICAL_SPECIFICATION.md helyesen sorolja fel ezeket a kötelező mezők között, de a forráskódban opcionálisnak vannak megjelölve.

**Megjegyzés**: Az 1.011 és 1.012 mezők formátuma "xx.xx" (ppmm), és "00.00"-ra kell beállítani, ha nincs Type-4 rekord a tranzakcióban. Ez a forráskódban nincs implementálva.

---

## 4. FONTOS: Impression Type kódok elavultak/hibásak

**Érintett fájlok**:
- `src/NistParser/Constants/FingerPositionCode.cs` (ImpressionTypeCode osztály, 53-89. sorok)
- `src/NistParser/Constants/FieldDescriptions.cs` (167-180. sorok)

### A hiba

A 2015-ös frissítés (Table 8, p.71) **jelentősen átdolgozta** az impression type kódokat. Az implementáció egy keveréket használ a régi és új kódokból, és néhány kód tévesen van hozzárendelve.

### Összehasonlítás

| Kód | PDF 2015 (Table 8) | Forráskód (ImpressionTypeCode) | Státusz |
|-----|--------------------|-----------------------------|---------|
| 0 | Plain Contact | Live-scan plain | Eltérő elnevezés |
| 1 | Rolled Contact | Live-scan rolled | Eltérő elnevezés |
| 2 | *(legacy, nem Table 8-ban)* | Nonlive-scan plain | Legacy, elfogadható |
| 3 | *(legacy)* | Nonlive-scan rolled | Legacy, elfogadható |
| 4 | Latent image | Latent impression | Eltérő elnevezés |
| 5 | *(legacy)* | Latent tracing | Legacy, elfogadható |
| 6 | *(legacy)* | Latent photo | Legacy, elfogadható |
| 7 | *(legacy)* | Latent lift | Legacy, elfogadható |
| 8 | Live-scan swipe | Live-scan swipe | OK |
| 9 | *(nincs ilyen kód!)* | Live-scan vertical roll | **HIBAS** - ez nem létezik |
| 10 | *(nincs ilyen kód!)* | Live-scan palm | **HIBAS** - ez nem létezik |
| 20 | *(nincs ilyen kód!)* | Other | **HIBAS** - "Other" = 28 |
| 21 | *(nincs ilyen kód!)* | Contactless plain | **HIBAS** - ez 24 lenne |
| 22 | *(nincs ilyen kód!)* | Contactless rolled | **HIBAS** - ez 25 lenne |
| 24 | Plain contactless - stationary subject | *(hiányzik)* | **HIÁNYZIK** |
| 25 | Rolled contactless - stationary subject | *(hiányzik)* | **HIÁNYZIK** |
| 28 | Other | Unknown live-scan | **HIBAS** - ez "Other" |
| 29 | Unknown | Unknown | OK |
| 41 | Rolled contactless - moving subject | *(hiányzik)* | **HIÁNYZIK** |
| 42 | Plain contactless - moving subject | *(hiányzik)* | **HIÁNYZIK** |

**Megjegyzés a legacy kódokról**: A specifikáció (p.70) szerint a régi kódok (0-39 az előző verziókból) "still allowed for use but are considered to be 'legacy'". A 2015-ös frissítés szétválasztotta az impression type-ot és a capture technology-t (FCT, Table 11).

### A `FieldDescriptions.cs` impression type hibái

Az `FieldDescriptions.cs` 167-180. sorokban lévő `GetValueInterpretation` is hibás, mert:
- "9" => "Unknown (9)" - de a specifikációban a 9-es kód nem létezik az aktuális Table 8-ban
- "8" => "Swipe (8)" - helyes, de a teljes neve "Live-scan swipe"

---

## 5. FONTOS: Finger Position kódok inkonzisztenciája

**Érintett fájlok**:
- `src/NistParser/Constants/FieldDescriptions.cs` (182-199. sorok)
- `docs/TECHNICAL_SPECIFICATION.md` (6.5 szekció)
- `docs/IMAGE_EXTRACTION_FEATURE.md` (ujjpozíció kódok táblázat)

### A hiba

A `FieldDescriptions.cs` GetValueInterpretation metódusában a 14.005 értelmezésénél (ami amúgy is rossz mezőszám, lásd 1. pont) a finger position kódok **hibásan vannak** a 11-13 tartományban:

| Kód | PDF Table 9 (p.72) | FieldDescriptions.cs | FingerPositionCode.cs | Státusz |
|-----|--------------------|---------------------|-----------------------|---------|
| 11 | **Plain right thumb** | Right four fingers | Plain Right Thumb | FD.cs HIBAS |
| 12 | **Plain left thumb** | Left four fingers | Plain Left Thumb | FD.cs HIBAS |
| 13 | **Plain right four fingers** | Both thumbs | Plain Right Four Fingers | FD.cs HIBAS |
| 14 | Plain left four fingers | *(hiányzik)* | Plain Left Four Fingers | FD.cs-ből HIÁNYZIK |
| 15 | Left & right thumbs | *(hiányzik)* | Plain Both Thumbs | FD.cs-ből HIÁNYZIK |
| 16 | Right extra digit | *(hiányzik)* | *(hiányzik)* | Mindenhonnan HIÁNYZIK |
| 17 | Left extra digit | *(hiányzik)* | *(hiányzik)* | Mindenhonnan HIÁNYZIK |
| 18 | Unknown friction ridge | *(hiányzik)* | *(hiányzik)* | Mindenhonnan HIÁNYZIK |
| 19 | EJI or tip | *(hiányzik)* | *(hiányzik)* | Mindenhonnan HIÁNYZIK |

**Fontos**: A `FingerPositionCode.cs` HELYESEN tartalmazza a 11-15 kódokat, de a `FieldDescriptions.cs` NEM. Ez inkonzisztencia a saját kódbázison belül is.

A **dokumentációkban** (`TECHNICAL_SPECIFICATION.md` és `IMAGE_EXTRACTION_FEATURE.md`) szintén a hibás kódok vannak (11=Right four fingers, 12=Left four fingers, 13=Both thumbs).

---

## 6. KÖZEPES: Type-10 mező elnevezési hibák

**Érintett fájlok**:
- `src/NistParser/Constants/FieldDescriptions.cs` (52-66. sorok)

### Összehasonlítás

| Mező | PDF specifikáció (p.217-232) | FieldDescriptions.cs | Státusz |
|------|-----------------------------|--------------------|---------|
| 10.003 | IMT - Image Type | IMT - Image Type | OK |
| 10.005 | PHD - **Photo Capture Date** | PHD - **Photo Description** | **HIBAS** |
| 10.012 | **CSP - Color Space** | **BPX - Bits Per Pixel** | **HIBAS** |
| 10.013 | SAP - Subject Acquisition Profile | *(hiányzik)* | HIÁNYZIK |
| 10.014 | FIP - Face Image Bounding Box | *(hiányzik)* | HIÁNYZIK |

A 10.005 leírása hibás: nem "Photo Description" hanem "Photo Capture Date".
A 10.012 teljesen hibás: nem BPX hanem CSP (Color Space).

---

## 7. KÖZEPES: Type-1 mező 1.004 TOT hossz korlátozás

**Érintett fájlok**:
- `src/NistParser/Editing/FieldMetadataProvider.cs` (89-93. sorok)

### A hiba

A forráskód maximum 4 karakterre korlátozza a TOT mezőt (`maxLength: 4`), de a hivatalos specifikáció (p.124) szerint:

> "This shall be a maximum of **16** alphabetic characters."

A korábbi (2011 előtti) verziók korlátozták 4 karakterre, de a jelenlegi szabvány 16-ot engedélyez.

---

## 8. KÖZEPES: Type-1 mező 1.017 struktúra hibája

**Érintett fájlok**:
- `src/NistParser/Constants/FieldDescriptions.cs` (44. sor)
- `src/NistParser/Editing/FieldMetadataProvider.cs` (158-160. sorok)

### A hiba

A PDF specifikáció (Table 34, p.123) szerint az 1.017-es mező mnemonikja **ANM** (Agency Names), nem OAN, és két al-mezőt tartalmaz:
- **DAN** - Destination Agency Name (opcionális)
- **OAN** - Originating Agency Name (opcionális)

A forráskód az egész 1.017 mezőt "OAN - Originating Agency Name"-ként kezeli, ami csak az egyik al-mező.

---

## 9. KÖZEPES: Type-1 mező 1.012 mnemonikja

**Érintett fájl**: `docs/TECHNICAL_SPECIFICATION.md`

### A hiba

A dokumentáció az 1.012 mezőt "NTR - Nominal Transmitting Resolution"-ként azonosítja. A PDF specifikáció (p.126) szerint a mező teljes neve **"Nominal resolution / NTR"** (a "Transmitting" szót elhagyták a 2011-es verzióban), bár a mnemonik megmaradt NTR-nek. Ez kisebb eltérés.

---

## 10. ALACSONY: Hiányzó FGP (Friction Ridge Generalized Position) terminológia

**Érintett fájlok**:
- `docs/TECHNICAL_SPECIFICATION.md` (6.5 szekció)
- `src/NistParser/Constants/FieldDescriptions.cs`

### A hiba

A specifikáció a 2011-es verzió óta a "Finger Position" helyett a **"Friction Ridge Generalized Position (FGP)"** terminológiát használja, mivel a tábla (Table 9) immár tartalmazza a tenyér- és talpnyomat pozíciókat is (kódok 20-84). A forráskód és dokumentáció még a régi "Finger Position" elnevezést használja.

A `FingerPositionCode.cs` fájl neve és kommentjei is a régi terminológiát tükrözik. Valójában a teljes Table 9 tartalmazza:
- Finger position kódok: 0-19
- Palm position kódok: 20-38
- Plantar position kódok: 60-84

---

## 11. ALACSONY: Type-11 és Type-12 rekord típusok besorolása

**Érintett fájl**: `docs/TECHNICAL_SPECIFICATION.md` (5.1 szekció)

### A hiba

A TECHNICAL_SPECIFICATION.md a Type-11-et és Type-12-t "Reserved"-ként jelöli, de a 2013-as frissítés óta ezek **teljesen definiált** rekord típusok:
- Type-11: Voice Data (hangrögzítések tárolása)
- Type-12: Dental Records (fogászati adatok)

---

## 12. INFO: Hiányzó Type-14 mezők a forráskódban

A következő Type-14 mezők szerepelnek a hivatalos specifikációban, de hiányoznak a `FieldDescriptions.cs`-ből:

| Mező | Mnemonik | Leírás |
|------|----------|--------|
| 14.016 | SHPS | Scanned Horizontal Pixel Scale |
| 14.017 | SVPS | Scanned Vertical Pixel Scale |
| 14.018 | AMP | Amputated or Bandaged |
| 14.025 | ASEG | Alternate Finger Segment Position(s) |
| 14.026 | SCF | Simultaneous Capture |
| 14.027 | SIF | Stitched Image Flag |
| 14.031 | FAP | Subject Acquisition Profile - Fingerprint |
| 14.046 | SUB | Image Subject Condition |
| 14.047 | CON | Capture Organization Name |
| 14.901 | FCT | Friction Ridge Capture Technology |
| 14.902 | ANN | Annotation Information |
| 14.903 | DUI | Device Unique Identifier |
| 14.904 | MMS | Make/Model/Serial Number |
| 14.993 | SAN | Source Agency Name |
| 14.994 | EFR | External File Reference |
| 14.995 | ASC | Associated Context |
| 14.996 | HAS | Hash |
| 14.997 | SOR | Source Representation |
| 14.998 | GEO | Geographic Sample Acquisition Location |

---

## Összefoglaló - Prioritás szerint

### Kritikus (Azonnali javítást igényel)
1. **Type-14 mezőszámok** - Teljes újramapping szükséges (14.005-14.015 hibás)
2. **Kompressziós kódok** - Type-4 numerikus vs. egyéb rekordok szöveges label

### Fontos (Javítandó)
3. **1.011/1.012 kötelezőség** - Mandatory, nem Optional
4. **Impression type kódok** - 2015-ös frissítés figyelembevétele
5. **Finger position kódok inkonzisztencia** - FieldDescriptions.cs vs FingerPositionCode.cs

### Közepes
6. **Type-10 mező hibák** - 10.005 és 10.012 elnevezés
7. **TOT mező hossz** - 4 helyett 16 karakter
8. **1.017 ANM struktúra** - DAN + OAN al-mezők

### Alacsony
9. FGP terminológia frissítése
10. Type-11/12 nem "Reserved"
11. Hiányzó Type-14 mezők kiegészítése

---

*Dokumentum verzió: 1.0*
*Készült: 2026-02-14*
*Forrás: ANSI/NIST-ITL 1-2011 Update:2015 (NIST SP 500-290 Edition 3)*
