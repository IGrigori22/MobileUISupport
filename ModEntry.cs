using MobileUISupport.Events;
using MobileUISupport.Framework;
using MobileUISupport.Integrations;
using StardewModdingAPI;

namespace MobileUISupport
{
    /// <summary>
    /// Main entry point untuk Mobile UI Support mod.
    /// Bertanggung jawab hanya untuk wiring dan initialization.
    /// </summary>
    public class ModEntry : Mod
    {
        // ═══════════════════════════════════════════════════════
        // Fields
        // ═══════════════════════════════════════════════════════

        private IntegrationManager _integrations = null!;
        private GameLifecycleHandler _lifecycleHandler = null!;
        private InputHandler _inputHandler = null!;
        private RenderHandler _renderHandler = null!;

        // ═══════════════════════════════════════════════════════
        // Entry Point
        // ═══════════════════════════════════════════════════════

        public override void Entry(IModHelper helper)
        {
            // Phase 1: Initialize core services
            ModServices.Initialize(helper, Monitor, ModManifest);

            // Phase 2: Log startup
            Logger.Banner("Starting...");

            // Phase 3: Create managers
            InitializeManagers();

            // Phase 4: Register event handlers
            RegisterEventHandlers();

            Logger.Debug("Initialization complete");
        }

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        private void InitializeManagers()
        {
            _integrations = new IntegrationManager();
            _lifecycleHandler = new GameLifecycleHandler(_integrations);
            _inputHandler = new InputHandler(_integrations);
            _renderHandler = new RenderHandler(_integrations);
        }

        private void RegisterEventHandlers()
        {
            _lifecycleHandler.Register();
            _inputHandler.Register();
            _renderHandler.Register();
        }
    }
}