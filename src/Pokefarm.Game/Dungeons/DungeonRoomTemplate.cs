using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

/// <summary>
/// Executes the Dungeon Room Template operation.
/// </summary>
internal sealed record DungeonRoomTemplate(
    Point Size,
    IReadOnlyList<string> LayoutRows,
    IReadOnlyList<DungeonObstacleDefinition> Obstacles,
    IReadOnlyList<DungeonSpawnPoint> SpawnPoints,
    IReadOnlyList<DungeonPokemonSpawnEntry> PokemonSpawns,
    IReadOnlyList<string>? Tags = null);
