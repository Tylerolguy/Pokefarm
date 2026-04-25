namespace Pokefarm.Game;

/// <summary>
/// Executes the Dungeon Definition operation.
/// </summary>
internal sealed record DungeonDefinition(
    string Name,
    int MinRoomCount,
    int MaxRoomCount,
    IReadOnlyList<DungeonRoomDefinition> RoomPool);
