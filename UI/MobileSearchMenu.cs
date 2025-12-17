// File: Integrations/LookupAnything/MobileSearchMenu.cs

using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MobileUISupport.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MobileUISupport.Integrations.LookupAnything
{
    public class MobileSearchMenu : IClickableMenu
    {
        // ═══════════════════════════════════════════════════════
        // Constants
        // ═══════════════════════════════════════════════════════

        private const int PADDING = 16;
        private const int SEARCH_BOX_HEIGHT = 52;
        private const int CATEGORY_HEIGHT = 44;
        private const int ITEM_HEIGHT = 80;          // Diperbesar untuk jarak
        private const int ITEM_SEPARATOR = 4;         // Jarak pemisah
        private const int SCROLLBAR_WIDTH = 12;
        private const float TAP_DISTANCE_THRESHOLD = 20f;
        private const int WRAP_BATCH_SIZE = 50;

        private readonly bool IsAndroid = Constants.TargetPlatform == GamePlatform.Android;

        // ═══════════════════════════════════════════════════════
        // Fields - Data
        // ═══════════════════════════════════════════════════════

        private readonly List<object> _rawSubjects;
        private readonly List<SubjectWrapper> _wrappedSubjects = new();
        private List<SubjectWrapper> _filteredSubjects = new();
        private readonly Action<object> _onSelectSubject;

        private int _wrapIndex = 0;
        private bool _isFullyLoaded = false;

        // ═══════════════════════════════════════════════════════
        // Fields - UI Components
        // ═══════════════════════════════════════════════════════

        private readonly List<CategoryButton> _categoryButtons = new();

        private ClickableTextureComponent _clearButton = null!;
        private ClickableTextureComponent _closeButton = null!;
        private ClickableTextureComponent _searchIcon = null!;

        // ═══════════════════════════════════════════════════════
        // Fields - Search Box (CJBItemSpawner style)
        // ═══════════════════════════════════════════════════════

        private TextBox _searchBox = null!;
        private Rectangle _searchBoxBounds;
        private string _lastSearchText = "";
        private bool _isSearchBoxSelectedExplicitly = false;

        // Cached reflection methods untuk Android keyboard
        private MethodInfo? _showAndroidKeyboard;
        private MethodInfo? _hideAndroidKeyboard;

        // ═══════════════════════════════════════════════════════
        // Fields - Scroll State
        // ═══════════════════════════════════════════════════════

        private float _scrollOffset = 0f;
        private float _maxScrollOffset = 0f;
        private bool _isDragging = false;
        private Vector2 _lastDragPos;
        private Vector2 _dragStartPos;
        private float _totalDragDistance = 0f;

        // ═══════════════════════════════════════════════════════
        // Fields - State
        // ═══════════════════════════════════════════════════════

        private string _currentCategory = "All";
        private bool _needsFilterUpdate = true;

        // ═══════════════════════════════════════════════════════
        // Fields - Layout
        // ═══════════════════════════════════════════════════════

        private Rectangle _categoryArea;
        private Rectangle _resultsArea;
        private Rectangle _scrollbarArea;
        private int _maxVisibleItems;

        // ═══════════════════════════════════════════════════════
        // Constructor
        // ═══════════════════════════════════════════════════════

        public MobileSearchMenu(IEnumerable<object> subjects, Action<object> onSelectSubject)
            : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
        {
            _onSelectSubject = onSelectSubject;
            _rawSubjects = subjects.ToList();

            Logger.Debug($"MobileSearchMenu created with {_rawSubjects.Count} raw subjects");

            // Cache reflection methods untuk Android
            if (IsAndroid)
            {
                CacheAndroidKeyboardMethods();
            }

            CalculateLayout();
            CreateComponents();
        }

        // ═══════════════════════════════════════════════════════
        // Android Keyboard Reflection
        // ═══════════════════════════════════════════════════════

        private void CacheAndroidKeyboardMethods()
        {
            try
            {
                var textBoxType = typeof(TextBox);
                _showAndroidKeyboard = textBoxType.GetMethod(
                    "ShowAndroidKeyboard",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                _hideAndroidKeyboard = textBoxType.GetMethod(
                    "HideAndroidKeyboard",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                // Alternatif nama method
                if (_hideAndroidKeyboard == null)
                {
                    _hideAndroidKeyboard = textBoxType.GetMethod(
                        "HideStatusBar",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                    );
                }

                Logger.Debug($"Android keyboard methods cached: Show={_showAndroidKeyboard != null}, Hide={_hideAndroidKeyboard != null}");
            }
            catch (Exception ex)
            {
                Logger.Debug($"Failed to cache Android keyboard methods: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════════════
        // Layout
        // ═══════════════════════════════════════════════════════

        private void CalculateLayout()
        {
            int margin = Math.Min(24, Game1.uiViewport.Width / 25);

            xPositionOnScreen = margin;
            yPositionOnScreen = margin;
            width = Game1.uiViewport.Width - (margin * 2);
            height = Game1.uiViewport.Height - (margin * 2);

            int contentX = xPositionOnScreen + PADDING;
            int contentWidth = width - (PADDING * 2) - SCROLLBAR_WIDTH - 8;
            int currentY = yPositionOnScreen + PADDING + 40;

            // Search box bounds
            _searchBoxBounds = new Rectangle(
                contentX,
                currentY,
                contentWidth - 56,
                SEARCH_BOX_HEIGHT
            );
            currentY += SEARCH_BOX_HEIGHT + PADDING;

            // Category area
            _categoryArea = new Rectangle(contentX, currentY, contentWidth, CATEGORY_HEIGHT);
            currentY += CATEGORY_HEIGHT + PADDING;

            // Results area
            _resultsArea = new Rectangle(
                contentX, currentY,
                contentWidth,
                yPositionOnScreen + height - currentY - PADDING - 24
            );

            // Scrollbar
            _scrollbarArea = new Rectangle(
                _resultsArea.Right + 8,
                _resultsArea.Y,
                SCROLLBAR_WIDTH,
                _resultsArea.Height
            );

            _maxVisibleItems = _resultsArea.Height / ITEM_HEIGHT;
        }

        private void CreateComponents()
        {
            // Search box (CJBItemSpawner style)
            _searchBox = new TextBox(
                textBoxTexture: Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                caretTexture: null,
                font: Game1.smallFont,
                textColor: Game1.textColor
            )
            {
                X = _searchBoxBounds.X + 40,
                Y = _searchBoxBounds.Y + 8,
                Width = _searchBoxBounds.Width - 50,
                Height = _searchBoxBounds.Height,
                Text = ""
            };

            // Search icon
            _searchIcon = new ClickableTextureComponent(
                bounds: new Rectangle(
                    _searchBoxBounds.X + 8,
                    _searchBoxBounds.Y + (_searchBoxBounds.Height - 26) / 2,
                    26, 26
                ),
                texture: Game1.mouseCursors,
                sourceRect: new Rectangle(80, 0, 13, 13),
                scale: 2f
            );

            // Clear button
            _clearButton = new ClickableTextureComponent(
                bounds: new Rectangle(
                    _searchBoxBounds.Right + 8,
                    _searchBoxBounds.Y + (_searchBoxBounds.Height - 44) / 2,
                    44, 44
                ),
                texture: Game1.mouseCursors,
                sourceRect: new Rectangle(322, 498, 12, 12),
                scale: 3.5f
            );

            // Close button
            _closeButton = new ClickableTextureComponent(
                bounds: new Rectangle(
                    xPositionOnScreen + width - 56 - 8,
                    yPositionOnScreen + 8,
                    56, 56
                ),
                texture: Game1.mouseCursors,
                sourceRect: new Rectangle(337, 494, 12, 12),
                scale: 56f / 12f
            );

            CreateDefaultCategoryButtons();
        }

        private void CreateDefaultCategoryButtons()
        {
            _categoryButtons.Clear();

            var categories = new List<string> { "All", "Items", "NPCs", "Buildings" };

            int buttonWidth = (_categoryArea.Width - ((categories.Count - 1) * 6)) / categories.Count;
            int buttonX = _categoryArea.X;

            foreach (var category in categories)
            {
                _categoryButtons.Add(new CategoryButton
                {
                    Bounds = new Rectangle(buttonX, _categoryArea.Y, buttonWidth, CATEGORY_HEIGHT),
                    Category = category,
                    IsSelected = category == "All"
                });
                buttonX += buttonWidth + 6;
            }
        }

        private void UpdateCategoryButtons()
        {
            var existingCategories = _wrappedSubjects
                .Select(s => s.GetCategory())
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            var categories = new List<string> { "All" };
            categories.AddRange(existingCategories);

            if (categories.Count > 6)
            {
                categories = categories.Take(6).ToList();
            }

            _categoryButtons.Clear();

            int buttonWidth = (_categoryArea.Width - ((categories.Count - 1) * 6)) / categories.Count;
            int buttonX = _categoryArea.X;

            foreach (var category in categories)
            {
                _categoryButtons.Add(new CategoryButton
                {
                    Bounds = new Rectangle(buttonX, _categoryArea.Y, buttonWidth, CATEGORY_HEIGHT),
                    Category = category,
                    IsSelected = category == _currentCategory
                });
                buttonX += buttonWidth + 6;
            }
        }

        private void CalculateMaxScroll()
        {
            int totalContentHeight = _filteredSubjects.Count * ITEM_HEIGHT;
            int viewHeight = _resultsArea.Height;
            _maxScrollOffset = Math.Max(0, totalContentHeight - viewHeight);
        }

        // ═══════════════════════════════════════════════════════
        // Search Box Selection (CJBItemSpawner style)
        // ═══════════════════════════════════════════════════════

        private void SelectSearchBox(bool explicitly)
        {
            _searchBox.Selected = true;
            _isSearchBoxSelectedExplicitly = explicitly;

            // Di Android, buka keyboard native
            if (IsAndroid && _showAndroidKeyboard != null)
            {
                try
                {
                    _showAndroidKeyboard.Invoke(_searchBox, null);
                }
                catch (Exception ex)
                {
                    Logger.Debug($"Failed to show Android keyboard: {ex.Message}");
                    // Fallback ke showTextEntry
                    Game1.showTextEntry(_searchBox);
                }
            }
        }

        private void DeselectSearchBox()
        {
            Game1.closeTextEntry();

            _searchBox.Selected = false;
            _isSearchBoxSelectedExplicitly = false;

            // Di Android, tutup keyboard
            if (IsAndroid && _hideAndroidKeyboard != null)
            {
                try
                {
                    _hideAndroidKeyboard.Invoke(_searchBox, null);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        private void ClearSearch()
        {
            _searchBox.Text = "";
            _needsFilterUpdate = true;
            Game1.playSound("smallSelect");
        }

        // ═══════════════════════════════════════════════════════
        // Lazy Loading
        // ═══════════════════════════════════════════════════════

        private void ProcessLazyLoading()
        {
            if (_isFullyLoaded)
                return;

            int endIndex = Math.Min(_wrapIndex + WRAP_BATCH_SIZE, _rawSubjects.Count);

            for (int i = _wrapIndex; i < endIndex; i++)
            {
                var wrapper = SubjectWrapper.Create(_rawSubjects[i]);
                if (wrapper != null)
                {
                    _wrappedSubjects.Add(wrapper);
                }
            }

            _wrapIndex = endIndex;

            if (_wrapIndex >= _rawSubjects.Count)
            {
                _isFullyLoaded = true;
                _wrappedSubjects.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
                UpdateCategoryButtons();
                Logger.Debug($"Lazy loading complete: {_wrappedSubjects.Count} subjects wrapped");
            }

            _needsFilterUpdate = true;
        }

        // ═══════════════════════════════════════════════════════
        // Filtering
        // ═══════════════════════════════════════════════════════

        private void ApplyFilter()
        {
            string query = (_searchBox.Text ?? "").Trim().ToLowerInvariant();

            _filteredSubjects = _wrappedSubjects
                .Where(s =>
                {
                    if (_currentCategory != "All" && s.GetCategory() != _currentCategory)
                        return false;

                    if (!string.IsNullOrEmpty(query))
                    {
                        return s.Name.ToLowerInvariant().Contains(query) ||
                               s.Description.ToLowerInvariant().Contains(query);
                    }

                    return true;
                })
                .ToList();

            if (!string.IsNullOrEmpty(query))
            {
                _filteredSubjects = _filteredSubjects
                    .OrderBy(s => !s.Name.ToLowerInvariant().StartsWith(query))
                    .ThenBy(s => s.Name)
                    .ToList();
            }

            _scrollOffset = 0;
            CalculateMaxScroll();
            _needsFilterUpdate = false;
        }

        // ═══════════════════════════════════════════════════════
        // Input Handling
        // ═══════════════════════════════════════════════════════

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            // Close button
            if (_closeButton.containsPoint(x, y))
            {
                exitThisMenu();
                return;
            }

            // Search box - select dan buka keyboard
            if (_searchBoxBounds.Contains(x, y))
            {
                if (!_searchBox.Selected || !_isSearchBoxSelectedExplicitly)
                {
                    if (playSound) Game1.playSound("smallSelect");
                    SelectSearchBox(explicitly: true);
                }
                return;
            }

            // Deselect search box jika klik di luar
            if (_searchBox.Selected)
            {
                DeselectSearchBox();
            }

            // Clear button
            if (!string.IsNullOrEmpty(_searchBox.Text) && _clearButton.bounds.Contains(x, y))
            {
                ClearSearch();
                return;
            }

            // Category buttons
            foreach (var btn in _categoryButtons)
            {
                if (btn.Bounds.Contains(x, y))
                {
                    if (_currentCategory != btn.Category)
                    {
                        _currentCategory = btn.Category;
                        foreach (var b in _categoryButtons)
                            b.IsSelected = b.Category == _currentCategory;
                        _needsFilterUpdate = true;
                        if (playSound) Game1.playSound("smallSelect");
                    }
                    return;
                }
            }

            // Start drag untuk scroll
            if (_resultsArea.Contains(x, y))
            {
                _isDragging = true;
                _lastDragPos = new Vector2(x, y);
                _dragStartPos = new Vector2(x, y);
                _totalDragDistance = 0f;
            }
        }

        public override void leftClickHeld(int x, int y)
        {
            if (!_isDragging)
                return;

            Vector2 currentPos = new Vector2(x, y);
            float deltaY = _lastDragPos.Y - y;
            _totalDragDistance += Vector2.Distance(_lastDragPos, currentPos);
            _scrollOffset = MathHelper.Clamp(_scrollOffset + deltaY, 0, _maxScrollOffset);
            _lastDragPos = currentPos;
        }

        public override void releaseLeftClick(int x, int y)
        {
            if (_isDragging && _totalDragDistance < TAP_DISTANCE_THRESHOLD)
            {
                TrySelectItemAt((int)_dragStartPos.X, (int)_dragStartPos.Y);
            }

            _isDragging = false;
            _totalDragDistance = 0f;
        }

        private void TrySelectItemAt(int x, int y)
        {
            if (!_resultsArea.Contains(x, y))
                return;

            int relativeY = y - _resultsArea.Y + (int)_scrollOffset;
            int itemIndex = relativeY / ITEM_HEIGHT;

            if (itemIndex >= 0 && itemIndex < _filteredSubjects.Count)
            {
                var subject = _filteredSubjects[itemIndex];
                SelectResult(subject);
                Game1.playSound("select");
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            float scrollAmount = ITEM_HEIGHT * 2;
            _scrollOffset = MathHelper.Clamp(
                _scrollOffset + (direction > 0 ? -scrollAmount : scrollAmount),
                0,
                _maxScrollOffset
            );
        }

        public override void receiveKeyPress(Keys key)
        {
            // Clear textbox dengan Escape
            if (key == Keys.Escape)
            {
                if (_searchBox.Selected && !string.IsNullOrEmpty(_searchBox.Text))
                {
                    _searchBox.Text = "";
                    DeselectSearchBox();
                }
                else if (_searchBox.Selected)
                {
                    DeselectSearchBox();
                }
                else
                {
                    exitThisMenu();
                }
                return;
            }
        }

        private void SelectResult(SubjectWrapper subject)
        {
            Logger.Debug($"Selected: {subject.Name}");
            exitThisMenu(false);
            _onSelectSubject?.Invoke(subject.RawSubject);
        }

        // ═══════════════════════════════════════════════════════
        // Update
        // ═══════════════════════════════════════════════════════

        public override void update(GameTime time)
        {
            base.update(time);

            // Lazy loading
            if (!_isFullyLoaded)
            {
                ProcessLazyLoading();
            }

            // Deteksi perubahan search text (CJBItemSpawner style)
            string currentText = _searchBox.Text ?? "";
            if (_lastSearchText != currentText)
            {
                _lastSearchText = currentText;
                _needsFilterUpdate = true;
            }

            // Deselect jika textbox tidak lagi selected
            if (_isSearchBoxSelectedExplicitly && !_searchBox.Selected)
            {
                DeselectSearchBox();
            }

            // Update filter jika perlu
            if (_needsFilterUpdate)
            {
                ApplyFilter();
            }
        }

        // ═══════════════════════════════════════════════════════
        // Draw
        // ═══════════════════════════════════════════════════════

        public override void draw(SpriteBatch b)
        {
            // Dim background
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

            // Menu background
            drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height,
                Color.White, 1f, true
            );

            // Title
            string title = "Search Encyclopedia";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(
                b, title, Game1.dialogueFont,
                new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen + PADDING),
                Game1.textColor
            );

            // Close button
            _closeButton.draw(b);

            // Search box
            DrawSearchBox(b);

            // Clear button
            if (!string.IsNullOrEmpty(_searchBox.Text))
            {
                _clearButton.draw(b);
            }

            // Category buttons
            DrawCategoryButtons(b);

            // Results area background
            b.Draw(Game1.staminaRect, _resultsArea, Color.Black * 0.25f);

            // Result items dengan separator
            DrawResultItems(b);

            // Scrollbar
            DrawScrollbar(b);

            // Results count
            DrawResultsCount(b);

            // Draw cursor
            drawMouse(b);
        }

        private void DrawSearchBox(SpriteBatch b)
        {
            // Background box
            drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                _searchBoxBounds.X - 4,
                _searchBoxBounds.Y - 4,
                _searchBoxBounds.Width + 8,
                _searchBoxBounds.Height + 8,
                Color.White, 1f, false
            );

            // Inner background
            b.Draw(Game1.staminaRect, _searchBoxBounds, Color.White);

            // Search icon
            _searchIcon.draw(b, Color.Gray, 1f);

            // TextBox
            _searchBox.Draw(b);

            // Placeholder jika kosong dan tidak selected
            if (string.IsNullOrEmpty(_searchBox.Text) && !_searchBox.Selected)
            {
                Utility.drawTextWithShadow(
                    b, "Tap to search...", Game1.smallFont,
                    new Vector2(_searchBox.X, _searchBox.Y + 4),
                    Color.Gray
                );
            }
        }

        private void DrawCategoryButtons(SpriteBatch b)
        {
            foreach (var btn in _categoryButtons)
            {
                Color bgColor = btn.IsSelected ? new Color(100, 150, 100) : new Color(80, 80, 80);
                Color textColor = btn.IsSelected ? Color.White : Color.LightGray;

                drawTextureBox(
                    b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                    btn.Bounds.X, btn.Bounds.Y, btn.Bounds.Width, btn.Bounds.Height,
                    bgColor, 1f, false
                );

                string displayText = TruncateText(btn.Category, Game1.smallFont, btn.Bounds.Width - 12);
                Vector2 textSize = Game1.smallFont.MeasureString(displayText);
                Vector2 textPos = new(
                    btn.Bounds.X + (btn.Bounds.Width - textSize.X) / 2,
                    btn.Bounds.Y + (btn.Bounds.Height - textSize.Y) / 2
                );
                Utility.drawTextWithShadow(b, displayText, Game1.smallFont, textPos, textColor);
            }
        }

        private void DrawResultItems(SpriteBatch b)
        {
            if (_filteredSubjects.Count == 0)
            {
                string message = _isFullyLoaded ? "No results found" : "Loading...";
                Vector2 msgSize = Game1.smallFont.MeasureString(message);
                Utility.drawTextWithShadow(b, message, Game1.smallFont,
                    new Vector2(
                        _resultsArea.X + (_resultsArea.Width - msgSize.X) / 2,
                        _resultsArea.Y + (_resultsArea.Height - msgSize.Y) / 2
                    ),
                    Color.Gray);
                return;
            }

            int firstVisibleIndex = (int)(_scrollOffset / ITEM_HEIGHT);
            int lastVisibleIndex = firstVisibleIndex + _maxVisibleItems + 1;

            for (int i = firstVisibleIndex; i <= lastVisibleIndex && i < _filteredSubjects.Count; i++)
            {
                if (i < 0) continue;

                var subject = _filteredSubjects[i];
                int itemY = _resultsArea.Y + (i * ITEM_HEIGHT) - (int)_scrollOffset;

                if (itemY + ITEM_HEIGHT < _resultsArea.Y || itemY > _resultsArea.Bottom)
                    continue;

                // Item content area (dengan padding untuk separator)
                Rectangle itemBounds = new Rectangle(
                    _resultsArea.X,
                    itemY,
                    _resultsArea.Width,
                    ITEM_HEIGHT - ITEM_SEPARATOR
                );

                if (itemBounds.Bottom > _resultsArea.Y && itemBounds.Y < _resultsArea.Bottom)
                {
                    DrawResultItem(b, subject, itemBounds, i);

                    // Draw separator line
                    int separatorY = itemY + ITEM_HEIGHT - ITEM_SEPARATOR;
                    if (separatorY > _resultsArea.Y && separatorY < _resultsArea.Bottom - 4)
                    {
                        b.Draw(
                            Game1.staminaRect,
                            new Rectangle(_resultsArea.X + 8, separatorY, _resultsArea.Width - 16, 1),
                            Color.White * 0.2f
                        );
                    }
                }
            }
        }

        private void DrawResultItem(SpriteBatch b, SubjectWrapper subject, Rectangle bounds, int index)
        {
            // Background
            Color bgColor = index % 2 == 0 ? Color.White * 0.08f : Color.White * 0.04f;

            Rectangle clippedBounds = Rectangle.Intersect(bounds, _resultsArea);
            if (clippedBounds.Width <= 0 || clippedBounds.Height <= 0)
                return;

            b.Draw(Game1.staminaRect, clippedBounds, bgColor);

            if (bounds.Y < _resultsArea.Y - 20 || bounds.Bottom > _resultsArea.Bottom + 20)
                return;

            // Portrait
            int portraitSize = bounds.Height - 12;
            Vector2 portraitPos = new(bounds.X + 6, bounds.Y + 6);

            if (portraitPos.Y >= _resultsArea.Y - portraitSize && portraitPos.Y < _resultsArea.Bottom)
            {
                // Portrait background
                b.Draw(Game1.staminaRect,
                    new Rectangle((int)portraitPos.X - 2, (int)portraitPos.Y - 2, portraitSize + 4, portraitSize + 4),
                    Color.Black * 0.2f);

                if (!subject.DrawPortrait(b, portraitPos, new Vector2(portraitSize)))
                {
                    b.Draw(Game1.staminaRect,
                        new Rectangle((int)portraitPos.X, (int)portraitPos.Y, portraitSize, portraitSize),
                        Color.Gray * 0.3f);

                    string abbr = GetCategoryAbbreviation(subject.GetCategory());
                    Vector2 abbrSize = Game1.tinyFont.MeasureString(abbr);
                    Utility.drawTextWithShadow(b, abbr, Game1.tinyFont,
                        new Vector2(
                            portraitPos.X + (portraitSize - abbrSize.X) / 2,
                            portraitPos.Y + (portraitSize - abbrSize.Y) / 2
                        ),
                        Color.White);
                }
            }

            // Text area
            int textX = bounds.X + portraitSize + 20;
            int textWidth = bounds.Right - textX - 8;
            int textY = bounds.Y + 10;

            if (textY >= _resultsArea.Y - 30 && textY < _resultsArea.Bottom)
            {
                // Name dengan highlight
                DrawHighlightedName(b, subject.Name, textX, textY, textWidth);

                // Description
                if (!string.IsNullOrEmpty(subject.Description))
                {
                    string desc = TruncateText(subject.Description, Game1.tinyFont, textWidth);
                    Utility.drawTextWithShadow(b, desc, Game1.tinyFont,
                        new Vector2(textX, textY + 28), Color.Gray);
                }

                // Category badge
                string categoryText = $"[{subject.GetCategory()}]";
                Vector2 categorySize = Game1.tinyFont.MeasureString(categoryText);
                Utility.drawTextWithShadow(b, categoryText, Game1.tinyFont,
                    new Vector2(bounds.Right - categorySize.X - 10, textY),
                    Color.LightBlue);
            }
        }

        private void DrawHighlightedName(SpriteBatch b, string name, int x, int y, int maxWidth)
        {
            string displayName = TruncateText(name, Game1.smallFont, maxWidth - 80);
            string searchText = (_searchBox.Text ?? "").Trim();

            if (!string.IsNullOrEmpty(searchText))
            {
                int matchIndex = displayName.ToLowerInvariant().IndexOf(searchText.ToLowerInvariant());
                if (matchIndex >= 0)
                {
                    string before = displayName[..matchIndex];
                    string match = displayName.Substring(matchIndex, Math.Min(searchText.Length, displayName.Length - matchIndex));
                    string after = displayName[(matchIndex + match.Length)..];

                    float currentX = x;

                    if (!string.IsNullOrEmpty(before))
                    {
                        Utility.drawTextWithShadow(b, before, Game1.smallFont,
                            new Vector2(currentX, y), Color.White);
                        currentX += Game1.smallFont.MeasureString(before).X;
                    }

                    if (!string.IsNullOrEmpty(match))
                    {
                        Vector2 matchSize = Game1.smallFont.MeasureString(match);
                        b.Draw(Game1.staminaRect,
                            new Rectangle((int)currentX - 1, y - 1, (int)matchSize.X + 2, (int)matchSize.Y + 2),
                            Color.Yellow * 0.3f);

                        Utility.drawTextWithShadow(b, match, Game1.smallFont,
                            new Vector2(currentX, y), Color.Yellow);
                        currentX += matchSize.X;
                    }

                    if (!string.IsNullOrEmpty(after))
                    {
                        Utility.drawTextWithShadow(b, after, Game1.smallFont,
                            new Vector2(currentX, y), Color.White);
                    }
                    return;
                }
            }

            Utility.drawTextWithShadow(b, displayName, Game1.smallFont,
                new Vector2(x, y), Color.White);
        }

        private void DrawScrollbar(SpriteBatch b)
        {
            if (_maxScrollOffset <= 0)
                return;

            // Track
            b.Draw(Game1.staminaRect, _scrollbarArea, Color.Black * 0.3f);

            // Thumb
            float viewRatio = (float)_resultsArea.Height / (_filteredSubjects.Count * ITEM_HEIGHT);
            int thumbHeight = Math.Max(30, (int)(_scrollbarArea.Height * viewRatio));

            float scrollProgress = _maxScrollOffset > 0 ? _scrollOffset / _maxScrollOffset : 0;
            int thumbY = _scrollbarArea.Y + (int)((_scrollbarArea.Height - thumbHeight) * scrollProgress);

            b.Draw(Game1.staminaRect,
                new Rectangle(_scrollbarArea.X, thumbY, _scrollbarArea.Width, thumbHeight),
                Color.White * 0.7f);
        }

        private void DrawResultsCount(SpriteBatch b)
        {
            string countText;

            if (!_isFullyLoaded)
            {
                int percent = _rawSubjects.Count > 0 ? (_wrapIndex * 100) / _rawSubjects.Count : 0;
                countText = $"Loading... {percent}%";
            }
            else
            {
                countText = _filteredSubjects.Count == _wrappedSubjects.Count
                    ? $"{_wrappedSubjects.Count} items"
                    : $"{_filteredSubjects.Count} of {_wrappedSubjects.Count}";

                string searchText = (_searchBox.Text ?? "").Trim();
                if (!string.IsNullOrEmpty(searchText))
                {
                    countText = $"'{TruncateText(searchText, Game1.smallFont, 100)}': {countText}";
                }
            }

            Utility.drawTextWithShadow(b, countText, Game1.smallFont,
                new Vector2(_resultsArea.X, _resultsArea.Bottom + 4), Color.Gray);
        }

        // ═══════════════════════════════════════════════════════
        // Helpers
        // ═══════════════════════════════════════════════════════

        private static string TruncateText(string text, SpriteFont font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (font.MeasureString(text).X <= maxWidth) return text;

            string truncated = text;
            while (truncated.Length > 0 && font.MeasureString(truncated + "...").X > maxWidth)
            {
                truncated = truncated[..^1];
            }
            return truncated + "...";
        }

        private static string GetCategoryAbbreviation(string category)
        {
            return category switch
            {
                "NPCs" => "NPC",
                "Items" => "ITM",
                "Buildings" => "BLD",
                "Animals" => "ANM",
                "Crops" => "CRP",
                "Terrain" => "TRN",
                "Monsters" => "MON",
                _ => "???"
            };
        }

        // ═══════════════════════════════════════════════════════
        // Helper Classes
        // ═══════════════════════════════════════════════════════

        private class CategoryButton
        {
            public Rectangle Bounds { get; set; }
            public string Category { get; set; } = "";
            public bool IsSelected { get; set; }
        }
    }
}