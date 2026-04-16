using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal static class ItemCatalog
{
    public static readonly ItemDefinition Barn = new("Barn", new Color(162, 92, 71), ItemKind.Building, true, new Point(80, 80));
    public static readonly ItemDefinition WorkBench = new("Work Bench", new Color(124, 86, 54), ItemKind.Building, true, new Point(64, 48), true, "WORK BENCH");
    public static readonly ItemDefinition BasicSnack = new("Basic Snack", new Color(214, 186, 112), ItemKind.Snack, true, new Point(15, 15));
    public static readonly ItemDefinition BasicSnack2 = new("Basic Snack 2", new Color(0, 186, 112), ItemKind.Snack, true, new Point(15, 15));
    public static readonly ItemDefinition Bed = new("Bed", new Color(181, 112, 96), ItemKind.Building, true, new Point(40, 24));
    public static readonly ItemDefinition Planter = new("Planter", new Color(115, 158, 98), ItemKind.Building, false, new Point(48, 48));
    public static readonly ItemDefinition Wood = new("Wood", new Color(161, 116, 67), ItemKind.Material, false, new Point(32, 32));
    public static readonly ItemDefinition Stone = new("Stone", new Color(122, 122, 132), ItemKind.Material, false, new Point(32, 32));
}
