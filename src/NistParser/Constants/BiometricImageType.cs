namespace NistParser.Constants
{
    /// <summary>
    /// A biometrikus képek típusai a NIST szabvány szerint.
    /// Az egyes típusok a NIST rekordtípusoknak felelnek meg.
    /// </summary>
    public enum BiometricImageType
    {
        /// <summary>
        /// Ujjlenyomat kép (Type-4, Type-14)
        /// Rolled vagy flat ujjlenyomat, magas felbontású
        /// </summary>
        Fingerprint,

        /// <summary>
        /// Látens ujjlenyomat kép (Type-13)
        /// Helyszíni nyomok, általában alacsonyabb minőségű
        /// </summary>
        LatentFingerprint,

        /// <summary>
        /// Arckép (Type-10, IMT="FACE")
        /// Teljes arc, profil, vagy egyéb arckép
        /// </summary>
        Face,

        /// <summary>
        /// Heg, jegy, tetoválás kép (Type-10, IMT="SCAR"/"MARK"/"TATTOO")
        /// Scars, Marks, and Tattoos
        /// </summary>
        SMT,

        /// <summary>
        /// Tenyérnyomat kép (Type-15)
        /// Teljes tenyér vagy résztenyér nyomat
        /// </summary>
        PalmPrint,

        /// <summary>
        /// Írisz kép (Type-17)
        /// Szemírisz biometrikus kép
        /// </summary>
        Iris,

        /// <summary>
        /// Lábnyomat kép (Type-19)
        /// Plantar (talp) biometrikus kép
        /// </summary>
        Footprint,

        /// <summary>
        /// Aláírás kép (Type-8)
        /// Digitalizált aláírás
        /// </summary>
        Signature,

        /// <summary>
        /// Egyéb vagy ismeretlen típusú kép
        /// Type-7, Type-20, Type-21, Type-22, stb.
        /// </summary>
        Other
    }
}
