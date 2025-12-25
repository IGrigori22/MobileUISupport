using MobileUISupport.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MobileUISupport.Integrations.MHEventsList
{
    /// <summary>
    /// Reflection-based API wrapper untuk MHEventsList mod.
    /// Menyediakan akses penuh tanpa compile-time dependency.
    /// </summary>
    public class MHEventsListAPI
    {
        #region ═══ Constants ═══

        public const string ModID = "mahsouto.MHEventsList";

        #endregion

        #region ═══ Dependencies ═══

        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;

        #endregion

        #region ═══ Cached Types ═══

        private Type? _mhEventsListModType;
        private Type? _eventRegistryType;
        private Type? _eventDataType;
        private Type? _eventDetailMenuType;
        private Type? _mhEventsMenuType;
        private Type? _i18nResolverType;

        #endregion

        #region ═══ Cached Instances ═══

        private object? _modEntry;
        private object? _modConfig;
        private object? _modHelper;
        private object? _i18n;
        private object? _contentPatcher;

        #endregion

        #region ═══ Cached Methods ═══

        private MethodInfo? _eventRegistryInitialize;
        private MethodInfo? _eventRegistryGetAllEvents;
        private MethodInfo? _i18nResolveTokens;
        private MethodInfo? _i18nCleanupTokens;
        private PropertyInfo? _eventsToUnmarkProperty;

        #endregion

        #region ═══ State ═══

        private bool _isInitialized;
        private string? _modVersion;
        private Assembly? _mhEventsAssembly;

        #endregion

        #region ═══ Properties ═══

        public bool IsInitialized => _isInitialized;
        public string ModVersion => _modVersion ?? "Unknown";

        // Config properties with fallback defaults
        public bool UseDarkTheme => GetConfigValue("UseDarkTheme", false);
        public bool MarkAsSeenWhenPlaying => GetConfigValue("MarkAsSeenWhenPlaying", true);
        public bool ShowGoToLocationButton => GetConfigValue("ShowGoToLocationButton", true);

        #endregion

        #region ═══ Constructor ═══

        public MHEventsListAPI(IMonitor monitor, IModHelper helper)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
            _helper = helper ?? throw new ArgumentNullException(nameof(helper));
        }

        #endregion

        #region ═══ Initialization ═══

        /// <summary>
        /// Initialize the API by discovering and caching all required types and methods.
        /// </summary>
        public bool Initialize()
        {
            try
            {
                Logger.Debug("Initializing MHEventsList API...");

                // Step 1: Check mod installed
                var modInfo = _helper.ModRegistry.Get(ModID);
                if (modInfo == null)
                {
                    Logger.Error("MHEventsList mod not installed");
                    return false;
                }

                _modVersion = modInfo.Manifest.Version.ToString();
                Logger.Info($"Found MHEventsList v{_modVersion}");

                // Step 2: Get ModEntry instance
                if (!GetModEntryInstance(modInfo))
                {
                    Logger.Error("Could not get ModEntry instance");
                    return false;
                }

                // Step 3: Find assembly and cache types
                if (!FindAssemblyAndTypes())
                {
                    Logger.Error("Could not find MHEventsList types");
                    return false;
                }

                // Step 4: Cache static references
                CacheStaticReferences();

                // Step 5: Cache methods
                if (!CacheMethods())
                {
                    Logger.Error("Could not cache required methods");
                    return false;
                }

                // Step 6: Initialize EventRegistry
                InitializeEventRegistry();

                _isInitialized = true;
                Logger.Info("✓ MHEventsList API initialized successfully!");

                // Debug diagnostics
                if (ModServices.Config?.DebugMode == true)
                {
                    LogStatus();
                    QuickHealthCheck();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"MHEventsList API initialization failed: {ex.Message}");
                Logger.Trace($"Stack: {ex.StackTrace}");

                _isInitialized = false;
                return false;
            }
        }

        private bool GetModEntryInstance(IModInfo modInfo)
        {
            try
            {
                var modProperty = modInfo.GetType().GetProperty("Mod",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                _modEntry = modProperty?.GetValue(modInfo);

                if (_modEntry != null)
                {
                    Logger.Debug($"Got ModEntry: {_modEntry.GetType().FullName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Trace($"GetModEntryInstance error: {ex.Message}");
                return false;
            }
        }

        private bool FindAssemblyAndTypes()
        {
            try
            {
                // Find assembly in loaded assemblies
                _mhEventsAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.FullName?.Contains("MHEventsList") == true);

                // Fallback: get from ModEntry
                _mhEventsAssembly ??= _modEntry?.GetType().Assembly;

                if (_mhEventsAssembly == null)
                {
                    Logger.Error("Could not find MHEventsList assembly");
                    return false;
                }

                Logger.Debug($"Found assembly: {_mhEventsAssembly.GetName().Name}");

                // Cache types
                _mhEventsListModType = _mhEventsAssembly.GetType("MHEventsList.MHEventsListMod");
                _eventRegistryType = _mhEventsAssembly.GetType("MHEventsList.Core.EventRegistry");
                _eventDataType = _mhEventsAssembly.GetType("MHEventsList.Core.EventData");
                _eventDetailMenuType = _mhEventsAssembly.GetType("MHEventsList.UI.EventDetailMenu");
                _mhEventsMenuType = _mhEventsAssembly.GetType("MHEventsList.UI.MHEventsMenu");
                _i18nResolverType = _mhEventsAssembly.GetType("MHEventsList.Integration.I18nResolver");

                // Verify required types
                bool hasRequiredTypes = _eventRegistryType != null && _eventDataType != null;

                Logger.Debug($"Types - EventRegistry: {_eventRegistryType != null}, " +
                            $"EventData: {_eventDataType != null}");

                return hasRequiredTypes;
            }
            catch (Exception ex)
            {
                Logger.Error($"FindAssemblyAndTypes error: {ex.Message}");
                return false;
            }
        }

        private void CacheStaticReferences()
        {
            try
            {
                if (_mhEventsListModType == null) return;

                var flags = BindingFlags.Public | BindingFlags.Static;

                _modConfig = _mhEventsListModType.GetProperty("Config", flags)?.GetValue(null);
                _modHelper = _mhEventsListModType.GetProperty("Helper", flags)?.GetValue(null);
                _i18n = _mhEventsListModType.GetProperty("I18n", flags)?.GetValue(null);
                _contentPatcher = _mhEventsListModType.GetProperty("ContentPatcher", flags)?.GetValue(null);
                _eventsToUnmarkProperty = _mhEventsListModType.GetProperty("EventsToUnmark", flags);

                Logger.Debug($"Static refs - Config: {_modConfig != null}, I18n: {_i18n != null}");
            }
            catch (Exception ex)
            {
                Logger.Trace($"CacheStaticReferences error: {ex.Message}");
            }
        }

        private bool CacheMethods()
        {
            try
            {
                var staticFlags = BindingFlags.Public | BindingFlags.Static;

                // EventRegistry methods
                if (_eventRegistryType != null)
                {
                    _eventRegistryInitialize = _eventRegistryType.GetMethod("Initialize", staticFlags);
                    _eventRegistryGetAllEvents = _eventRegistryType.GetMethod("GetAllEvents", staticFlags);
                }

                // I18nResolver methods
                if (_i18nResolverType != null)
                {
                    _i18nResolveTokens = _i18nResolverType.GetMethod("ResolveI18nTokens", staticFlags);
                    _i18nCleanupTokens = _i18nResolverType.GetMethod("CleanupUnresolvedTokens", staticFlags);
                }

                return _eventRegistryGetAllEvents != null;
            }
            catch (Exception ex)
            {
                Logger.Trace($"CacheMethods error: {ex.Message}");
                return false;
            }
        }

        private void InitializeEventRegistry()
        {
            try
            {
                _eventRegistryInitialize?.Invoke(null, null);
                Logger.Debug("EventRegistry.Initialize() called");
            }
            catch (Exception ex)
            {
                Logger.Warn($"InitializeEventRegistry error: {ex.Message}");
            }
        }

        #endregion

        #region ═══ Event Data Access ═══

        /// <summary>Get all events as wrapped objects.</summary>
        public List<EventDataWrapper> GetAllEvents()
        {
            try
            {
                if (_eventRegistryGetAllEvents == null)
                {
                    Logger.Warn("GetAllEvents: Method not cached");
                    return new List<EventDataWrapper>();
                }

                var result = _eventRegistryGetAllEvents.Invoke(null, null);

                if (result is System.Collections.IEnumerable enumerable)
                    return EventDataWrapper.WrapCollection(enumerable);

                return new List<EventDataWrapper>();
            }
            catch (Exception ex)
            {
                Logger.Error($"GetAllEvents error: {ex.Message}");
                return new List<EventDataWrapper>();
            }
        }

        /// <summary>Get filtered events based on criteria.</summary>
        public List<EventDataWrapper> GetFilteredEvents(
            EventFilterMode mode,
            string? npcFilter = null,
            string? searchText = null,
            int maxHearts = 14)
        {
            try
            {
                var allEvents = GetAllEvents();
                var hiddenEvents = GetHiddenEventIds();
                var result = new List<EventDataWrapper>();

                foreach (var evt in allEvents)
                {
                    // Apply mode filter
                    if (!PassesModeFilter(evt, mode, hiddenEvents))
                        continue;

                    // Apply NPC filter
                    if (!PassesNpcFilter(evt, npcFilter, maxHearts))
                        continue;

                    // Apply search filter
                    if (!PassesSearchFilter(evt, searchText))
                        continue;

                    result.Add(evt);
                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"GetFilteredEvents error: {ex.Message}");
                return new List<EventDataWrapper>();
            }
        }

        private bool PassesModeFilter(EventDataWrapper evt, EventFilterMode mode, HashSet<string> hiddenEvents)
        {
            bool isHidden = hiddenEvents.Contains(evt.Id);
            bool isSeen = Game1.player.eventsSeen.Contains(evt.Id);

            return mode switch
            {
                EventFilterMode.Available => !isHidden && !isSeen && !evt.HasInvalidScript && evt.AreConditionsMet(),
                EventFilterMode.Hidden => isHidden,
                EventFilterMode.Seen => isSeen,
                EventFilterMode.ContentPatcher => evt.IsFromContentPatcher,
                EventFilterMode.All => true,
                _ => true
            };
        }

        private bool PassesNpcFilter(EventDataWrapper evt, string? npcFilter, int maxHearts)
        {
            if (string.IsNullOrEmpty(npcFilter))
                return true;

            bool hasNpc = evt.RequiredNpcs?.Any(n =>
                n.Equals(npcFilter, StringComparison.OrdinalIgnoreCase)) ?? false;

            if (!hasNpc) return false;

            // Check hearts requirement
            var heartReq = evt.HeartRequirements?.FirstOrDefault(h =>
                h.NpcName.Equals(npcFilter, StringComparison.OrdinalIgnoreCase));

            return heartReq == null || heartReq.Hearts <= maxHearts;
        }

        private bool PassesSearchFilter(EventDataWrapper evt, string? searchText)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;

            string search = searchText.ToLowerInvariant();

            return evt.Id.ToLowerInvariant().Contains(search) ||
                   evt.GetTranslatedLocation().ToLowerInvariant().Contains(search) ||
                   (evt.RequiredNpcs?.Any(n => n.ToLowerInvariant().Contains(search)) ?? false) ||
                   (evt.ModName?.ToLowerInvariant().Contains(search) ?? false);
        }

        /// <summary>Get list of NPCs that have events.</summary>
        public List<string> GetNpcListWithEvents()
        {
            try
            {
                var npcSet = new HashSet<string>();

                foreach (var evt in GetAllEvents())
                {
                    if (evt.RequiredNpcs == null) continue;

                    foreach (var npc in evt.RequiredNpcs)
                    {
                        var npcObj = Game1.getCharacterFromName(npc);
                        string displayName = npcObj?.displayName ?? npc;

                        if (!string.IsNullOrWhiteSpace(displayName) && !displayName.StartsWith("???"))
                            npcSet.Add(displayName);
                    }
                }

                return npcSet.OrderBy(n => n).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"GetNpcListWithEvents error: {ex.Message}");
                return new List<string>();
            }
        }

        #endregion

        #region ═══ Hidden Events Management ═══

        public HashSet<string> GetHiddenEventIds()
        {
            try
            {
                var hiddenList = _helper.Data.ReadSaveData<List<string>>("HiddenEvents");
                return hiddenList != null ? new HashSet<string>(hiddenList) : new HashSet<string>();
            }
            catch
            {
                return new HashSet<string>();
            }
        }

        public bool HideEvent(string eventId)
        {
            try
            {
                var hidden = GetHiddenEventIds();
                if (hidden.Add(eventId))
                {
                    SaveHiddenEvents(hidden);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"HideEvent error: {ex.Message}");
                return false;
            }
        }

        public bool UnhideEvent(string eventId)
        {
            try
            {
                var hidden = GetHiddenEventIds();
                if (hidden.Remove(eventId))
                {
                    SaveHiddenEvents(hidden);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"UnhideEvent error: {ex.Message}");
                return false;
            }
        }

        private void SaveHiddenEvents(HashSet<string> hiddenIds)
        {
            try
            {
                _helper.Data.WriteSaveData("HiddenEvents", hiddenIds.ToList());
            }
            catch (Exception ex)
            {
                Logger.Error($"SaveHiddenEvents error: {ex.Message}");
            }
        }

        #endregion

        #region ═══ Event Playback ═══

        /// <summary>Play an event with full handling (warp, script resolution, etc).</summary>
        public bool PlayEvent(EventDataWrapper evt, bool markAsSeen = true)
        {
            try
            {
                if (evt == null)
                {
                    Logger.Warn("PlayEvent: Event is null");
                    return false;
                }

                Logger.Debug($"PlayEvent: {evt.Id} at {evt.LocationName}");

                // Track for unmark if needed
                if (!markAsSeen)
                    AddEventToUnmark(evt.Id);

                // Remove from seen list temporarily
                Game1.player.eventsSeen.Remove(evt.Id);

                // Get event script
                string? script = GetEventScript(evt);
                if (string.IsNullOrEmpty(script))
                {
                    Logger.Warn($"PlayEvent: No script found for {evt.Id}");
                    Game1.addHUDMessage(new HUDMessage("Event script not found", HUDMessage.error_type));
                    return false;
                }

                // Determine and warp to location
                var (location, locationName) = GetTargetLocation(evt);
                WarpIfNeeded(location, locationName);

                // Play event with delay
                string finalScript = script;
                string eventId = evt.Id;

                Game1.delayedActions.Add(new DelayedAction(300, () =>
                {
                    try
                    {
                        Game1.currentLocation?.startEvent(new Event(finalScript, null, eventId));
                        Logger.Debug($"PlayEvent: Started {eventId}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"PlayEvent delayed error: {ex.Message}");
                    }
                }));

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"PlayEvent error: {ex.Message}");
                Game1.addHUDMessage(new HUDMessage("Failed to play event", HUDMessage.error_type));
                return false;
            }
        }

        private (GameLocation? location, string name) GetTargetLocation(EventDataWrapper evt)
        {
            var location = Game1.getLocationFromName(evt.LocationName);

            if (location != null)
                return (location, evt.LocationName);

            // Fallback to current location
            return (Game1.currentLocation ?? Game1.getLocationFromName("Farm"),
                    Game1.currentLocation?.Name ?? "Farm");
        }

        private void WarpIfNeeded(GameLocation? location, string locationName)
        {
            if (Game1.currentLocation?.Name == locationName || location == null)
                return;

            int warpX = location.warps.Count > 0 ? location.warps[0].X : Game1.player.TilePoint.X;
            int warpY = location.warps.Count > 0 ? location.warps[0].Y - 1 : Game1.player.TilePoint.Y;

            Game1.warpFarmer(locationName, warpX, warpY, false);
        }

        /// <summary>Get event script with token resolution.</summary>
        public string? GetEventScript(EventDataWrapper evt)
        {
            try
            {
                string? script = null;

                // Try get from location data first
                var location = Game1.getLocationFromName(evt.LocationName);
                if (location != null && location.TryGetLocationEvents(out var _, out var events))
                {
                    foreach (var kvp in events)
                    {
                        if (kvp.Key == evt.Id || kvp.Key.StartsWith(evt.Id + "/"))
                        {
                            script = kvp.Value;
                            break;
                        }
                    }
                }

                // Fallback to EventData method
                script ??= evt.GetEventScript();

                // Resolve tokens if present
                if (!string.IsNullOrEmpty(script) && script.Contains("{{"))
                    script = ResolveEventTokens(script, evt);

                return script;
            }
            catch (Exception ex)
            {
                Logger.Error($"GetEventScript error: {ex.Message}");
                return null;
            }
        }

        private string ResolveEventTokens(string script, EventDataWrapper evt)
        {
            try
            {
                // Try I18n resolution
                if (!string.IsNullOrEmpty(evt.ModFolderPath) && _i18nResolveTokens != null)
                {
                    var result = _i18nResolveTokens.Invoke(null, new object[] { script, evt.ModFolderPath });
                    if (result is string resolved)
                        script = resolved;
                }

                // Try Content Patcher resolution
                if (script.Contains("{{") && _contentPatcher != null)
                {
                    script = TryResolveContentPatcherTokens(script, evt.ModUniqueId);
                }

                // Cleanup unresolved tokens
                if (script.Contains("{{") && _i18nCleanupTokens != null)
                {
                    var result = _i18nCleanupTokens.Invoke(null, new object[] { script });
                    if (result is string cleaned)
                        script = cleaned;
                }

                return script;
            }
            catch (Exception ex)
            {
                Logger.Trace($"ResolveEventTokens error: {ex.Message}");
                return script;
            }
        }

        private string TryResolveContentPatcherTokens(string script, string? modUniqueId)
        {
            try
            {
                if (_contentPatcher == null) return script;

                var cpType = _contentPatcher.GetType();
                var isReadyProp = cpType.GetProperty("IsReady");

                if (isReadyProp?.GetValue(_contentPatcher) is not true)
                    return script;

                var resolveMethod = cpType.GetMethod("ResolveTokens");
                if (resolveMethod != null)
                {
                    var result = resolveMethod.Invoke(_contentPatcher,
                        new object[] { script, modUniqueId ?? "" });
                    if (result is string resolved)
                        return resolved;
                }
            }
            catch { /* Ignore */ }

            return script;
        }

        private void AddEventToUnmark(string eventId)
        {
            try
            {
                if (_eventsToUnmarkProperty != null)
                {
                    var list = _eventsToUnmarkProperty.GetValue(null) as HashSet<string>;
                    list?.Add(eventId);
                }
            }
            catch (Exception ex)
            {
                Logger.Trace($"AddEventToUnmark error: {ex.Message}");
            }
        }

        #endregion

        #region ═══ Config & Translation Access ═══

        public T? GetConfigValue<T>(string propertyName, T? defaultValue = default)
        {
            try
            {
                if (_modConfig == null) return defaultValue;

                var prop = _modConfig.GetType().GetProperty(propertyName,
                    BindingFlags.Instance | BindingFlags.Public);

                if (prop != null)
                {
                    var value = prop.GetValue(_modConfig);
                    if (value is T typedValue)
                        return typedValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Trace($"GetConfigValue error: {ex.Message}");
            }

            return defaultValue;
        }

        public string GetTranslation(string key)
        {
            try
            {
                if (_i18n == null) return key;

                var getMethod = _i18n.GetType().GetMethod("Get", new[] { typeof(string) });
                if (getMethod != null)
                {
                    var result = getMethod.Invoke(_i18n, new object[] { key });
                    return result?.ToString() ?? key;
                }
            }
            catch (Exception ex)
            {
                Logger.Trace($"GetTranslation error: {ex.Message}");
            }

            return key;
        }

        #endregion

        #region ═══ Menu Management ═══

        /// <summary>Open the original MHEventsMenu.</summary>
        public bool OpenOriginalMenu()
        {
            try
            {
                if (_mhEventsMenuType == null)
                {
                    Logger.Warn("OpenOriginalMenu: Menu type not found");
                    return false;
                }

                var menu = Activator.CreateInstance(_mhEventsMenuType);
                if (menu is IClickableMenu clickableMenu)
                {
                    Game1.activeClickableMenu = clickableMenu;
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"OpenOriginalMenu error: {ex.Message}");
                return false;
            }
        }

        /// <summary>Open EventDetailMenu for a specific event.</summary>
        public bool OpenEventDetail(EventDataWrapper evt, IClickableMenu? parent = null)
        {
            try
            {
                if (_eventDetailMenuType == null || _eventDataType == null)
                {
                    Logger.Warn("OpenEventDetail: Required types not found");
                    return false;
                }

                object? menu = TryCreateEventDetailMenu(evt, parent);

                if (menu is IClickableMenu clickableMenu)
                {
                    Game1.activeClickableMenu = clickableMenu;
                    return true;
                }

                Logger.Warn("OpenEventDetail: Could not create menu instance");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"OpenEventDetail error: {ex.Message}");
                return false;
            }
        }

        private object? TryCreateEventDetailMenu(EventDataWrapper evt, IClickableMenu? parent)
        {
            // Strategy 1: (EventData, MHEventsMenu) constructor
            if (_mhEventsMenuType != null)
            {
                var ctor = _eventDetailMenuType!.GetConstructor(
                    new Type[] { _eventDataType!, _mhEventsMenuType });

                if (ctor != null)
                {
                    try { return ctor.Invoke(new object?[] { evt.Instance, parent }); }
                    catch { /* Try next */ }
                }
            }

            // Strategy 2: (EventData) constructor only
            var ctor1 = _eventDetailMenuType!.GetConstructor(new Type[] { _eventDataType! });
            if (ctor1 != null)
            {
                try { return ctor1.Invoke(new object?[] { evt.Instance }); }
                catch { /* Try next */ }
            }

            // Strategy 3: Find any compatible constructor
            foreach (var ctor in _eventDetailMenuType!.GetConstructors())
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length >= 1 && parameters[0].ParameterType == _eventDataType)
                {
                    try
                    {
                        var args = new object?[parameters.Length];
                        args[0] = evt.Instance;

                        for (int i = 1; i < parameters.Length; i++)
                        {
                            args[i] = parameters[i].ParameterType == _mhEventsMenuType ? parent : null;
                        }

                        return ctor.Invoke(args);
                    }
                    catch { continue; }
                }
            }

            return null;
        }

        #endregion

        #region ═══ Diagnostics ═══

        public void LogStatus()
        {
            Logger.Section("MHEventsList API Status");
            Logger.Debug($"  Version           : {_modVersion}");
            Logger.Debug($"  Assembly          : {_mhEventsAssembly?.GetName().Name ?? "NULL"}");
            Logger.Debug($"  ModEntry          : {(_modEntry != null ? "✓" : "✗")}");
            Logger.Debug($"  EventRegistry     : {(_eventRegistryType != null ? "✓" : "✗")}");
            Logger.Debug($"  EventData         : {(_eventDataType != null ? "✓" : "✗")}");
            Logger.Debug($"  MHEventsMenu      : {(_mhEventsMenuType != null ? "✓" : "✗")}");
            Logger.Debug($"  EventDetailMenu   : {(_eventDetailMenuType != null ? "✓" : "✗")}");
            Logger.Debug($"  ModConfig         : {(_modConfig != null ? "✓" : "✗")}");
            Logger.Debug($"  I18n              : {(_i18n != null ? "✓" : "✗")}");
            Logger.Debug($"  Total Events      : {GetAllEvents().Count}");
        }

        public void QuickHealthCheck()
        {
            Logger.Section("MHEventsList Health Check");

            var checks = new (string Name, bool Status)[]
            {
                ("Mod Installed", _helper.ModRegistry.IsLoaded(ModID)),
                ("Assembly Found", _mhEventsAssembly != null),
                ("ModEntry", _modEntry != null),
                ("EventRegistry Type", _eventRegistryType != null),
                ("GetAllEvents Method", _eventRegistryGetAllEvents != null),
                ("Events Available", TryGetEvents()),
                ("ModConfig", _modConfig != null),
                ("I18n", _i18n != null),
            };

            int failedCount = 0;
            foreach (var (name, status) in checks)
            {
                var icon = status ? "✅" : "❌";
                Logger.Debug($"  {icon} {name}");
                if (!status) failedCount++;
            }

            if (failedCount > 0)
                Logger.Warn($"⚠️ {failedCount} checks failed!");
            else
                Logger.Info("✅ All checks passed!");
        }

        private bool TryGetEvents()
        {
            try { return GetAllEvents().Count > 0; }
            catch { return false; }
        }

        public void DiagnoseModStructure()
        {
            Logger.Section("DIAGNOSTIC: MHEventsList Structure");

            // List assemblies
            Logger.Debug("\n📦 Loaded MH Assemblies:");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.FullName?.Contains("MH", StringComparison.OrdinalIgnoreCase) == true))
            {
                Logger.Debug($"  • {assembly.GetName().Name} v{assembly.GetName().Version}");
            }

            // List types if assembly found
            if (_mhEventsAssembly != null)
            {
                Logger.Debug($"\n📦 Types in {_mhEventsAssembly.GetName().Name}:");
                try
                {
                    foreach (var type in _mhEventsAssembly.GetExportedTypes())
                    {
                        Logger.Debug($"  • {type.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug($"  Error: {ex.Message}");
                }
            }

            Logger.Section("END DIAGNOSTIC");
        }

        #endregion
    }

    #region ═══ Supporting Types ═══

    /// <summary>Event filter modes.</summary>
    public enum EventFilterMode
    {
        /// <summary>Events that are available (not seen, not hidden, conditions met).</summary>
        Available = 0,

        /// <summary>Events hidden by user.</summary>
        Hidden = 1,

        /// <summary>All events regardless of status.</summary>
        All = 2,

        /// <summary>Events that have been seen.</summary>
        Seen = 3,

        /// <summary>Events from Content Patcher mods.</summary>
        ContentPatcher = 4
    }

    #endregion
}