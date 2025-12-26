using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MobileUISupport.Integrations.AddonsAPI
{
    // ═══════════════════════════════════════════════════════════════════════════
    // ENUMS - Harus sama persis
    // ═══════════════════════════════════════════════════════════════════════════

    public enum KeyCategory
    {
        Menu,
        Farming,
        Tools,
        Cheats,
        Information,
        Social,
        Inventory,
        Teleport,
        Miscellaneous
    }

    public enum ButtonType
    {
        Momentary,
        Toggle,
        Hold
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IMobileAddonsAPI - HARUS EXACT MATCH dengan AddonsMobile.API.IMobileAddonsAPI
    // ═══════════════════════════════════════════════════════════════════════════

    public interface IMobileAddonsAPI
    {
        // ═══════════════════════════════════════════════════════════════════════
        // METADATA
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Versi API (format: "major.minor.patch")</summary>
        string ApiVersion { get; }  // ← BUKAN "Version"!

        /// <summary>Apakah berjalan di platform mobile</summary>
        bool IsMobilePlatform { get; }

        // ═══════════════════════════════════════════════════════════════════════
        // BUTTON REGISTRATION (Simple)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Register button sederhana</summary>
        bool RegisterSimpleButton(  // ← BUKAN "RegisterButton"!
            string uniqueId,
            string modId,
            string displayName,
            Action onPress,
            KeyCategory category = KeyCategory.Miscellaneous
        );

        // ═══════════════════════════════════════════════════════════════════════
        // BUTTON REGISTRATION (Builder Pattern)
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Create button builder</summary>
        IButtonBuilder CreateButton(string uniqueId, string modId);

        // ═══════════════════════════════════════════════════════════════════════
        // BUTTON MANAGEMENT
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Unregister button</summary>
        bool UnregisterButton(string uniqueId);

        /// <summary>Unregister semua button dari mod tertentu</summary>
        int UnregisterAllFromMod(string modId);  // ← Return INT, bukan void!

        /// <summary>Enable/disable button</summary>
        bool SetButtonEnabled(string uniqueId, bool enabled);

        /// <summary>Set toggle state</summary>
        bool SetToggleState(string uniqueId, bool toggled, bool invokeCallback = false);

        // ═══════════════════════════════════════════════════════════════════════
        // BUTTON TRIGGERING
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Trigger button secara programmatic</summary>
        bool TriggerButton(string uniqueId);

        // ═══════════════════════════════════════════════════════════════════════
        // QUERIES
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Cek apakah button terdaftar</summary>
        bool IsButtonRegistered(string uniqueId);

        /// <summary>Get jumlah total button terdaftar</summary>
        int GetRegisteredButtonCount();

        /// <summary>Get jumlah button dari mod tertentu</summary>
        int GetButtonCountForMod(string modId);

        // ═══════════════════════════════════════════════════════════════════════
        // UI CONTROL
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Refresh UI</summary>
        void RefreshUI();

        /// <summary>Set visibility</summary>
        void SetVisible(bool visible);

        /// <summary>Apakah UI sedang visible</summary>
        bool IsVisible { get; }

        // ═══════════════════════════════════════════════════════════════════════
        // EVENTS (Optional - boleh tidak digunakan)
        // ═══════════════════════════════════════════════════════════════════════

        // Note: Events dengan custom EventArgs mungkin tidak bisa di-proxy
        // Jika error, hapus bagian ini
        // event EventHandler<ButtonRegisteredEventArgs> ButtonRegistered;
        // event EventHandler<ButtonUnregisteredEventArgs> ButtonUnregistered;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // IButtonBuilder - HARUS EXACT MATCH dengan AddonsMobile.API.IButtonBuilder
    // ═══════════════════════════════════════════════════════════════════════════

    public interface IButtonBuilder
    {
        // ═══════════════════════════════════════════════════════════
        // BASIC PROPERTIES
        // ═══════════════════════════════════════════════════════════

        IButtonBuilder WithDisplayName(string name);
        IButtonBuilder WithDescription(string description);
        IButtonBuilder WithCategory(KeyCategory category);
        IButtonBuilder WithPriority(int priority);
        IButtonBuilder WithKeybind(string keybind);  // ← BUKAN "WithOriginalKeybind"!

        // ═══════════════════════════════════════════════════════════
        // VISUAL CUSTOMIZATION
        // ═══════════════════════════════════════════════════════════

        IButtonBuilder WithIcon(Texture2D texture, Rectangle? sourceRect = null);
        IButtonBuilder WithTint(Color normalColor, Color? toggledColor = null);  // ← BUKAN "WithTintColor"!

        // ═══════════════════════════════════════════════════════════
        // BEHAVIOR
        // ═══════════════════════════════════════════════════════════

        IButtonBuilder WithType(ButtonType type);
        IButtonBuilder WithCooldown(int milliseconds);
        IButtonBuilder WithVisibilityCondition(Func<bool> condition);
        IButtonBuilder WithEnabledCondition(Func<bool> condition);

        // ═══════════════════════════════════════════════════════════
        // ACTIONS
        // ═══════════════════════════════════════════════════════════

        IButtonBuilder OnPress(Action action);              // ← BUKAN "OnPressed"!
        IButtonBuilder OnHold(Action<float> action);        // ← BUKAN "OnHeld(Action)"!
        IButtonBuilder OnRelease(Action action);
        IButtonBuilder OnToggle(Action<bool> action);

        // ═══════════════════════════════════════════════════════════
        // FINALIZATION
        // ═══════════════════════════════════════════════════════════

        bool Register();
    }
}