// File: Integrations/LookupAnything/LookupAnythingAPI.cs

using System.Collections;
using System.Reflection;
using MobileUISupport.Framework;
using StardewModdingAPI;

namespace MobileUISupport.Integrations.LookupAnything
{
    /// <summary>
    /// API untuk berinteraksi dengan mod Lookup Anything.
    /// Menggunakan reflection untuk mengakses internal mod.
    /// </summary>
    public class LookupAnythingAPI
    {
        // ═══════════════════════════════════════════════════════
        // Fields
        // ═══════════════════════════════════════════════════════

        private readonly IMonitor _monitor;
        private readonly IModHelper _helper;

        // Cached mod entry
        private object _modEntry;
        private Type _modEntryType;

        // Cached MethodInfo
        private MethodInfo _showLookupMethod;
        private MethodInfo _hideLookupMethod;
        private MethodInfo _tryToggleSearchMethod;
        private MethodInfo _showLookupForMethod;

        // Cached FieldInfo untuk TargetFactory
        private FieldInfo _targetFactoryField;
        private MethodInfo _getSearchSubjectsMethod;

        // ═══════════════════════════════════════════════════════
        // Properties
        // ═══════════════════════════════════════════════════════

        /// <summary>Apakah API berhasil diinisialisasi.</summary>
        public bool IsValid { get; private set; }

        // ═══════════════════════════════════════════════════════
        // Constructor
        // ═══════════════════════════════════════════════════════

        public LookupAnythingAPI(IMonitor monitor, IModHelper helper)
        {
            _monitor = monitor;
            _helper = helper;

            Initialize();
        }

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        private void Initialize()
        {
            try
            {
                _monitor.Log("Initializing LookupAnything API...", LogLevel.Debug);

                // Get mod entry instance
                _modEntry = GetModEntry();
                if (_modEntry == null)
                {
                    _monitor.Log("Could not get LookupAnything mod entry!", LogLevel.Error);
                    IsValid = false;
                    return;
                }

                _modEntryType = _modEntry.GetType();
                _monitor.Log($"Found ModEntry type: {_modEntryType.FullName}", LogLevel.Debug);

                // Cache reflection info
                if (!CacheReflectionInfo())
                {
                    IsValid = false;
                    return;
                }

                IsValid = true;
                _monitor.Log("LookupAnything API initialized successfully!", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error initializing LookupAnything API: {ex.Message}", LogLevel.Error);
                _monitor.Log(ex.StackTrace ?? "", LogLevel.Debug);
                IsValid = false;
            }
        }

        private object GetModEntry()
        {
            var modInfo = _helper.ModRegistry.Get("Pathoschild.LookupAnything");
            if (modInfo == null)
            {
                _monitor.Log("LookupAnything mod info not found!", LogLevel.Error);
                return null;
            }

            var modInfoType = modInfo.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // Try property "Mod" first (SMAPI's IModInfo wraps the actual mod)
            var modProperty = modInfoType.GetProperty("Mod", flags);
            if (modProperty != null)
            {
                var result = modProperty.GetValue(modInfo);
                if (result != null)
                {
                    _monitor.Log($"Got mod entry via Mod property", LogLevel.Debug);
                    return result;
                }
            }

            // Try field "Mod"
            var modField = modInfoType.GetField("Mod", flags);
            if (modField != null)
            {
                var result = modField.GetValue(modInfo);
                if (result != null)
                {
                    _monitor.Log($"Got mod entry via Mod field", LogLevel.Debug);
                    return result;
                }
            }

            // Try internal SMAPI field
            var internalField = modInfoType.GetField("_mod", flags);
            if (internalField != null)
            {
                var result = internalField.GetValue(modInfo);
                if (result != null)
                {
                    _monitor.Log($"Got mod entry via _mod field", LogLevel.Debug);
                    return result;
                }
            }

            _monitor.Log("Could not access mod entry through any known method", LogLevel.Error);
            return null;
        }

        private bool CacheReflectionInfo()
        {
            if (_modEntryType == null) return false;

            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            try
            {
                // ─────────────────────────────────────────────────────
                // Cache methods dari ModEntry
                // Berdasarkan source code LookupAnything/ModEntry.cs
                // ─────────────────────────────────────────────────────

                // private void ShowLookup(bool ignoreCursor = false)
                _showLookupMethod = _modEntryType.GetMethod("ShowLookup", flags);
                _monitor.Log($"ShowLookup method: {_showLookupMethod != null}", LogLevel.Debug);

                // private void HideLookup()
                _hideLookupMethod = _modEntryType.GetMethod("HideLookup", flags);
                _monitor.Log($"HideLookup method: {_hideLookupMethod != null}", LogLevel.Debug);

                // private void TryToggleSearch()
                _tryToggleSearchMethod = _modEntryType.GetMethod("TryToggleSearch", flags);
                _monitor.Log($"TryToggleSearch method: {_tryToggleSearchMethod != null}", LogLevel.Debug);

                // internal void ShowLookupFor(ISubject subject)
                _showLookupForMethod = _modEntryType.GetMethod("ShowLookupFor", flags);
                _monitor.Log($"ShowLookupFor method: {_showLookupForMethod != null}", LogLevel.Debug);

                // ─────────────────────────────────────────────────────
                // Cache TargetFactory untuk GetSearchSubjects
                // ─────────────────────────────────────────────────────

                // private TargetFactory? TargetFactory
                _targetFactoryField = _modEntryType.GetField("TargetFactory", flags);
                _monitor.Log($"TargetFactory field: {_targetFactoryField != null}", LogLevel.Debug);

                if (_targetFactoryField != null)
                {
                    var targetFactory = _targetFactoryField.GetValue(_modEntry);
                    if (targetFactory != null)
                    {
                        // public IEnumerable<ISubject> GetSearchSubjects()
                        _getSearchSubjectsMethod = targetFactory.GetType()
                            .GetMethod("GetSearchSubjects", BindingFlags.Instance | BindingFlags.Public);
                        _monitor.Log($"GetSearchSubjects method: {_getSearchSubjectsMethod != null}", LogLevel.Debug);
                    }
                }

                // Validate: minimal ShowLookup harus ada
                if (_showLookupMethod == null)
                {
                    _monitor.Log("ShowLookup method not found - critical failure!", LogLevel.Error);
                    return false;
                }

                _monitor.Log("Reflection info cached successfully", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"Error caching reflection info: {ex.Message}", LogLevel.Error);
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Lookup
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Show lookup untuk target saat ini.
        /// Equivalent dengan menekan F1.
        /// </summary>
        /// <param name="ignoreCursor">
        /// true = lookup object di depan player (untuk mobile).
        /// false = lookup object di bawah cursor.
        /// </param>
        public bool ShowLookup(bool ignoreCursor = true)
        {
            if (_modEntry == null || _showLookupMethod == null)
            {
                _monitor.Log("ShowLookup: method not available", LogLevel.Debug);
                return false;
            }

            try
            {
                // Method signature: private void ShowLookup(bool ignoreCursor = false)
                _showLookupMethod.Invoke(_modEntry, new object[] { ignoreCursor });
                _monitor.Log($"ShowLookup invoked (ignoreCursor={ignoreCursor})", LogLevel.Debug);
                return true;
            }
            catch (TargetParameterCountException)
            {
                // Fallback: method mungkin tidak punya parameter
                try
                {
                    _showLookupMethod.Invoke(_modEntry, null);
                    _monitor.Log("ShowLookup invoked (no params fallback)", LogLevel.Debug);
                    return true;
                }
                catch (Exception ex)
                {
                    _monitor.Log($"ShowLookup fallback failed: {ex.Message}", LogLevel.Debug);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _monitor.Log($"ShowLookup failed: {ex.Message}", LogLevel.Debug);
                return false;
            }
        }

        /// <summary>
        /// Hide lookup menu.
        /// </summary>
        public bool HideLookup()
        {
            if (_modEntry == null || _hideLookupMethod == null)
            {
                _monitor.Log("HideLookup: method not available", LogLevel.Debug);
                return false;
            }

            try
            {
                _hideLookupMethod.Invoke(_modEntry, null);
                _monitor.Log("HideLookup invoked", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"HideLookup failed: {ex.Message}", LogLevel.Debug);
                return false;
            }
        }

        /// <summary>
        /// Show lookup untuk subject tertentu.
        /// Ini yang dipanggil ketika user memilih item dari search.
        /// </summary>
        /// <param name="subject">ISubject instance dari GetSearchSubjects().</param>
        public bool ShowLookupFor(object subject)
        {
            if (_modEntry == null || _showLookupForMethod == null)
            {
                _monitor.Log("ShowLookupFor: method not available", LogLevel.Debug);
                return false;
            }

            if (subject == null)
            {
                _monitor.Log("ShowLookupFor: subject is null", LogLevel.Debug);
                return false;
            }

            try
            {
                // Method signature: internal void ShowLookupFor(ISubject subject)
                _showLookupForMethod.Invoke(_modEntry, new[] { subject });
                _monitor.Log("ShowLookupFor invoked", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"ShowLookupFor failed: {ex.Message}", LogLevel.Debug);
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Search
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Toggle search menu asli dari LookupAnything.
        /// Equivalent dengan menekan Shift+F1.
        /// </summary>
        public bool TryToggleSearch()
        {
            if (_modEntry == null || _tryToggleSearchMethod == null)
            {
                _monitor.Log("TryToggleSearch: method not available", LogLevel.Debug);
                return false;
            }

            try
            {
                _tryToggleSearchMethod.Invoke(_modEntry, null);
                _monitor.Log("TryToggleSearch invoked", LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                _monitor.Log($"TryToggleSearch failed: {ex.Message}", LogLevel.Debug);
                return false;
            }
        }

        /// <summary>
        /// Dapatkan semua searchable subjects dari TargetFactory.
        /// Return type asli: IEnumerable&lt;ISubject&gt;
        /// </summary>
        public IEnumerable<object> GetSearchSubjects()
        {
            if (_targetFactoryField == null)
            {
                _monitor.Log("GetSearchSubjects: TargetFactory field not available", LogLevel.Debug);
                return null;
            }

            try
            {
                var targetFactory = _targetFactoryField.GetValue(_modEntry);
                if (targetFactory == null)
                {
                    _monitor.Log("GetSearchSubjects: TargetFactory instance is null", LogLevel.Debug);
                    return null;
                }

                if (_getSearchSubjectsMethod == null)
                {
                    // Try to get method again jika belum di-cache
                    _getSearchSubjectsMethod = targetFactory.GetType()
                        .GetMethod("GetSearchSubjects", BindingFlags.Instance | BindingFlags.Public);

                    if (_getSearchSubjectsMethod == null)
                    {
                        _monitor.Log("GetSearchSubjects: method not found", LogLevel.Debug);
                        return null;
                    }
                }

                var result = _getSearchSubjectsMethod.Invoke(targetFactory, null);
                if (result == null)
                {
                    _monitor.Log("GetSearchSubjects: result is null", LogLevel.Debug);
                    return null;
                }

                // Convert IEnumerable<ISubject> ke IEnumerable<object>
                if (result is IEnumerable enumerable)
                {
                    var list = new List<object>();
                    foreach (var item in enumerable)
                    {
                        if (item != null)
                            list.Add(item);
                    }

                    _monitor.Log($"GetSearchSubjects: found {list.Count} subjects", LogLevel.Debug);
                    return list;
                }

                _monitor.Log("GetSearchSubjects: result is not enumerable", LogLevel.Debug);
                return null;
            }
            catch (Exception ex)
            {
                _monitor.Log($"GetSearchSubjects failed: {ex.Message}", LogLevel.Debug);
                return null;
            }
        }
    }
}