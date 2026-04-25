namespace Pokefarm.Game;

/// <summary>
/// Executes the Pokemon Dialogue Option operation.
/// </summary>
internal sealed record PokemonDialogueOption(
    string Label,
    PokemonDialogueAction Action,
    string? ResponseText = null,
    int? TargetPokemonId = null,
    bool ExitAfterDelay = false);
