namespace MobileUISupport
{
    /// <summary>
    /// Anchor point untuk posisi tombol relatif terhadap layar
    /// Tombol akan tetap di posisi relatif yang sama saat zoom/resize
    /// </summary>
    public enum ButtonAnchor
    {
        /// <summary>Pojok kiri atas layar</summary>
        TopLeft,

        /// <summary>Pojok kanan atas layar</summary>
        TopRight,

        /// <summary>Pojok kiri bawah layar</summary>
        BottomLeft,

        /// <summary>Pojok kanan bawah layar</summary>
        BottomRight,

        /// <summary>Tengah sisi kiri layar</summary>
        CenterLeft,

        /// <summary>Tengah sisi kanan layar</summary>
        CenterRight
    }
}