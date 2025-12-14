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

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Squad API initialization failed: {ex.Message}");
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
            if (!_isInitialized || npc == null)
            {
                Logger.DebugOnly("Squad", "Cannot trigger: not initialized or NPC is null");
                return false;
            }

            try
            {
                // Pre-checks
                if (!IsPlayerFree())
                {
                    Logger.DebugOnly("Squad", "Player is not free");
                    return false;
                }

                if (IsFestival())
                {
                    Logger.DebugOnly("Squad", "Cannot interact during festival");
                    ShowFestivalError();
                    return false;
                }

                // Route to appropriate handler
                bool isRecruited = IsNPCRecruited(npc);

                return isRecruited
                    ? CallHandleManagement(npc)
                    : CallHandleRecruitment(npc);
            }
            catch (Exception ex)
            {
                Logger.Error($"TriggerInteraction error: {ex.Message}");
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

                // Call HandleRecruitment
                var method = _handleRecruitmentMethod ?? FindMethod(squadMate, "HandleRecruitment");

                if (method != null)
                {
                    Logger.DebugOnly("Squad", $"Calling HandleRecruitment for {npc.Name}");
                    method.Invoke(squadMate, new object[] { Game1.player });
                    return true;
                }

                Logger.Warn("HandleRecruitment method not found");
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
                // Get existing SquadMate
                var squadMate = _getMemberMethod?.Invoke(_squadManager, new object[] { npc });

                if (squadMate == null)
                {
                    Logger.Warn($"Could not get SquadMate for {npc.Name}");
                    return false;
                }

                // Call HandleManagement
                var method = _handleManagementMethod ?? FindMethod(squadMate, "HandleManagement");

                if (method != null)
                {
                    Logger.DebugOnly("Squad", $"Calling HandleManagement for {npc.Name}");
                    method.Invoke(squadMate, null);
                    return true;
                }

                Logger.Warn("HandleManagement method not found");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"CallHandleManagement error: {ex.Message}");
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

                // Get managers from ModEntry
                _squadMateFactory = modType.GetProperty("SquadMateFactory", flags)?.GetValue(_modEntry);
                _squadManager = modType.GetProperty("SquadManager", flags)?.GetValue(_modEntry);
                _modConfig = modType.GetProperty("Config", flags)?.GetValue(_modEntry);

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
                var squadMateType = _factoryCreateMethod.ReturnType;
                var publicInstance = BindingFlags.Instance | BindingFlags.Public;

                _handleRecruitmentMethod = squadMateType.GetMethod("HandleRecruitment", publicInstance);
                _handleManagementMethod = squadMateType.GetMethod("HandleManagement", publicInstance);
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
    }
}