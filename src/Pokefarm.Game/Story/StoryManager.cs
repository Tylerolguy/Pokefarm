namespace Pokefarm.Game;

internal enum StorySceneId
{
    TutorialChooseOne
}

internal sealed record StorySceneDefinition(
    StorySceneId Id,
    string SpeakerName,
    string OpeningText,
    List<PokemonDialogueOption> Options);

// Lightweight scaffold for story gating and scene definitions.
internal sealed class StoryManager
{
    public bool TutorialStarted { get; private set; }

    public void ResetForNewGame()
    {
        TutorialStarted = false;
    }

    public void LoadFlags(bool tutorialStarted)
    {
        TutorialStarted = tutorialStarted;
    }

    public void MarkTutorialStarted()
    {
        TutorialStarted = true;
    }

    public bool TryBuildPcTutorialScene(out StorySceneDefinition scene)
    {
        if (TutorialStarted)
        {
            scene = null!;
            return false;
        }

        scene = new StorySceneDefinition(
            StorySceneId.TutorialChooseOne,
            "ROTOM",
            "CHOOSE ONE",
            [
                new PokemonDialogueOption("A", PokemonDialogueAction.StoryTutorialChooseA),
                new PokemonDialogueOption("B", PokemonDialogueAction.StoryTutorialChooseB),
                new PokemonDialogueOption("BYE", PokemonDialogueAction.Exit)
            ]);
        return true;
    }
}
