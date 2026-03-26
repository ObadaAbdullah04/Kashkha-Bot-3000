public class MetaMenuState : IGameState
{
    private readonly GameStateManager _manager;

    public MetaMenuState(GameStateManager manager)
    {
        _manager = manager;
    }

    public void Enter()
    {
        // TODO Week 3: Load UI_MetaMenu scene additively.
        // TODO Week 3: Display Workshop upgrade panel.
    }

    public void Tick()
    {
        // No per-frame logic needed here.
        // UI button press will call _manager.GoToRun().
    }

    public void Exit()
    {
        // TODO Week 3: Unload UI_MetaMenu scene.
    }
}