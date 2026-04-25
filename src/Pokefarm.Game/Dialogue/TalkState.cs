using Microsoft.Xna.Framework.Graphics;

namespace Pokefarm.Game;

// Main runtime type for talk State, coordinating state and side effects for this feature.
sealed class TalkState
{
    public int ActivePokemonIndex { get; private set; } = -1;
    public int SelectedOptionIndex { get; private set; }
    public string Text { get; private set; } = "HI!";
    public List<PokemonDialogueOption> Options { get; private set; } = [];
    public string SpeakerName { get; private set; } = "SEWADDLE";
    public PlacedItem? ActiveBuilding { get; private set; }
    public Texture2D? IconTexture { get; private set; }
    public string? IconName { get; private set; }

    // Enters pokemon Talk flow and initializes transient interaction state.
    public void BeginPokemonTalk(int pokemonIndex, string text, List<PokemonDialogueOption> options, string speakerName)
    {
        ActivePokemonIndex = pokemonIndex;
        ActiveBuilding = null;
        SelectedOptionIndex = 0;
        Text = text;
        Options = options;
        SpeakerName = speakerName;
    }

    // Enters building Talk flow and initializes transient interaction state.
    public void BeginBuildingTalk(PlacedItem building, string text, List<PokemonDialogueOption> options, string speakerName)
    {
        ActivePokemonIndex = -1;
        ActiveBuilding = building;
        SelectedOptionIndex = 0;
        Text = text;
        Options = options;
        SpeakerName = speakerName;
    }

    // Moves selection while respecting collision and boundary rules.
    public void MoveSelection(int delta)
    {
        if (Options.Count == 0)
        {
            SelectedOptionIndex = 0;
            return;
        }

        SelectedOptionIndex = Math.Clamp(SelectedOptionIndex + delta, 0, Options.Count - 1);
    }

    // Computes and returns selected Option without mutating persistent game state.
    public PokemonDialogueOption? GetSelectedOption()
    {
        if (SelectedOptionIndex < 0 || SelectedOptionIndex >= Options.Count)
        {
            return null;
        }

        return Options[SelectedOptionIndex];
    }

    // Applies text and keeps connected state synchronized.
    public void SetText(string text)
    {
        Text = text;
    }

    // Applies options and keeps connected state synchronized.
    public void SetOptions(List<PokemonDialogueOption> options)
    {
        Options = options;
        SelectedOptionIndex = Math.Clamp(SelectedOptionIndex, 0, Math.Max(0, options.Count - 1));
    }

    // Applies icon and keeps connected state synchronized.
    public void SetIcon(string iconName, Texture2D? iconTexture)
    {
        IconName = iconName;
        IconTexture = iconTexture;
    }

    // Ticks building Reference each frame and keeps related timers and state synchronized.
    public void UpdateBuildingReference(PlacedItem building)
    {
        ActiveBuilding = building;
    }

    // Handles reset for this gameplay subsystem.
    public void Reset()
    {
        ActivePokemonIndex = -1;
        ActiveBuilding = null;
        SelectedOptionIndex = 0;
        Text = "HI!";
        Options = [];
        SpeakerName = "SEWADDLE";
        IconTexture = null;
        IconName = null;
    }
}
