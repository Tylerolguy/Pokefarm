namespace Pokefarm.Game;

/// <summary>
/// Represents the PokemonDialogueService.
/// </summary>
internal static class PokemonDialogueService
{
    /// <summary>
    /// Executes the Get Opening Text operation.
    /// </summary>
    public static string GetOpeningText(SpawnedPokemon pokemon)
    {
        if (!pokemon.IsClaimed && !pokemon.IsFollowingPlayer)
        {
            return "HI I WOULD LOVE TO MOVE IN HERE";
        }

        if (!pokemon.IsClaimed && pokemon.IsFollowingPlayer)
        {
            return "HAVE YOU FOUND ME A BED";
        }

        return "HI!";
    }

    /// <summary>
    /// Executes the Get Options operation.
    /// </summary>
    public static List<PokemonDialogueOption> GetOptions(SpawnedPokemon pokemon)
    {
        if (!pokemon.IsClaimed && !pokemon.IsFollowingPlayer)
        {
            return
            [
                new("WANT TO LIVE HERE", PokemonDialogueAction.ToggleFollowing, "I WILL WAIT HERE UNTIL YOU FIND A BED", ExitAfterDelay: true),
                new("BYE", PokemonDialogueAction.Exit, "BYE", ExitAfterDelay: true),
                new("LEAVE", PokemonDialogueAction.None)
            ];
        }

        if (!pokemon.IsClaimed && pokemon.IsFollowingPlayer)
        {
            return
            [
                new("CANT FIND ONE", PokemonDialogueAction.ToggleFollowing, "OK LET ME KNOW IF YOU CAN FIND ONE", ExitAfterDelay: true),
                new("BYE", PokemonDialogueAction.Exit, "BYE", ExitAfterDelay: true),
                new("LEAVE", PokemonDialogueAction.None)
            ];
        }

        string followOption = pokemon.IsFollowingPlayer ? "STOP FOLLOWING" : "FOLLOW ME";
        string followResponse = pokemon.IsFollowingPlayer ? "OK I WILL WAIT HERE" : "SURE I WILL FOLLOW YOU";
        return
        [
            new(followOption, PokemonDialogueAction.ToggleFollowing, followResponse, ExitAfterDelay: true),
            new("BYE", PokemonDialogueAction.Exit, "BYE", ExitAfterDelay: true),
            new("LEAVE", PokemonDialogueAction.None)
        ];
    }
}
