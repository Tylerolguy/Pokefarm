namespace Pokefarm.Game;

// Static helper for pokemon Dialogue Service logic shared across the game loop.
internal static class PokemonDialogueService
{
    // Computes and returns opening Text without mutating persistent game state.
    public static string GetOpeningText(SpawnedPokemon pokemon)
    {
        if (!pokemon.IsClaimed)
        {
            return "I WOULD LOVE TO MOVE IN HERE.";
        }

        return "HI DITTO";
    }

    // Computes and returns options without mutating persistent game state.
    public static List<PokemonDialogueOption> GetOptions(SpawnedPokemon pokemon)
    {
        if (!pokemon.IsClaimed)
        {
            PokemonDialogueOption findBedOption = !pokemon.IsFollowingPlayer
                ? new("FIND A BED", PokemonDialogueAction.ToggleFollowing, "I WILL WAIT HERE UNTIL YOU FIND A BED.", ExitAfterDelay: true)
                : new("STOP FINDING BED", PokemonDialogueAction.ToggleFollowing, "OK I WILL STOP LOOKING FOR A BED.", ExitAfterDelay: true);

            string skillsText = BuildSkillsText(pokemon);
            return
            [
                findBedOption,
                new("CHECK SKILLS", PokemonDialogueAction.SetText, skillsText),
                new("BYE", PokemonDialogueAction.Exit, "BYE", ExitAfterDelay: true)
            ];
        }

        PokemonDialogueOption followOption = !pokemon.IsFollowingPlayer
            ? new("FOLLOW ME", PokemonDialogueAction.ToggleFollowing, "SURE I WILL FOLLOW YOU.", ExitAfterDelay: true)
            : new("STOP FOLLOWING", PokemonDialogueAction.ToggleFollowing, "OK I WILL STAY HERE.", ExitAfterDelay: true);
        string claimedSkillsText = BuildSkillsText(pokemon);
        return
        [
            followOption,
            new("CHECK SKILLS", PokemonDialogueAction.SetText, claimedSkillsText),
            new("HOW ARE YOU DOING", PokemonDialogueAction.SetText, "IM GOOD HOW ARE YOU."),
            new("BYE", PokemonDialogueAction.Exit, "BYE", ExitAfterDelay: true)
        ];
    }

    // Builds a readable skill summary for dialogue responses.
    private static string BuildSkillsText(SpawnedPokemon pokemon)
    {
        if (pokemon.SkillLevels is null || pokemon.SkillLevels.Count == 0)
        {
            return "I DONT HAVE ANY SKILLS YET.";
        }

        List<string> skillEntries = [];
        foreach ((SkillType skill, int level) in pokemon.SkillLevels)
        {
            if (skill == SkillType.None || level <= 0)
            {
                continue;
            }

            skillEntries.Add($"{skill.ToString().ToUpperInvariant()} {level}");
        }

        if (skillEntries.Count == 0)
        {
            return "I DONT HAVE ANY SKILLS YET.";
        }

        return $"MY SKILLS: {string.Join(", ", skillEntries)}";
    }
}
