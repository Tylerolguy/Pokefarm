using Microsoft.Xna.Framework;
using static Pokefarm.Game.BuildingWorkerHelpers;

namespace Pokefarm.Game;

// Building interaction handlers used by talk menus to assign workers, manage crafting, and collect output.
public sealed partial class FarmGame
{
    // Assigns a crafting-capable Pokemon to the active workbench and refreshes talk state/feedback.
    private void AssignPokemonToActiveWorkbench(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.WorkBench)
        {
            return;
        }

        int workbenchIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (workbenchIndex < 0)
        {
            return;
        }

        int pokemonIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (pokemonIndex < 0)
        {
            return;
        }

        SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
        if (pokemon.GetSkillLevel(SkillType.Crafting) <= 0)
        {
            _talkState.SetText("THIS POKEMON CANT CRAFT");
            return;
        }

        if (TrySetWorkbenchWorker(workbenchIndex, pokemonId, out string? message))
        {
            _talkState.UpdateBuildingReference(_placedItems[workbenchIndex]);
            _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[workbenchIndex]));
            _talkState.SetText("WORKER ASSIGNED");
            _interactionMessage = message ?? "WORKER ASSIGNED";
            _interactionMessageTimer = InteractionMessageDuration;
        }
    }

    // Removes the currently selected worker from the active workbench and updates dialogue options.
    private void UnassignPokemonFromActiveWorkbench(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.WorkBench)
        {
            return;
        }

        int workbenchIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (workbenchIndex < 0)
        {
            return;
        }

        if (TrySetWorkbenchWorker(workbenchIndex, null, out string? message))
        {
            _talkState.UpdateBuildingReference(_placedItems[workbenchIndex]);
            _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[workbenchIndex]));
            _talkState.SetText("WORKER UNASSIGNED");
            _interactionMessage = message ?? "WORKER UNASSIGNED";
            _interactionMessageTimer = InteractionMessageDuration;
        }
    }

    // Cancels the current workbench queue entry and refunds recipe costs back into inventory.
    private void DequeueActiveWorkbenchItem()
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.WorkBench)
        {
            return;
        }

        int workbenchIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (workbenchIndex < 0)
        {
            return;
        }

        PlacedItem workbench = _placedItems[workbenchIndex];
        if (workbench.WorkbenchQueuedItem is null)
        {
            _talkState.SetText("NOTHING QUEUED");
            return;
        }

        RecipeDefinition? queuedRecipe = _unlockedRecipes.FirstOrDefault(recipe =>
            recipe.Source == CraftingSource.BasicWorkBenchCrafting &&
            recipe.Output == workbench.WorkbenchQueuedItem);

        if (queuedRecipe is not null)
        {
            if (!CanAddInventoryItems(queuedRecipe.Costs.Select(cost => cost.Item)))
            {
                _talkState.SetText("INVENTORY FULL");
                _interactionMessage = "INVENTORY FULL";
                _interactionMessageTimer = InteractionMessageDuration;
                return;
            }

            foreach (RecipeCost cost in queuedRecipe.Costs)
            {
                AddInventoryItem(cost.Item, cost.Quantity);
            }
        }

        _placedItems[workbenchIndex] = workbench with
        {
            WorkbenchQueuedItem = null,
            WorkbenchCraftEffortRemaining = 0f,
            WorkbenchCraftEffortRequired = 0f
        };

        _interactTarget = _placedItems[workbenchIndex];
        _talkState.UpdateBuildingReference(_placedItems[workbenchIndex]);
        _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[workbenchIndex]));
        _talkState.SetText("QUEUE CLEARED");
        _interactionMessage = "QUEUE CLEARED";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Collects a completed workbench item through the talk flow and refreshes the active panel.
    private void CollectReadyWorkbenchItemFromTalk()
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.WorkBench)
        {
            return;
        }

        int workbenchIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (workbenchIndex < 0)
        {
            return;
        }

        if (!TryCollectReadyWorkbenchItem(workbenchIndex, out string? pickupMessage))
        {
            _talkState.SetText("NO ITEM READY");
            return;
        }

        _interactionMessage = pickupMessage;
        _interactionMessageTimer = InteractionMessageDuration;
        _talkState.UpdateBuildingReference(_placedItems[workbenchIndex]);
        _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[workbenchIndex]));
        _talkState.SetText("ITEM PICKED UP");
    }

    // Prevents assigning workbench jobs to Pokemon whose home bed is outside their allowed work radius.
    private static bool IsWorkbenchWithinPokemonBedRange(SpawnedPokemon pokemon, PlacedItem workbench)
    {
        if (pokemon.HomePosition is not Vector2 homePosition)
        {
            return false;
        }

        Vector2 workbenchCenter = new(
            workbench.Bounds.Center.X - (PlayerSize / 2f),
            workbench.Bounds.Center.Y - (PlayerSize / 2f));
        return Vector2.DistanceSquared(homePosition, workbenchCenter) <= HomeWanderRadius * HomeWanderRadius;
    }

    // Applies the selected crop to the active farm tile and resets growth/progress counters for the new plant.
    private void SetActiveFarmPlant(ItemDefinition plant)
    {
        if (_activeFarmIndex < 0 || _activeFarmIndex >= _placedItems.Count)
        {
            return;
        }

        PlacedItem farm = _placedItems[_activeFarmIndex];
        if (farm.Definition != ItemCatalog.Farm)
        {
            return;
        }

        _placedItems[_activeFarmIndex] = farm with
        {
            FarmGrowingPlant = plant,
            StoredProducedUnits = 0,
            StoredProductionEffort = 0f,
            ProductionStepIndex = 0
        };

        _interactTarget = _placedItems[_activeFarmIndex];
        _interactionMessage = $"PLANT SET TO {plant.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Sets a bed as a Pokemon's home, clearing follow/work movement so the Pokemon transitions to resident state.
    private void AssignPokemonHome(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.Bed)
        {
            return;
        }

        int bedIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (bedIndex < 0)
        {
            return;
        }

        PlacedItem bed = _placedItems[bedIndex];
        if (IsBedFull(bed) && !HasBedResident(bed, pokemonId))
        {
            _talkState.SetText("THIS BED IS FULL");
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
            IsWorking = false,
            IsFollowingPlayer = false,
            IsMoving = false,
            MoveTimeRemaining = 0f,
            MoveCooldownRemaining = 0f,
            MoveTarget = pokemon.Position,
            HomePosition = homePosition,
            WanderTarget = homePosition,
            Direction = GetDirectionTowardTarget(pokemon.Position, homePosition),
            SpeechText = "HOME!",
            SpeechTimerRemaining = InteractionMessageDuration
        };

        ClearExistingBedForPokemon(pokemon.PokemonId);
        _placedItems[bedIndex] = AddResidentToBed(_placedItems[bedIndex], pokemon);
        _interactTarget = _placedItems[bedIndex];
        _talkState.UpdateBuildingReference(_placedItems[bedIndex]);

        _interactionMessage = $"{pokemon.Name.ToUpperInvariant()} MOVED IN";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Removes a resident from the active bed and sets that Pokemon back to unclaimed mode.
    private void UnassignPokemonFromActiveBed(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || _talkState.ActiveBuilding.Definition != ItemCatalog.Bed)
        {
            return;
        }

        int bedIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (bedIndex < 0)
        {
            return;
        }

        PlacedItem bed = _placedItems[bedIndex];
        if (!HasBedResident(bed, pokemonId))
        {
            _talkState.SetText("POKEMON NOT IN THIS BED");
            return;
        }

        _placedItems[bedIndex] = RemoveResidentFromBed(bed, pokemonId);
        UnclaimPokemon(pokemonId);

        _interactTarget = _placedItems[bedIndex];
        _talkState.UpdateBuildingReference(_placedItems[bedIndex]);
        _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[bedIndex]));
        _talkState.SetText("POKEMON UNASSIGNED");
        _interactionMessage = "POKEMON UNASSIGNED";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Assigns a Pokemon to a production building after validation and synchronizes both building and worker state.
    private bool AssignPokemonToResourceBuilding(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || !_talkState.ActiveBuilding.Definition.IsResourceProduction)
        {
            return false;
        }

        int pokemonIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (pokemonIndex < 0)
        {
            return false;
        }

        SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
        PlacedItem building = _talkState.ActiveBuilding;

        ResourceAssignmentFailureReason assignmentFailureReason = GetResourceAssignmentFailureReason(pokemon, building);
        if (assignmentFailureReason != ResourceAssignmentFailureReason.None)
        {
            ShowAssignmentFailureDialogue(pokemon, GetResourceAssignmentFailureDialogueText(assignmentFailureReason));
            return false;
        }

        ClearExistingWorkBuildingForPokemon(pokemon.PokemonId);

        int buildingIndex = _placedItems.FindIndex(item => item == building);
        if (buildingIndex < 0)
        {
            return false;
        }

        _placedItems[buildingIndex] = AddWorkerToBuilding(_placedItems[buildingIndex], pokemon);

        _spawnedDittos[pokemonIndex] = pokemon with
        {
            IsAssignedToWork = true,
            IsWorking = false,
            IsFollowingPlayer = false,
            IsMoving = false,
            MoveTimeRemaining = 0f,
            MoveCooldownRemaining = 0f,
            MoveTarget = pokemon.Position,
            IdleAnimationFrame = 0,
            IdleAnimationTimer = 0f,
            IdleCyclePauseRemaining = 0f
        };

        _interactTarget = _placedItems[buildingIndex];
        _talkState.UpdateBuildingReference(_placedItems[buildingIndex]);
        _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[buildingIndex]));

        _talkState.SetText($"{pokemon.Name.ToUpperInvariant()} WILL WORK AT {building.Definition.Name.ToUpperInvariant()}");
        _interactionMessage = $"{pokemon.Name.ToUpperInvariant()} ASSIGNED TO {building.Definition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
        return true;
    }

    // Unassigns a worker from the active production building and safely resets worker runtime flags/position.
    private void UnassignPokemonFromActiveResourceBuilding(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || !_talkState.ActiveBuilding.Definition.IsResourceProduction)
        {
            return;
        }

        int buildingIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (buildingIndex < 0)
        {
            return;
        }

        PlacedItem building = _placedItems[buildingIndex];
        if (!HasWorker(building, pokemonId))
        {
            _talkState.SetText("NO WORKER ASSIGNED");
            return;
        }

        int workerIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (workerIndex >= 0)
        {
            SpawnedPokemon worker = _spawnedDittos[workerIndex];
            bool shouldRespawnFromBuilding = worker.IsWorking;
            if (shouldRespawnFromBuilding)
            {
                Vector2 respawnPosition = GetWorkerRespawnPosition(building);
                _spawnedDittos[workerIndex] = worker with
                {
                    IsAssignedToWork = false,
                    IsWorking = false,
                    IsMoving = false,
                    MoveTimeRemaining = 0f,
                    MoveCooldownRemaining = GetRandomMoveDelaySeconds(),
                    MoveTarget = respawnPosition,
                    Position = respawnPosition
                };
            }
            else
            {
                _spawnedDittos[workerIndex] = worker with
                {
                    IsAssignedToWork = false,
                    IsWorking = false
                };
            }
        }

        _placedItems[buildingIndex] = RemoveWorkerFromBuilding(building, pokemonId);

        _interactTarget = _placedItems[buildingIndex];
        _talkState.UpdateBuildingReference(_placedItems[buildingIndex]);
        _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[buildingIndex]));
        _talkState.SetText("WORKER UNASSIGNED");
        _interactionMessage = "WORKER UNASSIGNED";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Transfers stored production from the active building into inventory, then clears the building's stored units.
    private void CollectProducedMaterialsFromActiveBuilding()
    {
        if (_talkState.ActiveBuilding is null)
        {
            return;
        }

        int buildingIndex = _placedItems.FindIndex(item => item == _talkState.ActiveBuilding);
        if (buildingIndex < 0)
        {
            return;
        }

        PlacedItem building = _placedItems[buildingIndex];
        ItemDefinition? producedMaterial = GetProducedMaterialForBuilding(building);
        if (!building.Definition.IsResourceProduction ||
            producedMaterial is null ||
            building.StoredProducedUnits <= 0)
        {
            _talkState.SetText("NOTHING TO COLLECT");
            return;
        }

        if (!CanAddInventoryItem(producedMaterial))
        {
            _talkState.SetText("INVENTORY FULL");
            _interactionMessage = "INVENTORY FULL";
            _interactionMessageTimer = InteractionMessageDuration;
            return;
        }

        AddInventoryItem(producedMaterial, building.StoredProducedUnits);
        _placedItems[buildingIndex] = building with
        {
            StoredProducedUnits = 0
        };

        _interactTarget = _placedItems[buildingIndex];
        _talkState.UpdateBuildingReference(_placedItems[buildingIndex]);
        _talkState.SetOptions(GetBuildingTalkOptions(_placedItems[buildingIndex]));
        _talkState.SetText($"COLLECTED {producedMaterial.Name.ToUpperInvariant()} X{building.StoredProducedUnits}");
        _interactionMessage = $"COLLECTED {producedMaterial.Name.ToUpperInvariant()} X{building.StoredProducedUnits}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

    // Enforces one active job per Pokemon by removing prior work assignments before applying a new one.
    private void ClearExistingWorkBuildingForPokemon(int pokemonId)
    {
        for (int index = 0; index < _placedItems.Count; index++)
        {
            PlacedItem item = _placedItems[index];
            if (!HasWorker(item, pokemonId))
            {
                continue;
            }

            _placedItems[index] = RemoveWorkerFromBuilding(item, pokemonId);
        }

        int workerIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (workerIndex >= 0)
        {
            SpawnedPokemon worker = _spawnedDittos[workerIndex];
            _spawnedDittos[workerIndex] = worker with
            {
                IsAssignedToWork = false,
                IsWorking = false
            };
        }
    }

    // Computes and returns resource Assignment Failure Reason without mutating persistent game state.
    private ResourceAssignmentFailureReason GetResourceAssignmentFailureReason(SpawnedPokemon pokemon, PlacedItem building)
    {
        if (!building.Definition.IsResourceProduction)
        {
            return ResourceAssignmentFailureReason.InvalidBuilding;
        }

        if (pokemon.HomePosition is not Vector2)
        {
            return ResourceAssignmentFailureReason.NoBed;
        }

        if (!IsBuildingExitWithinPokemonBedRange(pokemon, building))
        {
            return ResourceAssignmentFailureReason.BedTooFar;
        }

        int requiredSkillLevel = Math.Max(1, building.Definition.RequiredSkillLevel);
        if (building.Definition == ItemCatalog.Farm)
        {
            int plantingLevel = pokemon.GetSkillLevel(SkillType.Planting);
            int wateringLevel = pokemon.GetSkillLevel(SkillType.Water);
            int harvestingLevel = pokemon.GetSkillLevel(SkillType.Harvesting);
            int bestFarmSkillLevel = Math.Max(plantingLevel, Math.Max(wateringLevel, harvestingLevel));
            if (bestFarmSkillLevel <= 0)
            {
                return ResourceAssignmentFailureReason.SkillMismatch;
            }

            if (bestFarmSkillLevel < requiredSkillLevel)
            {
                return ResourceAssignmentFailureReason.SkillTooLow;
            }
        }
        else if (building.Definition.RequiredSkill != SkillType.None)
        {
            SkillType requiredSkill = building.Definition.RequiredSkill;
            int pokemonSkillLevel = pokemon.GetSkillLevel(requiredSkill);
            if (pokemonSkillLevel <= 0)
            {
                return ResourceAssignmentFailureReason.SkillMismatch;
            }

            if (pokemonSkillLevel < requiredSkillLevel)
            {
                return ResourceAssignmentFailureReason.SkillTooLow;
            }
        }

        if (pokemon.IsAssignedToWork || pokemon.IsWorking)
        {
            return ResourceAssignmentFailureReason.AlreadyWorking;
        }

        if (GetWorkerPokemonIds(building).Count >= Math.Max(1, building.Definition.MaxWorkers))
        {
            return ResourceAssignmentFailureReason.BuildingFull;
        }

        if (HasWorker(building, pokemon.PokemonId))
        {
            return ResourceAssignmentFailureReason.AlreadyAssignedHere;
        }

        return ResourceAssignmentFailureReason.None;
    }

    // Computes and returns resource Assignment Failure Dialogue Text without mutating persistent game state.
    private static string GetResourceAssignmentFailureDialogueText(ResourceAssignmentFailureReason failureReason)
    {
        return failureReason switch
        {
            ResourceAssignmentFailureReason.BedTooFar => "MY BED IS TOO FAR.",
            ResourceAssignmentFailureReason.SkillMismatch => "I DONT KNOW HOW TO WORK HERE.",
            ResourceAssignmentFailureReason.SkillTooLow => "I DONT KNOW HOW TO WORK HERE.",
            ResourceAssignmentFailureReason.BuildingFull => "THERE IS NO ROOM FOR ME TO WORK HERE.",
            _ => "I CANT WORK HERE RIGHT NOW."
        };
    }

    // Uses the building exit point for range checks so workers can reliably travel between bed and job site.
    private static bool IsBuildingExitWithinPokemonBedRange(SpawnedPokemon pokemon, PlacedItem building)
    {
        if (pokemon.HomePosition is not Vector2 homePosition)
        {
            return false;
        }

        Rectangle exitBounds = GetResourceBuildingExitBounds(building);
        if (exitBounds.IsEmpty)
        {
            return false;
        }

        return HasMinimumBedRangeCoverage(homePosition, exitBounds);
    }

    // Checks whether at least one tenth of exit-space pixels can be considered inside bed range.
    private static bool HasMinimumBedRangeCoverage(Vector2 homePosition, Rectangle exitBounds)
    {
        int totalPixels = exitBounds.Width * exitBounds.Height;
        if (totalPixels <= 0)
        {
            return false;
        }

        float radiusSquared = HomeWanderRadius * HomeWanderRadius;
        int insidePixels = 0;
        for (int y = exitBounds.Top; y < exitBounds.Bottom; y++)
        {
            for (int x = exitBounds.Left; x < exitBounds.Right; x++)
            {
                Vector2 candidateTopLeft = new(x - (PlayerSize / 2f), y - (PlayerSize / 2f));
                if (Vector2.DistanceSquared(homePosition, candidateTopLeft) <= radiusSquared)
                {
                    insidePixels++;
                }
            }
        }

        return insidePixels * 10 >= totalPixels;
    }

    // Resolves what item a building should output, including farm-specific crop overrides.
    private static ItemDefinition? GetProducedMaterialForBuilding(PlacedItem building)
    {
        if (!building.Definition.IsResourceProduction)
        {
            return null;
        }

        if (building.Definition == ItemCatalog.Farm)
        {
            if (building.FarmGrowingPlant is null || building.FarmGrowingPlant == ItemCatalog.NoBerry)
            {
                return null;
            }

            return building.FarmGrowingPlant;
        }

        return building.Definition.ProducedMaterial;
    }

    // Failure reasons used when validating Pokemon assignment to resource buildings.
    private enum ResourceAssignmentFailureReason
    {
        None,
        InvalidBuilding,
        NoBed,
        BedTooFar,
        SkillMismatch,
        SkillTooLow,
        AlreadyWorking,
        BuildingFull,
        AlreadyAssignedHere
    }
}
