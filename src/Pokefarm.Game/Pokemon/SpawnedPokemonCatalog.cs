namespace Pokefarm.Game;

internal sealed record SpawnedPokemonDefinition(
    string Name,
    IReadOnlyDictionary<SkillType, int> SkillLevels);

internal static class SpawnedPokemonCatalog
{
    private static readonly IReadOnlyDictionary<SkillType, int> NoSkills = new Dictionary<SkillType, int>();

    private static readonly Dictionary<string, SpawnedPokemonDefinition> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sewaddle"] = new SpawnedPokemonDefinition(
            "Sewaddle",
            CreateSkillLevels((SkillType.Lumber, 1), (SkillType.Farming, 1))),
        ["Azurill"] = new SpawnedPokemonDefinition(
            "Azurill",
            CreateSkillLevels((SkillType.Farming, 1))),
        ["Sunkern"] = new SpawnedPokemonDefinition("Sunkern", NoSkills),
        ["Pidgey"] = new SpawnedPokemonDefinition("Pidgey", NoSkills),
        ["Cleffa"] = new SpawnedPokemonDefinition("Cleffa", NoSkills),
        ["Igglybuff"] = new SpawnedPokemonDefinition("Igglybuff", NoSkills),
        ["Kricketot"] = new SpawnedPokemonDefinition("Kricketot", NoSkills),
        ["Pichu"] = new SpawnedPokemonDefinition("Pichu", NoSkills),
        ["Togepi"] = new SpawnedPokemonDefinition("Togepi", NoSkills),
        ["Caterpie"] = new SpawnedPokemonDefinition("Caterpie", NoSkills),
        ["Poliwag"] = new SpawnedPokemonDefinition("Poliwag", NoSkills),
        ["Hoppip"] = new SpawnedPokemonDefinition("Hoppip", NoSkills),
        ["Tyrogue"] = new SpawnedPokemonDefinition("Tyrogue", CreateSkillLevels((SkillType.Crafting, 1))),
        ["Smoochum"] = new SpawnedPokemonDefinition("Smoochum", NoSkills),
        ["Magby"] = new SpawnedPokemonDefinition("Magby", NoSkills),
        ["Nincada"] = new SpawnedPokemonDefinition("Nincada", NoSkills),
        ["Elekid"] = new SpawnedPokemonDefinition("Elekid", CreateSkillLevels((SkillType.Crafting, 1)))
    };

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
