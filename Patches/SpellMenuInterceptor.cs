using HarmonyLib;
using MobileUISupport;
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
    /// Intercept SpellMenu menggunakan SMAPI Events (lebih aman dari Harmony)
    /// </summary>
    public class SpellMenuInterceptor
    {
        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;
        private readonly ModConfig _config;
        private readonly MagicStardewAPI _api;

        // Info Refleksi yang di simpan dalam cache
        private Type? _spellMenuType;
        private FieldInfo? _spellsField;
        private FieldInfo? _playerField;
        private FieldInfo? _manaBarField;
        private FieldInfo? _favoritesField;
        private FieldInfo? _configField;

        // Flag untuk mencegah infinite loop
        private bool _isReplacingMenu = false;

        public SpellMenuInterceptor(IMonitor monitor, IModHelper helper, ModConfig config, MagicStardewAPI api)
        {
            _monitor = monitor;
            _helper = helper;
            _config = config;
            _api = api;
            
        }

        /// <summary>
        /// SETUP EVENTS LISTENER
        /// </summary>
        public bool Setup()
        {
            try
            {
                // Find SpellMenu type
                _spellMenuType = FindSpellMenuType();
                if (_spellMenuType == null)
                {
                    _monitor.Log("Could not find SpellMenu type!", LogLevel.Error);
                    return false;
                }

                _monitor.Log($"Found SpellMenu: {_spellMenuType.FullName}", LogLevel.Debug);

                // Cache field info
                CacheFieldInfo();

                // Register event handler
                _helper.Events.Display.MenuChanged += OnMenuChanged;

                _monitor.Log("SpellMenu interceptor setup complete!", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error in Setup: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private Type? FindSpellMenuType()
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
                        if (type.Name == "SpellMenu" && typeof(IClickableMenu).IsAssignableFrom(type))
                        {
                            return type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that fail to load types
                }
            }
            return null;
        }

        private void CacheFieldInfo()
        {
            if (_spellMenuType == null) return;

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;

            _spellsField = _spellMenuType.GetField("_spells", flags);
            _playerField = _spellMenuType.GetField("_player", flags);
            _manaBarField = _spellMenuType.GetField("_manaBar", flags);
            _favoritesField = _spellMenuType.GetField("_favorites", flags);
            _configField = _spellMenuType.GetField("_config", flags);

            _monitor.Log($"Field cache: spells={_spellsField != null}, player={_playerField != null}, " +
                        $"manaBar={_manaBarField != null}, favorites={_favoritesField != null}", LogLevel.Debug);
        }

        /// <summary>
        /// EVENTS HANDLER
        /// </summary>
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            // Skip jika sedang dalam proses replace
            if (_isReplacingMenu)
                return;

            // Skip jika menu baru adalah null
            if (e.NewMenu == null)
                return;

            // Skip jika menu baru adalah MobileSpellMenu kita
            if (e.NewMenu is MobileSpellMenu)
                return;

            // Cek apakah menu baru adalah SpellMenu dari Magic Stardew
            if (_spellMenuType != null && e.NewMenu.GetType() == _spellMenuType)
            {
                _monitor.Log("SpellMenu detected! Replacing with MobileSpellMenu...", LogLevel.Debug);
                ReplaceWithMobileMenu(e.NewMenu);
            }
        }

        /// <summary>
        /// Ganti SpellMenu dengan MobileSpellMenu
        /// </summary>
        private void ReplaceWithMobileMenu(IClickableMenu originalMenu)
        {
            try
            {
                _isReplacingMenu = true;

                // Extract data dari SpellMenu asli
                var spellList = _spellsField?.GetValue(originalMenu);
                var player = _playerField?.GetValue(originalMenu) as Farmer;
                var manaBar = _manaBarField?.GetValue(originalMenu);
                var favoritesRaw = _favoritesField?.GetValue(originalMenu);
                var magicConfig = _configField?.GetValue(originalMenu);

                if (spellList == null || player == null || manaBar == null)
                {
                    _monitor.Log("Failed to extract required data from SpellMenu!", LogLevel.Error);
                    _monitor.Log($"  spellList: {spellList != null}, player: {player != null}, manaBar: {manaBar != null}", LogLevel.Debug);
                    _isReplacingMenu = false;
                    return;
                }

                // Convert spells ke SpellData
                var spellDataList = _api.GetAllSpells(spellList, player);
                _monitor.Log($"Converted {spellDataList.Count} spells", LogLevel.Debug);

                if (spellDataList.Count == 0)
                {
                    _monitor.Log("No spells found! Keeping original menu.", LogLevel.Warn);
                    _isReplacingMenu = false;
                    return;
                }

                // Convert favorites
                var favorites = ConvertFavorites(favoritesRaw, spellDataList);

                // Buat MobileSpellMenu
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

                // Ganti menu aktif
                Game1.activeClickableMenu = mobileMenu;

                _monitor.Log($"MobileSpellMenu opened with {spellDataList.Count} spells", LogLevel.Debug);
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

        /// <summary>
        /// Convert favorites dari format Magic Stardew ke format kita
        /// </summary>
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
                            // Get IconIndex dari original Spell
                            var iconIndexProp = entry.Value.GetType().GetProperty("IconIndex");
                            if (iconIndexProp != null)
                            {
                                int iconIndex = (int)(iconIndexProp.GetValue(entry.Value) ?? -1);

                                // Find matching SpellData
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
                _monitor.Log($"Error converting favorites: {ex.Message}", LogLevel.Debug);
            }

            return result;
        }

        /// <summary>
        /// Cleanup saat mod unload
        /// </summary>
        public void Dispose()
        {
            _helper.Events.Display.MenuChanged -= OnMenuChanged;
        }
    }
}