namespace Pokefarm.Game;

internal sealed record DungeonRoomDefinition(
    string Name,
    DungeonRoomType Type,
    DungeonRoomTemplate Template,
    int Weight = 1,
    int MinDepth = 1,
    int MaxDepth = int.MaxValue);
