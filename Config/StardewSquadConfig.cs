using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MobileUISupport.Config
{
    internal sealed class StardewSquadConfig
    {
        // ═══════════════════════════════════════════════════════
        // Button Settings
        // ═══════════════════════════════════════════════════════

        /// <summary>Enable mobile button untuk The Stardew Squad</summary>
        public bool EnableSupport { get; set; } = true;

        /// <summary>Radius deteksi NPC (dalam tiles)</summary>
        public float DetectionRadius { get; set; } = 3.0f;

        /// <summary>
        /// Anchor point untuk tombol Squad
        /// Tombol akan diposisikan relatif terhadap titik ini
        /// </summary>
        public ButtonAnchor ButtonAnchor { get; set; } = ButtonAnchor.CenterRight;

        /// <summary>
        /// Offset horizontal dari anchor point (dalam pixels)
        /// Untuk anchor kanan: jarak dari tepi kanan
        /// Untuk anchor kiri: jarak dari tepi kiri
        /// </summary>
        public int ButtonOffsetX { get; set; } = 20;

        /// <summary>
        /// Offset vertikal dari anchor point (dalam pixels)
        /// Untuk anchor atas: jarak dari tepi atas
        /// Untuk anchor bawah: jarak dari tepi bawah
        /// Untuk anchor tengah: offset dari tengah (+ ke bawah, - ke atas)
        /// </summary>
        public int ButtonOffsetY { get; set; } = 0;

        /// <summary>Skala ukuran tombol (1.0 - 4.0)</summary>
        public float ButtonScale { get; set; } = 1.5f;

        /// <summary>Opacity tombol (0.0 - 1.0)</summary>
        public float ButtonOpacity { get; set; } = 0.95f;

        /// <summary>Tampilkan nama NPC di atas tombol</summary>
        public bool ShowNPCName { get; set; } = true;

        /// <summary>Hanya tampilkan tombol saat ada NPC terdekat</summary>
        public bool ShowButtonOnlyWhenNearNPC { get; set; } = false;

        public bool ShowNoNPCNearbyMessage { get; set; } = true;
    }
}
