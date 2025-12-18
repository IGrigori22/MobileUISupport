// File: Integrations/LookupAnything/MobileSearchMenu.cs

using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MobileUISupport.Framework;
using MobileUISupport.Integrations.LookupAnything;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace MobileUISupport.UI;

/// <summary>
/// Mobile-friendly search menu for LookupAnything integration.
/// Provides touch-optimized browsing and filtering of game subjects.
/// </summary>
public class MobileSearchMenu : IClickableMenu
{
    #region Constants

    private const int Padding = 16;
    private const int SearchBoxHeight = 52;
    private const int CategoryHeight = 44;
    private const int ItemHeight = 80;
    private const int ItemSeparator = 4;
    private const int ScrollbarWidth = 12;
    private const float TapDistanceThreshold = 20f;
    private const int WrapBatchSize = 50;
    private const float TitleScale = 0.75f;
    private const float DescriptionScale = 0.60f;
    private const float CountScale = 0.8f;

    #endregion

    #region Fields - Platform

    private readonly bool _isAndroid = Constants.TargetPlatform == GamePlatform.Android;
    private MethodInfo? _showAndroidKeyboard;
    private MethodInfo? _hideAndroidKeyboard;

    #endregion

    #region Fields - Data

    private readonly List<object> _rawSubjects;
    private readonly List<SubjectWrapper> _wrappedSubjects = new();
    private readonly Action<object> _onSelectSubject;

    private List<SubjectWrapper> _filteredSubjects = new();
    private int _wrapIndex;
    private bool _isFullyLoaded;

    #endregion

    #region Fields - UI Components

    private readonly List<CategoryButton> _categoryButtons = new();
    private TextBox _searchBox = null!;
    private ClickableTextureComponent _clearButton = null!;
    private ClickableTextureComponent _closeButton = null!;
    private ClickableTextureComponent _searchIcon = null!;

    #endregion

    #region Fields - Layout

    private Rectangle _searchBoxBounds;
    private Rectangle _categoryArea;
    private Rectangle _resultsArea;
    private Rectangle _scrollbarArea;
    private int _maxVisibleItems;

    #endregion

    #region Fields - Search State

    private string _lastSearchText = "";
    private string _currentCategory = "All";
    private bool _isSearchBoxSelectedExplicitly;
    private bool _needsFilterUpdate = true;

    #endregion

    #region Fields - Scroll State

    private float _scrollOffset;
    private float _maxScrollOffset;
    private bool _isDragging;
    private Vector2 _lastDragPos;
    private Vector2 _dragStartPos;
    private float _totalDragDistance;

    #endregion

    #region Constructor

    /// <summary>
    /// Creates a new mobile search menu.
    /// </summary>
    /// <param name="subjects">The subjects to display.</param>
    /// <param name="onSelectSubject">Callback when a subject is selected.</param>
    public MobileSearchMenu(IEnumerable<object> subjects, Action<object> onSelectSubject)
        : base(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height)
    {
        _onSelectSubject = onSelectSubject;
        _rawSubjects = subjects.ToList();

        Logger.Debug($"MobileSearchMenu created with {_rawSubjects.Count} raw subjects");

        if (_isAndroid)
            CacheAndroidKeyboardMethods();

        CalculateLayout();
        CreateComponents();
    }

    #endregion

    #region Initialization

    private void CacheAndroidKeyboardMethods()
    {
        try
        {
            var textBoxType = typeof(TextBox);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            _showAndroidKeyboard = textBoxType.GetMethod("ShowAndroidKeyboard", flags);
            _hideAndroidKeyboard = textBoxType.GetMethod("HideAndroidKeyboard", flags)
                                   ?? textBoxType.GetMethod("HideStatusBar", flags);

            Logger.Debug($"Android keyboard methods cached: Show={_showAndroidKeyboard != null}, Hide={_hideAndroidKeyboard != null}");
        }
        catch (Exception ex)
        {
            Logger.Debug($"Failed to cache Android keyboard methods: {ex.Message}");
        }
    }

    private void CalculateLayout()
    {
        int margin = Math.Min(24, Game1.uiViewport.Width / 25);

        xPositionOnScreen = margin;
        yPositionOnScreen = margin;
        width = Game1.uiViewport.Width - (margin * 2);
        height = Game1.uiViewport.Height - (margin * 2);

        int contentX = xPositionOnScreen + Padding;
        int contentWidth = width - (Padding * 2) - ScrollbarWidth - 8;
        int currentY = yPositionOnScreen + Padding + 40;

        // Search box
        _searchBoxBounds = new Rectangle(contentX + 20, currentY, contentWidth - 56, SearchBoxHeight);
        currentY += SearchBoxHeight + Padding;

        // Category area
        _categoryArea = new Rectangle(contentX, currentY, contentWidth, CategoryHeight);
        currentY += CategoryHeight + Padding;

        // Results area
        _resultsArea = new Rectangle(
            contentX + 14,
            currentY,
            contentWidth - 20,
            yPositionOnScreen + height - currentY - Padding - 24
        );

        // Scrollbar
        _scrollbarArea = new Rectangle(
            _resultsArea.Right + 8,
            _resultsArea.Y,
            ScrollbarWidth,
            _resultsArea.Height
        );

        _maxVisibleItems = _resultsArea.Height / ItemHeight;
    }

    private void CreateComponents()
    {
        CreateSearchBox();
        CreateButtons();
        CreateDefaultCategoryButtons();
    }

    private void CreateSearchBox()
    {
        _searchBox = new TextBox(
            textBoxTexture: Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
            caretTexture: null,
            font: Game1.smallFont,
            textColor: Game1.textColor
        )
        {
            X = _searchBoxBounds.X + 40,
            Y = _searchBoxBounds.Y + 4,
            Width = _searchBoxBounds.Width - 50,
            Height = _searchBoxBounds.Height,
            Text = ""
        };

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
    }

    private void CreateButtons()
    {
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
    }

    private void CreateDefaultCategoryButtons()
    {
        var categories = new List<string> { "All", "Items", "NPCs", "Buildings" };
        RebuildCategoryButtons(categories);
    }

    private void UpdateCategoryButtons()
    {
        var existingCategories = _wrappedSubjects
            .Select(s => s.GetCategory())
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var categories = new List<string> { "All" };
        categories.AddRange(existingCategories.Take(5)); // Max 6 total buttons

        RebuildCategoryButtons(categories);
    }

    private void RebuildCategoryButtons(List<string> categories)
    {
        _categoryButtons.Clear();

        int buttonWidth = (_categoryArea.Width - ((categories.Count - 1) * 6)) / categories.Count;
        int buttonX = _categoryArea.X;

        foreach (var category in categories)
        {
            _categoryButtons.Add(new CategoryButton
            {
                Bounds = new Rectangle(buttonX, _categoryArea.Y, buttonWidth, CategoryHeight),
                Category = category,
                IsSelected = category == _currentCategory
            });
            buttonX += buttonWidth + 6;
        }
    }

    #endregion

    #region Search Box Management

    private void SelectSearchBox(bool explicitly)
    {
        _searchBox.Selected = true;
        _isSearchBoxSelectedExplicitly = explicitly;

        if (_isAndroid && _showAndroidKeyboard != null)
        {
            try
            {
                _showAndroidKeyboard.Invoke(_searchBox, null);
            }
            catch (Exception ex)
            {
                Logger.Debug($"Failed to show Android keyboard: {ex.Message}");
                Game1.showTextEntry(_searchBox);
            }
        }
    }

    private void DeselectSearchBox()
    {
        Game1.closeTextEntry();
        _searchBox.Selected = false;
        _isSearchBoxSelectedExplicitly = false;

        if (_isAndroid && _hideAndroidKeyboard != null)
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

    #endregion

    #region Lazy Loading & Filtering

    private void ProcessLazyLoading()
    {
        if (_isFullyLoaded)
            return;

        int endIndex = Math.Min(_wrapIndex + WrapBatchSize, _rawSubjects.Count);

        for (int i = _wrapIndex; i < endIndex; i++)
        {
            var wrapper = SubjectWrapper.Create(_rawSubjects[i]);
            if (wrapper != null)
                _wrappedSubjects.Add(wrapper);
        }

        _wrapIndex = endIndex;

        if (_wrapIndex >= _rawSubjects.Count)
        {
            _isFullyLoaded = true;
            _wrappedSubjects.Sort((a, b) =>
                string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            UpdateCategoryButtons();
            Logger.Debug($"Lazy loading complete: {_wrappedSubjects.Count} subjects wrapped");
        }

        _needsFilterUpdate = true;
    }

    private void ApplyFilter()
    {
        string query = (_searchBox.Text ?? "").Trim().ToLowerInvariant();

        _filteredSubjects = _wrappedSubjects
            .Where(s => MatchesFilter(s, query))
            .ToList();

        // Sort by relevance if searching
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

    private bool MatchesFilter(SubjectWrapper subject, string query)
    {
        // Category filter
        if (_currentCategory != "All" && subject.GetCategory() != _currentCategory)
            return false;

        // Text filter
        if (string.IsNullOrEmpty(query))
            return true;

        return subject.Name.ToLowerInvariant().Contains(query)
               || subject.Description.ToLowerInvariant().Contains(query);
    }

    private void CalculateMaxScroll()
    {
        int totalContentHeight = _filteredSubjects.Count * ItemHeight;
        _maxScrollOffset = Math.Max(0, totalContentHeight - _resultsArea.Height);
    }

    #endregion

    #region Input Handling

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (_closeButton.containsPoint(x, y))
        {
            exitThisMenu();
            return;
        }

        if (_searchBoxBounds.Contains(x, y))
        {
            if (!_searchBox.Selected || !_isSearchBoxSelectedExplicitly)
            {
                if (playSound) Game1.playSound("smallSelect");
                SelectSearchBox(explicitly: true);
            }
            return;
        }

        if (_searchBox.Selected)
            DeselectSearchBox();

        if (!string.IsNullOrEmpty(_searchBox.Text) && _clearButton.bounds.Contains(x, y))
        {
            ClearSearch();
            return;
        }

        if (TryHandleCategoryClick(x, y, playSound))
            return;

        if (_resultsArea.Contains(x, y))
            StartDrag(x, y);
    }

    private bool TryHandleCategoryClick(int x, int y, bool playSound)
    {
        foreach (var btn in _categoryButtons)
        {
            if (!btn.Bounds.Contains(x, y))
                continue;

            if (_currentCategory != btn.Category)
            {
                _currentCategory = btn.Category;
                foreach (var b in _categoryButtons)
                    b.IsSelected = b.Category == _currentCategory;
                _needsFilterUpdate = true;
                if (playSound) Game1.playSound("smallSelect");
            }
            return true;
        }
        return false;
    }

    private void StartDrag(int x, int y)
    {
        _isDragging = true;
        _lastDragPos = new Vector2(x, y);
        _dragStartPos = new Vector2(x, y);
        _totalDragDistance = 0f;
    }

    public override void leftClickHeld(int x, int y)
    {
        if (!_isDragging)
            return;

        Vector2 currentPos = new(x, y);
        float deltaY = _lastDragPos.Y - y;

        _totalDragDistance += Vector2.Distance(_lastDragPos, currentPos);
        _scrollOffset = MathHelper.Clamp(_scrollOffset + deltaY, 0, _maxScrollOffset);
        _lastDragPos = currentPos;
    }

    public override void releaseLeftClick(int x, int y)
    {
        if (_isDragging && _totalDragDistance < TapDistanceThreshold)
            TrySelectItemAt((int)_dragStartPos.X, (int)_dragStartPos.Y);

        _isDragging = false;
        _totalDragDistance = 0f;
    }

    private void TrySelectItemAt(int x, int y)
    {
        if (!_resultsArea.Contains(x, y))
            return;

        int relativeY = y - _resultsArea.Y + (int)_scrollOffset;
        int itemIndex = relativeY / ItemHeight;

        if (itemIndex >= 0 && itemIndex < _filteredSubjects.Count)
        {
            SelectResult(_filteredSubjects[itemIndex]);
            Game1.playSound("select");
        }
    }

    public override void receiveScrollWheelAction(int direction)
    {
        float scrollAmount = ItemHeight * 2;
        _scrollOffset = MathHelper.Clamp(
            _scrollOffset + (direction > 0 ? -scrollAmount : scrollAmount),
            0,
            _maxScrollOffset
        );
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key != Keys.Escape)
            return;

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
    }

    private void SelectResult(SubjectWrapper subject)
    {
        Logger.Debug($"Selected: {subject.Name}");
        exitThisMenu(false);
        _onSelectSubject?.Invoke(subject.RawSubject);
    }

    #endregion

    #region Update

    public override void update(GameTime time)
    {
        base.update(time);

        if (!_isFullyLoaded)
            ProcessLazyLoading();

        // Detect search text changes
        string currentText = _searchBox.Text ?? "";
        if (_lastSearchText != currentText)
        {
            _lastSearchText = currentText;
            _needsFilterUpdate = true;
        }

        if (_isSearchBoxSelectedExplicitly && !_searchBox.Selected)
            DeselectSearchBox();

        if (_needsFilterUpdate)
            ApplyFilter();
    }

    #endregion

    #region Drawing

    public override void draw(SpriteBatch b)
    {
        DrawBackground(b);
        DrawTitle(b);

        _closeButton.draw(b);

        DrawSearchBox(b);
        DrawCategoryButtons(b);
        DrawResultsArea(b);
        DrawScrollbar(b);
        DrawResultsCount(b);

        drawMouse(b);
    }

    private void DrawBackground(SpriteBatch b)
    {
        // Dim background
        b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

        // Menu background
        drawTextureBox(
            b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            xPositionOnScreen, yPositionOnScreen, width, height,
            Color.White, 1f, true
        );
    }

    private void DrawTitle(SpriteBatch b)
    {
        const string title = "Search Encyclopedia";

        Vector2 titleSize = Game1.dialogueFont.MeasureString(title) * TitleScale;
        Vector2 titlePos = new(
            xPositionOnScreen + (width - titleSize.X) / 2,
            yPositionOnScreen + Padding
        );

        // Shadow
        b.DrawString(
            Game1.dialogueFont, title,
            titlePos + new Vector2(2, 2),
            Color.Black * 0.5f,
            0f, Vector2.Zero, TitleScale, SpriteEffects.None, 1f
        );

        // Text
        b.DrawString(
            Game1.dialogueFont, title,
            titlePos,
            Game1.textColor,
            0f, Vector2.Zero, TitleScale, SpriteEffects.None, 1f
        );
    }

    private void DrawSearchBox(SpriteBatch b)
    {
        // Background box
        drawTextureBox(
            b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
            _searchBoxBounds.X - 4, _searchBoxBounds.Y - 4,
            _searchBoxBounds.Width + 8, _searchBoxBounds.Height + 8,
            Color.White, 1f, false
        );

        // Inner background
        b.Draw(Game1.staminaRect, _searchBoxBounds, Color.White);

        // Search icon
        _searchIcon.draw(b, Color.Gray, 1f);

        // TextBox
        _searchBox.Draw(b);

        // Placeholder
        if (string.IsNullOrEmpty(_searchBox.Text) && !_searchBox.Selected)
        {
            Utility.drawTextWithShadow(
                b, "Tap to search...", Game1.smallFont,
                new Vector2(_searchBox.X + 15, _searchBox.Y + 4),
                Color.Gray
            );
        }

        // Clear button
        if (!string.IsNullOrEmpty(_searchBox.Text))
            _clearButton.draw(b);
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

    private void DrawResultsArea(SpriteBatch b)
    {
        // Background
        b.Draw(Game1.staminaRect, _resultsArea, Color.Black * 0.25f);

        // Items
        DrawResultItems(b);
    }

    private void DrawResultItems(SpriteBatch b)
    {
        if (_filteredSubjects.Count == 0)
        {
            DrawEmptyMessage(b);
            return;
        }

        int firstVisibleIndex = (int)(_scrollOffset / ItemHeight);
        int lastVisibleIndex = firstVisibleIndex + _maxVisibleItems + 1;

        for (int i = firstVisibleIndex; i <= lastVisibleIndex && i < _filteredSubjects.Count; i++)
        {
            if (i < 0) continue;

            var subject = _filteredSubjects[i];
            int itemY = _resultsArea.Y + (i * ItemHeight) - (int)_scrollOffset;

            if (itemY + ItemHeight < _resultsArea.Y || itemY > _resultsArea.Bottom)
                continue;

            Rectangle itemBounds = new(
                _resultsArea.X,
                itemY,
                _resultsArea.Width,
                ItemHeight - ItemSeparator
            );

            if (itemBounds.Bottom > _resultsArea.Y && itemBounds.Y < _resultsArea.Bottom)
            {
                DrawResultItem(b, subject, itemBounds, i);
                DrawItemSeparator(b, itemY);
            }
        }
    }

    private void DrawEmptyMessage(SpriteBatch b)
    {
        string message = _isFullyLoaded ? "No results found" : "Loading...";
        Vector2 msgSize = Game1.smallFont.MeasureString(message);

        Utility.drawTextWithShadow(
            b, message, Game1.smallFont,
            new Vector2(
                _resultsArea.X + (_resultsArea.Width - msgSize.X) / 2,
                _resultsArea.Y + (_resultsArea.Height - msgSize.Y) / 2
            ),
            Color.Gray
        );
    }

    private void DrawItemSeparator(SpriteBatch b, int itemY)
    {
        int separatorY = itemY + ItemHeight - ItemSeparator;
        if (separatorY > _resultsArea.Y && separatorY < _resultsArea.Bottom - 4)
        {
            b.Draw(
                Game1.staminaRect,
                new Rectangle(_resultsArea.X + 8, separatorY, _resultsArea.Width - 16, 1),
                Color.White * 0.2f
            );
        }
    }

    private void DrawResultItem(SpriteBatch b, SubjectWrapper subject, Rectangle bounds, int index)
    {
        Rectangle clippedBounds = Rectangle.Intersect(bounds, _resultsArea);
        if (clippedBounds.Width <= 0 || clippedBounds.Height <= 0)
            return;

        // Background
        Color bgColor = index % 2 == 0 ? Color.White * 0.08f : Color.White * 0.04f;
        b.Draw(Game1.staminaRect, clippedBounds, bgColor);

        if (bounds.Y < _resultsArea.Y - 20 || bounds.Bottom > _resultsArea.Bottom + 20)
            return;

        // Portrait
        int portraitSize = bounds.Height - 12;
        Vector2 portraitPos = new(bounds.X + 6, bounds.Y + 6);
        DrawItemPortrait(b, subject, portraitPos, portraitSize);

        // Text content
        int textX = bounds.X + portraitSize + 20;
        int textY = bounds.Y + 8;
        DrawItemText(b, subject, bounds, textX, textY);
    }

    private void DrawItemPortrait(SpriteBatch b, SubjectWrapper subject, Vector2 pos, int size)
    {
        if (pos.Y < _resultsArea.Y - size || pos.Y >= _resultsArea.Bottom)
            return;

        // Background
        b.Draw(Game1.staminaRect,
            new Rectangle((int)pos.X - 2, (int)pos.Y - 2, size + 4, size + 4),
            Color.Black * 0.2f);

        // Portrait or placeholder
        if (!subject.DrawPortrait(b, pos, new Vector2(size)))
        {
            b.Draw(Game1.staminaRect,
                new Rectangle((int)pos.X, (int)pos.Y, size, size),
                Color.Gray * 0.3f);

            string abbr = GetCategoryAbbreviation(subject.GetCategory());
            Vector2 abbrSize = Game1.smallFont.MeasureString(abbr);

            Utility.drawTextWithShadow(
                b, abbr, Game1.tinyFont,
                new Vector2(
                    pos.X + (size - abbrSize.X) / 2,
                    pos.Y + (size - abbrSize.Y) / 2
                ),
                Color.White
            );
        }
    }

    private void DrawItemText(SpriteBatch b, SubjectWrapper subject, Rectangle bounds, int textX, int textY)
    {
        const int contentPadding = 8;

        // Calculate category badge dimensions first
        string categoryText = subject.GetCategory();
        Vector2 categorySize = Game1.tinyFont.MeasureString(categoryText);
        int badgePadding = 6;
        int badgeWidth = (int)(categorySize.X + badgePadding * 2);
        int badgeHeight = (int)(categorySize.Y + 6);
        int badgeX = bounds.Right - badgeWidth - contentPadding;

        // Name
        int nameMaxWidth = badgeX - textX - 12;
        if (textY >= _resultsArea.Y - 30 && textY < _resultsArea.Bottom)
        {
            string displayName = TruncateText(subject.Name, Game1.smallFont, nameMaxWidth);
            DrawHighlightedName(b, displayName, textX, textY, nameMaxWidth);

            // Description
            DrawItemDescription(b, subject, bounds, textX, textY, displayName);

            // Category badge
            DrawCategoryBadge(b, categoryText, badgeX, textY, badgeWidth, badgeHeight, categorySize, badgePadding);
        }
    }

    private void DrawItemDescription(SpriteBatch b, SubjectWrapper subject, Rectangle bounds,
        int textX, int textY, string displayName)
    {
        if (string.IsNullOrEmpty(subject.Description))
            return;

        float nameHeight = Game1.smallFont.MeasureString(displayName).Y;
        float descY = textY + nameHeight + 4f;

        if (descY >= bounds.Bottom - 20 || descY < _resultsArea.Y || descY >= _resultsArea.Bottom - 10)
            return;

        int rightPadding = 10;
        int availableScreenWidth = bounds.Right - textX - rightPadding;
        int maxFontWidth = (int)(availableScreenWidth / DescriptionScale);

        string desc = TruncateText(subject.Description, Game1.smallFont, maxFontWidth);

        // Verify width after scale
        float actualWidth = Game1.smallFont.MeasureString(desc).X * DescriptionScale;
        if (actualWidth > availableScreenWidth)
        {
            maxFontWidth = (int)((availableScreenWidth - 20) / DescriptionScale);
            desc = TruncateText(subject.Description, Game1.smallFont, maxFontWidth);
        }

        Vector2 descPos = new(textX, descY);

        // Shadow
        b.DrawString(Game1.smallFont, desc, descPos + new Vector2(1f, 1f),
            Color.Black * 0.35f, 0f, Vector2.Zero, DescriptionScale, SpriteEffects.None, 0f);

        // Text
        b.DrawString(Game1.smallFont, desc, descPos,
            Color.Gray, 0f, Vector2.Zero, DescriptionScale, SpriteEffects.None, 0f);
    }

    private void DrawCategoryBadge(SpriteBatch b, string categoryText, int badgeX, int badgeY,
        int badgeWidth, int badgeHeight, Vector2 categorySize, int badgePadding)
    {
        if (badgeY < _resultsArea.Y - 10 || badgeY >= _resultsArea.Bottom)
            return;

        drawTextureBox(
            b, Game1.mouseCursors, new Rectangle(403, 373, 9, 9),
            badgeX, badgeY, badgeWidth, badgeHeight,
            Color.DarkSlateBlue * 0.9f, 2f, drawShadow: false
        );

        Vector2 textPos = new(
            badgeX + badgePadding,
            badgeY + (badgeHeight - categorySize.Y) / 2
        );

        b.DrawString(Game1.tinyFont, categoryText, textPos, Color.LightBlue);
    }

    private void DrawHighlightedName(SpriteBatch b, string name, int x, int y, int maxWidth)
    {
        string displayName = TruncateText(name, Game1.smallFont, maxWidth - 80);
        string searchText = (_searchBox.Text ?? "").Trim();

        if (string.IsNullOrEmpty(searchText))
        {
            Utility.drawTextWithShadow(b, displayName, Game1.smallFont, new Vector2(x, y), Color.White);
            return;
        }

        int matchIndex = displayName.ToLowerInvariant().IndexOf(searchText.ToLowerInvariant());
        if (matchIndex < 0)
        {
            Utility.drawTextWithShadow(b, displayName, Game1.smallFont, new Vector2(x, y), Color.White);
            return;
        }

        // Draw with highlight
        string before = displayName[..matchIndex];
        string match = displayName.Substring(matchIndex, Math.Min(searchText.Length, displayName.Length - matchIndex));
        string after = displayName[(matchIndex + match.Length)..];

        float currentX = x;

        if (!string.IsNullOrEmpty(before))
        {
            Utility.drawTextWithShadow(b, before, Game1.smallFont, new Vector2(currentX, y), Color.White);
            currentX += Game1.smallFont.MeasureString(before).X;
        }

        if (!string.IsNullOrEmpty(match))
        {
            Vector2 matchSize = Game1.smallFont.MeasureString(match);
            b.Draw(Game1.staminaRect,
                new Rectangle((int)currentX - 1, y - 1, (int)matchSize.X + 2, (int)matchSize.Y + 2),
                Color.Yellow * 0.3f);

            Utility.drawTextWithShadow(b, match, Game1.smallFont, new Vector2(currentX, y), Color.Yellow);
            currentX += matchSize.X;
        }

        if (!string.IsNullOrEmpty(after))
        {
            Utility.drawTextWithShadow(b, after, Game1.smallFont, new Vector2(currentX, y), Color.White);
        }
    }

    private void DrawScrollbar(SpriteBatch b)
    {
        if (_maxScrollOffset <= 0)
            return;

        // Track
        b.Draw(Game1.staminaRect, _scrollbarArea, Color.Black * 0.3f);

        // Thumb
        float viewRatio = (float)_resultsArea.Height / (_filteredSubjects.Count * ItemHeight);
        int thumbHeight = Math.Max(30, (int)(_scrollbarArea.Height * viewRatio));

        float scrollProgress = _maxScrollOffset > 0 ? _scrollOffset / _maxScrollOffset : 0;
        int thumbY = _scrollbarArea.Y + (int)((_scrollbarArea.Height - thumbHeight) * scrollProgress);

        b.Draw(Game1.staminaRect,
            new Rectangle(_scrollbarArea.X, thumbY, _scrollbarArea.Width, thumbHeight),
            Color.White * 0.7f);
    }

    private void DrawResultsCount(SpriteBatch b)
    {
        string countText = BuildCountText();
        Vector2 countPos = new(_resultsArea.X, _resultsArea.Bottom + 4);

        // Shadow
        b.DrawString(Game1.smallFont, countText, countPos + new Vector2(1, 1),
            Color.Black * 0.3f, 0f, Vector2.Zero, CountScale, SpriteEffects.None, 0.99f);

        // Text
        b.DrawString(Game1.smallFont, countText, countPos,
            Color.Gray, 0f, Vector2.Zero, CountScale, SpriteEffects.None, 1f);
    }

    private string BuildCountText()
    {
        if (!_isFullyLoaded)
        {
            int percent = _rawSubjects.Count > 0 ? (_wrapIndex * 100) / _rawSubjects.Count : 0;
            return $"Loading... {percent}%";
        }

        string countText = _filteredSubjects.Count == _wrappedSubjects.Count
            ? $"{_wrappedSubjects.Count} items"
            : $"{_filteredSubjects.Count} of {_wrappedSubjects.Count}";

        string searchText = (_searchBox.Text ?? "").Trim();
        if (!string.IsNullOrEmpty(searchText))
        {
            countText = $"'{TruncateText(searchText, Game1.smallFont, 100)}': {countText}";
        }

        return countText;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Truncates text to a single line with ellipsis if needed.
    /// </summary>
    private static string TruncateText(string text, SpriteFont font, int maxWidth)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Force single line
        text = text.Replace("\n", " ").Replace("\r", " ");
        while (text.Contains("  "))
            text = text.Replace("  ", " ");

        if (font.MeasureString(text).X <= maxWidth)
            return text;

        // Truncate with ellipsis
        const string ellipsis = "...";
        float ellipsisWidth = font.MeasureString(ellipsis).X;
        float targetWidth = maxWidth - ellipsisWidth;

        if (targetWidth <= 0)
            return ellipsis;

        for (int i = text.Length - 1; i > 0; i--)
        {
            string testString = text[..i];
            if (font.MeasureString(testString).X <= targetWidth)
                return testString.TrimEnd() + ellipsis;
        }

        return ellipsis;
    }

    private static string GetCategoryAbbreviation(string category) => category switch
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

    #endregion

    #region Nested Types

    private sealed class CategoryButton
    {
        public Rectangle Bounds { get; set; }
        public string Category { get; set; } = "";
        public bool IsSelected { get; set; }
    }

    #endregion
}