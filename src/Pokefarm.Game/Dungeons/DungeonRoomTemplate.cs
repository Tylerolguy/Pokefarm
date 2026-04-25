using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass dungeon Room Template information between game systems.
internal sealed record DungeonRoomTemplate(
    Point Size,
    IReadOnlyList<string> LayoutRows,
    IReadOnlyList<DungeonObstacleDefinition> Obstacles,
    IReadOnlyList<DungeonSpawnPoint> SpawnPoints,
    IReadOnlyList<DungeonPokemonSpawnEntry> PokemonSpawns,
    IReadOnlyList<string>? Tags = null);
