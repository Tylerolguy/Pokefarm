using System.Collections.Generic;

namespace Pokefarm.Game;

// Defines one craftable output, its material costs, and any effort/source rules required to produce it.
internal sealed record RecipeDefinition(
    string Name,
    ItemDefinition Output,
    List<RecipeCost> Costs,
    CraftingSource Source,
    float CraftEffortRequired = 0f);
