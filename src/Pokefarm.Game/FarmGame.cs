using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Text.Json;
using static Pokefarm.Game.BuildingWorkerHelpers;
using static Pokefarm.Game.WorkbenchCraftingHelpers;

namespace Pokefarm.Game;

public sealed partial class FarmGame : Microsoft.Xna.Framework.Game
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
    private const float PlayerIdleStartDelaySeconds = 1f;
    private const float PlayerIdleFrameTime = 0.22f;
    private const int PlayerIdleFrameCount = 5;
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
    private const float UnclaimedPokemonIdleFrameTime = 0.22f;
    private const float UnclaimedPokemonIdleCyclePauseSeconds = 0.8f;
    private const int UnclaimedPokemonIdleFrameCount = 5;
    private const string PlayerPokemonName = "Ditto";
    private const float TalkExitDelaySeconds = 1f;
    private const string StartupMusicFileName = "09 Celadon City's Theme.mp3";
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
        new(ItemCatalog.Tree, 2),
        new(ItemCatalog.Farm, 1),
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
    private readonly Dictionary<string, Texture2D?> _pokemonIconTextures = [];
    private readonly HashSet<string> _pokemonIconLoadAttempted = [];
    private Song? _backgroundMusic;
    private SoundEffect? _backgroundMusicEffect;
    private SoundEffectInstance? _backgroundMusicEffectInstance;
    private dynamic? _windowsMediaPlayer;
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
    private float _playerIdleStationaryTimer;
    private float _playerIdleAnimationTimer;
    private int _playerIdleAnimationFrame;
    private int _selectedCraftingIndex;
    private readonly TalkState _talkState = new();
    private CraftingSource _activeCraftingSource = CraftingSource.HandheldCrafting;
    private int _activeWorkbenchIndex = -1;
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
        TryStartBackgroundMusic();
    }

    protected override void UnloadContent()
    {
        MediaPlayer.Stop();
        _backgroundMusic?.Dispose();
        _backgroundMusic = null;
        _backgroundMusicEffectInstance?.Stop();
        _backgroundMusicEffectInstance?.Dispose();
        _backgroundMusicEffectInstance = null;
        _backgroundMusicEffect?.Dispose();
        _backgroundMusicEffect = null;
        try
        {
            if (_windowsMediaPlayer is not null)
            {
                _windowsMediaPlayer.controls.stop();
                _windowsMediaPlayer.close();
                _windowsMediaPlayer = null;
            }
        }
        catch
        {
            _windowsMediaPlayer = null;
        }
        base.UnloadContent();
    }

    private void TryStartBackgroundMusic()
    {
        string musicPath = Path.Combine(AppContext.BaseDirectory, "Assets", StartupMusicFileName);
        if (!File.Exists(musicPath))
        {
            return;
        }

        try
        {
            _backgroundMusic = Song.FromUri(StartupMusicFileName, new Uri(musicPath, UriKind.Absolute));
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.4f;
            MediaPlayer.Play(_backgroundMusic);
            return;
        }
        catch
        {
            _backgroundMusic = null;
        }

        try
        {
            _backgroundMusicEffect = SoundEffect.FromFile(musicPath);
            _backgroundMusicEffectInstance = _backgroundMusicEffect.CreateInstance();
            _backgroundMusicEffectInstance.IsLooped = true;
            _backgroundMusicEffectInstance.Volume = 0.4f;
            _backgroundMusicEffectInstance.Play();
        }
        catch
        {
            _backgroundMusicEffectInstance = null;
            _backgroundMusicEffect = null;
        }

        TryStartBackgroundMusicWithWindowsPlayer(musicPath);
    }

    private void TryStartBackgroundMusicWithWindowsPlayer(string musicPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            Type? windowsMediaPlayerType = Type.GetTypeFromProgID("WMPlayer.OCX");
            if (windowsMediaPlayerType is null)
            {
                return;
            }

            dynamic player = Activator.CreateInstance(windowsMediaPlayerType)!;
            player.URL = musicPath;
            player.settings.setMode("loop", true);
            player.settings.volume = 40;
            player.controls.play();
            _windowsMediaPlayer = player;
        }
        catch
        {
            _windowsMediaPlayer = null;
        }
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
            UpdateCraftingNavigation(moveUpPressed, moveDownPressed, moveLeftPressed, moveRightPressed);

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
        UpdateAssignedWorkerActivityStates();
        UpdateResourceProduction(deltaTime);
        UpdateWorkbenchCrafting(deltaTime);
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

    private bool CollidesWithPlacedItem(Vector2 playerTopLeft)
    {
        Rectangle playerBounds = new(
            (int)playerTopLeft.X,
            (int)playerTopLeft.Y,
            PlayerSize,
            PlayerSize);
        Rectangle currentPlayerBounds = new(
            (int)_playerPosition.X,
            (int)_playerPosition.Y,
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
            if (ditto.IsWorking)
            {
                continue;
            }

            Rectangle dittoBounds = new(
                (int)ditto.Position.X,
                (int)ditto.Position.Y,
                PlayerSize,
                PlayerSize);

            if (playerBounds.Intersects(dittoBounds))
            {
                if (currentPlayerBounds.Intersects(dittoBounds))
                {
                    // If Ditto is already overlapping this Pokemon, temporarily ignore it
                    // so the player can step out instead of getting trapped.
                    continue;
                }

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
            SpawnedPokemonDefinition spawnDefinition = SpawnedPokemonCatalog.GetOrDefault(spawnName);
            _spawnedDittos.Add(new SpawnedPokemon(
                _nextPokemonId++,
                spawnDefinition.Name,
                spawnPosition,
                Direction.Down,
                GetRandomMoveDelaySeconds(),
                spawnDefinition.SkillLevels));
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
            if (pokemon.IsWorking)
            {
                _spawnedDittos[index] = pokemon with
                {
                    IsMoving = false,
                    MoveTimeRemaining = 0f,
                    MoveTarget = pokemon.Position,
                    MoveCooldownRemaining = 0f,
                    IdleAnimationFrame = 0,
                    IdleAnimationTimer = 0f,
                    IdleCyclePauseRemaining = 0f
                };
                continue;
            }

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

            if (pokemon.IsAssignedToWork && !pokemon.IsWorking && !IsAssignedBuildingFull(pokemon.PokemonId))
            {
                int assignedWorkBuildingIndex = FindAssignedResourceBuildingIndex(pokemon.PokemonId);
                if (assignedWorkBuildingIndex >= 0)
                {
                    PlacedItem assignedBuilding = _placedItems[assignedWorkBuildingIndex];
                    Rectangle exitBounds = GetResourceBuildingExitBounds(assignedBuilding);
                    Rectangle pokemonBounds = new((int)pokemon.Position.X, (int)pokemon.Position.Y, PlayerSize, PlayerSize);
                    if (!exitBounds.IsEmpty && pokemonBounds.Intersects(exitBounds))
                    {
                        _spawnedDittos[index] = pokemon with
                        {
                            IsWorking = true,
                            IsFollowingPlayer = false,
                            IsMoving = false,
                            MoveTimeRemaining = 0f,
                            MoveCooldownRemaining = 0f,
                            MoveTarget = pokemon.Position
                        };
                        continue;
                    }

                    if (pokemon.IsMoving)
                    {
                        float moveTimeRemaining = pokemon.MoveTimeRemaining - deltaTime;
                        if (moveTimeRemaining <= 0f)
                        {
                            _spawnedDittos[index] = pokemon with
                            {
                                Position = pokemon.MoveTarget,
                                IsMoving = false,
                                MoveTimeRemaining = 0f,
                                MoveTarget = pokemon.MoveTarget,
                                IsFollowingPlayer = false,
                                MoveCooldownRemaining = 0f
                            };
                        }
                        else
                        {
                            float step = deltaTime / pokemon.MoveTimeRemaining;
                            Vector2 updatedPosition = Vector2.Lerp(pokemon.Position, pokemon.MoveTarget, step);
                            _spawnedDittos[index] = pokemon with
                            {
                                Position = updatedPosition,
                                MoveTimeRemaining = moveTimeRemaining,
                                IsFollowingPlayer = false,
                                MoveCooldownRemaining = 0f
                            };
                        }

                        continue;
                    }

                    SpawnedPokemon updatedPokemon = pokemon with
                    {
                        IsFollowingPlayer = false,
                        MoveCooldownRemaining = 0f
                    };

                    if (TryFindPathDirectionToTargetArea(
                        updatedPokemon.Position,
                        exitBounds,
                        index,
                        out Direction pathDirection,
                        ignoreDynamicActorCollisions: true))
                    {
                        Vector2 movement = DirectionToMovement(pathDirection);
                        Vector2 candidatePosition = updatedPokemon.Position + (movement * SpawnedPokemonMoveDistance);
                        _spawnedDittos[index] = updatedPokemon with
                        {
                            Direction = pathDirection,
                            IsMoving = true,
                            MoveTarget = candidatePosition,
                            MoveTimeRemaining = SpawnedPokemonMoveDuration
                        };
                    }
                    else
                    {
                        _spawnedDittos[index] = updatedPokemon;
                    }

                    continue;
                }
            }

            if (!pokemon.IsClaimed)
            {
                pokemon = UpdateUnclaimedPokemonIdleAnimation(pokemon, deltaTime);
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

            pokemon = UpdateUnclaimedPokemonIdleAnimation(pokemon, deltaTime);
            _spawnedDittos[index] = pokemon;

            if (pokemon.IsFollowingPlayer)
            {
                float followMoveTime = pokemon.MoveCooldownRemaining - deltaTime;
                if (followMoveTime > 0f)
                {
                    _spawnedDittos[index] = pokemon with { MoveCooldownRemaining = followMoveTime };
                    continue;
                }

                _spawnedDittos[index] = TryMoveSpawnedPokemon(pokemon, index) with
                {
                    MoveCooldownRemaining = 0f,
                    ShowWorkBlockedMarker = false
                };
                continue;
            }

            if (pokemon.HomePosition is Vector2 homePosition &&
                Vector2.DistanceSquared(pokemon.Position, homePosition) > HomeWanderRadius * HomeWanderRadius)
            {
                Rectangle homeTargetArea = new((int)homePosition.X, (int)homePosition.Y, PlayerSize, PlayerSize);
                if (TryFindPathDirectionToTargetArea(
                    pokemon.Position,
                    homeTargetArea,
                    index,
                    out Direction homeDirection,
                    ignoreDynamicActorCollisions: true))
                {
                    Vector2 movement = DirectionToMovement(homeDirection);
                    Vector2 candidatePosition = pokemon.Position + (movement * SpawnedPokemonMoveDistance);
                    _spawnedDittos[index] = pokemon with
                    {
                        Direction = homeDirection,
                        IsMoving = true,
                        MoveTarget = candidatePosition,
                        MoveTimeRemaining = SpawnedPokemonMoveDuration,
                        MoveCooldownRemaining = 0f,
                        WanderTarget = null,
                        ShowWorkBlockedMarker = false
                    };
                }
                else
                {
                    _spawnedDittos[index] = pokemon with
                    {
                        MoveCooldownRemaining = 0f
                    };
                }

                continue;
            }

            int assignedBuildingIndex = FindAssignedResourceBuildingIndex(pokemon.PokemonId);
            bool hasJob = assignedBuildingIndex >= 0;
            bool jobHasWork = hasJob && _placedItems[assignedBuildingIndex].StoredProducedUnits < _placedItems[assignedBuildingIndex].Definition.MaxStoredProducedUnits;
            bool workPathBlocked = false;

            if (jobHasWork)
            {
                PlacedItem assignedBuilding = _placedItems[assignedBuildingIndex];
                Rectangle exitBounds = GetResourceBuildingExitBounds(assignedBuilding);
                Rectangle pokemonBounds = new((int)pokemon.Position.X, (int)pokemon.Position.Y, PlayerSize, PlayerSize);
                if (!exitBounds.IsEmpty && pokemonBounds.Intersects(exitBounds))
                {
                    _spawnedDittos[index] = pokemon with
                    {
                        IsWorking = true,
                        IsFollowingPlayer = false,
                        IsMoving = false,
                        MoveTimeRemaining = 0f,
                        MoveCooldownRemaining = 0f,
                        MoveTarget = pokemon.Position,
                        ShowWorkBlockedMarker = false
                    };
                    continue;
                }

                if (TryFindPathDirectionToTargetArea(
                    pokemon.Position,
                    exitBounds,
                    index,
                    out Direction workDirection,
                    ignoreDynamicActorCollisions: true))
                {
                    Vector2 movement = DirectionToMovement(workDirection);
                    Vector2 candidatePosition = pokemon.Position + (movement * SpawnedPokemonMoveDistance);
                    _spawnedDittos[index] = pokemon with
                    {
                        Direction = workDirection,
                        IsMoving = true,
                        MoveTarget = candidatePosition,
                        MoveTimeRemaining = SpawnedPokemonMoveDuration,
                        MoveCooldownRemaining = 0f,
                        ShowWorkBlockedMarker = false
                    };
                    continue;
                }

                workPathBlocked = true;
            }

            float nextMoveTime = pokemon.MoveCooldownRemaining - deltaTime;
            if (nextMoveTime > 0f)
            {
                _spawnedDittos[index] = pokemon with
                {
                    MoveCooldownRemaining = nextMoveTime,
                    ShowWorkBlockedMarker = workPathBlocked
                };
                continue;
            }

            Vector2? wanderTarget = pokemon.WanderTarget;
            if (wanderTarget.HasValue && Vector2.DistanceSquared(pokemon.Position, wanderTarget.Value) <= 4f)
            {
                wanderTarget = null;
            }

            if (!wanderTarget.HasValue)
            {
                wanderTarget = TryPickRandomWanderTargetInHomeRange(pokemon, index);
                if (!wanderTarget.HasValue)
                {
                    _spawnedDittos[index] = pokemon with
                    {
                        WanderTarget = null,
                        MoveCooldownRemaining = GetRandomMoveDelaySeconds(),
                        ShowWorkBlockedMarker = workPathBlocked
                    };
                    continue;
                }
            }

            Rectangle targetArea = new((int)wanderTarget.Value.X, (int)wanderTarget.Value.Y, PlayerSize, PlayerSize);
            if (TryFindPathDirectionToTargetArea(pokemon.Position, targetArea, index, out Direction wanderDirection))
            {
                Vector2 movement = DirectionToMovement(wanderDirection);
                Vector2 candidatePosition = pokemon.Position + (movement * SpawnedPokemonMoveDistance);
                _spawnedDittos[index] = pokemon with
                {
                    Direction = wanderDirection,
                    IsMoving = true,
                    MoveTarget = candidatePosition,
                    MoveTimeRemaining = SpawnedPokemonMoveDuration,
                    WanderTarget = wanderTarget,
                    MoveCooldownRemaining = GetRandomMoveDelaySeconds(),
                    ShowWorkBlockedMarker = workPathBlocked
                };
            }
            else
            {
                _spawnedDittos[index] = pokemon with
                {
                    WanderTarget = null,
                    MoveCooldownRemaining = GetRandomMoveDelaySeconds(),
                    ShowWorkBlockedMarker = workPathBlocked
                };
            }
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
            if (pokemon.IsWorking)
            {
                continue;
            }

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

    private void UpdateResourceProduction(float deltaTime)
    {
        for (int index = 0; index < _placedItems.Count; index++)
        {
            PlacedItem building = _placedItems[index];
            if (!building.Definition.IsResourceProduction ||
                building.Definition.ProducedMaterial is null ||
                building.Definition.EffortPerProducedUnit <= 0f ||
                building.Definition.MaxStoredProducedUnits <= 0)
            {
                continue;
            }

            if (building.StoredProducedUnits >= building.Definition.MaxStoredProducedUnits)
            {
                continue;
            }

            List<int> workerIds = GetWorkerPokemonIds(building);
            if (workerIds.Count == 0)
            {
                continue;
            }

            float effortPerSecond = 0f;
            foreach (int workerId in workerIds)
            {
                SpawnedPokemon? worker = _spawnedDittos.FirstOrDefault(pokemon => pokemon.PokemonId == workerId);
                if (worker is null || !worker.IsWorking)
                {
                    continue;
                }

                effortPerSecond += GetPokemonEffortPerSecond(worker, building.Definition);
            }

            if (effortPerSecond <= 0f)
            {
                continue;
            }

            float effort = building.StoredProductionEffort + (effortPerSecond * deltaTime);
            int storedUnits = building.StoredProducedUnits;
            int stepIndex = Math.Clamp(building.ProductionStepIndex, 0, Math.Max(0, building.Definition.ProductionStepCount - 1));
            while (effort >= building.Definition.EffortPerProducedUnit && storedUnits < building.Definition.MaxStoredProducedUnits)
            {
                effort -= building.Definition.EffortPerProducedUnit;
                stepIndex++;
                if (stepIndex >= building.Definition.ProductionStepCount)
                {
                    stepIndex = 0;
                    storedUnits++;
                }
            }

            _placedItems[index] = building with
            {
                StoredProductionEffort = storedUnits >= building.Definition.MaxStoredProducedUnits ? 0f : effort,
                StoredProducedUnits = storedUnits,
                ProductionStepIndex = storedUnits >= building.Definition.MaxStoredProducedUnits ? 0 : stepIndex
            };

            if (_interactTarget == building)
            {
                _interactTarget = _placedItems[index];
            }

            if (_talkState.ActiveBuilding == building)
            {
                _talkState.UpdateBuildingReference(_placedItems[index]);
            }
        }
    }

    private void UpdateWorkbenchCrafting(float deltaTime)
    {
        for (int index = 0; index < _placedItems.Count; index++)
        {
            PlacedItem item = _placedItems[index];
            if (item.Definition != ItemCatalog.WorkBench ||
                item.WorkbenchQueuedItem is null ||
                item.WorkbenchCraftEffortRemaining <= 0f)
            {
                continue;
            }

            if (!item.WorkerPokemonId.HasValue)
            {
                continue;
            }

            SpawnedPokemon? worker = _spawnedDittos.FirstOrDefault(pokemon => pokemon.PokemonId == item.WorkerPokemonId.Value);
            if (worker is null)
            {
                _placedItems[index] = item with
                {
                    WorkerPokemonId = null,
                    WorkerPokemonName = null
                };
                continue;
            }

            if (worker.GetSkillLevel(SkillType.Crafting) <= 0 ||
                !IsWorkbenchWithinPokemonBedRange(worker, item))
            {
                continue;
            }

            float effortPerSecond = worker.GetSkillLevel(SkillType.Crafting);
            float remaining = Math.Max(0f, item.WorkbenchCraftEffortRemaining - (effortPerSecond * deltaTime));
            _placedItems[index] = item with
            {
                WorkbenchCraftEffortRemaining = remaining
            };

            if (remaining <= 0f && _interactTarget == item)
            {
                _interactTarget = _placedItems[index];
                _interactionMessage = "WORKBENCH ITEM READY";
                _interactionMessageTimer = InteractionMessageDuration;
            }
        }
    }

    private void UpdateAssignedWorkerActivityStates()
    {
        for (int pokemonIndex = 0; pokemonIndex < _spawnedDittos.Count; pokemonIndex++)
        {
            SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
            int assignedBuildingIndex = FindAssignedResourceBuildingIndex(pokemon.PokemonId);
            if (assignedBuildingIndex < 0)
            {
                if (pokemon.IsAssignedToWork || pokemon.IsWorking)
                {
                    _spawnedDittos[pokemonIndex] = pokemon with
                    {
                        IsAssignedToWork = false,
                        IsWorking = false,
                        ShowWorkBlockedMarker = false
                    };
                }

                continue;
            }

            PlacedItem building = _placedItems[assignedBuildingIndex];
            bool buildingIsFull = building.StoredProducedUnits >= building.Definition.MaxStoredProducedUnits;
            if (!buildingIsFull)
            {
                _spawnedDittos[pokemonIndex] = pokemon with
                {
                    IsAssignedToWork = true
                };
                continue;
            }

            if (!pokemon.IsWorking)
            {
                _spawnedDittos[pokemonIndex] = pokemon with
                {
                    IsAssignedToWork = true
                };
                continue;
            }

            Vector2 idlePosition = GetWorkerRespawnPosition(building);
            _spawnedDittos[pokemonIndex] = pokemon with
            {
                IsAssignedToWork = true,
                IsWorking = false,
                IsFollowingPlayer = false,
                IsMoving = false,
                MoveTimeRemaining = 0f,
                MoveCooldownRemaining = GetRandomMoveDelaySeconds(),
                MoveTarget = idlePosition,
                Position = idlePosition
            };
        }
    }

    private int FindAssignedResourceBuildingIndex(int pokemonId)
    {
        for (int buildingIndex = 0; buildingIndex < _placedItems.Count; buildingIndex++)
        {
            PlacedItem building = _placedItems[buildingIndex];
            if (!building.Definition.IsResourceProduction || !HasWorker(building, pokemonId))
            {
                continue;
            }

            return buildingIndex;
        }

        return -1;
    }

    private bool IsAssignedBuildingFull(int pokemonId)
    {
        int assignedBuildingIndex = FindAssignedResourceBuildingIndex(pokemonId);
        if (assignedBuildingIndex < 0)
        {
            return false;
        }

        PlacedItem building = _placedItems[assignedBuildingIndex];
        return building.Definition.IsResourceProduction &&
               building.StoredProducedUnits >= building.Definition.MaxStoredProducedUnits;
    }

    private Vector2? TryPickRandomWanderTargetInHomeRange(SpawnedPokemon pokemon, int pokemonIndex)
    {
        Rectangle playableArea = new(
            BorderThickness,
            BorderThickness,
            _worldBounds.Width - (BorderThickness * 2),
            _worldBounds.Height - (BorderThickness * 2));

        Vector2 origin = pokemon.HomePosition ?? pokemon.Position;
        const int maxAttempts = 10;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float angle = Random.Shared.NextSingle() * MathHelper.TwoPi;
            float distance = Random.Shared.NextSingle() * HomeWanderRadius;
            Vector2 offset = new(MathF.Cos(angle) * distance, MathF.Sin(angle) * distance);
            Vector2 candidate = origin + offset;
            candidate = new Vector2(
                MathF.Round(candidate.X / SpawnedPokemonMoveDistance) * SpawnedPokemonMoveDistance,
                MathF.Round(candidate.Y / SpawnedPokemonMoveDistance) * SpawnedPokemonMoveDistance);

            Rectangle candidateBounds = new((int)candidate.X, (int)candidate.Y, PlayerSize, PlayerSize);
            if (!playableArea.Contains(candidateBounds))
            {
                continue;
            }

            if (pokemon.HomePosition is Vector2 homePosition &&
                Vector2.DistanceSquared(candidate, homePosition) > HomeWanderRadius * HomeWanderRadius)
            {
                continue;
            }

            if (CollidesWithPlacedItem(candidate) || CollidesWithSpawnedPokemon(candidateBounds, pokemonIndex))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private bool TryFindPathDirectionToTargetArea(
        Vector2 startPosition,
        Rectangle targetArea,
        int pokemonIndex,
        out Direction direction,
        bool ignoreDynamicActorCollisions = false)
    {
        direction = Direction.Down;
        if (targetArea.IsEmpty)
        {
            return false;
        }

        Rectangle startBounds = new((int)startPosition.X, (int)startPosition.Y, PlayerSize, PlayerSize);
        if (startBounds.Intersects(targetArea))
        {
            return false;
        }

        Rectangle playableArea = new(
            BorderThickness,
            BorderThickness,
            _worldBounds.Width - (BorderThickness * 2),
            _worldBounds.Height - (BorderThickness * 2));

        Point start = new((int)startPosition.X, (int)startPosition.Y);
        Queue<Point> frontier = new();
        Dictionary<long, (long ParentKey, Direction MoveDirection)> cameFrom = new();
        HashSet<long> visited = [];
        long startKey = EncodePoint(start);
        frontier.Enqueue(start);
        visited.Add(startKey);

        Point? found = null;
        const int maxVisitedNodes = 1800;
        Direction[] searchDirections = [Direction.Up, Direction.Right, Direction.Down, Direction.Left];

        while (frontier.Count > 0 && visited.Count <= maxVisitedNodes)
        {
            Point current = frontier.Dequeue();
            foreach (Direction stepDirection in searchDirections)
            {
                Vector2 movement = DirectionToMovement(stepDirection);
                Point next = new(
                    current.X + (int)(movement.X * SpawnedPokemonMoveDistance),
                    current.Y + (int)(movement.Y * SpawnedPokemonMoveDistance));
                long nextKey = EncodePoint(next);
                if (visited.Contains(nextKey))
                {
                    continue;
                }

                Rectangle nextBounds = new(next.X, next.Y, PlayerSize, PlayerSize);
                if (!playableArea.Contains(nextBounds))
                {
                    continue;
                }

                if (CollidesWithPlacedItem(new Vector2(next.X, next.Y)) ||
                    (!ignoreDynamicActorCollisions && CollidesWithSpawnedPokemon(nextBounds, pokemonIndex)))
                {
                    continue;
                }

                visited.Add(nextKey);
                cameFrom[nextKey] = (EncodePoint(current), stepDirection);
                if (nextBounds.Intersects(targetArea))
                {
                    found = next;
                    frontier.Clear();
                    break;
                }

                frontier.Enqueue(next);
            }
        }

        if (found is null)
        {
            return false;
        }

        long cursorKey = EncodePoint(found.Value);
        while (cameFrom.TryGetValue(cursorKey, out (long ParentKey, Direction MoveDirection) step))
        {
            if (step.ParentKey == startKey)
            {
                direction = step.MoveDirection;
                return true;
            }

            cursorKey = step.ParentKey;
        }

        return false;
    }

    private bool CanReachTargetAreaFromPosition(Vector2 startPosition, Rectangle targetArea, int pokemonIndex)
    {
        if (targetArea.IsEmpty)
        {
            return false;
        }

        Rectangle startBounds = new((int)startPosition.X, (int)startPosition.Y, PlayerSize, PlayerSize);
        if (startBounds.Intersects(targetArea))
        {
            return true;
        }

        return TryFindPathDirectionToTargetArea(
            startPosition,
            targetArea,
            pokemonIndex,
            out _,
            ignoreDynamicActorCollisions: true);
    }

    private static long EncodePoint(Point point)
    {
        return ((long)point.X << 32) | (uint)point.Y;
    }

    private bool TryCollectReadyWorkbenchItem(int workbenchIndex, out string? pickupMessage)
    {
        pickupMessage = null;
        if (workbenchIndex < 0 || workbenchIndex >= _placedItems.Count)
        {
            return false;
        }

        PlacedItem workbench = _placedItems[workbenchIndex];
        if (workbench.Definition != ItemCatalog.WorkBench ||
            workbench.WorkbenchQueuedItem is null ||
            !IsWorkbenchItemReady(workbench))
        {
            return false;
        }

        AddInventoryItem(workbench.WorkbenchQueuedItem, 1);
        _placedItems[workbenchIndex] = workbench with
        {
            WorkbenchQueuedItem = null,
            WorkbenchCraftEffortRemaining = 0f,
            WorkbenchCraftEffortRequired = 0f
        };

        _interactTarget = _placedItems[workbenchIndex];
        pickupMessage = $"PICKED UP {workbench.WorkbenchQueuedItem.Name.ToUpperInvariant()}";
        return true;
    }

    private bool TrySetWorkbenchWorker(int workbenchIndex, int? pokemonId, out string? message)
    {
        message = null;
        if (workbenchIndex < 0 || workbenchIndex >= _placedItems.Count)
        {
            return false;
        }

        PlacedItem workbench = _placedItems[workbenchIndex];
        if (workbench.Definition != ItemCatalog.WorkBench)
        {
            return false;
        }

        if (!pokemonId.HasValue)
        {
            _placedItems[workbenchIndex] = workbench with
            {
                WorkerPokemonId = null,
                WorkerPokemonName = null
            };
            message = "WORKBENCH WORKER CLEARED";
            return true;
        }

        int workerIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId.Value);
        if (workerIndex < 0)
        {
            return false;
        }

        SpawnedPokemon worker = _spawnedDittos[workerIndex];
        ClearExistingWorkBuildingForPokemon(worker.PokemonId);
        int existingWorkbenchIndex = _placedItems.FindIndex(item => item.Definition == ItemCatalog.WorkBench && item.WorkerPokemonId == worker.PokemonId);
        if (existingWorkbenchIndex >= 0)
        {
            _placedItems[existingWorkbenchIndex] = _placedItems[existingWorkbenchIndex] with
            {
                WorkerPokemonId = null,
                WorkerPokemonName = null
            };
        }

        _spawnedDittos[workerIndex] = worker with
        {
            IsFollowingPlayer = false
        };

        _placedItems[workbenchIndex] = workbench with
        {
            WorkerPokemonId = worker.PokemonId,
            WorkerPokemonName = worker.Name
        };
        message = $"{worker.Name.ToUpperInvariant()} ASSIGNED TO WORKBENCH";
        return true;
    }

    private static SpawnedPokemon UpdateUnclaimedPokemonIdleAnimation(SpawnedPokemon pokemon, float deltaTime)
    {
        if (pokemon.IdleCyclePauseRemaining > 0f)
        {
            return pokemon with
            {
                IdleCyclePauseRemaining = Math.Max(0f, pokemon.IdleCyclePauseRemaining - deltaTime),
                IdleAnimationTimer = 0f,
                IdleAnimationFrame = 0
            };
        }

        float idleTimer = pokemon.IdleAnimationTimer + deltaTime;
        int idleFrame = pokemon.IdleAnimationFrame;
        float pauseRemaining = 0f;
        while (idleTimer >= UnclaimedPokemonIdleFrameTime)
        {
            idleTimer -= UnclaimedPokemonIdleFrameTime;
            idleFrame++;
            if (idleFrame >= UnclaimedPokemonIdleFrameCount)
            {
                idleFrame = 0;
                idleTimer = 0f;
                pauseRemaining = UnclaimedPokemonIdleCyclePauseSeconds;
                break;
            }
        }

        return pokemon with
        {
            IdleAnimationTimer = idleTimer,
            IdleAnimationFrame = idleFrame,
            IdleCyclePauseRemaining = pauseRemaining
        };
    }

    private static float GetNextPokemonMoveDelaySeconds(SpawnedPokemon pokemon)
    {
        if (pokemon.IsFollowingPlayer)
        {
            return 0f;
        }

        if (pokemon.HomePosition is Vector2 homePosition &&
            Vector2.DistanceSquared(pokemon.Position, homePosition) > HomeWanderRadius * HomeWanderRadius)
        {
            return 0f;
        }

        return GetRandomMoveDelaySeconds();
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
        if (pokemon.IsFollowingPlayer)
        {
            return false;
        }

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

    private List<PokemonDialogueOption> GetBuildingTalkOptions(PlacedItem building)
    {
        return BuildingDialogueService.GetOptions(
            building,
            _spawnedDittos,
            CanAssignPokemonToResourceBuilding,
            IsBuildingExitWithinPokemonBedRange,
            IsWorkbenchWithinPokemonBedRange);
    }

    private void SetActiveTalkIcon(string pokemonName)
    {
        if (_talkState.IconName == pokemonName)
        {
            return;
        }

        _talkState.SetIcon(pokemonName, TryGetPokemonIconTexture(pokemonName, out Texture2D? iconTexture) ? iconTexture : null);
    }

    private bool TryGetPokemonIconTexture(string pokemonName, out Texture2D? iconTexture)
    {
        if (_pokemonIconTextures.TryGetValue(pokemonName, out iconTexture))
        {
            return iconTexture is not null;
        }

        if (_pokemonIconLoadAttempted.Contains(pokemonName))
        {
            iconTexture = null;
            return false;
        }

        _pokemonIconLoadAttempted.Add(pokemonName);
        string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Pokemon Icons", $"{pokemonName}Icon.png");
        if (!File.Exists(iconPath))
        {
            _pokemonIconTextures[pokemonName] = null;
            iconTexture = null;
            return false;
        }

        using FileStream iconStream = File.OpenRead(iconPath);
        iconTexture = Texture2D.FromStream(GraphicsDevice, iconStream);
        _pokemonIconTextures[pokemonName] = iconTexture;
        return true;
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

        if (_activeCraftingSource == CraftingSource.BasicWorkBenchCrafting)
        {
            if (_activeWorkbenchIndex < 0 || _activeWorkbenchIndex >= _placedItems.Count)
            {
                _interactionMessage = "NO WORK BENCH";
                _interactionMessageTimer = InteractionMessageDuration;
                return;
            }

            PlacedItem workbench = _placedItems[_activeWorkbenchIndex];
            if (workbench.Definition != ItemCatalog.WorkBench)
            {
                _interactionMessage = "NO WORK BENCH";
                _interactionMessageTimer = InteractionMessageDuration;
                return;
            }

            if (workbench.WorkbenchQueuedItem is not null)
            {
                _interactionMessage = IsWorkbenchItemReady(workbench)
                    ? "PICK UP ITEM FIRST"
                    : "WORKBENCH BUSY";
                _interactionMessageTimer = InteractionMessageDuration;
                return;
            }

            if (GetWorkbenchQueuedItemCount(workbench) >= GetWorkbenchQueueCapacity(workbench))
            {
                _interactionMessage = "WORKBENCH BUSY";
                _interactionMessageTimer = InteractionMessageDuration;
                return;
            }

            foreach (RecipeCost queuedCost in recipe.Costs)
            {
                RemoveInventoryItem(queuedCost.Item, queuedCost.Quantity);
            }

            float craftEffortRequired = Math.Max(0.1f, recipe.CraftEffortRequired);
            _placedItems[_activeWorkbenchIndex] = _placedItems[_activeWorkbenchIndex] with
            {
                WorkbenchQueuedItem = recipe.Output,
                WorkbenchCraftEffortRemaining = craftEffortRequired,
                WorkbenchCraftEffortRequired = craftEffortRequired
            };

            _interactTarget = _placedItems[_activeWorkbenchIndex];
            _interactionMessage = $"{recipe.Output.Name.ToUpperInvariant()} QUEUED";
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

    private void UpdatePlayerIdleAnimation(float deltaTime)
    {
        _playerIdleAnimationTimer += deltaTime;
        if (_playerIdleAnimationTimer < PlayerIdleFrameTime)
        {
            return;
        }

        _playerIdleAnimationTimer -= PlayerIdleFrameTime;
        _playerIdleAnimationFrame = (_playerIdleAnimationFrame + 1) % PlayerIdleFrameCount;
    }

    private SpriteFrame? GetCurrentPlayerFrame(
        Dictionary<string, SpriteFrame> frames,
        Direction direction,
        bool isWalking,
        int walkFrame,
        int idleFrame = 0)
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

        int frameIndex = action == "Walk" ? walkFrame : Math.Max(0, idleFrame);
        string key = $"Normal/{action}/Anim/{directionIndex}/{frameIndex:0000}";
        if (frames.TryGetValue(key, out SpriteFrame? frame))
        {
            return frame;
        }

        string fallbackKey = $"Normal/Idle/Anim/{directionIndex}/0000";
        return frames.TryGetValue(fallbackKey, out frame) ? frame : null;
    }

}
