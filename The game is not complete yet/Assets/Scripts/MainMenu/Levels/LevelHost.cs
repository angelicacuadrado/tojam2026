using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Lives on the WindowLevel1 prefab. Loads/unloads the gameplay scene additively,
/// renders it into the window via the existing RenderTexture wiring, and owns the
/// pause UX (ESC pauses, Resume button resumes; minimize auto-pauses).
/// </summary>
[RequireComponent(typeof(WindowControls))]
public class LevelHost : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("Name of the gameplay scene to load additively (must be in Build Settings).")]
    [SerializeField] private string sceneName = "Level1";

    [Header("Pause UI")]
    [Tooltip("Full-rect overlay shown while paused. Lives inside WindowLevel1 (MainMenu canvas), NOT in the level scene.")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;

    [Header("Refs (auto-wired if blank)")]
    [SerializeField] private WindowControls windowControls;

    public bool _levelCompleted;
    private bool _paused;
    private bool _sceneLoaded;
    private bool _sceneLoading;

    private void Awake()
    {
        if (windowControls == null) windowControls = GetComponent<WindowControls>();
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
        }
        if (windowControls != null)
        {
            windowControls.Closing += OnWindowClosing;
            windowControls.Minimized += OnWindowMinimized;
        }
        Exit.AnyLevelCompleted += OnLevelCompleted;

        if (!_sceneLoaded && !_sceneLoading)
        {
            StartCoroutine(LoadAndStart());
        }
        else if (_sceneLoaded && !_paused)
        {
            // Restored from minimize while not paused — shouldn't happen via our flow,
            // but be safe and re-lock cursor.
            EnterPlaying();
        }
    }

    private void OnDisable()
    {
        if (windowControls != null)
        {
            windowControls.Closing -= OnWindowClosing;
            windowControls.Minimized -= OnWindowMinimized;
        }
        Exit.AnyLevelCompleted -= OnLevelCompleted;
        // Don't unload the scene here — Closing handles that. OnDisable also fires on
        // minimize, and we want to keep the scene loaded across minimize/restore.
    }

    private void OnDestroy()
    {
        // Safety net if something tore us down without going through Closing.
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (!_sceneLoaded || _paused) return;
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Pause();
        }
    }

    private IEnumerator LoadAndStart()
    {
        _sceneLoading = true;
        var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[LevelHost] LoadSceneAsync('{sceneName}') returned null. Is the scene in Build Settings?");
            _sceneLoading = false;
            yield break;
        }
        while (!op.isDone) yield return null;
        _sceneLoaded = true;
        _sceneLoading = false;
        EnterPlaying();

        //Start level audio
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Level1");
        }
        // Kick off the call window's incoming → connected sequence.
        var callWindow = MessageScheduler.Instance != null ? MessageScheduler.Instance.CallWindow : null;
        if (callWindow != null) callWindow.StartIncoming();
    }

    private void OnLevelCompleted(Exit _)
    {
        if (_levelCompleted) return;  // Exit can fire repeatedly if the player walks back through.
        _levelCompleted = true;

        var callWindow = MessageScheduler.Instance != null ? MessageScheduler.Instance.CallWindow : null;
        if (callWindow != null) callWindow.StartEnding();
    }

    public void Pause()
    {
        if (_paused) return;
        _paused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioManager.Instance?.PauseBGM(); // Stop level music when paused. Resumed in EnterPlaying.
        if (pausePanel != null) pausePanel.SetActive(true);
    }


    public void Resume()
    {
        if (!_paused) return;
        _paused = false;
        Time.timeScale = 1f;
        AudioManager.Instance?.UnPauseBGM(); // Resume level music when unpaused.
        EnterPlaying();
    }

    private void EnterPlaying()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnWindowMinimized(WindowControls _)
    {
        // Auto-pause so the player isn't being shot at while the window is hidden.
        // Restoring from minimize stays paused — user clicks Resume to re-enter the game.
        if (_sceneLoaded) Pause();
    }

    private void OnWindowClosing(WindowControls _)
    {
        // Always restore global state before tearing down the scene.
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        _paused = false;

        AudioManager.Instance?.PauseBGM();

        if (_sceneLoaded)
        {
            SceneManager.UnloadSceneAsync(sceneName);
            _sceneLoaded = false;
        }

        var scheduler = MessageScheduler.Instance;
        if (scheduler != null && scheduler.CallWindow != null)
        {
            // If user closed mid-sequence (e.g. before "Ended Call" finished), kill the call UI.
            scheduler.CallWindow.HideImmediate();
        }

        // Only advance the chapter once the user closes a *completed* level run.
        // Closing without finishing the level just discards the attempt.
        if (_levelCompleted && scheduler != null)
        {
            scheduler.AdvanceToNextChapter();
        }
    }
}
