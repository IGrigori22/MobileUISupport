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

        private IGenericModConfigMenuApi _api;
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

            _api.AddBoolOption(
                mod: Manifest,
                getValue: () => Config.DebugMode,
                setValue: v => Config.DebugMode = v,
                name: () => "Debug Mode",
                tooltip: () => "Show debug information and hitbox overlay"
            );

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
            target.StardewSquad.EnableSupport = source.StardewSquad.EnableSupport;
            target.StardewSquad.DetectionRadius = source.StardewSquad.DetectionRadius;
            target.StardewSquad.ButtonAnchor = source.StardewSquad.ButtonAnchor;
            target.StardewSquad.ButtonOffsetX = source.StardewSquad.ButtonOffsetX;
            target.StardewSquad.ButtonOffsetY = source.StardewSquad.ButtonOffsetY;
            target.StardewSquad.ButtonScale = source.StardewSquad.ButtonScale;
            target.StardewSquad.ButtonOpacity = source.StardewSquad.ButtonOpacity;
            target.StardewSquad.ShowNPCName = source.StardewSquad.ShowNPCName;
            target.StardewSquad.ShowButtonOnlyWhenNearNPC = source.StardewSquad.ShowNoNPCNearbyMessage;

            // Magic settings
            target.MagicStardew.EnableSupport = source.MagicStardew.EnableSupport;
            target.MagicStardew.SpellIconSize = source.MagicStardew.SpellIconSize;
            target.MagicStardew.GridColumns = source.MagicStardew.GridColumns;
            target.MagicStardew.GridRows = source.MagicStardew.GridRows;
            target.MagicStardew.IconSpacing = source.MagicStardew.IconSpacing;
            target.MagicStardew.MenuPadding = source.MagicStardew.MenuPadding;
            target.MagicStardew.ShowCastAnimation = source.MagicStardew.ShowCastAnimation;
            target.MagicStardew.ThemeColor = source.MagicStardew.ThemeColor;
            target.MagicStardew.ShowSelectionAnimation = source.MagicStardew.ShowSelectionAnimation;
            target.MagicStardew.AnimationSpeed = source.MagicStardew.AnimationSpeed;
            target.MagicStardew.BackgroundOpacity = source.MagicStardew.BackgroundOpacity;
            target.MagicStardew.CloseAfterCast = source.MagicStardew.CloseAfterCast;
            target.MagicStardew.CloseDelay = source.MagicStardew.CloseDelay;
            target.MagicStardew.ShowTooltips = source.MagicStardew.ShowTooltips;
            target.MagicStardew.ConfirmHighManaCast = source.MagicStardew.ConfirmHighManaCast;
            target.MagicStardew.HighManaThreshold = source.MagicStardew.HighManaThreshold;
            target.MagicStardew.EnableSounds = source.MagicStardew.EnableSounds;

            // Advanced settings
            target.DebugMode = source.DebugMode;
        }
    }
}