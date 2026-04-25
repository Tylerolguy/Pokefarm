namespace Pokefarm.Game;

internal sealed record DungeonPokemonSpawnEntry(
    string SpeciesName,
    int Weight,
    int MinCount = 1,
    int MaxCount = 1);
