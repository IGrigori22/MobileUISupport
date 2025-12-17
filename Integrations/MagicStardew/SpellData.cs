using Microsoft.Xna.Framework.Graphics;

namespace MobileUISupport.Integrations.MagicStardew
{
    /// <summary>
    /// Data class yang merepresentasikan sebuah spell
    /// Digunakan untuk menyimpan data dari mod Magic Stardew
    /// </summary>
    public class SpellData
    {
        /// <summary>Index unik spell</summary>
        public int IconIndex { get; set; }

        /// <summary>Nama spell</summary>
        public string Name { get; set; } = "";

        /// <summary>Deskripsi spell</summary>
        public string Description { get; set; } = "";

        /// <summary>Hint untuk unlock spell</summary>
        public string UnlockHint { get; set; } = "";

        /// <summary>Base mana cost</summary>
        public int ManaCost { get; set; }

        /// <summary>Level spell yang dimiliki player (1-5, atau -1 jika locked)</summary>
        public int Level { get; set; } = -1;

        /// <summary>Apakah spell sudah di-unlock</summary>
        public bool IsUnlocked { get; set; } = false;

        /// <summary>Apakah spell visible (unlocked atau dev mode)</summary>
        public bool IsVisible { get; set; } = false;

        /// <summary>Apakah spell sedang dalam cooldown/in use</summary>
        public bool IsInUse { get; set; } = false;

        /// <summary>Slot favorite (0 = tidak favorite, 1-5 = slot)</summary>
        public int FavoriteSlot { get; set; } = 0;

        /// <summary>Reference ke object Spell asli dari Magic Stardew</summary>
        public object? OriginalSpell { get; set; }

        /// <summary>Help text untuk spell</summary>
        public string HelpText { get; set; } = "";
    }
}