using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.AddonsAPI;
using MobileUISupport.Integrations.Base;
using MobileUISupport.UI;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;

namespace MobileUISupport.Integrations.StardewSquad
{
    /// <summary>
    /// Integration dengan The Stardew Squad mod.
    /// Menggunakan AddonsAPI jika tersedia, fallback ke SquadRecruitButton jika tidak
    /// </summary>
    public class StardewSquadIntegration : BaseIntegration
    {
        // ═══════════════════════════════════════════════════════
        // Constants - Button IDs
        // ═══════════════════════════════════════════════════════

        private const string BUTTON_ID_SQUAD_INTERACTIONS = "MobileUISupport.Squad.Interactions";
        private const string ICON_TEXTURE_SQUAD_INTERACTION = "assets/SquadRecruitButton.png";

        // ═══════════════════════════════════════════════════════
        // Properties - BaseIntegration Implementation
        // ═══════════════════════════════════════════════════════

        public override string ModId => StardewSquadAPI.MOD_ID;
        public override string DisplayName => "The Stardew Squad";
        public override bool IsEnabled => Config.StardewSquad.EnableSupport;

        // ═══════════════════════════════════════════════════════
        // Properties - Integration Specific
        // ═══════════════════════════════════════════════════════

        public StardewSquadAPI API { get; private set; }
        
        /// <summary>
        /// Fallback button - hanya digunakan jika AddonsAPI tidak tersedia.
        /// </summary>
        public SquadRecruitButton FallbackButton { get; private set; }

        // Reference ke AddonsAPI
        private AddonsAPIIntegration _addonsAPI;

        // Icon Texture
        private Texture2D _squadIcon;

        // Flag untuk tracking mode yang digunakan
        private bool _usingAddonsAPI;

        // ═══════════════════════════════════════════════════════
        // Properties - Public Status
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Apakah menggunakan AddonsAPI atau fallback button.
        /// </summary>
        public bool IsUsingAddonsAPI => _usingAddonsAPI;

        /// <summary>
        /// Apakah menggunakan fallback button.
        /// </summary>
        public bool IsUsingFallbackButton => !_usingAddonsAPI && FallbackButton != null;


        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        protected override bool DoInitialize()
        {
            API = new StardewSquadAPI();
            return API.Initialize();
        }

        /// <summary>
        /// Set reference ke AddonsAPI integration.
        /// Harus dipanggil sebelum OnSaveLoaded.
        /// </summary>
        public void SetAddonsAPI(AddonsAPIIntegration addonsAPI)
        {
            _addonsAPI = addonsAPI;
            Logger.Debug($"Addons reference set: {addonsAPI != null}");
        }

        public override void OnSaveLoaded()
        {
            if (!IsAvailable || API == null)
            {
                Logger.DebugOnly("Squad", "Integration not available");
                return;
            }

            // Reset state
            _usingAddonsAPI = false;
            FallbackButton = null;

            // Load Icon texture
            LoadButtonIcon();

            // Prioritas coba Addons dulu
            if (TryRegisterWithAddonsAPI())
            {
                Logger.Info("Squad button registered via AddonsAPI ✓");
                return;
            }

            // Fallback: Buat button bawaan
            CreateFallbackButton();
            Logger.Info("Squad button using fallback mode ✓");
        }

        public override void OnReturnedToTitle()
        {
            // Cleanup
            FallbackButton = null;
            _usingAddonsAPI = false;

            // Button AddonsAPI akan otomatis di-unregister
        }

        // ═══════════════════════════════════════════════════════
        // Button Registration - AddonsAPI
        // ═══════════════════════════════════════════════════════

        private void LoadButtonIcon()
        {
            try
            {
                _squadIcon = Helper.ModContent.Load<Texture2D>(ICON_TEXTURE_SQUAD_INTERACTION);
                Logger.Debug("Loaded custom squad interaction button icon");
            }
            catch
            {
                _squadIcon = null;
                Logger.Debug("Custom icon not found, will use default!");
            }
        }


        /// <summary>
        /// Coba register button ke AddonsAPI.
        /// </summary>
        /// <returns>True jika berhasil, false jika harus fallback.</returns>
        private bool TryRegisterWithAddonsAPI()
        {
            if (_addonsAPI == null || !_addonsAPI.IsAvailable)
            {
                // Check Addons API availabillity
                Logger.Debug("AddonsAPI not available, will use fallback button");
                return false;
            }

            // Create button builder
            var builder = _addonsAPI.CreateButton(BUTTON_ID_SQUAD_INTERACTIONS);
            if (builder == null)
            {
                Logger.Warn("Failed to create button builder for Squad Interactions, will use fallback button");
                return false;
            }

            // Configure button
            builder
                .WithDisplayName("Squad Interactions")
                .WithDescription("Recruit or dismiss nearby NPCs from your squad")
                .WithCategory(KeyCategory.Social)
                .WithPriority(99)
                .WithCooldown(300)
                .WithOriginalKeybind("S")
                .WithVisibilityCondition(CanShowInteractions)
                .OnPressed(OnSquadButtonPressed);

            // Add icon jika tersedia
            if (_squadIcon != null)
            {
                builder.WithIcon(_squadIcon, new Rectangle(0, 0, 24, 24));
            }

            // Tint color hijau untuk Squad theme
            builder.WithTintColor(new Color(100, 200, 150));

            // Register
            if (!builder.Register())
            {
                Logger.Warn("Failed to register Squad button to AddonsAPI, will use fallback");
                return false;
            }

            // Success!
            _usingAddonsAPI = true;
            _addonsAPI.RefreshUI();

            return true;
        }

        /// <summary>
        /// Buat fallback button jika AddonsAPI tidak tersedia.
        /// </summary>
        private void CreateFallbackButton()
        {
            if (!IsAvailable || API == null)
            {
                Logger.DebugOnly("Squad", "Integration not available, button not created");
                return;
            }

            // Constructor baru hanya butuh API
            FallbackButton = new SquadRecruitButton(API);

            Logger.Debug("Squad recruit button created ✓");
        }

        // ═══════════════════════════════════════════════════════
        // Visibility & Callbacks
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Kondisi untuk menampilkan Interaksi squad
        /// </summary>
        private bool CanShowInteractions()
        {
            // Harus in-game
            if (!Context.IsWorldReady)
                return false;

            // Tidak ada menu aktif
            if (Game1.activeClickableMenu != null)
                return false;

            // Tidak dalam event/cutscene
            if (Game1.eventUp)
                return false;

            // Tidak dalam dialog
            if (Game1.dialogueUp)
                return false;

            return true;
        }

        /// <summary>
        /// Callback saat button ditekan.
        /// </summary>
        private void OnSquadButtonPressed()
        {
            Logger.Debug("╔══════════════════════════════════════╗");
            Logger.Debug("║  CALLBACK FROM ADDONS API            ║");
            Logger.Debug($"║  _usingAddonsAPI: {_usingAddonsAPI,-18}║");
            Logger.Debug($"║  FallbackButton: {(FallbackButton != null ? "EXISTS" : "NULL"),-19}║");
            Logger.Debug("╚══════════════════════════════════════╝");
            TriggerNearbyNPCInteraction();
        }

        /// <summary>
        /// Trigger interaction dengan NPC terdekat.
        /// </summary>
        private void TriggerNearbyNPCInteraction()
        {
            if (API == null || !Context.IsWorldReady)
                return;

            var player = Game1.player;
            var location = player?.currentLocation;

            if (location == null)
            {
                Game1.playSound("cancel");
                return;
            }

            // Find nearest eligible NPC
            NPC nearestNPC = FindNearestEligibleNPC(location, player!);

            if (nearestNPC != null)
            {
                bool success = API.TriggerInteraction(nearestNPC);
                Game1.playSound(success ? "bigSelect" : "cancel");

                string status = API.IsNPCRecruited(nearestNPC) ? "dismissed from" : "recruited to";
                Logger.Debug($"Interaction with {nearestNPC.Name}: {(success ? status + " squad" : "failed")}");
            }
            else
            {
                Game1.playSound("cancel");

                // Optional: Show message to player
                if (Config.StardewSquad.ShowNoNPCNearbyMessage)
                {
                    Game1.addHUDMessage(new HUDMessage("No villager nearby to interact with", HUDMessage.error_type));
                }

                Logger.Debug("No eligible NPC nearby for squad interaction");
            }
        }

        /// <summary>
        /// Find NPC terdekat yang eligible untuk squad.
        /// </summary>
        private NPC FindNearestEligibleNPC(GameLocation location, Farmer player)
        {
            float radius = Config.StardewSquad.DetectionRadius;
            NPC nearest = null;
            float nearestDist = float.MaxValue;

            // Check villagers
            foreach (var npc in location.characters)
            {
                if (!IsEligibleNPC(npc))
                    continue;

                float dist = Vector2.Distance(player.Tile, npc.Tile);
                if (dist <= radius && dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = npc;
                }
            }

            // Check Pet
            var pet = player.getPet();
            if (pet != null && pet.currentLocation == location)
            {
                float dist = Vector2.Distance(player.Tile, pet.Tile);
                if (dist <= radius && dist < nearestDist)
                {
                    nearest = pet;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Check apakah NPC eligible untuk squad interaction.
        /// </summary>
        private static bool IsEligibleNPC(NPC npc)
        {
            if (npc == null)
                return false;

            // Harus villager atau pet
            if (!npc.IsVillager && npc is not Pet)
                return false;

            // Tidak invisible
            if (npc.IsInvisible)
                return false;

            // Bukan anak
            if (npc is Child)
                return false;

            return true;
        }

        // ═══════════════════════════════════════════════════════
        // Update & Draw - Conditional
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Update - hanya untuk fallback button.
        /// </summary>
        public void Update(uint ticks)
        {
            // Jika menggunakan AddonsAPI, tidak perlu update manual
            if (_usingAddonsAPI) return;

            FallbackButton?.Update(ticks);
        }

        /// <summary>
        /// Draw - hanya untuk fallback button.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Jika menggunakan addons API tidak perlu draw manual
            if (_usingAddonsAPI) return;
            FallbackButton?.Draw(spriteBatch);
        }

        // ═══════════════════════════════════════════════════════
        // Input Handling - Conditional
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Handle left click - hanya untuk fallback button.
        /// </summary>  
        public bool HandleLeftClick(ICursorPosition cursor)
        {
            // Jika menggunakan Addons API, Input ditangani addons API
            if (_usingAddonsAPI) return false;

            return FallbackButton?.ReceiveLeftClick(cursor) ?? false;
        }

        /// <summary>
        /// Handle left release - hanya untuk fallback button.
        /// </summary>
        public void HandleLeftRelease(ICursorPosition cursor)
        {
            if (_usingAddonsAPI) return;

            FallbackButton?.ReleaseLeftClick(cursor);
        }

        // ═══════════════════════════════════════════════════════
        // Button Control - Unified
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Enable/disable squad button.
        /// Bekerja untuk kedua mode (AddonsAPI dan fallback).
        /// </summary>
        public void SetSquadButtonEnabled(bool enabled)
        {
            if (_usingAddonsAPI)
            {
                _addonsAPI?.SetButtonEnabled(BUTTON_ID_SQUAD_INTERACTIONS, enabled);
            }
            // Note: Fallback button tidak punya built-in enable/disable
            // Bisa ditambahkan jika diperlukan.
        }

        /// <summary>
        /// Check apakah button sudah terdaftar/dibuat.
        /// </summary>
        public bool IsSquadButtonReady()
        {
            if (_usingAddonsAPI)
            {
                return _addonsAPI?.IsButtonRegistered(BUTTON_ID_SQUAD_INTERACTIONS) ?? false;
            }

            return FallbackButton != null;
        }

        // ═══════════════════════════════════════════════════════
        // Public API Wrappers
        // ═══════════════════════════════════════════════════════

        public bool TriggerInteraction(NPC npc) => API?.TriggerInteraction(npc) ?? false;
        public bool IsNPCRecruited(NPC npc) => API?.IsNPCRecruited(npc) ?? false;
        public int GetSquadCount() => API?.GetSquadCount() ?? 0;
        public int GetMaxSquadSize() => API?.GetMaxSquadSize() ?? 4;
        public bool IsSquadFull() => API?.IsSquadFull() ?? false;

        // ═══════════════════════════════════════════════════════
        // Debug Info
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Get debug info tentang mode yang digunakan.
        /// </summary>
        public string GetModeInfo()
        {
            if (_usingAddonsAPI)
                return "Using AddonsAPI";
            if (FallbackButton != null)
                return "Using Fallback button";
            return "Not initialized";
        }
    }
}