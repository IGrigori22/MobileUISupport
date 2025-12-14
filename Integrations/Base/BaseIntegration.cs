using MobileUISupport.Framework;
using StardewModdingAPI;

namespace MobileUISupport.Integrations.Base
{
    /// <summary>
    /// Base class untuk semua mod integrations.
    /// </summary>
    public abstract class BaseIntegration : IModIntegration
    {
        // ═══════════════════════════════════════════════════════
        // Properties
        // ═══════════════════════════════════════════════════════

        public abstract string ModId { get; }
        public abstract string DisplayName { get; }
        public bool IsAvailable { get; protected set; }
        public abstract bool IsEnabled { get; }

        protected IModHelper Helper => ModServices.Helper;
        protected IMonitor Monitor => ModServices.Monitor;
        protected ModConfig Config => ModServices.Config;

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        public bool Initialize()
        {
            // Check if enabled
            if (!IsEnabled)
            {
                Logger.DebugOnly(DisplayName, "Disabled in config");
                return false;
            }

            // Check if mod is loaded
            if (!Helper.ModRegistry.IsLoaded(ModId))
            {
                Logger.Debug($"{DisplayName} not found - integration disabled");
                return false;
            }

            Logger.Debug($"{DisplayName} detected");

            // Do actual initialization
            try
            {
                IsAvailable = DoInitialize();
                Logger.IntegrationStatus($"{DisplayName} integration", IsAvailable);
                return IsAvailable;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to initialize {DisplayName}: {ex.Message}");
                IsAvailable = false;
                return false;
            }
        }

        /// <summary>
        /// Override ini untuk initialization logic spesifik.
        /// </summary>
        protected abstract bool DoInitialize();

        /// <summary>
        /// Called saat save loaded.
        /// </summary>
        public virtual void OnSaveLoaded() { }

        /// <summary>
        /// Called saat returned to title.
        /// </summary>
        public virtual void OnReturnedToTitle() { }
    }
}