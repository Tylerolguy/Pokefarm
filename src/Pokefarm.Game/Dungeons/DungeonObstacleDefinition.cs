using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record DungeonObstacleDefinition(
    string Name,
    Point Position,
    Point Size,
    bool BlocksMovement = true);
