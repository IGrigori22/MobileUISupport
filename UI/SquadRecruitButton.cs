using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.StardewSquad;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;

namespace MobileUISupport.UI
{
    /// <summary>
    /// Tombol untuk recruit/dismiss NPC dari The Stardew Squad.
    /// Menggunakan ModServices untuk akses dependencies.
    /// </summary>
    public class SquadRecruitButton
    {
        // ═══════════════════════════════════════════════════════
        // Constants
        // ═══════════════════════════════════════════════════════

        private const int BASE_BUTTON_SIZE = 64;
        private const string ICON_ASSET_PATH = "assets/SquadRecruitButton.png";

        // ═══════════════════════════════════════════════════════
        // Fields - Dependencies
        // ═══════════════════════════════════════════════════════

        private readonly StardewSquadAPI _api;

        // ═══════════════════════════════════════════════════════
        // Fields - Textures
        // ═══════════════════════════════════════════════════════

        private Texture2D? _customIconTexture;
        private bool _useCustomIcon;

        // ═══════════════════════════════════════════════════════
        // Fields - State
        // ═══════════════════════════════════════════════════════

        private NPC? _targetNPC;
        private bool _isPressed;
        private bool _isHeldDown;
        private uint _lastDetectionTick;
        private bool _isEnabled;
        private bool _isTargetRecruited;
        private float _pulseTimer;
        private Rectangle _bounds;

        // ═══════════════════════════════════════════════════════
        // Properties - Shortcuts
        // ═══════════════════════════════════════════════════════

        private static ModConfig Config => ModServices.Config;
        private static IModHelper Helper => ModServices.Helper;

        // ═══════════════════════════════════════════════════════
        // Properties - Public
        // ═══════════════════════════════════════════════════════

        public Rectangle Bounds => _bounds;
        public NPC? TargetNPC => _targetNPC;
        public bool IsVisible => Config.EnableSquadSupport && _api.IsInitialized;
        public bool IsEnabled => _isEnabled && _targetNPC != null;

        // ═══════════════════════════════════════════════════════
        // Constructor
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Constructor utama menggunakan StardewSquadAPI.
        /// </summary>
        public SquadRecruitButton(StardewSquadAPI api)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));

            LoadCustomIcon();
            UpdateBounds();
        }

        /// <summary>
        /// Constructor untuk backward compatibility.
        /// </summary>
        [Obsolete("Use constructor with StardewSquadAPI instead")]
        public SquadRecruitButton(
            IModHelper helper,
            IMonitor monitor,
            ModConfig config,
            StardewSquadIntegration squad)
            : this(squad.API!)
        {
            // Legacy constructor - semua dependencies diambil dari ModServices
        }

        // ═══════════════════════════════════════════════════════
        // Initialization
        // ═══════════════════════════════════════════════════════

        private void LoadCustomIcon()
        {
            try
            {
                _customIconTexture = Helper.ModContent.Load<Texture2D>(ICON_ASSET_PATH);
                _useCustomIcon = true;
                Logger.Trace("Custom squad button icon loaded");
            }
            catch
            {
                _useCustomIcon = false;
                Logger.Trace("Using fallback squad button icon");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Bounds & Position
        // ═══════════════════════════════════════════════════════

        private void UpdateBounds()
        {
            int viewWidth = Game1.uiViewport.Width;
            int viewHeight = Game1.uiViewport.Height;
            int buttonSize = (int)(BASE_BUTTON_SIZE * Config.SquadButtonScale);

            var position = CalculatePosition(viewWidth, viewHeight, buttonSize);

            // Clamp to viewport
            int x = Math.Clamp(position.X, 0, Math.Max(0, viewWidth - buttonSize));
            int y = Math.Clamp(position.Y, 0, Math.Max(0, viewHeight - buttonSize));

            _bounds = new Rectangle(x, y, buttonSize, buttonSize);
        }

        private Point CalculatePosition(int viewWidth, int viewHeight, int buttonSize)
        {
            int offsetX = Config.SquadButtonOffsetX;
            int offsetY = Config.SquadButtonOffsetY;

            return Config.SquadButtonAnchor switch
            {
                ButtonAnchor.TopLeft => new Point(offsetX, offsetY),
                ButtonAnchor.TopRight => new Point(viewWidth - buttonSize - offsetX, offsetY),
                ButtonAnchor.BottomLeft => new Point(offsetX, viewHeight - buttonSize - offsetY),
                ButtonAnchor.BottomRight => new Point(viewWidth - buttonSize - offsetX, viewHeight - buttonSize - offsetY),
                ButtonAnchor.CenterLeft => new Point(offsetX, (viewHeight - buttonSize) / 2 + offsetY),
                ButtonAnchor.CenterRight => new Point(viewWidth - buttonSize - offsetX, (viewHeight - buttonSize) / 2 + offsetY),
                _ => new Point(viewWidth - buttonSize - offsetX, (viewHeight - buttonSize) / 2 + offsetY)
            };
        }

        // ═══════════════════════════════════════════════════════
        // Input Handling
        // ═══════════════════════════════════════════════════════

        /// <summary>
        /// Handle left click - menggunakan ModifyCoordinatesForUIScale seperti VirtualKeyboard.
        /// </summary>
        public bool ReceiveLeftClick(ICursorPosition cursor)
        {
            // Konversi koordinat untuk UI scale
            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(cursor.ScreenPixels);
            int x = (int)screenPixels.X;
            int y = (int)screenPixels.Y;

            if (_bounds.Contains(x, y))
            {
                _isHeldDown = true;
                OnPress();

                Logger.DebugOnly("SquadButton",
                    $"Click at ({x},{y}) - Bounds: {_bounds} → HIT!");

                return true;
            }

            Logger.DebugOnly("SquadButton",
                $"Click at ({x},{y}) - Bounds: {_bounds} → MISS");

            return false;
        }

        /// <summary>
        /// Handle left click release.
        /// </summary>
        public void ReleaseLeftClick(ICursorPosition cursor)
        {
            if (!_isHeldDown)
                return;

            Vector2 screenPixels = Utility.ModifyCoordinatesForUIScale(cursor.ScreenPixels);
            int x = (int)screenPixels.X;
            int y = (int)screenPixels.Y;

            _isHeldDown = false;

            if (_bounds.Contains(x, y))
            {
                OnRelease();
            }
            else
            {
                OnCancel();
            }
        }

        /// <summary>
        /// Check apakah mouse sedang hover di atas tombol.
        /// </summary>
        public bool IsMouseOver()
        {
            return _bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
        }

        private void OnPress()
        {
            if (!_isEnabled)
            {
                Game1.playSound("cancel");
                return;
            }

            _isPressed = true;
            Game1.playSound("smallSelect");
        }

        private void OnRelease()
        {
            if (!_isPressed)
                return;

            _isPressed = false;

            if (!_isEnabled || _targetNPC == null)
                return;

            TriggerSquadInteraction();
        }

        public void OnCancel()
        {
            _isPressed = false;
            _isHeldDown = false;
        }

        // ═══════════════════════════════════════════════════════
        // Update & Draw
        // ═══════════════════════════════════════════════════════

        public void Update(uint ticks)
        {
            if (!_api.IsInitialized)
                return;

            // Update bounds setiap frame
            UpdateBounds();

            // Detect NPC setiap 15 ticks
            if (ticks - _lastDetectionTick >= 15)
            {
                _lastDetectionTick = ticks;
                DetectNearbyNPC();
            }

            // Update pulse animation
            _pulseTimer += 0.1f;
            if (_pulseTimer > MathHelper.TwoPi)
                _pulseTimer = 0;
        }

        public void Draw(SpriteBatch b)
        {
            if (!IsVisible)
                return;

            if (Config.ShowButtonOnlyWhenNearNPC && !_isEnabled)
                return;

            bool isHovered = IsMouseOver();

            DrawButtonBackground(b, isHovered);
            DrawButtonIcon(b);

            if (_isEnabled && _targetNPC != null && Config.ShowNPCName)
            {
                DrawNPCLabel(b);
            }

            DrawSquadCounter(b);

            if (Config.DebugMode)
            {
                DrawDebugOverlay(b, isHovered);
            }
        }

        // ═══════════════════════════════════════════════════════
        // NPC Detection
        // ═══════════════════════════════════════════════════════

        private void DetectNearbyNPC()
        {
            _targetNPC = null;
            _isEnabled = false;
            _isTargetRecruited = false;

            if (!Context.IsWorldReady || Game1.player?.currentLocation == null)
                return;

            var player = Game1.player;
            var location = player.currentLocation;
            float radius = Config.SquadDetectionRadius;

            NPC? nearest = null;
            float nearestDist = float.MaxValue;

            // Check NPCs
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
            var pet = Game1.player.getPet();
            if (pet != null && pet.currentLocation == location)
            {
                float dist = Vector2.Distance(player.Tile, pet.Tile);
                if (dist <= radius && dist < nearestDist)
                {
                    nearest = pet;
                }
            }

            if (nearest != null)
            {
                _targetNPC = nearest;
                _isEnabled = true;
                _isTargetRecruited = _api.IsNPCRecruited(nearest);
            }
        }

        private static bool IsEligibleNPC(NPC npc)
        {
            if (npc == null)
                return false;

            if (!npc.IsVillager && npc is not Pet)
                return false;

            if (npc.IsInvisible)
                return false;

            if (npc is Child)
                return false;

            return true;
        }

        // ═══════════════════════════════════════════════════════
        // Squad Interaction
        // ═══════════════════════════════════════════════════════

        private void TriggerSquadInteraction()
        {
            if (_targetNPC == null)
                return;

            Logger.Debug($"Triggering Squad interaction for {_targetNPC.Name}");

            bool success = _api.TriggerInteraction(_targetNPC);
            Game1.playSound(success ? "bigSelect" : "cancel");
        }

        // ═══════════════════════════════════════════════════════
        // Drawing - Background
        // ═══════════════════════════════════════════════════════

        private void DrawButtonBackground(SpriteBatch b, bool isHovered)
        {
            float baseOpacity = Config.SquadButtonOpacity;
            Color boxColor = GetButtonColor(isHovered, baseOpacity);

            int pressOffset = _isHeldDown ? 4 : 0;

            // Shadow
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                _bounds.X + 3, _bounds.Y + 3,
                _bounds.Width, _bounds.Height,
                Color.Black * 0.3f, 1f, false
            );

            // Button
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                _bounds.X - pressOffset, _bounds.Y + pressOffset,
                _bounds.Width, _bounds.Height,
                boxColor, 1f, false
            );
        }

        private Color GetButtonColor(bool isHovered, float baseOpacity)
        {
            if (_isPressed && _isEnabled)
                return Color.Gold * baseOpacity;

            if (isHovered && _isEnabled)
                return Color.LightGoldenrodYellow * baseOpacity;

            if (_isEnabled)
            {
                Color baseColor = _isTargetRecruited
                    ? new Color(100, 200, 150)
                    : new Color(120, 255, 120);

                // Pulse effect
                float pulse = 1f + MathF.Sin(_pulseTimer) * 0.08f;
                return new Color(
                    (int)Math.Min(255, baseColor.R * pulse),
                    (int)Math.Min(255, baseColor.G * pulse),
                    (int)Math.Min(255, baseColor.B * pulse),
                    (int)(baseColor.A * baseOpacity)
                );
            }

            return new Color(80, 80, 80) * (baseOpacity * 0.7f);
        }

        // ═══════════════════════════════════════════════════════
        // Drawing - Icon
        // ═══════════════════════════════════════════════════════

        private void DrawButtonIcon(SpriteBatch b)
        {
            float opacity = _isEnabled ? 1f : 0.4f;
            Color iconColor = (_isEnabled ? Color.White : Color.Gray) * opacity;
            int pressOffset = _isHeldDown ? 4 : 0;

            if (_useCustomIcon && _customIconTexture != null)
            {
                DrawCustomIcon(b, iconColor, pressOffset);
            }
            else
            {
                DrawFallbackIcon(b, iconColor, pressOffset);
            }

            // Draw status indicator
            if (_isEnabled)
            {
                DrawStatusIndicator(b);
            }
        }

        private void DrawCustomIcon(SpriteBatch b, Color color, int pressOffset)
        {
            int padding = 8;
            int availableSize = _bounds.Width - padding * 2;

            float iconScale = (float)availableSize / Math.Max(
                _customIconTexture!.Width,
                _customIconTexture.Height
            );

            int scaledWidth = (int)(_customIconTexture.Width * iconScale);
            int scaledHeight = (int)(_customIconTexture.Height * iconScale);

            var iconPos = new Vector2(
                _bounds.X + (_bounds.Width - scaledWidth) / 2f - pressOffset,
                _bounds.Y + (_bounds.Height - scaledHeight) / 2f + pressOffset
            );

            b.Draw(
                _customIconTexture,
                iconPos,
                null,
                color,
                0f,
                Vector2.Zero,
                iconScale,
                SpriteEffects.None,
                0.99f
            );
        }

        private void DrawFallbackIcon(SpriteBatch b, Color color, int pressOffset)
        {
            int iconSize = 16;
            float iconScale = (float)(_bounds.Width - 16) / iconSize;

            var iconPos = new Vector2(
                _bounds.X + (_bounds.Width - iconSize * iconScale) / 2f - pressOffset,
                _bounds.Y + (_bounds.Height - iconSize * iconScale) / 2f + pressOffset
            );

            b.Draw(
                Game1.mouseCursors,
                iconPos,
                new Rectangle(80, 0, 16, 16),
                color,
                0f,
                Vector2.Zero,
                iconScale,
                SpriteEffects.None,
                0.99f
            );
        }

        private void DrawStatusIndicator(SpriteBatch b)
        {
            string indicator = _isTargetRecruited ? "★" : "+";
            var font = Game1.tinyFont;
            var size = font.MeasureString(indicator);
            var pos = new Vector2(_bounds.Right - size.X - 4, _bounds.Y + 2);

            Color color = _isTargetRecruited ? Color.Gold : Color.LightGreen;

            // Shadow
            b.DrawString(font, indicator, pos + Vector2.One, Color.Black * 0.5f);
            // Text
            b.DrawString(font, indicator, pos, color);
        }

        // ═══════════════════════════════════════════════════════
        // Drawing - Labels
        // ═══════════════════════════════════════════════════════

        private void DrawNPCLabel(SpriteBatch b)
        {
            if (_targetNPC == null)
                return;

            string status = _isTargetRecruited ? "(In Squad)" : "(Available)";
            string text = $"{_targetNPC.displayName} {status}";

            var font = Game1.smallFont;
            var size = font.MeasureString(text);

            // Position above button
            var pos = new Vector2(
                _bounds.X + _bounds.Width / 2f - size.X / 2f,
                _bounds.Y - size.Y - 8
            );

            // Clamp to viewport
            pos.X = Math.Clamp(pos.X, 4, Game1.uiViewport.Width - size.X - 4);
            pos.Y = Math.Max(4, pos.Y);

            // Background
            b.Draw(
                Game1.staminaRect,
                new Rectangle((int)pos.X - 4, (int)pos.Y - 2, (int)size.X + 8, (int)size.Y + 4),
                Color.Black * 0.75f
            );

            // Text
            Color textColor = _isTargetRecruited ? Color.LightBlue : Color.LightGreen;
            b.DrawString(font, text, pos + Vector2.One, Color.Black * 0.5f);
            b.DrawString(font, text, pos, textColor);
        }

        private void DrawSquadCounter(SpriteBatch b)
        {
            int count = _api.GetSquadCount();
            int max = _api.GetMaxSquadSize();
            string text = $"{count}/{max}";

            var font = Game1.tinyFont;
            var size = font.MeasureString(text);
            var pos = new Vector2(_bounds.Right - size.X - 3, _bounds.Bottom - size.Y - 1);

            // Background
            Color bgColor = count >= max ? new Color(100, 50, 50) : new Color(50, 50, 50);
            b.Draw(
                Game1.staminaRect,
                new Rectangle((int)pos.X - 2, (int)pos.Y - 1, (int)size.X + 4, (int)size.Y + 2),
                bgColor * 0.9f
            );

            // Text
            Color textColor = count >= max
                ? Color.Salmon
                : count > 0
                    ? Color.LightGreen
                    : Color.Gray;

            b.DrawString(font, text, pos, textColor);
        }

        // ═══════════════════════════════════════════════════════
        // Drawing - Debug
        // ═══════════════════════════════════════════════════════

        private void DrawDebugOverlay(SpriteBatch b, bool isHovered)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // Bounds outline
            Color outlineColor = isHovered ? Color.Lime : Color.Red;
            DrawRectOutline(b, _bounds, outlineColor, 3);

            // Crosshair at mouse position
            b.Draw(Game1.staminaRect, new Rectangle(mouseX - 15, mouseY - 1, 30, 2), Color.Yellow);
            b.Draw(Game1.staminaRect, new Rectangle(mouseX - 1, mouseY - 15, 2, 30), Color.Yellow);

            // Debug panel
            DrawDebugPanel(b, mouseX, mouseY, isHovered);
        }

        private void DrawDebugPanel(SpriteBatch b, int mouseX, int mouseY, bool isHovered)
        {
            string[] lines =
            {
                "══════ SQUAD BUTTON DEBUG ══════",
                "",
                $"Bounds: {_bounds}",
                $"Mouse (Game1): ({mouseX}, {mouseY})",
                $"IsHovered: {isHovered}",
                $"IsHeldDown: {_isHeldDown}",
                $"IsEnabled: {_isEnabled}",
                $"TargetNPC: {_targetNPC?.Name ?? "None"}",
                "",
                $"uiViewport: {Game1.uiViewport.Width}x{Game1.uiViewport.Height}",
                $"Zoom: {Game1.options.zoomLevel:F2}",
                "",
                isHovered ? "✓ HOVER DETECTED" : "Move mouse to button"
            };

            var font = Game1.tinyFont;
            float lineHeight = font.MeasureString("A").Y;
            float panelX = 10, panelY = 10;

            // Calculate panel width
            float maxWidth = 0;
            foreach (var line in lines)
            {
                maxWidth = Math.Max(maxWidth, font.MeasureString(line).X);
            }

            // Background
            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)panelX - 5,
                    (int)panelY - 5,
                    (int)maxWidth + 10,
                    (int)(lines.Length * lineHeight) + 10
                ),
                Color.Black * 0.9f
            );

            // Lines
            for (int i = 0; i < lines.Length; i++)
            {
                Color color = GetDebugLineColor(lines[i]);
                b.DrawString(font, lines[i], new Vector2(panelX, panelY + i * lineHeight), color);
            }
        }

        private static Color GetDebugLineColor(string line)
        {
            if (line.Contains("✓"))
                return Color.Lime;
            if (line.Contains("═"))
                return Color.Cyan;
            if (line.Contains("True"))
                return Color.Yellow;
            return Color.White;
        }

        private static void DrawRectOutline(SpriteBatch b, Rectangle rect, Color color, int thickness)
        {
            // Top
            b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Bottom
            b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            b.Draw(Game1.staminaRect, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Right
            b.Draw(Game1.staminaRect, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }
    }
}