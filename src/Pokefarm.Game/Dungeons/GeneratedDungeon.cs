namespace Pokefarm.Game;

/// <summary>
/// Executes the Generated Dungeon operation.
/// </summary>
internal sealed record GeneratedDungeon(
    string DungeonName,
    IReadOnlyList<GeneratedDungeonRoom> Rooms);
