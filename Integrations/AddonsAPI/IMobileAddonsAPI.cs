using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MobileUISupport.Integrations.AddonsAPI
{
    /// <summary>
    /// Kategori button - HARUS match dengan AddonsMobile.Framework.KeyCategory
    /// </summary>
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

    /// <summary>
    /// Interface untuk AddonsMobile API.
    /// HARUS exact match dengan AddonsMobile.API.IMobileAddonsAPI
    /// </summary>
    public interface IMobileAddonsAPI
    {
        string Version { get; }
        bool IsMobilePlatform { get; }

        bool RegisterButton(
            string uniqueId,
            string modId,
            string displayName,
            string description,
            KeyCategory category,
            Action onPressed,
            Texture2D iconTexture = null!,
            Rectangle? iconSourceRect = null,
            int priority = 0,
            string originalKeybind = null!
        );

        IButtonBuilder CreateButton(string uniqueId, string modId);
        bool UnregisterButton(string uniqueId);
        void UnregisterAllFromMod(string modId);
        bool SetButtonEnabled(string uniqueId, bool enabled);
        bool TriggerButton(string uniqueId);
        int GetRegisteredButtonCount();
        bool IsButtonRegistered(string uniqueId);
        void RefreshUI();
        void SetVisible(bool visible);
    }

    /// <summary>
    /// Builder interface untuk button.
    /// HARUS exact match dengan AddonsMobile.API.IButtonBuilder
    /// </summary>
    public interface IButtonBuilder
    {
        IButtonBuilder WithDisplayName(string name);
        IButtonBuilder WithDescription(string description);
        IButtonBuilder WithCategory(KeyCategory category);
        IButtonBuilder WithIcon(Texture2D texture, Rectangle? sourceRect = null);
        IButtonBuilder WithTintColor(Color color);
        IButtonBuilder WithPriority(int priority);
        IButtonBuilder WithCooldown(int milliseconds);
        IButtonBuilder WithVisibilityCondition(Func<bool> condition);
        IButtonBuilder WithOriginalKeybind(string keybind);
        IButtonBuilder OnPressed(Action action);
        IButtonBuilder OnHeld(Action action);
        bool Register();
    }
}