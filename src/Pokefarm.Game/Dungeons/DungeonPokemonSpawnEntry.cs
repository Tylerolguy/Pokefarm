namespace Pokefarm.Game;

// Data container used to pass dungeon Pokemon Spawn Entry information between game systems.
internal sealed record DungeonPokemonSpawnEntry(
    string SpeciesName,
    int Weight,
    int MinCount = 1,
    int MaxCount = 1);
