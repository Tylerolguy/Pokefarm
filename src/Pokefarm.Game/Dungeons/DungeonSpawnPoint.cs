using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record DungeonSpawnPoint(
    DungeonSpawnPointType Type,
    Point Position);
