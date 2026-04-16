namespace Pokefarm.Game;

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
    DequeueWorkbenchItem,
    CollectWorkbenchItem,
    SetText,
    Exit
}
