namespace Pokefarm.Game;

internal static class WorkbenchCraftingHelpers
{
    public static bool IsWorkbenchItemReady(PlacedItem workbench)
    {
        return workbench.Definition == ItemCatalog.WorkBench &&
               workbench.WorkbenchQueuedItem is not null &&
               workbench.WorkbenchCraftEffortRemaining <= 0f;
    }

    public static int GetWorkbenchQueueCapacity(PlacedItem workbench)
    {
        _ = workbench;
        // Keep this as a helper so we can support per-workbench queue size later.
        return 1;
    }

    public static int GetWorkbenchQueuedItemCount(PlacedItem workbench)
    {
        return workbench.WorkbenchQueuedItem is null ? 0 : 1;
    }
}
