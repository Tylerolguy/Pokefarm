namespace Pokefarm.Game;

// Lightweight immutable battle-stat container used for dungeon damage/HP calculations.
internal readonly record struct PokemonBattleStats(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed);
