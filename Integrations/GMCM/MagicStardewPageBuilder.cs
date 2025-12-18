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
            //AddAdvancedSection(api, manifest, config);
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
                getValue: () => config.MagicStardew.SpellIconSize,
                setValue: v => config.MagicStardew.SpellIconSize = v,
                name: () => "Spell Icon Size",
                tooltip: () => "Size of spell icons in pixels (32-192)",
                min: 32, max: 192, interval: 8
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.GridColumns,
                setValue: v => config.MagicStardew.GridColumns = v,
                name: () => "Grid Columns",
                tooltip: () => "Number of columns in the spell grid (3-8)",
                min: 3, max: 8, interval: 1
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.GridRows,
                setValue: v => config.MagicStardew.GridRows = v,
                name: () => "Grid Rows",
                tooltip: () => "Number of rows per page (2-6)",
                min: 2, max: 6, interval: 1
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.IconSpacing,
                setValue: v => config.MagicStardew.IconSpacing = v,
                name: () => "Icon Spacing",
                tooltip: () => "Space between spell icons in pixels (4-48)",
                min: 4, max: 48, interval: 2
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.MenuPadding,
                setValue: v => config.MagicStardew.MenuPadding = v,
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

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.MagicStardew.EnableSupport,
                setValue: v => config.MagicStardew.EnableSupport = v,
                name: () => "Enable Support",
                tooltip: () => "Disable mobile UI and use the original Magic Stardew menu (requires restart)"
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.MagicStardew.HideButton,
                setValue: v => config.MagicStardew.HideButton = v,
                name: () => "Hide Button",
                tooltip: () => "Hide Button on Addons Menu Bar"
            );

            api.AddTextOption(
                mod: manifest,
                getValue: () => config.MagicStardew.ThemeColor,
                setValue: v => config.MagicStardew.ThemeColor = v,
                name: () => "Theme Color",
                tooltip: () => "Color theme for the menu",
                allowedValues: new[] { "default", "dark", "light", "blue", "green", "purple" },
                formatAllowedValue: FormatThemeName
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.MagicStardew.ShowSelectionAnimation,
                setValue: v => config.MagicStardew.ShowSelectionAnimation = v,
                name: () => "Selection Animation",
                tooltip: () => "Show pulsing animation on selected spell"
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.AnimationSpeed,
                setValue: v => config.MagicStardew.AnimationSpeed = v,
                name: () => "Animation Speed",
                tooltip: () => "Speed of the selection animation (0.5-3.0)",
                min: 0.5f, max: 3.0f, interval: 0.1f
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.BackgroundOpacity,
                setValue: v => config.MagicStardew.BackgroundOpacity = v,
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
                getValue: () => config.MagicStardew.CloseAfterCast,
                setValue: v => config.MagicStardew.CloseAfterCast = v,
                name: () => "Close After Cast",
                tooltip: () => "Automatically close menu after casting a spell"
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.CloseDelay,
                setValue: v => config.MagicStardew.CloseDelay = v,
                name: () => "Close Delay (ms)",
                tooltip: () => "Delay before closing menu after cast (100-1000ms)",
                min: 100, max: 1000, interval: 50
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.MagicStardew.ShowTooltips,
                setValue: v => config.MagicStardew.ShowTooltips = v,
                name: () => "Show Tooltips",
                tooltip: () => "Display tooltips when hovering over spells"
            );

            api.AddBoolOption(
                mod: manifest,
                getValue: () => config.MagicStardew.ConfirmHighManaCast,
                setValue: v => config.MagicStardew.ConfirmHighManaCast = v,
                name: () => "Confirm High Mana Cast",
                tooltip: () => "Ask for confirmation before casting expensive spells"
            );

            api.AddNumberOption(
                mod: manifest,
                getValue: () => config.MagicStardew.HighManaThreshold,
                setValue: v => config.MagicStardew.HighManaThreshold = v,
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
                getValue: () => config.MagicStardew.EnableSounds,
                setValue: v => config.MagicStardew.EnableSounds = v,
                name: () => "Enable Sounds",
                tooltip: () => "Play sound effects for menu actions"
            );
        }

        // ═══════════════════════════════════════════════════════
        // Advanced Section
        // ═══════════════════════════════════════════════════════

        //private static void AddAdvancedSection(IGenericModConfigMenuApi api, IManifest manifest, ModConfig config)
        //{
        //    api.AddSectionTitle(manifest, () => "🔧 Advanced");

            
        //}

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