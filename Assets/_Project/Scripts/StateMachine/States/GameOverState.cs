public class GameOverState : IGameState
{
    private readonly GameStateManager _manager;

    public GameOverState(GameStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        // TODO Week 2: Build RunResult struct from SO Variable values.
        // TODO Week 2: Bank scrap into SaveManager.
        // TODO Week 2: Display PostRun screen with grade.
    }

    public void Tick() { }

    public void Exit()
    {
        // TODO Week 2: Dismiss PostRun screen.
    }
}