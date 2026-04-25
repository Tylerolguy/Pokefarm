namespace Pokefarm.Game;

/// <summary>
/// Represents the RecipeCatalog.
/// </summary>
internal static class RecipeCatalog
{
    public static readonly RecipeDefinition WorkBench = new(
        "Work Bench",
        ItemCatalog.WorkBench,
        [
            new RecipeCost(ItemCatalog.Wood, 1)
        ],
        CraftingSource.HandheldCrafting);

    public static readonly RecipeDefinition Bed = new(
        "Bed",
        ItemCatalog.Bed,
        [
            new RecipeCost(ItemCatalog.Wood, 2)
        ],
        CraftingSource.BasicWorkBenchCrafting,
        5f);

    public static readonly RecipeDefinition OranBerryPlant = new(
        "Oran Berry",
        ItemCatalog.OranBerry,
        [],
        CraftingSource.FarmGrowing);

    public static readonly RecipeDefinition NoBerryPlant = new(
        "No Berry",
        ItemCatalog.NoBerry,
        [],
        CraftingSource.FarmGrowing);
}
