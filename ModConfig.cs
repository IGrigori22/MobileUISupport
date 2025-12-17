namespace MobileUISupport
{
    public sealed class ModConfig
    {
        // ═══════════════════════════════════════════════════════════
        // Lookup Anything - Config
        // ═══════════════════════════════════════════════════════════

        /// <summary>Enable integrasi dengan Lookup Anything.</summary>
        public bool EnableLookupAnythingIntegration { get; set; } = true;

        /// <summary>Gunakan custom mobile search menu (lebih nyaman untuk touch).</summary>
        public bool UseMobileSearchMenu { get; set; } = true;

        // ═══════════════════════════════════════════════════════
        // THE STARDEW SQUAD - Button Settings
        // ═══════════════════════════════════════════════════════

        /// <summary>Enable mobile button untuk The Stardew Squad</summary>
        public bool EnableSquadSupport { get; set; } = true;

        /// <summary>Radius deteksi NPC (dalam tiles)</summary>
        public float SquadDetectionRadius { get; set; } = 3.0f;

        /// <summary>
        /// Anchor point untuk tombol Squad
        /// Tombol akan diposisikan relatif terhadap titik ini
        /// </summary>
        public ButtonAnchor SquadButtonAnchor { get; set; } = ButtonAnchor.CenterRight;

        /// <summary>
        /// Offset horizontal dari anchor point (dalam pixels)
        /// Untuk anchor kanan: jarak dari tepi kanan
        /// Untuk anchor kiri: jarak dari tepi kiri
        /// </summary>
        public int SquadButtonOffsetX { get; set; } = 20;

        /// <summary>
        /// Offset vertikal dari anchor point (dalam pixels)
        /// Untuk anchor atas: jarak dari tepi atas
        /// Untuk anchor bawah: jarak dari tepi bawah
        /// Untuk anchor tengah: offset dari tengah (+ ke bawah, - ke atas)
        /// </summary>
        public int SquadButtonOffsetY { get; set; } = 0;

        /// <summary>Skala ukuran tombol (1.0 - 4.0)</summary>
        public float SquadButtonScale { get; set; } = 1.5f;

        /// <summary>Opacity tombol (0.0 - 1.0)</summary>
        public float SquadButtonOpacity { get; set; } = 0.95f;

        /// <summary>Tampilkan nama NPC di atas tombol</summary>
        public bool ShowNPCName { get; set; } = true;

        /// <summary>Hanya tampilkan tombol saat ada NPC terdekat</summary>
        public bool ShowButtonOnlyWhenNearNPC { get; set; } = false;

        public bool ShowNoNPCNearbyMessage { get; set; } = true;


        // ═══════════════════════════════════════════════════════
        // MAGIC STARDEW - Layout Settings
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// MagicStardew: Ukuran Icon Spell dalam Pixels (32-96)
        /// </summary>
        public int SpellIconSize { get; set; } = 80;

        /// <summary>
        /// MagicStardew: Jumlah kolom dalam grid spell (3-8)
        /// </summary>
        public int GridColumns { get; set; } = 5;

        /// <summary>
        /// MagicStardew: Jumlah baris dalam grid spell per halaman (2-6)
        /// </summary>
        public int GridRows { get; set; } = 4;

        /// <summary>
        /// MagicStardew: Jarak antar icon dalam pixels (4-24)
        /// </summary>
        public int IconSpacing { get; set; } = 24;

        /// <summary>
        /// MagicStardew: Padding dari tepi menu dalam pixels (8-40)
        /// </summary>
        public int MenuPadding { get; set; } = 40;

        /// <summary>
        /// MagicStardew: Tampilkan animasi saat cast spell
        /// </summary>
        public bool ShowCastAnimation { get; set; } = true;


        // ═══════════════════════════════════════════════════════
        // MAGIC STARDEW - Appearance Settings
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// MagicStardew: Warna tema (mengikuti game atau custom)
        /// Options: default, dark, light, blue, green
        /// </summary>
        public string ThemeColor { get; set; } = "default";

        /// <summary>
        /// MagicStardew: Tampilkan animasi pulse pada spell yang dipilih
        /// </summary>
        public bool ShowSelectionAnimation { get; set; } = true;

        /// <summary>
        /// MagicStardew: Kecepatan animasi pulse (0.5 - 3.0)
        /// </summary>
        public float AnimationSpeed { get; set; } = 1.0f;

        /// <summary>
        /// MagicStardew: Opacity background menu (0.3 - 1.0)
        /// </summary>
        public float BackgroundOpacity { get; set; } = 0.6f;


        // ═══════════════════════════════════════════════════════
        // MAGIC STARDEW - Behavior Settings
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Tutup menu setelah cast spell
        /// </summary>
        public bool CloseAfterCast { get; set; } = true;

        /// <summary>
        /// Delay sebelum menu tertutup setelah cast (dalam ms)
        /// </summary>
        public int CloseDelay { get; set; } = 400;

        /// <summary>
        /// Tampilkan tooltip saat hover di spell
        /// </summary>
        public bool ShowTooltips { get; set; } = true;

        /// <summary>
        /// Konfirmasi sebelum cast spell dengan mana tinggi (>50)
        /// </summary>
        public bool ConfirmHighManaCast { get; set; } = false;

        /// <summary>
        /// Threshold mana untuk konfirmasi
        /// </summary>
        public int HighManaThreshold { get; set; } = 50;


        // ═══════════════════════════════════════════════════════
        // MAGIC STARDEW - Sound Settings
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Aktifkan sound effects
        /// </summary>
        public bool EnableSounds { get; set; } = true;


        // ═══════════════════════════════════════════════════════
        // ADVANCED SETTINGS
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Mode debug - tampilkan info debug dan hitbox
        /// </summary>
        public bool DebugMode { get; set; } = false;

        /// <summary>
        /// Gunakan menu asli Magic Stardew (nonaktifkan replacement)
        /// </summary>
        public bool UseOriginalMenu { get; set; } = false;
    }
}