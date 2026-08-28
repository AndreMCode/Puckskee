using UnityEngine;
using UnityEngine.SceneManagement;

public class State_SceneTransition : IGameState
{
    private GameStateManager _context;
    private string _sceneName;

    public State_SceneTransition(GameStateManager context, string sceneName)
    {
        _context = context;
        _sceneName = sceneName;
    }

    public void Enter()
    {
        Debug.Log($"[State_SceneTransition] Locking down scene to load: {_sceneName}");

        // Disable the puck controllers so Update() cannot run during teardown
        if (_context.PuckP1 != null) _context.PuckP1.enabled = false;
        if (_context.PuckP2 != null) _context.PuckP2.enabled = false;

        // Reset global properties
        Time.timeScale = 1f;
        InputReader.IsInputBlocked = false;

        SceneManager.LoadScene(_sceneName);
    }

    public void UpdateState() { }

    public void Exit() { }
}