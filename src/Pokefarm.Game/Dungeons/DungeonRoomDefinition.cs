namespace Pokefarm.Game;

// Data container used to pass dungeon Room Definition information between game systems.
internal sealed record DungeonRoomDefinition(
    string Name,
    DungeonRoomType Type,
    DungeonRoomTemplate Template,
    int Weight = 1,
    int MinDepth = 1,
    int MaxDepth = int.MaxValue);
