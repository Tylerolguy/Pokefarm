namespace Pokefarm.Game;

// Workbench-specific helper rules used by talk, crafting, and rendering paths.
internal static class WorkbenchCraftingHelpers
{
    // A workbench item is ready only when a queue exists and the required effort has fully elapsed.
    public static bool IsWorkbenchItemReady(PlacedItem workbench)
    {
        return workbench.Definition == ItemCatalog.WorkBench &&
               workbench.WorkbenchQueuedItem is not null &&
               workbench.WorkbenchCraftEffortRemaining <= 0f;
    }

    // Single place for queue capacity so per-workbench upgrades can be introduced without changing callers.
    public static int GetWorkbenchQueueCapacity(PlacedItem workbench)
    {
        _ = workbench;
        // Keep this as a helper so we can support per-workbench queue size later.
        return 1;
    }

    // Current queue count helper that keeps callers decoupled from the underlying queue representation.
    public static int GetWorkbenchQueuedItemCount(PlacedItem workbench)
    {
        return workbench.WorkbenchQueuedItem is null ? 0 : 1;
    }
}
