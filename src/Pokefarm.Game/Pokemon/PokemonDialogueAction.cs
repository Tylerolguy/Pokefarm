namespace Pokefarm.Game;

// Named options used by gameplay flow to branch behavior for pokemon Dialogue Action.
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
