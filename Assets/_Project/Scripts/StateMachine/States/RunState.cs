public class RunState : IGameState
{
    private readonly GameStateManager _manager;

    public RunState(GameStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        // TODO Week 1: Call ResetToInitial() on all runtime SO Variables.
        // TODO Week 2: Load House 1 scene additively.
        // TODO Week 2: Begin first encounter via EncounterManager.
    }

    public void Tick() { }

    public void Exit()
    {
        // TODO: Unload house scene.
    }
}