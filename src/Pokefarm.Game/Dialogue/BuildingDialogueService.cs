namespace Pokefarm.Game;

// Builds context-aware building dialogue text/options based on nearby Pokemon and current building state.
internal static class BuildingDialogueService
{
    // Generates the opening prompt shown when the player starts interacting with a building.
    public static string GetOpeningText(PlacedItem building)
    {
        if (building.IsConstructionSite)
        {
            return $"THIS IS A {building.Definition.Name.ToUpperInvariant()} CONSTRUCTION SITE";
        }

        return $"WHAT SHOULD I DO WITH THIS {building.Definition.Name.ToUpperInvariant()}";
    }

    // Produces the full interaction menu for the active building, including assignment, collection, and navigation actions.
    public static List<PokemonDialogueOption> GetOptions(
        PlacedItem building,
        IReadOnlyList<SpawnedPokemon> spawnedPokemon,
        Func<SpawnedPokemon, PlacedItem, bool> isWorkbenchWithinBedRange,
        Func<PlacedItem, ItemDefinition?> getProducedMaterialForBuilding,
        Func<PlacedItem, bool> isDittoWorkingAtBuilding)
    {
        List<PokemonDialogueOption> options = [];
        if (building.IsConstructionSite)
        {
            if (isDittoWorkingAtBuilding(building))
            {
                options.Add(new PokemonDialogueOption("STOP WORKING", PokemonDialogueAction.StopDittoWork));
            }
            else
            {
                options.Add(new PokemonDialogueOption("WORK HERE", PokemonDialogueAction.StartDittoWork));
            }

            foreach (SpawnedPokemon pokemon in spawnedPokemon.Where(pokemon =>
                         pokemon.AssignedConstructionSiteId == building.ConstructionSiteId))
            {
                options.Add(new PokemonDialogueOption(
                    $"UNASSIGN {pokemon.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.UnassignConstructionWorker,
                    TargetPokemonId: pokemon.PokemonId));
            }

            foreach (SpawnedPokemon pokemon in spawnedPokemon)
            {
                if (!pokemon.IsFollowingPlayer || !CanPokemonHelpConstruction(pokemon, building))
                {
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"ASSIGN {pokemon.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.AssignConstructionWorker,
                    TargetPokemonId: pokemon.PokemonId));
            }

            string requirementsSummary = GetConstructionRequirementSummary(building);
            options.Add(new PokemonDialogueOption(
                $"NEEDS {requirementsSummary}",
                PokemonDialogueAction.None,
                $"REQUIRES: {requirementsSummary}"));
        }
        else if (building.Definition == ItemCatalog.Bed)
        {
            foreach (int residentPokemonId in BuildingWorkerHelpers.GetBedResidentPokemonIds(building))
            {
                SpawnedPokemon? resident = spawnedPokemon.FirstOrDefault(pokemon => pokemon.PokemonId == residentPokemonId);
                if (resident is null)
                {
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"UNASSIGN {resident.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.UnassignBedResident,
                    TargetPokemonId: resident.PokemonId));
            }

            bool bedFull = BuildingWorkerHelpers.IsBedFull(building);
            foreach (SpawnedPokemon pokemon in spawnedPokemon)
            {
                if (!pokemon.IsFollowingPlayer)
                {
                    continue;
                }

                if (bedFull && !BuildingWorkerHelpers.HasBedResident(building, pokemon.PokemonId))
                {
                    options.Add(new PokemonDialogueOption(
                        "THIS BED IS FULL",
                        PokemonDialogueAction.None,
                        "THIS BED IS FULL"));
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"SET {pokemon.Name.ToUpperInvariant()} HOME",
                    PokemonDialogueAction.SetHome,
                    TargetPokemonId: pokemon.PokemonId));
            }
        }
        else if (building.Definition == ItemCatalog.WorkBench)
        {
            if (isDittoWorkingAtBuilding(building))
            {
                options.Add(new PokemonDialogueOption("STOP WORKING", PokemonDialogueAction.StopDittoWork));
            }
            else
            {
                options.Add(new PokemonDialogueOption("WORK HERE", PokemonDialogueAction.StartDittoWork));
            }

            if (WorkbenchCraftingHelpers.HasWorkbenchStoredItems(building))
            {
                options.Add(new PokemonDialogueOption("PICKUP ITEMS", PokemonDialogueAction.CollectWorkbenchItem));
            }

            options.Add(new PokemonDialogueOption("QUEUE ITEMS", PokemonDialogueAction.OpenWorkbenchQueue));
            if (WorkbenchCraftingHelpers.HasWorkbenchQueuedItems(building))
            {
                options.Add(new PokemonDialogueOption("DEQUEUE ITEMS", PokemonDialogueAction.DequeueWorkbenchItem));
            }

            foreach (SpawnedPokemon pokemon in spawnedPokemon)
            {
                if (!pokemon.IsFollowingPlayer || pokemon.GetSkillLevel(SkillType.Crafting) <= 0)
                {
                    continue;
                }

                if (!isWorkbenchWithinBedRange(pokemon, building))
                {
                    options.Add(new PokemonDialogueOption(
                        $"{pokemon.Name.ToUpperInvariant()} BED TO FAR.",
                        PokemonDialogueAction.None));
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"ASSIGN {pokemon.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.AssignWorkbenchWorker,
                    TargetPokemonId: pokemon.PokemonId));
            }

            if (building.WorkerPokemonId.HasValue)
            {
                options.Add(new PokemonDialogueOption(
                    $"UNASSIGN {building.WorkerPokemonName?.ToUpperInvariant() ?? "WORKER"}",
                    PokemonDialogueAction.UnassignWorkbenchWorker,
                    TargetPokemonId: building.WorkerPokemonId.Value));
            }
        }
        else if (building.Definition == ItemCatalog.Pc)
        {
            options.Add(new PokemonDialogueOption("ACCESS QUESTS", PokemonDialogueAction.OpenPcQuests));
            options.Add(new PokemonDialogueOption("CHECK LEVEL", PokemonDialogueAction.OpenPcLevel));
            options.Add(new PokemonDialogueOption("ACCESS PC", PokemonDialogueAction.OpenPcStorage));
            options.Add(new PokemonDialogueOption("SAVE GAME", PokemonDialogueAction.SaveGame));

            foreach (SpawnedPokemon pokemon in spawnedPokemon)
            {
                if (!pokemon.IsFollowingPlayer)
                {
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"STORE {pokemon.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.StorePokemonInPc,
                    TargetPokemonId: pokemon.PokemonId));
            }
        }
        else if (building.Definition == ItemCatalog.DungeonPortal)
        {
            options.Add(new PokemonDialogueOption("ACCESS DUNGEONS", PokemonDialogueAction.OpenDungeonMenu));
        }
        else if (building.Definition.IsResourceProduction)
        {
            if (isDittoWorkingAtBuilding(building))
            {
                options.Add(new PokemonDialogueOption("STOP WORKING", PokemonDialogueAction.StopDittoWork));
            }
            else
            {
                options.Add(new PokemonDialogueOption("WORK HERE", PokemonDialogueAction.StartDittoWork));
            }

            if (building.Definition == ItemCatalog.Farm)
            {
                options.Add(new PokemonDialogueOption("GROW PLANTS", PokemonDialogueAction.OpenFarmGrowingMenu));
            }

            foreach (SpawnedPokemon pokemon in spawnedPokemon)
            {
                if (!pokemon.IsFollowingPlayer)
                {
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"ASSIGN {pokemon.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.AssignResourceWork,
                    TargetPokemonId: pokemon.PokemonId));
            }

            foreach (int workerPokemonId in BuildingWorkerHelpers.GetWorkerPokemonIds(building))
            {
                SpawnedPokemon? worker = spawnedPokemon.FirstOrDefault(pokemon => pokemon.PokemonId == workerPokemonId);
                if (worker is null)
                {
                    continue;
                }

                options.Add(new PokemonDialogueOption(
                    $"UNASSIGN {worker.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.UnassignResourceWork,
                    TargetPokemonId: worker.PokemonId));
            }

            ItemDefinition? producedMaterial = getProducedMaterialForBuilding(building);
            if (producedMaterial is not null && building.StoredProducedUnits > 0)
            {
                options.Add(new PokemonDialogueOption(
                    $"TAKE {building.StoredProducedUnits} {producedMaterial.Name.ToUpperInvariant()}",
                    PokemonDialogueAction.CollectProduction));
            }
        }

        options.Add(new PokemonDialogueOption("BYE", PokemonDialogueAction.Exit));
        return options;
    }

    // Checks whether this Pokemon can satisfy at least one required construction skill for the site.
    private static bool CanPokemonHelpConstruction(SpawnedPokemon pokemon, PlacedItem building)
    {
        if (pokemon.AssignedConstructionSiteId == building.ConstructionSiteId)
        {
            return false;
        }

        foreach ((SkillType skillType, int requiredLevel) in GetConstructionSkillRequirements(building.Definition))
        {
            if (pokemon.GetSkillLevel(skillType) >= requiredLevel)
            {
                return true;
            }
        }

        return false;
    }

    // Computes and returns construction Requirement Summary without mutating persistent game state.
    private static string GetConstructionRequirementSummary(PlacedItem building)
    {
        List<string> parts = [];
        foreach ((SkillType skillType, int requiredLevel) in GetConstructionSkillRequirements(building.Definition))
        {
            parts.Add($"{skillType.ToString().ToUpperInvariant()} {requiredLevel}");
        }

        return parts.Count == 0 ? "NONE" : string.Join(", ", parts);
    }

    // Computes and returns construction Skill Requirements without mutating persistent game state.
    private static IEnumerable<(SkillType SkillType, int RequiredLevel)> GetConstructionSkillRequirements(ItemDefinition definition)
    {
        if (definition.ConstructionRequiredSkill1 != SkillType.None && definition.ConstructionRequiredSkillLevel1 > 0)
        {
            yield return (definition.ConstructionRequiredSkill1, definition.ConstructionRequiredSkillLevel1);
        }

        if (definition.ConstructionRequiredSkill2 != SkillType.None && definition.ConstructionRequiredSkillLevel2 > 0)
        {
            yield return (definition.ConstructionRequiredSkill2, definition.ConstructionRequiredSkillLevel2);
        }

        if (definition.ConstructionRequiredSkill3 != SkillType.None && definition.ConstructionRequiredSkillLevel3 > 0)
        {
            yield return (definition.ConstructionRequiredSkill3, definition.ConstructionRequiredSkillLevel3);
        }
    }
}
