namespace Pokefarm.Game;

/// <summary>
/// Defines possible values for PokemonDialogueAction.
/// </summary>
internal enum PokemonDialogueAction
{
    None,
    ToggleFollowing,
    SetHome,
    AssignResourceWork,
    UnassignResourceWork,
    CollectProduction,
    AssignWorkbenchWorker,
    UnassignWorkbenchWorker,
    OpenWorkbenchQueue,
    OpenFarmGrowingMenu,
    DequeueWorkbenchItem,
    CollectWorkbenchItem,
    OpenPcQuests,
    OpenPcLevel,
    OpenPcStorage,
    OpenDungeonMenu,
    StorePokemonInPc,
    SetText,
    Exit
}
