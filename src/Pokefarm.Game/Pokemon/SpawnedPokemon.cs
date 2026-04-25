using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Data container used to pass spawned Pokemon information between game systems.
internal sealed record SpawnedPokemon(
    int PokemonId,
    string Name,
    Vector2 Position,
    Direction Direction,
    float MoveCooldownRemaining,
    IReadOnlyDictionary<SkillType, int>? SkillLevels = null,
    bool IsAssignedToWork = false,
    bool IsWorking = false,
    bool IsMoving = false,
    Vector2 MoveTarget = default,
    float MoveTimeRemaining = 0f,
    bool IsClaimed = false,
    bool IsFollowingPlayer = false,
    Vector2? HomePosition = null,
    string? SpeechText = null,
    float SpeechTimerRemaining = 0f,
    bool ShowWorkBlockedMarker = false,
    int? AssignedConstructionSiteId = null,
    Vector2? WanderTarget = null,
    float IdleAnimationTimer = 0f,
    int IdleAnimationFrame = 0,
    float IdleCyclePauseRemaining = 0f)
{
    // Computes and returns skill Level without mutating persistent game state.
    public int GetSkillLevel(SkillType skillType)
    {
        if (skillType == SkillType.None || SkillLevels is null)
        {
            return 0;
        }

        return SkillLevels.TryGetValue(skillType, out int level)
            ? Math.Max(0, level)
            : 0;
    }
}
