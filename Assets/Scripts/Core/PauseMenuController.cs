using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _pauseRoot;

    // Panel Containers
    private VisualElement _pauseMainPanel;
    private VisualElement _pauseSettingsPanel;

    // Sliders
    private Slider _sliderSensX;
    private Slider _sliderSensY;
    private Slider _sliderSensZoom;
    private Slider _sliderSensSpin;

    // Timescale values
    private float _prePauseTimeScale = 1f;
    private float _prePauseFixedDeltaTime = 0.02f;

    private bool _isPaused = false;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        // Query Containers
        _pauseRoot = root.Q<VisualElement>("pause-root");
        _pauseMainPanel = root.Q<VisualElement>("pause-main-panel");
        _pauseSettingsPanel = root.Q<VisualElement>("pause-settings-panel");

        // Query & Bind Sliders
        _sliderSensX = root.Q<Slider>("slider-sens-x");
        _sliderSensY = root.Q<Slider>("slider-sens-y");
        _sliderSensZoom = root.Q<Slider>("slider-sens-zoom");
        _sliderSensSpin = root.Q<Slider>("slider-sens-spin");

        // Load existing settings
        _sliderSensX.value = SaveManager.CameraSensX;
        _sliderSensY.value = SaveManager.CameraSensY;
        _sliderSensZoom.value = SaveManager.ZoomSens;
        _sliderSensSpin.value = SaveManager.SpinSens;

        _sliderSensX.RegisterValueChangedCallback(evt => SaveManager.SetCameraSensX(evt.newValue));
        _sliderSensY.RegisterValueChangedCallback(evt => SaveManager.SetCameraSensY(evt.newValue));
        _sliderSensZoom.RegisterValueChangedCallback(evt => SaveManager.SetZoomSens(evt.newValue));
        _sliderSensSpin.RegisterValueChangedCallback(evt => SaveManager.SetSpinSens(evt.newValue));

        // Bind Buttons
        root.Q<Button>("btn-resume").clicked += ResumeGame;
        root.Q<Button>("btn-settings").clicked += () => SwitchMenu(true);
        root.Q<Button>("btn-settings-back").clicked += () => SwitchMenu(false);
        root.Q<Button>("btn-restart").clicked += RestartGame;
        root.Q<Button>("btn-menu").clicked += LoadMainMenu;

        // Intercept D-Pad Navigation
        root.RegisterCallback<NavigationMoveEvent>(HandleDpadNavigation);

        // Ensure the game starts unpaused and the menu is hidden
        ResumeGame();
    }

    private void OnEnable()
    {
        InputReader.OnPause += TogglePause;
    }

    private void OnDisable()
    {
        InputReader.OnPause -= TogglePause;

        if (_uiDocument != null && _uiDocument.rootVisualElement != null)
        {
            _uiDocument.rootVisualElement.UnregisterCallback<NavigationMoveEvent>(HandleDpadNavigation);
        }
    }

    private void TogglePause()
    {
        if (_isPaused) ResumeGame();
        else PauseGame();
    }

    private void PauseGame()
    {
        _isPaused = true;
        InputReader.IsInputBlocked = true; // Lock the state machine out from input

        GameEvents.OnPauseToggled?.Invoke(true); // Tell the HUD to hide its popups

        // Capture the exact time scales before freezing
        _prePauseTimeScale = Time.timeScale;
        _prePauseFixedDeltaTime = Time.fixedDeltaTime;

        // Halt the game physics and update loops
        Time.timeScale = 0f;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        // Show the UI
        if (_pauseRoot != null)
        {
            _pauseRoot.style.display = DisplayStyle.Flex;
            SwitchMenu(false); // Always show the main panel first
        }
    }

    private void SwitchMenu(bool showSettings)
    {
        // Toggle panel visibility
        _pauseMainPanel.style.display = showSettings ? DisplayStyle.None : DisplayStyle.Flex;
        _pauseSettingsPanel.style.display = showSettings ? DisplayStyle.Flex : DisplayStyle.None;

        // Clear focus so the D-Pad correctly targets the newly opened menu
        _uiDocument.rootVisualElement.Focus();
    }

    private void ResumeGame()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        // Hide the UI
        if (_pauseRoot != null)
        {
            _pauseRoot.style.display = DisplayStyle.None;
        }

        // Defer the actual system unpause by one frame
        StartCoroutine(ResumeRoutine());
    }

    private void RestartGame()
    {
        var manager = FindAnyObjectByType<GameStateManager>();
        if (manager != null)
        {
            manager.ChangeState(new State_SceneTransition(manager, SceneManager.GetActiveScene().name));
        }
    }

    private void LoadMainMenu()
    {
        var manager = FindAnyObjectByType<GameStateManager>();
        if (manager != null)
        {
            manager.ChangeState(new State_SceneTransition(manager, "MainMenu"));
        }
        else
        {
            // Fallback if GameStateManager is missing
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void HandleDpadNavigation(NavigationMoveEvent evt)
    {
        // Ignore input if we aren't paused
        if (!_isPaused) return;

        // Allow built-in navigation to handle it if a Button OR Slider is already focused
        var focusedElement = _uiDocument.rootVisualElement.focusController.focusedElement;
        if (focusedElement is Button || focusedElement is Slider) return;

        // Determine which panel is currently visible
        VisualElement activePanel = _pauseSettingsPanel.style.display == DisplayStyle.Flex
            ? _pauseSettingsPanel
            : _pauseMainPanel;

        // Highlight the first button in the currently active panel
        var firstButton = activePanel.Q<Button>();
        if (firstButton != null)
        {
            firstButton.Focus();
            evt.StopPropagation(); // Stop the event from moving focus a second time
        }
    }

    private System.Collections.IEnumerator ResumeRoutine()
    {
        // Wait until the end of the current frame so the UI click is fully consumed
        yield return null;

        _isPaused = false;

        // Restore the captured time scales
        Time.timeScale = _prePauseTimeScale;
        Time.fixedDeltaTime = _prePauseFixedDeltaTime;

        InputReader.IsInputBlocked = false; // Re-enable gameplay inputs

        GameEvents.OnPauseToggled?.Invoke(false); // Notify the HUD
    }
}