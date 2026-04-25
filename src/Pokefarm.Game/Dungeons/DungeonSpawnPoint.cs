using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

/// <summary>
/// Executes the Dungeon Spawn Point operation.
/// </summary>
internal sealed record DungeonSpawnPoint(
    DungeonSpawnPointType Type,
    Point Position);
