using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record SpawnedPokemon(
    Vector2 Position,
    Direction Direction,
    float MoveCooldownRemaining,
    bool IsMoving = false,
    Vector2 MoveTarget = default,
    float MoveTimeRemaining = 0f,
    bool IsFollowingPlayer = false,
    string? SpeechText = null,
    float SpeechTimerRemaining = 0f);
