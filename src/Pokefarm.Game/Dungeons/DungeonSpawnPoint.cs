using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass dungeon Spawn Point information between game systems.
internal sealed record DungeonSpawnPoint(
    DungeonSpawnPointType Type,
    Point Position);
