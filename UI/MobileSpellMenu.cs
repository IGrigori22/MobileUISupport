using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MobileUISupport;
using MobileUISupport.Framework;
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
        // Dependencies
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly ModConfig _config;
        private readonly MagicStardewAPI _api;

        // Data
        private readonly List<SpellData> _spells;
        private readonly Farmer _player;
        private readonly object _manaBar;
        private readonly Dictionary<int, SpellData> _favorites;

        // UI State
        private int _selectedIndex = 0;
        private int _currentPage = 0;
        private int _spellsPerPage;
        private int _totalPages;
        private float _pulseTimer = 0f;
        private string _hoverText = "";

        // UI Components
        private List<ClickableComponent> _spellSlots = new();
        private ClickableTextureComponent? _prevPageButton;
        private ClickableTextureComponent? _nextPageButton;
        private ClickableTextureComponent? _closeButton;
        private ClickableComponent? _castButton;
        private List<ClickableComponent> _favoriteSlots = new();

        // Layout constants
        private const int ICON_SIZE = 64;
        private const int ICON_SPACING = 12;
        private const int GRID_COLUMNS = 5;
        private const int GRID_ROWS = 4;

        // Calculated areas
        private Rectangle _gridArea;
        private Rectangle _infoPanel;

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

            _spellsPerPage = GRID_COLUMNS * GRID_ROWS;
            _totalPages = Math.Max(1, (int)Math.Ceiling((double)_spells.Count / _spellsPerPage));

            CalculateLayout();
            CreateUIComponents();

            // Play sound saat menu dibuka
            if (Game1.soundBank != null)
            {
                try { Game1.playSound("bigSelect"); } catch { }
            }
        }

        private void CalculateLayout()
        {
            int viewportWidth = Game1.uiViewport.Width;
            int viewportHeight = Game1.uiViewport.Height;

            // Hitung ukuran grid
            int gridWidth = (ICON_SIZE * GRID_COLUMNS) + (ICON_SPACING * (GRID_COLUMNS - 1));
            int gridHeight = (ICON_SIZE * GRID_ROWS) + (ICON_SPACING * (GRID_ROWS - 1));

            // Panel info di sebelah kanan
            int panelWidth = 250;

            // Total ukuran menu
            int totalWidth = gridWidth + panelWidth + 100; // padding
            int totalHeight = gridHeight + 140; // header + footer

            // Clamp ke ukuran layar
            totalWidth = Math.Min(totalWidth, viewportWidth - 40);
            totalHeight = Math.Min(totalHeight, viewportHeight - 40);

            // Set bounds
            width = totalWidth;
            height = totalHeight;
            xPositionOnScreen = (viewportWidth - width) / 2;
            yPositionOnScreen = Math.Max(20, (viewportHeight - height) / 2);

            // Calculate sub-areas
            int padding = 24;
            int headerHeight = 60;

            _gridArea = new Rectangle(
                xPositionOnScreen + padding,
                yPositionOnScreen + headerHeight,
                gridWidth + 24,
                gridHeight + 24
            );

            _infoPanel = new Rectangle(
                _gridArea.Right + 16,
                yPositionOnScreen + headerHeight,
                width - _gridArea.Width - padding * 2 - 16,
                gridHeight + 24
            );

            _monitor.Log($"Menu layout: {width}x{height} at ({xPositionOnScreen}, {yPositionOnScreen})", LogLevel.Debug);
        }

        private void CreateUIComponents()
        {
            _spellSlots.Clear();
            _favoriteSlots.Clear();

            // Close button
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
                int row = i / GRID_COLUMNS;
                int col = i % GRID_COLUMNS;

                int slotX = startX + col * (ICON_SIZE + ICON_SPACING);
                int slotY = startY + row * (ICON_SIZE + ICON_SPACING);

                _spellSlots.Add(new ClickableComponent(
                    new Rectangle(slotX, slotY, ICON_SIZE, ICON_SIZE),
                    $"spell_{i}"
                ));
            }

            // Footer area
            int footerY = _gridArea.Bottom + 16;

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
            int favStartX = xPositionOnScreen + 200;
            for (int i = 0; i < 5; i++)
            {
                _favoriteSlots.Add(new ClickableComponent(
                    new Rectangle(favStartX + i * 52, footerY + 4, 44, 44),
                    $"fav_{i + 1}"
                ));
            }
        }

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

        public override void update(GameTime time)
        {
            base.update(time);
            _pulseTimer += (float)time.ElapsedGameTime.TotalSeconds;
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Close button
            if (_closeButton?.containsPoint(x, y) == true)
            {
                exitThisMenu();
                try { Game1.playSound("bigDeSelect"); } catch { }
                return;
            }

            // Spell slots
            var pageSpells = GetCurrentPageSpells();
            for (int i = 0; i < _spellSlots.Count && i < pageSpells.Count; i++)
            {
                if (_spellSlots[i].containsPoint(x, y))
                {
                    _selectedIndex = i;
                    try { Game1.playSound("smallSelect"); } catch { }
                    return;
                }
            }

            // Cast button
            if (_castButton?.containsPoint(x, y) == true)
            {
                TryCastSelectedSpell();
                return;
            }

            // Page navigation
            if (_prevPageButton?.containsPoint(x, y) == true && _totalPages > 1)
            {
                _currentPage = (_currentPage - 1 + _totalPages) % _totalPages;
                _selectedIndex = 0;
                try { Game1.playSound("shwip"); } catch { }
                return;
            }

            if (_nextPageButton?.containsPoint(x, y) == true && _totalPages > 1)
            {
                _currentPage = (_currentPage + 1) % _totalPages;
                _selectedIndex = 0;
                try { Game1.playSound("shwip"); } catch { }
                return;
            }

            // Favorite slots
            for (int i = 0; i < _favoriteSlots.Count; i++)
            {
                if (_favoriteSlots[i].containsPoint(x, y))
                {
                    int slotNum = i + 1;
                    if (_favorites.TryGetValue(slotNum, out var favSpell))
                    {
                        SelectSpellByData(favSpell);
                        try { Game1.playSound("smallSelect"); } catch { }
                    }
                    return;
                }
            }

            // Click outside = close
            if (!isWithinBounds(x, y))
            {
                exitThisMenu();
                try { Game1.playSound("bigDeSelect"); } catch { }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            var pageSpells = GetCurrentPageSpells();
            for (int i = 0; i < _spellSlots.Count && i < pageSpells.Count; i++)
            {
                if (_spellSlots[i].containsPoint(x, y))
                {
                    _selectedIndex = i;
                    // TODO: Show favorite assignment dialog
                    try { Game1.playSound("coin"); } catch { }
                    return;
                }
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            _hoverText = "";

            // Hover on spell slots
            var pageSpells = GetCurrentPageSpells();
            for (int i = 0; i < _spellSlots.Count && i < pageSpells.Count; i++)
            {
                if (_spellSlots[i].containsPoint(x, y))
                {
                    _selectedIndex = i;
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

        private void TryCastSelectedSpell()
        {
            var spell = GetSelectedSpell();
            if (spell == null)
            {
                try { Game1.playSound("cancel"); } catch { }
                return;
            }

            if (!spell.IsVisible || !spell.IsUnlocked)
            {
                try { Game1.playSound("cancel"); } catch { }
                Game1.showRedMessage("Spell is locked!");
                return;
            }

            int currentMana = _api.GetCurrentMana(_manaBar, _player);
            if (currentMana < spell.ManaCost)
            {
                try { Game1.playSound("cancel"); } catch { }
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // Cast the spell
            try { Game1.playSound("wand"); } catch { }
            _player.completelyStopAnimatingOrDoingAction();
            _player.faceDirection(2);

            bool success = _api.CastSpell(spell, _player, _manaBar);
            if (success)
            {
                DelayedAction.functionAfterDelay(() =>
                {
                    if (Game1.activeClickableMenu == this)
                    {
                        exitThisMenu(false);
                    }
                }, 400);
            }
        }

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

        public override void draw(SpriteBatch b)
        {
            // Safety check
            if (b == null) return;

            // Dim background
            b.Draw(Game1.fadeToBlackRect,
                   Game1.graphics.GraphicsDevice.Viewport.Bounds,
                   Color.Black * 0.6f);

            // Main menu background
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

            // Header
            DrawHeader(b);

            // Grid background
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

            // Spell grid
            DrawSpellGrid(b);

            // Info panel background
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

            // Selected spell info
            DrawSpellInfo(b);

            // Footer
            DrawFooter(b);

            // Close button
            _closeButton?.draw(b);

            // Hover text
            if (!string.IsNullOrEmpty(_hoverText))
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

            Utility.drawTextWithShadow(b, title, Game1.dialogueFont, titlePos,
                new Color(86, 22, 12)); // Dark brown color
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
                        // Draw lock
                        IClickableMenu.drawTextureBox(
                            b, Game1.menuTexture,
                            new Rectangle(0, 256, 60, 60),
                            slot.bounds.X, slot.bounds.Y,
                            slot.bounds.Width, slot.bounds.Height,
                            Color.Gray * 0.4f, 1f, false
                        );

                        // Lock icon from cursors
                        Rectangle lockSource = new Rectangle(107, 442, 7, 8);
                        Rectangle lockDest = new Rectangle(
                            slot.bounds.X + 16, slot.bounds.Y + 16,
                            32, 32
                        );
                        b.Draw(Game1.mouseCursors, lockDest, lockSource, Color.White);
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
                        float pulse = 1f + 0.08f * (float)Math.Sin(_pulseTimer * 5f);
                        int border = 4;
                        Rectangle highlight = new Rectangle(
                            slot.bounds.X - border,
                            slot.bounds.Y - border,
                            slot.bounds.Width + border * 2,
                            slot.bounds.Height + border * 2
                        );

                        // Yellow pulsing border
                        IClickableMenu.drawTextureBox(
                            b, Game1.menuTexture,
                            new Rectangle(0, 256, 60, 60),
                            highlight.X, highlight.Y,
                            highlight.Width, highlight.Height,
                            Color.Gold * 0.9f * pulse,
                            1f, false
                        );

                        // Re-draw icon on top
                        if (spell.IsVisible && spellIconsTexture != null)
                        {
                            Rectangle sourceRect = new Rectangle(spell.IconIndex * 16, 0, 16, 16);
                            Color tint = spell.IsUnlocked ? Color.White : Color.Gray * 0.6f;
                            b.Draw(spellIconsTexture, slot.bounds, sourceRect, tint);
                        }
                    }
                }
                else
                {
                    // Empty slot
                    IClickableMenu.drawTextureBox(
                        b, Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        slot.bounds.X, slot.bounds.Y,
                        slot.bounds.Width, slot.bounds.Height,
                        Color.Black * 0.2f, 1f, false
                    );
                }
            }
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
            float nameX = _infoPanel.X + (_infoPanel.Width - nameSize.X) / 2;
            Utility.drawTextWithShadow(b, name, Game1.dialogueFont,
                new Vector2(nameX, y), new Color(86, 22, 12));

            y += (int)nameSize.Y + 12;

            // Large icon
            int iconSize = 72;
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
                new Vector2(_infoPanel.X + padding, y), Color.White);

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
            if (_castButton != null && spell.IsVisible && spell.IsUnlocked)
            {
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
                    xPositionOnScreen + 95,
                    footerY + 10
                );
                Utility.drawTextWithShadow(b, pageText, Game1.smallFont, pagePos, Color.White);
            }

            // Favorites
            string favLabel = "Favorites:";
            Utility.drawTextWithShadow(b, favLabel, Game1.smallFont,
                new Vector2(xPositionOnScreen + 200 - 70, footerY + 14), Color.Gold);

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

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            CalculateLayout();
            CreateUIComponents();
        }
    }
}