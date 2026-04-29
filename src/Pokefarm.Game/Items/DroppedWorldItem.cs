using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass dropped world Item information between game systems.
internal sealed record DroppedWorldItem(
    Rectangle Bounds,
    ItemDefinition Definition,
    double DroppedAtWorldTimeSeconds);
