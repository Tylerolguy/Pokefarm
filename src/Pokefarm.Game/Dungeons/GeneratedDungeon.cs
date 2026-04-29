using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass generated Dungeon information between game systems.
internal sealed record GeneratedDungeon(
    string DungeonName,
    IReadOnlyList<GeneratedDungeonRoom> Rooms,
    IReadOnlyList<string> LayoutRows,
    Point PlayerStartTile);
