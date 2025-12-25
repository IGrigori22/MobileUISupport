using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.MHEventsList;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileUISupport.UI
{
    /// <summary>
    /// Custom mobile-friendly menu untuk MHEventsList.
    /// Menggunakan MHEventsListAPI untuk akses data via reflection.
    /// </summary>
    internal class MobileMHEventsMenu : IClickableMenu, IKeyboardSubscriber
    {
        // ═══════════════════════════════════════════════════════
        // Constants - Mirror dari original
        // ═══════════════════════════════════════════════════════

        private const int MENU_WIDTH = 1280;
        private const int MENU_HEIGHT = 760;
        private const int MAX_ENTRIES_SHOWN = 6;
        private const int ENTRY_HEIGHT = 88;
        private const int BOX_BORDER = 12;
        private const int MAX_NPC_SHOWN = 8;

        // ═══════════════════════════════════════════════════════
        // API Reference
        // ═══════════════════════════════════════════════════════

        private readonly MHEventsListAPI _api;

        // ═══════════════════════════════════════════════════════
        // Data Lists
        // ═══════════════════════════════════════════════════════

        private List<EventDataWrapper> _allEvents;
        private List<EventDataWrapper> _filteredEvents;
        private List<string> _npcList;
        private HashSet<string> _hiddenEventIds;

        // ═══════════════════════════════════════════════════════
        // UI Components
        // ═══════════════════════════════════════════════════════

        private readonly List<ClickableComponent> _eventSlots;
        private readonly List<ClickableTextureComponent> _detailButtons;
        private readonly List<ClickableTextureComponent> _goToButtons;
        private readonly List<ClickableTextureComponent> _hideButtons;
        private readonly List<ClickableComponent> _npcSlots;

        private ClickableTextureComponent? _scrollBar;
        private Rectangle _scrollBarRunner;
        private bool _scrollBarHeld;

        private TextBox? _searchBox;
        private ClickableTextureComponent? _clearSearchButton;
        private string _lastSearchText = "";

        private TextBox? _npcSearchBox;
        private string _npcSearchText = "";
        private Rectangle _npcSearchBounds;
        private bool _npcSearchSelected;

        // ═══════════════════════════════════════════════════════
        // Filter State
        // ═══════════════════════════════════════════════════════

        private EventFilterMode _filterMode = EventFilterMode.Available;
        private static EventFilterMode _lastFilterMode = EventFilterMode.Available;
        private static int _lastSelectedNpcIndex = -1;

        private int _selectedNpcIndex = -1;
        private int _npcScrollOffset;
        private bool _showOnlyRelationships;

        private int _sortBy;
        private bool _sortAscending = true;
        private int _maxHeartsFilter = 14;

        // ═══════════════════════════════════════════════════════
        // Layout Dimensions
        // ═══════════════════════════════════════════════════════

        private int _eventBoxX, _eventBoxY, _eventBoxWidth, _eventBoxHeight;
        private int _optionsPanelX, _optionsPanelWidth;
        private int _startIndex, _endIndex;

        // ═══════════════════════════════════════════════════════
        // Button Bounds
        // ═══════════════════════════════════════════════════════

        private Rectangle _clearFilterBounds;
        private Rectangle _toggleSeenBounds;
        private Rectangle _toggleRelationshipsBounds;
        private Rectangle _sortButtonBounds;
        private Rectangle _sortOrderBounds;
        private Rectangle _heartsMinusBounds;
        private Rectangle _heartsPlusBounds;

        // ═══════════════════════════════════════════════════════
        // Touch/Drag State (Android)
        // ═══════════════════════════════════════════════════════

        private bool _isDraggingEventList;
        private bool _isDraggingNpcList;
        private int _lastDragY;
        private int _dragAccumulator;

        // ═══════════════════════════════════════════════════════
        // Properties
        // ═══════════════════════════════════════════════════════

        public bool Selected { get; set; }
        private bool UseDarkTheme => _api.UseDarkTheme;
        private bool IsAndroid => Constants.TargetPlatform == GamePlatform.Android;

        // ═══════════════════════════════════════════════════════
        // Constructor
        // ═══════════════════════════════════════════════════════

        public MobileMHEventsMenu(MHEventsListAPI api)
            : base(
                (Game1.uiViewport.Width - MENU_WIDTH) / 2,
                (Game1.uiViewport.Height - MENU_HEIGHT) / 2,
                MENU_WIDTH,
                MENU_HEIGHT,
                showUpperRightCloseButton: true)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));

            // Initialize collections
            _eventSlots = new List<ClickableComponent>();
            _detailButtons = new List<ClickableTextureComponent>();
            _goToButtons = new List<ClickableTextureComponent>();
            _hideButtons = new List<ClickableTextureComponent>();
            _npcSlots = new List<ClickableComponent>();
            _filteredEvents = new List<EventDataWrapper>();

            // Load data via API
            _hiddenEventIds = _api.GetHiddenEventIds();
            _allEvents = _api.GetAllEvents();
            _npcList = _api.GetNpcListWithEvents();

            // Restore last state
            _filterMode = _lastFilterMode;
            _selectedNpcIndex = _lastSelectedNpcIndex;
            if (_selectedNpcIndex >= _npcList.Count)
                _selectedNpcIndex = -1;

            // Setup UI
            CalculateLayout();
            SetupUIComponents();
            RefreshEventList();

            Logger.Debug($"MobileMHEventsMenu created with {_allEvents.Count} events");
        }

        // ═══════════════════════════════════════════════════════
        // Layout Calculation
        // ═══════════════════════════════════════════════════════

        private void CalculateLayout()
        {
            int panelWidthPercent = _api.GetConfigValue("OptionsPanelWidthPercent", 25);
            panelWidthPercent = Math.Clamp(panelWidthPercent, 15, 35);

            _optionsPanelX = xPositionOnScreen + 40;
            _optionsPanelWidth = (int)((float)((width - 80) * panelWidthPercent) / 100f);

            _eventBoxX = _optionsPanelX + _optionsPanelWidth + 40;
            _eventBoxY = yPositionOnScreen + 180;
            _eventBoxWidth = width - _optionsPanelWidth - 140;
            _eventBoxHeight = 533;
        }

        private void SetupUIComponents()
        {
            // Search box
            _searchBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null, Game1.smallFont, Game1.textColor)
            {
                X = _eventBoxX,
                Y = yPositionOnScreen + 115,
                Width = _eventBoxWidth - 60,
                Height = 36,
                Text = ""
            };

            _clearSearchButton = new ClickableTextureComponent(
                new Rectangle(_eventBoxX + _eventBoxWidth - 50, yPositionOnScreen + 115, 36, 36),
                Game1.mouseCursors,
                new Rectangle(322, 498, 12, 12),
                2.8f);

            // NPC search box
            _npcSearchBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null, Game1.smallFont, Game1.textColor)
            {
                X = 0,
                Y = 0,
                Width = 200,
                Height = 36,
                Text = ""
            };

            // Event slots & buttons
            bool showPlayButton = _api.GetConfigValue("ShowPlayInListInsteadOfHide", false);

            for (int i = 0; i < MAX_ENTRIES_SHOWN; i++)
            {
                _eventSlots.Add(new ClickableComponent(
                    new Rectangle(_eventBoxX + BOX_BORDER,
                        _eventBoxY + i * ENTRY_HEIGHT + BOX_BORDER,
                        _eventBoxWidth - 180, 80),
                    $"slot_{i}"));

                _detailButtons.Add(new ClickableTextureComponent(
                    new Rectangle(_eventBoxX + _eventBoxWidth - 160,
                        _eventBoxY + i * ENTRY_HEIGHT + 20, 40, 40),
                    Game1.mouseCursors,
                    new Rectangle(208, 320, 16, 16), 2.5f));

                _goToButtons.Add(new ClickableTextureComponent(
                    new Rectangle(_eventBoxX + _eventBoxWidth - 110,
                        _eventBoxY + i * ENTRY_HEIGHT + 20, 40, 40),
                    Game1.mouseCursors,
                    new Rectangle(0, 192, 64, 64), 0.6f));

                Rectangle hideSourceRect = showPlayButton
                    ? new Rectangle(310, 392, 16, 16)
                    : new Rectangle(322, 498, 12, 12);
                float hideScale = showPlayButton ? 2.5f : 2.8f;

                _hideButtons.Add(new ClickableTextureComponent(
                    new Rectangle(_eventBoxX + _eventBoxWidth - 60,
                        _eventBoxY + i * ENTRY_HEIGHT + 20, 40, 40),
                    Game1.mouseCursors,
                    hideSourceRect, hideScale));
            }

            // Scroll bar
            _scrollBarRunner = new Rectangle(
                _eventBoxX + _eventBoxWidth + 8, _eventBoxY, 24, _eventBoxHeight);
            _scrollBar = new ClickableTextureComponent(
                new Rectangle(_scrollBarRunner.X, _scrollBarRunner.Y, 24, 40),
                Game1.mouseCursors,
                new Rectangle(435, 463, 6, 10), 4f);

            // NPC slots
            int npcStartY = yPositionOnScreen + 130;
            for (int j = 0; j < MAX_NPC_SHOWN; j++)
            {
                _npcSlots.Add(new ClickableComponent(
                    new Rectangle(_optionsPanelX, npcStartY + j * 32,
                        _optionsPanelWidth - 10, 28),
                    $"npc_{j}"));
            }
        }

        // ═══════════════════════════════════════════════════════
        // Event Filtering & Sorting
        // ═══════════════════════════════════════════════════════

        private void RefreshEventList()
        {
            string? npcFilter = null;
            if (_selectedNpcIndex >= 0 && _selectedNpcIndex < _npcList.Count)
            {
                npcFilter = _npcList[_selectedNpcIndex];
            }

            // Gunakan API untuk filter
            _filteredEvents = _api.GetFilteredEvents(
                _filterMode,
                npcFilter,
                _searchBox?.Text,
                _maxHeartsFilter
            );

            // Apply relationship filter
            if (_showOnlyRelationships)
            {
                _filteredEvents = _filteredEvents
                    .Where(e => e.HeartRequirements?.Count > 0)
                    .ToList();
            }

            // Apply sorting
            ApplySorting();

            // Reset scroll
            _startIndex = 0;
            _endIndex = Math.Min(MAX_ENTRIES_SHOWN, _filteredEvents.Count);
            UpdateScrollBar();
        }

        private void ApplySorting()
        {
            if (_sortBy == 0) return;

            IOrderedEnumerable<EventDataWrapper>? ordered = null;

            switch (_sortBy)
            {
                case 1: // By ID
                    ordered = _sortAscending
                        ? _filteredEvents.OrderBy(e => e.Id)
                        : _filteredEvents.OrderByDescending(e => e.Id);
                    break;

                case 2: // By Location
                    ordered = _sortAscending
                        ? _filteredEvents.OrderBy(e => e.GetTranslatedLocation())
                        : _filteredEvents.OrderByDescending(e => e.GetTranslatedLocation());
                    break;

                case 3: // By Hearts
                    ordered = _sortAscending
                        ? _filteredEvents.OrderBy(e => e.HeartRequirements?.Max(h => h.Hearts) ?? 0)
                        : _filteredEvents.OrderByDescending(e => e.HeartRequirements?.Max(h => h.Hearts) ?? 0);
                    break;

                case 4: // By Mod
                    ordered = _sortAscending
                        ? _filteredEvents.OrderBy(e => e.ModName ?? "Vanilla", StringComparer.OrdinalIgnoreCase)
                        : _filteredEvents.OrderByDescending(e => e.ModName ?? "Vanilla", StringComparer.OrdinalIgnoreCase);
                    break;
            }

            if (ordered != null)
            {
                _filteredEvents = ordered.ToList();
            }
        }

        private void UpdateScrollBar()
        {
            if (_filteredEvents.Count <= MAX_ENTRIES_SHOWN)
            {
                _scrollBar.bounds.Y = _scrollBarRunner.Y;
                return;
            }

            int maxScroll = _filteredEvents.Count - MAX_ENTRIES_SHOWN;
            float ratio = (float)_startIndex / maxScroll;
            _scrollBar.bounds.Y = (int)((_scrollBarRunner.Height - 40) * ratio) + _scrollBarRunner.Y;
        }

        private void Scroll(bool up)
        {
            if (_filteredEvents.Count <= MAX_ENTRIES_SHOWN) return;

            if (up && _startIndex > 0)
            {
                _startIndex--;
                _endIndex--;
            }
            else if (!up && _endIndex < _filteredEvents.Count)
            {
                _startIndex++;
                _endIndex++;
            }

            UpdateScrollBar();
        }

        // ═══════════════════════════════════════════════════════
        // Draw Methods
        // ═══════════════════════════════════════════════════════

        public override void draw(SpriteBatch b)
        {
            // Dim background
            b.Draw(Game1.fadeToBlackRect,
                new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height),
                Color.Black * 0.75f);

            // Main menu box
            Color boxColor = UseDarkTheme ? new Color(40, 40, 50) : Color.White;
            IClickableMenu.drawTextureBox(b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height, boxColor);

            // Title
            DrawTitle(b);

            // Status text
            DrawStatusText(b);

            // Search box
            DrawSearchBox(b);

            // Options panel
            DrawOptionsPanel(b);

            // Event list box
            DrawEventListBox(b);

            // Events
            DrawEvents(b);

            // Scroll bar
            if (_filteredEvents.Count > MAX_ENTRIES_SHOWN)
            {
                IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
                    new Rectangle(403, 383, 6, 6),
                    _scrollBarRunner.X, _scrollBarRunner.Y,
                    _scrollBarRunner.Width, _scrollBarRunner.Height,
                    Color.White, 4f, drawShadow: false);
                _scrollBar.draw(b);
            }

            // Base (close button)
            base.draw(b);

            // Tooltips
            DrawTooltips(b);

            drawMouse(b);
        }

        private void DrawTitle(SpriteBatch b)
        {
            string title = _api.GetTranslation("menu.title");
            if (string.IsNullOrEmpty(title) || title == "menu.title")
                title = "Events List";

            if (UseDarkTheme)
            {
                Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
                b.DrawString(Game1.dialogueFont, title,
                    new Vector2(xPositionOnScreen + (width - titleSize.X) / 2f, yPositionOnScreen + 12),
                    new Color(220, 180, 120));
            }
            else
            {
                SpriteText.drawStringHorizontallyCenteredAt(b, title,
                    xPositionOnScreen + width / 2, yPositionOnScreen + 12);
            }
        }

        private void DrawStatusText(SpriteBatch b)
        {
            Color textColor = UseDarkTheme ? Color.White : Color.Black;
            int statusY = yPositionOnScreen + 22;

            // View mode
            string viewText = GetViewModeText();
            b.DrawString(Game1.smallFont, viewText,
                new Vector2(_optionsPanelX, statusY), textColor);

            // Filter mode
            string filterText = _showOnlyRelationships
                ? "Filter: Relationships"
                : "Filter: All";
            b.DrawString(Game1.smallFont, filterText,
                new Vector2(_optionsPanelX, statusY + 20), textColor);

            // Selected NPC
            if (_selectedNpcIndex >= 0 && _selectedNpcIndex < _npcList.Count)
            {
                b.DrawString(Game1.smallFont, $"NPC: {_npcList[_selectedNpcIndex]}",
                    new Vector2(_optionsPanelX, statusY + 40), textColor);
            }

            // Event count
            b.DrawString(Game1.smallFont, $"Events: {_filteredEvents.Count}",
                new Vector2(_optionsPanelX, statusY + 56 + 20), textColor);
        }

        private string GetViewModeText()
        {
            return _filterMode switch
            {
                EventFilterMode.Available => "View: Available",
                EventFilterMode.Hidden => "View: Hidden",
                EventFilterMode.Seen => "View: Seen",
                EventFilterMode.All => "View: All",
                EventFilterMode.ContentPatcher => "View: Content Patcher",
                _ => "View: All"
            };
        }

        private void DrawSearchBox(SpriteBatch b)
        {
            Rectangle searchBounds = new Rectangle(
                _searchBox.X, _searchBox.Y, _searchBox.Width, _searchBox.Height);

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
                new Rectangle(403, 373, 9, 9),
                searchBounds.X, searchBounds.Y, searchBounds.Width, searchBounds.Height,
                _searchBox.Selected ? Color.Wheat : Color.White,
                3f, drawShadow: false);

            string displayText = string.IsNullOrEmpty(_searchBox.Text)
                ? "Search events..."
                : _searchBox.Text;

            float textHeight = Game1.smallFont.MeasureString(displayText).Y;
            int textY = (int)((searchBounds.Height - textHeight) / 2f);

            b.DrawString(Game1.smallFont, displayText,
                new Vector2(searchBounds.X + 10, searchBounds.Y + textY),
                Color.Black);

            if (!string.IsNullOrEmpty(_searchBox.Text))
            {
                _clearSearchButton.draw(b);
            }
        }

        private void DrawOptionsPanel(SpriteBatch b)
        {
            Color textColor = UseDarkTheme ? Color.White : Color.Black;
            int panelY = yPositionOnScreen + 160;
            int panelHeight = _eventBoxHeight + 30;

            // Panel background
            if (UseDarkTheme)
            {
                IClickableMenu.drawTextureBox(b, Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    _optionsPanelX - 10, panelY - 10,
                    _optionsPanelWidth + 10, panelHeight,
                    new Color(50, 50, 60));
            }
            else
            {
                Game1.DrawBox(_optionsPanelX - 10, panelY - 10,
                    _optionsPanelWidth + 10, panelHeight);
            }

            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // Draw NPC list
            DrawNPCList(b, panelY, mouseX, mouseY, textColor);

            // Draw buttons
            DrawOptionButtons(b, panelY, panelHeight, mouseX, mouseY);
        }

        private void DrawNPCList(SpriteBatch b, int startY, int mouseX, int mouseY, Color textColor)
        {
            // NPC section header
            b.DrawString(Game1.smallFont, "Characters",
                new Vector2(_optionsPanelX, startY), textColor);

            // NPC search box
            int searchY = startY + 29;
            _npcSearchBounds = new Rectangle(_optionsPanelX + 4, searchY, _optionsPanelWidth - 28, 38);

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
                new Rectangle(403, 373, 9, 9),
                _npcSearchBounds.X, _npcSearchBounds.Y,
                _npcSearchBounds.Width, _npcSearchBounds.Height,
                _npcSearchSelected ? Color.Wheat : Color.White,
                3f, drawShadow: false);

            string npcSearchDisplay = string.IsNullOrEmpty(_npcSearchText)
                ? "Search NPC..."
                : _npcSearchText;

            float textHeight = Game1.smallFont.MeasureString(npcSearchDisplay).Y;
            int textYOffset = (int)((_npcSearchBounds.Height - textHeight) / 2f);

            b.DrawString(Game1.smallFont, npcSearchDisplay,
                new Vector2(_npcSearchBounds.X + 10, _npcSearchBounds.Y + textYOffset),
                Color.Black);

            // NPC list
            List<string> visibleNpcs = string.IsNullOrEmpty(_npcSearchText)
                ? _npcList
                : _npcList.Where(n => n.ToLower().Contains(_npcSearchText.ToLower())).ToList();

            int listY = searchY + 48;
            int maxVisible = Math.Min(7, visibleNpcs.Count - _npcScrollOffset);

            for (int i = 0; i < maxVisible; i++)
            {
                int npcIndex = i + _npcScrollOffset;
                if (npcIndex >= visibleNpcs.Count) break;

                string npcName = visibleNpcs[npcIndex];
                int actualIndex = _npcList.IndexOf(npcName);

                Rectangle slotBounds = new Rectangle(
                    _optionsPanelX, listY + i * 28, _optionsPanelWidth - 20, 26);

                if (i < _npcSlots.Count)
                    _npcSlots[i].bounds = slotBounds;

                bool isSelected = actualIndex == _selectedNpcIndex;
                bool isHovered = slotBounds.Contains(mouseX, mouseY);

                // Background
                if (isSelected)
                {
                    b.Draw(Game1.staminaRect, slotBounds, new Color(86, 22, 12) * 0.6f);
                }
                else if (isHovered)
                {
                    b.Draw(Game1.staminaRect, slotBounds,
                        (UseDarkTheme ? Color.Gray : Color.White) * 0.25f);
                }

                // Truncate name if too long
                string displayName = npcName;
                while (Game1.smallFont.MeasureString(displayName + "...").X > _optionsPanelWidth - 30
                       && displayName.Length > 3)
                {
                    displayName = displayName.Substring(0, displayName.Length - 1);
                }
                if (displayName != npcName) displayName += "...";

                Color nameColor = isSelected ? Color.White : textColor;
                b.DrawString(Game1.smallFont, displayName,
                    new Vector2(_optionsPanelX + 5, listY + i * 28 + 3), nameColor);
            }
        }

        private void DrawOptionButtons(SpriteBatch b, int panelY, int panelHeight, int mouseX, int mouseY)
        {
            int buttonHeight = 40;
            int smallButtonHeight = 32;
            int spacing = 6;
            int buttonWidth = _optionsPanelWidth - 20;
            int halfWidth = (buttonWidth - 10) / 2;

            // Calculate positions from bottom
            int bottomY = panelY + panelHeight - 15;

            // Clear filter button
            _clearFilterBounds = new Rectangle(_optionsPanelX, bottomY - buttonHeight, buttonWidth, buttonHeight);
            DrawButton(b, _clearFilterBounds, "Clear Filter", mouseX, mouseY);

            // Toggle seen button
            string toggleSeenText = GetToggleSeenText();
            _toggleSeenBounds = new Rectangle(_optionsPanelX,
                _clearFilterBounds.Y - buttonHeight - spacing, buttonWidth, buttonHeight);
            DrawButton(b, _toggleSeenBounds, toggleSeenText, mouseX, mouseY,
                _filterMode == EventFilterMode.ContentPatcher);

            // Toggle relationships button
            string relText = _showOnlyRelationships ? "Relationships Only" : "All Events";
            _toggleRelationshipsBounds = new Rectangle(_optionsPanelX,
                _toggleSeenBounds.Y - buttonHeight - spacing, buttonWidth, buttonHeight);
            DrawButton(b, _toggleRelationshipsBounds, relText, mouseX, mouseY, _showOnlyRelationships);

            // Sort buttons
            int sortY = _toggleRelationshipsBounds.Y - smallButtonHeight - spacing - 10;

            string sortText = _sortBy switch
            {
                0 => "Sort: None",
                1 => "Sort: ID",
                2 => "Sort: Location",
                3 => "Sort: Hearts",
                4 => "Sort: Mod",
                _ => "Sort: None"
            };

            _sortButtonBounds = new Rectangle(_optionsPanelX, sortY, halfWidth, smallButtonHeight);
            DrawSmallButton(b, _sortButtonBounds, sortText, mouseX, mouseY);

            string orderText = _sortAscending ? "↑ Asc" : "↓ Desc";
            _sortOrderBounds = new Rectangle(_optionsPanelX + halfWidth + 10, sortY, halfWidth, smallButtonHeight);
            DrawSmallButton(b, _sortOrderBounds, orderText, mouseX, mouseY, isActive: false, _sortBy > 0);

            // Hearts filter
            bool heartsClickable = _selectedNpcIndex >= 0;
            int heartsY = sortY - smallButtonHeight - spacing;
            int btnSize = 32;
            int heartsDisplayWidth = buttonWidth - btnSize * 2 - 10;

            _heartsMinusBounds = new Rectangle(_optionsPanelX, heartsY, btnSize, btnSize);
            DrawSmallButton(b, _heartsMinusBounds, "-", mouseX, mouseY, isActive: false, heartsClickable);

            string heartsText = _maxHeartsFilter >= 14 ? "All Hearts" : $"{_maxHeartsFilter} Hearts";
            Rectangle heartsDisplay = new Rectangle(_optionsPanelX + btnSize + 5, heartsY, heartsDisplayWidth, btnSize);
            DrawSmallButton(b, heartsDisplay, heartsText, mouseX, mouseY, isActive: false, heartsClickable);

            _heartsPlusBounds = new Rectangle(_optionsPanelX + btnSize + heartsDisplayWidth + 10, heartsY, btnSize, btnSize);
            DrawSmallButton(b, _heartsPlusBounds, "+", mouseX, mouseY, isActive: false, heartsClickable);
        }

        private string GetToggleSeenText()
        {
            return _filterMode switch
            {
                EventFilterMode.Available => "Show All",
                EventFilterMode.All => "Show Hidden",
                EventFilterMode.Hidden => "Show Seen",
                EventFilterMode.Seen => "Show CP",
                EventFilterMode.ContentPatcher => "Show Available",
                _ => "Show All"
            };
        }

        private void DrawButton(SpriteBatch b, Rectangle bounds, string text, int mouseX, int mouseY, bool isActive = false)
        {
            bool isHovered = bounds.Contains(mouseX, mouseY);

            Color bgColor, textColor;
            if (UseDarkTheme)
            {
                bgColor = isActive ? new Color(60, 100, 60)
                    : isHovered ? new Color(70, 70, 80) : new Color(50, 50, 60);
                textColor = Color.White;
            }
            else
            {
                bgColor = isActive ? new Color(200, 230, 200)
                    : isHovered ? Color.Wheat : Color.White;
                textColor = Color.Black;
            }

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                bgColor, 4f, drawShadow: false);

            Vector2 textSize = Game1.smallFont.MeasureString(text);
            b.DrawString(Game1.smallFont, text,
                new Vector2(bounds.X + (bounds.Width - textSize.X) / 2f,
                           bounds.Y + (bounds.Height - textSize.Y) / 2f),
                textColor);
        }

        private void DrawSmallButton(SpriteBatch b, Rectangle bounds, string text,
            int mouseX, int mouseY, bool isActive = false, bool isClickable = true)
        {
            bool isHovered = isClickable && bounds.Contains(mouseX, mouseY);

            Color bgColor, textColor;
            if (UseDarkTheme)
            {
                bgColor = isActive ? new Color(60, 100, 60)
                    : isHovered ? new Color(70, 70, 80) : new Color(50, 50, 60);
                textColor = isClickable ? Color.White : new Color(120, 120, 120);
            }
            else
            {
                bgColor = isActive ? new Color(200, 230, 200)
                    : isHovered ? Color.Wheat : Color.White;
                if (!isClickable) bgColor = Color.LightGray;
                textColor = Color.Black;
            }

            IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
                new Rectangle(432, 439, 9, 9),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                bgColor, 3f, drawShadow: false);

            Vector2 textSize = Game1.smallFont.MeasureString(text) * 0.9f;
            b.DrawString(Game1.smallFont, text,
                new Vector2(bounds.X + (bounds.Width - textSize.X) / 2f,
                           bounds.Y + (bounds.Height - textSize.Y) / 2f),
                textColor, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 1f);
        }

        private void DrawEventListBox(SpriteBatch b)
        {
            if (UseDarkTheme)
            {
                IClickableMenu.drawTextureBox(b, Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    _eventBoxX, _eventBoxY, _eventBoxWidth, _eventBoxHeight,
                    new Color(50, 50, 60));
            }
            else
            {
                Game1.DrawBox(_eventBoxX, _eventBoxY, _eventBoxWidth, _eventBoxHeight);
            }
        }

        private void DrawEvents(SpriteBatch b)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            if (_filteredEvents.Count == 0)
            {
                DrawNoResults(b);
                return;
            }

            for (int i = 0; i < MAX_ENTRIES_SHOWN; i++)
            {
                int index = _startIndex + i;
                if (index >= _filteredEvents.Count) break;

                EventDataWrapper evt = _filteredEvents[index];
                int yPos = _eventBoxY + i * ENTRY_HEIGHT + BOX_BORDER;

                // Hover highlight
                if (_eventSlots[i].containsPoint(mouseX, mouseY))
                {
                    b.Draw(Game1.staminaRect,
                        new Rectangle(_eventBoxX + 4, yPos - 4, _eventBoxWidth - 8, 84),
                        Color.White * 0.2f);
                }

                DrawEventEntry(b, evt, yPos, i);

                // Separator line
                if (i < MAX_ENTRIES_SHOWN - 1 && index < _filteredEvents.Count - 1)
                {
                    b.Draw(Game1.menuTexture,
                        new Rectangle(_eventBoxX + 8, yPos + ENTRY_HEIGHT - 8, _eventBoxWidth - 16, 4),
                        Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 25),
                        Color.White * 0.5f);
                }
            }
        }

        private void DrawNoResults(SpriteBatch b)
        {
            Color textColor = (UseDarkTheme ? Color.LightGray : Color.Gray) * 0.7f;

            string noResultsText = "No events found";
            string hintText = "Try adjusting your filters";

            Vector2 size1 = Game1.smallFont.MeasureString(noResultsText);
            Vector2 size2 = Game1.smallFont.MeasureString(hintText);

            int centerX = _eventBoxX + _eventBoxWidth / 2;
            int centerY = _eventBoxY + _eventBoxHeight / 2;

            b.DrawString(Game1.smallFont, noResultsText,
                new Vector2(centerX - size1.X / 2f, centerY - 20), textColor);
            b.DrawString(Game1.smallFont, hintText,
                new Vector2(centerX - size2.X / 2f, centerY + 10), textColor * 0.8f);
        }

        private void DrawEventEntry(SpriteBatch b, EventDataWrapper evt, int yPos, int buttonIndex)
        {
            Color textColor = UseDarkTheme ? Color.White : Color.Black;
            Color locationColor = UseDarkTheme ? new Color(200, 150, 100) : new Color(86, 22, 12);
            Color conditionColor = UseDarkTheme ? Color.LightGray : Color.DimGray;

            int textX = _eventBoxX + BOX_BORDER + 8;
            int maxTextWidth = _eventBoxWidth - 180;
            int lineHeight = 22;
            int currentY = yPos + 4;

            bool showThreeLines = _api.GetConfigValue("EventListFormat", 0) == 2; // ThreeLines
            bool showModName = _api.GetConfigValue("ShowModNameInList", false);
            bool isSeen = Game1.player.eventsSeen.Contains(evt.Id);

            // Line 1: ID (and maybe mod name)
            string idText = $"ID: {evt.Id}";
            if (showModName && !string.IsNullOrEmpty(evt.ModName))
            {
                idText += $" - {evt.ModName}";
            }
            idText = TruncateText(idText, maxTextWidth - 80);
            b.DrawString(Game1.smallFont, idText, new Vector2(textX, currentY), textColor);

            // Status indicators
            int statusX = _eventBoxX + _eventBoxWidth - 240;
            if (isSeen)
            {
                b.DrawString(Game1.smallFont, "[Seen]", new Vector2(statusX, currentY), Color.ForestGreen);
                statusX += 60;
            }
            if (evt.HasInvalidScript)
            {
                b.DrawString(Game1.smallFont, "[NULL]", new Vector2(statusX, currentY), Color.OrangeRed);
            }

            currentY += lineHeight + 2;

            // Line 2: Location
            string location = TruncateText(evt.GetTranslatedLocation(), maxTextWidth - 100);
            b.DrawString(Game1.smallFont, location, new Vector2(textX, currentY), locationColor);

            currentY += lineHeight + 2;

            // Line 3: Conditions (if showing three lines)
            if (showThreeLines)
            {
                string? conditions = evt.GetConditionsSummary();
                if (!string.IsNullOrEmpty(conditions))
                {
                    conditions = TruncateText(conditions, maxTextWidth);
                    b.DrawString(Game1.smallFont, conditions, new Vector2(textX, currentY), conditionColor);
                }
            }

            // Draw buttons
            int buttonY = yPos + 24 + 10;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            // Detail button
            bool detailHovered = _detailButtons[buttonIndex].containsPoint(mouseX, mouseY);
            _detailButtons[buttonIndex].bounds.Y = buttonY;
            _detailButtons[buttonIndex].draw(b, Color.White * (detailHovered ? 0.7f : 1f), 0.88f);

            // Go to button
            if (_api.ShowGoToLocationButton)
            {
                bool goToHovered = _goToButtons[buttonIndex].containsPoint(mouseX, mouseY);
                _goToButtons[buttonIndex].bounds.Y = buttonY;
                _goToButtons[buttonIndex].draw(b, Color.White * (goToHovered ? 0.7f : 1f), 0.88f);
            }

            // Hide/Play button
            bool hideHovered = _hideButtons[buttonIndex].containsPoint(mouseX, mouseY);
            _hideButtons[buttonIndex].bounds.Y = buttonY;

            bool isHidden = _hiddenEventIds.Contains(evt.Id);
            Color hideColor = isHidden ? Color.Orange : Color.White;

            if (_api.GetConfigValue("ShowPlayInListInsteadOfHide", false))
            {
                hideColor = Color.Gold;
            }
            else if (_filterMode == EventFilterMode.Hidden)
            {
                hideColor = Color.LightGreen;
                _hideButtons[buttonIndex].sourceRect = new Rectangle(310, 392, 16, 16);
            }

            _hideButtons[buttonIndex].draw(b, hideColor * (hideHovered ? 0.7f : 1f), 0.88f);
        }

        private string TruncateText(string text, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";

            while (Game1.smallFont.MeasureString(text).X > maxWidth && text.Length > 3)
            {
                text = text.Substring(0, text.Length - 1);
            }

            if (text.Length < text.Length)
            {
                text += "...";
            }

            return text;
        }

        private void DrawTooltips(SpriteBatch b)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            for (int i = 0; i < MAX_ENTRIES_SHOWN; i++)
            {
                int index = _startIndex + i;
                if (index >= _filteredEvents.Count) break;

                if (_detailButtons[i].containsPoint(mouseX, mouseY))
                {
                    IClickableMenu.drawToolTip(b, "View Details", "", null);
                    return;
                }

                if (_api.ShowGoToLocationButton && _goToButtons[i].containsPoint(mouseX, mouseY))
                {
                    IClickableMenu.drawToolTip(b, "Go to Location", "", null);
                    return;
                }

                if (_hideButtons[i].containsPoint(mouseX, mouseY))
                {
                    bool showPlay = _api.GetConfigValue("ShowPlayInListInsteadOfHide", false);
                    string tooltip = showPlay ? "Play Event"
                        : (_filterMode == EventFilterMode.Hidden ? "Unhide" : "Hide");
                    IClickableMenu.drawToolTip(b, tooltip, "", null);
                    return;
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // Input Handling
        // ═══════════════════════════════════════════════════════

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            // Search box click
            if (new Rectangle(_searchBox.X, _searchBox.Y, _searchBox.Width, _searchBox.Height).Contains(x, y))
            {
                SelectSearchBox();
                return;
            }
            DeselectSearchBox();

            // Clear search button
            if (!string.IsNullOrEmpty(_searchBox.Text) && _clearSearchButton.containsPoint(x, y))
            {
                Game1.playSound("drumkit6");
                _searchBox.Text = "";
                RefreshEventList();
                return;
            }

            // NPC search box click
            if (_npcSearchBounds.Contains(x, y))
            {
                SelectNpcSearchBox();
                return;
            }
            DeselectNpcSearchBox();

            // Scroll bar
            if (_scrollBar.containsPoint(x, y) || _scrollBarRunner.Contains(x, y))
            {
                _scrollBarHeld = true;
                leftClickHeld(x, y);
                return;
            }

            // Android drag start
            if (IsAndroid)
            {
                HandleAndroidDragStart(x, y);
            }

            // NPC slot clicks
            if (HandleNpcSlotClick(x, y)) return;

            // Button clicks
            if (HandleButtonClicks(x, y)) return;

            // Event button clicks
            HandleEventButtonClicks(x, y);
        }

        private void HandleAndroidDragStart(int x, int y)
        {
            // Event list drag
            if (new Rectangle(_eventBoxX, _eventBoxY, _eventBoxWidth, _eventBoxHeight).Contains(x, y))
            {
                bool onButton = false;
                for (int i = 0; i < MAX_ENTRIES_SHOWN; i++)
                {
                    if (_detailButtons[i].containsPoint(x, y) ||
                        _goToButtons[i].containsPoint(x, y) ||
                        _hideButtons[i].containsPoint(x, y))
                    {
                        onButton = true;
                        break;
                    }
                }

                if (!onButton)
                {
                    _isDraggingEventList = true;
                    _lastDragY = y;
                    _dragAccumulator = 0;
                }
            }

            // NPC list drag
            int npcListY = yPositionOnScreen + 130;
            if (x >= _optionsPanelX && x <= _optionsPanelX + _optionsPanelWidth &&
                y >= npcListY && y <= npcListY + 256 && !_npcSearchBounds.Contains(x, y))
            {
                bool onSlot = _npcSlots.Any(s => s.containsPoint(x, y));
                if (!onSlot)
                {
                    _isDraggingNpcList = true;
                    _lastDragY = y;
                    _dragAccumulator = 0;
                }
            }
        }

        private bool HandleNpcSlotClick(int x, int y)
        {
            List<string> visibleNpcs = string.IsNullOrEmpty(_npcSearchText)
                ? _npcList
                : _npcList.Where(n => n.ToLower().Contains(_npcSearchText.ToLower())).ToList();

            for (int i = 0; i < 7; i++)
            {
                int npcIndex = i + _npcScrollOffset;
                if (npcIndex >= visibleNpcs.Count) break;

                if (i < _npcSlots.Count && _npcSlots[i].containsPoint(x, y))
                {
                    Game1.playSound("smallSelect");
                    string npcName = visibleNpcs[npcIndex];
                    int actualIndex = _npcList.IndexOf(npcName);

                    _selectedNpcIndex = (_selectedNpcIndex == actualIndex) ? -1 : actualIndex;
                    _lastSelectedNpcIndex = _selectedNpcIndex;
                    RefreshEventList();
                    return true;
                }
            }

            return false;
        }

        private bool HandleButtonClicks(int x, int y)
        {
            // Sort button
            if (_sortButtonBounds.Contains(x, y))
            {
                Game1.playSound("drumkit6");
                bool showModName = _api.GetConfigValue("ShowModNameInList", false);
                int maxSort = showModName ? 5 : 4;
                _sortBy = (_sortBy + 1) % maxSort;
                RefreshEventList();
                return true;
            }

            // Sort order
            if (_sortOrderBounds.Contains(x, y) && _sortBy > 0)
            {
                Game1.playSound("drumkit6");
                _sortAscending = !_sortAscending;
                RefreshEventList();
                return true;
            }

            // Hearts minus
            if (_heartsMinusBounds.Contains(x, y) && _selectedNpcIndex >= 0)
            {
                Game1.playSound("drumkit6");
                _maxHeartsFilter = Math.Max(0, _maxHeartsFilter - 1);
                RefreshEventList();
                return true;
            }

            // Hearts plus
            if (_heartsPlusBounds.Contains(x, y) && _selectedNpcIndex >= 0)
            {
                Game1.playSound("drumkit6");
                _maxHeartsFilter = Math.Min(14, _maxHeartsFilter + 1);
                RefreshEventList();
                return true;
            }

            // Clear filter
            if (_clearFilterBounds.Contains(x, y))
            {
                Game1.playSound("bigDeSelect");
                _selectedNpcIndex = -1;
                _lastSelectedNpcIndex = -1;
                _maxHeartsFilter = 14;
                _searchBox.Text = "";
                RefreshEventList();
                return true;
            }

            // Toggle seen/filter mode
            if (_toggleSeenBounds.Contains(x, y))
            {
                Game1.playSound("drumkit6");
                _filterMode = _filterMode switch
                {
                    EventFilterMode.Available => EventFilterMode.All,
                    EventFilterMode.All => EventFilterMode.Hidden,
                    EventFilterMode.Hidden => EventFilterMode.Seen,
                    EventFilterMode.Seen => EventFilterMode.ContentPatcher,
                    EventFilterMode.ContentPatcher => EventFilterMode.Available,
                    _ => EventFilterMode.Available
                };
                _lastFilterMode = _filterMode;
                RefreshEventList();
                return true;
            }

            // Toggle relationships
            if (_toggleRelationshipsBounds.Contains(x, y))
            {
                Game1.playSound("drumkit6");
                _showOnlyRelationships = !_showOnlyRelationships;
                RefreshEventList();
                return true;
            }

            return false;
        }

        private void HandleEventButtonClicks(int x, int y)
        {
            for (int i = 0; i < MAX_ENTRIES_SHOWN; i++)
            {
                int index = _startIndex + i;
                if (index >= _filteredEvents.Count) break;

                EventDataWrapper evt = _filteredEvents[index];

                // Hide/Play button
                if (_hideButtons[i].containsPoint(x, y))
                {
                    if (_api.GetConfigValue("ShowPlayInListInsteadOfHide", false))
                    {
                        Game1.playSound("newArtifact");
                        exitThisMenuNoSound();
                        _api.PlayEvent(evt, _api.MarkAsSeenWhenPlaying);
                    }
                    else
                    {
                        Game1.playSound("drumkit6");
                        if (_filterMode == EventFilterMode.Hidden)
                        {
                            _api.UnhideEvent(evt.Id);
                            _hiddenEventIds.Remove(evt.Id);
                        }
                        else
                        {
                            _api.HideEvent(evt.Id);
                            _hiddenEventIds.Add(evt.Id);
                        }
                        _startIndex = 0;
                        RefreshEventList();
                    }
                    return;
                }

                // Detail button
                if (_detailButtons[i].containsPoint(x, y))
                {
                    Game1.playSound("bigSelect");
                    _api.OpenEventDetail(evt, this);
                    return;
                }

                // Go to button
                if (_api.ShowGoToLocationButton && _goToButtons[i].containsPoint(x, y))
                {
                    Game1.playSound("drumkit6");
                    WarpToEvent(evt);
                    return;
                }
            }
        }

        private void WarpToEvent(EventDataWrapper evt)
        {
            try
            {
                GameLocation? location = Game1.getLocationFromName(evt.LocationName);
                if (location != null && location.warps.Count > 0)
                {
                    exitThisMenu();
                    Game1.warpFarmer(evt.LocationName, location.warps[0].X, location.warps[0].Y, false);
                    Game1.addHUDMessage(new HUDMessage($"Warped to {evt.GetTranslatedLocation()}", 2));
                }
                else
                {
                    Game1.addHUDMessage(new HUDMessage("Location not found", 3));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Warp error: {ex.Message}");
                Game1.addHUDMessage(new HUDMessage("Warp failed", 3));
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);

            // Scroll bar drag
            if (_scrollBarHeld && _filteredEvents.Count > MAX_ENTRIES_SHOWN)
            {
                int relativeY = Math.Clamp(y - _scrollBarRunner.Y, 0, _scrollBarRunner.Height);
                int maxScroll = _filteredEvents.Count - MAX_ENTRIES_SHOWN;
                float ratio = (float)relativeY / _scrollBarRunner.Height;
                _startIndex = (int)(ratio * maxScroll);
                _endIndex = _startIndex + MAX_ENTRIES_SHOWN;
                UpdateScrollBar();
            }

            // Android drag scrolling
            if (!IsAndroid) return;

            int deltaY = _lastDragY - y;
            _lastDragY = y;

            if (_isDraggingEventList && _filteredEvents.Count > MAX_ENTRIES_SHOWN)
            {
                _dragAccumulator += deltaY;
                int threshold = 44;

                while (_dragAccumulator >= threshold)
                {
                    Scroll(up: false);
                    _dragAccumulator -= threshold;
                }
                while (_dragAccumulator <= -threshold)
                {
                    Scroll(up: true);
                    _dragAccumulator += threshold;
                }
            }

            if (_isDraggingNpcList)
            {
                _dragAccumulator += deltaY;
                int threshold = 16;

                List<string> visibleNpcs = string.IsNullOrEmpty(_npcSearchText)
                    ? _npcList
                    : _npcList.Where(n => n.ToLower().Contains(_npcSearchText.ToLower())).ToList();

                while (_dragAccumulator >= threshold)
                {
                    if (_npcScrollOffset < visibleNpcs.Count - MAX_NPC_SHOWN + 2)
                        _npcScrollOffset++;
                    _dragAccumulator -= threshold;
                }
                while (_dragAccumulator <= -threshold)
                {
                    if (_npcScrollOffset > 0)
                        _npcScrollOffset--;
                    _dragAccumulator += threshold;
                }
            }
        }

        public override void releaseLeftClick(int x, int y)
        {
            base.releaseLeftClick(x, y);
            _scrollBarHeld = false;
            _isDraggingEventList = false;
            _isDraggingNpcList = false;
            _dragAccumulator = 0;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);

            int mouseX = Game1.getMouseX();

            if (mouseX < _eventBoxX)
            {
                // NPC list scroll
                if (direction > 0 && _npcScrollOffset > 0)
                    _npcScrollOffset--;
                else if (direction < 0 && _npcScrollOffset < _npcList.Count - MAX_NPC_SHOWN + 2)
                    _npcScrollOffset++;
            }
            else
            {
                // Event list scroll
                Scroll(direction > 0);
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            if (_npcSearchSelected)
            {
                if (key == Keys.Escape)
                    DeselectNpcSearchBox();
                return;
            }

            if (_searchBox.Selected)
            {
                if (key == Keys.Escape)
                    DeselectSearchBox();
                return;
            }

            base.receiveKeyPress(key);
        }

        public override void update(GameTime time)
        {
            base.update(time);

            // Check for search text changes
            if (_searchBox.Text != _lastSearchText)
            {
                _lastSearchText = _searchBox.Text;
                RefreshEventList();
            }

            // Sync NPC search box for Android
            if (IsAndroid && _npcSearchBox != null && _npcSearchBox.Text != _npcSearchText)
            {
                _npcSearchText = _npcSearchBox.Text;
                _npcScrollOffset = 0;
            }
        }

        // ═══════════════════════════════════════════════════════
        // Search Box Management
        // ═══════════════════════════════════════════════════════

        private void SelectSearchBox()
        {
            _searchBox.Selected = true;
            DeselectNpcSearchBox();

            if (IsAndroid)
            {
                // Show Android keyboard via reflection
                var showMethod = typeof(TextBox).GetMethod("ShowAndroidKeyboard",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                showMethod?.Invoke(_searchBox, null);
            }
            else
            {
                Game1.keyboardDispatcher.Subscriber = _searchBox;
            }
        }

        private void DeselectSearchBox()
        {
            Game1.closeTextEntry();
            _searchBox.Selected = false;

            if (IsAndroid)
            {
                var hideMethod = typeof(TextBox).GetMethod("HideStatusBar",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                hideMethod?.Invoke(_searchBox, null);
            }
            else if (Game1.keyboardDispatcher.Subscriber == _searchBox)
            {
                Game1.keyboardDispatcher.Subscriber = null;
            }
        }

        private void SelectNpcSearchBox()
        {
            DeselectSearchBox();
            _npcSearchSelected = true;

            if (IsAndroid)
            {
                _npcSearchBox.Text = _npcSearchText;
                var showMethod = typeof(TextBox).GetMethod("ShowAndroidKeyboard",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                showMethod?.Invoke(_npcSearchBox, null);
            }
            else
            {
                Game1.keyboardDispatcher.Subscriber = this;
            }
        }

        private void DeselectNpcSearchBox()
        {
            Game1.closeTextEntry();
            _npcSearchSelected = false;

            if (Game1.keyboardDispatcher.Subscriber == this)
            {
                Game1.keyboardDispatcher.Subscriber = null;
            }

            if (IsAndroid)
            {
                var hideMethod = typeof(TextBox).GetMethod("HideStatusBar",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                hideMethod?.Invoke(_npcSearchBox, null);
            }
        }

        protected override void cleanupBeforeExit()
        {
            if (!IsAndroid && (Game1.keyboardDispatcher.Subscriber == this ||
                               Game1.keyboardDispatcher.Subscriber == _searchBox))
            {
                Game1.keyboardDispatcher.Subscriber = null;
            }

            base.cleanupBeforeExit();
        }

        // ═══════════════════════════════════════════════════════
        // IKeyboardSubscriber Implementation
        // ═══════════════════════════════════════════════════════

        public void RecieveTextInput(char inputChar)
        {
            if (_npcSearchSelected && !char.IsControl(inputChar))
            {
                _npcSearchText += inputChar;
                _npcScrollOffset = 0;
            }
        }

        public void RecieveTextInput(string text)
        {
            if (_npcSearchSelected)
            {
                _npcSearchText += text;
                _npcScrollOffset = 0;
            }
        }

        public void RecieveCommandInput(char command)
        {
            if (_npcSearchSelected && command == '\b' && _npcSearchText.Length > 0)
            {
                _npcSearchText = _npcSearchText.Substring(0, _npcSearchText.Length - 1);
                _npcScrollOffset = 0;
            }
        }

        public void RecieveSpecialInput(Keys key)
        {
            // Not needed
        }
    }
}