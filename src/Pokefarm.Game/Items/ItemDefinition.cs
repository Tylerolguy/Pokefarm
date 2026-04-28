using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass item Definition information between game systems.
internal sealed record ItemDefinition(
    string Name,
    Color Tint,
    ItemKind Kind,
    bool HasCollision,
    Point Size,
    bool IsInteractable = false,
    string? InteractionMessage = null,
    SkillType RequiredSkill = SkillType.None,
    ItemDefinition? ProducedMaterial = null,
    float EffortPerProducedUnit = 0f,
    int MaxStoredProducedUnits = 0,
    int ProductionStepCount = 1,
    int MaxWorkers = 1,
    Point ExitSize = default,
    int BedCapacity = 1,
    int RequiredSkillLevel = 1,
    int WorkbenchQueueSlots = 1,
    int WorkbenchStorageCapacity = 1,
    float ConstructionEffortRequired = 1f,
    SkillType ConstructionRequiredSkill1 = SkillType.Construction,
    int ConstructionRequiredSkillLevel1 = 1,
    SkillType ConstructionRequiredSkill2 = SkillType.None,
    int ConstructionRequiredSkillLevel2 = 0,
    SkillType ConstructionRequiredSkill3 = SkillType.None,
    int ConstructionRequiredSkillLevel3 = 0,
    int StorageCapacity = 0)
{
    public bool IsBuildingLike => Kind == ItemKind.Building || Kind == ItemKind.ResourceProductionBuilding;

    public bool IsPlaceable => IsBuildingLike || Kind == ItemKind.Snack;

    public bool IsResourceProduction => Kind == ItemKind.ResourceProductionBuilding;
}
