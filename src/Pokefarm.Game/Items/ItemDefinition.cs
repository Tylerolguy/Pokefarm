using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record ItemDefinition(
    string Name,
    Color Tint,
    ItemKind Kind,
    bool HasCollision,
    Point Size,
    bool IsInteractable = false,
    string? InteractionMessage = null,
    PokemonSkill RequiredSkill = PokemonSkill.None,
    ItemDefinition? ProducedMaterial = null,
    float EffortPerProducedUnit = 0f,
    int MaxStoredProducedUnits = 0,
    int ProductionStepCount = 1,
    int MaxWorkers = 1)
{
    public bool IsBuildingLike => Kind == ItemKind.Building || Kind == ItemKind.ResourceProductionBuilding;

    public bool IsPlaceable => IsBuildingLike || Kind == ItemKind.Snack;

    public bool IsResourceProduction => Kind == ItemKind.ResourceProductionBuilding;
}
