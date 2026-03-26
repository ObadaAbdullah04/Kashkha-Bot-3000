public class WinState : IGameState
{
    private readonly GameStateManager _manager;

    public WinState(GameStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        // TODO Week 4: Play win cinematic.
        // TODO Week 2: Build RunResult. Display grade card.
        // TODO Week 4: Fire Android native share intent.
        // TODO Week 2: Bank scrap into SaveManager.
    }

    public void Tick() { }

    public void Exit()
    {
        // TODO Week 4: Dismiss win screen.
    }
}