using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text.Json;

namespace Pokefarm.Game;

public sealed class FarmGame : Microsoft.Xna.Framework.Game
{
    private const int InventoryColumns = 4;
    private const int InventoryRows = 2;
    private const int PlayerSize = 32;
    private const float PlayerSpeed = 180f;
    private const float PreviewMoveSpeed = 260f;
    private const float PreviewMaxDistance = 180f;
    private const int TileSize = PlayerSize;
    private const int DefaultIconSize = 64;
    private const int WorldWidth = 1280;
    private const int WorldHeight = 720;
    private const int BorderThickness = 24;
    private const float WalkAnimationFrameTime = 0.12f;
    private const int PlayerSpriteCanvasSize = 32;
    private const int PlayerRenderSize = 48;
    private const int UiFontPixelSize = 2;
    private const int UiFontSpacing = 1;
    private const float InteractionRange = 18f;
    private const float InteractionMessageDuration = 2f;
    private const double SnackLifetimeSeconds = 2d;
    private const float SpawnedPokemonMoveDistance = 32f;
    private const float FollowStopDistance = 56f;
    private const float HomeWanderRadius = 96f;
    private const float DialogueGameplayShift = 120f;
    private const float DialogueTransitionDuration = 0.18f;
    private const int VisibleTalkOptionCount = 3;
    private const float SpawnedPokemonMinMoveDelay = 2f;
    private const float SpawnedPokemonMaxMoveDelay = 4f;
    private const float SpawnedPokemonMoveDuration = 0.3f;
    private const string PlayerPokemonName = "Ditto";
    private const float TalkExitDelaySeconds = 1f;
    private static readonly Color UnclaimedMarkerBackground = new(30, 20, 14, 230);
    private static readonly Color UnclaimedMarkerText = new(236, 220, 196);

    private readonly GraphicsDeviceManager _graphics;
    private readonly Rectangle _worldBounds = new(0, 0, WorldWidth, WorldHeight);
    private readonly List<PlacedItem> _placedItems = [];
    private readonly List<SpawnedPokemon> _spawnedDittos = [];
    private readonly List<InventoryEntry> _inventoryItems =
    [
        new(ItemCatalog.Bed, 3),
        new(ItemCatalog.WorkBench, 1),
        new(ItemCatalog.BasicSnack, 12),
        new(ItemCatalog.BasicSnack2, 11),
        new(ItemCatalog.Planter, 1),
        new(ItemCatalog.Wood, 3),
        new(ItemCatalog.Stone, 5)
    ];
    private readonly List<RecipeDefinition> _unlockedRecipes =
    [
        RecipeCatalog.WorkBench,
        RecipeCatalog.Bed
    ];
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private Texture2D? _circleTexture;
    private readonly Dictionary<string, Texture2D?> _pokemonSpriteSheets = [];
    private readonly Dictionary<string, Dictionary<string, SpriteFrame>> _pokemonFrames = [];
    private readonly HashSet<string> _pokemonSpriteLoadAttempted = [];
    private Vector2 _playerPosition = new(200f, 200f);
    private Vector2 _playerMovement;
    private Matrix _cameraMatrix = Matrix.Identity;
    private KeyboardState _previousKeyboard;
    private bool _previewPlacementValid;
    private int _selectedInventoryIndex;
    private Vector2 _previewOffset = new(PlayerSize + 24f, 0f);
    private PlacedItem? _previewItem;
    private PlacedItem? _removeTarget;
    private PlacedItem? _interactTarget;
    private int _talkTargetIndex = -1;
    private Rectangle _removeSelectorBounds;
    private InputMode _inputMode;
    private Direction _playerDirection = Direction.Down;
    private float _walkAnimationTimer;
    private int _walkAnimationFrame;
    private int _selectedCraftingIndex;
    private readonly TalkState _talkState = new();
    private CraftingSource _activeCraftingSource = CraftingSource.HandheldCrafting;
    private double _elapsedWorldTimeSeconds;
    private float _interactionMessageTimer;
    private float _dialogueTransition;
    private float _talkExitTimer;
    private string? _interactionMessage;
    private int _nextPokemonId = 1;
    private static readonly Dictionary<char, string[]> PixelFont = new()
    {
        ['A'] = ["01110","10001","10001","11111","10001","10001","10001"],
        ['B'] = ["11110","10001","10001","11110","10001","10001","11110"],
        ['C'] = ["01110","10001","10000","10000","10000","10001","01110"],
        ['D'] = ["11110","10001","10001","10001","10001","10001","11110"],
        ['E'] = ["11111","10000","10000","11110","10000","10000","11111"],
        ['F'] = ["11111","10000","10000","11110","10000","10000","10000"],
        ['G'] = ["01110","10001","10000","10111","10001","10001","01110"],
        ['H'] = ["10001","10001","10001","11111","10001","10001","10001"],
        ['I'] = ["11111","00100","00100","00100","00100","00100","11111"],
        ['K'] = ["10001","10010","10100","11000","10100","10010","10001"],
        ['L'] = ["10000","10000","10000","10000","10000","10000","11111"],
        ['M'] = ["10001","11011","10101","10001","10001","10001","10001"],
        ['N'] = ["10001","11001","10101","10011","10001","10001","10001"],
        ['O'] = ["01110","10001","10001","10001","10001","10001","01110"],
        ['P'] = ["11110","10001","10001","11110","10000","10000","10000"],
        ['Q'] = ["01110","10001","10001","10001","10101","10010","01101"],
        ['R'] = ["11110","10001","10001","11110","10100","10010","10001"],
        ['S'] = ["01111","10000","10000","01110","00001","00001","11110"],
        ['T'] = ["11111","00100","00100","00100","00100","00100","00100"],
        ['U'] = ["10001","10001","10001","10001","10001","10001","01110"],
        ['V'] = ["10001","10001","10001","10001","10001","01010","00100"],
        ['W'] = ["10001","10001","10001","10101","10101","10101","01010"],
        ['Y'] = ["10001","10001","01010","00100","00100","00100","00100"],
        ['0'] = ["01110","10001","10011","10101","11001","10001","01110"],
        ['1'] = ["00100","01100","00100","00100","00100","00100","01110"],
        ['2'] = ["01110","10001","00001","00010","00100","01000","11111"],
        ['3'] = ["11110","00001","00001","01110","00001","00001","11110"],
        ['4'] = ["00010","00110","01010","10010","11111","00010","00010"],
        ['5'] = ["11111","10000","10000","11110","00001","00001","11110"],
        ['6'] = ["01110","10000","10000","11110","10001","10001","01110"],
        ['7'] = ["11111","00001","00010","00100","01000","01000","01000"],
        ['8'] = ["01110","10001","10001","01110","10001","10001","01110"],
        ['9'] = ["01110","10001","10001","01111","00001","00001","01110"],
        ['!'] = ["00100","00100","00100","00100","00100","00000","00100"],
        ['^'] = ["00100","01010","10001","00000","00000","00000","00000"],
        ['/'] = ["00001","00010","00010","00100","01000","01000","10000"],
        [' '] = ["000","000","000","000","000","000","000"]
    };

    public FarmGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
        _circleTexture = CreateCircleTexture(DefaultIconSize);
        EnsurePokemonSpriteLoaded(PlayerPokemonName);
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboard = Keyboard.GetState();
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _elapsedWorldTimeSeconds += gameTime.ElapsedGameTime.TotalSeconds;
        _playerMovement = Vector2.Zero;
        bool inventoryPressed = keyboard.IsKeyDown(Keys.I) && !_previousKeyboard.IsKeyDown(Keys.I);
        bool confirmPressed = keyboard.IsKeyDown(Keys.Space) && !_previousKeyboard.IsKeyDown(Keys.Space);
        bool removeModePressed = keyboard.IsKeyDown(Keys.U) && !_previousKeyboard.IsKeyDown(Keys.U);
        bool interactPressed = keyboard.IsKeyDown(Keys.E) && !_previousKeyboard.IsKeyDown(Keys.E);
        bool talkPressed = keyboard.IsKeyDown(Keys.Q) && !_previousKeyboard.IsKeyDown(Keys.Q);
        bool moveLeftPressed = keyboard.IsKeyDown(Keys.A) && !_previousKeyboard.IsKeyDown(Keys.A);
        bool moveRightPressed = keyboard.IsKeyDown(Keys.D) && !_previousKeyboard.IsKeyDown(Keys.D);
        bool moveUpPressed = keyboard.IsKeyDown(Keys.W) && !_previousKeyboard.IsKeyDown(Keys.W);
        bool moveDownPressed = keyboard.IsKeyDown(Keys.S) && !_previousKeyboard.IsKeyDown(Keys.S);

        if (keyboard.IsKeyDown(Keys.Escape))
        {
            if (_inputMode == InputMode.Placement)
            {
                ExitPlacementMode(InputMode.Inventory);
            }
            else if (_inputMode == InputMode.Removal)
            {
                ExitRemovalMode(InputMode.Gameplay);
            }
            else if (_inputMode == InputMode.Inventory)
            {
                _inputMode = InputMode.Gameplay;
            }
            else if (_inputMode == InputMode.Crafting)
            {
                _inputMode = InputMode.Gameplay;
            }
            else if (_inputMode == InputMode.Talking)
            {
                ExitTalkMode();
            }
            else
            {
                Exit();
            }
        }

        if (inventoryPressed)
        {
            if (_inputMode == InputMode.Placement)
            {
                ExitPlacementMode(InputMode.Inventory);
            }
            else if (_inputMode == InputMode.Removal)
            {
                _inputMode = InputMode.Inventory;
                _removeTarget = null;
            }
            else
            {
                ToggleInventoryMode();
            }
        }

        if (removeModePressed)
        {
            if (_inputMode == InputMode.Removal)
            {
                ExitRemovalMode(InputMode.Gameplay);
            }
            else if (_inputMode == InputMode.Gameplay)
            {
                BeginRemovalMode();
            }
        }

        if (_inputMode == InputMode.Inventory)
        {
            UpdateInventoryNavigation(moveLeftPressed, moveRightPressed, moveUpPressed, moveDownPressed);

            if (confirmPressed)
            {
                BeginPlacementFromInventory();
            }
        }
        else if (_inputMode == InputMode.Placement)
        {
            UpdatePlacementPreview(keyboard, gameTime, moveLeftPressed, moveRightPressed, moveUpPressed, moveDownPressed);

            if (confirmPressed)
            {
                TryPlaceSelectedItem();
            }
        }
        else if (_inputMode == InputMode.Removal)
        {
            UpdateRemovalPreview(keyboard, gameTime, moveLeftPressed, moveRightPressed, moveUpPressed, moveDownPressed);

            if (confirmPressed)
            {
                TryPickUpSelectedItem();
            }
        }
        else if (_inputMode == InputMode.Crafting)
        {
            UpdateCraftingNavigation(moveUpPressed, moveDownPressed);

            if (interactPressed)
            {
                _inputMode = InputMode.Gameplay;
            }

            if (confirmPressed)
            {
                CraftSelectedRecipe();
            }
        }
        else if (_inputMode == InputMode.Talking)
        {
            if (_talkExitTimer > 0f)
            {
                _talkExitTimer = Math.Max(0f, _talkExitTimer - deltaTime);
                if (_talkExitTimer <= 0f)
                {
                    ExitTalkMode();
                }
            }
            else
            {
                UpdateTalkNavigation(moveUpPressed, moveDownPressed);

                if (talkPressed)
                {
                    ExitTalkMode();
                }

                if (confirmPressed)
                {
                    ConfirmTalkOption();
                }
            }
        }
        else
        {
            UpdateGameplayMovement(keyboard, gameTime);

            if (interactPressed)
            {
                TryInteractWithBuilding();
            }

            if (talkPressed)
            {
                TryTalkWithPokemon();
            }
        }

        _playerPosition.X = MathHelper.Clamp(
            _playerPosition.X,
            BorderThickness,
            _worldBounds.Width - BorderThickness - PlayerSize);
        _playerPosition.Y = MathHelper.Clamp(
            _playerPosition.Y,
            BorderThickness,
            _worldBounds.Height - BorderThickness - PlayerSize);

        UpdateExpiredSnacks();
        UpdateSpawnedPokemon(deltaTime);
        _interactTarget = _inputMode == InputMode.Gameplay ? FindInteractableTarget() : null;
        _talkTargetIndex = _inputMode == InputMode.Gameplay ? FindNearbyPokemonTargetIndex() : -1;
        if (_interactionMessageTimer > 0f)
        {
            _interactionMessageTimer = Math.Max(0f, _interactionMessageTimer - deltaTime);
            if (_interactionMessageTimer <= 0f)
            {
                _interactionMessage = null;
            }
        }

        float dialogueTransitionTarget = _inputMode == InputMode.Talking ? 1f : 0f;
        float dialogueTransitionStep = deltaTime / DialogueTransitionDuration;
        _dialogueTransition = MoveToward(_dialogueTransition, dialogueTransitionTarget, dialogueTransitionStep);

        UpdateCamera();
        _previousKeyboard = keyboard;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(77, 54, 36));

        if (_spriteBatch is null || _pixel is null)
        {
            base.Draw(gameTime);
            return;
        }

        _spriteBatch.Begin(transformMatrix: _cameraMatrix);
        DrawFarm();
        DrawPlacedItems();
        DrawSpawnedDittos();
        DrawPlacementPreview();
        DrawRemovalPreview();
        DrawPlayer();
        _spriteBatch.End();

        _spriteBatch.Begin();
        if (_inputMode == InputMode.Inventory)
        {
            DrawInventoryScreen();
        }
        else if (_inputMode == InputMode.Crafting)
        {
            DrawCraftingScreen();
        }
        else if (_inputMode == InputMode.Talking && _dialogueTransition >= 0.98f)
        {
            DrawTalkScreen();
        }

        DrawInteractionOverlay();
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void UpdateCamera()
    {
        Viewport viewport = GraphicsDevice.Viewport;
        float maxCameraX = Math.Max(0f, _worldBounds.Width - viewport.Width);
        float maxCameraY = Math.Max(0f, _worldBounds.Height - viewport.Height);

        float cameraX = MathHelper.Clamp(
            _playerPosition.X + (PlayerSize / 2f) - (viewport.Width / 2f),
            0f,
            maxCameraX);
        float cameraY = MathHelper.Clamp(
            _playerPosition.Y + (PlayerSize / 2f) - (viewport.Height / 2f),
            0f,
            maxCameraY);

        float dialogueShift = _dialogueTransition * DialogueGameplayShift;
        _cameraMatrix = Matrix.CreateTranslation(-cameraX, -cameraY - dialogueShift, 0f);
    }

    private void DrawFarm()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Color dirt = new(116, 82, 56);
        Color boundary = new(74, 49, 30);

        for (int y = 0; y < _worldBounds.Height; y += TileSize)
        {
            for (int x = 0; x < _worldBounds.Width; x += TileSize)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle(x, y, TileSize, TileSize),
                    dirt);
            }
        }

        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, _worldBounds.Width, BorderThickness), boundary);
        _spriteBatch.Draw(_pixel, new Rectangle(0, _worldBounds.Height - BorderThickness, _worldBounds.Width, BorderThickness), boundary);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, BorderThickness, _worldBounds.Height), boundary);
        _spriteBatch.Draw(_pixel, new Rectangle(_worldBounds.Width - BorderThickness, 0, BorderThickness, _worldBounds.Height), boundary);
    }

    private void DrawPlacedItems()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        foreach (PlacedItem item in _placedItems)
        {
            Texture2D texture = (item.Definition.Kind == ItemKind.Building || item.Definition.Kind == ItemKind.Snack) && item.Definition.HasCollision
                ? _pixel
                : _circleTexture;
            _spriteBatch.Draw(texture, item.Bounds, item.Definition.Tint);
            DrawPanelBorder(item.Bounds, new Color(40, 28, 20));
        }
    }

    private void DrawPlacementPreview()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null || _previewItem is null)
        {
            return;
        }

        Color tint = _previewPlacementValid
            ? new Color(_previewItem.Definition.Tint.R, _previewItem.Definition.Tint.G, _previewItem.Definition.Tint.B, (byte)150)
            : new Color((byte)220, (byte)80, (byte)80, (byte)150);

        Texture2D texture = (_previewItem.Definition.Kind == ItemKind.Building || _previewItem.Definition.Kind == ItemKind.Snack) && _previewItem.Definition.HasCollision
            ? _pixel
            : _circleTexture;
        _spriteBatch.Draw(texture, _previewItem.Bounds, tint);
        DrawPanelBorder(_previewItem.Bounds, _previewPlacementValid ? new Color(255, 245, 180) : new Color(180, 70, 70));
    }

    private void DrawSpawnedDittos()
    {
        if (_spriteBatch is null)
        {
            return;
        }

        foreach (SpawnedPokemon pokemon in _spawnedDittos)
        {
            DrawPokemonAt(pokemon.Position, pokemon.Name, pokemon.Direction);
            DrawUnclaimedMarker(pokemon);
        }
    }

    private void DrawUnclaimedMarker(SpawnedPokemon pokemon)
    {
        if (_spriteBatch is null || _pixel is null || pokemon.IsClaimed)
        {
            return;
        }

        Point textSize = MeasurePixelText("!", UiFontPixelSize, UiFontSpacing);
        int markerX = (int)pokemon.Position.X + (PlayerSize / 2);
        int markerY = (int)pokemon.Position.Y - 18;
        Rectangle panel = new(
            markerX - (textSize.X / 2) - 5,
            markerY - (textSize.Y / 2) - 3,
            textSize.X + 10,
            textSize.Y + 6);

        _spriteBatch.Draw(_pixel, panel, UnclaimedMarkerBackground);
        DrawPanelBorder(panel, new Color(181, 138, 95));
        DrawPixelText("!", new Vector2(panel.X + 5, panel.Y + 3), UnclaimedMarkerText);
    }

    private void DrawRemovalPreview()
    {
        if (_spriteBatch is null || _pixel is null || _inputMode != InputMode.Removal)
        {
            return;
        }

        _spriteBatch.Draw(_pixel, _removeSelectorBounds, new Color(255, 255, 255, 45));
        DrawPanelBorder(_removeSelectorBounds, new Color(255, 245, 180));

        if (_removeTarget is null)
        {
            return;
        }

        Color tint = new(255, 215, 90, 140);
        Texture2D texture = (_removeTarget.Definition.Kind == ItemKind.Building || _removeTarget.Definition.Kind == ItemKind.Snack) && _removeTarget.Definition.HasCollision
            ? _pixel
            : _circleTexture ?? _pixel;
        _spriteBatch.Draw(texture, _removeTarget.Bounds, tint);
        DrawPanelBorder(_removeTarget.Bounds, Color.Gold);
    }

    private void DrawInventoryScreen()
    {
        if (_spriteBatch is null || _pixel is null || _circleTexture is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle overlay = new(0, 0, viewport.Width, viewport.Height);
        Rectangle panel = new(viewport.Width / 2 - 360, viewport.Height / 2 - 200, 720, 400);

        _spriteBatch.Draw(_pixel, overlay, new Color(20, 14, 10, 200));
        _spriteBatch.Draw(_pixel, panel, new Color(44, 31, 23, 240));
        DrawPanelBorder(panel, new Color(181, 138, 95));

        for (int slotIndex = 0; slotIndex < InventoryColumns * InventoryRows; slotIndex++)
        {
            int column = slotIndex % InventoryColumns;
            int row = slotIndex / InventoryColumns;
            Rectangle slot = new(
                panel.X + 84 + (column * 140),
                panel.Y + 74 + (row * 140),
                96,
                96);
            bool hasItem = slotIndex < _inventoryItems.Count;
            bool selected = slotIndex == _selectedInventoryIndex;

            _spriteBatch.Draw(_pixel, slot, hasItem ? new Color(88, 66, 49) : new Color(58, 43, 33));
            DrawPanelBorder(slot, selected ? Color.Gold : new Color(120, 90, 65));

            if (hasItem)
            {
                InventoryEntry entry = _inventoryItems[slotIndex];
                int iconSize = Math.Min(64, Math.Max(entry.Definition.Size.X, entry.Definition.Size.Y));
                Rectangle iconBounds = new(
                    slot.Center.X - (iconSize / 2),
                    slot.Y + 22,
                    iconSize,
                    iconSize);
                Texture2D texture = (entry.Definition.Kind == ItemKind.Building || entry.Definition.Kind == ItemKind.Snack) && entry.Definition.HasCollision
                    ? _pixel
                    : _circleTexture;
                string label = entry.Definition.Name.ToUpperInvariant();
                Point labelSize = MeasurePixelText(label, UiFontPixelSize, UiFontSpacing);
                Vector2 labelPosition = new(slot.Center.X - (labelSize.X / 2f), slot.Y + 6);

                DrawPixelText(label, labelPosition, new Color(236, 220, 196));

                _spriteBatch.Draw(texture, iconBounds, entry.Definition.Tint);
                DrawPanelBorder(iconBounds, new Color(48, 36, 26));

                string quantityText = entry.Quantity.ToString();
                Point quantitySize = MeasurePixelText(quantityText, UiFontPixelSize, UiFontSpacing);
                Rectangle quantityBadge = new(
                    slot.Center.X - (quantitySize.X / 2) - 6,
                    slot.Bottom - quantitySize.Y - 10,
                    quantitySize.X + 12,
                    quantitySize.Y + 6);
                _spriteBatch.Draw(_pixel, quantityBadge, new Color(45, 33, 25, 220));
                DrawPanelBorder(quantityBadge, new Color(160, 130, 100));
                DrawPixelText(
                    quantityText,
                    new Vector2(quantityBadge.X + 6, quantityBadge.Y + 3),
                    new Color(236, 220, 196));
            }
        }
    }

    private void DrawCraftingScreen()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle overlay = new(0, 0, viewport.Width, viewport.Height);
        Rectangle panel = new(viewport.Width / 2 - 360, viewport.Height / 2 - 240, 720, 480);
        Rectangle listArea = new(panel.X + 36, panel.Y + 52, panel.Width - 72, panel.Height - 88);

        _spriteBatch.Draw(_pixel, overlay, new Color(20, 14, 10, 210));
        _spriteBatch.Draw(_pixel, panel, new Color(44, 31, 23, 245));
        DrawPanelBorder(panel, new Color(181, 138, 95));
        DrawPixelText(GetCraftingTitle(), new Vector2(panel.X + 24, panel.Y + 18), new Color(236, 220, 196));

        if (activeRecipes.Count == 0)
        {
            DrawPixelText("NONE", new Vector2(panel.X + 24, panel.Y + 64), new Color(210, 180, 152));
            return;
        }

        int rowHeight = 92;
        int visibleRows = Math.Max(1, listArea.Height / rowHeight);
        int scrollOffset = Math.Clamp(_selectedCraftingIndex - visibleRows + 1, 0, Math.Max(0, activeRecipes.Count - visibleRows));

        for (int visibleIndex = 0; visibleIndex < visibleRows; visibleIndex++)
        {
            int recipeIndex = scrollOffset + visibleIndex;
            if (recipeIndex >= activeRecipes.Count)
            {
                break;
            }

            RecipeDefinition recipe = activeRecipes[recipeIndex];
            Rectangle rowBounds = new(
                listArea.X,
                listArea.Y + (visibleIndex * rowHeight),
                listArea.Width,
                rowHeight - 10);
            bool selected = recipeIndex == _selectedCraftingIndex;
            bool canCraft = CanCraftRecipe(recipe);

            _spriteBatch.Draw(_pixel, rowBounds, selected ? new Color(88, 66, 49) : new Color(58, 43, 33));
            DrawPanelBorder(rowBounds, selected ? Color.Gold : new Color(120, 90, 65));

            DrawPixelText(recipe.Output.Name.ToUpperInvariant(), new Vector2(rowBounds.X + 16, rowBounds.Y + 12), new Color(236, 220, 196));
            string inventoryText = $"IN INVENTORY {GetInventoryQuantity(recipe.Output)}";
            Point inventoryTextSize = MeasurePixelText(inventoryText);
            DrawPixelText(
                inventoryText,
                new Vector2(rowBounds.Right - inventoryTextSize.X - 16, rowBounds.Y + 12),
                new Color(210, 190, 164));

            for (int costIndex = 0; costIndex < recipe.Costs.Count; costIndex++)
            {
                RecipeCost cost = recipe.Costs[costIndex];
                int ownedAmount = GetInventoryQuantity(cost.Item);
                Color costColor = ownedAmount >= cost.Quantity ? new Color(196, 226, 180) : new Color(236, 180, 164);
                string costText = $"{cost.Item.Name.ToUpperInvariant()} {ownedAmount}/{cost.Quantity}";
                DrawPixelText(costText, new Vector2(rowBounds.X + 16, rowBounds.Y + 36 + (costIndex * 18)), costColor);
            }

            DrawPixelText(
                canCraft ? "SPACE MAKE" : "NEED ITEMS",
                new Vector2(rowBounds.Right - 142, rowBounds.Y + 34),
                canCraft ? new Color(224, 210, 158) : new Color(180, 130, 110));
        }
    }

    private void DrawPanelBorder(Rectangle rectangle, Color color)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Y, rectangle.Width, 2), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Bottom - 2, rectangle.Width, 2), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.X, rectangle.Y, 2, rectangle.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rectangle.Right - 2, rectangle.Y, 2, rectangle.Height), color);
    }

    private void DrawPixelText(string text, Vector2 position, Color color, int pixelSize = UiFontPixelSize, int spacing = UiFontSpacing)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        int cursorX = (int)position.X;
        int cursorY = (int)position.Y;

        foreach (char rawCharacter in text)
        {
            char character = char.ToUpperInvariant(rawCharacter);
            if (!PixelFont.TryGetValue(character, out string[]? glyph))
            {
                glyph = PixelFont[' '];
            }

            int glyphWidth = glyph[0].Length;
            for (int row = 0; row < glyph.Length; row++)
            {
                for (int column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] != '1')
                    {
                        continue;
                    }

                    _spriteBatch.Draw(
                        _pixel,
                        new Rectangle(
                            cursorX + (column * pixelSize),
                            cursorY + (row * pixelSize),
                            pixelSize,
                            pixelSize),
                        color);
                }
            }

            cursorX += (glyphWidth * pixelSize) + spacing;
        }
    }

    private Point MeasurePixelText(string text, int pixelSize = UiFontPixelSize, int spacing = UiFontSpacing)
    {
        int width = 0;
        int maxHeight = 0;

        foreach (char rawCharacter in text)
        {
            char character = char.ToUpperInvariant(rawCharacter);
            if (!PixelFont.TryGetValue(character, out string[]? glyph))
            {
                glyph = PixelFont[' '];
            }

            width += (glyph[0].Length * pixelSize) + spacing;
            maxHeight = Math.Max(maxHeight, glyph.Length * pixelSize);
        }

        if (text.Length > 0)
        {
            width -= spacing;
        }

        return new Point(width, maxHeight);
    }

    private void DrawPlayer()
    {
        DrawPokemonAt(_playerPosition, PlayerPokemonName, _playerDirection, _playerMovement != Vector2.Zero, _walkAnimationFrame);
    }

    private void DrawPokemonAt(
        Vector2 topLeftPosition,
        string pokemonName,
        Direction direction = Direction.Down,
        bool isWalking = false,
        int walkFrame = 0)
    {
        if (_spriteBatch is null)
        {
            return;
        }

        if (!TryGetPokemonSpriteData(pokemonName, out Texture2D? spriteSheet, out Dictionary<string, SpriteFrame>? frames) ||
            spriteSheet is null ||
            frames is null)
        {
            if (_pixel is not null)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)topLeftPosition.X, (int)topLeftPosition.Y, PlayerSize, PlayerSize),
                    Color.CornflowerBlue);
            }

            return;
        }

        SpriteFrame? frame = GetCurrentPlayerFrame(frames, direction, isWalking, walkFrame);
        if (frame is null)
        {
            if (_pixel is not null)
            {
                _spriteBatch.Draw(
                    _pixel,
                    new Rectangle((int)topLeftPosition.X, (int)topLeftPosition.Y, PlayerSize, PlayerSize),
                    Color.CornflowerBlue);
            }

            return;
        }

        float scale = PlayerRenderSize / (float)PlayerSpriteCanvasSize;
        int renderX = (int)topLeftPosition.X;
        int renderY = (int)topLeftPosition.Y + PlayerSize - (int)MathF.Round(PlayerSpriteCanvasSize * scale);

        Rectangle destination = new(
            renderX + (int)MathF.Round(frame.OffsetX * scale),
            renderY + (int)MathF.Round(frame.OffsetY * scale),
            (int)MathF.Round(frame.Source.Width * scale),
            (int)MathF.Round(frame.Source.Height * scale));

        _spriteBatch.Draw(spriteSheet, destination, frame.Source, Color.White);
    }

    private void DrawInteractionOverlay()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        if (_talkTargetIndex >= 0)
        {
            string pokemonName = _spawnedDittos[_talkTargetIndex].Name.ToUpperInvariant();
            DrawPromptPanel($"PRESS Q TO TALK TO {pokemonName}", new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 104));
        }

        if (_interactTarget?.Definition == ItemCatalog.Bed && !string.IsNullOrEmpty(_interactTarget.ResidentPokemonName))
        {
            DrawPromptPanel($"{_interactTarget.ResidentPokemonName!.ToUpperInvariant()} LIVES HERE", new Point(GraphicsDevice.Viewport.Width / 2, 48));
        }

        if (_interactTarget is not null)
        {
            string buildingName = _interactTarget.Definition.Name.ToUpperInvariant();
            DrawPromptPanel($"PRESS E TO USE {buildingName}", new Point(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height - 64));
        }

        if (!string.IsNullOrEmpty(_interactionMessage))
        {
            DrawPromptPanel(_interactionMessage, new Point(GraphicsDevice.Viewport.Width / 2, 48));
        }
    }

    private void DrawTalkScreen()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle panel = new(36, viewport.Height - 137, viewport.Width - 72, 127);
        Rectangle iconPanel = new(panel.X + 18, panel.Y + 15, 97, 97);
        Rectangle textPanel = new(iconPanel.Right + 18, panel.Y + 15, panel.Width - 97 - 265 - 72, 97);
        Rectangle optionsPanel = new(textPanel.Right + 18, panel.Y + 15, 205, 97);
        List<PokemonDialogueOption> options = _talkState.Options;
        int scrollOffset = Math.Clamp(_talkState.SelectedOptionIndex - VisibleTalkOptionCount + 1, 0, Math.Max(0, options.Count - VisibleTalkOptionCount));

        _spriteBatch.Draw(_pixel, panel, new Color(44, 31, 23, 245));
        DrawPanelBorder(panel, new Color(181, 138, 95));

        _spriteBatch.Draw(_pixel, iconPanel, new Color(58, 43, 33));
        DrawPanelBorder(iconPanel, new Color(120, 90, 65));
        Rectangle portraitBounds = new(iconPanel.X + 17, iconPanel.Y + 13, 64, 64);
        if (_talkState.IconTexture is not null)
        {
            _spriteBatch.Draw(_talkState.IconTexture, portraitBounds, Color.White);
        }
        else
        {
            _spriteBatch.Draw(_circleTexture ?? _pixel, portraitBounds, new Color(178, 208, 118));
        }

        DrawPanelBorder(portraitBounds, new Color(236, 220, 196));
        DrawPixelText(_talkState.SpeakerName.ToUpperInvariant(), new Vector2(iconPanel.X + 8, iconPanel.Bottom - 19), new Color(236, 220, 196));

        _spriteBatch.Draw(_pixel, textPanel, new Color(58, 43, 33));
        DrawPanelBorder(textPanel, new Color(120, 90, 65));
        DrawPixelText(_talkState.Text, new Vector2(textPanel.X + 16, textPanel.Y + 18), new Color(236, 220, 196));

        _spriteBatch.Draw(_pixel, optionsPanel, new Color(58, 43, 33));
        DrawPanelBorder(optionsPanel, new Color(120, 90, 65));
        DrawPixelText("YOU SAY", new Vector2(optionsPanel.X + 12, optionsPanel.Y + 8), new Color(236, 220, 196));

        if (scrollOffset > 0)
        {
            DrawTriangleIndicator(new Point(optionsPanel.Right - 10, optionsPanel.Y + 13), true, new Color(236, 220, 196));
        }

        if (scrollOffset + VisibleTalkOptionCount < options.Count)
        {
            DrawTriangleIndicator(new Point(optionsPanel.Right - 10, optionsPanel.Bottom - 13), false, new Color(236, 220, 196));
        }

        for (int visibleIndex = 0; visibleIndex < VisibleTalkOptionCount; visibleIndex++)
        {
            int optionIndex = scrollOffset + visibleIndex;
            if (optionIndex >= options.Count)
            {
                break;
            }

            Rectangle optionBounds = new(optionsPanel.X + 10, optionsPanel.Y + 28 + (visibleIndex * 20), optionsPanel.Width - 30, 16);
            bool selected = optionIndex == _talkState.SelectedOptionIndex;

            _spriteBatch.Draw(_pixel, optionBounds, selected ? new Color(88, 66, 49) : new Color(58, 43, 33));
            DrawPanelBorder(optionBounds, selected ? Color.Gold : new Color(120, 90, 65));
            DrawPixelText(options[optionIndex].Label, new Vector2(optionBounds.X + 7, optionBounds.Y + 2), new Color(236, 220, 196));
        }
    }

    private void DrawPromptPanel(string text, Point center)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Point textSize = MeasurePixelText(text, UiFontPixelSize, UiFontSpacing);
        Rectangle panel = new(
            center.X - (textSize.X / 2) - 12,
            center.Y - (textSize.Y / 2) - 8,
            textSize.X + 24,
            textSize.Y + 16);

        _spriteBatch.Draw(_pixel, panel, new Color(30, 20, 14, 220));
        DrawPanelBorder(panel, new Color(181, 138, 95));
        DrawPixelText(text, new Vector2(panel.X + 12, panel.Y + 8), new Color(236, 220, 196));
    }

    private void DrawTriangleIndicator(Point center, bool pointUp, Color color)
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        int[] rowWidths = [2, 4, 6];
        for (int row = 0; row < rowWidths.Length; row++)
        {
            int width = rowWidths[row];
            int y = pointUp ? center.Y + row : center.Y - row - 1;
            Rectangle rowRect = new(center.X - (width / 2), y, width, 1);
            _spriteBatch.Draw(_pixel, rowRect, color);
        }
    }

    private Vector2 ResolveMovement(Vector2 candidatePosition)
    {
        Vector2 resolvedPosition = _playerPosition;
        Vector2 horizontalCandidate = new(candidatePosition.X, _playerPosition.Y);
        Vector2 verticalCandidate = new(_playerPosition.X, candidatePosition.Y);

        if (!CollidesWithPlacedItem(horizontalCandidate))
        {
            resolvedPosition.X = horizontalCandidate.X;
        }

        if (!CollidesWithPlacedItem(verticalCandidate))
        {
            resolvedPosition.Y = verticalCandidate.Y;
        }

        return resolvedPosition;
    }

    private bool CollidesWithPlacedItem(Vector2 playerTopLeft)
    {
        Rectangle playerBounds = new(
            (int)playerTopLeft.X,
            (int)playerTopLeft.Y,
            PlayerSize,
            PlayerSize);

        foreach (PlacedItem item in _placedItems)
        {
            if (item.Definition.HasCollision && playerBounds.Intersects(item.Bounds))
            {
                return true;
            }
        }

        foreach (SpawnedPokemon ditto in _spawnedDittos)
        {
            Rectangle dittoBounds = new(
                (int)ditto.Position.X,
                (int)ditto.Position.Y,
                PlayerSize,
                PlayerSize);

            if (playerBounds.Intersects(dittoBounds))
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateExpiredSnacks()
    {
        for (int index = _placedItems.Count - 1; index >= 0; index--)
        {
            PlacedItem item = _placedItems[index];
            if (item.Definition.Kind != ItemKind.Snack)
            {
                continue;
            }

            if (item.GetAgeSeconds(_elapsedWorldTimeSeconds) < SnackLifetimeSeconds)
            {
                continue;
            }

            Vector2 spawnPosition = new(
                item.Bounds.Center.X - (PlayerSize / 2f),
                item.Bounds.Center.Y - (PlayerSize / 2f));
            string spawnName = SnackSpawnCatalog.RollSpawnName(item.Definition);
            _spawnedDittos.Add(new SpawnedPokemon(
                _nextPokemonId++,
                spawnName,
                spawnPosition,
                Direction.Down,
                GetRandomMoveDelaySeconds()));
            _placedItems.RemoveAt(index);

            if (_removeTarget == item)
            {
                _removeTarget = null;
            }
        }
    }

    private void UpdateSpawnedPokemon(float deltaTime)
    {
        for (int index = 0; index < _spawnedDittos.Count; index++)
        {
            SpawnedPokemon pokemon = _spawnedDittos[index];
            if (_inputMode == InputMode.Talking && index == _talkState.ActivePokemonIndex)
            {
                Direction facePlayerDirection = GetDirectionTowardTarget(pokemon.Position, _playerPosition);
                _spawnedDittos[index] = pokemon with
                {
                    Direction = facePlayerDirection,
                    IsMoving = false,
                    MoveTimeRemaining = 0f,
                    MoveTarget = pokemon.Position
                };
                continue;
            }

            if (pokemon.SpeechTimerRemaining > 0f)
            {
                float speechTimeRemaining = pokemon.SpeechTimerRemaining - deltaTime;
                pokemon = pokemon with
                {
                    SpeechTimerRemaining = Math.Max(0f, speechTimeRemaining),
                    SpeechText = speechTimeRemaining > 0f ? pokemon.SpeechText : null
                };
                _spawnedDittos[index] = pokemon;
            }

            if (!pokemon.IsClaimed)
            {
                _spawnedDittos[index] = pokemon with
                {
                    IsMoving = false,
                    MoveTimeRemaining = 0f,
                    MoveTarget = pokemon.Position,
                    MoveCooldownRemaining = 0f
                };
                continue;
            }

            if (pokemon.IsMoving)
            {
                float moveTimeRemaining = pokemon.MoveTimeRemaining - deltaTime;
                if (moveTimeRemaining <= 0f)
                {
                    pokemon = pokemon with
                    {
                        Position = pokemon.MoveTarget,
                        IsMoving = false,
                        MoveTimeRemaining = 0f,
                        MoveTarget = pokemon.MoveTarget
                    };
                    _spawnedDittos[index] = pokemon;
                    if (!pokemon.IsFollowingPlayer)
                    {
                        continue;
                    }
                }
                else
                {
                    float step = deltaTime / pokemon.MoveTimeRemaining;
                    Vector2 updatedPosition = Vector2.Lerp(pokemon.Position, pokemon.MoveTarget, step);
                    _spawnedDittos[index] = pokemon with
                    {
                        Position = updatedPosition,
                        MoveTimeRemaining = moveTimeRemaining
                    };
                }

                continue;
            }

            float nextMoveTime = pokemon.IsFollowingPlayer ? 0f : pokemon.MoveCooldownRemaining - deltaTime;
            if (nextMoveTime > 0f)
            {
                _spawnedDittos[index] = pokemon with { MoveCooldownRemaining = nextMoveTime };
                continue;
            }

            _spawnedDittos[index] = TryMoveSpawnedPokemon(pokemon, index) with
            {
                MoveCooldownRemaining = GetNextPokemonMoveDelaySeconds(pokemon)
            };
        }
    }

    private SpawnedPokemon TryMoveSpawnedPokemon(SpawnedPokemon pokemon, int pokemonIndex)
    {
        if (pokemon.IsFollowingPlayer)
        {
            Vector2 toPlayer = _playerPosition - pokemon.Position;
            if (toPlayer.LengthSquared() <= FollowStopDistance * FollowStopDistance)
            {
                return pokemon;
            }
        }

        Direction[] directions = pokemon.IsFollowingPlayer
            ? GetFollowDirectionsTowardPlayer(pokemon.Position)
            : GetPokemonWanderDirections(pokemon);

        foreach (Direction direction in directions)
        {
            Vector2 movement = DirectionToMovement(direction);
            if (pokemon.IsFollowingPlayer && movement == Vector2.Zero)
            {
                continue;
            }

            Vector2 candidatePosition = pokemon.Position + (movement * SpawnedPokemonMoveDistance);
            Rectangle candidateBounds = new(
                (int)candidatePosition.X,
                (int)candidatePosition.Y,
                PlayerSize,
                PlayerSize);
            Rectangle playableArea = new(
                BorderThickness,
                BorderThickness,
                _worldBounds.Width - (BorderThickness * 2),
                _worldBounds.Height - (BorderThickness * 2));

            if (!playableArea.Contains(candidateBounds) ||
                CollidesWithPlacedItem(candidatePosition) ||
                CollidesWithSpawnedPokemon(candidateBounds, pokemonIndex) ||
                IsOutsideHomeRange(pokemon, candidatePosition))
            {
                continue;
            }

            return pokemon with
            {
                Direction = direction,
                IsMoving = true,
                MoveTarget = candidatePosition,
                MoveTimeRemaining = SpawnedPokemonMoveDuration
            };
        }

        if (pokemon.IsFollowingPlayer)
        {
            Direction direction = GetDirectionTowardPlayer(pokemon.Position);
            return pokemon with { Direction = direction };
        }

        return pokemon;
    }

    private bool CollidesWithSpawnedPokemon(Rectangle candidateBounds, int currentPokemonIndex)
    {
        Rectangle playerBounds = new((int)_playerPosition.X, (int)_playerPosition.Y, PlayerSize, PlayerSize);
        if (candidateBounds.Intersects(playerBounds))
        {
            return true;
        }

        for (int index = 0; index < _spawnedDittos.Count; index++)
        {
            if (index == currentPokemonIndex)
            {
                continue;
            }

            SpawnedPokemon pokemon = _spawnedDittos[index];

            Rectangle pokemonBounds = new(
                (int)pokemon.Position.X,
                (int)pokemon.Position.Y,
                PlayerSize,
                PlayerSize);

            if (candidateBounds.Intersects(pokemonBounds))
            {
                return true;
            }
        }

        return false;
    }

    private static float GetRandomMoveDelaySeconds()
    {
        return Random.Shared.NextSingle() * (SpawnedPokemonMaxMoveDelay - SpawnedPokemonMinMoveDelay) + SpawnedPokemonMinMoveDelay;
    }

    private static float GetNextPokemonMoveDelaySeconds(SpawnedPokemon pokemon)
    {
        return pokemon.IsFollowingPlayer ? 0f : GetRandomMoveDelaySeconds();
    }

    private static Vector2 DirectionToMovement(Direction direction)
    {
        return direction switch
        {
            Direction.Down => new Vector2(0f, 1f),
            Direction.Left => new Vector2(-1f, 0f),
            Direction.Up => new Vector2(0f, -1f),
            Direction.Right => new Vector2(1f, 0f),
            _ => Vector2.Zero
        };
    }

    private Direction GetDirectionTowardPlayer(Vector2 pokemonPosition)
    {
        return GetDirectionTowardTarget(pokemonPosition, _playerPosition);
    }

    private Direction[] GetFollowDirectionsTowardPlayer(Vector2 pokemonPosition)
    {
        Vector2 delta = _playerPosition - pokemonPosition;
        Direction horizontal = delta.X < 0f ? Direction.Left : Direction.Right;
        Direction vertical = delta.Y < 0f ? Direction.Up : Direction.Down;

        if (MathF.Abs(delta.X) > MathF.Abs(delta.Y))
        {
            return [horizontal, vertical];
        }

        return [vertical, horizontal];
    }

    private Direction[] GetPokemonWanderDirections(SpawnedPokemon pokemon)
    {
        if (pokemon.HomePosition is Vector2 homePosition)
        {
            Vector2 toHome = homePosition - pokemon.Position;
            if (toHome.LengthSquared() > HomeWanderRadius * HomeWanderRadius)
            {
                return GetDirectionsTowardTarget(pokemon.Position, homePosition);
            }
        }

        Direction[] directions = [Direction.Down, Direction.Left, Direction.Up, Direction.Right];
        for (int index = directions.Length - 1; index > 0; index--)
        {
            int swapIndex = Random.Shared.Next(index + 1);
            (directions[index], directions[swapIndex]) = (directions[swapIndex], directions[index]);
        }

        return directions;
    }

    private bool IsOutsideHomeRange(SpawnedPokemon pokemon, Vector2 candidatePosition)
    {
        if (pokemon.HomePosition is not Vector2 homePosition)
        {
            return false;
        }

        float radiusSquared = HomeWanderRadius * HomeWanderRadius;
        float currentDistanceSquared = Vector2.DistanceSquared(pokemon.Position, homePosition);
        float candidateDistanceSquared = Vector2.DistanceSquared(candidatePosition, homePosition);

        if (currentDistanceSquared > radiusSquared)
        {
            return candidateDistanceSquared >= currentDistanceSquared;
        }

        return candidateDistanceSquared > radiusSquared;
    }

    private Direction[] GetDirectionsTowardTarget(Vector2 startPosition, Vector2 targetPosition)
    {
        Vector2 delta = targetPosition - startPosition;
        Direction horizontal = delta.X < 0f ? Direction.Left : Direction.Right;
        Direction vertical = delta.Y < 0f ? Direction.Up : Direction.Down;

        if (MathF.Abs(delta.X) > MathF.Abs(delta.Y))
        {
            return [horizontal, vertical];
        }

        return [vertical, horizontal];
    }

    private void TryPlaceSelectedItem()
    {
        if (_inputMode != InputMode.Placement || !_previewPlacementValid || _previewItem is null || _inventoryItems.Count == 0)
        {
            return;
        }

        _placedItems.Add(_previewItem);
        RemoveInventoryUnitAt(_selectedInventoryIndex);
        ExitPlacementMode(InputMode.Gameplay);

        if (_inventoryItems.Count == 0)
        {
            _selectedInventoryIndex = 0;
        }
        else if (_selectedInventoryIndex >= _inventoryItems.Count)
        {
            _selectedInventoryIndex = _inventoryItems.Count - 1;
        }
    }

    private void ToggleInventoryMode()
    {
        if (_inputMode == InputMode.Inventory)
        {
            _inputMode = InputMode.Gameplay;
            return;
        }

        ExitPlacementMode(InputMode.Gameplay);
        ExitRemovalMode(InputMode.Gameplay);
        _inputMode = InputMode.Inventory;
    }

    private void BeginPlacementFromInventory()
    {
        if (_selectedInventoryIndex >= _inventoryItems.Count)
        {
            return;
        }

        if (!_inventoryItems[_selectedInventoryIndex].Definition.IsPlaceable)
        {
            return;
        }

        _inputMode = InputMode.Placement;
        _previewOffset = new Vector2(PlayerSize + 24f, 0f);
        UpdatePlacementPreview(Keyboard.GetState(), new GameTime(), false, false, false, false);
    }

    private void ExitPlacementMode(InputMode nextMode)
    {
        _inputMode = nextMode;
        _previewItem = null;
        _previewPlacementValid = false;
    }

    private void BeginRemovalMode()
    {
        _inputMode = InputMode.Removal;
        _previewOffset = new Vector2(PlayerSize + 24f, 0f);
        UpdateRemovalPreview(Keyboard.GetState(), new GameTime(), false, false, false, false);
    }

    private void ExitRemovalMode(InputMode nextMode)
    {
        if (_inputMode == InputMode.Removal)
        {
            _inputMode = nextMode;
        }

        _removeTarget = null;
        _removeSelectorBounds = Rectangle.Empty;
    }

    private void UpdatePlacementPreview(
        KeyboardState keyboard,
        GameTime gameTime,
        bool moveLeftPressed,
        bool moveRightPressed,
        bool moveUpPressed,
        bool moveDownPressed)
    {
        if (_inventoryItems.Count == 0)
        {
            _previewItem = null;
            _previewPlacementValid = false;
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 previewMovement = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.A) && !moveLeftPressed)
        {
            previewMovement.X -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.D) && !moveRightPressed)
        {
            previewMovement.X += 1f;
        }

        if (keyboard.IsKeyDown(Keys.W) && !moveUpPressed)
        {
            previewMovement.Y -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.S) && !moveDownPressed)
        {
            previewMovement.Y += 1f;
        }

        if (previewMovement != Vector2.Zero)
        {
            previewMovement.Normalize();
            _previewOffset += previewMovement * PreviewMoveSpeed * deltaTime;
        }

        if (_previewOffset != Vector2.Zero && _previewOffset.LengthSquared() > PreviewMaxDistance * PreviewMaxDistance)
        {
            _previewOffset.Normalize();
            _previewOffset *= PreviewMaxDistance;
        }

        InventoryEntry selectedItem = _inventoryItems[_selectedInventoryIndex];
        Vector2 playerCenter = _playerPosition + new Vector2(PlayerSize / 2f, PlayerSize / 2f);
        Vector2 previewCenter = playerCenter + _previewOffset;
        Point itemSize = selectedItem.Definition.Size;
        Rectangle itemBounds = new(
            (int)(previewCenter.X - (itemSize.X / 2f)),
            (int)(previewCenter.Y - (itemSize.Y / 2f)),
            itemSize.X,
            itemSize.Y);
        PlacedItem previewItem = new(itemBounds, selectedItem.Definition, _elapsedWorldTimeSeconds);

        _previewItem = previewItem;
        _previewPlacementValid = CanPlaceItem(previewItem);
    }

    private void UpdateRemovalPreview(
        KeyboardState keyboard,
        GameTime gameTime,
        bool moveLeftPressed,
        bool moveRightPressed,
        bool moveUpPressed,
        bool moveDownPressed)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 previewMovement = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.A) && !moveLeftPressed)
        {
            previewMovement.X -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.D) && !moveRightPressed)
        {
            previewMovement.X += 1f;
        }

        if (keyboard.IsKeyDown(Keys.W) && !moveUpPressed)
        {
            previewMovement.Y -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.S) && !moveDownPressed)
        {
            previewMovement.Y += 1f;
        }

        if (previewMovement != Vector2.Zero)
        {
            previewMovement.Normalize();
            _previewOffset += previewMovement * PreviewMoveSpeed * deltaTime;
        }

        if (_previewOffset != Vector2.Zero && _previewOffset.LengthSquared() > PreviewMaxDistance * PreviewMaxDistance)
        {
            _previewOffset.Normalize();
            _previewOffset *= PreviewMaxDistance;
        }

        Vector2 playerCenter = _playerPosition + new Vector2(PlayerSize / 2f, PlayerSize / 2f);
        Vector2 previewCenter = playerCenter + _previewOffset;
        Point selectorSize = GetRemovalSelectorSize();
        _removeSelectorBounds = new Rectangle(
            (int)(previewCenter.X - (selectorSize.X / 2f)),
            (int)(previewCenter.Y - (selectorSize.Y / 2f)),
            selectorSize.X,
            selectorSize.Y);

        _removeTarget = null;
        foreach (PlacedItem item in _placedItems)
        {
            if (item.Bounds.Intersects(_removeSelectorBounds))
            {
                _removeTarget = item;
                break;
            }
        }
    }

    private void UpdateGameplayMovement(KeyboardState keyboard, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 movement = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.W))
        {
            movement.Y -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.S))
        {
            movement.Y += 1f;
        }

        if (keyboard.IsKeyDown(Keys.A))
        {
            movement.X -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.D))
        {
            movement.X += 1f;
        }

        _playerMovement = movement;
        if (movement == Vector2.Zero)
        {
            _walkAnimationTimer = 0f;
            _walkAnimationFrame = 0;
            return;
        }

        movement.Normalize();
        UpdatePlayerDirection(movement);
        UpdateWalkAnimation(deltaTime);
        Vector2 candidatePosition = _playerPosition + (movement * PlayerSpeed * deltaTime);
        _playerPosition = ResolveMovement(candidatePosition);
    }

    private void UpdateInventoryNavigation(bool moveLeft, bool moveRight, bool moveUp, bool moveDown)
    {
        int currentColumn = _selectedInventoryIndex % InventoryColumns;
        int currentRow = _selectedInventoryIndex / InventoryColumns;

        if (moveLeft)
        {
            currentColumn = Math.Max(0, currentColumn - 1);
        }

        if (moveRight)
        {
            currentColumn = Math.Min(InventoryColumns - 1, currentColumn + 1);
        }

        if (moveUp)
        {
            currentRow = Math.Max(0, currentRow - 1);
        }

        if (moveDown)
        {
            currentRow = Math.Min(InventoryRows - 1, currentRow + 1);
        }

        int newIndex = (currentRow * InventoryColumns) + currentColumn;
        _selectedInventoryIndex = Math.Clamp(newIndex, 0, (InventoryColumns * InventoryRows) - 1);
    }

    private bool CanPlaceItem(PlacedItem candidateItem)
    {
        Rectangle playerBounds = new((int)_playerPosition.X, (int)_playerPosition.Y, PlayerSize, PlayerSize);
        Rectangle playableArea = new(
            BorderThickness,
            BorderThickness,
            _worldBounds.Width - (BorderThickness * 2),
            _worldBounds.Height - (BorderThickness * 2));

        if (candidateItem.Definition.Kind != ItemKind.Building && candidateItem.Definition.Kind != ItemKind.Snack)
        {
            return false;
        }

        if (!playableArea.Contains(candidateItem.Bounds) || (candidateItem.Definition.HasCollision && candidateItem.Bounds.Intersects(playerBounds)))
        {
            return false;
        }

        foreach (PlacedItem item in _placedItems)
        {
            if (!item.Bounds.Intersects(candidateItem.Bounds))
            {
                continue;
            }

            if (item.Definition.HasCollision || candidateItem.Definition.HasCollision)
            {
                return false;
            }
        }

        return true;
    }

    private void TryPickUpSelectedItem()
    {
        if (_inputMode != InputMode.Removal || _removeTarget is null || _inventoryItems.Count >= InventoryColumns * InventoryRows)
        {
            return;
        }

        _placedItems.Remove(_removeTarget);
        AddInventoryItem(_removeTarget.Definition, 1);
        _removeTarget = null;
        UpdateRemovalPreview(Keyboard.GetState(), new GameTime(), false, false, false, false);
    }

    private void TryInteractWithBuilding()
    {
        if (_interactTarget is null)
        {
            OpenCrafting(CraftingSource.HandheldCrafting);
            return;
        }

        if (_interactTarget.Definition == ItemCatalog.WorkBench)
        {
            OpenCrafting(CraftingSource.BasicWorkBenchCrafting);
            return;
        }

        OpenBuildingTalk(_interactTarget);
    }

    private void TryTalkWithPokemon()
    {
        if (_talkTargetIndex < 0)
        {
            return;
        }

        FaceConversationTarget(_spawnedDittos[_talkTargetIndex].Position);
        FacePokemonTowardPlayer(_talkTargetIndex);
        _talkState.BeginPokemonTalk(
            _talkTargetIndex,
            PokemonDialogueService.GetOpeningText(_spawnedDittos[_talkTargetIndex]),
            PokemonDialogueService.GetOptions(_spawnedDittos[_talkTargetIndex]),
            _spawnedDittos[_talkTargetIndex].Name);
        _talkExitTimer = 0f;
        SetActiveTalkIcon(_spawnedDittos[_talkTargetIndex].Name);
        _inputMode = InputMode.Talking;
    }

    private void OpenCrafting(CraftingSource craftingSource)
    {
        _activeCraftingSource = craftingSource;
        _inputMode = InputMode.Crafting;
        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        _selectedCraftingIndex = Math.Clamp(_selectedCraftingIndex, 0, Math.Max(0, activeRecipes.Count - 1));
    }

    private PlacedItem? FindInteractableTarget()
    {
        Rectangle searchArea = GetFacingInteractionArea();

        foreach (PlacedItem item in _placedItems)
        {
            if (item.Definition.Kind != ItemKind.Building && item.Definition != ItemCatalog.Bed)
            {
                continue;
            }

            if (item.Bounds.Intersects(searchArea))
            {
                return item;
            }
        }

        return null;
    }

    private void OpenBuildingTalk(PlacedItem building)
    {
        FaceConversationTarget(new Vector2(building.Bounds.Center.X, building.Bounds.Center.Y));
        _talkState.BeginBuildingTalk(
            building,
            $"WHAT SHOULD I DO WITH THIS {building.Definition.Name.ToUpperInvariant()}",
            GetBuildingTalkOptions(building),
            "DITTO");
        _talkExitTimer = 0f;
        SetActiveTalkIcon("Ditto");
        _inputMode = InputMode.Talking;
    }

    private void ClearExistingBedForPokemon(int pokemonId)
    {
        for (int index = 0; index < _placedItems.Count; index++)
        {
            PlacedItem item = _placedItems[index];
            if (item.Definition != ItemCatalog.Bed || item.ResidentPokemonId != pokemonId)
            {
                continue;
            }

            _placedItems[index] = item with
            {
                ResidentPokemonName = null,
                ResidentPokemonId = null
            };
        }
    }

    private static Vector2 GetBedHomePosition(PlacedItem bed)
    {
        return new Vector2(
            bed.Bounds.Center.X - (PlayerSize / 2f),
            bed.Bounds.Center.Y - (PlayerSize / 2f));
    }

    private int FindNearbyPokemonTargetIndex()
    {
        Rectangle searchArea = GetFacingInteractionArea();

        for (int index = 0; index < _spawnedDittos.Count; index++)
        {
            SpawnedPokemon pokemon = _spawnedDittos[index];
            Rectangle pokemonBounds = new(
                (int)pokemon.Position.X,
                (int)pokemon.Position.Y,
                PlayerSize,
                PlayerSize);

            if (pokemonBounds.Intersects(searchArea))
            {
                return index;
            }
        }

        return -1;
    }

    private Rectangle GetFacingInteractionArea()
    {
        Rectangle playerBounds = new((int)_playerPosition.X, (int)_playerPosition.Y, PlayerSize, PlayerSize);
        Vector2 facingMovement = DirectionToMovement(_playerDirection);

        if (facingMovement == Vector2.Zero)
        {
            return playerBounds;
        }

        if (facingMovement.X != 0f)
        {
            int width = (int)InteractionRange;
            int x = facingMovement.X > 0f ? playerBounds.Right : playerBounds.X - width;
            return new Rectangle(x, playerBounds.Y, width, playerBounds.Height);
        }

        int height = (int)InteractionRange;
        int y = facingMovement.Y > 0f ? playerBounds.Bottom : playerBounds.Y - height;
        return new Rectangle(playerBounds.X, y, playerBounds.Width, height);
    }

    private void UpdateTalkNavigation(bool moveUp, bool moveDown)
    {
        if (moveUp)
        {
            _talkState.MoveSelection(-1);
        }

        if (moveDown)
        {
            _talkState.MoveSelection(1);
        }
    }

    private void ConfirmTalkOption()
    {
        PokemonDialogueOption? selectedOption = _talkState.GetSelectedOption();
        if (selectedOption is null)
        {
            ExitTalkMode();
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.ToggleFollowing)
        {
            if (_talkState.ActivePokemonIndex < 0 || _talkState.ActivePokemonIndex >= _spawnedDittos.Count)
            {
                ExitTalkMode();
                return;
            }

            SpawnedPokemon pokemon = _spawnedDittos[_talkState.ActivePokemonIndex];
            bool willFollowPlayer = !pokemon.IsFollowingPlayer;
            pokemon = pokemon with
            {
                IsFollowingPlayer = willFollowPlayer,
                MoveCooldownRemaining = pokemon.IsFollowingPlayer ? GetRandomMoveDelaySeconds() : 0f,
                IsMoving = false,
                MoveTimeRemaining = 0f,
                MoveTarget = pokemon.Position
            };

            _spawnedDittos[_talkState.ActivePokemonIndex] = pokemon;
            _talkState.SetOptions(PokemonDialogueService.GetOptions(pokemon));
            if (!string.IsNullOrEmpty(selectedOption.ResponseText))
            {
                _talkState.SetText(selectedOption.ResponseText);
            }

            if (selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
            }

            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.SetHome && selectedOption.TargetPokemonId.HasValue)
        {
            AssignPokemonHome(selectedOption.TargetPokemonId.Value);
            if (selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
            }
            else
            {
                ExitTalkMode();
            }
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.SetText && !string.IsNullOrEmpty(selectedOption.ResponseText))
        {
            _talkState.SetText(selectedOption.ResponseText);
            if (selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
            }
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.Exit)
        {
            if (!string.IsNullOrEmpty(selectedOption.ResponseText))
            {
                _talkState.SetText(selectedOption.ResponseText);
            }

            if (selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
            }
            else
            {
                ExitTalkMode();
            }

            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.None)
        {
            if (!string.IsNullOrEmpty(selectedOption.ResponseText))
            {
                _talkState.SetText(selectedOption.ResponseText);
            }

            return;
        }

        ExitTalkMode();
    }

    private void BeginTalkExitCountdown()
    {
        _talkExitTimer = TalkExitDelaySeconds;
    }

    private void ExitTalkMode()
    {
        _inputMode = InputMode.Gameplay;
        _talkExitTimer = 0f;
        _talkState.Reset();
    }

    private List<PokemonDialogueOption> GetBuildingTalkOptions(PlacedItem building)
    {
        List<PokemonDialogueOption> options = [];
        if (building.Definition == ItemCatalog.Bed)
        {
            foreach (SpawnedPokemon pokemon in _spawnedDittos)
            {
                if (!pokemon.IsFollowingPlayer)
                {
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"SET {pokemon.Name.ToUpperInvariant()} HOME",
                    PokemonDialogueAction.SetHome,
                    TargetPokemonId: pokemon.PokemonId));
            }
        }

        options.Add(new PokemonDialogueOption("BYE", PokemonDialogueAction.Exit));
        return options;
    }

    private void AssignPokemonHome(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.Bed)
        {
            return;
        }

        int pokemonIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (pokemonIndex < 0)
        {
            return;
        }

        SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
        Vector2 homePosition = GetBedHomePosition(_talkState.ActiveBuilding);
        _spawnedDittos[pokemonIndex] = pokemon with
        {
            IsClaimed = true,
            IsFollowingPlayer = false,
            IsMoving = false,
            MoveTimeRemaining = 0f,
            MoveTarget = pokemon.Position,
            HomePosition = homePosition,
            SpeechText = "HOME!",
            SpeechTimerRemaining = InteractionMessageDuration
        };

        ClearExistingBedForPokemon(pokemon.PokemonId);

        int bedIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (bedIndex >= 0)
        {
            PlacedItem bed = _placedItems[bedIndex];
            _placedItems[bedIndex] = bed with
            {
                ResidentPokemonName = pokemon.Name,
                ResidentPokemonId = pokemon.PokemonId
            };
            _interactTarget = _placedItems[bedIndex];
            _talkState.UpdateBuildingReference(_placedItems[bedIndex]);
        }

        _interactionMessage = $"{pokemon.Name.ToUpperInvariant()} MOVED IN";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    private void SetActiveTalkIcon(string pokemonName)
    {
        if (_talkState.IconName == pokemonName)
        {
            return;
        }

        _talkState.SetIcon(pokemonName, null);
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Pokemon Icons", $"{pokemonName}Icon.png");
        if (!File.Exists(iconPath))
        {
            return;
        }

        using FileStream iconStream = File.OpenRead(iconPath);
        _talkState.SetIcon(pokemonName, Texture2D.FromStream(GraphicsDevice, iconStream));
    }

    private void FaceConversationTarget(Vector2 targetPosition)
    {
        _playerDirection = GetDirectionTowardTarget(_playerPosition, targetPosition);
    }

    private void FacePokemonTowardPlayer(int pokemonIndex)
    {
        if (pokemonIndex < 0 || pokemonIndex >= _spawnedDittos.Count)
        {
            return;
        }

        SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
        _spawnedDittos[pokemonIndex] = pokemon with
        {
            Direction = GetDirectionTowardTarget(pokemon.Position, _playerPosition),
            IsMoving = false,
            MoveTimeRemaining = 0f,
            MoveTarget = pokemon.Position
        };
    }

    private static Direction GetDirectionTowardTarget(Vector2 sourcePosition, Vector2 targetPosition)
    {
        Vector2 delta = targetPosition - sourcePosition;
        if (delta == Vector2.Zero)
        {
            return Direction.Down;
        }

        if (MathF.Abs(delta.X) > MathF.Abs(delta.Y))
        {
            return delta.X < 0f ? Direction.Left : Direction.Right;
        }

        return delta.Y < 0f ? Direction.Up : Direction.Down;
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + MathF.Sign(target - current) * maxDelta;
    }

    private Point GetRemovalSelectorSize()
    {
        if (_removeTarget is not null)
        {
            return _removeTarget.Definition.Size;
        }

        int maxSize = 0;
        foreach (InventoryEntry entry in _inventoryItems)
        {
            if (!entry.Definition.IsPlaceable)
            {
                continue;
            }

            maxSize = Math.Max(maxSize, Math.Max(entry.Definition.Size.X, entry.Definition.Size.Y));
        }

        maxSize = Math.Max(maxSize, 48);
        return new Point(maxSize, maxSize);
    }

    private void AddInventoryItem(ItemDefinition definition, int quantity)
    {
        int existingIndex = _inventoryItems.FindIndex(entry => entry.Definition == definition);
        if (existingIndex >= 0)
        {
            InventoryEntry existing = _inventoryItems[existingIndex];
            _inventoryItems[existingIndex] = existing with { Quantity = existing.Quantity + quantity };
            return;
        }

        _inventoryItems.Add(new InventoryEntry(definition, quantity));
    }

    private int GetInventoryQuantity(ItemDefinition definition)
    {
        InventoryEntry? entry = _inventoryItems.Find(entry => entry.Definition == definition);
        return entry?.Quantity ?? 0;
    }

    private void UpdateCraftingNavigation(bool moveUp, bool moveDown)
    {
        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        if (activeRecipes.Count == 0)
        {
            _selectedCraftingIndex = 0;
            return;
        }

        if (moveUp)
        {
            _selectedCraftingIndex = Math.Max(0, _selectedCraftingIndex - 1);
        }

        if (moveDown)
        {
            _selectedCraftingIndex = Math.Min(activeRecipes.Count - 1, _selectedCraftingIndex + 1);
        }
    }

    private bool CanCraftRecipe(RecipeDefinition recipe)
    {
        foreach (RecipeCost cost in recipe.Costs)
        {
            if (GetInventoryQuantity(cost.Item) < cost.Quantity)
            {
                return false;
            }
        }

        return true;
    }

    private void CraftSelectedRecipe()
    {
        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        if (_inputMode != InputMode.Crafting || activeRecipes.Count == 0)
        {
            return;
        }

        RecipeDefinition recipe = activeRecipes[_selectedCraftingIndex];
        if (!CanCraftRecipe(recipe))
        {
            _interactionMessage = "NEED ITEMS";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        foreach (RecipeCost cost in recipe.Costs)
        {
            RemoveInventoryItem(cost.Item, cost.Quantity);
        }

        AddInventoryItem(recipe.Output, 1);
        _interactionMessage = $"{recipe.Output.Name.ToUpperInvariant()} MADE";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    private List<RecipeDefinition> GetActiveRecipes()
    {
        return _unlockedRecipes
            .FindAll(recipe => recipe.Source == _activeCraftingSource);
    }

    private string GetCraftingTitle()
    {
        return _activeCraftingSource switch
        {
            CraftingSource.HandheldCrafting => "HANDHELD CRAFTING",
            CraftingSource.BasicWorkBenchCrafting => "BASIC WORK BENCH",
            _ => "CRAFTING"
        };
    }

    private void RemoveInventoryUnitAt(int index)
    {
        InventoryEntry entry = _inventoryItems[index];
        if (entry.Quantity > 1)
        {
            _inventoryItems[index] = entry with { Quantity = entry.Quantity - 1 };
            return;
        }

        _inventoryItems.RemoveAt(index);
    }

    private void RemoveInventoryItem(ItemDefinition definition, int quantity)
    {
        int existingIndex = _inventoryItems.FindIndex(entry => entry.Definition == definition);
        if (existingIndex < 0)
        {
            return;
        }

        InventoryEntry entry = _inventoryItems[existingIndex];
        int remaining = Math.Max(0, entry.Quantity - quantity);
        if (remaining == 0)
        {
            _inventoryItems.RemoveAt(existingIndex);
            if (_selectedInventoryIndex >= _inventoryItems.Count)
            {
                _selectedInventoryIndex = Math.Max(0, _inventoryItems.Count - 1);
            }

            return;
        }

        _inventoryItems[existingIndex] = entry with { Quantity = remaining };
    }

    private Texture2D CreateCircleTexture(int diameter)
    {
        Texture2D texture = new(GraphicsDevice, diameter, diameter);
        Color[] data = new Color[diameter * diameter];
        float radius = diameter / 2f;
        Vector2 center = new(radius, radius);

        for (int y = 0; y < diameter; y++)
        {
            for (int x = 0; x < diameter; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                data[(y * diameter) + x] = distance <= radius ? Color.White : Color.Transparent;
            }
        }

        texture.SetData(data);
        return texture;
    }

    private bool TryGetPokemonSpriteData(
        string pokemonName,
        out Texture2D? spriteSheet,
        out Dictionary<string, SpriteFrame>? frames)
    {
        EnsurePokemonSpriteLoaded(pokemonName);
        if (_pokemonSpriteSheets.TryGetValue(pokemonName, out spriteSheet) &&
            _pokemonFrames.TryGetValue(pokemonName, out frames))
        {
            return true;
        }

        frames = null;
        spriteSheet = null;
        return false;
    }

    private void EnsurePokemonSpriteLoaded(string pokemonName)
    {
        if (_pokemonSpriteLoadAttempted.Contains(pokemonName))
        {
            return;
        }

        _pokemonSpriteLoadAttempted.Add(pokemonName);
        string assetDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Pokemon Models");
        string spritePath = Path.Combine(assetDirectory, $"{pokemonName}.png");
        string atlasPath = Path.Combine(assetDirectory, $"{pokemonName}.json");

        Dictionary<string, SpriteFrame> frames = [];
        LoadSpriteSet(spritePath, atlasPath, out Texture2D? spriteSheet, frames);
        if (spriteSheet is null || frames.Count == 0)
        {
            return;
        }

        _pokemonSpriteSheets[pokemonName] = spriteSheet;
        _pokemonFrames[pokemonName] = frames;
    }

    private void LoadSpriteSet(
        string spritePath,
        string atlasPath,
        out Texture2D? spriteSheet,
        Dictionary<string, SpriteFrame> targetFrames)
    {
        spriteSheet = null;
        targetFrames.Clear();

        if (!File.Exists(spritePath) || !File.Exists(atlasPath))
        {
            return;
        }

        using FileStream spriteStream = File.OpenRead(spritePath);
        spriteSheet = Texture2D.FromStream(GraphicsDevice, spriteStream);

        string atlasJson = File.ReadAllText(atlasPath);
        SpriteAtlas? atlas = JsonSerializer.Deserialize<SpriteAtlas>(
            atlasJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        if (atlas?.Textures is null)
        {
            return;
        }

        foreach (AtlasTexture texture in atlas.Textures)
        {
            if (texture.Frames is null)
            {
                continue;
            }

            foreach (AtlasFrame frame in texture.Frames)
            {
                if (frame.Filename is null || frame.Frame is null || frame.SourceSize is null || frame.SpriteSourceSize is null)
                {
                    continue;
                }

                targetFrames[frame.Filename] = new SpriteFrame(
                    new Rectangle(frame.Frame.X, frame.Frame.Y, frame.Frame.W, frame.Frame.H),
                    frame.SourceSize.W,
                    frame.SourceSize.H,
                    frame.SpriteSourceSize.X,
                    frame.SpriteSourceSize.Y);
            }
        }
    }

    private void UpdatePlayerDirection(Vector2 movement)
    {
        if (MathF.Abs(movement.X) > MathF.Abs(movement.Y))
        {
            _playerDirection = movement.X < 0f ? Direction.Left : Direction.Right;
            return;
        }

        if (movement.Y != 0f)
        {
            _playerDirection = movement.Y < 0f ? Direction.Up : Direction.Down;
        }
    }

    private void UpdateWalkAnimation(float deltaTime)
    {
        _walkAnimationTimer += deltaTime;
        if (_walkAnimationTimer < WalkAnimationFrameTime)
        {
            return;
        }

        _walkAnimationTimer -= WalkAnimationFrameTime;
        _walkAnimationFrame = (_walkAnimationFrame + 1) % 5;
    }

    private SpriteFrame? GetCurrentPlayerFrame(Dictionary<string, SpriteFrame> frames, Direction direction, bool isWalking, int walkFrame)
    {
        string action = isWalking ? "Walk" : "Idle";
        int directionIndex = direction switch
        {
            Direction.Down => 0,
            Direction.Left => 6,
            Direction.Up => 4,
            Direction.Right => 2,
            _ => 0
        };

        int frameIndex = action == "Walk" ? walkFrame : 0;
        string key = $"Normal/{action}/Anim/{directionIndex}/{frameIndex:0000}";
        if (frames.TryGetValue(key, out SpriteFrame? frame))
        {
            return frame;
        }

        string fallbackKey = $"Normal/Idle/Anim/{directionIndex}/0000";
        return frames.TryGetValue(fallbackKey, out frame) ? frame : null;
    }

}
