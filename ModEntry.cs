using MobileUISupport;
using MobileUISupport.Framework;
using MobileUISupport.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace MobileUISupport
{
    public class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private MagicStardewAPI? API;
        private SpellMenuInterceptor? Interceptor;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();

            Monitor.Log("╔═══════════════════════════════════════════╗", LogLevel.Info);
            Monitor.Log("║      Magic Mobile UI - Starting...        ║", LogLevel.Info);
            Monitor.Log("║      Event-based Menu Replacement         ║", LogLevel.Info);
            Monitor.Log("╚═══════════════════════════════════════════╝", LogLevel.Info);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Check for Magic Stardew
            if (!Helper.ModRegistry.IsLoaded("Zexu2K.MagicStardew.C"))
            {
                Monitor.Log("╔═══════════════════════════════════════════╗", LogLevel.Error);
                Monitor.Log("║  ERROR: Magic Stardew not found!          ║", LogLevel.Error);
                Monitor.Log("║  Please install Zexu2K.MagicStardew.C     ║", LogLevel.Error);
                Monitor.Log("╚═══════════════════════════════════════════╝", LogLevel.Error);
                return;
            }

            Monitor.Log("Magic Stardew detected ✓", LogLevel.Info);

            // Initialize API
            API = new MagicStardewAPI(Monitor, Helper);

            // Create interceptor (uses SMAPI events, not Harmony)
            Interceptor = new SpellMenuInterceptor(Monitor, Helper, Config, API);

            if (Interceptor.Setup())
            {
                Monitor.Log("╔═══════════════════════════════════════════╗", LogLevel.Info);
                Monitor.Log("║      Magic Mobile UI - Ready! ✓           ║", LogLevel.Info);
                Monitor.Log("║      SpellMenu will be replaced           ║", LogLevel.Info);
                Monitor.Log("╚═══════════════════════════════════════════╝", LogLevel.Info);
            }
            else
            {
                Monitor.Log("Failed to setup menu interceptor!", LogLevel.Error);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Initialize API after game is fully loaded
            if (API != null)
            {
                bool initialized = API.Initialize();
                Monitor.Log($"MagicStardew API initialized: {initialized}", LogLevel.Debug);
            }

            if (Config.DebugMode)
            {
                Monitor.Log($"Screen: {StardewValley.Game1.uiViewport.Width}x{StardewValley.Game1.uiViewport.Height}", LogLevel.Debug);
            }
        }
    }
}