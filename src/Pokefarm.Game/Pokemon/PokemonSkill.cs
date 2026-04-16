namespace Pokefarm.Game;

[Flags]
internal enum PokemonSkill
{
    None = 0,
    Lumber = 1 << 0,
    Farming = 1 << 1
}
