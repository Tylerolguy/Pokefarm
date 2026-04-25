using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Static helper for building Worker Helpers logic shared across the game loop.
internal static class BuildingWorkerHelpers
{
    // Computes and returns resource Building Exit Bounds without mutating persistent game state.
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

    // Computes and returns worker Respawn Position without mutating persistent game state.
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

    // Computes and returns pokemon Effort Per Second without mutating persistent game state.
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

    // Computes and returns worker Pokemon Ids without mutating persistent game state.
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

    // Computes and returns worker Pokemon Names without mutating persistent game state.
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

    // Computes and returns bed resident Pokemon Ids without mutating persistent game state.
    public static List<int> GetBedResidentPokemonIds(PlacedItem bed)
    {
        List<int> residentIds = [];
        if (bed.ResidentPokemonId.HasValue)
        {
            residentIds.Add(bed.ResidentPokemonId.Value);
        }

        if (bed.ResidentPokemonId2.HasValue)
        {
            residentIds.Add(bed.ResidentPokemonId2.Value);
        }

        if (bed.ResidentPokemonId3.HasValue)
        {
            residentIds.Add(bed.ResidentPokemonId3.Value);
        }

        return residentIds;
    }

    // Computes and returns bed resident Pokemon Names without mutating persistent game state.
    public static List<string> GetBedResidentPokemonNames(PlacedItem bed)
    {
        List<string> residentNames = [];
        if (!string.IsNullOrEmpty(bed.ResidentPokemonName))
        {
            residentNames.Add(bed.ResidentPokemonName);
        }

        if (!string.IsNullOrEmpty(bed.ResidentPokemonName2))
        {
            residentNames.Add(bed.ResidentPokemonName2);
        }

        if (!string.IsNullOrEmpty(bed.ResidentPokemonName3))
        {
            residentNames.Add(bed.ResidentPokemonName3);
        }

        return residentNames;
    }

    // Checks whether bed has capacity available for another resident.
    public static bool IsBedFull(PlacedItem bed)
    {
        int capacity = Math.Max(1, bed.Definition.BedCapacity);
        return GetBedResidentPokemonIds(bed).Count >= capacity;
    }

    // Checks whether this Pokemon currently lives in the bed.
    public static bool HasBedResident(PlacedItem bed, int pokemonId)
    {
        return bed.ResidentPokemonId == pokemonId ||
               bed.ResidentPokemonId2 == pokemonId ||
               bed.ResidentPokemonId3 == pokemonId;
    }

    // Adds a resident to the first available bed slot.
    public static PlacedItem AddResidentToBed(PlacedItem bed, SpawnedPokemon pokemon)
    {
        if (!bed.ResidentPokemonId.HasValue)
        {
            return bed with { ResidentPokemonId = pokemon.PokemonId, ResidentPokemonName = pokemon.Name };
        }

        if (!bed.ResidentPokemonId2.HasValue)
        {
            return bed with { ResidentPokemonId2 = pokemon.PokemonId, ResidentPokemonName2 = pokemon.Name };
        }

        if (!bed.ResidentPokemonId3.HasValue)
        {
            return bed with { ResidentPokemonId3 = pokemon.PokemonId, ResidentPokemonName3 = pokemon.Name };
        }

        return bed;
    }

    // Removes a resident from whichever bed slot currently contains it.
    public static PlacedItem RemoveResidentFromBed(PlacedItem bed, int pokemonId)
    {
        if (bed.ResidentPokemonId == pokemonId)
        {
            return bed with { ResidentPokemonId = null, ResidentPokemonName = null };
        }

        if (bed.ResidentPokemonId2 == pokemonId)
        {
            return bed with { ResidentPokemonId2 = null, ResidentPokemonName2 = null };
        }

        if (bed.ResidentPokemonId3 == pokemonId)
        {
            return bed with { ResidentPokemonId3 = null, ResidentPokemonName3 = null };
        }

        return bed;
    }

    // Checks whether worker is currently true for the active world state.
    public static bool HasWorker(PlacedItem building, int pokemonId)
    {
        return building.WorkerPokemonId == pokemonId ||
               building.WorkerPokemonId2 == pokemonId ||
               building.WorkerPokemonId3 == pokemonId;
    }

    // Adds worker To Building and updates related collections/counters to stay consistent.
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

    // Removes worker From Building and reconciles dependent state.
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

    // Computes and returns production Step Label without mutating persistent game state.
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
