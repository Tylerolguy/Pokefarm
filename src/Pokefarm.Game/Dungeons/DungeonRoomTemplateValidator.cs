using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

/// <summary>
/// Represents the DungeonRoomTemplateValidator.
/// </summary>
internal static class DungeonRoomTemplateValidator
{
    /// <summary>
    /// Executes the Is Valid operation.
    /// </summary>
    public static bool IsValid(DungeonRoomTemplate template, out string reason)
    {
        if (template.Size.X <= 0 || template.Size.Y <= 0)
        {
            reason = "Room size must be positive.";
            return false;
        }

        if (template.LayoutRows.Count != template.Size.Y)
        {
            reason = "Layout row count does not match room height.";
            return false;
        }

        for (int y = 0; y < template.LayoutRows.Count; y++)
        {
            if (template.LayoutRows[y].Length != template.Size.X)
            {
                reason = $"Layout row {y} width does not match room width.";
                return false;
            }
        }

        if (!template.SpawnPoints.Any(point => point.Type == DungeonSpawnPointType.PlayerStart))
        {
            reason = "Room needs at least one player spawn point.";
            return false;
        }

        if (!template.SpawnPoints.Any(point => point.Type == DungeonSpawnPointType.Exit))
        {
            reason = "Room needs at least one exit point.";
            return false;
        }

        foreach (DungeonSpawnPoint point in template.SpawnPoints)
        {
            if (!IsInside(point.Position, template.Size))
            {
                reason = $"Spawn point {point.Type} is out of bounds.";
                return false;
            }

            if (!IsWalkableLayoutCell(template, point.Position))
            {
                reason = $"Spawn point {point.Type} is placed on non-walkable layout.";
                return false;
            }
        }

        foreach (DungeonObstacleDefinition obstacle in template.Obstacles)
        {
            if (obstacle.Size.X <= 0 || obstacle.Size.Y <= 0)
            {
                reason = $"Obstacle {obstacle.Name} has invalid size.";
                return false;
            }

            Rectangle obstacleBounds = new(obstacle.Position, obstacle.Size);
            Rectangle roomBounds = new(Point.Zero, template.Size);
            if (!roomBounds.Contains(obstacleBounds))
            {
                reason = $"Obstacle {obstacle.Name} is out of room bounds.";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    /// <summary>
    /// Executes the Is Inside operation.
    /// </summary>
    private static bool IsInside(Point point, Point size)
    {
        return point.X >= 0 && point.Y >= 0 && point.X < size.X && point.Y < size.Y;
    }

    /// <summary>
    /// Executes the Is Walkable Layout Cell operation.
    /// </summary>
    private static bool IsWalkableLayoutCell(DungeonRoomTemplate template, Point point)
    {
        char tile = template.LayoutRows[point.Y][point.X];
        return tile != '#';
    }
}
