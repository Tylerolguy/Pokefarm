namespace Pokefarm.Game;

internal sealed record PokemonDialogueOption(
    string Label,
    PokemonDialogueAction Action,
    string? ResponseText = null,
    int? TargetPokemonId = null,
    bool ExitAfterDelay = false);
