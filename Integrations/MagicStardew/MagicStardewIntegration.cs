using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.AddonsAPI;
using MobileUISupport.Integrations.Base;
using MobileUISupport.Patches;
using StardewModdingAPI;
using StardewValley;

namespace MobileUISupport.Integrations.MagicStardew
{
    /// <summary>
    /// Integration dengan Magic Stardew mod.
    /// Register button ke AddonsMobile untuk akses spell menu.
    /// </summary>
    public class MagicStardewIntegration : BaseIntegration
    {
        // ═══════════════════════════════════════════════════════
        // Constants - Button IDs
        // ═══════════════════════════════════════════════════════

        private const string BUTTON_ID_SPELL_MENU = "MobileUISupport.Magic.SpellMenu";

        // ═══════════════════════════════════════════════════════
        // Properties - BaseIntegration
        // ═══════════════════════════════════════════════════════

        public override string ModId => "Zexu2K.MagicStardew.C";
        public override string DisplayName => "Magic Stardew";
        public override bool IsEnabled => !Config.UseOriginalMenu;

        // ═══════════════════════════════════════════════════════
        // Properties - Components
        // ═══════════════════════════════════════════════════════

        public MagicStardewAPI? API { get; private set; }
        public SpellMenuInterceptor? MenuInterceptor { get; private set; }

        // Reference ke AddonsAPI
        private AddonsAPIIntegration? _addonsAPI;

        // Textures
        private Texture2D? _spellMenuIcon;

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        protected override bool DoInitialize()
        {
            // Initialize API
            API = new MagicStardewAPI(Monitor, Helper);

            // Create interceptor
            MenuInterceptor = new SpellMenuInterceptor(
                Monitor,
                Helper,
                Config,
                API
            );

            // Setup interceptor
            if (!MenuInterceptor.Setup())
            {
                Logger.Error("Failed to setup Magic menu interceptor!");
                return false;
            }

            Logger.Banner("Magic Mobile UI - Ready! ✓");
            Logger.LogConfigSummary();

            return true;
        }

        /// <summary>
        /// Set reference ke AddonsAPI integration.
        /// </summary>
        public void SetAddonsAPI(AddonsAPIIntegration? addonsAPI)
        {
            _addonsAPI = addonsAPI;
            Logger.Debug($"AddonsAPI reference set: {addonsAPI != null}");
        }

        public override void OnSaveLoaded()
        {
            if (API == null)
                return;

            // Initialize Magic API
            bool initialized = API.Initialize();
            Logger.DebugOnly("MagicStardew", $"API initialized: {initialized}");

            if (!initialized)
                return;

            // Load icon texture
            LoadButtonIcon();

            // Register button ke AddonsAPI
            RegisterSpellMenuButton();
        }

        public override void OnReturnedToTitle()
        {
            // Button akan otomatis di-unregister oleh AddonsAPIIntegration
            // karena kita menggunakan wrapper yang track IDs
        }

        // ═══════════════════════════════════════════════════════
        // Button Registration
        // ═══════════════════════════════════════════════════════

        private void LoadButtonIcon()
        {
            try
            {
                // Coba load custom icon
                _spellMenuIcon = Helper.ModContent.Load<Texture2D>("assets/MagicSpellMenuIcon.png");
                Logger.Debug("Loaded custom spell menu icon");
            }
            catch
            {
                // Fallback: gunakan texture dari Magic Stardew
                _spellMenuIcon = API?.SpellIconsTexture;
                Logger.Debug("Using MagicStardew spell icons as button icon");
            }
        }

        private void RegisterSpellMenuButton()
        {
            if (_addonsAPI == null || !_addonsAPI.IsAvailable)
            {
                Logger.Debug("AddonsAPI not available, skipping button registration");
                return;
            }

            // Gunakan builder untuk registrasi
            var builder = _addonsAPI.CreateButton(BUTTON_ID_SPELL_MENU);

            if (builder == null)
            {
                Logger.Warn("Failed to create button builder for Spell Menu");
                return;
            }

            // Configure button
            builder
                .WithDisplayName("Spell Menu")
                .WithDescription("Open the mobile-friendly spell casting menu")
                .WithCategory(KeyCategory.Menu)
                .WithPriority(100)
                .WithCooldown(300)
                .WithOriginalKeybind("C")
                .WithVisibilityCondition(CanShowSpellMenu)
                .OnPressed(OnSpellMenuPressed);

            // Add icon jika tersedia
            if (_spellMenuIcon != null)
            {
                // Icon pertama dari spritesheet, 16x16
                builder.WithIcon(_spellMenuIcon, new Rectangle(0, 0, 16, 16));
            }

            // Tint color ungu untuk magic theme
            builder.WithTintColor(new Color(138, 43, 226));

            // Register
            if (builder.Register())
            {
                Logger.Info("✓ Spell Menu button registered to AddonsMobile");
            }

            // Refresh UI
            _addonsAPI.RefreshUI();
        }

        // ═══════════════════════════════════════════════════════
        // Visibility & Callbacks
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Kondisi untuk menampilkan spell menu button.
        /// </summary>
        private bool CanShowSpellMenu()
        {
            // Harus in-game
            if (!Context.IsWorldReady)
                return false;

            // Tidak ada menu aktif
            if (Game1.activeClickableMenu != null)
                return false;

            // Tidak dalam event/cutscene
            if (Game1.eventUp)
                return false;

            // Tidak dalam dialog
            if (Game1.dialogueUp)
                return false;

            return true;
        }

        /// <summary>
        /// Callback saat button ditekan.
        /// </summary>
        private void OnSpellMenuPressed()
        {
            Logger.Debug("Spell Menu button pressed");
            OpenSpellMenu();
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Spell Menu
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Buka spell menu mobile.
        /// </summary>
        public void OpenSpellMenu()
        {
            if (!IsAvailable || MenuInterceptor == null)
            {
                Logger.Warn("Cannot open spell menu: integration not available");
                return;
            }

            if (Game1.activeClickableMenu != null)
            {
                Logger.Debug("Cannot open spell menu: another menu is active");
                return;
            }

            try
            {
                bool success = MenuInterceptor.OpenMobileSpellMenu();

                if (success)
                {
                    Logger.Debug("Spell menu opened successfully");
                }
                else
                {
                    Logger.Warn("Failed to open spell menu");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error opening spell menu: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Button Control
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Enable/disable spell menu button.
        /// </summary>
        public void SetSpellMenuButtonEnabled(bool enabled)
        {
            _addonsAPI?.SetButtonEnabled(BUTTON_ID_SPELL_MENU, enabled);
        }

        /// <summary>
        /// Check apakah button terdaftar.
        /// </summary>
        public bool IsSpellMenuButtonRegistered()
        {
            return _addonsAPI?.IsButtonRegistered(BUTTON_ID_SPELL_MENU) ?? false;
        }
    }
}