using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass dungeon Obstacle Definition information between game systems.
internal sealed record DungeonObstacleDefinition(
    string Name,
    Point Position,
    Point Size,
    bool BlocksMovement = true);
