using System.Collections.Generic;

namespace Pokefarm.Game;

internal sealed record RecipeDefinition(
    string Name,
    ItemDefinition Output,
    List<RecipeCost> Costs,
    CraftingSource Source,
    float CraftEffortRequired = 0f);
