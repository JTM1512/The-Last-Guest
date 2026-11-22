using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
public class GameScreens : MonoBehaviour
{
    public static GameScreens Instance { get; private set; }

    public GameObject mainMenu;
    public GameObject pauseMenu;
    public GameObject solvedScreen;
    public GameObject partialScreen;
    public GameObject escapedScreen;
    public GameObject hint;

    public bool Playing { get; private set; }
    public bool BlocksPlay => !Playing || (pauseMenu != null && pauseMenu.activeSelf);

    static bool startPlaying;

    void Awake()
    {
        Instance = this;
        if (!startPlaying)
            ShowMainMenu();
    }

    void Start()
    {
        if (startPlaying)
        {
            startPlaying = false;
            PlayGame();
        }
    }

    void Update()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var keyboard = Keyboard.current;
        if (keyboard == null || !Playing || pauseMenu == null)
            return;
        if (!keyboard.pKey.wasPressedThisFrame)
            return;

        if (pauseMenu.activeSelf)
            Resume();
        else
            Pause();
    }

    public void ShowMainMenu()
    {
        Playing = false;
        ShowOnly(mainMenu);
        if (hint != null)
            hint.SetActive(false);
    }

    public void PlayGame()
    {
        Playing = true;
        HideAll();
        if (hint != null)
            hint.SetActive(true);
    }

    public void Pause()
    {
        if (!Playing)
            return;
        pauseMenu.SetActive(true);
    }

    public void Resume()
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
    }

    public void Restart()
    {
        startPlaying = true;
        SceneManager.LoadScene(gameObject.scene.name);
    }

    public void ReturnToMenu()
    {
        startPlaying = false;
        SceneManager.LoadScene(gameObject.scene.name);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void ShowResult(Accusation.Verdict result)
    {
        GameObject screen = null;
        if (result == Accusation.Verdict.Solved)
            screen = solvedScreen;
        else if (result == Accusation.Verdict.Partial)
            screen = partialScreen;
        else if (result == Accusation.Verdict.Escaped)
            screen = escapedScreen;
        if (screen == null)
            return;

        Playing = false;
        var charge = FindAnyObjectByType<Accusation>();
        if (charge != null && charge.panel != null)
            charge.panel.SetActive(false);
        if (hint != null)
            hint.SetActive(false);
        ShowOnly(screen);
    }

    void ShowOnly(GameObject screen)
    {
        HideAll();
        if (screen != null)
            screen.SetActive(true);
    }

    void HideAll()
    {
        if (mainMenu != null)
            mainMenu.SetActive(false);
        if (pauseMenu != null)
            pauseMenu.SetActive(false);
        if (solvedScreen != null)
            solvedScreen.SetActive(false);
        if (partialScreen != null)
            partialScreen.SetActive(false);
        if (escapedScreen != null)
            escapedScreen.SetActive(false);
    }
}
