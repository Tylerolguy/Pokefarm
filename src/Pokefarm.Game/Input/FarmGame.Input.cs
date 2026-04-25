using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static Pokefarm.Game.BuildingWorkerHelpers;

namespace Pokefarm.Game;

// Main runtime type for farm Game, coordinating state and side effects for this feature.
public sealed partial class FarmGame
{
    // Resolves movement from current input and gameplay context.
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

    // Attempts to place Selected Item and reports success so callers can handle failure without exceptions.
    private void TryPlaceSelectedItem(bool skipConstructionSite = false)
    {
        if (_inputMode != InputMode.Placement || !_previewPlacementValid || _previewItem is null || _inventoryItems.Count == 0)
        {
            return;
        }

        PlacedItem placedItem = _previewItem;
        if (placedItem.Definition.IsBuildingLike && !skipConstructionSite)
        {
            placedItem = CreateConstructionSite(placedItem);
        }
        else if (placedItem.Definition.IsBuildingLike && skipConstructionSite)
        {
            // DEV MODE: pressing Enter while placing skips construction and places the finished building directly.
            placedItem = placedItem with
            {
                IsConstructionSite = false,
                ConstructionSiteId = null,
                ConstructionEffort = 0f
            };
        }

        _placedItems.Add(placedItem);
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

        EnsureInventorySelectionVisible();
    }

    // Converts a placed building preview into a construction site shell that workers can complete over time.
    private PlacedItem CreateConstructionSite(PlacedItem placedBuilding)
    {
        return placedBuilding with
        {
            IsConstructionSite = true,
            ConstructionSiteId = _nextConstructionSiteId++,
            ConstructionEffort = 0f
        };
    }

    // Switches inventory Mode between modes and applies associated state resets.
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
        EnsureInventorySelectionVisible();
    }

    // Enters placement From Inventory flow and initializes transient interaction state.
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

    // Leaves placement Mode flow and restores default interaction state.
    private void ExitPlacementMode(InputMode nextMode)
    {
        _inputMode = nextMode;
        _previewItem = null;
        _previewPlacementValid = false;
    }

    // Enters removal Mode flow and initializes transient interaction state.
    private void BeginRemovalMode()
    {
        _inputMode = InputMode.Removal;
        _previewOffset = new Vector2(PlayerSize + 24f, 0f);
        UpdateRemovalPreview(Keyboard.GetState(), new GameTime(), false, false, false, false);
    }

    // Leaves removal Mode flow and restores default interaction state.
    private void ExitRemovalMode(InputMode nextMode)
    {
        if (_inputMode == InputMode.Removal)
        {
            _inputMode = nextMode;
        }

        _removeTarget = null;
        _removeSelectorBounds = Rectangle.Empty;
    }

    // Ticks placement Preview each frame and keeps related timers and state synchronized.
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

    // Ticks removal Preview each frame and keeps related timers and state synchronized.
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

    // Ticks gameplay Movement each frame and keeps related timers and state synchronized.
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

    // Ticks inventory Navigation each frame and keeps related timers and state synchronized.
    private void UpdateInventoryNavigation(bool moveLeft, bool moveRight, bool moveUp, bool moveDown)
    {
        if (_inventoryItems.Count == 0)
        {
            _selectedInventoryIndex = 0;
            _inventoryVisibleStartIndex = 0;
            return;
        }

        EnsureInventorySelectionVisible();

        int visibleSlots = InventoryColumns * InventoryRows;
        int currentLocalIndex = _selectedInventoryIndex - _inventoryVisibleStartIndex;
        int currentColumn = Math.Clamp(currentLocalIndex % InventoryColumns, 0, InventoryColumns - 1);
        int currentRow = Math.Clamp(currentLocalIndex / InventoryColumns, 0, InventoryRows - 1);

        if (moveLeft && currentColumn > 0)
        {
            int candidate = _selectedInventoryIndex - 1;
            if (candidate >= _inventoryVisibleStartIndex)
            {
                _selectedInventoryIndex = candidate;
            }
        }

        if (moveRight && currentColumn < InventoryColumns - 1)
        {
            int candidate = _selectedInventoryIndex + 1;
            if (candidate < _inventoryItems.Count && candidate < _inventoryVisibleStartIndex + visibleSlots)
            {
                _selectedInventoryIndex = candidate;
            }
        }

        if (moveUp)
        {
            int candidate = _selectedInventoryIndex - InventoryColumns;
            if (candidate >= _inventoryVisibleStartIndex)
            {
                _selectedInventoryIndex = candidate;
            }
            else if (_inventoryVisibleStartIndex > 0)
            {
                _inventoryVisibleStartIndex = Math.Max(0, _inventoryVisibleStartIndex - InventoryColumns);
                _selectedInventoryIndex = Math.Max(0, _selectedInventoryIndex - InventoryColumns);
            }
        }

        if (moveDown)
        {
            int candidate = _selectedInventoryIndex + InventoryColumns;
            if (candidate < _inventoryItems.Count && candidate < _inventoryVisibleStartIndex + visibleSlots)
            {
                _selectedInventoryIndex = candidate;
            }
            else if (_inventoryVisibleStartIndex + visibleSlots < _inventoryItems.Count)
            {
                _inventoryVisibleStartIndex = Math.Min(
                    Math.Max(0, _inventoryItems.Count - visibleSlots),
                    _inventoryVisibleStartIndex + InventoryColumns);
                _selectedInventoryIndex = Math.Min(_inventoryItems.Count - 1, _selectedInventoryIndex + InventoryColumns);
            }
        }

        EnsureInventorySelectionVisible();
    }

    // Checks whether place Item is currently true for the active world state.
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

        if (candidateItem.Definition.IsBuildingLike)
        {
            foreach (SpawnedPokemon pokemon in _spawnedDittos)
            {
                if (pokemon.IsWorking)
                {
                    continue;
                }

                Rectangle pokemonBounds = new(
                    (int)pokemon.Position.X,
                    (int)pokemon.Position.Y,
                    PlayerSize,
                    PlayerSize);

                if (candidateItem.Bounds.Intersects(pokemonBounds))
                {
                    return false;
                }

                if (!candidateExitBounds.IsEmpty && candidateExitBounds.Intersects(pokemonBounds))
                {
                    return false;
                }
            }
        }

        return true;
    }

    // Attempts to pick Up Selected Item and reports success so callers can handle failure without exceptions.
    private void TryPickUpSelectedItem()
    {
        if (_inputMode != InputMode.Removal || _removeTarget is null)
        {
            return;
        }

        ItemDefinition? producedMaterial = GetProducedMaterialForBuilding(_removeTarget);
        if (_removeTarget.Definition.IsResourceProduction &&
            producedMaterial is not null &&
            _removeTarget.StoredProducedUnits > 0 &&
            !CanAddInventoryItem(producedMaterial))
        {
            _interactionMessage = "INVENTORY FULL";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        if (!CanAddInventoryItem(_removeTarget.Definition))
        {
            _interactionMessage = "INVENTORY FULL";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        if (_removeTarget.Definition == ItemCatalog.Bed)
        {
            foreach (int residentPokemonId in GetBedResidentPokemonIds(_removeTarget))
            {
                UnclaimPokemon(residentPokemonId);
            }
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

        if (_removeTarget.IsConstructionSite && _removeTarget.ConstructionSiteId.HasValue)
        {
            int constructionSiteId = _removeTarget.ConstructionSiteId.Value;
            for (int pokemonIndex = 0; pokemonIndex < _spawnedDittos.Count; pokemonIndex++)
            {
                SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
                if (pokemon.AssignedConstructionSiteId != constructionSiteId)
                {
                    continue;
                }

                _spawnedDittos[pokemonIndex] = pokemon with
                {
                    AssignedConstructionSiteId = null,
                    IsAssignedToWork = false,
                    IsWorking = false,
                    IsFollowingPlayer = false,
                    IsMoving = false,
                    MoveTimeRemaining = 0f,
                    MoveCooldownRemaining = GetRandomMoveDelaySeconds(),
                    MoveTarget = pokemon.Position
                };
            }
        }

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

    // Attempts to interact With Building and reports success so callers can handle failure without exceptions.
    private void TryInteractWithBuilding()
    {
        if (_interactTarget is null)
        {
            OpenCrafting(CraftingSource.HandheldCrafting);
            return;
        }

        OpenBuildingTalk(_interactTarget);
    }

    // Attempts to talk With Pokemon and reports success so callers can handle failure without exceptions.
    private void TryTalkWithPokemon()
    {
        if (_talkTargetIndex < 0)
        {
            return;
        }

        ResetAssignmentFailureDialogueState();
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

    // Enters crafting flow and initializes transient interaction state.
    private void OpenCrafting(CraftingSource craftingSource)
    {
        _activeCraftingSource = craftingSource;
        _activeWorkbenchIndex = -1;
        _activeFarmIndex = -1;
        if (craftingSource == CraftingSource.BasicWorkBenchCrafting &&
            _interactTarget?.Definition == ItemCatalog.WorkBench &&
            _interactTarget?.IsConstructionSite == false)
        {
            _activeWorkbenchIndex = _placedItems.FindIndex(item => item == _interactTarget);
        }
        else if (craftingSource == CraftingSource.FarmGrowing &&
                 _interactTarget?.Definition == ItemCatalog.Farm &&
                 _interactTarget?.IsConstructionSite == false)
        {
            _activeFarmIndex = _placedItems.FindIndex(item => item == _interactTarget);
        }

        _inputMode = InputMode.Crafting;
        List<RecipeDefinition> activeRecipes = GetActiveRecipes();
        _selectedCraftingIndex = Math.Clamp(_selectedCraftingIndex, 0, Math.Max(0, activeRecipes.Count - 1));
    }

    // Searches current state to locate interactable Target.
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

    // Enters building Talk flow and initializes transient interaction state.
    private void OpenBuildingTalk(PlacedItem building)
    {
        ResetAssignmentFailureDialogueState();
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

    // Searches current state to locate nearby Pokemon Target Index.
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

    // Computes and returns facing Interaction Area without mutating persistent game state.
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

    // Ticks talk Navigation each frame and keeps related timers and state synchronized.
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

    // Finalizes talk Option and applies the selected action to game state.
    private void ConfirmTalkOption()
    {
        PokemonDialogueOption? selectedOption = _talkState.GetSelectedOption();
        if (selectedOption is null)
        {
            ExitTalkMode();
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.ReturnToBuildingDialogue)
        {
            ReturnToBuildingDialogueAfterAssignmentFailure();
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

        if (selectedOption.Action == PokemonDialogueAction.UnassignBedResident && selectedOption.TargetPokemonId.HasValue)
        {
            UnassignPokemonFromActiveBed(selectedOption.TargetPokemonId.Value);
            if (selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
            }
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.AssignResourceWork && selectedOption.TargetPokemonId.HasValue)
        {
            bool assignedSuccessfully = AssignPokemonToResourceBuilding(selectedOption.TargetPokemonId.Value);
            if (assignedSuccessfully && selectedOption.ExitAfterDelay)
            {
                BeginTalkExitCountdown();
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

        if (selectedOption.Action == PokemonDialogueAction.AssignConstructionWorker && selectedOption.TargetPokemonId.HasValue)
        {
            AssignPokemonToActiveConstructionSite(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.UnassignConstructionWorker && selectedOption.TargetPokemonId.HasValue)
        {
            UnassignPokemonFromActiveConstructionSite(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.StartDittoWork)
        {
            StartDittoWorkingAtActiveBuilding();
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.StopDittoWork)
        {
            StopDittoWorking();
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

    // Enters talk Exit Countdown flow and initializes transient interaction state.
    private void BeginTalkExitCountdown()
    {
        _talkExitTimer = TalkExitDelaySeconds;
    }

    // Leaves talk Mode flow and restores default interaction state.
    private void ExitTalkMode()
    {
        _inputMode = InputMode.Gameplay;
        _talkExitTimer = 0f;
        _isDittoWorking = false;
        _dittoWorkBuildingIndex = -1;
        _dittoWorkType = DittoWorkType.None;
        _dittoWorkBuildingBounds = Rectangle.Empty;
        _dittoWorkBuildingDefinition = null;
        _dittoWorkDialogueDotTimer = 0f;
        _dittoWorkDialogueDotCount = 0;
        _talkState.Reset();
        ResetAssignmentFailureDialogueState();
    }

    // Opens an assignment-failure sub-dialogue from the Pokemon's perspective with a single continue option.
    private void ShowAssignmentFailureDialogue(SpawnedPokemon pokemon, string failureText)
    {
        if (_talkState.ActiveBuilding is null)
        {
            _talkState.SetText(failureText);
            return;
        }

        _assignmentFailureReturnBuilding = _talkState.ActiveBuilding;
        _isAssignmentFailureDialogueActive = true;
        _talkState.BeginBuildingTalk(
            _talkState.ActiveBuilding,
            failureText,
            [new PokemonDialogueOption("CONTINUE", PokemonDialogueAction.ReturnToBuildingDialogue)],
            pokemon.Name.ToUpperInvariant());
        SetActiveTalkIcon(pokemon.Name);
    }

    // Restores the original building dialogue after the assignment-failure sub-dialogue is acknowledged.
    private void ReturnToBuildingDialogueAfterAssignmentFailure()
    {
        if (!_isAssignmentFailureDialogueActive)
        {
            return;
        }

        if (_assignmentFailureReturnBuilding is null)
        {
            ExitTalkMode();
            return;
        }

        int buildingIndex = _placedItems.FindIndex(item => item == _assignmentFailureReturnBuilding);
        if (buildingIndex < 0)
        {
            ExitTalkMode();
            return;
        }

        PlacedItem building = _placedItems[buildingIndex];
        _talkState.BeginBuildingTalk(
            building,
            BuildingDialogueService.GetOpeningText(building),
            GetBuildingTalkOptions(building),
            "DITTO");
        SetActiveTalkIcon("Ditto");
        ResetAssignmentFailureDialogueState();
    }

    // Clears transient assignment-failure dialogue state.
    private void ResetAssignmentFailureDialogueState()
    {
        _isAssignmentFailureDialogueActive = false;
        _assignmentFailureReturnBuilding = null;
    }

    // Ticks crafting Navigation each frame and keeps related timers and state synchronized.
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

    // Enters pc Menu flow and initializes transient interaction state.
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
        _isPcStorageActionMenuOpen = false;
        _selectedPcStorageActionIndex = 0;
        _inputMode = InputMode.PcMenu;
        _talkExitTimer = 0f;
    }

    // Leaves pc Menu flow and restores default interaction state.
    private void ClosePcMenu()
    {
        _inputMode = InputMode.Gameplay;
        _selectedPcMenuIndex = 0;
        _isPcStorageActionMenuOpen = false;
        _selectedPcStorageActionIndex = 0;
        _activePcIndex = -1;
    }

    // Ticks pc Menu Navigation each frame and keeps related timers and state synchronized.
    private void UpdatePcMenuNavigation(bool moveUp, bool moveDown, bool moveLeft, bool moveRight)
    {
        if (_activePcMenuScreen == PcMenuScreen.Storage && _isPcStorageActionMenuOpen)
        {
            List<string> actionOptions = GetSelectedPcStorageEntryActions();
            if (actionOptions.Count <= 0)
            {
                _selectedPcStorageActionIndex = 0;
                return;
            }

            if (moveUp || moveLeft)
            {
                _selectedPcStorageActionIndex = Math.Max(0, _selectedPcStorageActionIndex - 1);
            }

            if (moveDown || moveRight)
            {
                _selectedPcStorageActionIndex = Math.Min(actionOptions.Count - 1, _selectedPcStorageActionIndex + 1);
            }

            return;
        }

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

    // Finalizes pc Menu Option and applies the selected action to game state.
    private void ConfirmPcMenuOption()
    {
        if (_inputMode != InputMode.PcMenu || _activePcMenuScreen != PcMenuScreen.Storage)
        {
            return;
        }

        List<PcStorageEntry> entries = GetPcStorageEntries();
        if (entries.Count == 0 ||
            _selectedPcMenuIndex < 0 ||
            _selectedPcMenuIndex >= entries.Count)
        {
            return;
        }

        if (_activePcIndex < 0 || _activePcIndex >= _placedItems.Count || _placedItems[_activePcIndex].Definition != ItemCatalog.Pc)
        {
            _interactionMessage = "PC NOT AVAILABLE";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        if (!_isPcStorageActionMenuOpen)
        {
            _isPcStorageActionMenuOpen = true;
            _selectedPcStorageActionIndex = 0;
            return;
        }

        PcStorageEntry entry = entries[_selectedPcMenuIndex];
        List<string> actions = GetPcStorageActions(entry);
        if (actions.Count == 0)
        {
            _isPcStorageActionMenuOpen = false;
            return;
        }

        _selectedPcStorageActionIndex = Math.Clamp(_selectedPcStorageActionIndex, 0, actions.Count - 1);
        string action = actions[_selectedPcStorageActionIndex];
        if (entry.IsStoredInPc && (action == "RELEASE" || action == "PLACE ON FARM"))
        {
            if (!TryReleaseStoredPokemon(entry.StoredIndex, action))
            {
                return;
            }
        }
        else if (!entry.IsStoredInPc && action == "STORE")
        {
            if (entry.PokemonId is not int pokemonId || !TryStorePokemonFromFarmById(pokemonId))
            {
                return;
            }
        }

        _isPcStorageActionMenuOpen = false;
        _selectedPcStorageActionIndex = 0;
        int refreshedCount = GetPcStorageEntries().Count;
        _selectedPcMenuIndex = Math.Clamp(_selectedPcMenuIndex, 0, Math.Max(0, refreshedCount - 1));
    }

    // Computes and returns pc Menu Entry Count without mutating persistent game state.
    private int GetPcMenuEntryCount()
    {
        return _activePcMenuScreen switch
        {
            PcMenuScreen.Quests => _activeQuests.Count,
            PcMenuScreen.Storage => GetPcStorageEntries().Count,
            _ => 0
        };
    }

    // Handles store Following Pokemon In Pc for this gameplay subsystem.
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

        ClearExistingBedForPokemon(pokemon.PokemonId);
        ClearExistingWorkBuildingForPokemon(pokemon.PokemonId);
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

    // Closes the PC storage entry action submenu and returns to the main storage list selection.
    private void ClosePcStorageActionMenu()
    {
        _isPcStorageActionMenuOpen = false;
        _selectedPcStorageActionIndex = 0;
    }

    // Computes and returns pc Storage Entries without mutating persistent game state.
    private List<PcStorageEntry> GetPcStorageEntries()
    {
        List<PcStorageEntry> entries = [];
        for (int index = 0; index < _storedPcPokemonNames.Count; index++)
        {
            entries.Add(new PcStorageEntry(_storedPcPokemonNames[index], true, index));
        }

        foreach (SpawnedPokemon pokemon in _spawnedDittos)
        {
            entries.Add(new PcStorageEntry(pokemon.Name, false, -1, pokemon.PokemonId));
        }

        return entries;
    }

    // Computes and returns the available action labels for the currently highlighted PC storage entry.
    private List<string> GetSelectedPcStorageEntryActions()
    {
        List<PcStorageEntry> entries = GetPcStorageEntries();
        if (_selectedPcMenuIndex < 0 || _selectedPcMenuIndex >= entries.Count)
        {
            return [];
        }

        return GetPcStorageActions(entries[_selectedPcMenuIndex]);
    }

    // Computes and returns action options for a PC storage entry based on whether that Pokemon is boxed or on farm.
    private static List<string> GetPcStorageActions(PcStorageEntry entry)
    {
        if (entry.IsStoredInPc)
        {
            return ["RELEASE", "PLACE ON FARM"];
        }

        return ["STORE"];
    }

    // Attempts to release a boxed Pokemon near the PC and reports success so callers can handle failure without exceptions.
    private bool TryReleaseStoredPokemon(int storedIndex, string actionLabel)
    {
        if (storedIndex < 0 || storedIndex >= _storedPcPokemonNames.Count)
        {
            return false;
        }

        if (_activePcIndex < 0 || _activePcIndex >= _placedItems.Count || _placedItems[_activePcIndex].Definition != ItemCatalog.Pc)
        {
            _interactionMessage = "PC NOT AVAILABLE";
            _interactionMessageTimer = InteractionMessageDuration;
            return false;
        }

        string pokemonName = _storedPcPokemonNames[storedIndex];
        if (!TryFindNearbyOpenPokemonSpawnPosition(_placedItems[_activePcIndex].Bounds, out Vector2 spawnPosition))
        {
            _interactionMessage = "NO VALID SPOT AVAILABLE";
            _interactionMessageTimer = InteractionMessageDuration;
            return false;
        }

        SpawnedPokemonDefinition spawnDefinition = SpawnedPokemonCatalog.GetOrDefault(pokemonName);
        _spawnedDittos.Add(new SpawnedPokemon(
            _nextPokemonId++,
            spawnDefinition.Name,
            spawnPosition,
            Direction.Down,
            GetRandomMoveDelaySeconds(),
            spawnDefinition.SkillLevels));
        _storedPcPokemonNames.RemoveAt(storedIndex);
        _interactionMessage = actionLabel == "PLACE ON FARM"
            ? $"{spawnDefinition.Name.ToUpperInvariant()} PLACED ON FARM"
            : $"{spawnDefinition.Name.ToUpperInvariant()} RELEASED";
        _interactionMessageTimer = InteractionMessageDuration;
        return true;
    }

    // Attempts to store an on-farm Pokemon in the PC and reports success so callers can handle failure without exceptions.
    private bool TryStorePokemonFromFarmById(int pokemonId)
    {
        int pokemonIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (pokemonIndex < 0)
        {
            return false;
        }

        SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
        ClearExistingBedForPokemon(pokemon.PokemonId);
        ClearExistingWorkBuildingForPokemon(pokemon.PokemonId);
        _storedPcPokemonNames.Add(pokemon.Name);
        _spawnedDittos.RemoveAt(pokemonIndex);
        _interactionMessage = $"{pokemon.Name.ToUpperInvariant()} STORED IN PC";
        _interactionMessageTimer = InteractionMessageDuration;
        return true;
    }

    // Attempts to find Nearby Open Pokemon Spawn Position and reports success so callers can handle failure without exceptions.
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

    // Computes and returns spawn Offsets without mutating persistent game state.
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

    // Enters dungeon Menu flow and initializes transient interaction state.
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

    // Leaves dungeon Menu flow and restores default interaction state.
    private void CloseDungeonMenu()
    {
        _inputMode = InputMode.Gameplay;
        if (_activeDungeonRun is null)
        {
            _activeDungeonPortalIndex = -1;
        }
    }

    // Ticks dungeon Menu Navigation each frame and keeps related timers and state synchronized.
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

    // Finalizes dungeon Menu Selection and applies the selected action to game state.
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

    // Handles enter Dungeon Run for this gameplay subsystem.
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

    // Handles advance Dungeon Room Or Exit for this gameplay subsystem.
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

    // Handles leave Active Dungeon Run for this gameplay subsystem.
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

    // Computes and returns dungeon Portal Exit Spawn Position without mutating persistent game state.
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
