using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record PlacedItem(
    Rectangle Bounds,
    ItemDefinition Definition,
    double PlacedAtWorldTimeSeconds,
    string? ResidentPokemonName = null,
    int? ResidentPokemonId = null)
{
    public double GetAgeSeconds(double currentWorldTimeSeconds) => Math.Max(0d, currentWorldTimeSeconds - PlacedAtWorldTimeSeconds);
}
