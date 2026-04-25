namespace Pokefarm.Game;

/// <summary>
/// Executes the Dungeon Pokemon Spawn Entry operation.
/// </summary>
internal sealed record DungeonPokemonSpawnEntry(
    string SpeciesName,
    int Weight,
    int MinCount = 1,
    int MaxCount = 1);
