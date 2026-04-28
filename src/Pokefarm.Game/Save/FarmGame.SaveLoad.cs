using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text;
using System.Text.Json;

namespace Pokefarm.Game;

public sealed partial class FarmGame
{
    private const string SaveFileExtension = ".json";

    private static readonly JsonSerializerOptions SaveJsonOptions = new()
    {
        WriteIndented = true
    };

    private string SaveDirectoryPath => Path.Combine(AppContext.BaseDirectory, "Saves");

    private void UpdateBootFlow(KeyboardState keyboard, bool confirmPressed, bool interactPressed, bool moveUpPressed, bool moveDownPressed)
    {
        bool enterPressed = keyboard.IsKeyDown(Keys.Enter) && !_previousKeyboard.IsKeyDown(Keys.Enter);
        bool deletePressed = keyboard.IsKeyDown(Keys.Delete) && !_previousKeyboard.IsKeyDown(Keys.Delete);

        if (_bootFlowState == BootFlowState.TitleScreen)
        {
            if (confirmPressed || interactPressed)
            {
                RefreshProfileList();
                _bootFlowState = BootFlowState.ProfileSelect;
            }

            return;
        }

        if (_bootFlowState == BootFlowState.ProfileSelect)
        {
            if (keyboard.IsKeyDown(Keys.N) && !_previousKeyboard.IsKeyDown(Keys.N))
            {
                _newProfileNameBuffer = string.Empty;
                _bootFlowState = BootFlowState.ProfileCreate;
                return;
            }

            if (deletePressed && _availableProfiles.Count > 0)
            {
                DeleteSelectedProfile();
                return;
            }

            if (_availableProfiles.Count > 0)
            {
                if (moveUpPressed)
                {
                    _selectedProfileIndex = Math.Max(0, _selectedProfileIndex - 1);
                }

                if (moveDownPressed)
                {
                    _selectedProfileIndex = Math.Min(_availableProfiles.Count - 1, _selectedProfileIndex + 1);
                }

                if (confirmPressed)
                {
                    LoadProfile(_availableProfiles[_selectedProfileIndex]);
                }
            }

            return;
        }

        if (_bootFlowState == BootFlowState.ProfileCreate)
        {
            if (keyboard.IsKeyDown(Keys.Escape) && !_previousKeyboard.IsKeyDown(Keys.Escape))
            {
                _bootFlowState = BootFlowState.ProfileSelect;
                return;
            }

            UpdateProfileNameTyping(keyboard);

            if (enterPressed)
            {
                string profileName = SanitizeProfileName(_newProfileNameBuffer);
                if (string.IsNullOrWhiteSpace(profileName))
                {
                    _interactionMessage = "ENTER A PROFILE NAME";
                    _interactionMessageTimer = InteractionMessageDuration;
                    return;
                }

                StartNewProfile(profileName);
            }
        }
    }

    private void DrawBootFlowScreen()
    {
        if (_spriteBatch is null || _pixel is null)
        {
            return;
        }

        Viewport viewport = GraphicsDevice.Viewport;
        Rectangle full = new(0, 0, viewport.Width, viewport.Height);

        if (_bootFlowState == BootFlowState.TitleScreen)
        {
            _spriteBatch.Draw(_pixel, full, new Color(24, 38, 26));
            string title = "POKEFARM";
            Point titleSize = MeasurePixelText(title, 10, 2);
            Vector2 titlePos = new((viewport.Width - titleSize.X) / 2f, (viewport.Height / 2f) - 120f);
            DrawPixelText(title, titlePos, new Color(236, 244, 186), 10, 2);
            DrawPixelText("PRESS SPACE OR E TO START", new Vector2((viewport.Width / 2f) - 180f, (viewport.Height / 2f) + 40f), new Color(220, 226, 202));
            return;
        }

        _spriteBatch.Draw(_pixel, full, new Color(16, 20, 28));
        Rectangle panel = new(viewport.Width / 2 - 400, viewport.Height / 2 - 260, 800, 520);
        _spriteBatch.Draw(_pixel, panel, new Color(34, 44, 58));
        DrawPanelBorder(panel, new Color(128, 164, 204));

        if (_bootFlowState == BootFlowState.ProfileSelect)
        {
            DrawPixelText("PROFILE SELECT", new Vector2(panel.X + 28, panel.Y + 26), new Color(236, 220, 196));
            DrawPixelText("SPACE LOAD PROFILE  N NEW PROFILE  DEL DELETE", new Vector2(panel.X + 28, panel.Y + 52), new Color(198, 210, 220));

            if (_availableProfiles.Count == 0)
            {
                DrawPixelText("NO SAVE FILES FOUND", new Vector2(panel.X + 28, panel.Y + 108), new Color(236, 200, 172));
                DrawPixelText("PRESS N TO CREATE YOUR FIRST PROFILE", new Vector2(panel.X + 28, panel.Y + 132), new Color(216, 224, 232));
                return;
            }

            for (int index = 0; index < _availableProfiles.Count; index++)
            {
                Rectangle row = new(panel.X + 24, panel.Y + 96 + (index * 46), panel.Width - 48, 38);
                bool selected = index == _selectedProfileIndex;
                _spriteBatch.Draw(_pixel, row, selected ? new Color(56, 80, 108) : new Color(42, 52, 68));
                DrawPanelBorder(row, selected ? Color.Gold : new Color(104, 134, 168));
                DrawPixelText(_availableProfiles[index].ToUpperInvariant(), new Vector2(row.X + 12, row.Y + 10), new Color(236, 220, 196));
            }

            return;
        }

        DrawPixelText("CREATE PROFILE", new Vector2(panel.X + 28, panel.Y + 26), new Color(236, 220, 196));
        DrawPixelText("TYPE NAME, ENTER TO CREATE, ESC TO CANCEL", new Vector2(panel.X + 28, panel.Y + 52), new Color(198, 210, 220));

        Rectangle inputBox = new(panel.X + 28, panel.Y + 100, panel.Width - 56, 52);
        _spriteBatch.Draw(_pixel, inputBox, new Color(22, 30, 42));
        DrawPanelBorder(inputBox, new Color(132, 176, 220));
        string shownName = string.IsNullOrEmpty(_newProfileNameBuffer) ? "_" : _newProfileNameBuffer;
        DrawPixelText(shownName.ToUpperInvariant(), new Vector2(inputBox.X + 12, inputBox.Y + 18), new Color(236, 220, 196));
    }

    private void UpdateProfileNameTyping(KeyboardState keyboard)
    {
        Keys[] pressedKeys = keyboard.GetPressedKeys();
        bool shiftDown = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        foreach (Keys key in pressedKeys)
        {
            if (_previousKeyboard.IsKeyDown(key))
            {
                continue;
            }

            if (key == Keys.Back)
            {
                if (_newProfileNameBuffer.Length > 0)
                {
                    _newProfileNameBuffer = _newProfileNameBuffer[..^1];
                }

                continue;
            }

            if (key == Keys.Space)
            {
                if (_newProfileNameBuffer.Length < 24)
                {
                    _newProfileNameBuffer += " ";
                }

                continue;
            }

            char? nextCharacter = ConvertKeyToCharacter(key, shiftDown);
            if (nextCharacter.HasValue && _newProfileNameBuffer.Length < 24)
            {
                _newProfileNameBuffer += nextCharacter.Value;
            }
        }
    }

    private static char? ConvertKeyToCharacter(Keys key, bool shiftDown)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            char character = (char)('a' + (key - Keys.A));
            return shiftDown ? char.ToUpperInvariant(character) : character;
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            return (char)('0' + (key - Keys.D0));
        }

        if (key == Keys.OemMinus)
        {
            return '-';
        }

        if (key == Keys.OemPeriod)
        {
            return '.';
        }

        if (key == Keys.OemComma)
        {
            return ',';
        }

        return null;
    }

    private void RefreshProfileList()
    {
        Directory.CreateDirectory(SaveDirectoryPath);
        _availableProfiles.Clear();

        foreach (string filePath in Directory.GetFiles(SaveDirectoryPath, $"*{SaveFileExtension}"))
        {
            string profileName = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrWhiteSpace(profileName))
            {
                _availableProfiles.Add(profileName);
            }
        }

        _availableProfiles.Sort(StringComparer.OrdinalIgnoreCase);
        _selectedProfileIndex = Math.Clamp(_selectedProfileIndex, 0, Math.Max(0, _availableProfiles.Count - 1));
    }

    private string GetProfileFilePath(string profileName)
    {
        string safeName = SanitizeProfileName(profileName);
        return Path.Combine(SaveDirectoryPath, $"{safeName}{SaveFileExtension}");
    }

    private static string SanitizeProfileName(string profileName)
    {
        StringBuilder builder = new();
        foreach (char character in profileName.Trim())
        {
            if (char.IsLetterOrDigit(character) || character == ' ' || character == '-' || character == '_')
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Trim();
    }

    private void StartNewProfile(string profileName)
    {
        string sanitizedName = SanitizeProfileName(profileName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return;
        }

        ResetWorldToNewGameDefaults();
        _activeProfileName = sanitizedName;
        SaveActiveProfile();
        _bootFlowState = BootFlowState.Playing;
    }

    private void DeleteSelectedProfile()
    {
        if (_selectedProfileIndex < 0 || _selectedProfileIndex >= _availableProfiles.Count)
        {
            return;
        }

        string profileName = _availableProfiles[_selectedProfileIndex];
        string saveFilePath = GetProfileFilePath(profileName);
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }

        RefreshProfileList();
        _interactionMessage = $"DELETED {profileName.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    private void SaveActiveProfile()
    {
        if (string.IsNullOrWhiteSpace(_activeProfileName))
        {
            return;
        }

        Directory.CreateDirectory(SaveDirectoryPath);
        GameSaveData saveData = BuildSaveData();
        string saveFilePath = GetProfileFilePath(_activeProfileName);
        string json = JsonSerializer.Serialize(saveData, SaveJsonOptions);
        File.WriteAllText(saveFilePath, json);

        RefreshProfileList();
        _interactionMessage = $"SAVED {_activeProfileName.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    private void LoadProfile(string profileName)
    {
        string saveFilePath = GetProfileFilePath(profileName);
        if (!File.Exists(saveFilePath))
        {
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        GameSaveData? saveData = JsonSerializer.Deserialize<GameSaveData>(json, SaveJsonOptions);
        if (saveData is null)
        {
            return;
        }

        ApplySaveData(saveData);
        _activeProfileName = profileName;
        _bootFlowState = BootFlowState.Playing;
        _interactionMessage = $"LOADED {profileName.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    private void ResetWorldToNewGameDefaults()
    {
        _placedItems.Clear();
        _spawnedDittos.Clear();
        _inventoryItems.Clear();
        _unlockedRecipes.Clear();
        _activeQuests.Clear();
        _availableDungeons.Clear();
        _storedPcPokemonNames.Clear();

        _inventoryItems.AddRange(
        [
            new InventoryEntry(ItemCatalog.Bed, 3),
            new InventoryEntry(ItemCatalog.WorkBench, 1),
            new InventoryEntry(ItemCatalog.Wood, 30),
            new InventoryEntry(ItemCatalog.Farm, 1),
            new InventoryEntry(ItemCatalog.BasicSnack, 12),
            new InventoryEntry(ItemCatalog.BasicSnack2, 11),
            new InventoryEntry(ItemCatalog.BasicSnack3, 2),
            new InventoryEntry(ItemCatalog.BasicSnack4, 1)
        ]);

        _unlockedRecipes.AddRange(
        [
            RecipeCatalog.NoBerryPlant,
            RecipeCatalog.WorkBench,
            RecipeCatalog.Chest,
            RecipeCatalog.Bed,
            RecipeCatalog.OranBerryPlant
        ]);

        _activeQuests.AddRange(
        [
            QuestCatalog.WelcomeHome,
            QuestCatalog.BuildYourFarm
        ]);

        _availableDungeons.Add(DungeonCatalog.MysteryGrove);

        Point pcSize = ItemCatalog.Pc.Size;
        Rectangle pcBounds = new(
            (_worldBounds.Width - pcSize.X) / 2,
            (_worldBounds.Height - pcSize.Y) / 2,
            pcSize.X,
            pcSize.Y);
        _placedItems.Add(new PlacedItem(pcBounds, ItemCatalog.Pc, 0d));

        Point portalSize = ItemCatalog.DungeonPortal.Size;
        Rectangle portalBounds = new(
            Math.Clamp(pcBounds.Right + 48, BorderThickness, _worldBounds.Width - BorderThickness - portalSize.X),
            Math.Clamp(pcBounds.Y, BorderThickness, _worldBounds.Height - BorderThickness - portalSize.Y),
            portalSize.X,
            portalSize.Y);
        _placedItems.Add(new PlacedItem(portalBounds, ItemCatalog.DungeonPortal, 0d));

        Point chestSize = ItemCatalog.Chest.Size;
        Rectangle chestBounds = new(
            Math.Clamp(pcBounds.X - chestSize.X - 36, BorderThickness, _worldBounds.Width - BorderThickness - chestSize.X),
            Math.Clamp(pcBounds.Y + pcBounds.Height - chestSize.Y, BorderThickness, _worldBounds.Height - BorderThickness - chestSize.Y),
            chestSize.X,
            chestSize.Y);
        _placedItems.Add(new PlacedItem(chestBounds, ItemCatalog.Chest, 0d, StoredItems: [new InventoryEntry(ItemCatalog.Wood, 10)]));

        _playerPosition = new Vector2(200f, 200f);
        _playerDirection = Direction.Down;
        _inputMode = InputMode.Gameplay;
        _selectedInventoryIndex = 0;
        _inventoryVisibleStartIndex = 0;
        _selectedCraftingIndex = 0;
        _selectedPcMenuIndex = 0;
        _selectedDungeonIndex = 0;
        _activeWorkbenchIndex = -1;
        _activeFarmIndex = -1;
        _activePcIndex = -1;
        _activeChestIndex = -1;
        _activeDungeonRoomIndex = -1;
        _activeDungeonPortalIndex = -1;
        _activeDungeonRun = null;
        _generatedDungeonPreview = null;
        _interactionMessage = null;
        _interactionMessageTimer = 0f;
        _talkState.Reset();
        _elapsedWorldTimeSeconds = 0d;
        _nextPokemonId = 1;
        _nextConstructionSiteId = 1;
        _isDittoWorking = false;
        _dittoWorkBuildingIndex = -1;
        _dittoWorkType = DittoWorkType.None;
    }

    private GameSaveData BuildSaveData()
    {
        return new GameSaveData
        {
            PlayerPositionX = _playerPosition.X,
            PlayerPositionY = _playerPosition.Y,
            PlayerDirection = _playerDirection,
            ElapsedWorldTimeSeconds = _elapsedWorldTimeSeconds,
            NextPokemonId = _nextPokemonId,
            NextConstructionSiteId = _nextConstructionSiteId,
            InventoryItems = _inventoryItems.Select(entry => new SavedInventoryEntry(entry.Definition.Name, entry.Quantity)).ToList(),
            PlacedItems = _placedItems.Select(ToSavedPlacedItem).ToList(),
            SpawnedPokemon = _spawnedDittos.Select(ToSavedPokemon).ToList(),
            StoredPcPokemonNames = [.. _storedPcPokemonNames],
            ActiveQuests = _activeQuests.Select(quest => quest.Name).ToList(),
            AvailableDungeons = _availableDungeons.Select(dungeon => dungeon.Name).ToList(),
            UnlockedRecipes = _unlockedRecipes.Select(recipe => recipe.Name).ToList()
        };
    }

    private static SavedSpawnedPokemon ToSavedPokemon(SpawnedPokemon pokemon)
    {
        return new SavedSpawnedPokemon
        {
            PokemonId = pokemon.PokemonId,
            Name = pokemon.Name,
            PositionX = pokemon.Position.X,
            PositionY = pokemon.Position.Y,
            Direction = pokemon.Direction,
            MoveCooldownRemaining = pokemon.MoveCooldownRemaining,
            SkillLevels = pokemon.SkillLevels?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [],
            IsAssignedToWork = pokemon.IsAssignedToWork,
            IsWorking = pokemon.IsWorking,
            IsMoving = pokemon.IsMoving,
            MoveTargetX = pokemon.MoveTarget.X,
            MoveTargetY = pokemon.MoveTarget.Y,
            MoveTimeRemaining = pokemon.MoveTimeRemaining,
            IsClaimed = pokemon.IsClaimed,
            IsFollowingPlayer = pokemon.IsFollowingPlayer,
            HomePositionX = pokemon.HomePosition?.X,
            HomePositionY = pokemon.HomePosition?.Y,
            SpeechText = pokemon.SpeechText,
            SpeechTimerRemaining = pokemon.SpeechTimerRemaining,
            ShowWorkBlockedMarker = pokemon.ShowWorkBlockedMarker,
            AssignedConstructionSiteId = pokemon.AssignedConstructionSiteId,
            WanderTargetX = pokemon.WanderTarget?.X,
            WanderTargetY = pokemon.WanderTarget?.Y,
            IdleAnimationTimer = pokemon.IdleAnimationTimer,
            IdleAnimationFrame = pokemon.IdleAnimationFrame,
            IdleCyclePauseRemaining = pokemon.IdleCyclePauseRemaining
        };
    }

    private static SavedPlacedItem ToSavedPlacedItem(PlacedItem item)
    {
        return new SavedPlacedItem
        {
            X = item.Bounds.X,
            Y = item.Bounds.Y,
            Width = item.Bounds.Width,
            Height = item.Bounds.Height,
            DefinitionName = item.Definition.Name,
            PlacedAtWorldTimeSeconds = item.PlacedAtWorldTimeSeconds,
            ResidentPokemonName = item.ResidentPokemonName,
            ResidentPokemonId = item.ResidentPokemonId,
            ResidentPokemonName2 = item.ResidentPokemonName2,
            ResidentPokemonId2 = item.ResidentPokemonId2,
            ResidentPokemonName3 = item.ResidentPokemonName3,
            ResidentPokemonId3 = item.ResidentPokemonId3,
            WorkerPokemonName = item.WorkerPokemonName,
            WorkerPokemonId = item.WorkerPokemonId,
            WorkerPokemonName2 = item.WorkerPokemonName2,
            WorkerPokemonId2 = item.WorkerPokemonId2,
            WorkerPokemonName3 = item.WorkerPokemonName3,
            WorkerPokemonId3 = item.WorkerPokemonId3,
            StoredProductionEffort = item.StoredProductionEffort,
            StoredProducedUnits = item.StoredProducedUnits,
            ProductionStepIndex = item.ProductionStepIndex,
            FarmGrowingPlantName = item.FarmGrowingPlant?.Name,
            WorkbenchQueuedItemName = item.WorkbenchQueuedItem?.Name,
            WorkbenchQueuedQuantity = item.WorkbenchQueuedQuantity,
            WorkbenchQueuedItemName2 = item.WorkbenchQueuedItem2?.Name,
            WorkbenchQueuedQuantity2 = item.WorkbenchQueuedQuantity2,
            WorkbenchQueuedItemName3 = item.WorkbenchQueuedItem3?.Name,
            WorkbenchQueuedQuantity3 = item.WorkbenchQueuedQuantity3,
            WorkbenchCraftEffortRemaining = item.WorkbenchCraftEffortRemaining,
            WorkbenchCraftEffortRequired = item.WorkbenchCraftEffortRequired,
            WorkbenchStoredItemName = item.WorkbenchStoredItem?.Name,
            WorkbenchStoredQuantity = item.WorkbenchStoredQuantity,
            StoredItems = item.StoredItems?.Select(entry => new SavedInventoryEntry(entry.Definition.Name, entry.Quantity)).ToList() ?? [],
            IsConstructionSite = item.IsConstructionSite,
            ConstructionSiteId = item.ConstructionSiteId,
            ConstructionEffort = item.ConstructionEffort
        };
    }

    private void ApplySaveData(GameSaveData data)
    {
        ResetWorldToNewGameDefaults();
        List<SavedInventoryEntry> inventoryEntries = data.InventoryItems ?? [];
        List<SavedPlacedItem> placedItems = data.PlacedItems ?? [];
        List<SavedSpawnedPokemon> spawnedPokemon = data.SpawnedPokemon ?? [];
        List<string> storedPcNames = data.StoredPcPokemonNames ?? [];
        List<string> activeQuestNames = data.ActiveQuests ?? [];
        List<string> availableDungeonNames = data.AvailableDungeons ?? [];
        List<string> unlockedRecipeNames = data.UnlockedRecipes ?? [];

        _playerPosition = new Vector2(data.PlayerPositionX, data.PlayerPositionY);
        _playerDirection = data.PlayerDirection;
        _elapsedWorldTimeSeconds = data.ElapsedWorldTimeSeconds;
        _nextPokemonId = Math.Max(1, data.NextPokemonId);
        _nextConstructionSiteId = Math.Max(1, data.NextConstructionSiteId);

        _inventoryItems.Clear();
        foreach (SavedInventoryEntry entry in inventoryEntries)
        {
            if (!TryResolveItem(entry.DefinitionName, out ItemDefinition definition))
            {
                continue;
            }

            _inventoryItems.Add(new InventoryEntry(definition, Math.Max(0, entry.Quantity)));
        }

        _placedItems.Clear();
        foreach (SavedPlacedItem savedItem in placedItems)
        {
            if (!TryResolveItem(savedItem.DefinitionName, out ItemDefinition definition))
            {
                continue;
            }

            List<InventoryEntry> storedEntries = [];
            foreach (SavedInventoryEntry storedEntry in savedItem.StoredItems)
            {
                if (TryResolveItem(storedEntry.DefinitionName, out ItemDefinition storedDefinition))
                {
                    storedEntries.Add(new InventoryEntry(storedDefinition, Math.Max(0, storedEntry.Quantity)));
                }
            }

            ItemDefinition? farmPlant = TryResolveItem(savedItem.FarmGrowingPlantName, out ItemDefinition resolvedFarmPlant) ? resolvedFarmPlant : null;
            ItemDefinition? queued1 = TryResolveItem(savedItem.WorkbenchQueuedItemName, out ItemDefinition resolvedQueued1) ? resolvedQueued1 : null;
            ItemDefinition? queued2 = TryResolveItem(savedItem.WorkbenchQueuedItemName2, out ItemDefinition resolvedQueued2) ? resolvedQueued2 : null;
            ItemDefinition? queued3 = TryResolveItem(savedItem.WorkbenchQueuedItemName3, out ItemDefinition resolvedQueued3) ? resolvedQueued3 : null;
            ItemDefinition? storedWorkbenchItem = TryResolveItem(savedItem.WorkbenchStoredItemName, out ItemDefinition resolvedStoredWorkbenchItem) ? resolvedStoredWorkbenchItem : null;

            _placedItems.Add(new PlacedItem(
                new Rectangle(savedItem.X, savedItem.Y, savedItem.Width, savedItem.Height),
                definition,
                savedItem.PlacedAtWorldTimeSeconds,
                savedItem.ResidentPokemonName,
                savedItem.ResidentPokemonId,
                savedItem.ResidentPokemonName2,
                savedItem.ResidentPokemonId2,
                savedItem.ResidentPokemonName3,
                savedItem.ResidentPokemonId3,
                savedItem.WorkerPokemonName,
                savedItem.WorkerPokemonId,
                savedItem.WorkerPokemonName2,
                savedItem.WorkerPokemonId2,
                savedItem.WorkerPokemonName3,
                savedItem.WorkerPokemonId3,
                savedItem.StoredProductionEffort,
                savedItem.StoredProducedUnits,
                savedItem.ProductionStepIndex,
                farmPlant,
                queued1,
                savedItem.WorkbenchQueuedQuantity,
                queued2,
                savedItem.WorkbenchQueuedQuantity2,
                queued3,
                savedItem.WorkbenchQueuedQuantity3,
                savedItem.WorkbenchCraftEffortRemaining,
                savedItem.WorkbenchCraftEffortRequired,
                storedWorkbenchItem,
                savedItem.WorkbenchStoredQuantity,
                storedEntries,
                savedItem.IsConstructionSite,
                savedItem.ConstructionSiteId,
                savedItem.ConstructionEffort));
        }

        _spawnedDittos.Clear();
        foreach (SavedSpawnedPokemon savedPokemon in spawnedPokemon)
        {
            Dictionary<SkillType, int> skills = [];
            foreach ((SkillType skill, int level) in savedPokemon.SkillLevels)
            {
                skills[skill] = Math.Max(0, level);
            }

            Vector2? homePosition = savedPokemon.HomePositionX.HasValue && savedPokemon.HomePositionY.HasValue
                ? new Vector2(savedPokemon.HomePositionX.Value, savedPokemon.HomePositionY.Value)
                : null;
            Vector2? wanderTarget = savedPokemon.WanderTargetX.HasValue && savedPokemon.WanderTargetY.HasValue
                ? new Vector2(savedPokemon.WanderTargetX.Value, savedPokemon.WanderTargetY.Value)
                : null;

            _spawnedDittos.Add(new SpawnedPokemon(
                savedPokemon.PokemonId,
                savedPokemon.Name,
                new Vector2(savedPokemon.PositionX, savedPokemon.PositionY),
                savedPokemon.Direction,
                savedPokemon.MoveCooldownRemaining,
                skills,
                savedPokemon.IsAssignedToWork,
                savedPokemon.IsWorking,
                savedPokemon.IsMoving,
                new Vector2(savedPokemon.MoveTargetX, savedPokemon.MoveTargetY),
                savedPokemon.MoveTimeRemaining,
                savedPokemon.IsClaimed,
                savedPokemon.IsFollowingPlayer,
                homePosition,
                savedPokemon.SpeechText,
                savedPokemon.SpeechTimerRemaining,
                savedPokemon.ShowWorkBlockedMarker,
                savedPokemon.AssignedConstructionSiteId,
                wanderTarget,
                savedPokemon.IdleAnimationTimer,
                savedPokemon.IdleAnimationFrame,
                savedPokemon.IdleCyclePauseRemaining));
        }

        _storedPcPokemonNames.Clear();
        _storedPcPokemonNames.AddRange(storedPcNames.Where(name => !string.IsNullOrWhiteSpace(name)));

        _activeQuests.Clear();
        foreach (string questName in activeQuestNames)
        {
            if (string.Equals(questName, QuestCatalog.WelcomeHome.Name, StringComparison.OrdinalIgnoreCase))
            {
                _activeQuests.Add(QuestCatalog.WelcomeHome);
            }
            else if (string.Equals(questName, QuestCatalog.BuildYourFarm.Name, StringComparison.OrdinalIgnoreCase))
            {
                _activeQuests.Add(QuestCatalog.BuildYourFarm);
            }
        }

        _availableDungeons.Clear();
        foreach (string dungeonName in availableDungeonNames)
        {
            if (string.Equals(dungeonName, DungeonCatalog.MysteryGrove.Name, StringComparison.OrdinalIgnoreCase))
            {
                _availableDungeons.Add(DungeonCatalog.MysteryGrove);
            }
        }

        if (_availableDungeons.Count == 0)
        {
            _availableDungeons.Add(DungeonCatalog.MysteryGrove);
        }

        _unlockedRecipes.Clear();
        foreach (string recipeName in unlockedRecipeNames)
        {
            if (string.Equals(recipeName, RecipeCatalog.WorkBench.Name, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedRecipes.Add(RecipeCatalog.WorkBench);
            }
            else if (string.Equals(recipeName, RecipeCatalog.Chest.Name, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedRecipes.Add(RecipeCatalog.Chest);
            }
            else if (string.Equals(recipeName, RecipeCatalog.Bed.Name, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedRecipes.Add(RecipeCatalog.Bed);
            }
            else if (string.Equals(recipeName, RecipeCatalog.OranBerryPlant.Name, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedRecipes.Add(RecipeCatalog.OranBerryPlant);
            }
            else if (string.Equals(recipeName, RecipeCatalog.NoBerryPlant.Name, StringComparison.OrdinalIgnoreCase))
            {
                _unlockedRecipes.Add(RecipeCatalog.NoBerryPlant);
            }
        }

        if (_activeQuests.Count == 0)
        {
            _activeQuests.Add(QuestCatalog.WelcomeHome);
            _activeQuests.Add(QuestCatalog.BuildYourFarm);
        }

        if (_unlockedRecipes.Count == 0)
        {
            _unlockedRecipes.Add(RecipeCatalog.NoBerryPlant);
            _unlockedRecipes.Add(RecipeCatalog.WorkBench);
            _unlockedRecipes.Add(RecipeCatalog.Chest);
            _unlockedRecipes.Add(RecipeCatalog.Bed);
            _unlockedRecipes.Add(RecipeCatalog.OranBerryPlant);
        }
    }

    private static bool TryResolveItem(string? definitionName, out ItemDefinition definition)
    {
        definition = null!;
        if (string.IsNullOrWhiteSpace(definitionName))
        {
            return false;
        }

        if (!ItemCatalog.TryGetByName(definitionName, out ItemDefinition resolved))
        {
            return false;
        }

        definition = resolved;
        return true;
    }
}

internal sealed class GameSaveData
{
    public float PlayerPositionX { get; set; }
    public float PlayerPositionY { get; set; }
    public Direction PlayerDirection { get; set; } = Direction.Down;
    public double ElapsedWorldTimeSeconds { get; set; }
    public int NextPokemonId { get; set; } = 1;
    public int NextConstructionSiteId { get; set; } = 1;
    public List<SavedInventoryEntry> InventoryItems { get; set; } = [];
    public List<SavedPlacedItem> PlacedItems { get; set; } = [];
    public List<SavedSpawnedPokemon> SpawnedPokemon { get; set; } = [];
    public List<string> StoredPcPokemonNames { get; set; } = [];
    public List<string> ActiveQuests { get; set; } = [];
    public List<string> AvailableDungeons { get; set; } = [];
    public List<string> UnlockedRecipes { get; set; } = [];
}

internal sealed class SavedInventoryEntry
{
    public SavedInventoryEntry()
    {
        DefinitionName = string.Empty;
    }

    public SavedInventoryEntry(string definitionName, int quantity)
    {
        DefinitionName = definitionName;
        Quantity = quantity;
    }

    public string DefinitionName { get; set; }
    public int Quantity { get; set; }
}

internal sealed class SavedPlacedItem
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string DefinitionName { get; set; } = string.Empty;
    public double PlacedAtWorldTimeSeconds { get; set; }
    public string? ResidentPokemonName { get; set; }
    public int? ResidentPokemonId { get; set; }
    public string? ResidentPokemonName2 { get; set; }
    public int? ResidentPokemonId2 { get; set; }
    public string? ResidentPokemonName3 { get; set; }
    public int? ResidentPokemonId3 { get; set; }
    public string? WorkerPokemonName { get; set; }
    public int? WorkerPokemonId { get; set; }
    public string? WorkerPokemonName2 { get; set; }
    public int? WorkerPokemonId2 { get; set; }
    public string? WorkerPokemonName3 { get; set; }
    public int? WorkerPokemonId3 { get; set; }
    public float StoredProductionEffort { get; set; }
    public int StoredProducedUnits { get; set; }
    public int ProductionStepIndex { get; set; }
    public string? FarmGrowingPlantName { get; set; }
    public string? WorkbenchQueuedItemName { get; set; }
    public int WorkbenchQueuedQuantity { get; set; }
    public string? WorkbenchQueuedItemName2 { get; set; }
    public int WorkbenchQueuedQuantity2 { get; set; }
    public string? WorkbenchQueuedItemName3 { get; set; }
    public int WorkbenchQueuedQuantity3 { get; set; }
    public float WorkbenchCraftEffortRemaining { get; set; }
    public float WorkbenchCraftEffortRequired { get; set; }
    public string? WorkbenchStoredItemName { get; set; }
    public int WorkbenchStoredQuantity { get; set; }
    public List<SavedInventoryEntry> StoredItems { get; set; } = [];
    public bool IsConstructionSite { get; set; }
    public int? ConstructionSiteId { get; set; }
    public float ConstructionEffort { get; set; }
}

internal sealed class SavedSpawnedPokemon
{
    public int PokemonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public Direction Direction { get; set; }
    public float MoveCooldownRemaining { get; set; }
    public Dictionary<SkillType, int> SkillLevels { get; set; } = [];
    public bool IsAssignedToWork { get; set; }
    public bool IsWorking { get; set; }
    public bool IsMoving { get; set; }
    public float MoveTargetX { get; set; }
    public float MoveTargetY { get; set; }
    public float MoveTimeRemaining { get; set; }
    public bool IsClaimed { get; set; }
    public bool IsFollowingPlayer { get; set; }
    public float? HomePositionX { get; set; }
    public float? HomePositionY { get; set; }
    public string? SpeechText { get; set; }
    public float SpeechTimerRemaining { get; set; }
    public bool ShowWorkBlockedMarker { get; set; }
    public int? AssignedConstructionSiteId { get; set; }
    public float? WanderTargetX { get; set; }
    public float? WanderTargetY { get; set; }
    public float IdleAnimationTimer { get; set; }
    public int IdleAnimationFrame { get; set; }
    public float IdleCyclePauseRemaining { get; set; }
}
