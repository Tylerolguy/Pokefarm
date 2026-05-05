namespace Pokefarm.Game;

// Data container used to pass spawned Pokemon Definition information between game systems.
internal sealed record SpawnedPokemonDefinition(
    string Name,
    IReadOnlyDictionary<SkillType, int> SkillLevels,
    PokemonBattleStats BaseStats);

// Static helper for spawned Pokemon Catalog logic shared across the game loop.
internal static class SpawnedPokemonCatalog
{
    private static readonly IReadOnlyDictionary<SkillType, int> NoSkills = new Dictionary<SkillType, int>();

    private static readonly Dictionary<string, SpawnedPokemonDefinition> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sewaddle"] = new SpawnedPokemonDefinition(
            "Sewaddle",
            CreateSkillLevels((SkillType.Lumber, 1), (SkillType.Farming, 1), (SkillType.Water, 1), 
            (SkillType.Planting, 1), (SkillType.Harvesting, 1), (SkillType.Construction, 1), (SkillType.Transport, 1), (SkillType.Teleporting, 1)),
            new PokemonBattleStats(45, 53, 70, 40, 60, 42)),
        ["Azurill"] = new SpawnedPokemonDefinition(
            "Azurill",
            CreateSkillLevels((SkillType.Farming, 1)),
            new PokemonBattleStats(50, 20, 40, 20, 40, 20)),
        ["Sunkern"] = new SpawnedPokemonDefinition("Sunkern", CreateSkillLevels((SkillType.Planting, 1)), new PokemonBattleStats(30, 30, 30, 30, 30, 30)),
        ["Pidgey"] = new SpawnedPokemonDefinition("Pidgey", CreateSkillLevels((SkillType.Transport, 1)), new PokemonBattleStats(40, 45, 40, 35, 35, 56)),
        ["Cleffa"] = new SpawnedPokemonDefinition("Cleffa", NoSkills, new PokemonBattleStats(50, 25, 28, 45, 55, 15)),
        ["Igglybuff"] = new SpawnedPokemonDefinition("Igglybuff", NoSkills, new PokemonBattleStats(90, 30, 15, 40, 20, 15)),
        ["Kricketot"] = new SpawnedPokemonDefinition("Kricketot", NoSkills, new PokemonBattleStats(37, 25, 41, 25, 41, 25)),
        ["Pichu"] = new SpawnedPokemonDefinition("Pichu", NoSkills, new PokemonBattleStats(20, 40, 15, 35, 35, 60)),
        ["Togepi"] = new SpawnedPokemonDefinition("Togepi", NoSkills, new PokemonBattleStats(35, 20, 65, 40, 65, 20)),
        ["Caterpie"] = new SpawnedPokemonDefinition("Caterpie", NoSkills, new PokemonBattleStats(45, 30, 35, 20, 20, 45)),
        ["Poliwag"] = new SpawnedPokemonDefinition("Poliwag", NoSkills, new PokemonBattleStats(40, 50, 40, 40, 40, 90)),
        ["Hoppip"] = new SpawnedPokemonDefinition("Hoppip", NoSkills, new PokemonBattleStats(35, 35, 40, 35, 55, 50)),
        ["Tyrogue"] = new SpawnedPokemonDefinition("Tyrogue", CreateSkillLevels((SkillType.Crafting, 1), (SkillType.Construction, 1)), new PokemonBattleStats(35, 35, 35, 35, 35, 35)),
        ["Smoochum"] = new SpawnedPokemonDefinition("Smoochum", NoSkills, new PokemonBattleStats(45, 30, 15, 85, 65, 65)),
        ["Magby"] = new SpawnedPokemonDefinition("Magby", NoSkills, new PokemonBattleStats(45, 75, 37, 70, 55, 83)),
        ["Nincada"] = new SpawnedPokemonDefinition("Nincada", NoSkills, new PokemonBattleStats(31, 45, 90, 30, 30, 40)),
        ["Elekid"] = new SpawnedPokemonDefinition("Elekid", CreateSkillLevels((SkillType.Crafting, 1), (SkillType.Construction, 1)), new PokemonBattleStats(45, 63, 37, 65, 55, 95)),
        ["Noibat"] = new SpawnedPokemonDefinition("Noibat", CreateSkillLevels((SkillType.Harvesting, 1)), new PokemonBattleStats(40, 30, 35, 45, 40, 55)),
        ["Dratini"] = new SpawnedPokemonDefinition("Dratini", CreateSkillLevels((SkillType.Water, 1), (SkillType.Transport, 2)), new PokemonBattleStats(41, 64, 45, 50, 50, 50)),
        ["Ralts"] = new SpawnedPokemonDefinition("Ralts", CreateSkillLevels((SkillType.Teleporting, 1)), new PokemonBattleStats(28, 25, 25, 45, 35, 40)),
        ["Ditto"] = new SpawnedPokemonDefinition("Ditto", NoSkills, new PokemonBattleStats(48, 48, 48, 48, 48, 48)),
        ["Abra"] = new SpawnedPokemonDefinition("Abra", NoSkills, new PokemonBattleStats(25, 20, 15, 105, 55, 90)),
        ["Rotom"] = new SpawnedPokemonDefinition("Rotom", NoSkills, new PokemonBattleStats(50, 50, 77, 95, 77, 91))
    };

    // Computes and returns or Default without mutating persistent game state.
    public static SpawnedPokemonDefinition GetOrDefault(string pokemonName)
    {
        if (Entries.TryGetValue(pokemonName, out SpawnedPokemonDefinition? definition))
        {
            return definition;
        }

        return new SpawnedPokemonDefinition(pokemonName, NoSkills, new PokemonBattleStats(50, 50, 50, 50, 50, 50));
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
