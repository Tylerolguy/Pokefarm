using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static Pokefarm.Game.BuildingWorkerHelpers;

namespace Pokefarm.Game;

public sealed partial class FarmGame
{
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

        if (_removeTarget.Definition.IsResourceProduction &&
            _removeTarget.Definition.ProducedMaterial is ItemDefinition producedMaterial &&
            _removeTarget.StoredProducedUnits > 0)
        {
            AddInventoryItem(producedMaterial, _removeTarget.StoredProducedUnits);
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
        _activeWorkbenchIndex = -1;
        if (craftingSource == CraftingSource.BasicWorkBenchCrafting && _interactTarget?.Definition == ItemCatalog.WorkBench)
        {
            _activeWorkbenchIndex = _placedItems.FindIndex(item => item == _interactTarget);
        }

        _inputMode = InputMode.Crafting;
        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        _selectedCraftingIndex = Math.Clamp(_selectedCraftingIndex, 0, Math.Max(0, activeRecipes.Count - 1));
    }

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
}
