using MobileUISupport.Framework;
using MobileUISupport.Integrations.AddonsAPI;
using MobileUISupport.Integrations.Base;
using MobileUISupport.Integrations.GMCM;
using MobileUISupport.Integrations.MagicStardew;
using MobileUISupport.Integrations.StardewSquad;

namespace MobileUISupport.Integrations
{
    /// <summary>
    /// Manages semua mod integrations.
    /// </summary>
    public class IntegrationManager
    {
        // ═══════════════════════════════════════════════════════
        // Fields
        // ═══════════════════════════════════════════════════════

        private readonly List<IModIntegration> _integrations = new();

        // ═══════════════════════════════════════════════════════
        // Public Accessors
        // ═══════════════════════════════════════════════════════

        public GMCMIntegration? GMCM { get; private set; }
        public AddonsAPIIntegration? AddonsAPI { get; private set; }
        public MagicStardewIntegration? Magic { get; private set; }
        public StardewSquadIntegration? Squad { get; private set; }

        // ═══════════════════════════════════════════════════════
        // Statistics
        // ═══════════════════════════════════════════════════════

        public int TotalIntegrations => _integrations.Count;
        public int ActiveIntegrations => _integrations.Count(i => i.IsAvailable);

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        public void InitializeAll()
        {
            Logger.Debug("Initializing integrations...");

            // ─────────────────────────────────────────────────────
            // Phase 1: Core integrations
            // ─────────────────────────────────────────────────────

            // GMCM
            GMCM = new GMCMIntegration();
            GMCM.Initialize();

            // AddonsAPI (harus sebelum mod yang butuh register button)
            AddonsAPI = new AddonsAPIIntegration();
            RegisterIntegration(AddonsAPI);
            AddonsAPI.Initialize();

            // ─────────────────────────────────────────────────────
            // Phase 2: Game mod integrations
            // ─────────────────────────────────────────────────────

            Magic = new MagicStardewIntegration();
            RegisterIntegration(Magic);

            Squad = new StardewSquadIntegration();
            RegisterIntegration(Squad);

            // ─────────────────────────────────────────────────────
            // Phase 3: Initialize all (kecuali yang sudah di-init)
            // ─────────────────────────────────────────────────────

            foreach (var integration in _integrations)
            {
                if (integration != AddonsAPI) // AddonsAPI sudah di-init
                {
                    integration.Initialize();
                }
            }

            // ─────────────────────────────────────────────────────
            // Phase 4: Wire up dependencies
            // ─────────────────────────────────────────────────────

            WireDependencies();

            // Log summary
            LogIntegrationSummary();
        }

        /// <summary>
        /// Wire up cross-integration dependencies.
        /// </summary>
        private void WireDependencies()
        {
            // Pass AddonsAPI ke integrations yang butuh
            if (AddonsAPI?.IsAvailable == true)
            {
                Magic?.SetAddonsAPI(AddonsAPI);
                Squad?.SetAddonsAPI(AddonsAPI); // Uncomment jika Squad juga butuh

                Logger.Debug("Wired AddonsAPI dependencies");
            }
        }

        private void LogIntegrationSummary()
        {
            Logger.Debug($"Integrations initialized: {ActiveIntegrations}/{TotalIntegrations} active");

            foreach (var integration in _integrations)
            {
                string status = integration.IsAvailable ? "✓" : "✗";
                Logger.Debug($"  [{status}] {integration.DisplayName}");
            }
        }

        /// <summary>
        /// Register integration baru.
        /// </summary>
        public void RegisterIntegration(IModIntegration integration)
        {
            _integrations.Add(integration);
        }

        // ═══════════════════════════════════════════════════════
        // Lifecycle Events
        // ═══════════════════════════════════════════════════════

        public void OnSaveLoaded()
        {
            // AddonsAPI dulu (harus ready sebelum yang lain register button)
            if (AddonsAPI?.IsAvailable == true)
            {
                try
                {
                    AddonsAPI.OnSaveLoaded();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in AddonsAPI.OnSaveLoaded: {ex.Message}");
                }
            }

            // Lalu yang lain
            foreach (var integration in _integrations.Where(i => i.IsAvailable && i != AddonsAPI))
            {
                try
                {
                    integration.OnSaveLoaded();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in {integration.DisplayName}.OnSaveLoaded: {ex.Message}");
                }
            }
        }

        public void OnReturnedToTitle()
        {
            // Game mod integrations dulu
            foreach (var integration in _integrations.Where(i => i.IsAvailable && i != AddonsAPI))
            {
                try
                {
                    integration.OnReturnedToTitle();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in {integration.DisplayName}.OnReturnedToTitle: {ex.Message}");
                }
            }

            // AddonsAPI terakhir (untuk cleanup buttons)
            if (AddonsAPI?.IsAvailable == true)
            {
                try
                {
                    AddonsAPI.OnReturnedToTitle();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in AddonsAPI.OnReturnedToTitle: {ex.Message}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // Query Methods
        // ═══════════════════════════════════════════════════════

        public T? Get<T>() where T : class, IModIntegration
        {
            return _integrations.OfType<T>().FirstOrDefault();
        }

        public bool IsAvailable<T>() where T : class, IModIntegration
        {
            return Get<T>()?.IsAvailable ?? false;
        }

        public IEnumerable<IModIntegration> GetAvailable()
        {
            return _integrations.Where(i => i.IsAvailable);
        }
    }
}