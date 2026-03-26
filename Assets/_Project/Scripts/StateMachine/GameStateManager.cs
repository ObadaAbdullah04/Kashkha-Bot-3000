using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    private MetaMenuState  _metaMenuState;
    private RunState       _runState;
    private EncounterState _encounterState;
    private MinigameState  _minigameState;
    private GameOverState  _gameOverState;
    private WinState       _winState;

    private IGameState _currentState;

    private void Awake()
    {
        _metaMenuState  = new MetaMenuState(this);
        _runState       = new RunState(this);
        _encounterState = new EncounterState(this);
        _minigameState  = new MinigameState(this);
        _gameOverState  = new GameOverState(this);
        _winState       = new WinState(this);
    }

    private void Start() => TransitionTo(_metaMenuState);
    private void Update() => _currentState?.Tick();

    private void TransitionTo(IGameState nextState)
    {
        if (nextState == null)
        {
            Debug.LogError("[GameStateManager] Attempted transition to a null state.");
            return;
        }

        _currentState?.Exit();
        _currentState = nextState;
        _currentState.Enter();

        Debug.Log($"[GameStateManager] → {_currentState.GetType().Name}");
    }

    public void GoToMetaMenu()   => TransitionTo(_metaMenuState);
    public void GoToRun()        => TransitionTo(_runState);
    public void GoToEncounter()  => TransitionTo(_encounterState);
    public void GoToMinigame()   => TransitionTo(_minigameState);
    public void GoToGameOver()   => TransitionTo(_gameOverState);
    public void GoToWin()        => TransitionTo(_winState);
}
