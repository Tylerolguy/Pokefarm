namespace Pokefarm.Game;

internal static class SnackSpawnCatalog
{
    private static readonly Dictionary<ItemDefinition, IReadOnlyList<WeightedPokemonSpawn>> SpawnBuckets = new()
    {
        [ItemCatalog.BasicSnack] =
        [
            new WeightedPokemonSpawn("Sewaddle", 10f),
            new WeightedPokemonSpawn("Azurill", 1f),
            new WeightedPokemonSpawn("Sunkern", 1f),
            new WeightedPokemonSpawn("Pidgey", 1f),
            new WeightedPokemonSpawn("Cleffa", 1f),
            new WeightedPokemonSpawn("Igglybuff", 1f),
            new WeightedPokemonSpawn("Kricketot", 1f)

        ],
        
        [ItemCatalog.BasicSnack2] =
        [
            new WeightedPokemonSpawn("Pichu", 1f),
            new WeightedPokemonSpawn("Togepi", 1f),
            new WeightedPokemonSpawn("Caterpie", 1f),
            new WeightedPokemonSpawn("Poliwag", 1f),
            new WeightedPokemonSpawn("Hoppip", 1f),
            new WeightedPokemonSpawn("Tyrogue", 1f),
            new WeightedPokemonSpawn("Smoochum", 1f),
            new WeightedPokemonSpawn("Magby", 1f),
            new WeightedPokemonSpawn("Nincada", 1f),
            new WeightedPokemonSpawn("Elekid", 1f)


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
