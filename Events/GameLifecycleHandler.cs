using MobileUISupport.Framework;
using MobileUISupport.Integrations;
using StardewModdingAPI.Events;

namespace MobileUISupport.Events
{
    /// <summary>
    /// Handle game lifecycle events.
    /// </summary>
    public class GameLifecycleHandler
    {
        private readonly IntegrationManager _integrations;

        public GameLifecycleHandler(IntegrationManager integrations)
        {
            _integrations = integrations;
        }

        // ═══════════════════════════════════════════════════════
        // Registration
        // ═══════════════════════════════════════════════════════

        public void Register()
        {
            var events = ModServices.Helper.Events.GameLoop;

            events.GameLaunched += OnGameLaunched;
            events.SaveLoaded += OnSaveLoaded;
            events.ReturnedToTitle += OnReturnedToTitle;
            events.DayStarted += OnDayStarted;
        }

        // ═══════════════════════════════════════════════════════
        // Event Handlers
        // ═══════════════════════════════════════════════════════

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            _integrations.InitializeAll();
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            _integrations.OnSaveLoaded();

            if (ModServices.Config.DebugMode)
            {
                Logger.LogDebugInfo();
                LogIntegrationStatus();
            }
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            _integrations.OnReturnedToTitle();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Hook untuk daily events jika diperlukan
        }

        // ═══════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════

        private void LogIntegrationStatus()
        {
            Logger.Section("Integration Status");

            foreach (var integration in _integrations.GetAvailable())
            {
                Logger.Debug($"  ✓ {integration.DisplayName}");
            }

            Logger.Debug($"Squad Available: {_integrations.Squad?.IsAvailable ?? false}");
            Logger.Debug($"Magic Available: {_integrations.Magic?.IsAvailable ?? false}");
        }
    }
}