using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

/// <summary>
/// Executes the Dungeon Obstacle Definition operation.
/// </summary>
internal sealed record DungeonObstacleDefinition(
    string Name,
    Point Position,
    Point Size,
    bool BlocksMovement = true);
