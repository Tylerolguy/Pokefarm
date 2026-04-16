namespace Pokefarm.Game;

internal static class BuildingDialogueService
{
    public static string GetOpeningText(PlacedItem building)
    {
        return $"WHAT SHOULD I DO WITH THIS {building.Definition.Name.ToUpperInvariant()}";
    }

    public static List<PokemonDialogueOption> GetOptions(
        PlacedItem building,
        IReadOnlyList<SpawnedPokemon> spawnedPokemon,
        Func<SpawnedPokemon, PlacedItem, bool> canAssignToResourceBuilding,
        Func<SpawnedPokemon, PlacedItem, bool> isBuildingExitWithinBedRange,
        Func<SpawnedPokemon, PlacedItem, bool> isWorkbenchWithinBedRange)
    {
        List<PokemonDialogueOption> options = [];
        if (building.Definition == ItemCatalog.Bed)
        {
            foreach (SpawnedPokemon pokemon in spawnedPokemon)
            {
                if (!pokemon.IsFollowingPlayer)
                {
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
        else if (building.Definition.IsResourceProduction)
        {
            foreach (SpawnedPokemon pokemon in spawnedPokemon)
            {
                if (!pokemon.IsFollowingPlayer)
                {
                    continue;
                }

                if (!isBuildingExitWithinBedRange(pokemon, building))
                {
                    options.Add(new PokemonDialogueOption(
                        $"{pokemon.Name.ToUpperInvariant()} BED TO FAR.",
                        PokemonDialogueAction.None));
                    continue;
                }

                if (!canAssignToResourceBuilding(pokemon, building))
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

            if (building.Definition.ProducedMaterial is ItemDefinition producedMaterial && building.StoredProducedUnits > 0)
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
