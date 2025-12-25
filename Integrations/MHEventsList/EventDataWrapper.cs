using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MobileUISupport.Integrations.MHEventsList
{
    /// <summary>
    /// Wrapper class untuk EventData dari MHEventsList.
    /// Mengakses semua properties via reflection tanpa compile-time dependency.
    /// </summary>
    public class EventDataWrapper
    {
        #region ═══ Fields ═══

        private readonly object _instance;
        private readonly Type _type;

        // Static cache - shared across all instances untuk performance
        private static readonly Dictionary<string, PropertyInfo?> _propertyCache = new();
        private static readonly Dictionary<string, MethodInfo?> _methodCache = new();
        private static bool _cacheInitialized = false;
        private static readonly object _cacheLock = new();

        #endregion

        #region ═══ Constructor ═══

        public EventDataWrapper(object eventDataInstance)
        {
            _instance = eventDataInstance ?? throw new ArgumentNullException(nameof(eventDataInstance));
            _type = eventDataInstance.GetType();

            InitializeCacheIfNeeded();
        }

        private void InitializeCacheIfNeeded()
        {
            if (_cacheInitialized) return;

            lock (_cacheLock)
            {
                if (_cacheInitialized) return;

                var flags = BindingFlags.Instance | BindingFlags.Public;

                // Cache properties
                string[] properties = {
                    "Id", "LocationName", "ModName", "ModUniqueId", "ModFolderPath",
                    "RequiredNpcs", "HeartRequirements", "HasInvalidScript",
                    "IsFromContentPatcher", "IsDisabledByCP"
                };

                foreach (var propName in properties)
                {
                    _propertyCache[propName] = _type.GetProperty(propName, flags);
                }

                // Cache methods
                string[] methods = {
                    "GetTranslatedLocation", "GetConditionsSummary",
                    "GetEventScript", "AreConditionsMet"
                };

                foreach (var methodName in methods)
                {
                    _methodCache[methodName] = _type.GetMethod(methodName, flags);
                }

                _cacheInitialized = true;
            }
        }

        #endregion

        #region ═══ Properties ═══

        /// <summary>Original instance untuk passing ke original mod methods.</summary>
        public object Instance => _instance;

        /// <summary>Event ID (precondition key).</summary>
        public string Id => GetPropertyValue<string>("Id") ?? "";

        /// <summary>Location name where event occurs.</summary>
        public string LocationName => GetPropertyValue<string>("LocationName") ?? "";

        /// <summary>Source mod name (null for vanilla).</summary>
        public string? ModName => GetPropertyValue<string>("ModName");

        /// <summary>Source mod unique ID.</summary>
        public string? ModUniqueId => GetPropertyValue<string>("ModUniqueId");

        /// <summary>Source mod folder path.</summary>
        public string? ModFolderPath => GetPropertyValue<string>("ModFolderPath");

        /// <summary>Whether the event script has parse errors.</summary>
        public bool HasInvalidScript => GetPropertyValue<bool>("HasInvalidScript");

        /// <summary>Whether the event is from Content Patcher.</summary>
        public bool IsFromContentPatcher => GetPropertyValue<bool>("IsFromContentPatcher");

        /// <summary>Whether the event is disabled by Content Patcher conditions.</summary>
        public bool IsDisabledByCP => GetPropertyValue<bool>("IsDisabledByCP");

        /// <summary>List of required NPC names.</summary>
        public List<string>? RequiredNpcs
        {
            get
            {
                try
                {
                    var value = GetPropertyValue<object>("RequiredNpcs");

                    if (value is IEnumerable<string> stringEnumerable)
                        return stringEnumerable.ToList();

                    if (value is System.Collections.IEnumerable collection)
                        return collection.Cast<object>()
                                        .Select(o => o?.ToString() ?? "")
                                        .ToList();
                }
                catch { /* Ignore */ }

                return null;
            }
        }

        /// <summary>Heart requirements as wrapped list.</summary>
        public List<HeartRequirementWrapper>? HeartRequirements
        {
            get
            {
                try
                {
                    var value = GetPropertyValue<object>("HeartRequirements");

                    if (value is System.Collections.IEnumerable collection)
                    {
                        var result = new List<HeartRequirementWrapper>();

                        foreach (var item in collection)
                        {
                            if (item != null)
                                result.Add(new HeartRequirementWrapper(item));
                        }

                        return result;
                    }
                }
                catch { /* Ignore */ }

                return null;
            }
        }

        #endregion

        #region ═══ Methods ═══

        /// <summary>Get translated location name.</summary>
        public string GetTranslatedLocation()
        {
            return InvokeMethod<string>("GetTranslatedLocation") ?? LocationName;
        }

        /// <summary>Get human-readable conditions summary.</summary>
        public string? GetConditionsSummary()
        {
            return InvokeMethod<string>("GetConditionsSummary");
        }

        /// <summary>Get the full event script.</summary>
        public string? GetEventScript()
        {
            return InvokeMethod<string>("GetEventScript");
        }

        /// <summary>Check if all preconditions are currently met.</summary>
        public bool AreConditionsMet()
        {
            return InvokeMethod<bool>("AreConditionsMet");
        }

        #endregion

        #region ═══ Helper Methods ═══

        private T? GetPropertyValue<T>(string propertyName)
        {
            try
            {
                // Try cache first
                if (_propertyCache.TryGetValue(propertyName, out var prop) && prop != null)
                {
                    var value = prop.GetValue(_instance);
                    if (value is T typed)
                        return typed;
                }
                else
                {
                    // Fallback: direct lookup
                    var directProp = _type.GetProperty(propertyName,
                        BindingFlags.Instance | BindingFlags.Public);

                    if (directProp != null)
                    {
                        var value = directProp.GetValue(_instance);
                        if (value is T typed)
                            return typed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetPropertyValue({propertyName}) error: {ex.Message}");
            }

            return default;
        }

        private T? InvokeMethod<T>(string methodName, params object[] args)
        {
            try
            {
                // Try cache first
                if (_methodCache.TryGetValue(methodName, out var method) && method != null)
                {
                    var result = method.Invoke(_instance, args);
                    if (result is T typed)
                        return typed;
                }
                else
                {
                    // Fallback: direct lookup
                    var directMethod = _type.GetMethod(methodName,
                        BindingFlags.Instance | BindingFlags.Public);

                    if (directMethod != null)
                    {
                        var result = directMethod.Invoke(_instance, args);
                        if (result is T typed)
                            return typed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InvokeMethod({methodName}) error: {ex.Message}");
            }

            return default;
        }

        #endregion

        #region ═══ Static Factory ═══

        /// <summary>Wrap a collection of EventData objects.</summary>
        public static List<EventDataWrapper> WrapCollection(System.Collections.IEnumerable? collection)
        {
            var result = new List<EventDataWrapper>();

            if (collection == null)
                return result;

            foreach (var item in collection)
            {
                if (item != null)
                    result.Add(new EventDataWrapper(item));
            }

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Wrapper untuk CharacterHeartRequirement dari MHEventsList.
    /// </summary>
    public class HeartRequirementWrapper
    {
        private readonly object _instance;
        private readonly Type _type;

        public HeartRequirementWrapper(object instance)
        {
            _instance = instance ?? throw new ArgumentNullException(nameof(instance));
            _type = instance.GetType();
        }

        /// <summary>NPC internal name.</summary>
        public string NpcName
        {
            get
            {
                try
                {
                    var prop = _type.GetProperty("NpcName");
                    return prop?.GetValue(_instance) as string ?? "";
                }
                catch { return ""; }
            }
        }

        /// <summary>Required heart level.</summary>
        public int Hearts
        {
            get
            {
                try
                {
                    var prop = _type.GetProperty("Hearts");
                    var value = prop?.GetValue(_instance);
                    return value is int i ? i : 0;
                }
                catch { return 0; }
            }
        }
    }
}