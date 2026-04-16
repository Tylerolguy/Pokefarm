using Microsoft.Xna.Framework;
using static Pokefarm.Game.BuildingWorkerHelpers;

namespace Pokefarm.Game;

public sealed partial class FarmGame
{
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
            IsWorking = false,
            IsFollowingPlayer = false,
            IsMoving = false,
            MoveTimeRemaining = 0f,
            MoveCooldownRemaining = 0f,
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

    private void AssignPokemonToResourceBuilding(int pokemonId)
    {
        if (_talkState.ActiveBuilding is null || !_talkState.ActiveBuilding.Definition.IsResourceProduction)
        {
            return;
        }

        int pokemonIndex = _spawnedDittos.FindIndex(pokemon => pokemon.PokemonId == pokemonId);
        if (pokemonIndex < 0)
        {
            return;
        }

        SpawnedPokemon pokemon = _spawnedDittos[pokemonIndex];
        PlacedItem building = _talkState.ActiveBuilding;

        if (!CanAssignPokemonToResourceBuilding(pokemon, building))
        {
            _talkState.SetText("THIS POKEMON CANT WORK HERE");
            return;
        }

        ClearExistingWorkBuildingForPokemon(pokemon.PokemonId);

        int buildingIndex = _placedItems.FindIndex(item => item == building);
        if (buildingIndex < 0)
        {
            return;
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

        _talkState.SetText($"{pokemon.Name.ToUpperInvariant()} STARTS LUMBER WORK");
        _interactionMessage = $"{pokemon.Name.ToUpperInvariant()} ASSIGNED TO {building.Definition.Name.ToUpperInvariant()}";
        _interactionMessageTimer = InteractionMessageDuration;
    }

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
        if (!building.Definition.IsResourceProduction ||
            building.Definition.ProducedMaterial is not ItemDefinition producedMaterial ||
            building.StoredProducedUnits <= 0)
        {
            _talkState.SetText("NOTHING TO COLLECT");
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

    private bool CanAssignPokemonToResourceBuilding(SpawnedPokemon pokemon, PlacedItem building)
    {
        if (!building.Definition.IsResourceProduction)
        {
            return false;
        }

        if (pokemon.HomePosition is not Vector2 homePosition)
        {
            return false;
        }

        if (!PokemonHasSkillForBuilding(pokemon, building.Definition))
        {
            return false;
        }

        if (pokemon.IsAssignedToWork || pokemon.IsWorking)
        {
            return false;
        }

        if (GetWorkerPokemonIds(building).Count >= Math.Max(1, building.Definition.MaxWorkers))
        {
            return false;
        }

        if (HasWorker(building, pokemon.PokemonId))
        {
            return false;
        }

        Rectangle exitBounds = GetResourceBuildingExitBounds(building);
        if (exitBounds.IsEmpty)
        {
            return false;
        }

        if (!IsBuildingExitWithinPokemonBedRange(pokemon, building))
        {
            return false;
        }

        int pokemonIndex = _spawnedDittos.FindIndex(candidate => candidate.PokemonId == pokemon.PokemonId);
        return CanReachTargetAreaFromPosition(pokemon.Position, exitBounds, pokemonIndex);
    }

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

        Vector2 exitCenter = new(exitBounds.Center.X - (PlayerSize / 2f), exitBounds.Center.Y - (PlayerSize / 2f));
        return Vector2.DistanceSquared(homePosition, exitCenter) <= HomeWanderRadius * HomeWanderRadius;
    }

    private static bool PokemonHasSkillForBuilding(SpawnedPokemon pokemon, ItemDefinition buildingDefinition)
    {
        SkillType requiredSkill = buildingDefinition.RequiredSkill;
        return requiredSkill == SkillType.None || pokemon.GetSkillLevel(requiredSkill) > 0;
    }
}
