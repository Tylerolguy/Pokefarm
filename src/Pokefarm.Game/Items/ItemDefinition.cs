using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record ItemDefinition(
    string Name,
    Color Tint,
    ItemKind Kind,
    bool HasCollision,
    Point Size,
    bool IsInteractable = false,
    string? InteractionMessage = null)
{
    public bool IsPlaceable => Kind == ItemKind.Building || Kind == ItemKind.Snack;
}
