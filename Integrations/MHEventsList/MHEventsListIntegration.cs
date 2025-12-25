using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.AddonsAPI;
using MobileUISupport.Integrations.Base;
using MobileUISupport.UI;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileUISupport.Integrations.MHEventsList
{
    /// <summary>
    /// Integration dengan MH Events List mod.
    /// Register button ke AddonsMobile untuk akses events menu.
    /// </summary>
    public class MHEventsListIntegration : BaseIntegration
    {
        #region ═══ Constants ═══

        private const string ButtonIDEventsMenu = "MobileUISupport.MHEventsList.EventsMenu";
        private const string ButtonIDQuickFilter = "MobileUISupport.MHEventsList.QuickFilter";
        private const string EventsMenuIcon = "assets/EventsListIcon.png";

        #endregion

        #region ═══ Properties - BaseIntegration ═══

        public override string ModId => MHEventsListAPI.ModID;
        public override string DisplayName => "MH Events List";
        public override bool IsEnabled => Config.MHEventsList.EnableSupport;

        #endregion

        #region ═══ Properties - Components ═══

        /// <summary>The MHEventsList API wrapper.</summary>
        public MHEventsListAPI? API { get; private set; }

        // Reference ke AddonsAPI
        private AddonsAPIIntegration? _addonsAPI;

        // Textures
        private Texture2D? _eventsMenuIcon;

        // State
        private EventFilterMode _currentFilterMode = EventFilterMode.Available;
        private string? _currentNpcFilter;
        private int _cachedAvailableCount;
        private DateTime _lastCountUpdate = DateTime.MinValue;

        #endregion

        #region ═══ Initialization ═══

        protected override bool DoInitialize()
        {
            // Create API instance
            API = new MHEventsListAPI(Monitor, Helper);

            Logger.Banner("MH Events List Mobile UI - Created");

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

            // Initialize Magic API (deferred until save loaded)
            bool initialized = API.Initialize();
            Logger.DebugOnly("MHEventsList", $"API initialized: {initialized}");

            if (!initialized)
            {
                Logger.Warn("MHEventsList API initialization failed");
                return;
            }

            // Load icon texture
            LoadButtonIcon();

            // Register button ke AddonsAPI
            RegisterEventsMenuButton();

            // Log statistics
            LogEventStatistics();
        }

        public override void OnReturnedToTitle()
        {
            // Reset state
            _currentFilterMode = EventFilterMode.Available;
            _currentNpcFilter = null;
            _cachedAvailableCount = 0;
            _lastCountUpdate = DateTime.MinValue;

            Logger.Debug("MHEventsList integration reset");
        }

        #endregion

        #region ═══ Button Registration ═══

        private void LoadButtonIcon()
        {
            try
            {
                // Try load custom icon
                _eventsMenuIcon = Helper.ModContent.Load<Texture2D>(EventsMenuIcon);
                Logger.Debug("Loaded custom events menu icon");
            }
            catch
            {
                // Fallback: will use default icon from AddonsAPI
                _eventsMenuIcon = null;
                Logger.Debug("Using default icon for events menu button");
            }
        }

        private void RegisterEventsMenuButton()
        {
            if (_addonsAPI == null || !_addonsAPI.IsAvailable)
            {
                Logger.Debug("AddonsAPI not available, skipping button registration");
                return;
            }

            // Main Events Menu Button
            RegisterMainMenuButton();

            // Optional: Quick Filter Button (cycle through filter modes)
            if (Config.MHEventsList.ShowQuickFilterButton)
            {
                RegisterQuickFilterButton();
            }

            // Refresh UI
            _addonsAPI.RefreshUI();
        }

        private void RegisterMainMenuButton()
        {
            var builder = _addonsAPI!.CreateButton(ButtonIDEventsMenu);

            if (builder == null)
            {
                Logger.Warn("Failed to create button builder for Events Menu");
                return;
            }

            // Configure button
            builder
                .WithDisplayName("Events List")
                .WithDescription("View available story events and their requirements")
                .WithCategory(KeyCategory.Menu)
                .WithPriority(90)
                .WithCooldown(500)
                .WithOriginalKeybind("F7")
                .WithVisibilityCondition(CanShowEventsMenu)
                .OnPressed(OnEventsMenuPressed);

            // Add icon jika tersedia
            if (_eventsMenuIcon != null)
            {
                builder.WithIcon(_eventsMenuIcon, new Rectangle(0, 0, 16, 16));
            }

            // Tint color - menggunakan warna biru untuk events theme
            // builder.WithTintColor(new Color(70, 130, 180)); // Steel Blue

            // Register
            if (builder.Register())
            {
                Logger.Info("✓ Events Menu button registered to AddonsMobile");
            }
        }

        private void RegisterQuickFilterButton()
        {
            var builder = _addonsAPI!.CreateButton(ButtonIDQuickFilter);

            if (builder == null)
            {
                Logger.Warn("Failed to create button builder for Quick Filter");
                return;
            }

            builder
                .WithDisplayName("Event Filter")
                .WithDescription("Cycle through event filter modes")
                .WithCategory(KeyCategory.Tools)
                .WithPriority(89)
                .WithCooldown(300)
                .WithVisibilityCondition(CanShowEventsMenu)
                .OnPressed(OnQuickFilterPressed);

            if (builder.Register())
            {
                Logger.Debug("✓ Quick Filter button registered");
            }
        }

        #endregion

        #region ═══ Visibility & Callbacks ═══

        /// <summary>
        /// Kondisi untuk menampilkan events menu button.
        /// </summary>
        private bool CanShowEventsMenu()
        {
            // Harus in-game
            if (!Context.IsWorldReady)
                return false;

            // Tidak ada menu aktif (kecuali kalau config allow)
            if (Game1.activeClickableMenu != null)
                return false;

            // Tidak dalam event/cutscene
            if (Game1.eventUp)
                return false;

            // Tidak dalam dialogue
            if (Game1.dialogueUp)
                return false;

            // API harus ready
            if (API == null || !API.IsInitialized)
                return false;

            return true;
        }

        /// <summary>
        /// Callback saat Events Menu button ditekan.
        /// </summary>
        private void OnEventsMenuPressed()
        {
            Logger.Debug("Events Menu button pressed");
            OpenEventsMenu();
        }

        /// <summary>
        /// Callback saat Quick Filter button ditekan.
        /// </summary>
        private void OnQuickFilterPressed()
        {
            CycleFilterMode();
            ShowFilterModeNotification();
        }

        #endregion

        #region ═══ Public Methods - Menu ═══

        /// <summary>
        /// Buka events menu.
        /// </summary>
        public void OpenEventsMenu()
        {
            if (!IsAvailable || API == null)
            {
                Logger.Warn("Cannot open events menu: integration not available");
                return;
            }

            if (Game1.activeClickableMenu != null)
            {
                Logger.Debug("Cannot open events menu: another menu is active");
                return;
            }

            try
            {
                if (TryOpenMobileMenu())
                {
                    Logger.Debug("Mobile MH Events menu opened successfully");
                    return;
                }
                // Coba buka menu asli MHEventsList
                bool fallback = API.OpenOriginalMenu();

                if (fallback)
                {
                    Logger.Debug("Events menu opened successfully");
                }
                else
                {
                    Logger.Warn("Failed to open events menu via API");
                    ShowErrorNotification("Failed to open events menu");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error opening events menu: {ex.Message}");
                ShowErrorNotification("Error opening events menu");
            }
        }

        private bool TryOpenMobileMenu()
        {
            try
            {
                Game1.activeClickableMenu = new MobileMHEventsMenu(API!);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Trace($"Mobile menu creation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Buka detail untuk event tertentu.
        /// </summary>
        public void OpenEventDetail(EventDataWrapper evt)
        {
            if (!IsAvailable || API == null || evt == null)
            {
                Logger.Warn("Cannot open event detail: invalid parameters");
                return;
            }

            try
            {
                bool success = API.OpenEventDetail(evt);

                if (!success)
                {
                    Logger.Warn($"Failed to open detail for event: {evt.Id}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error opening event detail: {ex.Message}");
            }
        }

        /// <summary>
        /// Play event langsung.
        /// </summary>
        public bool PlayEvent(EventDataWrapper evt, bool markAsSeen = true)
        {
            if (!IsAvailable || API == null || evt == null)
            {
                Logger.Warn("Cannot play event: invalid parameters");
                return false;
            }

            try
            {
                return API.PlayEvent(evt, markAsSeen);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error playing event: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region ═══ Public Methods - Event Data ═══

        /// <summary>
        /// Get semua events.
        /// </summary>
        public List<EventDataWrapper> GetAllEvents()
        {
            return API?.GetAllEvents() ?? new List<EventDataWrapper>();
        }

        /// <summary>
        /// Get events dengan filter saat ini.
        /// </summary>
        public List<EventDataWrapper> GetFilteredEvents()
        {
            return API?.GetFilteredEvents(
                _currentFilterMode,
                _currentNpcFilter
            ) ?? new List<EventDataWrapper>();
        }

        /// <summary>
        /// Get events dengan filter custom.
        /// </summary>
        public List<EventDataWrapper> GetFilteredEvents(
            EventFilterMode mode,
            string? npcFilter = null,
            string? searchText = null,
            int maxHearts = 14)
        {
            return API?.GetFilteredEvents(mode, npcFilter, searchText, maxHearts)
                   ?? new List<EventDataWrapper>();
        }

        /// <summary>
        /// Get daftar NPC yang punya events.
        /// </summary>
        public List<string> GetNpcListWithEvents()
        {
            return API?.GetNpcListWithEvents() ?? new List<string>();
        }

        /// <summary>
        /// Get jumlah events yang available.
        /// </summary>
        public int GetAvailableEventCount()
        {
            // Cache count untuk 5 detik untuk performance
            if ((DateTime.Now - _lastCountUpdate).TotalSeconds > 5)
            {
                _cachedAvailableCount = GetFilteredEvents(EventFilterMode.Available).Count;
                _lastCountUpdate = DateTime.Now;
            }

            return _cachedAvailableCount;
        }

        #endregion

        #region ═══ Public Methods - Hidden Events ═══

        /// <summary>
        /// Hide event dari daftar.
        /// </summary>
        public bool HideEvent(string eventId)
        {
            return API?.HideEvent(eventId) ?? false;
        }

        /// <summary>
        /// Unhide event.
        /// </summary>
        public bool UnhideEvent(string eventId)
        {
            return API?.UnhideEvent(eventId) ?? false;
        }

        /// <summary>
        /// Get daftar hidden event IDs.
        /// </summary>
        public HashSet<string> GetHiddenEventIds()
        {
            return API?.GetHiddenEventIds() ?? new HashSet<string>();
        }

        #endregion

        #region ═══ Filter Control ═══

        /// <summary>
        /// Set filter mode.
        /// </summary>
        public void SetFilterMode(EventFilterMode mode)
        {
            _currentFilterMode = mode;
            Logger.Debug($"Filter mode set to: {mode}");
        }

        /// <summary>
        /// Get current filter mode.
        /// </summary>
        public EventFilterMode GetFilterMode()
        {
            return _currentFilterMode;
        }

        /// <summary>
        /// Set NPC filter.
        /// </summary>
        public void SetNpcFilter(string? npcName)
        {
            _currentNpcFilter = npcName;
            Logger.Debug($"NPC filter set to: {npcName ?? "None"}");
        }

        /// <summary>
        /// Clear NPC filter.
        /// </summary>
        public void ClearNpcFilter()
        {
            _currentNpcFilter = null;
        }

        /// <summary>
        /// Cycle ke filter mode berikutnya.
        /// </summary>
        public void CycleFilterMode()
        {
            _currentFilterMode = _currentFilterMode switch
            {
                EventFilterMode.Available => EventFilterMode.All,
                EventFilterMode.All => EventFilterMode.Seen,
                EventFilterMode.Seen => EventFilterMode.Hidden,
                EventFilterMode.Hidden => EventFilterMode.ContentPatcher,
                EventFilterMode.ContentPatcher => EventFilterMode.Available,
                _ => EventFilterMode.Available
            };

            Logger.Debug($"Filter mode cycled to: {_currentFilterMode}");
        }

        #endregion

        #region ═══ Button Control ═══

        /// <summary>
        /// Enable/disable events menu button.
        /// </summary>
        public void SetEventsMenuButtonEnabled(bool enabled)
        {
            _addonsAPI?.SetButtonEnabled(ButtonIDEventsMenu, enabled);
        }

        /// <summary>
        /// Check apakah button terdaftar.
        /// </summary>
        public bool IsEventsMenuButtonRegistered()
        {
            return _addonsAPI?.IsButtonRegistered(ButtonIDEventsMenu) ?? false;
        }

        /// <summary>
        /// Unregister semua button.
        /// </summary>
        public void UnregisterAllButtons()
        {
            _addonsAPI?.UnregisterButton(ButtonIDEventsMenu);
            _addonsAPI?.UnregisterButton(ButtonIDQuickFilter);
        }

        #endregion

        #region ═══ Config Access ═══

        /// <summary>
        /// Get config value dari MHEventsList.
        /// </summary>
        public T? GetMHEventsListConfig<T>(string propertyName, T? defaultValue = default)
        {
            return API.GetConfigValue(propertyName, defaultValue) ?? defaultValue;
        }

        /// <summary>
        /// Apakah menggunakan dark theme.
        /// </summary>
        public bool UseDarkTheme => API?.UseDarkTheme ?? false;

        /// <summary>
        /// Apakah mark as seen saat playing.
        /// </summary>
        public bool MarkAsSeenWhenPlaying => API?.MarkAsSeenWhenPlaying ?? true;

        #endregion

        #region ═══ Translation ═══

        /// <summary>
        /// Get translation dari MHEventsList.
        /// </summary>
        public string GetMHTranslation(string key)
        {
            return API?.GetTranslation(key) ?? key;
        }

        #endregion

        #region ═══ Notifications ═══

        private void ShowFilterModeNotification()
        {
            string modeName = _currentFilterMode switch
            {
                EventFilterMode.Available => "Available Events",
                EventFilterMode.All => "All Events",
                EventFilterMode.Seen => "Seen Events",
                EventFilterMode.Hidden => "Hidden Events",
                EventFilterMode.ContentPatcher => "Content Patcher Events",
                _ => "Unknown"
            };

            int count = GetFilteredEvents().Count;

            Game1.addHUDMessage(new HUDMessage($"Filter: {modeName} ({count})", HUDMessage.newQuest_type));
        }

        private void ShowErrorNotification(string message)
        {
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.error_type));
        }

        #endregion

        #region ═══ Diagnostics ═══

        private void LogEventStatistics()
        {
            if (API == null || !API.IsInitialized)
                return;

            var allEvents = API.GetAllEvents();
            var availableEvents = API.GetFilteredEvents(EventFilterMode.Available);
            var seenEvents = API.GetFilteredEvents(EventFilterMode.Seen);
            var hiddenEvents = API.GetHiddenEventIds();

            Logger.Section("MHEventsList Statistics");
            Logger.Debug($"  Total Events    : {allEvents.Count}");
            Logger.Debug($"  Available       : {availableEvents.Count}");
            Logger.Debug($"  Seen            : {seenEvents.Count}");
            Logger.Debug($"  Hidden          : {hiddenEvents.Count}");
            Logger.Debug($"  NPCs with Events: {API.GetNpcListWithEvents().Count}");

            // Top 5 NPCs dengan events terbanyak
            var npcEventCounts = allEvents
                .Where(e => e.RequiredNpcs != null)
                .SelectMany(e => e.RequiredNpcs!)
                .GroupBy(n => n)
                .Select(g => new { Npc = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5);

            Logger.Debug("  Top NPCs:");
            foreach (var npc in npcEventCounts)
            {
                Logger.Debug($"    - {npc.Npc}: {npc.Count} events");
            }
        }

        /// <summary>
        /// Run health check dan log hasil.
        /// </summary>
        public void RunHealthCheck()
        {
            API?.QuickHealthCheck();
        }

        /// <summary>
        /// Run full diagnostic.
        /// </summary>
        public void RunDiagnostic()
        {
            API?.DiagnoseModStructure();
        }

        #endregion
    }
}