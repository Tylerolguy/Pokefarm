using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static Pokefarm.Game.BuildingWorkerHelpers;

namespace Pokefarm.Game;

/// <summary>
/// Represents the FarmGame.
/// </summary>
public sealed partial class FarmGame
{
    /// <summary>
    /// Executes the Resolve Movement operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Try Place Selected Item operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Toggle Inventory Mode operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Begin Placement From Inventory operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Exit Placement Mode operation.
    /// </summary>
    private void ExitPlacementMode(InputMode nextMode)
    {
        _inputMode = nextMode;
        _previewItem = null;
        _previewPlacementValid = false;
    }

    /// <summary>
    /// Executes the Begin Removal Mode operation.
    /// </summary>
    private void BeginRemovalMode()
    {
        _inputMode = InputMode.Removal;
        _previewOffset = new Vector2(PlayerSize + 24f, 0f);
        UpdateRemovalPreview(Keyboard.GetState(), new GameTime(), false, false, false, false);
    }

    /// <summary>
    /// Executes the Exit Removal Mode operation.
    /// </summary>
    private void ExitRemovalMode(InputMode nextMode)
    {
        if (_inputMode == InputMode.Removal)
        {
            _inputMode = nextMode;
        }

        _removeTarget = null;
        _removeSelectorBounds = Rectangle.Empty;
    }

    /// <summary>
    /// Executes the Update Placement Preview operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Update Removal Preview operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Update Gameplay Movement operation.
    /// </summary>
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
            _playerIdleStationaryTimer += deltaTime;
            if (_playerIdleStationaryTimer >= PlayerIdleStartDelaySeconds)
            {
                UpdatePlayerIdleAnimation(deltaTime);
            }
            return;
        }

        _playerIdleStationaryTimer = 0f;
        _playerIdleAnimationTimer = 0f;
        _playerIdleAnimationFrame = 0;

        movement.Normalize();
        UpdatePlayerDirection(movement);
        UpdateWalkAnimation(deltaTime);
        Vector2 candidatePosition = _playerPosition + (movement * PlayerSpeed * deltaTime);
        _playerPosition = ResolveMovement(candidatePosition);
    }

    /// <summary>
    /// Executes the Update Inventory Navigation operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Can Place Item operation.
    /// </summary>
    private bool CanPlaceItem(PlacedItem candidateItem)
    {
        Rectangle playerBounds = new((int)_playerPosition.X, (int)_playerPosition.Y, PlayerSize, PlayerSize);
        Rectangle playableArea = new(
            BorderThickness,
            BorderThickness,
            _worldBounds.Width - (BorderThickness * 2),
            _worldBounds.Height - (BorderThickness * 2));

        if (!candidateItem.Definition.IsBuildingLike && candidateItem.Definition.Kind != ItemKind.Snack)
        {
            return false;
        }

        if (!playableArea.Contains(candidateItem.Bounds) || (candidateItem.Definition.HasCollision && candidateItem.Bounds.Intersects(playerBounds)))
        {
            return false;
        }

        Rectangle candidateExitBounds = GetResourceBuildingExitBounds(candidateItem);
        if (!candidateExitBounds.IsEmpty && !playableArea.Contains(candidateExitBounds))
        {
            return false;
        }

        foreach (PlacedItem item in _placedItems)
        {
            if (!item.Bounds.Intersects(candidateItem.Bounds))
            {
                Rectangle existingExitBounds = GetResourceBuildingExitBounds(item);
                if (candidateItem.Definition.IsBuildingLike && !existingExitBounds.IsEmpty && existingExitBounds.Intersects(candidateItem.Bounds))
                {
                    return false;
                }

                if (item.Definition.IsBuildingLike && !candidateExitBounds.IsEmpty && candidateExitBounds.Intersects(item.Bounds))
                {
                    return false;
                }

                if (!candidateExitBounds.IsEmpty && !existingExitBounds.IsEmpty && candidateExitBounds.Intersects(existingExitBounds))
                {
                    return false;
                }

                continue;
            }

            if (item.Definition.HasCollision || candidateItem.Definition.HasCollision)
            {
                return false;
            }

            Rectangle overlappingExitBounds = GetResourceBuildingExitBounds(item);
            if (!candidateExitBounds.IsEmpty && !overlappingExitBounds.IsEmpty && candidateExitBounds.Intersects(overlappingExitBounds))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Executes the Try Pick Up Selected Item operation.
    /// </summary>
    private void TryPickUpSelectedItem()
    {
        if (_inputMode != InputMode.Removal || _removeTarget is null || _inventoryItems.Count >= InventoryColumns * InventoryRows)
        {
            return;
        }

        foreach (int workerPokemonId in GetWorkerPokemonIds(_removeTarget))
        {
            int workerIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == workerPokemonId);
            if (workerIndex >= 0)
            {
                SpawnedPokemon worker = _spawnedDittos[workerIndex];
                Vector2 respawnPosition = GetWorkerRespawnPosition(_removeTarget);
                _spawnedDittos[workerIndex] = worker with
                {
                    IsAssignedToWork = false,
                    IsWorking = false,
                    IsFollowingPlayer = false,
                    IsMoving = false,
                    MoveTimeRemaining = 0f,
                    MoveCooldownRemaining = GetRandomMoveDelaySeconds(),
                    MoveTarget = respawnPosition,
                    Position = respawnPosition
                };
            }
        }

        ItemDefinition? producedMaterial = GetProducedMaterialForBuilding(_removeTarget);
        if (_removeTarget.Definition.IsResourceProduction &&
            producedMaterial is not null &&
            _removeTarget.StoredProducedUnits > 0)
        {
            AddInventoryItem(producedMaterial, _removeTarget.StoredProducedUnits);
        }

        _placedItems.Remove(_removeTarget);
        AddInventoryItem(_removeTarget.Definition, 1);
        _removeTarget = null;
        UpdateRemovalPreview(Keyboard.GetState(), new GameTime(), false, false, false, false);
    }

    /// <summary>
    /// Executes the Try Interact With Building operation.
    /// </summary>
    private void TryInteractWithBuilding()
    {
        if (_interactTarget is null)
        {
            OpenCrafting(CraftingSource.HandheldCrafting);
            return;
        }

        OpenBuildingTalk(_interactTarget);
    }

    /// <summary>
    /// Executes the Try Talk With Pokemon operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Open Crafting operation.
    /// </summary>
    private void OpenCrafting(CraftingSource craftingSource)
    {
        _activeCraftingSource = craftingSource;
        _activeWorkbenchIndex = -1;
        _activeFarmIndex = -1;
        if (craftingSource == CraftingSource.BasicWorkBenchCrafting && _interactTarget?.Definition == ItemCatalog.WorkBench)
        {
            _activeWorkbenchIndex = _placedItems.FindIndex(item => item == _interactTarget);
        }
        else if (craftingSource == CraftingSource.FarmGrowing && _interactTarget?.Definition == ItemCatalog.Farm)
        {
            _activeFarmIndex = _placedItems.FindIndex(item => item == _interactTarget);
        }

        _inputMode = InputMode.Crafting;
        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        _selectedCraftingIndex = Math.Clamp(_selectedCraftingIndex, 0, Math.Max(0, activeRecipes.Count - 1));
    }

    /// <summary>
    /// Executes the Find Interactable Target operation.
    /// </summary>
    private PlacedItem? FindInteractableTarget()
    {
        Rectangle searchArea = GetFacingInteractionArea();

        foreach (PlacedItem item in _placedItems)
        {
            if (!item.Definition.IsBuildingLike && item.Definition != ItemCatalog.Bed)
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

    /// <summary>
    /// Executes the Open Building Talk operation.
    /// </summary>
    private void OpenBuildingTalk(PlacedItem building)
    {
        FaceConversationTarget(new Vector2(building.Bounds.Center.X, building.Bounds.Center.Y));
        _talkState.BeginBuildingTalk(
            building,
            BuildingDialogueService.GetOpeningText(building),
            GetBuildingTalkOptions(building),
            "DITTO");
        _talkExitTimer = 0f;
        SetActiveTalkIcon("Ditto");
        _inputMode = InputMode.Talking;
    }

    /// <summary>
    /// Executes the Find Nearby Pokemon Target Index operation.
    /// </summary>
    private int FindNearbyPokemonTargetIndex()
    {
        Rectangle searchArea = GetFacingInteractionArea();

        for (int index = 0; index < _spawnedDittos.Count; index++)
        {
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

            if (pokemonBounds.Intersects(searchArea))
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>
    /// Executes the Get Facing Interaction Area operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Update Talk Navigation operation.
    /// </summary>
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

    /// <summary>
    /// Executes the Confirm Talk Option operation.
    /// </summary>
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

        if (selectedOption.Action == PokemonDialogueAction.AssignResourceWork && selectedOption.TargetPokemonId.HasValue)
        {
            AssignPokemonToResourceBuilding(selectedOption.TargetPokemonId.Value);
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

        if (selectedOption.Action == PokemonDialogueAction.UnassignResourceWork && selectedOption.TargetPokemonId.HasValue)
        {
            UnassignPokemonFromActiveResourceBuilding(selectedOption.TargetPokemonId.Value);
            if (selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
            }
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.CollectProduction)
        {
            CollectProducedMaterialsFromActiveBuilding();
            if (selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
            }
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.AssignWorkbenchWorker && selectedOption.TargetPokemonId.HasValue)
        {
            AssignPokemonToActiveWorkbench(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.UnassignWorkbenchWorker && selectedOption.TargetPokemonId.HasValue)
        {
            UnassignPokemonFromActiveWorkbench(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.OpenWorkbenchQueue)
        {
            if (_talkState.ActiveBuilding is not null)
            {
                _interactTarget = _talkState.ActiveBuilding;
            }

            OpenCrafting(CraftingSource.BasicWorkBenchCrafting);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.OpenFarmGrowingMenu)
        {
            if (_talkState.ActiveBuilding is not null)
            {
                _interactTarget = _talkState.ActiveBuilding;
            }

            OpenCrafting(CraftingSource.FarmGrowing);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.OpenPcQuests)
        {
            OpenPcMenu(PcMenuScreen.Quests);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.OpenPcStorage)
        {
            OpenPcMenu(PcMenuScreen.Storage);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.OpenPcLevel)
        {
            OpenPcMenu(PcMenuScreen.Level);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.OpenDungeonMenu)
        {
            OpenDungeonMenu();
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.StorePokemonInPc && selectedOption.TargetPokemonId.HasValue)
        {
            StoreFollowingPokemonInPc(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.DequeueWorkbenchItem)
        {
            DequeueActiveWorkbenchItem();
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.CollectWorkbenchItem)
        {
            CollectReadyWorkbenchItemFromTalk();
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

    /// <summary>
    /// Executes the Begin Talk Exit Countdown operation.
    /// </summary>
    private void BeginTalkExitCountdown()
    {
        _talkExitTimer = TalkExitDelaySeconds;
    }

    /// <summary>
    /// Executes the Exit Talk Mode operation.
    /// </summary>
    private void ExitTalkMode()
    {
        _inputMode = InputMode.Gameplay;
        _talkExitTimer = 0f;
        _talkState.Reset();
    }

    /// <summary>
    /// Executes the Update Crafting Navigation operation.
    /// </summary>
    private void UpdateCraftingNavigation(bool moveUp, bool moveDown, bool moveLeft, bool moveRight)
    {
        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        if (activeRecipes.Count == 0)
        {
            _selectedCraftingIndex = 0;
        }
        else
        {
            if (moveUp)
            {
                _selectedCraftingIndex = Math.Max(0, _selectedCraftingIndex - 1);
            }

            if (moveDown)
            {
                _selectedCraftingIndex = Math.Min(activeRecipes.Count - 1, _selectedCraftingIndex + 1);
            }
        }

        _ = moveLeft;
        _ = moveRight;
    }

    /// <summary>
    /// Executes the Open Pc Menu operation.
    /// </summary>
    private void OpenPcMenu(PcMenuScreen screen)
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.Pc)
        {
            return;
        }

        _activePcIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (_activePcIndex < 0)
        {
            return;
        }

        _activePcMenuScreen = screen;
        _selectedPcMenuIndex = 0;
        _inputMode = InputMode.PcMenu;
        _talkExitTimer = 0f;
    }

    /// <summary>
    /// Executes the Close Pc Menu operation.
    /// </summary>
    private void ClosePcMenu()
    {
        _inputMode = InputMode.Gameplay;
        _selectedPcMenuIndex = 0;
        _activePcIndex = -1;
    }

    /// <summary>
    /// Executes the Update Pc Menu Navigation operation.
    /// </summary>
    private void UpdatePcMenuNavigation(bool moveUp, bool moveDown)
    {
        int count = GetPcMenuEntryCount();
        if (count <= 0)
        {
            _selectedPcMenuIndex = 0;
            return;
        }

        if (moveUp)
        {
            _selectedPcMenuIndex = Math.Max(0, _selectedPcMenuIndex - 1);
        }

        if (moveDown)
        {
            _selectedPcMenuIndex = Math.Min(count - 1, _selectedPcMenuIndex + 1);
        }
    }

    /// <summary>
    /// Executes the Confirm Pc Menu Option operation.
    /// </summary>
    private void ConfirmPcMenuOption()
    {
        if (_inputMode != InputMode.PcMenu || _activePcMenuScreen != PcMenuScreen.Storage)
        {
            return;
        }

        if (_storedPcPokemonNames.Count == 0 ||
            _selectedPcMenuIndex < 0 ||
            _selectedPcMenuIndex >= _storedPcPokemonNames.Count)
        {
            return;
        }

        if (_activePcIndex < 0 || _activePcIndex >= _placedItems.Count || _placedItems[_activePcIndex].Definition != ItemCatalog.Pc)
        {
            _interactionMessage = "PC NOT AVAILABLE";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        string pokemonName = _storedPcPokemonNames[_selectedPcMenuIndex];
        if (!TryFindNearbyOpenPokemonSpawnPosition(_placedItems[_activePcIndex].Bounds, out Vector2 spawnPosition))
        {
            _interactionMessage = "NO VALID SPOT AVAILABLE";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        SpawnedPokemonDefinition spawnDefinition = SpawnedPokemonCatalog.GetOrDefault(pokemonName);
        _spawnedDittos.Add(new SpawnedPokemon(
            _nextPokemonId++,
            spawnDefinition.Name,
            spawnPosition,
            Direction.Down,
            GetRandomMoveDelaySeconds(),
            spawnDefinition.SkillLevels));
        _storedPcPokemonNames.RemoveAt(_selectedPcMenuIndex);
        _selectedPcMenuIndex = Math.Clamp(_selectedPcMenuIndex, 0, Math.Max(0, _storedPcPokemonNames.Count - 1));
        _interactionMessage = $"{spawnDefinition.Name.ToUpperInvariant()} RELEASED";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    /// <summary>
    /// Executes the Get Pc Menu Entry Count operation.
    /// </summary>
    private int GetPcMenuEntryCount()
    {
        return _activePcMenuScreen switch
        {
            PcMenuScreen.Quests => _activeQuests.Count,
            PcMenuScreen.Storage => _storedPcPokemonNames.Count,
            _ => 0
        };
    }

    /// <summary>
    /// Executes the Store Following Pokemon In Pc operation.
    /// </summary>
    private void StoreFollowingPokemonInPc(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.Pc)
        {
            return;
        }

        int pokemonIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (pokemonIndex < 0)
        {
            _talkState.SetText("POKEMON NOT FOUND");
            return;
        }

        SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
        if (!pokemon.IsFollowingPlayer)
        {
            _talkState.SetText("NOT FOLLOWING");
            return;
        }

        _storedPcPokemonNames.Add(pokemon.Name);
        _spawnedDittos.RemoveAt(pokemonIndex);

        if (_talkTargetIndex >= _spawnedDittos.Count)
        {
            _talkTargetIndex = -1;
        }

        _talkState.SetText($"{pokemon.Name.ToUpperInvariant()} STORED");
        if (_talkState.ActiveBuilding is not null)
        {
            _talkState.SetOptions(GetBuildingTalkOptions(_talkState.ActiveBuilding));
        }
        _interactionMessage = $"{pokemon.Name.ToUpperInvariant()} STORED IN PC";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    /// <summary>
    /// Executes the Try Find Nearby Open Pokemon Spawn Position operation.
    /// </summary>
    private bool TryFindNearbyOpenPokemonSpawnPosition(Rectangle originBounds, out Vector2 spawnPosition)
    {
        Rectangle playableArea = new(
            BorderThickness,
            BorderThickness,
            _worldBounds.Width - (BorderThickness * 2),
            _worldBounds.Height - (BorderThickness * 2));

        Point originCenter = originBounds.Center;
        int[] radii = [0, PlayerSize, PlayerSize * 2, PlayerSize * 3, PlayerSize * 4];
        foreach (int radius in radii)
        {
            foreach (Point offset in GetSpawnOffsets(radius))
            {
                Vector2 candidate = new(
                    originCenter.X - (PlayerSize / 2f) + offset.X,
                    originCenter.Y - (PlayerSize / 2f) + offset.Y);
                Rectangle candidateBounds = new((int)candidate.X, (int)candidate.Y, PlayerSize, PlayerSize);

                if (!playableArea.Contains(candidateBounds))
                {
                    continue;
                }

                if (CollidesWithPlacedItem(candidate) || CollidesWithSpawnedPokemon(candidateBounds, -1))
                {
                    continue;
                }

                spawnPosition = candidate;
                return true;
            }
        }

        spawnPosition = Vector2.Zero;
        return false;
    }

    /// <summary>
    /// Executes the Get Spawn Offsets operation.
    /// </summary>
    private static IEnumerable<Point> GetSpawnOffsets(int radius)
    {
        if (radius == 0)
        {
            yield return Point.Zero;
            yield break;
        }

        yield return new Point(radius, 0);
        yield return new Point(-radius, 0);
        yield return new Point(0, radius);
        yield return new Point(0, -radius);
        yield return new Point(radius, radius);
        yield return new Point(radius, -radius);
        yield return new Point(-radius, radius);
        yield return new Point(-radius, -radius);
    }

    /// <summary>
    /// Executes the Open Dungeon Menu operation.
    /// </summary>
    private void OpenDungeonMenu()
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.DungeonPortal)
        {
            return;
        }

        _activeDungeonPortalIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        _inputMode = InputMode.DungeonMenu;
        _selectedDungeonIndex = Math.Clamp(_selectedDungeonIndex, 0, Math.Max(0, _availableDungeons.Count - 1));
        _talkExitTimer = 0f;
    }

    /// <summary>
    /// Executes the Close Dungeon Menu operation.
    /// </summary>
    private void CloseDungeonMenu()
    {
        _inputMode = InputMode.Gameplay;
        if (_activeDungeonRun is null)
        {
            _activeDungeonPortalIndex = -1;
        }
    }

    /// <summary>
    /// Executes the Update Dungeon Menu Navigation operation.
    /// </summary>
    private void UpdateDungeonMenuNavigation(bool moveUp, bool moveDown)
    {
        if (_availableDungeons.Count == 0)
        {
            _selectedDungeonIndex = 0;
            return;
        }

        if (moveUp)
        {
            _selectedDungeonIndex = Math.Max(0, _selectedDungeonIndex - 1);
        }

        if (moveDown)
        {
            _selectedDungeonIndex = Math.Min(_availableDungeons.Count - 1, _selectedDungeonIndex + 1);
        }
    }

    /// <summary>
    /// Executes the Confirm Dungeon Menu Selection operation.
    /// </summary>
    private void ConfirmDungeonMenuSelection()
    {
        if (_inputMode != InputMode.DungeonMenu ||
            _selectedDungeonIndex < 0 ||
            _selectedDungeonIndex >= _availableDungeons.Count)
        {
            return;
        }

        DungeonDefinition dungeonDefinition = _availableDungeons[_selectedDungeonIndex];
        GeneratedDungeon generatedDungeon = DungeonGenerator.Generate(dungeonDefinition);
        _generatedDungeonPreview = generatedDungeon;
        EnterDungeonRun(generatedDungeon);
        _interactionMessage = $"ENTERED {dungeonDefinition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    /// <summary>
    /// Executes the Enter Dungeon Run operation.
    /// </summary>
    private void EnterDungeonRun(GeneratedDungeon dungeonRun)
    {
        _activeDungeonRun = dungeonRun;
        _activeDungeonRoomIndex = 0;
        _inputMode = InputMode.Gameplay;
        _interactTarget = null;
        _talkTargetIndex = -1;
        _playerPosition = new Vector2(
            _worldBounds.Center.X - (PlayerSize / 2f),
            _worldBounds.Center.Y - (PlayerSize / 2f));
    }

    /// <summary>
    /// Executes the Advance Dungeon Room Or Exit operation.
    /// </summary>
    private void AdvanceDungeonRoomOrExit()
    {
        if (_activeDungeonRun is null)
        {
            return;
        }

        int nextRoomIndex = _activeDungeonRoomIndex + 1;
        if (nextRoomIndex >= _activeDungeonRun.Rooms.Count)
        {
            LeaveActiveDungeonRun("LEFT");
            return;
        }

        _activeDungeonRoomIndex = nextRoomIndex;
        GeneratedDungeonRoom room = _activeDungeonRun.Rooms[_activeDungeonRoomIndex];
        _interactionMessage = $"ROOM {_activeDungeonRoomIndex + 1}: {room.Definition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    /// <summary>
    /// Executes the Leave Active Dungeon Run operation.
    /// </summary>
    private void LeaveActiveDungeonRun(string messagePrefix)
    {
        if (_activeDungeonRun is null)
        {
            return;
        }

        string dungeonName = _activeDungeonRun.DungeonName;
        _activeDungeonRun = null;
        _activeDungeonRoomIndex = -1;
        _inputMode = InputMode.Gameplay;
        _interactTarget = null;
        _talkTargetIndex = -1;
        _playerPosition = GetDungeonPortalExitSpawnPosition();
        _interactionMessage = $"{messagePrefix} {dungeonName.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
        _activeDungeonPortalIndex = -1;
    }

    /// <summary>
    /// Executes the Get Dungeon Portal Exit Spawn Position operation.
    /// </summary>
    private Vector2 GetDungeonPortalExitSpawnPosition()
    {
        PlacedItem? portal = null;
        if (_activeDungeonPortalIndex >= 0 &&
            _activeDungeonPortalIndex < _placedItems.Count &&
            _placedItems[_activeDungeonPortalIndex].Definition == ItemCatalog.DungeonPortal)
        {
            portal = _placedItems[_activeDungeonPortalIndex];
        }
        else
        {
            portal = _placedItems.FirstOrDefault(item => item.Definition == ItemCatalog.DungeonPortal);
        }

        if (portal is null)
        {
            return new Vector2(
                _worldBounds.Center.X - (PlayerSize / 2f),
                _worldBounds.Center.Y - (PlayerSize / 2f));
        }

        Rectangle exitBounds = GetResourceBuildingExitBounds(portal);
        if (!exitBounds.IsEmpty)
        {
            return new Vector2(exitBounds.X, exitBounds.Y);
        }

        return new Vector2(
            portal.Bounds.Center.X - (PlayerSize / 2f),
            portal.Bounds.Bottom + 4f);
    }
}
