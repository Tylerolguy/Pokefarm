namespace Pokefarm.Game;

/// <summary>
/// Represents the WorkbenchCraftingHelpers.
/// </summary>
internal static class WorkbenchCraftingHelpers
{
    /// <summary>
    /// Executes the Is Workbench Item Ready operation.
    /// </summary>
    public static bool IsWorkbenchItemReady(PlacedItem workbench)
    {
        return workbench.Definition == ItemCatalog.WorkBench &&
               workbench.WorkbenchQueuedItem is not null &&
               workbench.WorkbenchCraftEffortRemaining <= 0f;
    }

    /// <summary>
    /// Executes the Get Workbench Queue Capacity operation.
    /// </summary>
    public static int GetWorkbenchQueueCapacity(PlacedItem workbench)
    {
        _ = workbench;
        // Keep this as a helper so we can support per-workbench queue size later.
        return 1;
    }

    /// <summary>
    /// Executes the Get Workbench Queued Item Count operation.
    /// </summary>
    public static int GetWorkbenchQueuedItemCount(PlacedItem workbench)
    {
        return workbench.WorkbenchQueuedItem is null ? 0 : 1;
    }
}
