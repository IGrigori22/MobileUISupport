using StardewModdingAPI;

namespace MobileUISupport
{
    /// <summary>
    /// Central service locator untuk akses shared dependencies.
    /// </summary>
    public static class ModServices
    {
        // ═══════════════════════════════════════════════════════
        // Core Services
        // ═══════════════════════════════════════════════════════

        public static IModHelper Helper { get; private set; } = null!;
        public static IMonitor Monitor { get; private set; } = null!;
        public static IManifest Manifest { get; private set; } = null!;

        // ═══════════════════════════════════════════════════════
        // Mod Services
        // ═══════════════════════════════════════════════════════

        public static ModConfig Config { get; set; } = null!;

        // ═══════════════════════════════════════════════════════
        // State
        // ═══════════════════════════════════════════════════════

        public static bool IsInitialized { get; private set; }

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Initialize core services.
        /// </summary>
        public static void Initialize(IModHelper helper, IMonitor monitor, IManifest manifest)
        {
            Helper = helper;
            Monitor = monitor;
            Manifest = manifest;

            // Load config
            Config = helper.ReadConfig<ModConfig>();

            IsInitialized = true;
        }

        /// <summary>
        /// Save config ke file.
        /// </summary>
        public static void SaveConfig()
        {
            Helper.WriteConfig(Config);
        }

        /// <summary>
        /// Reload config dari file.
        /// </summary>
        public static void ReloadConfig()
        {
            Config = Helper.ReadConfig<ModConfig>();
        }
    }
}