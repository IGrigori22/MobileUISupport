using MobileUISupport.Config;

namespace MobileUISupport
{
    public sealed class ModConfig
    {
        internal MHEventsListConfig MHEventsList { get; set; } = new();
        /// <summary>
        /// Lookup Anything integration settings.
        /// </summary>
        internal LookupAnythingConfig LookupAnything { get; set; } = new();

        /// <summary>
        /// Magic Stardew integration settings.
        /// </summary>
        internal MagicStardewConfig MagicStardew { get; set; } = new();

        /// <summary>
        /// The Stardew Squad button settings.
        /// </summary>
        internal StardewSquadConfig StardewSquad { get; set; } = new();

        /// <summary>
        /// Debug settings.
        /// </summary>
        internal AdvanceConfig Advance { get; set; } = new();


        /// <summary>
        /// Mode debug - tampilkan info debug dan hitbox
        /// </summary>
        public bool DebugMode { get; set; } = false;
    }
}