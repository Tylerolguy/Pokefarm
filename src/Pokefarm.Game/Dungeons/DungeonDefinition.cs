namespace Pokefarm.Game;

internal sealed record DungeonDefinition(
    string Name,
    int MinRoomCount,
    int MaxRoomCount,
    IReadOnlyList<DungeonRoomDefinition> RoomPool);
