using MobileUISupport.Integrations;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MobileUISupport.Events
{
    /// <summary>
    /// Handle input events (button press/release).
    /// </summary>
    public class InputHandler
    {
        private readonly IntegrationManager _integrations;

        public InputHandler(IntegrationManager integrations)
        {
            _integrations = integrations;
        }

        // ═══════════════════════════════════════════════════════
        // Registration
        // ═══════════════════════════════════════════════════════

        public void Register()
        {
            var events = ModServices.Helper.Events.Input;

            events.ButtonPressed += OnButtonPressed;
            events.ButtonReleased += OnButtonReleased;
        }

        // ═══════════════════════════════════════════════════════
        // Event Handlers
        // ═══════════════════════════════════════════════════════

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!CanProcessInput())
                return;

            if (e.Button != SButton.MouseLeft)
                return;

            // Try Squad button first
            if (_integrations.Squad?.HandleLeftClick(e.Cursor) == true)
            {
                ModServices.Helper.Input.Suppress(e.Button);
                return;
            }

            // Add more button handlers here
            // if (_integrations.Magic?.HandleLeftClick(e.Cursor) == true)
            // {
            //     ModServices.Helper.Input.Suppress(e.Button);
            //     return;
            // }
        }

        private void OnButtonReleased(object? sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button != SButton.MouseLeft)
                return;

            _integrations.Squad?.HandleLeftRelease(e.Cursor);
        }

        // ═══════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════

        private static bool CanProcessInput()
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