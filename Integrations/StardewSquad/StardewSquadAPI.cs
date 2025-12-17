using System.Reflection;
using MobileUISupport.Framework;
using StardewModdingAPI;
using StardewValley;

namespace MobileUISupport.Integrations.StardewSquad
{
    /// <summary>
    /// API layer untuk The Stardew Squad mod.
    /// Menghandle semua reflection dan method invocation.
    /// </summary>
    public class StardewSquadAPI
    {
        // ═══════════════════════════════════════════════════════
        // Constants
        // ═══════════════════════════════════════════════════════

        public const string MOD_ID = "ThaliaFawnheart.TheStardewSquad";

        // ═══════════════════════════════════════════════════════
        // Fields - Cached Instances
        // ═══════════════════════════════════════════════════════

        private object? _modEntry;
        private object? _squadManager;
        private object? _squadMateFactory;
        private object? _gameContext;
        private object? _uiService;
        private object? _modConfig;

        // ═══════════════════════════════════════════════════════
        // Fields - Cached Methods/Properties
        // ═══════════════════════════════════════════════════════

        private MethodInfo? _factoryCreateMethod;
        private MethodInfo? _handleRecruitmentMethod;
        private MethodInfo? _handleManagementMethod;
        private MethodInfo? _isRecruitedMethod;
        private MethodInfo? _getMemberMethod;
        private PropertyInfo? _countProperty;
        private PropertyInfo? _maxSquadSizeProperty;

        // ═══════════════════════════════════════════════════════
        // Fields - State
        // ═══════════════════════════════════════════════════════

        private bool _isInitialized;

        // ═══════════════════════════════════════════════════════
        // Properties
        // ═══════════════════════════════════════════════════════

        public bool IsInitialized => _isInitialized;

        private IModHelper Helper => ModServices.Helper;

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Initialize API dengan reflection.
        /// </summary>
        public bool Initialize()
        {
            try
            {
                Logger.Debug("Initializing Stardew Squad API...");

                var modInfo = Helper.ModRegistry.Get(MOD_ID);
                if (modInfo == null)
                {
                    Logger.Info("The Stardew Squad not installed");
                    return false;
                }

                Logger.Info($"Found The Stardew Squad v{modInfo.Manifest.Version}");

                // Step-by-step initialization
                if (!GetModEntryInstance(modInfo)) return false;
                if (!GetManagers()) return false;
                if (!CacheMethods()) return false;

                _isInitialized = true;

                Logger.Info("✓ Stardew Squad API initialized!");
                LogStatus();

                // 🔍 TAMBAHKAN INI UNTUK DEBUG
                if (ModServices.Config.DebugMode)
                {
                    QuickHealthCheck();
                    DiagnoseModStructure();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Squad API initialization failed: {ex.Message}");

                // 🔍 TETAP DIAGNOSE MESKI GAGAL
                try { DiagnoseModStructure(); } catch { }

                _isInitialized = false;
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Trigger Interaction
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Trigger interaksi dengan NPC (recruitment atau management).
        /// Sama persis dengan yang terjadi saat user tekan RecruitKey.
        /// </summary>
        public bool TriggerInteraction(NPC npc)
        {
            Logger.Debug($"[TriggerInteraction] Called for: {npc?.Name ?? "NULL"}");

            if (!_isInitialized)
            {
                Logger.Debug("[TriggerInteraction] Not initialized!");
                return false;
            }

            if (npc == null)
            {
                Logger.Debug("[TriggerInteraction] NPC is null!");
                return false;
            }

            try
            {
                // Pre-checks with logging
                bool playerFree = IsPlayerFree();
                Logger.Debug($"[TriggerInteraction] IsPlayerFree: {playerFree}");

                if (!playerFree)
                {
                    Logger.Debug("[TriggerInteraction] Player is not free - aborting");
                    return false;
                }

                bool isFestival = IsFestival();
                Logger.Debug($"[TriggerInteraction] IsFestival: {isFestival}");

                if (isFestival)
                {
                    ShowFestivalError();
                    return false;
                }

                // Check recruitment status
                bool isRecruited = IsNPCRecruited(npc);
                Logger.Debug($"[TriggerInteraction] IsRecruited({npc.Name}): {isRecruited}");

                // Route to handler
                bool result;
                if (isRecruited)
                {
                    Logger.Debug($"[TriggerInteraction] Routing to HandleManagement...");
                    result = CallHandleManagement(npc);
                }
                else
                {
                    Logger.Debug($"[TriggerInteraction] Routing to HandleRecruitment...");
                    result = CallHandleRecruitment(npc);
                }

                Logger.Debug($"[TriggerInteraction] Result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"[TriggerInteraction] Unexpected error: {ex.Message}");
                Logger.Error($"  Stack: {ex.StackTrace}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Query Status
        // ═══════════════════════════════════════════════════════

        public bool IsNPCRecruited(NPC npc)
        {
            if (!_isInitialized || npc == null)
                return false;

            try
            {
                var result = _isRecruitedMethod?.Invoke(_squadManager, new object[] { npc });
                return result is true;
            }
            catch
            {
                return false;
            }
        }

        public int GetSquadCount()
        {
            if (!_isInitialized)
                return 0;

            try
            {
                var result = _countProperty?.GetValue(_squadManager);
                return result is int count ? count : 0;
            }
            catch
            {
                return 0;
            }
        }

        public int GetMaxSquadSize()
        {
            if (_modConfig != null && _maxSquadSizeProperty != null)
            {
                try
                {
                    var result = _maxSquadSizeProperty.GetValue(_modConfig);
                    return result is int size ? size : 4;
                }
                catch { }
            }
            return 4;
        }

        public bool IsSquadFull() => GetSquadCount() >= GetMaxSquadSize();

        // ═══════════════════════════════════════════════════════
        // Private Methods - Call Original Methods
        // ═══════════════════════════════════════════════════════

        private bool CallHandleRecruitment(NPC npc)
        {
            try
            {             

                // Create SquadMate from NPC
                var squadMate = _factoryCreateMethod?.Invoke(_squadMateFactory, new object[] { npc });

                if (squadMate == null)
                {
                    Logger.Warn($"Failed to create SquadMate for {npc.Name}");
                    return false;
                }

                // ⭐ PENTING: Get method dari CONCRETE type, bukan cached interface method
                var concreteType = squadMate.GetType();
                Logger.Debug($"[CallHandleRecruitment] Concrete type: {concreteType.FullName}");

                var method = concreteType.GetMethod("HandleRecruitment",
            BindingFlags.Instance | BindingFlags.Public);

                if (method != null)
                {
                    Logger.Debug($"[CallHandleRecruitment] Found method on concrete type");
                    method.Invoke(squadMate, new object[] { Game1.player });
                    return true;
                }

                Logger.Warn("HandleRecruitment method not found on concrete type");
                return false;
            }
            catch (TargetInvocationException tie)
            {
                Logger.Error($"Inner exception: {tie.InnerException?.Message}");
                Logger.Error($"Stack: {tie.InnerException?.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"CallHandleRecruitment error: {ex.Message}");
                return false;
            }
        }

        private bool CallHandleManagement(NPC npc)
        {
            try
            {
                Logger.Debug($"[CallHandleManagement] Starting for {npc.Name}");

                // Step 1: Get existing SquadMate
                Logger.Debug($"[CallHandleManagement] Invoking SquadManager.GetMember...");

                var squadMate = _getMemberMethod?.Invoke(_squadManager, new object[] { npc });


                if (squadMate == null)
                {
                    Logger.Warn($"Could not get SquadMate for {npc.Name}");
                    // Debug: Check if NPC is actually recruited
                    var isRecruited = IsNPCRecruited(npc);
                    Logger.Debug($"[CallHandleManagement] IsRecruited check: {isRecruited}");

                    return false;
                }

                Logger.Debug($"[CallHandleManagement] SquadMate found: {squadMate.GetType().Name}");

                // Step 2: Get method
                var method = _handleManagementMethod;

                if (method == null)
                {
                    Logger.Debug("[CallHandleManagement] Cached method null, searching on instance...");
                    method = FindMethod(squadMate, "HandleManagement");
                }

                if (method == null)
                {
                    Logger.Warn("[CallHandleManagement] HandleManagement method not found!");
                    return false;
                }

                Logger.Debug($"[CallHandleManagement] Method found: {method.Name}");
                Logger.Debug($"[CallHandleManagement] Method params count: {method.GetParameters().Length}");

                // Step 3: Invoke
                Logger.Debug($"[CallHandleManagement] Invoking...");

                method.Invoke(squadMate, null);

                Logger.Debug($"[CallHandleManagement] ✓ Success for {npc.Name}");
                return true;
            }
            catch (TargetInvocationException tie)
            {
                Logger.Error($"[CallHandleManagement] TargetInvocationException:");
                Logger.Error($"  Inner: {tie.InnerException?.GetType().Name}: {tie.InnerException?.Message}");
                Logger.Error($"  Stack: {tie.InnerException?.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"[CallHandleManagement] Exception: {ex.GetType().Name}: {ex.Message}");
                Logger.Error($"  Stack: {ex.StackTrace}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Private Methods - Context Checks
        // ═══════════════════════════════════════════════════════

        private bool IsPlayerFree()
        {
            if (_gameContext != null)
            {
                try
                {
                    var property = _gameContext.GetType().GetProperty("IsPlayerFree",
                        BindingFlags.Instance | BindingFlags.Public);
                    var result = property?.GetValue(_gameContext);
                    return result is true;
                }
                catch { }
            }

            // Fallback
            return Context.IsPlayerFree && Game1.activeClickableMenu == null && !Game1.eventUp;
        }

        private bool IsFestival()
        {
            if (_gameContext != null)
            {
                try
                {
                    var property = _gameContext.GetType().GetProperty("IsFestival",
                        BindingFlags.Instance | BindingFlags.Public);
                    var result = property?.GetValue(_gameContext);
                    return result is true;
                }
                catch { }
            }

            return Game1.isFestival();
        }

        private void ShowFestivalError()
        {
            if (_uiService != null)
            {
                try
                {
                    var showError = _uiService.GetType().GetMethod("ShowErrorMessage",
                        BindingFlags.Instance | BindingFlags.Public);
                    var getTrans = _uiService.GetType().GetMethod("GetTranslation",
                        BindingFlags.Instance | BindingFlags.Public);

                    if (showError != null && getTrans != null)
                    {
                        var message = getTrans.Invoke(_uiService, new object[] { "recruitment.festival_block" });
                        showError.Invoke(_uiService, new object[] { message });
                        return;
                    }
                }
                catch { }
            }

            // Fallback
            Game1.addHUDMessage(new HUDMessage("Cannot recruit during festival!", HUDMessage.error_type));
        }

        // ═══════════════════════════════════════════════════════
        // Private Methods - Initialization Steps
        // ═══════════════════════════════════════════════════════

        private bool GetModEntryInstance(IModInfo modInfo)
        {
            try
            {
                var modProperty = modInfo.GetType().GetProperty("Mod",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                _modEntry = modProperty?.GetValue(modInfo);
                return _modEntry != null;
            }
            catch (Exception ex)
            {
                Logger.Trace($"GetModEntryInstance error: {ex.Message}");
                return false;
            }
        }

        private bool GetManagers()
        {
            if (_modEntry == null)
                return false;

            try
            {
                var modType = _modEntry.GetType();
                var flags = BindingFlags.Instance | BindingFlags.Public;

                // Cari dengan nama alternatif
                _squadMateFactory = GetPropertyValue(modType, _modEntry,
                    "SquadMateFactory", "CompanionFactory", "MemberFactory", "Factory");

                _squadManager = GetPropertyValue(modType, _modEntry,
                    "SquadManager", "CompanionManager", "PartyManager", "Manager");

                _modConfig = GetPropertyValue(modType, _modEntry,
                    "Config", "ModConfig", "Settings");

                // Get context from InteractionManager
                var interactionManager = modType.GetProperty("InteractionManager", flags)?.GetValue(_modEntry);
                if (interactionManager != null)
                {
                    var imType = interactionManager.GetType();
                    var privateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

                    _gameContext = imType.GetField("_gameContext", privateFlags)?.GetValue(interactionManager);
                    _uiService = imType.GetField("_uiService", privateFlags)?.GetValue(interactionManager);
                }

                Logger.Trace($"Managers - Factory: {_squadMateFactory != null}, Squad: {_squadManager != null}");

                return _squadMateFactory != null && _squadManager != null;
            }
            catch (Exception ex)
            {
                Logger.Trace($"GetManagers error: {ex.Message}");
                return false;
            }
        }

        private object? GetPropertyValue(Type type, object instance, params string[] possibleNames)
        {
            var prop = FindPropertyFlexible(type, possibleNames);
            return prop?.GetValue(instance);
        }

        private bool CacheMethods()
        {
            try
            {
                var publicInstance = BindingFlags.Instance | BindingFlags.Public;

                // SquadMateFactory.Create(NPC)
                if (_squadMateFactory != null)
                {
                    _factoryCreateMethod = _squadMateFactory.GetType()
                        .GetMethod("Create", publicInstance, null, new[] { typeof(NPC) }, null);
                }

                // SquadManager methods
                if (_squadManager != null)
                {
                    var smType = _squadManager.GetType();
                    _isRecruitedMethod = smType.GetMethod("IsRecruited", publicInstance);
                    _getMemberMethod = smType.GetMethod("GetMember", publicInstance);
                    _countProperty = smType.GetProperty("Count", publicInstance);
                }

                // ModConfig.MaxSquadSize
                if (_modConfig != null)
                {
                    _maxSquadSizeProperty = _modConfig.GetType()
                        .GetProperty("MaxSquadSize", publicInstance);
                }

                // Cache SquadMate methods from return type
                CacheSquadMateMethods();

                return _factoryCreateMethod != null;
            }
            catch (Exception ex)
            {
                Logger.Trace($"CacheMethods error: {ex.Message}");
                return false;
            }
        }

        private void CacheSquadMateMethods()
        {
            if (_factoryCreateMethod == null)
                return;

            try
            {
                // INI RETURNS ISquadMate (interface)
                var squadMateType = _factoryCreateMethod.ReturnType;

                Logger.Debug($"[CacheSquadMateMethods] Return type: {squadMateType.Name}");
                Logger.Debug($"[CacheSquadMateMethods] IsInterface: {squadMateType.IsInterface}");

                var publicInstance = BindingFlags.Instance | BindingFlags.Public;

                _handleRecruitmentMethod = squadMateType.GetMethod("HandleRecruitment", publicInstance);
                _handleManagementMethod = squadMateType.GetMethod("HandleManagement", publicInstance);

                Logger.Debug($"[CacheSquadMateMethods] HandleRecruitment: {_handleRecruitmentMethod != null}");
                Logger.Debug($"[CacheSquadMateMethods] HandleManagement: {_handleManagementMethod != null}");

                // JIKA INTERFACE, method seharusnya tetap bisa dipanggil
                // Tapi untuk safety, kita bisa coba get dari concrete implementation
            }
            catch (Exception ex)
            {
                Logger.Trace($"CacheSquadMateMethods error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Private Methods - Helpers
        // ═══════════════════════════════════════════════════════

        private static MethodInfo? FindMethod(object obj, string methodName)
        {
            return obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        }

        /// <summary>
        /// Cari method dengan nama mirip (untuk handle rename).
        /// </summary>
        private MethodInfo? FindMethodFlexible(Type type, params string[] possibleNames)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (var name in possibleNames)
            {
                // Exact match first
                var method = type.GetMethod(name, flags);
                if (method != null) return method;

                // Partial match
                method = type.GetMethods(flags)
                    .FirstOrDefault(m => m.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                if (method != null)
                {
                    Logger.Debug($"Found similar method: {method.Name} (searched: {name})");
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Cari property dengan nama mirip.
        /// </summary>
        private PropertyInfo? FindPropertyFlexible(Type type, params string[] possibleNames)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var name in possibleNames)
            {
                var prop = type.GetProperty(name, flags);
                if (prop != null) return prop;

                prop = type.GetProperties(flags)
                    .FirstOrDefault(p => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
                if (prop != null)
                {
                    Logger.Debug($"Found similar property: {prop.Name} (searched: {name})");
                    return prop;
                }
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════
        // Debug
        // ═══════════════════════════════════════════════════════

        private void LogStatus()
        {
            if (!ModServices.Config.DebugMode)
                return;

            Logger.Section("Stardew Squad API Status");
            Logger.Debug($"  SquadMateFactory    : {(_squadMateFactory != null ? "✓" : "✗")}");
            Logger.Debug($"  Factory.Create      : {(_factoryCreateMethod != null ? "✓" : "✗")}");
            Logger.Debug($"  HandleRecruitment   : {(_handleRecruitmentMethod != null ? "✓" : "✗")}");
            Logger.Debug($"  HandleManagement    : {(_handleManagementMethod != null ? "✓" : "✗")}");
            Logger.Debug($"  SquadManager        : {(_squadManager != null ? "✓" : "✗")}");
            Logger.Debug($"  GameContext         : {(_gameContext != null ? "✓" : "✗")}");
            Logger.Debug($"  Max Squad Size      : {GetMaxSquadSize()}");
        }

        // ═══════════════════════════════════════════════════════════
        // Diagnostic Methods - Untuk Debug Setelah Update
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// Dump semua public members dari object untuk melihat perubahan struktur.
        /// </summary>
        public void DiagnoseModStructure()
        {
            if (_modEntry == null)
            {
                Logger.Error("ModEntry is null - Initialize() mungkin gagal");
                return;
            }

            Logger.Section("=== DIAGNOSTIC: Mod Structure Analysis ===");

            // 1. Analyze ModEntry
            Logger.Debug("\n📦 ModEntry Properties:");
            DumpMembers(_modEntry, "ModEntry");

            // 2. Analyze SquadMateFactory
            if (_squadMateFactory != null)
            {
                Logger.Debug("\n🏭 SquadMateFactory Methods:");
                DumpMembers(_squadMateFactory, "SquadMateFactory");

                // Check Create method signature
                CheckCreateMethodSignature();
            }
            else
            {
                Logger.Warn("❌ SquadMateFactory is NULL - Property name mungkin berubah!");
                FindSimilarProperties(_modEntry, "Factory", "SquadMate", "Create");
            }

            // 3. Analyze SquadManager
            if (_squadManager != null)
            {
                Logger.Debug("\n👥 SquadManager Methods:");
                DumpMembers(_squadManager, "SquadManager");
            }
            else
            {
                Logger.Warn("❌ SquadManager is NULL - Property name mungkin berubah!");
                FindSimilarProperties(_modEntry, "Manager", "Squad", "Member");
            }

            // 4. Check SquadMate type (jika factory ada)
            if (_factoryCreateMethod != null)
            {
                Logger.Debug("\n🧑 SquadMate (Return Type) Methods:");
                var squadMateType = _factoryCreateMethod.ReturnType;
                DumpTypeMembers(squadMateType, "SquadMate");
            }

            // 5. Check InteractionManager
            CheckInteractionManager();

            Logger.Section("=== END DIAGNOSTIC ===");
        }

        private void DumpMembers(object obj, string label)
        {
            var type = obj.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public;

            // Properties
            Logger.Debug($"  Properties of {type.Name}:");
            foreach (var prop in type.GetProperties(flags))
            {
                Logger.Debug($"    • {prop.Name} : {prop.PropertyType.Name}");
            }

            // Methods
            Logger.Debug($"  Methods of {type.Name}:");
            foreach (var method in type.GetMethods(flags | BindingFlags.DeclaredOnly))
            {
                var parameters = string.Join(", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Logger.Debug($"    • {method.Name}({parameters}) : {method.ReturnType.Name}");
            }
        }

        private void DumpTypeMembers(Type type, string label)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public;

            Logger.Debug($"  Type: {type.FullName}");

            // Methods yang kita cari
            var targetMethods = new[] { "HandleRecruitment", "HandleManagement", "Recruit", "Manage", "Interact" };

            foreach (var method in type.GetMethods(flags | BindingFlags.DeclaredOnly))
            {
                var parameters = string.Join(", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));

                // Highlight jika mirip dengan yang kita cari
                bool isImportant = targetMethods.Any(t =>
                    method.Name.Contains(t, StringComparison.OrdinalIgnoreCase));

                var marker = isImportant ? "⭐" : "  ";
                Logger.Debug($"  {marker} {method.Name}({parameters}) : {method.ReturnType.Name}");
            }
        }

        private void FindSimilarProperties(object obj, params string[] keywords)
        {
            var type = obj.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            Logger.Debug("  🔍 Searching for similar properties:");
            foreach (var prop in type.GetProperties(flags))
            {
                foreach (var keyword in keywords)
                {
                    if (prop.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        var access = prop.GetMethod?.IsPublic == true ? "public" : "private";
                        Logger.Debug($"    → Found: {access} {prop.Name} : {prop.PropertyType.Name}");
                    }
                }
            }
        }

        private void CheckCreateMethodSignature()
        {
            if (_squadMateFactory == null) return;

            var type = _squadMateFactory.GetType();
            var createMethods = type.GetMethods()
                .Where(m => m.Name.Contains("Create", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Logger.Debug("  🔍 All 'Create' methods found:");
            foreach (var method in createMethods)
            {
                var parameters = string.Join(", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Logger.Debug($"    → {method.Name}({parameters})");
            }
        }

        private void CheckInteractionManager()
        {
            if (_modEntry == null) return;

            var modType = _modEntry.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Cari property yang mengandung "Interaction" atau "Manager"
            Logger.Debug("\n🎮 Looking for InteractionManager:");

            foreach (var prop in modType.GetProperties(flags))
            {
                if (prop.Name.Contains("Interaction") || prop.Name.Contains("Handler"))
                {
                    var value = prop.GetValue(_modEntry);
                    Logger.Debug($"  Found: {prop.Name}");

                    if (value != null)
                    {
                        DumpMembers(value, prop.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Quick test untuk cek apakah semua critical methods tersedia.
        /// </summary>
        public void QuickHealthCheck()
        {
            Logger.Section("🏥 Quick Health Check");

            var checks = new Dictionary<string, bool>
            {
                ["ModEntry"] = _modEntry != null,
                ["SquadMateFactory"] = _squadMateFactory != null,
                ["Factory.Create"] = _factoryCreateMethod != null,
                ["SquadManager"] = _squadManager != null,
                ["IsRecruited"] = _isRecruitedMethod != null,
                ["GetMember"] = _getMemberMethod != null,
                ["HandleRecruitment"] = _handleRecruitmentMethod != null,
                ["HandleManagement"] = _handleManagementMethod != null,
                ["GameContext"] = _gameContext != null,
                ["UIService"] = _uiService != null,
            };

            foreach (var (name, status) in checks)
            {
                var icon = status ? "✅" : "❌";
                Logger.Debug($"  {icon} {name}");
            }

            var failedCount = checks.Count(c => !c.Value);
            if (failedCount > 0)
            {
                Logger.Warn($"\n⚠️ {failedCount} components failed! Run DiagnoseModStructure() for details.");
            }
            else
            {
                Logger.Info("\n✅ All components OK!");
            }
        }
    }
}