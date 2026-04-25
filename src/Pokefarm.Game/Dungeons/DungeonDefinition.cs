namespace Pokefarm.Game;

// Data container used to pass dungeon Definition information between game systems.
internal sealed record DungeonDefinition(
    string Name,
    int MinRoomCount,
    int MaxRoomCount,
    IReadOnlyList<DungeonRoomDefinition> RoomPool);
