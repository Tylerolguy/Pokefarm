namespace Pokefarm.Game;

// Static helper for quest Catalog logic shared across the game loop.
internal static class QuestCatalog
{
    public static readonly QuestDefinition WelcomeHome = new("Welcome Home");
    public static readonly QuestDefinition BuildYourFarm = new("Build Your Farm");
}
