using System.Collections.Generic;

namespace Pokefarm.Game;

/// <summary>
/// Executes the Recipe Definition operation.
/// </summary>
internal sealed record RecipeDefinition(
    string Name,
    ItemDefinition Output,
    List<RecipeCost> Costs,
    CraftingSource Source,
    float CraftEffortRequired = 0f);
