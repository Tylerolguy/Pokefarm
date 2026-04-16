namespace Pokefarm.Game;

internal static class SnackSpawnCatalog
{
    private static readonly Dictionary<ItemDefinition, IReadOnlyList<WeightedPokemonSpawn>> SpawnBuckets = new()
    {
        [ItemCatalog.BasicSnack] =
        [
            new WeightedPokemonSpawn("Sewaddle", 1f),
            new WeightedPokemonSpawn("Azurill", 1f)
        ]
    };

    public static string RollSpawnName(ItemDefinition snackDefinition)
    {
        if (!SpawnBuckets.TryGetValue(snackDefinition, out IReadOnlyList<WeightedPokemonSpawn>? bucket) || bucket.Count == 0)
        {
            return "Sewaddle";
        }

        float totalWeight = 0f;
        foreach (WeightedPokemonSpawn entry in bucket)
        {
            totalWeight += Math.Max(0f, entry.Weight);
        }

        if (totalWeight <= 0f)
        {
            return bucket[0].PokemonName;
        }

        float roll = Random.Shared.NextSingle() * totalWeight;
        float runningWeight = 0f;
        foreach (WeightedPokemonSpawn entry in bucket)
        {
            runningWeight += Math.Max(0f, entry.Weight);
            if (roll <= runningWeight)
            {
                return entry.PokemonName;
            }
        }

        return bucket[^1].PokemonName;
    }
}

internal readonly record struct WeightedPokemonSpawn(string PokemonName, float Weight);
