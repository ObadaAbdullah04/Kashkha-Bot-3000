public class MinigameState : IGameState
{
    private readonly GameStateManager _manager;

    public MinigameState(GameStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        // TODO Week 3: Receive MiniGameType from RunState.
        // TODO Week 3: IMiniGame.Initialize() → StartGame()
    }

    public void Tick() { }

    public void Exit()
    {
        // TODO Week 3: IMiniGame.EndGame(out result)
        // TODO Week 3: Apply result deltas to SO Variables.
    }
}