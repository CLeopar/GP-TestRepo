using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the Start Menu UI.
/// Attach this script to a GameObject in your Start/Main Menu scene.
///
/// Inspector Setup:
///   - Assign scene names in the Inspector fields.
///   - Assign all Button references from your Canvas hierarchy.
///   - "Continue Game" and "New Game" buttons should be initially hidden (disabled).
///     They are shown automatically when the player has saved progress.
///
/// PlayerPrefs Key Used:
///   "HasSaveData" (int, 1 = has save, 0 or missing = no save)
///   Set PlayerPrefs.SetInt("HasSaveData", 1) from your gameplay scene when a level is started/saved.
/// </summary>
public class StartMenuUI : MonoBehaviour
{
    // ─── Scene Names (set in Inspector) ───────────────────────────────────────
    [Header("Scene Names")]
    [Tooltip("Name of the scene to load when starting a new game.")]
    [SerializeField] private string newGameSceneName = "Level_01";

    [Tooltip("Name of the scene to load when continuing a saved game.")]
    [SerializeField] private string continueSceneName = "Level_01";

    // ─── Button References (assign in Inspector) ───────────────────────────────
    [Header("Always-Visible Buttons")]
    [SerializeField] private Button startButton;       // "Start Game" — shown when NO save data exists
    [SerializeField] private Button settingsButton;    // Opens Settings (no-op for now)
    [SerializeField] private Button creditsButton;     // Opens Credits / Producer info (no-op for now)
    [SerializeField] private Button quitButton;        // Exits the application

    [Header("Buttons Shown Only After First Play")]
    [SerializeField] private Button continueButton;    // Continue saved game
    [SerializeField] private Button newGameButton;     // Start a brand-new game (discards save)

    // ─── Optional: root GameObjects to show/hide entire button areas ──────────
    [Header("Optional Layout Groups (can be left empty)")]
    [Tooltip("Root object that holds Continue + New Game buttons. Will be toggled.")]
    [SerializeField] private GameObject saveButtonsGroup;

    // ──────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        RefreshSaveButtonVisibility();
        RegisterButtonListeners();
    }

    // ─── Save-state visibility ─────────────────────────────────────────────────

    /// <summary>
    /// Shows or hides the Continue / New Game buttons depending on whether
    /// a save file exists. Call this again after clearing save data if needed.
    /// </summary>
    private void RefreshSaveButtonVisibility()
    {
        bool hasSave = PlayerPrefs.GetInt("HasSaveData", 0) == 1;

        // "Start Game" button is shown only when there is NO existing save.
        if (startButton != null)
            startButton.gameObject.SetActive(!hasSave);

        // Continue and New Game are shown only when save data exists.
        if (continueButton != null)
            continueButton.gameObject.SetActive(hasSave);

        if (newGameButton != null)
            newGameButton.gameObject.SetActive(hasSave);

        // If you grouped Continue + New Game under one parent, toggle that too.
        if (saveButtonsGroup != null)
            saveButtonsGroup.SetActive(hasSave);
    }

    // ─── Button wiring ─────────────────────────────────────────────────────────

    private void RegisterButtonListeners()
    {
        // Guard-check each reference so the menu doesn't crash if a button
        // hasn't been assigned in the Inspector yet.

        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);

        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueGame);

        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettings);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(OnCredits);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }

    // ─── Button Handlers ───────────────────────────────────────────────────────

    /// <summary>
    /// "Start Game" — visible only when there is no save data.
    /// Marks that the player has started, then loads the first level.
    /// </summary>
    private void OnStartGame()
    {
        Debug.Log("[StartMenu] Start Game pressed.");
        PlayerPrefs.SetInt("HasSaveData", 1);
        PlayerPrefs.Save();
        LoadScene(newGameSceneName);
    }

    /// <summary>
    /// "Continue" — loads the scene the player last reached.
    /// </summary>
    private void OnContinueGame()
    {
        Debug.Log("[StartMenu] Continue Game pressed.");
        LoadScene(continueSceneName);
    }

    /// <summary>
    /// "New Game" — clears save data and starts from the beginning.
    /// You may want to show a confirmation dialog before calling this.
    /// </summary>
    private void OnNewGame()
    {
        Debug.Log("[StartMenu] New Game pressed — clearing save data.");
        PlayerPrefs.DeleteKey("HasSaveData");
        // Delete any other save-related keys here, e.g.:
        // PlayerPrefs.DeleteKey("CurrentLevel");
        PlayerPrefs.Save();
        LoadScene(newGameSceneName);
    }

    /// <summary>
    /// "Settings" — placeholder. Wire up your Settings panel/scene here later.
    /// </summary>
    private void OnSettings()
    {
        Debug.Log("[StartMenu] Settings pressed — no interface yet.");
        // TODO: Open settings panel, e.g.:
        // settingsPanel.SetActive(true);
    }

    /// <summary>
    /// "Credits / Producer" — placeholder. Wire up your Credits panel/scene here later.
    /// </summary>
    private void OnCredits()
    {
        Debug.Log("[StartMenu] Credits pressed — no interface yet.");
        // TODO: Open credits panel, e.g.:
        // creditsPanel.SetActive(true);
    }

    /// <summary>
    /// "Quit" — exits the application (or stops Play Mode in the Editor).
    /// </summary>
    private void OnQuit()
    {
        Debug.Log("[StartMenu] Quit pressed.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── Utility ───────────────────────────────────────────────────────────────

    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[StartMenu] Scene name is empty. Please set it in the Inspector.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    // ─── Cleanup ───────────────────────────────────────────────────────────────

    private void OnDestroy()
    {
        // Remove listeners to prevent memory leaks if the object is destroyed
        // before Unity's GC cleans up button references.
        if (startButton != null)    startButton.onClick.RemoveListener(OnStartGame);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueGame);
        if (newGameButton != null)  newGameButton.onClick.RemoveListener(OnNewGame);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettings);
        if (creditsButton != null)  creditsButton.onClick.RemoveListener(OnCredits);
        if (quitButton != null)     quitButton.onClick.RemoveListener(OnQuit);
    }
}
