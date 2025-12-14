using MobileUISupport.Framework;
using MobileUISupport.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Reflection;

namespace MobileUISupport.Patches
{
    /// <summary>
    /// Intercept SpellMenu dan bisa membuka MobileSpellMenu secara langsung.
    /// </summary>
    public class SpellMenuInterceptor
    {
        // ═══════════════════════════════════════════════════════
        // Fields - Dependencies
        // ═══════════════════════════════════════════════════════

        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;
        private readonly ModConfig _config;
        private readonly MagicStardewAPI _api;

        // ═══════════════════════════════════════════════════════
        // Fields - SpellMenu Reflection Cache
        // ═══════════════════════════════════════════════════════

        private Type? _spellMenuType;
        private ConstructorInfo? _spellMenuConstructor;
        private FieldInfo? _spellsField;
        private FieldInfo? _playerField;
        private FieldInfo? _manaBarField;
        private FieldInfo? _favoritesField;
        private FieldInfo? _configField;

        // ═══════════════════════════════════════════════════════
        // Fields - Magic Stardew Reflection Cache
        // ═══════════════════════════════════════════════════════

        private object? _magicModEntry;
        private Type? _magicModEntryType;

        // SpellManager instance dan fields
        private object? _spellManager;
        private Type? _spellManagerType;
        private FieldInfo? _spellManager_AllSpellsField;
        private FieldInfo? _spellManager_ManaBarField;
        private FieldInfo? _spellManager_HelperField;
        private FieldInfo? _spellManager_ConfigField;
        private MethodInfo? _spellManager_AnimateBookOpeningMethod;
        private MethodInfo? _spellManager_GetAllSpellsMethod;

        // ═══════════════════════════════════════════════════════
        // Fields - State
        // ═══════════════════════════════════════════════════════

        private bool _isReplacingMenu = false;
        private bool _isSetupComplete = false;

        // ═══════════════════════════════════════════════════════
        // Constructor
        // ═══════════════════════════════════════════════════════

        public SpellMenuInterceptor(IMonitor monitor, IModHelper helper, ModConfig config, MagicStardewAPI api)
        {
            _monitor = monitor;
            _helper = helper;
            _config = config;
            _api = api;
        }

        // ═══════════════════════════════════════════════════════
        // Setup
        // ═══════════════════════════════════════════════════════

        public bool Setup()
        {
            try
            {
                _monitor.Log("Setting up SpellMenu interceptor...", LogLevel.Debug);

                // 1. Find SpellMenu type
                _spellMenuType = FindType("SpellMenu");
                if (_spellMenuType == null)
                {
                    _monitor.Log("Could not find SpellMenu type!", LogLevel.Error);
                    return false;
                }
                _monitor.Log($"Found SpellMenu: {_spellMenuType.FullName}", LogLevel.Debug);

                // 2. Find SpellManager type
                _spellManagerType = FindType("SpellManager");
                if (_spellManagerType == null)
                {
                    _monitor.Log("Could not find SpellManager type!", LogLevel.Warn);
                }
                else
                {
                    _monitor.Log($"Found SpellManager: {_spellManagerType.FullName}", LogLevel.Debug);
                }

                // 3. Cache SpellMenu fields
                CacheSpellMenuFields();

                // 4. Cache Magic ModEntry dan SpellManager
                CacheMagicModEntry();

                // 5. Register event handler
                _helper.Events.Display.MenuChanged += OnMenuChanged;

                _isSetupComplete = true;
                _monitor.Log("SpellMenu interceptor setup complete!", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in Setup: {ex.Message}", LogLevel.Error);
                _monitor.Log(ex.StackTrace ?? "", LogLevel.Debug);
                return false;
            }
        }

        private Type? FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                if (name?.Contains("MagicStardew", StringComparison.OrdinalIgnoreCase) != true)
                    continue;

                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == typeName)
                        {
                            return type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip
                }
            }
            return null;
        }

        private void CacheSpellMenuFields()
        {
            if (_spellMenuType == null) return;

            var privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

            _spellsField = _spellMenuType.GetField("_spells", privateInstance);
            _playerField = _spellMenuType.GetField("_player", privateInstance);
            _manaBarField = _spellMenuType.GetField("_manaBar", privateInstance);
            _favoritesField = _spellMenuType.GetField("_favorites", privateInstance);
            _configField = _spellMenuType.GetField("_config", privateInstance);

            // Get constructor: SpellMenu(List<Spell>, Farmer, ManaBar, IModHelper, ModConfig, bool)
            _spellMenuConstructor = _spellMenuType.GetConstructors().FirstOrDefault();

            _monitor.Log($"SpellMenu fields: spells={_spellsField != null}, player={_playerField != null}, " +
                        $"manaBar={_manaBarField != null}, constructor={_spellMenuConstructor != null}", LogLevel.Debug);
        }

        private void CacheMagicModEntry()
        {
            try
            {
                var modInfo = _helper.ModRegistry.Get("Zexu2K.MagicStardew.C");
                if (modInfo == null)
                {
                    _monitor.Log("Magic Stardew mod not found", LogLevel.Warn);
                    return;
                }

                // Get Mod instance
                var modProperty = modInfo.GetType().GetProperty("Mod",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _magicModEntry = modProperty?.GetValue(modInfo);

                if (_magicModEntry == null)
                {
                    _monitor.Log("Could not get Magic ModEntry instance", LogLevel.Warn);
                    return;
                }

                _magicModEntryType = _magicModEntry.GetType();
                _monitor.Log($"Got Magic ModEntry: {_magicModEntryType.FullName}", LogLevel.Debug);

                // Find SpellManager in ModEntry
                FindSpellManager();
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error caching Magic ModEntry: {ex.Message}", LogLevel.Error);
            }
        }

        private void FindSpellManager()
        {
            if (_magicModEntry == null || _magicModEntryType == null)
                return;

            var allFlags = BindingFlags.Public | BindingFlags.NonPublic |
                          BindingFlags.Static | BindingFlags.Instance;

            _monitor.Log("=== Searching for SpellManager in ModEntry ===", LogLevel.Debug);

            // Search for SpellManager field/property
            foreach (var field in _magicModEntryType.GetFields(allFlags))
            {
                if (field.FieldType.Name == "SpellManager" ||
                    field.Name.Contains("SpellManager", StringComparison.OrdinalIgnoreCase) ||
                    field.Name.Contains("spellManager", StringComparison.OrdinalIgnoreCase))
                {
                    _monitor.Log($"Found SpellManager field: {field.Name}", LogLevel.Debug);
                    _spellManager = field.GetValue(field.IsStatic ? null : _magicModEntry);

                    if (_spellManager != null)
                    {
                        _monitor.Log($"Got SpellManager instance: {_spellManager.GetType().Name}", LogLevel.Debug);
                        CacheSpellManagerFields();
                        return;
                    }
                }
            }

            foreach (var prop in _magicModEntryType.GetProperties(allFlags))
            {
                if (prop.PropertyType.Name == "SpellManager" ||
                    prop.Name.Contains("SpellManager", StringComparison.OrdinalIgnoreCase))
                {
                    _monitor.Log($"Found SpellManager property: {prop.Name}", LogLevel.Debug);
                    bool isStatic = prop.GetMethod?.IsStatic ?? false;
                    _spellManager = prop.GetValue(isStatic ? null : _magicModEntry);

                    if (_spellManager != null)
                    {
                        _monitor.Log($"Got SpellManager instance: {_spellManager.GetType().Name}", LogLevel.Debug);
                        CacheSpellManagerFields();
                        return;
                    }
                }
            }

            // Log all fields for debugging
            _monitor.Log("SpellManager not found. Logging all fields:", LogLevel.Debug);
            foreach (var field in _magicModEntryType.GetFields(allFlags))
            {
                string staticStr = field.IsStatic ? "[S]" : "[I]";
                _monitor.Log($"  {staticStr} {field.Name} : {field.FieldType.Name}", LogLevel.Debug);
            }
        }

        private void CacheSpellManagerFields()
        {
            if (_spellManager == null)
                return;

            var smType = _spellManager.GetType();
            var privateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
            var publicInstance = BindingFlags.Public | BindingFlags.Instance;

            _monitor.Log("=== Caching SpellManager fields ===", LogLevel.Debug);

            // allSpells (private readonly List<Spell>)
            _spellManager_AllSpellsField = smType.GetField("allSpells", privateInstance);
            _monitor.Log($"  allSpells: {_spellManager_AllSpellsField != null}", LogLevel.Debug);

            // manaBar (public readonly ManaBar)
            _spellManager_ManaBarField = smType.GetField("manaBar", publicInstance);
            _monitor.Log($"  manaBar: {_spellManager_ManaBarField != null}", LogLevel.Debug);

            // helper (private readonly IModHelper)
            _spellManager_HelperField = smType.GetField("helper", privateInstance);
            _monitor.Log($"  helper: {_spellManager_HelperField != null}", LogLevel.Debug);

            // config (private ModConfig)
            _spellManager_ConfigField = smType.GetField("config", privateInstance);
            _monitor.Log($"  config: {_spellManager_ConfigField != null}", LogLevel.Debug);

            // AnimateBookOpening method
            _spellManager_AnimateBookOpeningMethod = smType.GetMethod("AnimateBookOpening", publicInstance);
            _monitor.Log($"  AnimateBookOpening: {_spellManager_AnimateBookOpeningMethod != null}", LogLevel.Debug);

            // GetAllSpells method
            _spellManager_GetAllSpellsMethod = smType.GetMethod("GetAllSpells", publicInstance);
            _monitor.Log($"  GetAllSpells: {_spellManager_GetAllSpellsMethod != null}", LogLevel.Debug);
        }

        // ═══════════════════════════════════════════════════════
        // Public - Open Mobile Spell Menu
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Buka MobileSpellMenu secara programmatic.
        /// Prioritas: Construct SpellMenu → AnimateBookOpening → Direct Open
        /// </summary>
        public bool OpenMobileSpellMenu()
        {
            _monitor.Log("═══════════════════════════════════════════", LogLevel.Debug);
            _monitor.Log("OpenMobileSpellMenu() called", LogLevel.Debug);

            if (!CanOpenMenu())
            {
                return false;
            }

            //// Strategy 1: Construct SpellMenu langsung (akan di-intercept, TANPA delay animasi)
            //// SpellMenu constructor memanggil ResortSpells() sehingga urutan spell benar
            //bool constructSuccess = TryConstructSpellMenu();
            //if (constructSuccess)
            //{
            //    return true;
            //}

            // Strategy 2: Call AnimateBookOpening (ada delay 750ms untuk animasi)
            _monitor.Log("Construction failed, trying AnimateBookOpening...", LogLevel.Debug);
            bool animateSuccess = TryCallAnimateBookOpening();
            if (animateSuccess)
            {
                return true;
            }

            // Strategy 3: Direct open sebagai fallback terakhir (urutan mungkin berbeda)
            _monitor.Log("AnimateBookOpening failed, trying direct open as last resort...", LogLevel.Debug);
            return TryOpenDirect();
        }

        private bool CanOpenMenu()
        {
            if (!_isSetupComplete)
            {
                _monitor.Log("Setup not complete", LogLevel.Warn);
                return false;
            }

            if (!Context.IsWorldReady)
            {
                _monitor.Log("World not ready", LogLevel.Debug);
                return false;
            }

            if (Game1.activeClickableMenu != null)
            {
                _monitor.Log($"Menu already open: {Game1.activeClickableMenu.GetType().Name}", LogLevel.Debug);
                return false;
            }

            if (Game1.eventUp || Game1.dialogueUp)
            {
                _monitor.Log("In event or dialogue", LogLevel.Debug);
                return false;
            }

            if (!_api.IsInitialized)
            {
                _monitor.Log("MagicStardewAPI not initialized", LogLevel.Warn);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Direct open as last fallback.
        /// Note: Spell order might differ from original SpellMenu.
        /// </summary>
        private bool TryOpenDirect()
        {
            try
            {
                _monitor.Log("Attempting direct menu open (fallback)...", LogLevel.Debug);

                if (_spellManager == null)
                {
                    _monitor.Log("SpellManager is null", LogLevel.Debug);
                    return false;
                }

                // Get data
                object? manaBar = _spellManager_ManaBarField?.GetValue(_spellManager);
                object? spellsEnumerable = _spellManager_GetAllSpellsMethod?.Invoke(_spellManager, null);
                object? spellList = ConvertToList(spellsEnumerable);
                object? magicConfig = _spellManager_ConfigField?.GetValue(_spellManager);

                if (manaBar == null || spellList == null)
                {
                    _monitor.Log("Missing required data", LogLevel.Debug);
                    return false;
                }

                // Convert spells
                var player = Game1.player;
                var spellDataList = _api.GetAllSpells(spellList, player);

                if (spellDataList.Count == 0)
                {
                    _monitor.Log("No spells found", LogLevel.Warn);
                    return false;
                }

                // Sort spells to match SpellMenu behavior
                SortSpellsLikeOriginal(spellDataList, player);

                // Get favorites
                var favorites = ExtractFavorites(magicConfig, spellDataList);

                // Create mobile menu
                _isReplacingMenu = true;

                var mobileMenu = new MobileSpellMenu(
                    spells: spellDataList,
                    player: player,
                    manaBar: manaBar,
                    favorites: favorites,
                    helper: _helper,
                    monitor: _monitor,
                    config: _config,
                    api: _api
                );

                Game1.activeClickableMenu = mobileMenu;
                _isReplacingMenu = false;

                _monitor.Log($"✓ MobileSpellMenu opened directly with {spellDataList.Count} spells", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Direct open failed: {ex.Message}", LogLevel.Debug);
                _isReplacingMenu = false;
                return false;
            }
        }

        /// <summary>
        /// Try to call SpellManager.AnimateBookOpening() to open SpellMenu.
        /// Note: This has a 750ms delay for animation before menu opens.
        /// </summary>
        private bool TryCallAnimateBookOpening()
        {
            try
            {
                if (_spellManager == null || _spellManager_AnimateBookOpeningMethod == null)
                {
                    _monitor.Log("SpellManager or AnimateBookOpening method not available", LogLevel.Debug);
                    return false;
                }

                _monitor.Log("Calling SpellManager.AnimateBookOpening (750ms delay)...", LogLevel.Debug);

                // AnimateBookOpening(Farmer who, bool showMenu, bool openedWithGamepad = false)
                var parameters = _spellManager_AnimateBookOpeningMethod.GetParameters();

                object?[] args;
                if (parameters.Length >= 3)
                {
                    args = new object?[] { Game1.player, true, false };
                }
                else if (parameters.Length == 2)
                {
                    args = new object?[] { Game1.player, true };
                }
                else
                {
                    args = new object?[] { Game1.player };
                }

                _spellManager_AnimateBookOpeningMethod.Invoke(_spellManager, args);
                _monitor.Log("✓ AnimateBookOpening called (menu opens after animation)", LogLevel.Debug);

                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"AnimateBookOpening failed: {ex.Message}", LogLevel.Debug);
                return false;
            }
        }

        /// <summary>
        /// Try to construct SpellMenu directly.
        /// SpellMenu constructor akan memanggil ResortSpells() sehingga urutan spell benar.
        /// Menu ini kemudian akan di-intercept oleh OnMenuChanged.
        /// </summary>
        private bool TryConstructSpellMenu()
        {
            try
            {
                if (_spellMenuConstructor == null)
                {
                    _monitor.Log("SpellMenu constructor not found", LogLevel.Debug);
                    return false;
                }

                if (_spellManager == null)
                {
                    _monitor.Log("SpellManager is null", LogLevel.Debug);
                    return false;
                }

                _monitor.Log("Constructing SpellMenu (will be intercepted)...", LogLevel.Debug);

                // Get required data from SpellManager
                object? manaBar = _spellManager_ManaBarField?.GetValue(_spellManager);
                object? magicHelper = _spellManager_HelperField?.GetValue(_spellManager);
                object? magicConfig = _spellManager_ConfigField?.GetValue(_spellManager);

                // Get spells via GetAllSpells() and convert to List
                object? spellsEnumerable = _spellManager_GetAllSpellsMethod?.Invoke(_spellManager, null);
                object? spellList = ConvertToList(spellsEnumerable);

                _monitor.Log($"  Data: spells={spellList != null}, manaBar={manaBar != null}, " +
                            $"helper={magicHelper != null}, config={magicConfig != null}", LogLevel.Debug);

                if (spellList == null || manaBar == null || magicHelper == null || magicConfig == null)
                {
                    _monitor.Log("Missing required data for SpellMenu construction", LogLevel.Debug);
                    return false;
                }

                // Check constructor parameters
                var parameters = _spellMenuConstructor.GetParameters();
                _monitor.Log($"  Constructor has {parameters.Length} parameters", LogLevel.Debug);

                // Build arguments based on constructor signature
                // SpellMenu(List<Spell> spells, Farmer player, ManaBar manaBar, IModHelper helper, ModConfig config, bool openedWithGamepad)
                object?[] args = new object?[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    var paramName = param.Name?.ToLower() ?? "";
                    var paramType = param.ParameterType;

                    if (paramType.Name.Contains("List") || paramName.Contains("spell"))
                    {
                        args[i] = spellList;
                    }
                    else if (paramType == typeof(Farmer) || paramName.Contains("player"))
                    {
                        args[i] = Game1.player;
                    }
                    else if (paramType.Name == "ManaBar" || paramName.Contains("mana"))
                    {
                        args[i] = manaBar;
                    }
                    else if (paramType == typeof(IModHelper) || paramName.Contains("helper"))
                    {
                        args[i] = magicHelper;
                    }
                    else if (paramType.Name == "ModConfig" || paramName.Contains("config"))
                    {
                        args[i] = magicConfig;
                    }
                    else if (paramType == typeof(bool))
                    {
                        args[i] = false; // openedWithGamepad = false
                    }
                    else
                    {
                        args[i] = param.HasDefaultValue ? param.DefaultValue : null;
                    }
                }

                // Construct SpellMenu - this triggers ResortSpells() internally
                var menu = _spellMenuConstructor.Invoke(args) as IClickableMenu;

                if (menu != null)
                {
                    // Set as active menu - OnMenuChanged will intercept and replace with MobileSpellMenu
                    Game1.activeClickableMenu = menu;
                    _monitor.Log("✓ SpellMenu constructed (will be replaced by MobileSpellMenu)", LogLevel.Debug);
                    return true;
                }

                _monitor.Log("SpellMenu construction returned null", LogLevel.Warn);
                return false;
            }
            catch (Exception ex)
            {
                _monitor.Log($"SpellMenu construction failed: {ex.Message}", LogLevel.Debug);
                _monitor.Log(ex.StackTrace ?? "", LogLevel.Trace);
                return false;
            }
        }

        /// <summary>
        /// Convert IEnumerable to List using reflection.
        /// </summary>
        private object? ConvertToList(object? enumerable)
        {
            if (enumerable == null)
                return null;

            try
            {
                var enumerableType = enumerable.GetType();
                var genericArgs = enumerableType.GetGenericArguments();

                if (genericArgs.Length == 0)
                {
                    // Try to get from interfaces
                    var iEnumerableInterface = enumerableType.GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                    if (iEnumerableInterface != null)
                    {
                        genericArgs = iEnumerableInterface.GetGenericArguments();
                    }
                }

                if (genericArgs.Length == 0)
                {
                    _monitor.Log("Could not determine generic type for ToList", LogLevel.Debug);
                    return null;
                }

                var elementType = genericArgs[0];
                var toListMethod = typeof(Enumerable)
                    .GetMethod("ToList")?
                    .MakeGenericMethod(elementType);

                return toListMethod?.Invoke(null, new[] { enumerable });
            }
            catch (Exception ex)
            {
                _monitor.Log($"ConvertToList error: {ex.Message}", LogLevel.Debug);
                return null;
            }
        }

        /// <summary>
        /// Sort spells to match SpellMenu.ResortSpells() behavior.
        /// Order: Favorites first → Unlocked → Alphabetical
        /// </summary>
        private void SortSpellsLikeOriginal(List<SpellData> spells, Farmer player)
        {
            spells.Sort((a, b) =>
            {
                bool aIsFavorite = a.FavoriteSlot > 0;
                bool bIsFavorite = b.FavoriteSlot > 0;

                // Favorites first
                if (aIsFavorite && !bIsFavorite) return -1;
                if (!aIsFavorite && bIsFavorite) return 1;

                // Then unlocked spells
                if (a.IsUnlocked && !b.IsUnlocked) return -1;
                if (!a.IsUnlocked && b.IsUnlocked) return 1;

                // Finally alphabetical
                return string.Compare(a.Name, b.Name, StringComparison.Ordinal);
            });
        }

        // ═══════════════════════════════════════════════════════
        // Data Helpers
        // ═══════════════════════════════════════════════════════

        private Dictionary<int, SpellData> ExtractFavorites(object? magicConfig, List<SpellData> allSpells)
        {
            var result = new Dictionary<int, SpellData>();

            try
            {
                if (magicConfig == null)
                    return result;

                // Get FavoriteSpells from config (Dictionary<int, int>)
                var favoritesProp = magicConfig.GetType().GetProperty("FavoriteSpells");
                var favoritesField = magicConfig.GetType().GetField("FavoriteSpells");

                var favoritesRaw = favoritesProp?.GetValue(magicConfig) ?? favoritesField?.GetValue(magicConfig);

                if (favoritesRaw is System.Collections.IDictionary dict)
                {
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        if (entry.Key is int slotNum && entry.Value is int iconIndex)
                        {
                            var spell = allSpells.FirstOrDefault(s => s.IconIndex == iconIndex);
                            if (spell != null)
                            {
                                result[slotNum] = spell;
                                spell.FavoriteSlot = slotNum;
                            }
                        }
                    }
                }

                _monitor.Log($"Extracted {result.Count} favorites from config", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                _monitor.Log($"ExtractFavorites error: {ex.Message}", LogLevel.Debug);
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════
        // Event Handler - Intercept Original SpellMenu
        // ═══════════════════════════════════════════════════════

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (_isReplacingMenu)
                return;

            if (e.NewMenu == null)
                return;

            if (e.NewMenu is MobileSpellMenu)
                return;

            if (_spellMenuType != null && e.NewMenu.GetType() == _spellMenuType)
            {
                _monitor.Log("SpellMenu detected! Replacing with MobileSpellMenu...", LogLevel.Debug);
                ReplaceWithMobileMenu(e.NewMenu);
            }
        }

        private void ReplaceWithMobileMenu(IClickableMenu originalMenu)
        {
            try
            {
                _isReplacingMenu = true;

                // Extract data from original SpellMenu
                var spellList = _spellsField?.GetValue(originalMenu);
                var player = _playerField?.GetValue(originalMenu) as Farmer;
                var manaBar = _manaBarField?.GetValue(originalMenu);
                var favoritesRaw = _favoritesField?.GetValue(originalMenu);

                _monitor.Log($"Extracted from SpellMenu: spells={spellList != null}, " +
                            $"player={player != null}, manaBar={manaBar != null}", LogLevel.Debug);

                if (spellList == null || player == null || manaBar == null)
                {
                    _monitor.Log("Failed to extract required data!", LogLevel.Error);
                    _isReplacingMenu = false;
                    return;
                }

                // Convert spells
                var spellDataList = _api.GetAllSpells(spellList, player);
                _monitor.Log($"Converted {spellDataList.Count} spells", LogLevel.Debug);

                if (spellDataList.Count == 0)
                {
                    _monitor.Log("No spells found! Keeping original menu.", LogLevel.Warn);
                    _isReplacingMenu = false;
                    return;
                }

                // Convert favorites (Dictionary<int, Spell>)
                var favorites = ConvertFavorites(favoritesRaw, spellDataList);

                // Create MobileSpellMenu
                var mobileMenu = new MobileSpellMenu(
                    spells: spellDataList,
                    player: player,
                    manaBar: manaBar,
                    favorites: favorites,
                    helper: _helper,
                    monitor: _monitor,
                    config: _config,
                    api: _api
                );

                Game1.activeClickableMenu = mobileMenu;
                _monitor.Log($"✓ MobileSpellMenu opened with {spellDataList.Count} spells", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error replacing menu: {ex.Message}", LogLevel.Error);
                _monitor.Log(ex.StackTrace ?? "", LogLevel.Debug);
            }
            finally
            {
                _isReplacingMenu = false;
            }
        }

        private Dictionary<int, SpellData> ConvertFavorites(object? favoritesRaw, List<SpellData> allSpells)
        {
            var result = new Dictionary<int, SpellData>();

            try
            {
                if (favoritesRaw == null)
                    return result;

                // favoritesRaw is Dictionary<int, Spell>
                if (favoritesRaw is System.Collections.IDictionary dict)
                {
                    foreach (System.Collections.DictionaryEntry entry in dict)
                    {
                        if (entry.Key is int slotNum && entry.Value != null)
                        {
                            var iconIndexProp = entry.Value.GetType().GetProperty("IconIndex");
                            if (iconIndexProp != null)
                            {
                                int iconIndex = (int)(iconIndexProp.GetValue(entry.Value) ?? -1);
                                var spellData = allSpells.FirstOrDefault(s => s.IconIndex == iconIndex);
                                if (spellData != null)
                                {
                                    result[slotNum] = spellData;
                                    spellData.FavoriteSlot = slotNum;
                                }
                            }
                        }
                    }
                }

                _monitor.Log($"Converted {result.Count} favorites", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                _monitor.Log($"ConvertFavorites error: {ex.Message}", LogLevel.Debug);
            }

            return result;
        }

        // ═══════════════════════════════════════════════════════
        // Cleanup
        // ═══════════════════════════════════════════════════════

        public void Dispose()
        {
            _helper.Events.Display.MenuChanged -= OnMenuChanged;
        }
    }
}