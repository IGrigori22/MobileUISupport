using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileUISupport.Config
{
    internal sealed class MagicStardewConfig
    {
        // ═══════════════════════════════════════════════════════
        // Layout Settings
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
        // Appearance Settings
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
        // Behavior Settings
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
        //  Sound Settings
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Aktifkan sound effects
        /// </summary>
        public bool EnableSounds { get; set; } = true;               

        // ═══════════════════════════════════════════════════════
        //  Advance Settings
        // ═══════════════════════════════════════════════════════

        public bool HideButton {  get; set; } = false;
        
        /// <summary>
        /// Gunakan menu asli Magic Stardew (nonaktifkan replacement)
        /// </summary>
        public bool EnableSupport { get; set; } = true;
    }
}
