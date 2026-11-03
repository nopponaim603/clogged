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
    public const string TutorialScene = "Tutorial";

    /// <summary>
    /// Set by <see cref="NewGameFromMenu"/>: the game scene raises the goal card
    /// once it has loaded, over the map the run actually starts on. The card is
    /// for a player arriving from the title screen, so any other way into a run
    /// (coming back from the tour, retrying after a loss) leaves it off.
    /// </summary>
    public static bool PendingGoalPrompt { get; set; }

    /// <summary>
    /// Claims the one-shot "state the goal" request. The game scene calls this
    /// once on the way in; a run that did not come from the title screen gets
    /// nothing, and the request never survives to the next scene.
    /// </summary>
    public static bool ConsumeGoalPrompt()
    {
        bool pending = PendingGoalPrompt;
        PendingGoalPrompt = false;
        return pending;
    }

    /// <summary>
    /// Fresh run: drop the save and every static tally, then load the game. No
    /// goal card - it belongs to the title screen's NEW GAME only.
    /// </summary>
    public static void NewGame()
    {
        BeginRun();
        PendingGoalPrompt = false;

        Time.timeScale = 1f;
        SceneManager.LoadScene(GameScene);
    }

    /// <summary>
    /// NEW GAME from the title screen: a fresh run that also states the goal. The
    /// card is raised by <see cref="RunBootstrap"/> after the scene has loaded, so
    /// the goal lands on the board it is talking about.
    /// </summary>
    public static void NewGameFromMenu()
    {
        PendingGoalPrompt = true;

        BeginRun();

        Time.timeScale = 1f;
        SceneManager.LoadScene(GameScene);
    }

    /// <summary>
    /// The tour: the same fresh-run reset, loaded into its own scene. It is a
    /// separate scene so nothing in it can touch a real save, and so the tutorial
    /// can be opened straight from the editor without playing the menu first.
    /// </summary>
    public static void NewTutorial()
    {
        BeginRun();

        // The tour teaches the goal itself, one card at a time.
        PendingGoalPrompt = false;

        Time.timeScale = 1f;
        SceneManager.LoadScene(TutorialScene);
    }

    /// <summary>The reset every "start playing" path shares.</summary>
    private static void BeginRun()
    {
        RunStats.Reset();
        RunLog.Clear();
        SaveSystem.Delete();
        SaveSystem.ClearPendingLoad();

        // The run track is rolled here, once, and never changed again until the
        // run ends - the director survives the scene load so the music starts
        // without a restart.
        AudioDirector.PendingRunTrack = AudioDirector.RollRunTrack();
        AudioDirector.EnsureExists().BeginRun(AudioDirector.PendingRunTrack);
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

        // No roll here: the save remembers which track this run is using.
        AudioDirector.PendingRunTrack = -1;

        RunLog.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameScene);
    }

    /// <summary>
    /// Retry after a loss. The save is already gone (the run ended), so this is
    /// the same reset as <see cref="NewGame"/>, music roll included.
    /// </summary>
    public static void TryAgain() => NewGame();

    public static void ToMainMenu()
    {
        Time.timeScale = 1f;
        SaveSystem.ClearPendingLoad();

        // The title track owns the menu, so swap it in before the scene changes.
        AudioDirector.EnsureExists().PlayTitleMusic();

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
