namespace Pokefarm.Game;

// Data container used to pass pokemon Dialogue Option information between game systems.
internal sealed record PokemonDialogueOption(
    string Label,
    PokemonDialogueAction Action,
    string? ResponseText = null,
    int? TargetPokemonId = null,
    bool ExitAfterDelay = false);
