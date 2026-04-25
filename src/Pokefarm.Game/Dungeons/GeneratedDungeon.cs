namespace Pokefarm.Game;

internal sealed record GeneratedDungeon(
    string DungeonName,
    IReadOnlyList<GeneratedDungeonRoom> Rooms);
