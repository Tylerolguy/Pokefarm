using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pokefarm.Game;

// Static helper for item Catalog logic shared across the game loop.
internal static class ItemCatalog
{
    public static readonly ItemDefinition Wood = new("Wood", new Color(161, 116, 67), ItemKind.Material, false, new Point(32, 32));
    public static readonly ItemDefinition Stone = new("Stone", new Color(122, 122, 132), ItemKind.Material, false, new Point(32, 32));

    public static readonly ItemDefinition Barn = new("Barn", new Color(162, 92, 71), ItemKind.Building, true, new Point(80, 80), ConstructionEffortRequired: 12f);
    public static readonly ItemDefinition LogsDebris = new("Logs Debris", new Color(112, 82, 52), ItemKind.Debris, true, new Point(44, 32));
    public static readonly ItemDefinition BoulderDebris = new("Boulder Debris", new Color(96, 100, 108), ItemKind.Debris, true, new Point(40, 40));
    public static readonly ItemDefinition WorkBench = new("Work Bench", new Color(124, 86, 54), ItemKind.Building, true, new Point(64, 48), true, "WORK BENCH", ExitSize: new Point(32, 32), WorkbenchQueueSlots: 3, WorkbenchStorageCapacity: 5, ConstructionEffortRequired: 10f, ConstructionRequiredSkill2: SkillType.Crafting, ConstructionRequiredSkillLevel2: 1);
    public static readonly ItemDefinition Chest = new("Chest", new Color(155, 109, 68), ItemKind.Building, true, new Point(40, 32), true, "CHEST", ConstructionEffortRequired: 8f, ConstructionRequiredSkill2: SkillType.Crafting, ConstructionRequiredSkillLevel2: 1, StorageCapacity: 8);
    public static readonly ItemDefinition BasicSnack = new("Basic Snack", new Color(214, 186, 112), ItemKind.Snack, true, new Point(15, 15));
    public static readonly ItemDefinition BasicSnack2 = new("Basic Snack 2", new Color(0, 186, 112), ItemKind.Snack, true, new Point(15, 15));
    public static readonly ItemDefinition BasicSnack3 = new("Basic Snack 3", new Color(103, 160, 226), ItemKind.Snack, true, new Point(15, 15));
    public static readonly ItemDefinition BasicSnack4 = new("Basic Snack 4", new Color(136, 103, 186), ItemKind.Snack, true, new Point(15, 15));
    public static readonly ItemDefinition Bed = new("Bed", new Color(181, 112, 96), ItemKind.Building, true, new Point(40, 24), BedCapacity: 2, ConstructionEffortRequired: 6f);
    public static readonly ItemDefinition Pc = new("PC", new Color(168, 176, 232), ItemKind.Building, true, new Point(56, 56), true, "PC", ConstructionEffortRequired: 14f, ConstructionRequiredSkill2: SkillType.Crafting, ConstructionRequiredSkillLevel2: 1);
    public static readonly ItemDefinition DungeonPortal = new("Dungeon Portal", new Color(136, 136, 144), ItemKind.Building, true, new Point(64, 64), true, "DUNGEON PORTAL", ExitSize: new Point(32, 32), ConstructionEffortRequired: 16f, MaxWorkers: 3);
    public static readonly ItemDefinition NoBerry = new("No Berry", new Color(156, 156, 156), ItemKind.Material, false, new Point(32, 32));
    public static readonly ItemDefinition OranBerry = new("Oran Berry", new Color(91, 156, 216), ItemKind.Material, false, new Point(32, 32));
    public static readonly ItemDefinition Tree = new("Tree", new Color(72, 128, 72), ItemKind.ResourceProductionBuilding, true, new Point(56, 56), true, "TREE", SkillType.Lumber, Wood, 5f, 1, 1, 1, new Point(32, 32), ConstructionEffortRequired: 8f, ConstructionRequiredSkill2: SkillType.Lumber, ConstructionRequiredSkillLevel2: 1);
    public static readonly ItemDefinition Farm = new("Farm", new Color(150, 118, 72), ItemKind.ResourceProductionBuilding, true, new Point(72, 56), true, "FARM", SkillType.Farming, OranBerry, 5f, 5, 3, 3, new Point(32, 32), ConstructionEffortRequired: 12f, ConstructionRequiredSkill2: SkillType.Farming, ConstructionRequiredSkillLevel2: 1);
    public static readonly ItemDefinition Planter = new("Planter", new Color(115, 158, 98), ItemKind.Building, false, new Point(48, 48));

    public static readonly IReadOnlyList<ItemDefinition> AllDefinitions =
    [
        Wood,
        Stone,
        Barn,
        LogsDebris,
        BoulderDebris,
        WorkBench,
        Chest,
        BasicSnack,
        BasicSnack2,
        BasicSnack3,
        BasicSnack4,
        Bed,
        Pc,
        DungeonPortal,
        NoBerry,
        OranBerry,
        Tree,
        Farm,
        Planter
    ];

    public static bool TryGetByName(string name, out ItemDefinition definition)
    {
        definition = AllDefinitions.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))!;
        return definition is not null;
    }
}
