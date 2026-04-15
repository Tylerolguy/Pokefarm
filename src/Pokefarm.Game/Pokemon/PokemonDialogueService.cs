namespace Pokefarm.Game;

internal static class PokemonDialogueService
{
    public static string GetOpeningText(SpawnedPokemon pokemon)
    {
        return "HI!";
    }

    public static List<PokemonDialogueOption> GetOptions(SpawnedPokemon pokemon)
    {
        string followOption = pokemon.IsFollowingPlayer ? "STOP FOLLOWING" : "FOLLOW ME";
        List<PokemonDialogueOption> options =
        [
            new(followOption, PokemonDialogueAction.ToggleFollowing),
            new(pokemon.IsFollowingPlayer ? "HOW ARE YOU FEELING?" : "HOW ARE YOU?", PokemonDialogueAction.SetText, "I AM HAPPY")
        ];

        options.Add(new PokemonDialogueOption("BYE", PokemonDialogueAction.Exit));
        return options;
    }
}
