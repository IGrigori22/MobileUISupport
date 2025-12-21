using MobileUISupport.Integrations;
using MobileUISupport.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MobileUISupport.Events
{
    /// <summary>
    /// Handle render dan update events.
    /// </summary>
    public class RenderHandler
    {
        private readonly IntegrationManager _integrations;

        public RenderHandler(IntegrationManager integrations)
        {
            _integrations = integrations;
        }

        // ═══════════════════════════════════════════════════════
        // Registration
        // ═══════════════════════════════════════════════════════

        public void Register()
        {
            var events = ModServices.Helper.Events;

            events.GameLoop.UpdateTicked += OnUpdateTicked;
            events.Display.RenderedHud += OnRenderedHud;
        }

        // ═══════════════════════════════════════════════════════
        // Event Handlers
        // ═══════════════════════════════════════════════════════

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Update Squad button
            _integrations.Squad?.Update(e.Ticks);

        }

        private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
        {
            if (!ShouldRender())
                return;

            // Draw Squad button
            _integrations.Squad?.Draw(e.SpriteBatch);

            // Add more render handlers here
        }

        // ═══════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════

        private static bool ShouldRender()
        {
            if (!Context.IsWorldReady)
                return false;

            if (Game1.eventUp)
                return false;

            if (Game1.activeClickableMenu != null)
                return false;

            return true;
        }
    }
}