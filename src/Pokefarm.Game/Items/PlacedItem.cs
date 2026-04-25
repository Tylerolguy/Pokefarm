using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass placed Item information between game systems.
internal sealed record PlacedItem(
    Rectangle Bounds,
    ItemDefinition Definition,
    double PlacedAtWorldTimeSeconds,
    string? ResidentPokemonName = null,
    int? ResidentPokemonId = null,
    string? WorkerPokemonName = null,
    int? WorkerPokemonId = null,
    string? WorkerPokemonName2 = null,
    int? WorkerPokemonId2 = null,
    string? WorkerPokemonName3 = null,
    int? WorkerPokemonId3 = null,
    float StoredProductionEffort = 0f,
    int StoredProducedUnits = 0,
    int ProductionStepIndex = 0,
    ItemDefinition? FarmGrowingPlant = null,
    ItemDefinition? WorkbenchQueuedItem = null,
    float WorkbenchCraftEffortRemaining = 0f,
    float WorkbenchCraftEffortRequired = 0f)
{
    // Computes and returns age Seconds without mutating persistent game state.
    public double GetAgeSeconds(double currentWorldTimeSeconds) => Math.Max(0d, currentWorldTimeSeconds - PlacedAtWorldTimeSeconds);
}
