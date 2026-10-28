using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The only place that knows scene names, so a rename is one edit instead of a
/// hunt through UI scripts. Also the single entry point for "begin a run",
/// which has to reset the static run state before the game scene loads.
/// </summary>
public static class SceneFlow
{
    public const string MainMenuScene = "MainMenu";
    public const string GameScene = "Core";

    /// <summary>Fresh run: drop the save and every static tally, then load the game.</summary>
    public static void NewGame()
    {
        RunStats.Reset();
        RunLog.Clear();
        SaveSystem.Delete();
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameScene);
    }

    /// <summary>
    /// Picks the save up and lets <see cref="RunBootstrap"/> apply it after the
    /// scene wakes. Falls back to a fresh run when the file is missing or stale.
    /// </summary>
    public static void Continue()
    {
        if (!SaveSystem.QueueLoad())
        {
            NewGame();
            return;
        }

        RunLog.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameScene);
    }

    /// <summary>
    /// Retry after a loss. The save is already gone (the run ended), so this is
    /// the same reset as <see cref="NewGame"/>.
    /// </summary>
    public static void TryAgain() => NewGame();

    public static void ToMainMenu()
    {
        Time.timeScale = 1f;
        SaveSystem.ClearPendingLoad();
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
