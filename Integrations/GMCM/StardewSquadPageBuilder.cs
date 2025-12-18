using StardewModdingAPI;

namespace MobileUISupport.Integrations.GMCM
{
    /// <summary>
    /// GMCM page builder untuk Stardew Squad settings.
    /// </summary>
    public class StardewSquadPageBuilder : IGMCMPageBuilder
    {
        public string PageId => "StardewSquad";
        public string PageTitle => "👥 The Stardew Squad";
        public string PageTooltip => "Configurations for The Stardew Squad";

        public void Build(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            // Create page
            api.AddPage(manifest, PageId, () => "The Stardew Squad");

            // Header
            AddHeader(api, manifest);

            // Sections
            AddGeneralSection(api, manifest, config);
            AddPositionSection(api, manifest, config);
            AddAppearanceSection(api, manifest, config);
        }

        // ═══════════════════════════════════════════════════════
        // Header
        // ═══════════════════════════════════════════════════════

        private static void AddHeader(IGenericModConfigMenuApi api, IManifest manifest)
        {
            api.AddSectionTitle(manifest, () => "━━━━━━━━━━ The Stardew Squad ━━━━━━━━━━");
            api.AddParagraph(manifest,
                () => "Configure the mobile recruit button for The Stardew Squad mod.");
        }

        // ═══════════════════════════════════════════════════════
        // General Section
        // ═══════════════════════════════════════════════════════

        private static void AddGeneralSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "⚙️ General");

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.StardewSquad.EnableSupport,
                setValue: v => config.StardewSquad.EnableSupport = v,
                name: () => "Enable Squad Button",
                tooltip: () => "Show recruit/dismiss button for The Stardew Squad mod"
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.StardewSquad.DetectionRadius,
                setValue: v => config.StardewSquad.DetectionRadius = v,
                name: () => "Detection Radius",
                tooltip: () => "How close to NPC to enable button (in tiles)",
                min: 1f, max: 10f, interval: 0.5f
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.StardewSquad.ShowNPCName,
                setValue: v => config.StardewSquad.ShowNPCName = v,
                name: () => "Show NPC Name",
                tooltip: () => "Display nearby NPC name above the button"
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.StardewSquad.ShowButtonOnlyWhenNearNPC,
                setValue: v => config.StardewSquad.ShowButtonOnlyWhenNearNPC = v,
                name: () => "Hide When No NPC Nearby",
                tooltip: () => "Only show button when a recruitable NPC is within range"
            );
        }

        // ═══════════════════════════════════════════════════════
        // Position Section
        // ═══════════════════════════════════════════════════════

        private static void AddPositionSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "📍 Button Position");

            api.AddParagraph(manifest,
                () => "The button position is relative to the anchor point. " +
                      "This ensures the button stays in place when zooming or resizing the window.");

            api.AddTextOption(
                mod: manifest,
                getValue: () => config.StardewSquad.ButtonAnchor.ToString(),
                setValue: v => config.StardewSquad.ButtonAnchor = Enum.Parse<ButtonAnchor>(v),
                name: () => "Anchor Point",
                tooltip: () => "Which corner/edge of the screen to anchor the button to",
                allowedValues: Enum.GetNames<ButtonAnchor>(),
                formatAllowedValue: FormatAnchorName
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.StardewSquad.ButtonOffsetX,
                setValue: v => config.StardewSquad.ButtonOffsetX = v,
                name: () => "Horizontal Offset",
                tooltip: () => "Distance from the anchor edge in pixels (horizontal)",
                min: 0, max: 500, interval: 10
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.StardewSquad.ButtonOffsetY,
                setValue: v => config.StardewSquad.ButtonOffsetY = v,
                name: () => "Vertical Offset",
                tooltip: () => "Distance from the anchor edge in pixels (vertical)",
                min: -300, max: 500, interval: 10
            );
        }

        // ═══════════════════════════════════════════════════════
        // Appearance Section
        // ═══════════════════════════════════════════════════════

        private static void AddAppearanceSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "🎨 Appearance");

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.StardewSquad.ButtonScale,
                setValue: v => config.StardewSquad.ButtonScale = v,
                name: () => "Button Scale",
                tooltip: () => "Size multiplier for the button (1.0 = 64px base size)",
                min: 0.5f, max: 3.0f, interval: 0.25f
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.StardewSquad.ButtonOpacity,
                setValue: v => config.StardewSquad.ButtonOpacity = v,
                name: () => "Button Opacity",
                tooltip: () => "Transparency of the button (0.3 = very transparent, 1.0 = solid)",
                min: 0.3f, max: 1.0f, interval: 0.05f
            );
        }

        // ═══════════════════════════════════════════════════════
        // Formatters
        // ═══════════════════════════════════════════════════════

        private static string FormatAnchorName(string value)
        {
            return value switch
            {
                "TopLeft" => "↖️ Top Left",
                "TopRight" => "↗️ Top Right",
                "BottomLeft" => "↙️ Bottom Left",
                "BottomRight" => "↘️ Bottom Right",
                "CenterLeft" => "⬅️ Center Left",
                "CenterRight" => "➡️ Center Right",
                _ => value
            };
        }
    }
}