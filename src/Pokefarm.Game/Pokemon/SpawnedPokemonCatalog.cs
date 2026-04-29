namespace Pokefarm.Game;

// Data container used to pass spawned Pokemon Definition information between game systems.
internal sealed record SpawnedPokemonDefinition(
    string Name,
    IReadOnlyDictionary<SkillType, int> SkillLevels);

// Static helper for spawned Pokemon Catalog logic shared across the game loop.
internal static class SpawnedPokemonCatalog
{
    private static readonly IReadOnlyDictionary<SkillType, int> NoSkills = new Dictionary<SkillType, int>();

    private static readonly Dictionary<string, SpawnedPokemonDefinition> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sewaddle"] = new SpawnedPokemonDefinition(
            "Sewaddle",
            CreateSkillLevels((SkillType.Lumber, 1), (SkillType.Farming, 1), (SkillType.Water, 1), 
            (SkillType.Planting, 1), (SkillType.Harvesting, 1), (SkillType.Construction, 1), (SkillType.Transport, 1))),
        ["Azurill"] = new SpawnedPokemonDefinition(
            "Azurill",
            CreateSkillLevels((SkillType.Farming, 1))),
        ["Sunkern"] = new SpawnedPokemonDefinition("Sunkern", CreateSkillLevels((SkillType.Planting, 1))),
        ["Pidgey"] = new SpawnedPokemonDefinition("Pidgey", CreateSkillLevels((SkillType.Transport, 1))),
        ["Cleffa"] = new SpawnedPokemonDefinition("Cleffa", NoSkills),
        ["Igglybuff"] = new SpawnedPokemonDefinition("Igglybuff", NoSkills),
        ["Kricketot"] = new SpawnedPokemonDefinition("Kricketot", NoSkills),
        ["Pichu"] = new SpawnedPokemonDefinition("Pichu", NoSkills),
        ["Togepi"] = new SpawnedPokemonDefinition("Togepi", NoSkills),
        ["Caterpie"] = new SpawnedPokemonDefinition("Caterpie", NoSkills),
        ["Poliwag"] = new SpawnedPokemonDefinition("Poliwag", NoSkills),
        ["Hoppip"] = new SpawnedPokemonDefinition("Hoppip", NoSkills),
        ["Tyrogue"] = new SpawnedPokemonDefinition("Tyrogue", CreateSkillLevels((SkillType.Crafting, 1), (SkillType.Construction, 1))),
        ["Smoochum"] = new SpawnedPokemonDefinition("Smoochum", NoSkills),
        ["Magby"] = new SpawnedPokemonDefinition("Magby", NoSkills),
        ["Nincada"] = new SpawnedPokemonDefinition("Nincada", NoSkills),
        ["Elekid"] = new SpawnedPokemonDefinition("Elekid", CreateSkillLevels((SkillType.Crafting, 1), (SkillType.Construction, 1))),
        ["Noibat"] = new SpawnedPokemonDefinition("Noibat", CreateSkillLevels((SkillType.Harvesting, 1))),
        ["Dratini"] = new SpawnedPokemonDefinition("Dratini", CreateSkillLevels((SkillType.Water, 1), (SkillType.Transport, 2)))
    };

    // Computes and returns or Default without mutating persistent game state.
    public static SpawnedPokemonDefinition GetOrDefault(string pokemonName)
    {
        if (Entries.TryGetValue(pokemonName, out SpawnedPokemonDefinition? definition))
        {
            return definition;
        }

        return new SpawnedPokemonDefinition(pokemonName, NoSkills);
    }

    private static IReadOnlyDictionary<SkillType, int> CreateSkillLevels(params (SkillType SkillType, int Level)[] values)
    {
        Dictionary<SkillType, int> levels = new(values.Length);
        foreach ((SkillType skillType, int level) in values)
        {
            if (skillType == SkillType.None || level <= 0)
            {
                continue;
            }

            levels[skillType] = level;
        }

        return levels;
    }
}
