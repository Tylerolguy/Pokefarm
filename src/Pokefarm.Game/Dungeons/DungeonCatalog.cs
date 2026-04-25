using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

/// <summary>
/// Represents the DungeonCatalog.
/// </summary>
internal static class DungeonCatalog
{
    public static readonly DungeonDefinition MysteryGrove = new(
        "Mystery Grove",
        4,
        7,
        [
            new DungeonRoomDefinition(
                "Abandoned Camp",
                DungeonRoomType.Reward,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#..####......#",
                        "#............#",
                        "#.....##.....#",
                        "#............#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.RewardSpawn, new Point(10, 2))
                    ],
                    [
                        new DungeonObstacleDefinition("Campfire", new Point(4, 3), new Point(2, 2)),
                        new DungeonObstacleDefinition("Broken Crate", new Point(9, 4), new Point(1, 1))
                    ],
                    [
                        new DungeonPokemonSpawnEntry("Sewaddle", 1, 0, 0)
                    ],
                    ["forest", "reward", "earlygame"]),
                Weight: 3),
            new DungeonRoomDefinition(
                "Spiked Corridor",
                DungeonRoomType.Trap,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#.##########.#",
                        "#............#",
                        "#.##########.#",
                        "#............#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.TrapAnchor, new Point(7, 3))
                    ],
                    [
                        new DungeonObstacleDefinition("Spike Racks", new Point(6, 2), new Point(2, 3))
                    ],
                    [],
                    ["forest", "trap"]),
                Weight: 2),
            new DungeonRoomDefinition(
                "Moss Puzzle Hall",
                DungeonRoomType.Puzzle,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#..#......#..#",
                        "#............#",
                        "#..#......#..#",
                        "#............#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.PuzzleAnchor, new Point(7, 3))
                    ],
                    [
                        new DungeonObstacleDefinition("Puzzle Pillar A", new Point(3, 2), new Point(1, 1)),
                        new DungeonObstacleDefinition("Puzzle Pillar B", new Point(10, 4), new Point(1, 1))
                    ],
                    [],
                    ["forest", "puzzle"]),
                Weight: 2),
            new DungeonRoomDefinition(
                "Wild Den",
                DungeonRoomType.Enemy,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#....####....#",
                        "#............#",
                        "#....####....#",
                        "#............#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.EnemySpawn, new Point(9, 3)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.EnemySpawn, new Point(10, 5))
                    ],
                    [
                        new DungeonObstacleDefinition("Fallen Log", new Point(5, 2), new Point(4, 1))
                    ],
                    [
                        new DungeonPokemonSpawnEntry("Sewaddle", 3, 1, 2),
                        new DungeonPokemonSpawnEntry("Azurill", 2, 1, 1)
                    ],
                    ["forest", "enemy"]),
                Weight: 3),
            new DungeonRoomDefinition(
                "Hidden Cache",
                DungeonRoomType.Reward,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#..##....##..#",
                        "#............#",
                        "#............#",
                        "#....##......#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.RewardSpawn, new Point(6, 3))
                    ],
                    [
                        new DungeonObstacleDefinition("Ancient Chest", new Point(6, 3), new Point(1, 1), BlocksMovement: false)
                    ],
                    [],
                    ["forest", "reward", "midgame"]),
                Weight: 1,
                MinDepth: 3),
            new DungeonRoomDefinition(
                "Tangled Vines",
                DungeonRoomType.Trap,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#..########..#",
                        "#............#",
                        "#..########..#",
                        "#............#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.TrapAnchor, new Point(7, 5))
                    ],
                    [
                        new DungeonObstacleDefinition("Vine Cluster", new Point(6, 4), new Point(2, 2))
                    ],
                    [],
                    ["forest", "trap", "midgame"]),
                Weight: 2,
                MinDepth: 2),
            new DungeonRoomDefinition(
                "Rune Door",
                DungeonRoomType.Puzzle,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#....####....#",
                        "#............#",
                        "#....####....#",
                        "#............#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.PuzzleAnchor, new Point(7, 3))
                    ],
                    [
                        new DungeonObstacleDefinition("Rune Gate", new Point(6, 2), new Point(2, 1))
                    ],
                    [],
                    ["forest", "puzzle", "midgame"]),
                Weight: 2),
            new DungeonRoomDefinition(
                "Alpha Clearing",
                DungeonRoomType.Enemy,
                CreateRoomTemplate(
                    [
                        "##############",
                        "#............#",
                        "#............#",
                        "#.....##.....#",
                        "#............#",
                        "#............#",
                        "#............#",
                        "##############"
                    ],
                    [
                        new DungeonSpawnPoint(DungeonSpawnPointType.PlayerStart, new Point(1, 1)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.Exit, new Point(12, 6)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.EnemySpawn, new Point(9, 2)),
                        new DungeonSpawnPoint(DungeonSpawnPointType.EnemySpawn, new Point(10, 4))
                    ],
                    [
                        new DungeonObstacleDefinition("Stone Totem", new Point(7, 3), new Point(1, 1))
                    ],
                    [
                        new DungeonPokemonSpawnEntry("Sewaddle", 2, 1, 2),
                        new DungeonPokemonSpawnEntry("Azurill", 3, 1, 2)
                    ],
                    ["forest", "enemy", "boss-room"]),
                Weight: 1,
                MinDepth: 4)
        ]);

    /// <summary>
    /// Executes the Create Room Template operation.
    /// </summary>
    private static DungeonRoomTemplate CreateRoomTemplate(
        IReadOnlyList<string> layoutRows,
        IReadOnlyList<DungeonSpawnPoint> spawnPoints,
        IReadOnlyList<DungeonObstacleDefinition> obstacles,
        IReadOnlyList<DungeonPokemonSpawnEntry> pokemonSpawns,
        IReadOnlyList<string> tags)
    {
        Point size = new(layoutRows[0].Length, layoutRows.Count);
        return new DungeonRoomTemplate(
            size,
            layoutRows,
            obstacles,
            spawnPoints,
            pokemonSpawns,
            tags);
    }
}
