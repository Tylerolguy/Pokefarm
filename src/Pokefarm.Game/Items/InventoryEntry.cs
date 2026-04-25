namespace Pokefarm.Game;

/// <summary>
/// Executes the Inventory Entry operation.
/// </summary>
internal sealed record InventoryEntry(ItemDefinition Definition, int Quantity);
