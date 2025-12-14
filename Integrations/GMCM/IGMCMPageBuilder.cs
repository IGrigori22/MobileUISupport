using StardewModdingAPI;

namespace MobileUISupport.Integrations.GMCM
{
    /// <summary>
    /// Interface untuk GMCM page builders.
    /// Setiap mod integration bisa punya page builder sendiri.
    /// </summary>
    public interface IGMCMPageBuilder
    {
        /// <summary>
        /// Unique page ID.
        /// </summary>
        string PageId { get; }

        /// <summary>
        /// Title untuk navigation link.
        /// </summary>
        string PageTitle { get; }

        /// <summary>
        /// Tooltip untuk navigation link.
        /// </summary>
        string PageTooltip { get; }

        /// <summary>
        /// Build page content.
        /// </summary>
        void Build(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config);
    }
}