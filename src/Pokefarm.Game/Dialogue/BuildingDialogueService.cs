namespace Pokefarm.Game;

// Builds context-aware building dialogue text/options based on nearby Pokemon and current building state.
internal static class BuildingDialogueService
{
    // Generates the opening prompt shown when the player starts interacting with a building.
    public static string GetOpeningText(PlacedItem building)
    {
        return $"WHAT SHOULD I DO WITH THIS {building.Definition.Name.ToUpperInvariant()}";
    }

    // Produces the full interaction menu for the active building, including assignment, collection, and navigation actions.
    public static List<PokemonDialogueOption> GetOptions(
        PlacedItem building,
        IReadOnlyList<SpawnedPokemon> spawnedPokemon,
        Func<SpawnedPokemon, PlacedItem, bool> isWorkbenchWithinBedRange,
        Func<PlacedItem, ItemDefinition?> getProducedMaterialForBuilding)
    {
        List<PokemonDialogueOption> options = [];
        if (building.Definition == ItemCatalog.Bed)
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
            if (WorkbenchCraftingHelpers.IsWorkbenchItemReady(building))
            {
                options.Add(new PokemonDialogueOption("PICKUP ITEMS", PokemonDialogueAction.CollectWorkbenchItem));
            }

            options.Add(new PokemonDialogueOption("QUEUE ITEMS", PokemonDialogueAction.OpenWorkbenchQueue));
            if (building.WorkbenchQueuedItem is not null && !WorkbenchCraftingHelpers.IsWorkbenchItemReady(building))
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
}
