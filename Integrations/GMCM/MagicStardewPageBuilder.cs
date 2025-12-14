using StardewModdingAPI;

namespace MobileUISupport.Integrations.GMCM
{
    /// <summary>
    /// GMCM page builder untuk Magic Stardew settings.
    /// </summary>
    public class MagicStardewPageBuilder : IGMCMPageBuilder
    {
        public string PageId => "MagicStardew";
        public string PageTitle => "🔮 Magic Stardew";
        public string PageTooltip => "Configurations for Magic Stardew Mod";

        public void Build(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            // Create page
            api.AddPage(manifest, PageId, () => "Magic Stardew");

            // Header
            AddHeader(api, manifest);

            // Sections
            AddLayoutSection(api, manifest, config);
            AddAppearanceSection(api, manifest, config);
            AddBehaviorSection(api, manifest, config);
            AddSoundSection(api, manifest, config);
            AddAdvancedSection(api, manifest, config);
        }

        // ═══════════════════════════════════════════════════════
        // Header
        // ═══════════════════════════════════════════════════════

        private static void AddHeader(IGenericModConfigMenuApi api, IManifest manifest)
        {
            api.AddSectionTitle(manifest, () => "━━━━━━━━━━ Magic Stardew ━━━━━━━━━━");
            api.AddParagraph(manifest,
                () => "Configure the mobile-friendly spell menu for Magic Stardew mod.");
        }

        // ═══════════════════════════════════════════════════════
        // Layout Section
        // ═══════════════════════════════════════════════════════

        private static void AddLayoutSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "📐 Layout Settings");

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.SpellIconSize,
                setValue: v => config.SpellIconSize = v,
                name: () => "Spell Icon Size",
                tooltip: () => "Size of spell icons in pixels (32-192)",
                min: 32, max: 192, interval: 8
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.GridColumns,
                setValue: v => config.GridColumns = v,
                name: () => "Grid Columns",
                tooltip: () => "Number of columns in the spell grid (3-8)",
                min: 3, max: 8, interval: 1
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.GridRows,
                setValue: v => config.GridRows = v,
                name: () => "Grid Rows",
                tooltip: () => "Number of rows per page (2-6)",
                min: 2, max: 6, interval: 1
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.IconSpacing,
                setValue: v => config.IconSpacing = v,
                name: () => "Icon Spacing",
                tooltip: () => "Space between spell icons in pixels (4-48)",
                min: 4, max: 48, interval: 2
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MenuPadding,
                setValue: v => config.MenuPadding = v,
                name: () => "Menu Padding",
                tooltip: () => "Padding from menu edges in pixels (8-200)",
                min: 8, max: 200, interval: 4
            );
        }

        // ═══════════════════════════════════════════════════════
        // Appearance Section
        // ═══════════════════════════════════════════════════════

        private static void AddAppearanceSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "🎨 Appearance");

            api.AddTextOption(
                mod: manifest,
                getValue: () => config.ThemeColor,
                setValue: v => config.ThemeColor = v,
                name: () => "Theme Color",
                tooltip: () => "Color theme for the menu",
                allowedValues: new[] { "default", "dark", "light", "blue", "green", "purple" },
                formatAllowedValue: FormatThemeName
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.ShowSelectionAnimation,
                setValue: v => config.ShowSelectionAnimation = v,
                name: () => "Selection Animation",
                tooltip: () => "Show pulsing animation on selected spell"
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.AnimationSpeed,
                setValue: v => config.AnimationSpeed = v,
                name: () => "Animation Speed",
                tooltip: () => "Speed of the selection animation (0.5-3.0)",
                min: 0.5f, max: 3.0f, interval: 0.1f
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.BackgroundOpacity,
                setValue: v => config.BackgroundOpacity = v,
                name: () => "Background Opacity",
                tooltip: () => "Opacity of the background dimming (0.3-1.0)",
                min: 0.3f, max: 1.0f, interval: 0.1f
            );
        }

        // ═══════════════════════════════════════════════════════
        // Behavior Section
        // ═══════════════════════════════════════════════════════

        private static void AddBehaviorSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "⚙️ Behavior");

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.CloseAfterCast,
                setValue: v => config.CloseAfterCast = v,
                name: () => "Close After Cast",
                tooltip: () => "Automatically close menu after casting a spell"
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.CloseDelay,
                setValue: v => config.CloseDelay = v,
                name: () => "Close Delay (ms)",
                tooltip: () => "Delay before closing menu after cast (100-1000ms)",
                min: 100, max: 1000, interval: 50
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.ShowTooltips,
                setValue: v => config.ShowTooltips = v,
                name: () => "Show Tooltips",
                tooltip: () => "Display tooltips when hovering over spells"
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.ConfirmHighManaCast,
                setValue: v => config.ConfirmHighManaCast = v,
                name: () => "Confirm High Mana Cast",
                tooltip: () => "Ask for confirmation before casting expensive spells"
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.HighManaThreshold,
                setValue: v => config.HighManaThreshold = v,
                name: () => "High Mana Threshold",
                tooltip: () => "Mana cost that triggers confirmation (20-100)",
                min: 20, max: 100, interval: 5
            );
        }

        // ═══════════════════════════════════════════════════════
        // Sound Section
        // ═══════════════════════════════════════════════════════

        private static void AddSoundSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "🔊 Sound");

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.EnableSounds,
                setValue: v => config.EnableSounds = v,
                name: () => "Enable Sounds",
                tooltip: () => "Play sound effects for menu actions"
            );
        }

        // ═══════════════════════════════════════════════════════
        // Advanced Section
        // ═══════════════════════════════════════════════════════

        private static void AddAdvancedSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        {
            api.AddSectionTitle(manifest, () => "🔧 Advanced");

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.UseOriginalMenu,
                setValue: v => config.UseOriginalMenu = v,
                name: () => "Use Original Menu",
                tooltip: () => "Disable mobile UI and use the original Magic Stardew menu (requires restart)"
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.DebugMode,
                setValue: v => config.DebugMode = v,
                name: () => "Debug Mode",
                tooltip: () => "Show debug information and hitbox overlay"
            );
        }

        // ═══════════════════════════════════════════════════════
        // Formatters
        // ═══════════════════════════════════════════════════════

        private static string FormatThemeName(string value)
        {
            return value switch
            {
                "default" => "🟤 Default (Brown)",
                "dark" => "⚫ Dark",
                "light" => "⚪ Light",
                "blue" => "🔵 Blue",
                "green" => "🟢 Green",
                "purple" => "🟣 Purple",
                _ => value
            };
        }
    }
}