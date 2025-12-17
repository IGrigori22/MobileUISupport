using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MobileUISupport;
using MobileUISupport.Integrations.MagicStardew;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MobileUISupport.UI
{
    /// <summary>
    /// Custom mobile-friendly spell menu
    /// </summary>
    public class MobileSpellMenu : IClickableMenu
    {
        /*********
        ** Dependencies
        *********/
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly ModConfig _config;
        private readonly MagicStardewAPI _api;

        /*********
        ** Spell Data
        *********/
        private readonly List<SpellData> _spells;
        private readonly Farmer _player;
        private readonly object _manaBar;
        private readonly Dictionary<int, SpellData> _favorites;

        /*********
        ** UI State
        *********/
        private int _selectedIndex = 0;
        private int _currentPage = 0;
        private int _spellsPerPage;
        private int _totalPages;
        private float _pulseTimer = 0f;
        private string _hoverText = "";
        private bool _isConfirmingCast = false;
        private SpellData? _pendingSpell = null;

        /*********
        ** UI Components
        *********/
        private List<ClickableComponent> _spellSlots = new();
        private ClickableTextureComponent? _prevPageButton;
        private ClickableTextureComponent? _nextPageButton;
        private ClickableTextureComponent? _closeButton;
        private ClickableComponent? _castButton;
        private List<ClickableComponent> _favoriteSlots = new();

        // Confirmation dialog buttons
        private ClickableComponent? _confirmYesButton;
        private ClickableComponent? _confirmNoButton;

        /*********
        ** Layout (from config)
        *********/
        private int IconSize => _config.SpellIconSize;
        private int IconSpacing => _config.IconSpacing;
        private int GridColumns => _config.GridColumns;
        private int GridRows => _config.GridRows;
        private int MenuPadding => _config.MenuPadding;

        /*********
        ** Calculated Areas
        *********/
        private Rectangle _gridArea;
        private Rectangle _infoPanel;
        private Rectangle _confirmDialog;

        /*********
        ** Theme Colors
        *********/
        private Color _titleColor;
        private Color _textColor;
        private Color _accentColor;
        private Color _highlightColor;

        /*********
        ** Constructor
        *********/
        public MobileSpellMenu(
            List<SpellData> spells,
            Farmer player,
            object manaBar,
            Dictionary<int, SpellData> favorites,
            IModHelper helper,
            IMonitor monitor,
            ModConfig config,
            MagicStardewAPI api)
            : base(0, 0, 0, 0, true)
        {
            _spells = spells ?? new List<SpellData>();
            _player = player;
            _manaBar = manaBar;
            _favorites = favorites ?? new Dictionary<int, SpellData>();
            _helper = helper;
            _monitor = monitor;
            _config = config;
            _api = api;

            // Calculate pagination
            _spellsPerPage = GridColumns * GridRows;
            _totalPages = Math.Max(1, (int)Math.Ceiling((double)_spells.Count / _spellsPerPage));

            // Setup theme colors
            ApplyTheme(_config.ThemeColor);

            // Calculate layout and create components
            CalculateLayout();
            CreateUIComponents();

            // Play open sound
            PlaySound("bigSelect");
        }

        /*********
        ** Theme System
        *********/

        /// <summary>
        /// Apply color theme based on config
        /// </summary>
        private void ApplyTheme(string themeName)
        {
            switch (themeName.ToLower())
            {
                case "dark":
                    _titleColor = new Color(200, 200, 200);
                    _textColor = new Color(220, 220, 220);
                    _accentColor = new Color(100, 100, 120);
                    _highlightColor = new Color(150, 150, 200);
                    break;

                case "light":
                    _titleColor = new Color(60, 40, 30);
                    _textColor = new Color(50, 50, 50);
                    _accentColor = new Color(200, 180, 150);
                    _highlightColor = new Color(255, 220, 150);
                    break;

                case "blue":
                    _titleColor = new Color(100, 150, 220);
                    _textColor = Color.White;
                    _accentColor = new Color(70, 100, 150);
                    _highlightColor = new Color(100, 180, 255);
                    break;

                case "green":
                    _titleColor = new Color(100, 180, 100);
                    _textColor = Color.White;
                    _accentColor = new Color(60, 120, 60);
                    _highlightColor = new Color(120, 220, 120);
                    break;

                case "purple":
                    _titleColor = new Color(180, 100, 220);
                    _textColor = Color.White;
                    _accentColor = new Color(100, 60, 130);
                    _highlightColor = new Color(200, 150, 255);
                    break;

                case "default":
                default:
                    _titleColor = new Color(86, 22, 12); // Dark brown
                    _textColor = Color.White;
                    _accentColor = new Color(180, 100, 50);
                    _highlightColor = Color.Gold;
                    break;
            }
        }

        /*********
        ** Layout Calculation
        *********/

        private void CalculateLayout()
        {
            int viewportWidth = Game1.uiViewport.Width;
            int viewportHeight = Game1.uiViewport.Height;

            // Calculate grid size based on config
            int gridWidth = (IconSize * GridColumns) + (IconSpacing * (GridColumns - 1));
            int gridHeight = (IconSize * GridRows) + (IconSpacing * (GridRows - 1));

            // Panel info width
            int panelWidth = Math.Max(200, (int)(viewportWidth * 0.25f));
            panelWidth = Math.Min(panelWidth, 280);

            // Total menu size
            int totalWidth = gridWidth + panelWidth + MenuPadding * 3 + 20;
            int totalHeight = gridHeight + 140; // header + footer

            // Clamp to viewport
            totalWidth = Math.Min(totalWidth, viewportWidth - 40);
            totalHeight = Math.Min(totalHeight, viewportHeight - 40);

            // Set menu bounds
            width = totalWidth;
            height = totalHeight;
            xPositionOnScreen = (viewportWidth - width) / 2;
            yPositionOnScreen = Math.Max(20, (viewportHeight - height) / 2);

            // Calculate sub-areas
            int headerHeight = 60;

            _gridArea = new Rectangle(
                xPositionOnScreen + MenuPadding,
                yPositionOnScreen + headerHeight,
                gridWidth + 24,
                gridHeight + 24
            );

            _infoPanel = new Rectangle(
                _gridArea.Right + 16,
                yPositionOnScreen + headerHeight,
                width - _gridArea.Width - MenuPadding * 2 - 16,
                gridHeight + 24
            );

            // Confirmation dialog (centered on screen)
            int dialogWidth = 350;
            int dialogHeight = 180;
            _confirmDialog = new Rectangle(
                (viewportWidth - dialogWidth) / 2,
                (viewportHeight - dialogHeight) / 2,
                dialogWidth,
                dialogHeight
            );

            if (_config.DebugMode)
            {
                _monitor.Log($"Menu layout: {width}x{height} at ({xPositionOnScreen}, {yPositionOnScreen})", LogLevel.Debug);
            }
        }

        private void CreateUIComponents()
        {
            _spellSlots.Clear();
            _favoriteSlots.Clear();

            // Close button (X)
            _closeButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + width - 52, yPositionOnScreen + 12, 44, 44),
                Game1.mouseCursors,
                new Rectangle(337, 494, 12, 12),
                3.5f
            );

            // Spell slots
            int startX = _gridArea.X + 12;
            int startY = _gridArea.Y + 12;

            for (int i = 0; i < _spellsPerPage; i++)
            {
                int row = i / GridColumns;
                int col = i % GridColumns;

                int slotX = startX + col * (IconSize + IconSpacing);
                int slotY = startY + row * (IconSize + IconSpacing);

                _spellSlots.Add(new ClickableComponent(
                    new Rectangle(slotX, slotY, IconSize, IconSize),
                    $"spell_{i}"
                ));
            }

            // Footer area
            int footerY = _gridArea.Bottom + 6;

            // Page buttons
            if (_totalPages > 1)
            {
                _prevPageButton = new ClickableTextureComponent(
                    new Rectangle(xPositionOnScreen + 30, footerY, 48, 44),
                    Game1.mouseCursors,
                    new Rectangle(352, 495, 12, 11),
                    4f
                );

                _nextPageButton = new ClickableTextureComponent(
                    new Rectangle(xPositionOnScreen + 140, footerY, 48, 44),
                    Game1.mouseCursors,
                    new Rectangle(365, 495, 12, 11),
                    4f
                );
            }

            // Cast button
            int castBtnWidth = 130;
            int castBtnHeight = 48;
            _castButton = new ClickableComponent(
                new Rectangle(
                    _infoPanel.X + (_infoPanel.Width - castBtnWidth) / 2,
                    _infoPanel.Bottom - castBtnHeight - 16,
                    castBtnWidth,
                    castBtnHeight
                ),
                "cast"
            );

            // Favorite slots
            int favStartX = xPositionOnScreen + 600;
            for (int i = 0; i < 5; i++)
            {
                _favoriteSlots.Add(new ClickableComponent(
                    new Rectangle(favStartX + i * 52, footerY - 6, 44, 44),
                    $"fav_{i + 1}"
                ));
            }

            // Confirmation dialog buttons
            int btnWidth = 100;
            int btnHeight = 44;
            int btnY = _confirmDialog.Bottom - btnHeight - 20;
            int btnSpacing = 20;

            _confirmYesButton = new ClickableComponent(
                new Rectangle(
                    _confirmDialog.Center.X - btnWidth - btnSpacing / 2,
                    btnY,
                    btnWidth,
                    btnHeight
                ),
                "confirm_yes"
            );

            _confirmNoButton = new ClickableComponent(
                new Rectangle(
                    _confirmDialog.Center.X + btnSpacing / 2,
                    btnY,
                    btnWidth,
                    btnHeight
                ),
                "confirm_no"
            );
        }

        /*********
        ** Helper Methods
        *********/

        private List<SpellData> GetCurrentPageSpells()
        {
            if (_spells == null || _spells.Count == 0)
                return new List<SpellData>();

            int startIndex = _currentPage * _spellsPerPage;
            return _spells.Skip(startIndex).Take(_spellsPerPage).ToList();
        }

        private SpellData? GetSelectedSpell()
        {
            var pageSpells = GetCurrentPageSpells();
            if (_selectedIndex >= 0 && _selectedIndex < pageSpells.Count)
            {
                return pageSpells[_selectedIndex];
            }
            return null;
        }

        /// <summary>
        /// Play sound dengan config check
        /// </summary>
        private void PlaySound(string soundName)
        {
            if (!_config.EnableSounds) return;

            try
            {
                Game1.playSound(soundName);
            }
            catch
            {
                // Ignore sound errors
            }
        }

        /*********
        ** Update
        *********/

        public override void update(GameTime time)
        {
            base.update(time);
            _pulseTimer += (float)time.ElapsedGameTime.TotalSeconds;
        }

        /*********
        ** Input Handling
        *********/

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // ═══════════════════════════════════════════════════════════
            // Handle Confirmation Dialog
            // ═══════════════════════════════════════════════════════════
            if (_isConfirmingCast)
            {
                HandleConfirmationClick(x, y);
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // Close button
            // ═══════════════════════════════════════════════════════════
            if (_closeButton?.containsPoint(x, y) == true)
            {
                exitThisMenu();
                PlaySound("bigDeSelect");
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // Spell slots
            // ═══════════════════════════════════════════════════════════
            var pageSpells = GetCurrentPageSpells();
            for (int i = 0; i < _spellSlots.Count && i < pageSpells.Count; i++)
            {
                if (_spellSlots[i].containsPoint(x, y))
                {
                    _selectedIndex = i;
                    PlaySound("smallSelect");
                    return;
                }
            }

            // ═══════════════════════════════════════════════════════════
            // Cast button
            // ═══════════════════════════════════════════════════════════
            if (_castButton?.containsPoint(x, y) == true)
            {
                TryCastSelectedSpell();
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // Page navigation
            // ═══════════════════════════════════════════════════════════
            if (_prevPageButton?.containsPoint(x, y) == true && _totalPages > 1)
            {
                _currentPage = (_currentPage - 1 + _totalPages) % _totalPages;
                _selectedIndex = 0;
                PlaySound("shwip");
                return;
            }

            if (_nextPageButton?.containsPoint(x, y) == true && _totalPages > 1)
            {
                _currentPage = (_currentPage + 1) % _totalPages;
                _selectedIndex = 0;
                PlaySound("shwip");
                return;
            }

            // ═══════════════════════════════════════════════════════════
            // Favorite slots
            // ═══════════════════════════════════════════════════════════
            for (int i = 0; i < _favoriteSlots.Count; i++)
            {
                if (_favoriteSlots[i].containsPoint(x, y))
                {
                    int slotNum = i + 1;
                    if (_favorites.TryGetValue(slotNum, out var favSpell))
                    {
                        SelectSpellByData(favSpell);
                        PlaySound("smallSelect");
                    }
                    return;
                }
            }

            // ═══════════════════════════════════════════════════════════
            // Click outside = close
            // ═══════════════════════════════════════════════════════════
            if (!isWithinBounds(x, y))
            {
                exitThisMenu();
                PlaySound("bigDeSelect");
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            // Ignore right click during confirmation
            if (_isConfirmingCast) return;

            var pageSpells = GetCurrentPageSpells();
            for (int i = 0; i < _spellSlots.Count && i < pageSpells.Count; i++)
            {
                if (_spellSlots[i].containsPoint(x, y))
                {
                    _selectedIndex = i;
                    ShowFavoriteMenu(pageSpells[i]);
                    return;
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            // Disable hover during confirmation
            if (_isConfirmingCast) return;

            base.performHoverAction(x, y);
            _hoverText = "";

            // Hover on spell slots
            var pageSpells = GetCurrentPageSpells();
            for (int i = 0; i < _spellSlots.Count && i < pageSpells.Count; i++)
            {
                if (_spellSlots[i].containsPoint(x, y))
                {
                    _selectedIndex = i;

                    // Show tooltip if enabled
                    if (_config.ShowTooltips && pageSpells[i].IsVisible)
                    {
                        _hoverText = pageSpells[i].Name;
                    }
                    return;
                }
            }

            // Hover on favorites
            for (int i = 0; i < _favoriteSlots.Count; i++)
            {
                if (_favoriteSlots[i].containsPoint(x, y))
                {
                    int slotNum = i + 1;
                    if (_favorites.TryGetValue(slotNum, out var favSpell))
                    {
                        _hoverText = favSpell.Name;
                    }
                    return;
                }
            }
        }

        /*********
        ** Spell Casting
        *********/

        private void TryCastSelectedSpell()
        {
            var spell = GetSelectedSpell();
            if (spell == null)
            {
                PlaySound("cancel");
                return;
            }

            // Check if visible and unlocked
            if (!spell.IsVisible || !spell.IsUnlocked)
            {
                PlaySound("cancel");
                Game1.showRedMessage("Spell is locked!");
                return;
            }

            // Check mana
            int currentMana = _api.GetCurrentMana(_manaBar, _player);
            if (currentMana < spell.ManaCost)
            {
                PlaySound("cancel");
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // Check for high mana confirmation
            if (_config.ConfirmHighManaCast && spell.ManaCost >= _config.HighManaThreshold)
            {
                ShowCastConfirmation(spell);
                return;
            }

            // Execute cast
            ExecuteSpellCast(spell);
        }

        /// <summary>
        /// Actually execute the spell cast
        /// </summary>
        private void ExecuteSpellCast(SpellData spell)
        {
            PlaySound("wand");
            _player.completelyStopAnimatingOrDoingAction();
            _player.faceDirection(2);

            bool success = _api.CastSpell(spell, _player, _manaBar);

            if (success && _config.CloseAfterCast)
            {
                DelayedAction.functionAfterDelay(() =>
                {
                    if (Game1.activeClickableMenu == this)
                    {
                        exitThisMenu(false);
                    }
                }, _config.CloseDelay);
            }
        }

        /*********
        ** Confirmation Dialog
        *********/

        /// <summary>
        /// Show confirmation dialog before casting expensive spell
        /// </summary>
        private void ShowCastConfirmation(SpellData spell)
        {
            _isConfirmingCast = true;
            _pendingSpell = spell;
            PlaySound("bigSelect");

            if (_config.DebugMode)
            {
                _monitor.Log($"Showing cast confirmation for: {spell.Name} (Cost: {spell.ManaCost})", LogLevel.Debug);
            }
        }

        /// <summary>
        /// Handle clicks on confirmation dialog
        /// </summary>
        private void HandleConfirmationClick(int x, int y)
        {
            // Yes button - cast the spell
            if (_confirmYesButton?.containsPoint(x, y) == true)
            {
                if (_pendingSpell != null)
                {
                    ExecuteSpellCast(_pendingSpell);
                }
                CloseConfirmation();
                return;
            }

            // No button or click outside - cancel
            if (_confirmNoButton?.containsPoint(x, y) == true || !_confirmDialog.Contains(x, y))
            {
                PlaySound("bigDeSelect");
                CloseConfirmation();
                return;
            }
        }

        /// <summary>
        /// Close confirmation dialog
        /// </summary>
        private void CloseConfirmation()
        {
            _isConfirmingCast = false;
            _pendingSpell = null;
        }

        /*********
        ** Selection Helpers
        *********/

        private void SelectSpellByData(SpellData targetSpell)
        {
            for (int page = 0; page < _totalPages; page++)
            {
                var pageSpells = _spells.Skip(page * _spellsPerPage).Take(_spellsPerPage).ToList();
                for (int i = 0; i < pageSpells.Count; i++)
                {
                    if (pageSpells[i].IconIndex == targetSpell.IconIndex)
                    {
                        _currentPage = page;
                        _selectedIndex = i;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Show favorite assignment menu
        /// </summary>
        private void ShowFavoriteMenu(SpellData spell)
        {
            // Create response options
            var responses = new List<Response>();

            // Check if already favorited
            int existingSlot = 0;
            foreach (var kvp in _favorites)
            {
                if (kvp.Value.IconIndex == spell.IconIndex)
                {
                    existingSlot = kvp.Key;
                    break;
                }
            }

            if (existingSlot > 0)
            {
                responses.Add(new Response("remove", $"Remove from Slot {existingSlot}"));
            }
            else
            {
                // Add available slots
                for (int i = 1; i <= 5; i++)
                {
                    if (!_favorites.ContainsKey(i))
                    {
                        responses.Add(new Response($"slot_{i}", $"Add to Slot {i}"));
                    }
                }
            }

            responses.Add(new Response("cancel", "Cancel"));

            Game1.currentLocation?.createQuestionDialogue(
                $"Favorite: {spell.Name}",
                responses.ToArray(),
                (who, answer) => HandleFavoriteResponse(spell, answer)
            );
        }

        private void HandleFavoriteResponse(SpellData spell, string answer)
        {
            if (answer == "cancel" || string.IsNullOrEmpty(answer))
            {
                PlaySound("cancel");
                return;
            }

            if (answer == "remove")
            {
                // Find and remove
                int slotToRemove = 0;
                foreach (var kvp in _favorites)
                {
                    if (kvp.Value.IconIndex == spell.IconIndex)
                    {
                        slotToRemove = kvp.Key;
                        break;
                    }
                }

                if (slotToRemove > 0)
                {
                    _favorites.Remove(slotToRemove);
                    spell.FavoriteSlot = 0;
                    PlaySound("trashcan");
                }
            }
            else if (answer.StartsWith("slot_"))
            {
                int slot = int.Parse(answer.Substring(5));

                // Remove from old slot if exists
                int oldSlot = 0;
                foreach (var kvp in _favorites)
                {
                    if (kvp.Value.IconIndex == spell.IconIndex)
                    {
                        oldSlot = kvp.Key;
                        break;
                    }
                }
                if (oldSlot > 0)
                {
                    _favorites.Remove(oldSlot);
                }

                // Add to new slot
                _favorites[slot] = spell;
                spell.FavoriteSlot = slot;
                PlaySound("coin");
            }
        }

        /*********
        ** Drawing
        *********/

        public override void draw(SpriteBatch b)
        {
            if (b == null) return;

            // ═══════════════════════════════════════════════════════════
            // Background dimming
            // ═══════════════════════════════════════════════════════════
            b.Draw(Game1.fadeToBlackRect,
                   Game1.graphics.GraphicsDevice.Viewport.Bounds,
                   Color.Black * _config.BackgroundOpacity);

            // ═══════════════════════════════════════════════════════════
            // Main menu box
            // ═══════════════════════════════════════════════════════════
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                xPositionOnScreen,
                yPositionOnScreen,
                width,
                height,
                Color.White,
                1f,
                true
            );

            // ═══════════════════════════════════════════════════════════
            // Draw all sections
            // ═══════════════════════════════════════════════════════════
            DrawHeader(b);
            DrawGridBackground(b);
            DrawSpellGrid(b);
            DrawInfoPanelBackground(b);
            DrawSpellInfo(b);
            DrawFooter(b);

            // Close button
            _closeButton?.draw(b);

            // ═══════════════════════════════════════════════════════════
            // Confirmation dialog (on top of everything)
            // ═══════════════════════════════════════════════════════════
            if (_isConfirmingCast)
            {
                DrawConfirmationDialog(b);
            }

            // ═══════════════════════════════════════════════════════════
            // Hover text
            // ═══════════════════════════════════════════════════════════
            if (!string.IsNullOrEmpty(_hoverText) && !_isConfirmingCast)
            {
                IClickableMenu.drawHoverText(b, _hoverText, Game1.smallFont);
            }

            // Mouse cursor
            drawMouse(b);
        }

        private void DrawHeader(SpriteBatch b)
        {
            string title = "SPELL BOOK";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Vector2 titlePos = new Vector2(
                xPositionOnScreen + (width - titleSize.X) / 2,
                yPositionOnScreen + 16
            );

            Utility.drawTextWithShadow(b, title, Game1.dialogueFont, titlePos, _titleColor);
        }

        private void DrawGridBackground(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                _gridArea.X,
                _gridArea.Y,
                _gridArea.Width,
                _gridArea.Height,
                Color.White * 0.5f,
                1f,
                false
            );
        }

        private void DrawSpellGrid(SpriteBatch b)
        {
            var pageSpells = GetCurrentPageSpells();
            var spellIconsTexture = _api.SpellIconsTexture;

            for (int i = 0; i < _spellSlots.Count; i++)
            {
                var slot = _spellSlots[i];

                if (i < pageSpells.Count)
                {
                    var spell = pageSpells[i];

                    // Draw spell icon or lock
                    if (spell.IsVisible && spellIconsTexture != null)
                    {
                        Rectangle sourceRect = new Rectangle(spell.IconIndex * 16, 0, 16, 16);
                        Color tint = spell.IsUnlocked ? Color.White : Color.Gray * 0.6f;
                        b.Draw(spellIconsTexture, slot.bounds, sourceRect, tint);
                    }
                    else
                    {
                        DrawLockedSlot(b, slot.bounds);
                    }

                    // Favorite star
                    if (spell.FavoriteSlot > 0)
                    {
                        Rectangle starDest = new Rectangle(
                            slot.bounds.Right - 18,
                            slot.bounds.Bottom - 18,
                            18, 18
                        );
                        b.Draw(Game1.mouseCursors, starDest,
                               new Rectangle(346, 392, 8, 8), Color.Gold);
                    }

                    // Selection highlight
                    if (i == _selectedIndex)
                    {
                        DrawSelectionHighlight(b, slot.bounds, spell, spellIconsTexture);
                    }
                }
                else
                {
                    // Empty slot
                    DrawEmptySlot(b, slot.bounds);
                }
            }
        }

        /// <summary>
        /// Draw locked spell slot
        /// </summary>
        private void DrawLockedSlot(SpriteBatch b, Rectangle bounds)
        {
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y,
                bounds.Width, bounds.Height,
                Color.Gray * 0.4f, 1f, false
            );

            // Lock icon
            Rectangle lockSource = new Rectangle(107, 442, 7, 8);
            Rectangle lockDest = new Rectangle(
                bounds.X + bounds.Width / 2 - 16,
                bounds.Y + bounds.Height / 2 - 16,
                32, 32
            );
            b.Draw(Game1.mouseCursors, lockDest, lockSource, Color.White);
        }

        /// <summary>
        /// Draw empty slot
        /// </summary>
        private void DrawEmptySlot(SpriteBatch b, Rectangle bounds)
        {
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y,
                bounds.Width, bounds.Height,
                Color.Black * 0.2f, 1f, false
            );
        }

        /// <summary>
        /// Draw selection highlight with animation or static
        /// </summary>
        private void DrawSelectionHighlight(SpriteBatch b, Rectangle slot, SpellData spell, Texture2D? spellIconsTexture)
        {
            int border = 4;
            Rectangle highlight = new Rectangle(
                slot.X - border,
                slot.Y - border,
                slot.Width + border * 2,
                slot.Height + border * 2
            );

            if (_config.ShowSelectionAnimation)
            {
                // Animated pulsing highlight
                float pulse = 1f + 0.08f * (float)Math.Sin(_pulseTimer * 5f * _config.AnimationSpeed);

                IClickableMenu.drawTextureBox(
                    b, Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    highlight.X, highlight.Y,
                    highlight.Width, highlight.Height,
                    _highlightColor * 0.9f * pulse,
                    1f, false
                );
            }
            else
            {
                // Static highlight
                DrawStaticHighlight(b, slot);
            }

            // Re-draw icon on top of highlight
            if (spell.IsVisible && spellIconsTexture != null)
            {
                Rectangle sourceRect = new Rectangle(spell.IconIndex * 16, 0, 16, 16);
                Color tint = spell.IsUnlocked ? Color.White : Color.Gray * 0.6f;
                b.Draw(spellIconsTexture, slot, sourceRect, tint);
            }
        }

        /// <summary>
        /// Draw static (non-animated) selection highlight
        /// </summary>
        private void DrawStaticHighlight(SpriteBatch b, Rectangle slot)
        {
            int border = 4;
            Rectangle highlight = new Rectangle(
                slot.X - border,
                slot.Y - border,
                slot.Width + border * 2,
                slot.Height + border * 2
            );

            // Solid border without animation
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                highlight.X, highlight.Y,
                highlight.Width, highlight.Height,
                _highlightColor * 0.85f,
                1f, false
            );

            // Inner glow effect
            Rectangle innerGlow = new Rectangle(
                slot.X - 2,
                slot.Y - 2,
                slot.Width + 4,
                slot.Height + 4
            );

            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                innerGlow.X, innerGlow.Y,
                innerGlow.Width, innerGlow.Height,
                Color.White * 0.3f,
                1f, false
            );
        }

        private void DrawInfoPanelBackground(SpriteBatch b)
        {
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                _infoPanel.X,
                _infoPanel.Y,
                _infoPanel.Width,
                _infoPanel.Height,
                Color.White * 0.5f,
                1f,
                false
            );
        }

        private void DrawSpellInfo(SpriteBatch b)
        {
            var spell = GetSelectedSpell();
            if (spell == null) return;

            int padding = 12;
            int y = _infoPanel.Y + padding;

            // Spell name
            string name = spell.IsVisible ? spell.Name : "???";
            Vector2 nameSize = Game1.dialogueFont.MeasureString(name);
            float maxWidth = _infoPanel.Width - padding * 2;
            float scale = Math.Min(1f, maxWidth / nameSize.X);

            Vector2 namePos = new Vector2(
                _infoPanel.X + (_infoPanel.Width - nameSize.X * scale) / 2,
                y
            );

            b.DrawString(Game1.dialogueFont, name, namePos + new Vector2(2, 2), Color.Black * 0.3f, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            b.DrawString(Game1.dialogueFont, name, namePos, _titleColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

            y += (int)(nameSize.Y * scale) + 12;

            // Large icon
            int iconSize = Math.Min(72, _infoPanel.Width - padding * 4);
            Rectangle iconRect = new Rectangle(
                _infoPanel.X + (_infoPanel.Width - iconSize) / 2,
                y, iconSize, iconSize
            );

            var spellIconsTexture = _api.SpellIconsTexture;
            if (spell.IsVisible && spellIconsTexture != null)
            {
                Rectangle src = new Rectangle(spell.IconIndex * 16, 0, 16, 16);
                b.Draw(spellIconsTexture, iconRect, src,
                       spell.IsUnlocked ? Color.White : Color.Gray);
            }
            else
            {
                b.Draw(Game1.mouseCursors, iconRect,
                       new Rectangle(107, 442, 7, 8), Color.White);
            }

            y += iconSize + 12;

            // Description
            string desc = spell.IsVisible ? spell.Description : "???";
            string wrapped = Game1.parseText(desc, Game1.smallFont, _infoPanel.Width - padding * 2);
            Utility.drawTextWithShadow(b, wrapped, Game1.smallFont,
                new Vector2(_infoPanel.X + padding, y), _textColor);

            Vector2 descSize = Game1.smallFont.MeasureString(wrapped);
            y += (int)descSize.Y + 12;

            // Mana cost
            if (spell.IsVisible)
            {
                string manaText = $"Mana: {spell.ManaCost}";
                int currentMana = _api.GetCurrentMana(_manaBar, _player);
                Color manaColor = currentMana >= spell.ManaCost ? Color.CornflowerBlue : Color.Red;

                Utility.drawTextWithShadow(b, manaText, Game1.smallFont,
                    new Vector2(_infoPanel.X + padding, y), manaColor);
            }

            // Cast button
            DrawCastButton(b, spell);
        }

        private void DrawCastButton(SpriteBatch b, SpellData spell)
        {
            if (_castButton == null) return;
            if (!spell.IsVisible || !spell.IsUnlocked) return;

            int currentMana = _api.GetCurrentMana(_manaBar, _player);
            bool canCast = currentMana >= spell.ManaCost;
            Color btnColor = canCast ? new Color(100, 180, 100) : Color.Gray;

            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                _castButton.bounds.X, _castButton.bounds.Y,
                _castButton.bounds.Width, _castButton.bounds.Height,
                btnColor, 1f, true
            );

            string castText = "CAST";
            Vector2 castSize = Game1.dialogueFont.MeasureString(castText);
            Vector2 castPos = new Vector2(
                _castButton.bounds.X + (_castButton.bounds.Width - castSize.X) / 2,
                _castButton.bounds.Y + (_castButton.bounds.Height - castSize.Y) / 2
            );
            Utility.drawTextWithShadow(b, castText, Game1.dialogueFont, castPos,
                canCast ? Color.White : Color.DarkGray);
        }

        private void DrawFooter(SpriteBatch b)
        {
            int footerY = _gridArea.Bottom + 16;

            // Page navigation
            if (_totalPages > 1)
            {
                _prevPageButton?.draw(b);
                _nextPageButton?.draw(b);

                string pageText = $"{_currentPage + 1}/{_totalPages}";
                Vector2 pagePos = new Vector2(
                    xPositionOnScreen + 85,
                    footerY - 5
                );
                Utility.drawTextWithShadow(b, pageText, Game1.smallFont, pagePos, _textColor);
            }

            // Favorites label
            string favLabel = "Favorites:";
            Utility.drawTextWithShadow(b, favLabel, Game1.smallFont,
                new Vector2(xPositionOnScreen + 600 - 120, footerY - 5), Color.Brown);

            // Favorite slots
            DrawFavoriteSlots(b);
        }

        private void DrawFavoriteSlots(SpriteBatch b)
        {
            var spellIconsTexture = _api.SpellIconsTexture;

            for (int i = 0; i < _favoriteSlots.Count; i++)
            {
                var slot = _favoriteSlots[i];
                int slotNum = i + 1;

                // Slot background
                IClickableMenu.drawTextureBox(
                    b, Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    slot.bounds.X, slot.bounds.Y,
                    slot.bounds.Width, slot.bounds.Height,
                    Color.White * 0.6f, 1f, false
                );

                // Favorite spell icon or number
                if (_favorites.TryGetValue(slotNum, out var favSpell) && spellIconsTexture != null)
                {
                    Rectangle src = new Rectangle(favSpell.IconIndex * 16, 0, 16, 16);
                    b.Draw(spellIconsTexture, slot.bounds, src, Color.White);
                }
                else
                {
                    string numStr = slotNum.ToString();
                    Vector2 numSize = Game1.smallFont.MeasureString(numStr);
                    Vector2 numPos = new Vector2(
                        slot.bounds.X + (slot.bounds.Width - numSize.X) / 2,
                        slot.bounds.Y + (slot.bounds.Height - numSize.Y) / 2
                    );
                    Utility.drawTextWithShadow(b, numStr, Game1.smallFont, numPos, Color.Gray);
                }
            }
        }

        /// <summary>
        /// Draw the confirmation dialog for high mana spells
        /// </summary>
        private void DrawConfirmationDialog(SpriteBatch b)
        {
            // Dim the menu behind dialog
            b.Draw(Game1.fadeToBlackRect,
                   new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height),
                   Color.Black * 0.5f);

            // Dialog box
            IClickableMenu.drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                _confirmDialog.X,
                _confirmDialog.Y,
                _confirmDialog.Width,
                _confirmDialog.Height,
                Color.White,
                1f,
                true
            );

            // Title
            string title = "Confirm Cast";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Vector2 titlePos = new Vector2(
                _confirmDialog.X + (_confirmDialog.Width - titleSize.X) / 2,
                _confirmDialog.Y + 16
            );
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont, titlePos, _titleColor);

            // Message
            if (_pendingSpell != null)
            {
                string message = $"Cast {_pendingSpell.Name}?";
                string manaCost = $"Mana Cost: {_pendingSpell.ManaCost}";

                Vector2 msgSize = Game1.smallFont.MeasureString(message);
                Vector2 msgPos = new Vector2(
                    _confirmDialog.X + (_confirmDialog.Width - msgSize.X) / 2,
                    _confirmDialog.Y + 60
                );
                Utility.drawTextWithShadow(b, message, Game1.smallFont, msgPos, _textColor);

                Vector2 manaSize = Game1.smallFont.MeasureString(manaCost);
                Vector2 manaPos = new Vector2(
                    _confirmDialog.X + (_confirmDialog.Width - manaSize.X) / 2,
                    _confirmDialog.Y + 85
                );
                Utility.drawTextWithShadow(b, manaCost, Game1.smallFont, manaPos, Color.CornflowerBlue);
            }

            // Yes button
            if (_confirmYesButton != null)
            {
                IClickableMenu.drawTextureBox(
                    b, Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    _confirmYesButton.bounds.X, _confirmYesButton.bounds.Y,
                    _confirmYesButton.bounds.Width, _confirmYesButton.bounds.Height,
                    new Color(100, 180, 100), 1f, true
                );

                string yesText = "Yes";
                Vector2 yesSize = Game1.dialogueFont.MeasureString(yesText);
                Vector2 yesPos = new Vector2(
                    _confirmYesButton.bounds.X + (_confirmYesButton.bounds.Width - yesSize.X) / 2,
                    _confirmYesButton.bounds.Y + (_confirmYesButton.bounds.Height - yesSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, yesText, Game1.dialogueFont, yesPos, Color.White);
            }

            // No button
            if (_confirmNoButton != null)
            {
                IClickableMenu.drawTextureBox(
                    b, Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    _confirmNoButton.bounds.X, _confirmNoButton.bounds.Y,
                    _confirmNoButton.bounds.Width, _confirmNoButton.bounds.Height,
                    new Color(180, 100, 100), 1f, true
                );

                string noText = "No";
                Vector2 noSize = Game1.dialogueFont.MeasureString(noText);
                Vector2 noPos = new Vector2(
                    _confirmNoButton.bounds.X + (_confirmNoButton.bounds.Width - noSize.X) / 2,
                    _confirmNoButton.bounds.Y + (_confirmNoButton.bounds.Height - noSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, noText, Game1.dialogueFont, noPos, Color.White);
            }
        }

        /*********
        ** Window Resize Handler
        *********/

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            CalculateLayout();
            CreateUIComponents();
        }
    }
}