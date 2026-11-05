using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Every clip the game can play, in one asset so no scene has to carry the
/// references. It lives in a Resources folder because <see cref="AudioDirector"/>
/// builds itself in code and therefore has nothing to serialise against - scene
/// wiring would mean the same eight references duplicated in MainMenu and Core.
/// </summary>
public class GameAudioLibrary : ScriptableObject
{
    public const string ResourceName = "GameAudioLibrary";

    [Tooltip("Mixer split into Master / Music / SFX groups.")]
    [SerializeField] private AudioMixer mixer;

    [Tooltip("Played on the title screen, in the Music group.")]
    [SerializeField] private AudioClip titleMusic;

    [Tooltip("One is picked at random when a run starts and loops until the run ends.")]
    [SerializeField] private AudioClip[] runMusic = new AudioClip[0];

    [Tooltip("Played on a Success or Hard Success roll, and again when an item is gained.")]
    [SerializeField] private AudioClip success;

    [Tooltip("Played on a Failure or Fumble roll - the dice landing badly.")]
    [SerializeField] private AudioClip fail;

    [Tooltip("Played when a click is rejected and a warning banner appears.")]
    [SerializeField] private AudioClip error;

    [Tooltip("Played when the run is won. One is picked at random so a second win does not sound identical.")]
    [SerializeField] private AudioClip[] win = new AudioClip[0];

    [Tooltip("Played when a lose condition fires.")]
    [SerializeField] private AudioClip gameOver;

    [Tooltip("Chrome clicks: menu, options, help, close, mode toggles, sliders.")]
    [SerializeField] private AudioClip click;

    [Tooltip("Picking something in the run: a unit card, a dungeon card, the confirm button.")]
    [SerializeField] private AudioClip select;

    [Tooltip("The cook skill.")]
    [SerializeField] private AudioClip cook;

    [Tooltip("The heal skill.")]
    [SerializeField] private AudioClip heal;

    [Tooltip("The craft skill.")]
    [SerializeField] private AudioClip craft;

    [Tooltip("A page opening: options or help.")]
    [SerializeField] private AudioClip popup;

    [Tooltip("Hover tick. Falls back to the plain click when empty, played much quieter.")]
    [SerializeField] private AudioClip hover;

    public AudioMixer Mixer => mixer;
    public AudioClip TitleMusic => titleMusic;
    public AudioClip Success => success;
    public AudioClip Fail => fail;
    public AudioClip Error => error;
    public AudioClip GameOver => gameOver;
    public AudioClip Click => click;
    public AudioClip Select => select;
    public AudioClip Cook => cook;
    public AudioClip Heal => heal;
    public AudioClip Craft => craft;
    public AudioClip Popup => popup;

    /// <summary>The hover tick, falling back to the chrome click so the cue exists with no extra asset.</summary>
    public AudioClip Hover => hover != null ? hover : click;
    public int RunTrackCount => runMusic != null ? runMusic.Length : 0;
    public int WinClipCount => win != null ? win.Length : 0;

    /// <summary>The clip a control of this kind plays, or null when the slot is empty.</summary>
    public AudioClip ForKind(UiClickKind kind)
    {
        switch (kind)
        {
            case UiClickKind.Select: return select;
            case UiClickKind.Cook: return cook;
            case UiClickKind.Heal: return heal;
            case UiClickKind.Craft: return craft;
            case UiClickKind.Popup: return popup;
            default: return click;
        }
    }

    /// <summary>Run track by index; out of range yields null so a partly filled array can't crash a run.</summary>
    public AudioClip RunTrack(int index)
    {
        if (runMusic == null || index < 0 || index >= runMusic.Length) return null;
        return runMusic[index];
    }

    /// <summary>Win clip by index, same tolerance as <see cref="RunTrack"/>.</summary>
    public AudioClip WinClip(int index)
    {
        if (win == null || index < 0 || index >= win.Length) return null;
        return win[index];
    }

    private static GameAudioLibrary cached;
    private static bool lookedUp;

    /// <summary>
    /// The library asset, or null when it has not been generated yet. Cached
    /// after the first lookup; a miss is remembered too, so a missing asset
    /// warns once instead of hitting Resources on every sound.
    /// </summary>
    public static GameAudioLibrary Load()
    {
        if (lookedUp) return cached;

        lookedUp = true;
        cached = Resources.Load<GameAudioLibrary>(ResourceName);
        if (cached == null)
        {
            Debug.LogWarning($"[GameAudioLibrary] Resources/{ResourceName} is missing - the game will run silent.");
        }

        return cached;
    }
}
