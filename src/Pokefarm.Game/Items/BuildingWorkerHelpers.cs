using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal static class BuildingWorkerHelpers
{
    public static Rectangle GetResourceBuildingExitBounds(PlacedItem building)
    {
        if (!building.Definition.IsBuildingLike || building.Definition.ExitSize.X <= 0 || building.Definition.ExitSize.Y <= 0)
        {
            return Rectangle.Empty;
        }

        Point size = building.Definition.ExitSize;
        return new Rectangle(
            building.Bounds.Center.X - (size.X / 2),
            building.Bounds.Bottom + 2,
            size.X,
            size.Y);
    }

    public static Vector2 GetWorkerRespawnPosition(PlacedItem building, int playerSize = 32)
    {
        Rectangle exitBounds = GetResourceBuildingExitBounds(building);
        if (!exitBounds.IsEmpty)
        {
            return new Vector2(exitBounds.X, exitBounds.Y);
        }

        return new Vector2(
            building.Bounds.Center.X - (playerSize / 2f),
            building.Bounds.Bottom + 4f);
    }

    public static float GetPokemonEffortPerSecond(SpawnedPokemon pokemon, ItemDefinition buildingDefinition)
    {
        SkillType requiredSkill = buildingDefinition.RequiredSkill;
        if (requiredSkill == SkillType.None)
        {
            return 1f;
        }

        int skillLevel = pokemon.GetSkillLevel(requiredSkill);
        if (skillLevel <= 0)
        {
            return 0f;
        }

        return skillLevel;
    }

    public static List<int> GetWorkerPokemonIds(PlacedItem building)
    {
        List<int> workerIds = [];
        if (building.WorkerPokemonId.HasValue)
        {
            workerIds.Add(building.WorkerPokemonId.Value);
        }

        if (building.WorkerPokemonId2.HasValue)
        {
            workerIds.Add(building.WorkerPokemonId2.Value);
        }

        if (building.WorkerPokemonId3.HasValue)
        {
            workerIds.Add(building.WorkerPokemonId3.Value);
        }

        return workerIds;
    }

    public static List<string> GetWorkerPokemonNames(PlacedItem building)
    {
        List<string> workerNames = [];
        if (!string.IsNullOrEmpty(building.WorkerPokemonName))
        {
            workerNames.Add(building.WorkerPokemonName);
        }

        if (!string.IsNullOrEmpty(building.WorkerPokemonName2))
        {
            workerNames.Add(building.WorkerPokemonName2);
        }

        if (!string.IsNullOrEmpty(building.WorkerPokemonName3))
        {
            workerNames.Add(building.WorkerPokemonName3);
        }

        return workerNames;
    }

    public static bool HasWorker(PlacedItem building, int pokemonId)
    {
        return building.WorkerPokemonId == pokemonId ||
               building.WorkerPokemonId2 == pokemonId ||
               building.WorkerPokemonId3 == pokemonId;
    }

    public static PlacedItem AddWorkerToBuilding(PlacedItem building, SpawnedPokemon pokemon)
    {
        if (!building.WorkerPokemonId.HasValue)
        {
            return building with { WorkerPokemonId = pokemon.PokemonId, WorkerPokemonName = pokemon.Name };
        }

        if (!building.WorkerPokemonId2.HasValue)
        {
            return building with { WorkerPokemonId2 = pokemon.PokemonId, WorkerPokemonName2 = pokemon.Name };
        }

        if (!building.WorkerPokemonId3.HasValue)
        {
            return building with { WorkerPokemonId3 = pokemon.PokemonId, WorkerPokemonName3 = pokemon.Name };
        }

        return building;
    }

    public static PlacedItem RemoveWorkerFromBuilding(PlacedItem building, int pokemonId)
    {
        if (building.WorkerPokemonId == pokemonId)
        {
            return building with { WorkerPokemonId = null, WorkerPokemonName = null };
        }

        if (building.WorkerPokemonId2 == pokemonId)
        {
            return building with { WorkerPokemonId2 = null, WorkerPokemonName2 = null };
        }

        if (building.WorkerPokemonId3 == pokemonId)
        {
            return building with { WorkerPokemonId3 = null, WorkerPokemonName3 = null };
        }

        return building;
    }

    public static string GetProductionStepLabel(PlacedItem building)
    {
        if (building.Definition == ItemCatalog.Farm)
        {
            return building.ProductionStepIndex switch
            {
                0 => "PLANTING",
                1 => "WATERING",
                _ => "HARVESTING"
            };
        }

        return "PROGRESS";
    }
}
