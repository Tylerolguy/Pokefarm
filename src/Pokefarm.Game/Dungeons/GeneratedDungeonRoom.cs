namespace Pokefarm.Game;

// Data container used to pass generated Dungeon Room information between game systems.
internal sealed record GeneratedDungeonRoom(int Index, DungeonRoomDefinition Definition);
