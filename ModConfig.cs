namespace MobileUISupport
{
    public sealed class ModConfig
    {
        /// <summary>
        /// MagicStardew: Ukuran Spell Icon dalam Pixels
        /// </summary>
        public int SpellIconSize { get; set; } = 64;

        /// <summary>
        /// MagicStardew: Jumlah kolom dalam grid spell
        /// </summary>
        public int GridColumns { get; set; } = 5;

        /// <summary>
        /// MagicStardew: Jumlah baris dalam grid spell (per halaman)
        /// </summary>
        public int GridRows { get; set; } = 4;

        /// <summary>
        /// MagicStardew: Jarak antar icon
        /// </summary>
        public int IconSpacing { get; set; } = 12;

        /// <summary>
        /// MagicStardew: Padding dari tepi menu
        /// </summary>
        public int MenuPadding { get; set; } = 20;

        /// <summary>
        /// MagicStardew: Tampilkan animasi saat cast spell
        /// </summary>
        public bool ShowCastAnimation { get; set; } = true;

        /// <summary>
        /// MagicStardew: Warna tema (mengikuti game atau custom)
        /// </summary>
        public string ThemeColor { get; set; } = "default";

        /// <summary>
        /// MagicStardew: Mode debug
        /// </summary>
        public bool DebugMode { get; set; } = false;
    }
}