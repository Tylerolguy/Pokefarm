namespace Pokefarm.Game;

// Workbench-specific helper rules used by talk, crafting, and rendering paths.
internal static class WorkbenchCraftingHelpers
{
    // A workbench has queued items when at least one queue slot has a valid item and quantity.
    public static bool HasWorkbenchQueuedItems(PlacedItem workbench)
    {
        if (workbench.Definition != ItemCatalog.WorkBench)
        {
            return false;
        }

        return IsValidQueueEntry(workbench.WorkbenchQueuedItem, workbench.WorkbenchQueuedQuantity) ||
               IsValidQueueEntry(workbench.WorkbenchQueuedItem2, workbench.WorkbenchQueuedQuantity2) ||
               IsValidQueueEntry(workbench.WorkbenchQueuedItem3, workbench.WorkbenchQueuedQuantity3);
    }

    // A workbench has stored items when both stored type and quantity are present.
    public static bool HasWorkbenchStoredItems(PlacedItem workbench)
    {
        return workbench.Definition == ItemCatalog.WorkBench &&
               workbench.WorkbenchStoredItem is not null &&
               workbench.WorkbenchStoredQuantity > 0;
    }

    // Central place for queue capacity so each workbench tier can expose a different queue size.
    public static int GetWorkbenchQueueCapacity(PlacedItem workbench)
    {
        if (workbench.Definition != ItemCatalog.WorkBench)
        {
            return 1;
        }

        return Math.Clamp(workbench.Definition.WorkbenchQueueSlots, 1, 3);
    }

    // Central place for storage capacity so each workbench tier can expose different output storage.
    public static int GetWorkbenchStorageCapacity(PlacedItem workbench)
    {
        if (workbench.Definition != ItemCatalog.WorkBench)
        {
            return 1;
        }

        return Math.Max(1, workbench.Definition.WorkbenchStorageCapacity);
    }

    // Counts occupied queue slots so callers can enforce slot-based limits without reading slot fields directly.
    public static int GetWorkbenchQueuedItemCount(PlacedItem workbench)
    {
        int occupied = 0;
        int capacity = GetWorkbenchQueueCapacity(workbench);
        for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
        {
            ItemDefinition? queuedItem = GetWorkbenchQueuedItemAtSlot(workbench, slotIndex);
            int queuedQuantity = GetWorkbenchQueuedQuantityAtSlot(workbench, slotIndex);
            if (IsValidQueueEntry(queuedItem, queuedQuantity))
            {
                occupied++;
            }
        }

        return occupied;
    }

    // Returns the queued item for the requested slot index.
    public static ItemDefinition? GetWorkbenchQueuedItemAtSlot(PlacedItem workbench, int slotIndex)
    {
        return slotIndex switch
        {
            0 => workbench.WorkbenchQueuedItem,
            1 => workbench.WorkbenchQueuedItem2,
            2 => workbench.WorkbenchQueuedItem3,
            _ => null
        };
    }

    // Returns the queued quantity for the requested slot index.
    public static int GetWorkbenchQueuedQuantityAtSlot(PlacedItem workbench, int slotIndex)
    {
        return slotIndex switch
        {
            0 => Math.Max(0, workbench.WorkbenchQueuedQuantity),
            1 => Math.Max(0, workbench.WorkbenchQueuedQuantity2),
            2 => Math.Max(0, workbench.WorkbenchQueuedQuantity3),
            _ => 0
        };
    }

    // Writes a queue slot in one place so callers do not duplicate with-expression switch logic.
    public static PlacedItem SetWorkbenchQueueSlot(PlacedItem workbench, int slotIndex, ItemDefinition? item, int quantity)
    {
        int safeQuantity = Math.Max(0, quantity);
        return slotIndex switch
        {
            0 => workbench with { WorkbenchQueuedItem = item, WorkbenchQueuedQuantity = safeQuantity },
            1 => workbench with { WorkbenchQueuedItem2 = item, WorkbenchQueuedQuantity2 = safeQuantity },
            2 => workbench with { WorkbenchQueuedItem3 = item, WorkbenchQueuedQuantity3 = safeQuantity },
            _ => workbench
        };
    }

    // Returns the active craft target from slot 1 of the queue.
    public static ItemDefinition? GetActiveWorkbenchQueuedItem(PlacedItem workbench)
    {
        return GetWorkbenchQueuedItemAtSlot(workbench, 0);
    }

    // Returns the active craft quantity from slot 1 of the queue.
    public static int GetActiveWorkbenchQueuedQuantity(PlacedItem workbench)
    {
        return GetWorkbenchQueuedQuantityAtSlot(workbench, 0);
    }

    // Finds an existing queue slot for a specific output item, or -1 if no matching slot exists.
    public static int FindWorkbenchQueueSlotForItem(PlacedItem workbench, ItemDefinition outputItem)
    {
        int capacity = GetWorkbenchQueueCapacity(workbench);
        for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
        {
            if (GetWorkbenchQueuedItemAtSlot(workbench, slotIndex) == outputItem &&
                GetWorkbenchQueuedQuantityAtSlot(workbench, slotIndex) > 0)
            {
                return slotIndex;
            }
        }

        return -1;
    }

    // Finds the first open queue slot index, or -1 if all slots are occupied.
    public static int FindFirstOpenWorkbenchQueueSlot(PlacedItem workbench)
    {
        int capacity = GetWorkbenchQueueCapacity(workbench);
        for (int slotIndex = 0; slotIndex < capacity; slotIndex++)
        {
            ItemDefinition? slotItem = GetWorkbenchQueuedItemAtSlot(workbench, slotIndex);
            int slotQuantity = GetWorkbenchQueuedQuantityAtSlot(workbench, slotIndex);
            if (!IsValidQueueEntry(slotItem, slotQuantity))
            {
                return slotIndex;
            }
        }

        return -1;
    }

    // Keeps queue slots contiguous by shifting later entries toward slot 1 after a dequeue/finish action.
    public static PlacedItem CompressWorkbenchQueue(PlacedItem workbench)
    {
        ItemDefinition?[] items =
        [
            workbench.WorkbenchQueuedItem,
            workbench.WorkbenchQueuedItem2,
            workbench.WorkbenchQueuedItem3
        ];
        int[] quantities =
        [
            Math.Max(0, workbench.WorkbenchQueuedQuantity),
            Math.Max(0, workbench.WorkbenchQueuedQuantity2),
            Math.Max(0, workbench.WorkbenchQueuedQuantity3)
        ];

        List<(ItemDefinition Item, int Quantity)> compacted = [];
        for (int index = 0; index < items.Length; index++)
        {
            if (items[index] is ItemDefinition queuedItem && quantities[index] > 0)
            {
                compacted.Add((queuedItem, quantities[index]));
            }
        }

        PlacedItem updated = workbench with
        {
            WorkbenchQueuedItem = null,
            WorkbenchQueuedQuantity = 0,
            WorkbenchQueuedItem2 = null,
            WorkbenchQueuedQuantity2 = 0,
            WorkbenchQueuedItem3 = null,
            WorkbenchQueuedQuantity3 = 0
        };

        for (int slotIndex = 0; slotIndex < compacted.Count && slotIndex < 3; slotIndex++)
        {
            updated = SetWorkbenchQueueSlot(updated, slotIndex, compacted[slotIndex].Item, compacted[slotIndex].Quantity);
        }

        return updated;
    }

    // Validates that a queue slot has both an item and a positive quantity.
    private static bool IsValidQueueEntry(ItemDefinition? item, int quantity)
    {
        return item is not null && quantity > 0;
    }
}
