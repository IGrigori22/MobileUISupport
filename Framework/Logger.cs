using StardewModdingAPI;
using StardewValley;

namespace MobileUISupport.Framework
{
    /// <summary>
    /// Centralized logging helper dengan banner dan formatting.
    /// </summary>
    public static class Logger
    {
        private const string MOD_NAME = "Mobile UI Support";

        // ═══════════════════════════════════════════════════════
        // Basic Logging
        // ═══════════════════════════════════════════════════════

        public static void Info(string message)
            => ModServices.Monitor.Log(message, LogLevel.Info);

        public static void Debug(string message)
            => ModServices.Monitor.Log(message, LogLevel.Debug);

        public static void Warn(string message)
            => ModServices.Monitor.Log(message, LogLevel.Warn);

        public static void Error(string message)
            => ModServices.Monitor.Log(message, LogLevel.Error);

        public static void Trace(string message)
            => ModServices.Monitor.Log(message, LogLevel.Trace);

        // ═══════════════════════════════════════════════════════
        // Conditional Logging
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Log hanya jika DebugMode aktif.
        /// </summary>
        public static void DebugOnly(string message)
        {
            if (ModServices.Config.DebugMode)
                Debug(message);
        }

        /// <summary>
        /// Log hanya jika DebugMode aktif dengan custom prefix.
        /// </summary>
        public static void DebugOnly(string category, string message)
        {
            if (ModServices.Config.DebugMode)
                Debug($"[{category}] {message}");
        }

        // ═══════════════════════════════════════════════════════
        // Banner Logging
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Log banner message dengan box.
        /// </summary>
        public static void Banner(string message)
        {
            var monitor = ModServices.Monitor;
            monitor.Log("╔═══════════════════════════════════════════╗", LogLevel.Info);
            monitor.Log($"║  {MOD_NAME} - {message,-20} ║", LogLevel.Info);
            monitor.Log("╚═══════════════════════════════════════════╝", LogLevel.Info);
        }

        /// <summary>
        /// Log section header.
        /// </summary>
        public static void Section(string title)
        {
            ModServices.Monitor.Log($"=== {title} ===", LogLevel.Debug);
        }

        // ═══════════════════════════════════════════════════════
        // Status Logging
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Log integration status dengan checkmark.
        /// </summary>
        public static void IntegrationStatus(string name, bool success)
        {
            string status = success ? "✓" : "✗";
            var level = success ? LogLevel.Info : LogLevel.Warn;
            ModServices.Monitor.Log($"{name} {status}", level);
        }

        /// <summary>
        /// Log feature enabled/disabled.
        /// </summary>
        public static void FeatureStatus(string feature, bool enabled)
        {
            string status = enabled ? "enabled" : "disabled";
            Debug($"{feature}: {status}");
        }

        // ═══════════════════════════════════════════════════════
        // Config & Debug Info
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Log config summary.
        /// </summary>
        public static void LogConfigSummary()
        {
            if (!ModServices.Config.DebugMode)
                return;

            var config = ModServices.Config;

            Section("Config Summary");
            Debug($"  Grid: {config.MagicStardew.GridColumns}x{config.MagicStardew.GridRows}");
            Debug($"  Icon Size: {config.MagicStardew.SpellIconSize}px");
            Debug($"  Theme: {config.MagicStardew.ThemeColor}");
            Debug($"  Close After Cast: {config.MagicStardew.CloseAfterCast}");
            Debug($"  Squad Support: {config.StardewSquad.EnableSupport}");
        }

        /// <summary>
        /// Log debug info tentang environment.
        /// </summary>
        public static void LogDebugInfo()
        {
            if (!ModServices.Config.DebugMode)
                return;

            Section("Debug Info");
            Debug($"  Platform: {Constants.TargetPlatform}");
            Debug($"  Screen: {Game1.uiViewport.Width}x{Game1.uiViewport.Height}");
            Debug($"  UI Scale: {Game1.options.uiScale}");
            Debug($"  Zoom: {Game1.options.zoomLevel}");
        }

        /// <summary>
        /// Log dengan key-value pairs.
        /// </summary>
        public static void LogKeyValues(string title, params (string key, object value)[] pairs)
        {
            if (!ModServices.Config.DebugMode)
                return;

            Section(title);
            foreach (var (key, value) in pairs)
            {
                Debug($"  {key}: {value}");
            }
        }
    }
}