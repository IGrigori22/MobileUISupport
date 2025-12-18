// File: Integrations/LookupAnything/LookupAnythingIntegration.cs

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.AddonsAPI;
using MobileUISupport.Integrations.Base;
using MobileUISupport.UI;
using MobileUISupport.Config;
using StardewModdingAPI;
using StardewValley;

namespace MobileUISupport.Integrations.LookupAnything
{
    /// <summary>
    /// Integration dengan Lookup Anything mod dari Pathoschild.
    /// Register button ke AddonsMobile untuk akses lookup dan search.
    /// </summary>
    public class LookupAnythingIntegration : BaseIntegration
    {
        // ═══════════════════════════════════════════════════════
        // Constants - Button IDs
        // ═══════════════════════════════════════════════════════

        private const string BUTTON_ID_LOOKUP = "MobileUISupport.Lookup.Trigger";
        private const string BUTTON_ID_SEARCH = "MobileUISupport.Lookup.Search";

        // ═══════════════════════════════════════════════════════
        // Properties - BaseIntegration
        // ═══════════════════════════════════════════════════════

        public override string ModId => "Pathoschild.LookupAnything";
        public override string DisplayName => "Lookup Anything";
        public override bool IsEnabled => Config.LookupAnything.EnableLookupAnythingIntegration;

        // ═══════════════════════════════════════════════════════
        // Properties - Components
        // ═══════════════════════════════════════════════════════

        public LookupAnythingAPI API { get; private set; }

        // Reference ke AddonsAPI
        private AddonsAPIIntegration _addonsAPI;

        // Textures
        private Texture2D _lookupIcon;
        private Texture2D _searchIcon;

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        protected override bool DoInitialize()
        {
            // Initialize API (reflection helper)
            API = new LookupAnythingAPI(Monitor, Helper);

            if (!API.IsValid)
            {
                Logger.Error("Failed to initialize LookupAnything reflection!");
                return false;
            }

            Logger.Banner("Lookup Anything Mobile UI - Ready! ✓");
            return true;
        }

        /// <summary>
        /// Set reference ke AddonsAPI integration.
        /// </summary>
        public void SetAddonsAPI(AddonsAPIIntegration addonsAPI)
        {
            _addonsAPI = addonsAPI;
            Logger.Debug($"AddonsAPI reference set: {addonsAPI != null}");
        }

        public override void OnSaveLoaded()
        {
            if (API == null || !API.IsValid)
                return;

            // Load icon textures
            LoadButtonIcons();

            // Register buttons ke AddonsAPI
            RegisterLookupButton();
            RegisterSearchButton();
        }

        public override void OnReturnedToTitle()
        {
            // Button akan otomatis di-unregister oleh AddonsAPIIntegration
        }

        // ═══════════════════════════════════════════════════════
        // Icon Loading
        // ═══════════════════════════════════════════════════════

        private void LoadButtonIcons()
        {
            try
            {
                // Coba load custom icons
                _lookupIcon = Helper.ModContent.Load<Texture2D>("assets/LookupIcon.png");
                Logger.Debug("Loaded custom lookup icon");
            }
            catch
            {
                // Fallback: akan gunakan icon dari game
                _lookupIcon = null;
                Logger.Debug("Using default lookup icon");
            }

            try
            {
                _searchIcon = Helper.ModContent.Load<Texture2D>("assets/SearchIcon.png");
                Logger.Debug("Loaded custom search icon");
            }
            catch
            {
                _searchIcon = null;
                Logger.Debug("Using default search icon");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Button Registration
        // ═══════════════════════════════════════════════════════

        private void RegisterLookupButton()
        {
            if (_addonsAPI == null || !_addonsAPI.IsAvailable)
            {
                Logger.Debug("AddonsAPI not available, skipping lookup button registration");
                return;
            }

            var builder = _addonsAPI.CreateButton(BUTTON_ID_LOOKUP);
            if (builder == null)
            {
                Logger.Warn("Failed to create button builder for Lookup");
                return;
            }

            // Configure button
            builder
                .WithDisplayName("Lookup")
                .WithDescription("Look up information about the object in front of you")
                .WithCategory(KeyCategory.Information)
                .WithPriority(90)
                .WithCooldown(200)
                .WithOriginalKeybind("F1")
                .WithVisibilityCondition(CanShowLookupButton)
                .OnPressed(OnLookupButtonPressed);

            // Add icon jika tersedia
            if (_lookupIcon != null)
            {
                builder.WithIcon(_lookupIcon, new Rectangle(0, 0, 16, 16));
            }
            else
            {
                // Gunakan icon dari game (magnifying glass dari cursors)
                builder.WithIcon(Game1.mouseCursors, new Rectangle(80, 0, 16, 16));
            }

            // Tint color biru untuk lookup theme
            builder.WithTintColor(new Color(70, 130, 180));

            if (builder.Register())
            {
                Logger.Info("✓ Lookup button registered to AddonsMobile");
            }

            // Refresh UI
            _addonsAPI.RefreshUI();
        }

        private void RegisterSearchButton()
        {
            if (_addonsAPI == null || !_addonsAPI.IsAvailable)
            {
                Logger.Debug("AddonsAPI not available, skipping search button registration");
                return;
            }

            var builder = _addonsAPI.CreateButton(BUTTON_ID_SEARCH);
            if (builder == null)
            {
                Logger.Warn("Failed to create button builder for Search");
                return;
            }

            // Configure button
            builder
                .WithDisplayName("Search")
                .WithDescription("Search the encyclopedia for items, NPCs, and more")
                .WithCategory(KeyCategory.Menu)
                .WithPriority(85)
                .WithCooldown(300)
                .WithOriginalKeybind("LeftShift + F1")
                .WithVisibilityCondition(CanShowSearchButton)
                .OnPressed(OnSearchButtonPressed);

            // Add icon
            if (_searchIcon != null)
            {
                builder.WithIcon(_searchIcon, new Rectangle(0, 0, 16, 16));
            }
            else
            {
                // Gunakan icon dari game
                builder.WithIcon(Game1.mouseCursors, new Rectangle(208, 320, 16, 16));
            }

            // Tint color hijau untuk search theme
            builder.WithTintColor(new Color(60, 150, 80));

            if (builder.Register())
            {
                Logger.Info("✓ Search button registered to AddonsMobile");
            }

            // Refresh UI
            _addonsAPI.RefreshUI();
        }

        // ═══════════════════════════════════════════════════════
        // Visibility Conditions
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Kondisi untuk menampilkan lookup button.
        /// </summary>
        private bool CanShowLookupButton()
        {
            // Harus in-game
            if (!Context.IsWorldReady)
                return false;

            // Tidak dalam event/cutscene
            if (Game1.eventUp)
                return false;

            // Tidak dalam dialog
            if (Game1.dialogueUp)
                return false;

            // Bisa tampil meski ada menu (untuk lookup item di inventory)
            return true;
        }

        /// <summary>
        /// Kondisi untuk menampilkan search button.
        /// </summary>
        private bool CanShowSearchButton()
        {
            // Harus in-game
            if (!Context.IsWorldReady)
                return false;

            // Tidak ada menu aktif (search butuh full screen)
            if (Game1.activeClickableMenu != null)
                return false;

            // Tidak dalam event/cutscene
            if (Game1.eventUp)
                return false;

            return true;
        }

        // ═══════════════════════════════════════════════════════
        // Button Callbacks
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Callback saat lookup button ditekan.
        /// </summary>
        private void OnLookupButtonPressed()
        {
            Logger.Debug("Lookup button pressed");
            TriggerLookup();
        }

        /// <summary>
        /// Callback saat search button ditekan.
        /// </summary>
        private void OnSearchButtonPressed()
        {
            Logger.Debug("Search button pressed");
            TriggerSearch();
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Lookup
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Trigger lookup untuk object di depan player.
        /// </summary>
        public void TriggerLookup()
        {
            if (!IsAvailable || API == null)
            {
                Logger.Warn("Cannot trigger lookup: integration not available");
                return;
            }

            try
            {
                // ignoreCursor = true karena di mobile tidak ada mouse cursor
                bool success = API.ShowLookup(ignoreCursor: true);

                if (success)
                {
                    Logger.Debug("Lookup triggered successfully");
                }
                else
                {
                    Logger.Debug("Lookup triggered but no target found");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error triggering lookup: {ex.Message}");
            }
        }

        /// <summary>
        /// Trigger search menu.
        /// </summary>
        public void TriggerSearch()
        {
            if (!IsAvailable || API == null)
            {
                Logger.Warn("Cannot trigger search: integration not available");
                return;
            }

            try
            {
                if (Config.LookupAnything.UseMobileSearchMenu)
                {
                    OpenMobileSearchMenu();
                }
                else
                {
                    // Gunakan SearchMenu asli dari LookupAnything
                    API.TryToggleSearch();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error triggering search: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Mobile Search Menu
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Buka custom mobile search menu.
        /// </summary>
        public void OpenMobileSearchMenu()
        {
            if (API == null)
            {
                Logger.Debug("Cannot open search menu - API not available");
                return;
            }

            // Dapatkan search subjects dari LookupAnything
            var subjects = API.GetSearchSubjects();
            if (subjects == null || !subjects.Any())
            {
                Logger.Warn("No search subjects available");
                Game1.addHUDMessage(new HUDMessage("No searchable items found", HUDMessage.error_type));
                return;
            }

            Logger.Debug($"Opening mobile search menu with {subjects.Count()} subjects");

            // Buat dan tampilkan mobile search menu
            var menu = new MobileSearchMenu(
                subjects: subjects,
                onSelectSubject: OnSubjectSelected
            );

            Game1.activeClickableMenu = menu;
        }

        /// <summary>
        /// Callback ketika user memilih subject dari search menu.
        /// </summary>
        private void OnSubjectSelected(object subject)
        {
            if (API == null) return;

            Logger.Debug("Subject selected from search menu");

            // Panggil ShowLookupFor dari LookupAnything
            bool success = API.ShowLookupFor(subject);

            if (!success)
            {
                Logger.Warn("Failed to show lookup for selected subject");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Status Checks
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Cek apakah LookupMenu sedang terbuka.
        /// </summary>
        public bool IsLookupMenuOpen()
        {
            return Game1.activeClickableMenu?.GetType().Name == "LookupMenu";
        }

        /// <summary>
        /// Cek apakah SearchMenu sedang terbuka (asli atau custom).
        /// </summary>
        public bool IsSearchMenuOpen()
        {
            var menuName = Game1.activeClickableMenu?.GetType().Name;
            return menuName == "SearchMenu" || menuName == "MobileSearchMenu";
        }

        // ═══════════════════════════════════════════════════════
        // Button Control
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Enable/disable lookup button.
        /// </summary>
        public void SetLookupButtonEnabled(bool enabled)
        {
            _addonsAPI?.SetButtonEnabled(BUTTON_ID_LOOKUP, enabled);
        }

        /// <summary>
        /// Enable/disable search button.
        /// </summary>
        public void SetSearchButtonEnabled(bool enabled)
        {
            _addonsAPI?.SetButtonEnabled(BUTTON_ID_SEARCH, enabled);
        }

        /// <summary>
        /// Check apakah lookup button terdaftar.
        /// </summary>
        public bool IsLookupButtonRegistered()
        {
            return _addonsAPI?.IsButtonRegistered(BUTTON_ID_LOOKUP) ?? false;
        }

        /// <summary>
        /// Check apakah search button terdaftar.
        /// </summary>
        public bool IsSearchButtonRegistered()
        {
            return _addonsAPI?.IsButtonRegistered(BUTTON_ID_SEARCH) ?? false;
        }
    }
}