using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

internal sealed record SpawnedPokemon(
    int PokemonId,
    string Name,
    Vector2 Position,
    Direction Direction,
    float MoveCooldownRemaining,
    bool IsMoving = false,
    Vector2 MoveTarget = default,
    float MoveTimeRemaining = 0f,
    bool IsClaimed = false,
    bool IsFollowingPlayer = false,
    Vector2? HomePosition = null,
    string? SpeechText = null,
    float SpeechTimerRemaining = 0f,
    float IdleAnimationTimer = 0f,
    int IdleAnimationFrame = 0,
    float IdleCyclePauseRemaining = 0f);
