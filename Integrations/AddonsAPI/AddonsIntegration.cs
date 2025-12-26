using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.Base;
using StardewModdingAPI;

namespace MobileUISupport.Integrations.AddonsAPI
{
    /// <summary>
    /// Integration dengan AddonsMobile mod.
    /// Menyediakan wrapper untuk registrasi button.
    /// </summary>
    public class AddonsAPIIntegration : BaseIntegration
    {
        // ═══════════════════════════════════════════════════════
        // Constants
        // ═══════════════════════════════════════════════════════

        // Ganti dengan Mod ID yang benar dari manifest.json AddonsMobile
        public const string ADDONS_MOD_ID = "IGrigori22.AddonsMobile";

        // ═══════════════════════════════════════════════════════
        // Properties - BaseIntegration
        // ═══════════════════════════════════════════════════════

        public override string ModId => ADDONS_MOD_ID;
        public override string DisplayName => "AddonsMobile";
        public override bool IsEnabled => true; // Selalu coba load jika tersedia

        // ═══════════════════════════════════════════════════════
        // Properties - API
        // ═══════════════════════════════════════════════════════

        public IMobileAddonsAPI? API { get; private set; }

        // Mod ID untuk registrasi button
        private string MyModId => ModServices.Manifest.UniqueID;

        // ═══════════════════════════════════════════════════════
        // Tracked Registrations
        // ═══════════════════════════════════════════════════════

        private readonly List<string> _registeredButtonIds = new();

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        protected override bool DoInitialize()
        {
            API = Helper.ModRegistry.GetApi<IMobileAddonsAPI>(ADDONS_MOD_ID);

            if (API == null)
            {
                Logger.Debug("AddonsMobile API not available");
                return false;
            }

            Logger.Info($"AddonsMobile API v{API.ApiVersion} connected");
            Logger.Debug($"Is Mobile Platform: {API.IsMobilePlatform}");

            return true;
        }

        public override void OnReturnedToTitle()
        {
            // Unregister semua button yang terdaftar oleh mod ini
            UnregisterAllButtons();
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Simple Registration
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Register button dengan method sederhana.
        /// </summary>
        public bool RegisterButton(
            string uniqueId,
            string displayName,
            Action onPressed,
            KeyCategory category = KeyCategory.Miscellaneous)
        {
            if (API == null)
            {
                Logger.Warn($"Cannot register button '{uniqueId}': AddonsAPI not available");
                return false;
            }

            try
            {
                // ← GANTI ke RegisterSimpleButton
                bool success = API.RegisterSimpleButton(
                    uniqueId: uniqueId,
                    modId: MyModId,
                    displayName: displayName,
                    onPress: onPressed,
                    category: category
                );


                if (success)
                {
                    _registeredButtonIds.Add(uniqueId);
                    Logger.Debug($"Registered button: {uniqueId}");
                }
                else
                {
                    Logger.Warn($"Failed to register button: {uniqueId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error registering button '{uniqueId}': {ex.Message}");
                return false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Builder Pattern
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Create button menggunakan builder pattern.
        /// </summary>
        public ButtonBuilderWrapper? CreateButton(string uniqueId)
        {
            if (API == null)
            {
                Logger.Warn($"Cannot create button '{uniqueId}': AddonsAPI not available");
                return null;
            }

            var builder = API.CreateButton(uniqueId, MyModId);
            return new ButtonBuilderWrapper(builder, uniqueId, _registeredButtonIds);
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Unregistration
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Unregister button tertentu.
        /// </summary>
        public bool UnregisterButton(string uniqueId)
        {
            if (API == null)
                return false;

            bool success = API.UnregisterButton(uniqueId);

            if (success)
            {
                _registeredButtonIds.Remove(uniqueId);
                Logger.Debug($"Unregistered button: {uniqueId}");
            }

            return success;
        }

        /// <summary>
        /// Unregister semua button yang didaftarkan oleh mod ini.
        /// </summary>
        public void UnregisterAllButtons()
        {
            if (API == null)
                return;

            // ← Sekarang return int
            int count = API.UnregisterAllFromMod(MyModId);
            _registeredButtonIds.Clear();

            if (count > 0)
            {
                Logger.Debug($"Unregistered {count} buttons");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Public Methods - Button Control
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Enable/disable button.
        /// </summary>
        public bool SetButtonEnabled(string uniqueId, bool enabled)
        {
            return API?.SetButtonEnabled(uniqueId, enabled) ?? false;
        }

        /// <summary>
        /// Trigger button secara programmatic.
        /// </summary>
        public bool TriggerButton(string uniqueId)
        {
            return API?.TriggerButton(uniqueId) ?? false;
        }

        /// <summary>
        /// Check apakah button sudah terdaftar.
        /// </summary>
        public bool IsButtonRegistered(string uniqueId)
        {
            return API?.IsButtonRegistered(uniqueId) ?? false;
        }

        /// <summary>
        /// Get jumlah button terdaftar (semua mod).
        /// </summary>
        public int GetRegisteredButtonCount()
        {
            return API?.GetRegisteredButtonCount() ?? 0;
        }

        /// <summary>
        /// Refresh UI setelah perubahan.
        /// </summary>
        public void RefreshUI()
        {
            API?.RefreshUI();
        }

        /// <summary>
        /// Set visibility FAB.
        /// </summary>
        public void SetVisible(bool visible)
        {
            API?.SetVisible(visible);
        }
    }

    // ═══════════════════════════════════════════════════════
    // Builder Wrapper
    // ═══════════════════════════════════════════════════════

    /// <summary>
    /// Wrapper untuk IButtonBuilder dengan auto-tracking.
    /// </summary>
    public class ButtonBuilderWrapper
    {
        private readonly IButtonBuilder _builder;
        private readonly string _uniqueId;
        private readonly List<string> _registeredIds;

        internal ButtonBuilderWrapper(
            IButtonBuilder builder,
            string uniqueId,
            List<string> registeredIds)
        {
            _builder = builder;
            _uniqueId = uniqueId;
            _registeredIds = registeredIds;
        }

        public ButtonBuilderWrapper WithDisplayName(string name)
        {
            _builder.WithDisplayName(name);
            return this;
        }

        public ButtonBuilderWrapper WithDescription(string description)
        {
            _builder.WithDescription(description);
            return this;
        }

        public ButtonBuilderWrapper WithCategory(KeyCategory category)
        {
            _builder.WithCategory(category);
            return this;
        }

        public ButtonBuilderWrapper WithIcon(Texture2D texture, Rectangle? sourceRect = null)
        {
            _builder.WithIcon(texture, sourceRect);
            return this;
        }

        public ButtonBuilderWrapper WithTintColor(Color color)
        {
            _builder.WithTint(color);
            return this;
        }

        public ButtonBuilderWrapper WithPriority(int priority)
        {
            _builder.WithPriority(priority);
            return this;
        }

        public ButtonBuilderWrapper WithCooldown(int milliseconds)
        {
            _builder.WithCooldown(milliseconds);
            return this;
        }

        public ButtonBuilderWrapper WithVisibilityCondition(Func<bool> condition)
        {
            _builder.WithVisibilityCondition(condition);
            return this;
        }

        public ButtonBuilderWrapper WithOriginalKeybind(string keybind)
        {
            _builder.WithKeybind(keybind);
            return this;
        }

        public ButtonBuilderWrapper OnPressed(Action action)
        {
            _builder.OnPress(action);
            return this;
        }

        public ButtonBuilderWrapper OnHeld(Action<float> action)
        {
            _builder.OnHold(action);
            return this;
        }

        /// <summary>
        /// Register button dan track ID.
        /// </summary>
        public bool Register()
        {
            bool success = _builder.Register();

            if (success)
            {
                _registeredIds.Add(_uniqueId);
                Logger.Debug($"Registered button via builder: {_uniqueId}");
            }
            else
            {
                Logger.Warn($"Failed to register button via builder: {_uniqueId}");
            }

            return success;
        }
    }
}