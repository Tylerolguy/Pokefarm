using Microsoft.Xna.Framework.Graphics;

namespace Pokefarm.Game;

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

    public void BeginPokemonTalk(int pokemonIndex, string text, List<PokemonDialogueOption> options, string speakerName)
    {
        ActivePokemonIndex = pokemonIndex;
        ActiveBuilding = null;
        SelectedOptionIndex = 0;
        Text = text;
        Options = options;
        SpeakerName = speakerName;
    }

    public void BeginBuildingTalk(PlacedItem building, string text, List<PokemonDialogueOption> options, string speakerName)
    {
        ActivePokemonIndex = -1;
        ActiveBuilding = building;
        SelectedOptionIndex = 0;
        Text = text;
        Options = options;
        SpeakerName = speakerName;
    }

    public void MoveSelection(int delta)
    {
        if (Options.Count == 0)
        {
            SelectedOptionIndex = 0;
            return;
        }

        SelectedOptionIndex = Math.Clamp(SelectedOptionIndex + delta, 0, Options.Count - 1);
    }

    public PokemonDialogueOption? GetSelectedOption()
    {
        if (SelectedOptionIndex < 0 || SelectedOptionIndex >= Options.Count)
        {
            return null;
        }

        return Options[SelectedOptionIndex];
    }

    public void SetText(string text)
    {
        Text = text;
    }

    public void SetOptions(List<PokemonDialogueOption> options)
    {
        Options = options;
        SelectedOptionIndex = Math.Clamp(SelectedOptionIndex, 0, Math.Max(0, options.Count - 1));
    }

    public void SetIcon(string iconName, Texture2D? iconTexture)
    {
        IconName = iconName;
        IconTexture = iconTexture;
    }

    public void UpdateBuildingReference(PlacedItem building)
    {
        ActiveBuilding = building;
    }

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
