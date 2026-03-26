public class EncounterState : IGameState
{
    private readonly GameStateManager _manager;

    public EncounterState(GameStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        // TODO Week 2: EncounterManager.LoadNextEncounter()
        // TODO Week 2: PanicTimer.Begin()
    }

    public void Tick() { }

    public void Exit()
    {
        // TODO Week 2: PanicTimer.Stop()
        // TODO Week 2: Clear dialogue UI
    }
}