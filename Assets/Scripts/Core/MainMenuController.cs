using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEditor; // For app-quit functionality

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("Dynamic Stage Settings")]
    [SerializeField] private VisualTreeAsset _stageCellTemplate;
    [SerializeField] private int _totalStages = 1; // Governs the number of stages available/displayed

    private UIDocument _uiDocument;

    // Menu Containers
    private VisualElement _gameTitle;
    private VisualElement _mainMenu;
    private VisualElement _difficultyMenu;
    private VisualElement _modeMenu;
    private VisualElement _optionsMenu;
    private VisualElement _stageSelectMenu;
    private VisualElement _stageGrid;
    private VisualElement _recordsMenu;
    private VisualElement _recordsList;
    private VisualElement _confirmOverlay;

    // Active State Tracking
    private enum MenuState { Main, Difficulty, Mode, Options, StageSelect, Records }
    private MenuState _currentState = MenuState.Main;
    private VisualElement _activeContainer;

    private GameStateManager.GameDifficulty _pendingDifficulty;
    private bool _pendingMultiplayer;

    private void Awake()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        _uiDocument = GetComponent<UIDocument>();
        var root = _uiDocument.rootVisualElement;

        // Container queries
        _gameTitle = root.Q<Label>("game-title");
        _mainMenu = root.Q<VisualElement>("main-menu-container");
        _difficultyMenu = root.Q<VisualElement>("difficulty-container");
        _modeMenu = root.Q<VisualElement>("mode-container");
        _optionsMenu = root.Q<VisualElement>("options-container");
        _stageSelectMenu = root.Q<VisualElement>("stage-select-container");
        _stageGrid = root.Q<VisualElement>("stage-grid");
        _recordsMenu = root.Q<VisualElement>("records-container");
        _recordsList = root.Q<VisualElement>("records-list");
        _confirmOverlay = root.Q<VisualElement>("clear-confirm-overlay");

        // Main Menu Buttons
        root.Q<Button>("btn-play").clicked += () => SwitchMenu(MenuState.Difficulty);
        root.Q<Button>("btn-options").clicked += () => SwitchMenu(MenuState.Options);
        root.Q<Button>("btn-quit").clicked += () => ExitGame();

        // Difficulty Buttons (Record choice, then switch menu)
        root.Q<Button>("btn-easy").clicked += () => {
            _pendingDifficulty = GameStateManager.GameDifficulty.Easy_Turns;
            SwitchMenu(MenuState.Mode);
        };
        root.Q<Button>("btn-hard").clicked += () => {
            _pendingDifficulty = GameStateManager.GameDifficulty.Hard_Distance;
            SwitchMenu(MenuState.Mode);
        };
        root.Q<Button>("btn-back-main").clicked += () => SwitchMenu(MenuState.Main);

        // Mode Buttons (Record choice, then switch menu)
        root.Q<Button>("btn-1p").clicked += () => {
            _pendingMultiplayer = false;
            SwitchMenu(MenuState.StageSelect);
        };
        root.Q<Button>("btn-2p").clicked += () => {
            _pendingMultiplayer = true;
            SwitchMenu(MenuState.StageSelect);
        };
        root.Q<Button>("btn-back-difficulty").clicked += () => SwitchMenu(MenuState.Difficulty);

        // Stage Select Back Button
        root.Q<Button>("btn-back-mode").clicked += () => SwitchMenu(MenuState.Mode);

        // Bind Options Menu transitions
        root.Q<Button>("btn-records").clicked += () => SwitchMenu(MenuState.Records);
        root.Q<Button>("btn-back-records").clicked += () => SwitchMenu(MenuState.Options);
        root.Q<Button>("btn-back-options").clicked += () => SwitchMenu(MenuState.Main);

        // Bind Confirmation Popup Logic
        root.Q<Button>("btn-clear-prompt").clicked += () => _confirmOverlay.style.display = DisplayStyle.Flex;
        root.Q<Button>("btn-confirm-no").clicked += () => _confirmOverlay.style.display = DisplayStyle.None;
        root.Q<Button>("btn-confirm-yes").clicked += ExecuteClearRecords;

        // Intercept D-Pad Navigation
        root.RegisterCallback<NavigationMoveEvent>(HandleDpadNavigation);

        // Generate the dynamic grid exactly once at startup
        if (_stageCellTemplate != null)
        {
            PopulateStageGrid();
        }
        else
        {
            Debug.LogWarning("[MainMenu] Stage Cell Template is missing! Assign it in the Inspector.");
        }

        PopulateRecordsList();

        // Initialize the Main Menu
        SwitchMenu(MenuState.Main);
    }

    private void SwitchMenu(MenuState newState)
    {
        _currentState = newState;

        // Hide everything first
        _gameTitle.style.display = DisplayStyle.Flex; // Title stays unless otherwise
        _mainMenu.style.display = DisplayStyle.None;
        _difficultyMenu.style.display = DisplayStyle.None;
        _modeMenu.style.display = DisplayStyle.None;
        _optionsMenu.style.display = DisplayStyle.None;
        _stageSelectMenu.style.display = DisplayStyle.None;
        _recordsMenu.style.display = DisplayStyle.None;
        _confirmOverlay.style.display = DisplayStyle.None;

        // Clear focus so nothing is highlighted by default
        _uiDocument.rootVisualElement.Focus();

        // Show the target menu
        switch (newState)
        {
            case MenuState.Main:
                _activeContainer = _mainMenu;
                break;
            case MenuState.Difficulty:
                _activeContainer = _difficultyMenu;
                break;
            case MenuState.Mode:
                _activeContainer = _modeMenu;
                break;
            case MenuState.Options:
                _activeContainer = _optionsMenu;
                break;
            case MenuState.StageSelect:
                _activeContainer = _stageSelectMenu;
                _gameTitle.style.display = DisplayStyle.None; // Title goes
                break;
            case MenuState.Records:
                _activeContainer = _recordsMenu;
                _gameTitle.style.display = DisplayStyle.None; // Title goes
                break;
        }

        _activeContainer.style.display = DisplayStyle.Flex;
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void HandleDpadNavigation(NavigationMoveEvent evt)
    {
        // If a UI element is already focused, let Unity's built-in D-Pad navigation handle it normally.
        if (_uiDocument.rootVisualElement.focusController.focusedElement is Button) return;

        // If nothing is focused and the player hits a direction, find the first button in the active container and focus it
        var firstButton = _activeContainer.Q<Button>();
        if (firstButton != null)
        {
            firstButton.Focus();
            evt.StopPropagation(); // Stop the event from moving focus a second time
        }
    }

    // ==========================================
    // DYNAMIC UI GENERATION
    // ==========================================

    private void PopulateStageGrid()
    {
        _stageGrid.Clear(); // Ensure the grid is empty

        for (int i = 1; i <= _totalStages; i++)
        {
            int stageNumber = i;

            // Clone the template
            TemplateContainer cellInstance = _stageCellTemplate.Instantiate();

            // Extract the button root and set nameLabel data
            Button cellButton = cellInstance.Q<Button>();
            Label nameLabel = cellInstance.Q<Label>("stage-name-label");

            // Apply stage number
            if (nameLabel != null)
            {
                nameLabel.text = $"STAGE {stageNumber}";
            }

            // Set additional label data
            Label turnsLabel = cellInstance.Q<Label>("stat-turns");
            Label distLabel = cellInstance.Q<Label>("stat-dist");

            // Apply values
            StageRecord record = SaveManager.GetRecord(stageNumber);
            if (record != null)
            {
                if (turnsLabel != null) turnsLabel.text = $"Turns: {record.BestTurns}";
                if (distLabel != null) distLabel.text = $"Dist: {record.BestDistance:F2}m";
            }
            else
            {
                if (turnsLabel != null) turnsLabel.text = "Turns: --";
                if (distLabel != null) distLabel.text = "Dist: --";
            }

            // Bind the click event to load the correct level
            if (cellButton != null)
            {
                cellButton.clicked += () => LoadStage(stageNumber);
            }

            // Inject it into the layout
            _stageGrid.Add(cellInstance);
        }
    }

    private void LoadStage(int stageNumber)
    {
        Debug.Log($"[MainMenu] Initializing Stage {stageNumber}. Passing settings to GameStateManager...");

        // Save game mode configuration
        MatchSettings.Difficulty = _pendingDifficulty;
        MatchSettings.IsMultiplayer = _pendingMultiplayer;

        SceneManager.LoadScene($"Stage_{stageNumber}");
    }

    // ==========================================
    // RECORDS DATA
    // ==========================================
    private void PopulateRecordsList()
    {
        _recordsList.Clear();

        for (int i = 1; i <= _totalStages; i++)
        {
            int stageNumber = i;

            // Create a row container
            VisualElement row = new();
            row.AddToClassList("record-row");

            // Create columns
            Label nameLabel = new($"STAGE {stageNumber}");
            nameLabel.AddToClassList("record-col-left");

            // Create placeholder data
            Label turnsLabel = new("--");
            turnsLabel.AddToClassList("record-col-right");

            Label distLabel = new("--");
            distLabel.AddToClassList("record-col-right");

            // Populate true data
            StageRecord record = SaveManager.GetRecord(stageNumber);
            if (record != null)
            {
                if (turnsLabel != null) turnsLabel.text = $"Turns: {record.BestTurns}";
                if (distLabel != null) distLabel.text = $"Dist: {record.BestDistance:F2}m";
            }
            else
            {
                if (turnsLabel != null) turnsLabel.text = "Turns: --";
                if (distLabel != null) distLabel.text = "Dist: --";
            }

            // Assemble
            row.Add(nameLabel);
            row.Add(turnsLabel);
            row.Add(distLabel);

            // Inject it into the list
            _recordsList.Add(row);
        }
    }

    private void ExecuteClearRecords()
    {
        Debug.Log("[Records] All stage records have been wiped.");

        SaveManager.ClearAllData();

        _confirmOverlay.style.display = DisplayStyle.None;

        // Refresh both lists so they instantly visually reset to "--"
        PopulateRecordsList();
        PopulateStageGrid();
    }
}