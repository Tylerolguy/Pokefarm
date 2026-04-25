namespace Pokefarm.Game;

// Data container used to pass inventory Entry information between game systems.
internal sealed record InventoryEntry(ItemDefinition Definition, int Quantity);
