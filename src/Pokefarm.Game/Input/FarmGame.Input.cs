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
            _isInventoryActionMenuOpen = false;
            _selectedInventoryActionIndex = 0;
            _inputMode = InputMode.Gameplay;
            return;
        }

        ExitPlacementMode(InputMode.Gameplay);
        _inputMode = InputMode.Inventory;
        _isInventoryActionMenuOpen = false;
        _selectedInventoryActionIndex = 0;
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

        if (keyboard.IsKeyDown(Keys.Left) && !moveLeftPressed)
        {
            previewMovement.X -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.Right) && !moveRightPressed)
        {
            previewMovement.X += 1f;
        }

        if (keyboard.IsKeyDown(Keys.Up) && !moveUpPressed)
        {
            previewMovement.Y -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.Down) && !moveDownPressed)
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

    // Handles inventory action confirmation flow (Build/Drop) for the selected inventory stack.
    private void ConfirmInventoryAction()
    {
        if (_selectedInventoryIndex < 0 || _selectedInventoryIndex >= _inventoryItems.Count)
        {
            _isInventoryActionMenuOpen = false;
            _selectedInventoryActionIndex = 0;
            return;
        }

        if (!_isInventoryActionMenuOpen)
        {
            _isInventoryActionMenuOpen = true;
            _selectedInventoryActionIndex = 0;
            return;
        }

        InventoryEntry selectedEntry = _inventoryItems[_selectedInventoryIndex];
        List<string> actions = GetInventoryActions(selectedEntry.Definition);
        if (_selectedInventoryActionIndex < 0 || _selectedInventoryActionIndex >= actions.Count)
        {
            _selectedInventoryActionIndex = 0;
            return;
        }

        string action = actions[_selectedInventoryActionIndex];
        if (action == "BUILD")
        {
            BeginPlacementFromInventory();
            _isInventoryActionMenuOpen = false;
            _selectedInventoryActionIndex = 0;
            return;
        }

        if (action == "DROP")
        {
            TryDropSelectedInventoryItem();
            _isInventoryActionMenuOpen = false;
            _selectedInventoryActionIndex = 0;
        }
    }

    // Computes and returns available inventory actions for a specific item definition.
    private static List<string> GetInventoryActions(ItemDefinition definition)
    {
        List<string> actions = [];
        if (definition.IsPlaceable)
        {
            actions.Add("BUILD");
        }

        actions.Add("DROP");
        return actions;
    }

    // Attempts to drop one unit from selected inventory stack onto the ground.
    private void TryDropSelectedInventoryItem()
    {
        if (_selectedInventoryIndex < 0 || _selectedInventoryIndex >= _inventoryItems.Count)
        {
            return;
        }

        InventoryEntry entry = _inventoryItems[_selectedInventoryIndex];
        if (!TryFindDroppedItemSpawnBounds(_playerPosition, entry.Definition, out Rectangle dropBounds))
        {
            _interactionMessage = "NO ROOM TO DROP";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        _droppedWorldItems.Add(new DroppedWorldItem(dropBounds, entry.Definition, _elapsedWorldTimeSeconds));
        RemoveInventoryItem(entry.Definition, 1);
        _interactionMessage = $"DROPPED {entry.Definition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Ticks gameplay Movement each frame and keeps related timers and state synchronized.
    private void UpdateGameplayMovement(KeyboardState keyboard, GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 movement = Vector2.Zero;

        if (keyboard.IsKeyDown(Keys.Up))
        {
            movement.Y -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.Down))
        {
            movement.Y += 1f;
        }

        if (keyboard.IsKeyDown(Keys.Left))
        {
            movement.X -= 1f;
        }

        if (keyboard.IsKeyDown(Keys.Right))
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
        if (_isInventoryActionMenuOpen)
        {
            if (_selectedInventoryIndex < 0 || _selectedInventoryIndex >= _inventoryItems.Count)
            {
                _isInventoryActionMenuOpen = false;
                _selectedInventoryActionIndex = 0;
                return;
            }

            InventoryEntry selectedEntry = _inventoryItems[_selectedInventoryIndex];
            List<string> actions = GetInventoryActions(selectedEntry.Definition);
            if (actions.Count <= 0)
            {
                _isInventoryActionMenuOpen = false;
                _selectedInventoryActionIndex = 0;
                return;
            }

            if (moveUp || moveLeft)
            {
                _selectedInventoryActionIndex = Math.Max(0, _selectedInventoryActionIndex - 1);
            }

            if (moveDown || moveRight)
            {
                _selectedInventoryActionIndex = Math.Min(actions.Count - 1, _selectedInventoryActionIndex + 1);
            }

            return;
        }

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

    // Attempts to interact With Building and reports success so callers can handle failure without exceptions.
    private void TryInteractWithBuilding()
    {
        if (_interactTarget is null)
        {
            OpenCrafting(CraftingSource.HandheldCrafting);
            return;
        }

        if (_interactTarget.Definition == ItemCatalog.Pc &&
            _storyManager.TryBuildPcTutorialScene(out StorySceneDefinition tutorialScene))
        {
            _storyManager.MarkTutorialStarted();
            StartStoryScene(tutorialScene, _interactTarget);
            return;
        }

        if (_interactTarget.Definition == ItemCatalog.Chest &&
            !_interactTarget.IsConstructionSite &&
            !HasFollowingTransportPokemon())
        {
            OpenChestStorage(_interactTarget);
            return;
        }

        OpenBuildingTalk(_interactTarget);
    }

    // Attempts to pick up the nearest dropped world item currently in interaction range.
    private bool TryPickUpNearbyDroppedWorldItem()
    {
        if (_nearbyDroppedItemIndex < 0 || _nearbyDroppedItemIndex >= _droppedWorldItems.Count)
        {
            return false;
        }

        DroppedWorldItem dropped = _droppedWorldItems[_nearbyDroppedItemIndex];
        if (!CanAddInventoryItem(dropped.Definition))
        {
            _interactionMessage = "INVENTORY FULL";
            _interactionMessageTimer = InteractionMessageDuration;
            return true;
        }

        if (!AddInventoryItem(dropped.Definition, 1))
        {
            return true;
        }

        _droppedWorldItems.RemoveAt(_nearbyDroppedItemIndex);
        _nearbyDroppedItemIndex = -1;
        _interactionMessage = $"PICKED UP {dropped.Definition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
        return true;
    }

    // Attempts to use Ditto's selected Cut/Rock Smash skill on a facing building.
    private void TryUseActiveSkillOnBuilding()
    {
        if (_interactTarget is null)
        {
            return;
        }

        if (_interactTarget.Definition == ItemCatalog.LogsDebris && _activeDittoSkill != SkillType.Cut)
        {
            _interactionMessage = "CUT REQUIRED";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        if (_interactTarget.Definition == ItemCatalog.BoulderDebris && _activeDittoSkill != SkillType.RockSmash)
        {
            _interactionMessage = "ROCK SMASH REQUIRED";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        if (_activeSkillDamageTarget is null || _activeSkillDamageTarget != _interactTarget)
        {
            _activeSkillDamageTarget = _interactTarget;
            _activeSkillDamageAmount = 0;
        }

        _activeSkillDamageAmount += 1;
        _activeSkillDamageLastWorldTimeSeconds = _elapsedWorldTimeSeconds;

        if (_activeSkillDamageAmount >= SkillBuildingHealth)
        {
            DestroyBuildingWithActiveSkill(_interactTarget);
            return;
        }

        _interactionMessage = $"{GetActiveSkillLabel().ToUpperInvariant()} {_activeSkillDamageAmount}/{SkillBuildingHealth}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Removes a building from the world and transfers it into inventory as a skill-destruction reward.
    private void DestroyBuildingWithActiveSkill(PlacedItem targetBuilding)
    {
        int buildingIndex = _placedItems.FindIndex(item => item == targetBuilding);
        if (buildingIndex < 0)
        {
            _activeSkillDamageTarget = null;
            _activeSkillDamageAmount = 0;
            return;
        }

        PlacedItem building = _placedItems[buildingIndex];

        _placedItems.RemoveAt(buildingIndex);
        DropStoredItemsFromDestroyedBuilding(building);
        Vector2 dropOrigin = new(building.Bounds.Center.X, building.Bounds.Center.Y);
        if (building.Definition.Kind != ItemKind.Debris &&
            TryFindDroppedItemSpawnBounds(dropOrigin, building.Definition, out Rectangle dropBounds))
        {
            _droppedWorldItems.Add(new DroppedWorldItem(dropBounds, building.Definition, _elapsedWorldTimeSeconds));
        }
        _activeSkillDamageTarget = null;
        _activeSkillDamageAmount = 0;
        _interactTarget = null;
        _interactionMessage = building.Definition.Kind == ItemKind.Debris
            ? $"{building.Definition.Name.ToUpperInvariant()} CLEARED"
            : $"{building.Definition.Name.ToUpperInvariant()} DROPPED";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Drops stored outputs/materials from a destroyed building onto the ground.
    private void DropStoredItemsFromDestroyedBuilding(PlacedItem building)
    {
        Vector2 dropOrigin = new(building.Bounds.Center.X, building.Bounds.Center.Y);

        if (building.Definition == ItemCatalog.LogsDebris)
        {
            int woodQuantity = GetStoredItems(building)
                .Where(entry => entry.Definition == ItemCatalog.Wood)
                .Sum(entry => entry.Quantity);
            DropWorldItemQuantity(dropOrigin, ItemCatalog.Wood, woodQuantity);
            return;
        }

        if (building.Definition == ItemCatalog.BoulderDebris)
        {
            int stoneQuantity = GetStoredItems(building)
                .Where(entry => entry.Definition == ItemCatalog.Stone)
                .Sum(entry => entry.Quantity);
            DropWorldItemQuantity(dropOrigin, ItemCatalog.Stone, stoneQuantity);
            return;
        }

        if (building.Definition == ItemCatalog.Chest)
        {
            foreach (InventoryEntry storedEntry in GetStoredItems(building))
            {
                DropWorldItemQuantity(dropOrigin, storedEntry.Definition, storedEntry.Quantity);
            }
        }

        if (building.Definition.IsResourceProduction)
        {
            ItemDefinition? producedMaterial = GetProducedMaterialForBuilding(building);
            if (producedMaterial is not null && building.StoredProducedUnits > 0)
            {
                DropWorldItemQuantity(dropOrigin, producedMaterial, building.StoredProducedUnits);
            }
        }

        if (building.Definition == ItemCatalog.WorkBench &&
            building.WorkbenchStoredItem is not null &&
            building.WorkbenchStoredQuantity > 0)
        {
            DropWorldItemQuantity(dropOrigin, building.WorkbenchStoredItem, building.WorkbenchStoredQuantity);
        }
    }

    // Drops a stack quantity as individual world items using the normal spread placement rules.
    private void DropWorldItemQuantity(Vector2 dropOrigin, ItemDefinition definition, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        for (int index = 0; index < quantity; index++)
        {
            if (!TryFindDroppedItemSpawnBounds(dropOrigin, definition, out Rectangle dropBounds))
            {
                break;
            }

            _droppedWorldItems.Add(new DroppedWorldItem(dropBounds, definition, _elapsedWorldTimeSeconds));
        }
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

    // Enters chest storage flow and initializes transient interaction state.
    private void OpenChestStorage(PlacedItem chest)
    {
        if (chest.Definition != ItemCatalog.Chest || chest.IsConstructionSite)
        {
            return;
        }

        int chestIndex = _placedItems.FindIndex(item => item == chest);
        if (chestIndex < 0)
        {
            return;
        }

        _activeChestIndex = chestIndex;
        _isChestSelectionOnChest = true;
        _selectedChestStorageIndex = 0;
        _selectedChestInventoryIndex = Math.Clamp(_selectedChestInventoryIndex, 0, Math.Max(0, _inventoryItems.Count - 1));
        _inputMode = InputMode.ChestStorage;
    }

    // Leaves chest storage flow and restores default interaction state.
    private void CloseChestStorage()
    {
        _inputMode = InputMode.Gameplay;
        _activeChestIndex = -1;
        _isChestSelectionOnChest = true;
        _selectedChestStorageIndex = 0;
        _selectedChestInventoryIndex = 0;
    }

    // Ticks chest storage navigation each frame and keeps related timers and state synchronized.
    private void UpdateChestStorageNavigation(bool moveUp, bool moveDown, bool moveLeft, bool moveRight)
    {
        if (moveLeft)
        {
            _isChestSelectionOnChest = true;
        }
        else if (moveRight)
        {
            _isChestSelectionOnChest = false;
        }

        if (_isChestSelectionOnChest)
        {
            List<InventoryEntry> storedItems = GetActiveChestStoredItems();
            if (storedItems.Count <= 0)
            {
                _selectedChestStorageIndex = 0;
                return;
            }

            if (moveUp)
            {
                _selectedChestStorageIndex = Math.Max(0, _selectedChestStorageIndex - 1);
            }

            if (moveDown)
            {
                _selectedChestStorageIndex = Math.Min(storedItems.Count - 1, _selectedChestStorageIndex + 1);
            }

            return;
        }

        if (_inventoryItems.Count <= 0)
        {
            _selectedChestInventoryIndex = 0;
            return;
        }

        if (moveUp)
        {
            _selectedChestInventoryIndex = Math.Max(0, _selectedChestInventoryIndex - 1);
        }

        if (moveDown)
        {
            _selectedChestInventoryIndex = Math.Min(_inventoryItems.Count - 1, _selectedChestInventoryIndex + 1);
        }
    }

    // Transfers one unit between player inventory and active chest based on current pane selection.
    private void TransferSelectedChestItem()
    {
        if (_activeChestIndex < 0 ||
            _activeChestIndex >= _placedItems.Count ||
            _placedItems[_activeChestIndex].Definition != ItemCatalog.Chest ||
            _placedItems[_activeChestIndex].IsConstructionSite)
        {
            _interactionMessage = "CHEST NOT AVAILABLE";
            _interactionMessageTimer = InteractionMessageDuration;
            CloseChestStorage();
            return;
        }

        PlacedItem chest = _placedItems[_activeChestIndex];
        List<InventoryEntry> storedItems = GetStoredItems(chest);

        if (_isChestSelectionOnChest)
        {
            if (storedItems.Count <= 0)
            {
                return;
            }

            _selectedChestStorageIndex = Math.Clamp(_selectedChestStorageIndex, 0, storedItems.Count - 1);
            InventoryEntry selectedChestEntry = storedItems[_selectedChestStorageIndex];
            if (!CanAddInventoryItem(selectedChestEntry.Definition))
            {
                _interactionMessage = "INVENTORY FULL";
                _interactionMessageTimer = InteractionMessageDuration;
                return;
            }

            if (!AddInventoryItem(selectedChestEntry.Definition, 1))
            {
                return;
            }

            storedItems = RemoveStoredItemUnit(storedItems, _selectedChestStorageIndex);
            _placedItems[_activeChestIndex] = chest with { StoredItems = storedItems };
            _selectedChestStorageIndex = Math.Clamp(_selectedChestStorageIndex, 0, Math.Max(0, storedItems.Count - 1));
            _interactionMessage = $"TOOK {selectedChestEntry.Definition.Name.ToUpperInvariant()}";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        if (_inventoryItems.Count <= 0)
        {
            return;
        }

        _selectedChestInventoryIndex = Math.Clamp(_selectedChestInventoryIndex, 0, _inventoryItems.Count - 1);
        InventoryEntry selectedInventoryEntry = _inventoryItems[_selectedChestInventoryIndex];
        if (!CanAddStoredItem(chest, selectedInventoryEntry.Definition))
        {
            _interactionMessage = "CHEST FULL";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        int existingIndex = storedItems.FindIndex(entry => entry.Definition == selectedInventoryEntry.Definition);
        if (existingIndex >= 0)
        {
            InventoryEntry existing = storedItems[existingIndex];
            storedItems[existingIndex] = existing with { Quantity = existing.Quantity + 1 };
        }
        else
        {
            storedItems.Add(new InventoryEntry(selectedInventoryEntry.Definition, 1));
        }

        RemoveInventoryItem(selectedInventoryEntry.Definition, 1);
        _placedItems[_activeChestIndex] = chest with { StoredItems = storedItems };
        _selectedChestInventoryIndex = Math.Clamp(_selectedChestInventoryIndex, 0, Math.Max(0, _inventoryItems.Count - 1));
        _interactionMessage = $"STORED {selectedInventoryEntry.Definition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Computes and returns active chest storage entries without mutating persistent game state.
    private List<InventoryEntry> GetActiveChestStoredItems()
    {
        if (_activeChestIndex < 0 || _activeChestIndex >= _placedItems.Count)
        {
            return [];
        }

        PlacedItem chest = _placedItems[_activeChestIndex];
        if (chest.Definition != ItemCatalog.Chest || chest.IsConstructionSite)
        {
            return [];
        }

        return GetStoredItems(chest);
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

    private void StartStoryScene(StorySceneDefinition scene, PlacedItem sourceBuilding)
    {
        ResetAssignmentFailureDialogueState();
        FaceConversationTarget(new Vector2(sourceBuilding.Bounds.Center.X, sourceBuilding.Bounds.Center.Y));
        _talkState.BeginBuildingTalk(
            sourceBuilding,
            scene.OpeningText,
            scene.Options,
            scene.SpeakerName);
        _talkExitTimer = 0f;
        SetActiveTalkIcon(scene.SpeakerName);
        _inputMode = InputMode.Talking;
        _activeStorySceneId = scene.Id;
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

    // Finds the closest dropped item currently inside the player's facing interaction area.
    private int FindNearbyDroppedWorldItemIndex()
    {
        Rectangle searchArea = GetFacingInteractionArea();
        Vector2 playerCenter = new(_playerPosition.X + (PlayerSize / 2f), _playerPosition.Y + (PlayerSize / 2f));
        int bestIndex = -1;
        float bestDistanceSquared = float.MaxValue;

        for (int index = 0; index < _droppedWorldItems.Count; index++)
        {
            DroppedWorldItem dropped = _droppedWorldItems[index];
            if (!dropped.Bounds.Intersects(searchArea))
            {
                continue;
            }

            Vector2 dropCenter = new(dropped.Bounds.Center.X, dropped.Bounds.Center.Y);
            float distanceSquared = Vector2.DistanceSquared(playerCenter, dropCenter);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    // Attempts to find a valid, slightly spread-out ground-drop location for an item.
    private bool TryFindDroppedItemSpawnBounds(Vector2 originPosition, ItemDefinition definition, out Rectangle dropBounds)
    {
        Point dropSize = new(
            Math.Max(14, definition.Size.X / 2),
            Math.Max(14, definition.Size.Y / 2));
        Rectangle playableArea = new(
            BorderThickness,
            BorderThickness,
            _worldBounds.Width - (BorderThickness * 2),
            _worldBounds.Height - (BorderThickness * 2));
        int[] radii = [0, 16, 28, 40, 56];

        foreach (int radius in radii)
        {
            foreach (Point offset in GetSpawnOffsets(radius))
            {
                Rectangle candidate = new(
                    (int)originPosition.X - (dropSize.X / 2) + offset.X,
                    (int)originPosition.Y - (dropSize.Y / 2) + offset.Y,
                    dropSize.X,
                    dropSize.Y);
                if (!playableArea.Contains(candidate))
                {
                    continue;
                }

                bool intersectsBuilding = _placedItems.Any(item => item.Definition.HasCollision && item.Bounds.Intersects(candidate));
                if (intersectsBuilding)
                {
                    continue;
                }

                bool intersectsDropped = _droppedWorldItems.Any(item => item.Bounds.Intersects(candidate));
                if (intersectsDropped)
                {
                    continue;
                }

                dropBounds = candidate;
                return true;
            }
        }

        dropBounds = Rectangle.Empty;
        return false;
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

        if (selectedOption.Action == PokemonDialogueAction.OpenChestStorage)
        {
            if (_talkState.ActiveBuilding is not null)
            {
                OpenChestStorage(_talkState.ActiveBuilding);
            }

            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.AssignChestTransport && selectedOption.TargetPokemonId.HasValue)
        {
            AssignPokemonToActiveChestTransport(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.UnassignChestTransport && selectedOption.TargetPokemonId.HasValue)
        {
            UnassignPokemonFromActiveChestTransport(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.AssignDungeonTeleporting && selectedOption.TargetPokemonId.HasValue)
        {
            AssignPokemonToActiveDungeonPortal(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.UnassignDungeonTeleporting && selectedOption.TargetPokemonId.HasValue)
        {
            UnassignPokemonFromActiveDungeonPortal(selectedOption.TargetPokemonId.Value);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.OpenPcLevel)
        {
            OpenPcMenu(PcMenuScreen.Level);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.SaveGame)
        {
            SaveActiveProfile();
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

        if (selectedOption.Action == PokemonDialogueAction.StoryTutorialChooseA)
        {
            _talkState.SetText("GREAT");
            _talkState.SetOptions([new PokemonDialogueOption("BYE", PokemonDialogueAction.Exit)]);
            return;
        }

        if (selectedOption.Action == PokemonDialogueAction.StoryTutorialChooseB)
        {
            bool spawned = TrySpawnStoryPokemonNearActiveBuilding("Rotom");
            _talkState.SetText(spawned ? "I AM HERE NOW" : "NO VALID SPOT");
            _talkState.SetOptions([new PokemonDialogueOption("BYE", PokemonDialogueAction.Exit)]);
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

            _activeStorySceneId = null;

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
        _activeStorySceneId = null;
    }

    // Checks whether at least one currently-following Pokemon can perform chest transport work.
    private bool HasFollowingTransportPokemon()
    {
        return _spawnedDittos.Any(pokemon => pokemon.IsFollowingPlayer && pokemon.GetSkillLevel(SkillType.Transport) > 0);
    }

    private bool TrySpawnStoryPokemonNearActiveBuilding(string pokemonName)
    {
        if (_talkState.ActiveBuilding is null)
        {
            return false;
        }

        if (_spawnedDittos.Any(pokemon => string.Equals(pokemon.Name, pokemonName, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (!TryFindNearbyOpenPokemonSpawnPosition(_talkState.ActiveBuilding.Bounds, out Vector2 spawnPosition))
        {
            return false;
        }

        SpawnedPokemonDefinition spawnDefinition = SpawnedPokemonCatalog.GetOrDefault(pokemonName);
        _spawnedDittos.Add(CreateSpawnedPokemon(spawnDefinition, spawnPosition, Direction.Down, GetRandomMoveDelaySeconds()));
        return true;
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
        _activeStorySceneId = null;
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
        _selectedDittoSkillSlotIndex = 0;
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
        _selectedDittoSkillSlotIndex = 0;
        _isPcStorageActionMenuOpen = false;
        _selectedPcStorageActionIndex = 0;
        _activePcIndex = -1;
    }

    // Ticks pc Menu Navigation each frame and keeps related timers and state synchronized.
    private void UpdatePcMenuNavigation(bool moveUp, bool moveDown, bool moveLeft, bool moveRight)
    {
        if (_activePcMenuScreen == PcMenuScreen.Level)
        {
            const int columns = 4;
            const int rows = 2;
            int currentRow = _selectedDittoSkillSlotIndex / columns;
            int currentColumn = _selectedDittoSkillSlotIndex % columns;

            if (moveLeft && currentColumn > 0)
            {
                _selectedDittoSkillSlotIndex--;
            }

            if (moveRight && currentColumn < columns - 1)
            {
                _selectedDittoSkillSlotIndex++;
            }

            if (moveUp && currentRow > 0)
            {
                _selectedDittoSkillSlotIndex -= columns;
            }

            if (moveDown && currentRow < rows - 1)
            {
                _selectedDittoSkillSlotIndex += columns;
            }

            _selectedDittoSkillSlotIndex = Math.Clamp(_selectedDittoSkillSlotIndex, 0, DittoSkillSlotCount - 1);
            return;
        }

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
        _spawnedDittos.Add(CreateSpawnedPokemon(spawnDefinition, spawnPosition, Direction.Down, GetRandomMoveDelaySeconds()));
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
        int currentTeleportingSkill = GetDungeonPortalTeleportingSkillTotal();
        int requiredTeleportingSkill = Math.Max(0, dungeonDefinition.RequiredTeleportingSkill);
        if (currentTeleportingSkill < requiredTeleportingSkill)
        {
            _interactionMessage = $"NEED TELEPORTING {requiredTeleportingSkill}";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        List<string> partyToEnter = GetDungeonPartyForEntry();
        int followerCount = Math.Max(0, partyToEnter.Count - 1);
        if (followerCount > MaxDungeonFollowersAtStart)
        {
            _interactionMessage = $"TOO MANY FOLLOWERS {followerCount}/{MaxDungeonFollowersAtStart}";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        GeneratedDungeon generatedDungeon = DungeonGenerator.Generate(dungeonDefinition);
        _generatedDungeonPreview = generatedDungeon;
        EnterDungeonRun(generatedDungeon, partyToEnter);
        _interactionMessage = $"ENTERED {dungeonDefinition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Sums teleporting skill for Pokemon assigned to the active dungeon portal and in bed range of that portal.
    private int GetDungeonPortalTeleportingSkillTotal()
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
            portal = _placedItems.FirstOrDefault(item => item.Definition == ItemCatalog.DungeonPortal && !item.IsConstructionSite);
        }

        if (portal is null)
        {
            return 0;
        }

        int total = 0;
        foreach (int workerPokemonId in GetWorkerPokemonIds(portal))
        {
            SpawnedPokemon? worker = _spawnedDittos.FirstOrDefault(pokemon => pokemon.PokemonId == workerPokemonId);
            if (worker is null || worker.HomePosition is not Vector2)
            {
                continue;
            }

            if (!IsBuildingExitWithinPokemonBedRange(worker, portal))
            {
                continue;
            }

            total += Math.Max(0, worker.GetSkillLevel(SkillType.Teleporting));
        }

        return total;
    }

    // Handles enter Dungeon Run for this gameplay subsystem.
    private void EnterDungeonRun(GeneratedDungeon dungeonRun, IReadOnlyList<string> partyToEnter)
    {
        _activeDungeonRun = dungeonRun;
        _dungeonPokemon.Clear();
        _activeDungeonMoveAnimations.Clear();
        _activeDungeonProjectiles.Clear();
        _dungeonMoveCooldownRemaining = 0f;
        _dungeonProjectileCooldownRemaining = 0f;
        _dungeonPartyPokemonNames.Clear();
        _dungeonPartyPokemonNames.AddRange(partyToEnter);
        if (_dungeonPartyPokemonNames.Count == 0)
        {
            _dungeonPartyPokemonNames.Add(PlayerPokemonName);
        }
        _dungeonPartyCurrentPp.Clear();
        _dungeonPartyMaxPp.Clear();
        foreach (string partyPokemonName in _dungeonPartyPokemonNames)
        {
            int maxPp = DungeonDittoMaxPp;
            _dungeonPartyMaxPp.Add(maxPp);
            _dungeonPartyCurrentPp.Add(maxPp);
        }
        _activeDungeonPartyIndex = 0;
        SpawnedPokemonDefinition dittoDefinition = SpawnedPokemonCatalog.GetOrDefault(PlayerPokemonName);
        _dungeonDittoMaxHp = CalculateMaxHp(dittoDefinition.BaseStats, 50);
        _dungeonDittoCurrentHp = _dungeonDittoMaxHp;
        _dungeonDittoCurrentPp = _dungeonPartyCurrentPp[0];
        _inputMode = InputMode.Gameplay;
        _interactTarget = null;
        _talkTargetIndex = -1;
        Point mapOrigin = GetActiveDungeonMapOrigin(dungeonRun);
        _playerPosition = new Vector2(
            mapOrigin.X + (dungeonRun.PlayerStartTile.X * DungeonTileSize),
            mapOrigin.Y + (dungeonRun.PlayerStartTile.Y * DungeonTileSize));

        if (string.Equals(dungeonRun.DungeonName, "Tutorial Cavern", StringComparison.OrdinalIgnoreCase))
        {
            SpawnedPokemonDefinition abra = SpawnedPokemonCatalog.GetOrDefault("Abra");
            Vector2 abraPosition = new(
                mapOrigin.X + (9 * DungeonTileSize),
                mapOrigin.Y + (20 * DungeonTileSize));
            _dungeonPokemon.Add(CreateSpawnedPokemon(abra, abraPosition, Direction.Left, 0f));
        }
    }

    private List<string> GetDungeonPartyForEntry()
    {
        List<string> party = [PlayerPokemonName];
        foreach (SpawnedPokemon pokemon in _spawnedDittos.Where(pokemon => pokemon.IsFollowingPlayer && pokemon.IsClaimed))
        {
            party.Add(pokemon.Name);
        }

        return party;
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
        _dungeonPokemon.Clear();
        _activeDungeonMoveAnimations.Clear();
        _activeDungeonProjectiles.Clear();
        _dungeonMoveCooldownRemaining = 0f;
        _dungeonProjectileCooldownRemaining = 0f;
        _dungeonPartyPokemonNames.Clear();
        _dungeonPartyCurrentPp.Clear();
        _dungeonPartyMaxPp.Clear();
        _activeDungeonPartyIndex = 0;
        _dungeonSwapLockRemaining = 0f;
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
