namespace Pokefarm.Game;

// Represents one required ingredient entry in a recipe (item + quantity).
internal sealed record RecipeCost(ItemDefinition Item, int Quantity);
