using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;

namespace MobileUISupport.Integrations.MagicStardew
{
    /// <summary>
    /// API untuk berinteraksi dengan mod Magic Stardew
    /// Menggunakan reflection untuk mengakses internal mod
    /// </summary>
    public class MagicStardewAPI
    {
        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;

        // Cached types dari Magic Stardew
        private Assembly? _magicAssembly;
        private Type? _spellType;
        private Type? _manaBarType;
        private Type? _modEntryType;

        // Cached instances
        private object? _manaBarInstance;

        // Cached textures
        public Texture2D? SpellIconsTexture { get; private set; }
        public Texture2D? SpellBorderTexture { get; private set; }
        public Texture2D? LevelFramesTexture { get; private set; }
        public Texture2D? LockTexture { get; private set; }

        // Spell list
        private List<object>? _originalSpells;
        private PropertyInfo? _spellNameProperty;
        private PropertyInfo? _spellDescriptionProperty;
        private PropertyInfo? _spellIconIndexProperty;
        private PropertyInfo? _spellUnlockHintProperty;
        private MethodInfo? _spellGetManaCostMethod;
        private MethodInfo? _spellIsUnlockedMethod;
        private MethodInfo? _spellGetLevelMethod;
        private MethodInfo? _spellCastMethod;
        private MethodInfo? _spellInUseMethod;
        private MethodInfo? _spellCanCastMethod;

        // Mana bar methods
        private MethodInfo? _getCurrentManaMethod;
        private MethodInfo? _useManaMethod;

        public bool IsInitialized { get; private set; } = false;

        public MagicStardewAPI(IMonitor monitor, IModHelper helper)
        {
            _monitor = monitor;
            _helper = helper;
        }

        /// <summary>
        /// Initialize API - harus dipanggil setelah game launched
        /// </summary>
        public bool Initialize()
        {
            try
            {
                _monitor.Log("Initializing MagicStardew API...", LogLevel.Debug);

                // Find Magic Stardew assembly
                _magicAssembly = FindMagicAssembly();
                if (_magicAssembly == null)
                {
                    _monitor.Log("Could not find MagicStardew assembly!", LogLevel.Error);
                    return false;
                }

                // Find required types
                if (!FindTypes())
                {
                    return false;
                }

                // Cache method/property info
                if (!CacheReflectionInfo())
                {
                    return false;
                }

                // Load textures
                LoadTextures();

                // Get spell list and mana bar from ModEntry
                if (!GetModEntryData())
                {
                    return false;
                }

                IsInitialized = true;
                _monitor.Log("MagicStardew API initialized successfully!", LogLevel.Info);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error initializing MagicStardew API: {ex.Message}", LogLevel.Error);
                _monitor.Log(ex.StackTrace ?? "", LogLevel.Debug);
                return false;
            }
        }

        private Assembly? FindMagicAssembly()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = assembly.GetName().Name;
                if (name?.Contains("MagicStardew", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _monitor.Log($"Found MagicStardew assembly: {name}", LogLevel.Debug);
                    return assembly;
                }
            }
            return null;
        }

        private bool FindTypes()
        {
            if (_magicAssembly == null) return false;

            foreach (var type in _magicAssembly.GetTypes())
            {
                switch (type.Name)
                {
                    case "Spell":
                        _spellType = type;
                        _monitor.Log($"Found Spell type: {type.FullName}", LogLevel.Debug);
                        break;
                    case "ManaBar":
                        _manaBarType = type;
                        _monitor.Log($"Found ManaBar type: {type.FullName}", LogLevel.Debug);
                        break;
                    case "ModEntry":
                        _modEntryType = type;
                        _monitor.Log($"Found ModEntry type: {type.FullName}", LogLevel.Debug);
                        break;
                }
            }

            if (_spellType == null || _manaBarType == null || _modEntryType == null)
            {
                _monitor.Log("Could not find all required types!", LogLevel.Error);
                return false;
            }

            return true;
        }

        private bool CacheReflectionInfo()
        {
            if (_spellType == null || _manaBarType == null) return false;

            try
            {
                // Spell properties
                _spellNameProperty = _spellType.GetProperty("Name");
                _spellDescriptionProperty = _spellType.GetProperty("Description");
                _spellIconIndexProperty = _spellType.GetProperty("IconIndex");
                _spellUnlockHintProperty = _spellType.GetProperty("UnlockHint");

                // Spell methods
                _spellGetManaCostMethod = _spellType.GetMethod("GetEffectiveManaCost");
                _spellIsUnlockedMethod = _spellType.GetMethod("IsUnlockedFor");
                _spellGetLevelMethod = _spellType.GetMethod("GetLevel");
                _spellCastMethod = _spellType.GetMethod("Cast");
                _spellInUseMethod = _spellType.GetMethod("InUse");

                // ManaBar methods
                _getCurrentManaMethod = _manaBarType.GetMethod("GetCurrentMana");
                _useManaMethod = _manaBarType.GetMethod("UseMana");

                _monitor.Log("Reflection info cached successfully", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error caching reflection info: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        private void LoadTextures()
        {
            try
            {
                // Get textures from ModEntry static fields
                if (_modEntryType != null)
                {
                    var spellIconsField = _modEntryType.GetField("SpellIcons",
                        BindingFlags.Public | BindingFlags.Static);
                    var borderField = _modEntryType.GetField("SpellBorder",
                        BindingFlags.Public | BindingFlags.Static);

                    if (spellIconsField != null)
                    {
                        SpellIconsTexture = spellIconsField.GetValue(null) as Texture2D;
                        _monitor.Log("Loaded SpellIcons texture", LogLevel.Debug);
                    }

                    if (borderField != null)
                    {
                        SpellBorderTexture = borderField.GetValue(null) as Texture2D;
                        _monitor.Log("Loaded SpellBorder texture", LogLevel.Debug);
                    }
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error loading textures: {ex.Message}", LogLevel.Warn);
            }
        }

        private bool GetModEntryData()
        {
            // Ini akan dipanggil saat menu dibuka untuk mendapatkan data terbaru
            return true;
        }

        /// <summary>
        /// Mendapatkan list semua spell sebagai SpellData
        /// </summary>
        public List<SpellData> GetAllSpells(object spellListFromMenu, Farmer player)
        {
            var result = new List<SpellData>();

            try
            {
                if (spellListFromMenu is not System.Collections.IList spellList)
                {
                    _monitor.Log("spellListFromMenu is not a list!", LogLevel.Error);
                    return result;
                }

                foreach (var originalSpell in spellList)
                {
                    var spellData = ConvertToSpellData(originalSpell, player);
                    if (spellData != null)
                    {
                        result.Add(spellData);
                    }
                }

                _monitor.Log($"Converted {result.Count} spells", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error getting spells: {ex.Message}", LogLevel.Error);
            }

            return result;
        }

        private SpellData? ConvertToSpellData(object originalSpell, Farmer player)
        {
            try
            {
                var spellData = new SpellData
                {
                    OriginalSpell = originalSpell,
                    Name = _spellNameProperty?.GetValue(originalSpell) as string ?? "Unknown",
                    Description = _spellDescriptionProperty?.GetValue(originalSpell) as string ?? "",
                    IconIndex = (int?)_spellIconIndexProperty?.GetValue(originalSpell) ?? 0,
                    UnlockHint = _spellUnlockHintProperty?.GetValue(originalSpell) as string ?? ""
                };

                // Get mana cost
                if (_spellGetManaCostMethod != null)
                {
                    spellData.ManaCost = (int?)_spellGetManaCostMethod.Invoke(originalSpell, new object[] { player }) ?? 0;
                }

                // Check if unlocked
                if (_spellIsUnlockedMethod != null)
                {
                    spellData.IsUnlocked = (bool?)_spellIsUnlockedMethod.Invoke(originalSpell, new object[] { player }) ?? false;
                }

                // Get level
                if (_spellGetLevelMethod != null)
                {
                    spellData.Level = (int?)_spellGetLevelMethod.Invoke(originalSpell, new object[] { player }) ?? -1;
                }

                // Check visibility (unlocked or dev mode)
                var devModeField = _modEntryType?.GetField("DevMode", BindingFlags.Public | BindingFlags.Static);
                bool devMode = (bool?)devModeField?.GetValue(null) ?? false;
                spellData.IsVisible = spellData.IsUnlocked || devMode;

                return spellData;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error converting spell: {ex.Message}", LogLevel.Debug);
                return null;
            }
        }

        /// <summary>
        /// Cast sebuah spell
        /// </summary>
        public bool CastSpell(SpellData spell, Farmer player, object manaBar)
        {
            try
            {
                if (spell.OriginalSpell == null)
                {
                    _monitor.Log("Original spell reference is null!", LogLevel.Error);
                    return false;
                }

                // Check mana
                int currentMana = GetCurrentMana(manaBar, player);
                if (currentMana < spell.ManaCost)
                {
                    _monitor.Log("Not enough mana!", LogLevel.Debug);
                    return false;
                }

                // Use mana
                UseMana(manaBar, player, spell.ManaCost);

                // Cast spell
                _spellCastMethod?.Invoke(spell.OriginalSpell, new object[] { player });

                _monitor.Log($"Casted spell: {spell.Name}", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error casting spell: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        /// <summary>
        /// Get current mana
        /// </summary>
        public int GetCurrentMana(object manaBar, Farmer player)
        {
            try
            {
                return (int?)_getCurrentManaMethod?.Invoke(manaBar, new object[] { player }) ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Use mana
        /// </summary>
        public void UseMana(object manaBar, Farmer player, int amount)
        {
            try
            {
                _useManaMethod?.Invoke(manaBar, new object[] { player, amount });
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error using mana: {ex.Message}", LogLevel.Error);
            }
        }
    }
}