using MobileUISupport.Framework;
using StardewModdingAPI;

namespace MobileUISupport.Integrations.GMCM
{
    /// <summary>
    /// Integration dengan Generic Mod Config Menu.
    /// Menggunakan page-based navigation untuk organisasi yang lebih baik.
    /// </summary>
    public class GMCMIntegration
    {
        // ═══════════════════════════════════════════════════════
        // Constants
        // ═══════════════════════════════════════════════════════

        private const string GMCM_MOD_ID = "spacechase0.GenericModConfigMenu";

        // ═══════════════════════════════════════════════════════
        // Fields
        // ═══════════════════════════════════════════════════════

        private IGenericModConfigMenuApi? _api;
        private readonly List<IGMCMPageBuilder> _pageBuilders = new();

        // ═══════════════════════════════════════════════════════
        // Properties
        // ═══════════════════════════════════════════════════════

        public bool IsAvailable => _api != null;

        private IModHelper Helper => ModServices.Helper;
        private IManifest Manifest => ModServices.Manifest;
        private ModConfig Config => ModServices.Config;

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Initialize dan register GMCM.
        /// </summary>
        public bool Initialize()
        {
            if (!Helper.ModRegistry.IsLoaded(GMCM_MOD_ID))
            {
                Logger.Info("GMCM is not installed.");
                return false;
            }

            _api = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(GMCM_MOD_ID);

            if (_api == null)
            {
                Logger.Warn("Failed to get GMCM API!");
                return false;
            }

            Logger.Debug("GMCM API obtained, registering...");

            // Register page builders
            RegisterPageBuilders();

            // Build menu
            BuildMenu();

            Logger.IntegrationStatus("GMCM integration", true);
            return true;
        }

        /// <summary>
        /// Register semua page builders.
        /// Tambahkan page builder baru di sini.
        /// </summary>
        private void RegisterPageBuilders()
        {
            _pageBuilders.Clear();

            // Register halaman-halaman
            _pageBuilders.Add(new MagicStardewPageBuilder());
            _pageBuilders.Add(new StardewSquadPageBuilder());

            // Tambahkan page builder baru di sini:
            // _pageBuilders.Add(new NewModPageBuilder());
        }

        // ═══════════════════════════════════════════════════════
        // Menu Building
        // ═══════════════════════════════════════════════════════

        private void BuildMenu()
        {
            // Unregister jika sudah ada
            TryUnregister();

            // Register mod
            _api!.Register(
                mod: Manifest,
                reset: ResetConfig,
                save: ModServices.SaveConfig
            );

            // Build main page dengan navigation links
            BuildMainPage();

            // Build each page
            foreach (var builder in _pageBuilders)
            {
                builder.Build(_api, Manifest, Config);
            }

            Logger.Info("GMCM config menu registered successfully!");
        }

        private void BuildMainPage()
        {
            // Add navigation links untuk setiap page
            foreach (var builder in _pageBuilders)
            {
                _api!.AddPageLink(
                    mod: Manifest,
                    pageId: builder.PageId,
                    text: () => builder.PageTitle,
                    tooltip: () => builder.PageTooltip
                );
            }
        }

        private void TryUnregister()
        {
            try
            {
                _api?.Unregister(Manifest);
            }
            catch
            {
                // Ignore - mungkin belum terdaftar
            }
        }

        // ═══════════════════════════════════════════════════════
        // Config Reset
        // ═══════════════════════════════════════════════════════

        private void ResetConfig()
        {
            var defaultConfig = new ModConfig();
            var config = Config;

            // Copy semua values dari default
            CopyConfigValues(defaultConfig, config);

            Logger.Debug("Config reset to defaults");
        }

        private static void CopyConfigValues(ModConfig source, ModConfig target)
        {
            // Squad settings
            target.EnableSquadSupport = source.EnableSquadSupport;
            target.SquadDetectionRadius = source.SquadDetectionRadius;
            target.SquadButtonAnchor = source.SquadButtonAnchor;
            target.SquadButtonOffsetX = source.SquadButtonOffsetX;
            target.SquadButtonOffsetY = source.SquadButtonOffsetY;
            target.SquadButtonScale = source.SquadButtonScale;
            target.SquadButtonOpacity = source.SquadButtonOpacity;
            target.ShowNPCName = source.ShowNPCName;
            target.ShowButtonOnlyWhenNearNPC = source.ShowButtonOnlyWhenNearNPC;

            // Magic settings
            target.SpellIconSize = source.SpellIconSize;
            target.GridColumns = source.GridColumns;
            target.GridRows = source.GridRows;
            target.IconSpacing = source.IconSpacing;
            target.MenuPadding = source.MenuPadding;
            target.ShowCastAnimation = source.ShowCastAnimation;
            target.ThemeColor = source.ThemeColor;
            target.ShowSelectionAnimation = source.ShowSelectionAnimation;
            target.AnimationSpeed = source.AnimationSpeed;
            target.BackgroundOpacity = source.BackgroundOpacity;
            target.CloseAfterCast = source.CloseAfterCast;
            target.CloseDelay = source.CloseDelay;
            target.ShowTooltips = source.ShowTooltips;
            target.ConfirmHighManaCast = source.ConfirmHighManaCast;
            target.HighManaThreshold = source.HighManaThreshold;
            target.EnableSounds = source.EnableSounds;

            // Advanced settings
            target.DebugMode = source.DebugMode;
            target.UseOriginalMenu = source.UseOriginalMenu;
        }
    }
}