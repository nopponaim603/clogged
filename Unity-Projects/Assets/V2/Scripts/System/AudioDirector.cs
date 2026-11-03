using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The game's only sound outlet: one looping Music source and one SFX source,
/// both routed through <see cref="GameAudioLibrary.Mixer"/> so Master / Music /
/// SFX can be faded independently from the options page.
///
/// It survives scene loads, which is what makes the run track keep playing while
/// the player moves between the menu and a run. The track itself is chosen once
/// per run and stored in the save, so Continue never restarts the music.
/// </summary>
[DefaultExecutionOrder(-500)]
public class AudioDirector : MonoBehaviour
{
    public const string MasterVolumeKey = "clogged.volume.master";
    public const string MusicVolumeKey = "clogged.volume.music";
    public const string SfxVolumeKey = "clogged.volume.sfx";

    private const string MasterParam = "MasterVolume";
    private const string MusicParam = "MusicVolume";
    private const string SfxParam = "SfxVolume";

    /// <summary>Below this the fader is treated as silence and mapped to the mixer floor.</summary>
    private const float MinLinear = 0.0001f;
    private const float SilenceDb = -80f;

    /// <summary>Shortest gap between two refused-click stings.</summary>
    private const float ErrorRepeatGuard = 0.2f;

    /// <summary>
    /// Hover ticks fire as the pointer crosses a row of controls, so they are
    /// both quieter than a click and guarded: without the floor, sweeping the
    /// mouse over the skill column turns into a rattle.
    /// </summary>
    private const float HoverVolume = 0.28f;
    private const float HoverRepeatGuard = 0.05f;

    private float lastErrorTime = -10f;
    private float lastHoverTime = -10f;

    /// <summary>How many hover ticks have played, so tests can see the cue wired without hearing it.</summary>
    public int HoverCount { get; private set; }

    public static AudioDirector Instance { get; private set; }

    /// <summary>
    /// Track chosen for the run that is about to load. SceneFlow rolls it before
    /// the scene change so the music can start without a hitch; the run reads it
    /// from the save once a run is resumed instead.
    /// </summary>
    public static int PendingRunTrack { get; set; } = -1;

    private GameAudioLibrary library;
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip currentMusic;

    /// <summary>The run track currently looping, or null between runs.</summary>
    public AudioClip CurrentMusic => currentMusic;

    /// <summary>Last clip handed to the SFX source, so tests can assert the wiring.</summary>
    public AudioClip LastSfx { get; private set; }

    /// <summary>How many stings have played, so a de-duplicated repeat is visible.</summary>
    public int SfxCount { get; private set; }

    /// <summary>
    /// How many refused-click stings have played. Counted separately because a
    /// rejected button plays its own click first and the error second, so
    /// <see cref="LastSfx"/> alone cannot prove the error landed.
    /// </summary>
    public int ErrorCount { get; private set; }

    // --------------------------------------------------------------- lifecycle

    /// <summary>Creates the director once per session and reuses it after that.</summary>
    public static AudioDirector EnsureExists()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("AudioDirector");
        Instance = go.AddComponent<AudioDirector>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        library = GameAudioLibrary.Load();
        BuildSources();
        ApplyVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void BuildSources()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        if (library == null || library.Mixer == null) return;

        var musicGroup = FindGroup(MusicParam, "Music");
        var sfxGroup = FindGroup(SfxParam, "SFX");
        if (musicGroup != null) musicSource.outputAudioMixerGroup = musicGroup;
        if (sfxGroup != null) sfxSource.outputAudioMixerGroup = sfxGroup;
    }

    private AudioMixerGroup FindGroup(string parameter, string groupName)
    {
        // The mixer names the groups Master/Music/SFX; the exposed parameters are
        // named separately, so route by group name and fade by parameter name.
        foreach (var group in library.Mixer.FindMatchingGroups(groupName))
        {
            if (group.name == groupName) return group;
        }

        Debug.LogWarning($"[AudioDirector] mixer has no '{groupName}' group - {parameter} has nowhere to route.");
        return null;
    }

    // ------------------------------------------------------------------ music

    /// <summary>Title screen track. Called from the menu's Awake.</summary>
    public void PlayTitleMusic()
    {
        if (library == null) return;
        PlayMusic(library.TitleMusic, restart: true);
    }

    /// <summary>Claims a track index for a fresh run. Rolled once, never mid-run.</summary>
    public static int RollRunTrack()
    {
        var library = GameAudioLibrary.Load();
        int count = library != null ? library.RunTrackCount : 0;
        return count > 0 ? Random.Range(0, count) : -1;
    }

    /// <summary>
    /// Starts the run track. Already playing the same clip is a no-op, so a
    /// reloaded scene or a resumed save keeps the music where it was.
    /// </summary>
    public void BeginRun(int trackIndex)
    {
        if (library == null) return;

        var clip = library.RunTrack(trackIndex);
        if (clip == null) clip = library.RunTrack(0);
        if (clip == null) return;

        PlayMusic(clip, restart: false);
    }

    private void PlayMusic(AudioClip clip, bool restart)
    {
        if (clip == null || musicSource == null) return;
        if (!restart && currentMusic == clip && musicSource.isPlaying) return;

        currentMusic = clip;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    /// <summary>Fades the music out for an ending. The clip is kept so a win/loss overlay can be dismissed quietly.</summary>
    public void StopMusic()
    {
        if (musicSource == null) return;

        musicSource.Stop();
        currentMusic = null;
    }

    // -------------------------------------------------------------------- sfx

    /// <summary>The same clip covers a good roll and an item landing in the bag.</summary>
    public void PlaySuccess()
    {
        if (library == null) return;
        PlaySfx(library.Success);
    }

    /// <summary>The dice landing badly - a Failure or a Fumble.</summary>
    public void PlayFail()
    {
        if (library == null) return;
        PlaySfx(library.Fail);
    }

    /// <summary>
    /// A click was refused and a warning banner is saying why. Distinct from
    /// <see cref="PlayFail"/> on purpose: a rejected click is "you cannot do
    /// that", not "the dice went against you".
    ///
    /// Throttled because the banner queue can absorb a burst of clicks and each
    /// one asks for the same sound; without the floor a held button would buzz.
    /// </summary>
    public void PlayError()
    {
        if (library == null) return;
        if (Time.unscaledTime - lastErrorTime < ErrorRepeatGuard) return;

        lastErrorTime = Time.unscaledTime;
        ErrorCount++;
        PlaySfx(library.Error);
    }

    /// <summary>Clears the error throttle. Only tests need this; play wants it on.</summary>
    public void ResetErrorThrottle() => lastErrorTime = -10f;

    /// <summary>
    /// The pointer just entered something clickable. Quiet and throttled on
    /// purpose: it is a "yes, this one" tick, not a press.
    /// </summary>
    public void PlayHover()
    {
        if (library == null) return;
        if (Time.unscaledTime - lastHoverTime < HoverRepeatGuard) return;

        lastHoverTime = Time.unscaledTime;
        HoverCount++;
        PlaySfx(library.Hover, HoverVolume);
    }

    /// <summary>Clears the hover throttle. Only a test that fires two hovers in one frame needs this.</summary>
    public void ResetHoverThrottle() => lastHoverTime = -10f;

    /// <summary>
    /// UI click. Every control names its own sound, so the player can hear what
    /// they are about to do: a bright tick for chrome, a chime for cook, the
    /// heal sting, a metallic craft, a warm select for cards and confirm.
    /// </summary>
    public void PlayClick(UiClickKind kind)
    {
        if (library == null) return;
        PlaySfx(library.ForKind(kind));
    }

    /// <summary>
    /// Win sting. Two clips exist and one is drawn at random, so back-to-back
    /// wins do not sound like the same button being pressed twice.
    ///
    /// Anything already on the SFX source is cut first: the win arrives right
    /// after the item pickup, and two overlapping stings read as a mistake.
    /// </summary>
    public void PlayWin()
    {
        if (library == null) return;

        StopMusic();
        if (sfxSource != null) sfxSource.Stop();

        int count = library.WinClipCount;
        PlaySfx(count > 0 ? library.WinClip(Random.Range(0, count)) : library.Success);
    }

    /// <summary>Lose condition. Music stops underneath so the sting is not fighting the loop.</summary>
    public void PlayGameOver()
    {
        if (library == null) return;

        StopMusic();
        PlaySfx(library.GameOver);
    }

    private void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null || sfxSource == null) return;

        LastSfx = clip;
        SfxCount++;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ----------------------------------------------------------------- volumes

    public static float MasterVolume => Pref(MasterVolumeKey);
    public static float MusicVolume => Pref(MusicVolumeKey);
    public static float SfxVolume => Pref(SfxVolumeKey);

    private static float Pref(string key) => Mathf.Clamp01(PlayerPrefs.GetFloat(key, 0.8f));

    /// <summary>Persists one fader and pushes the whole set through the mixer.</summary>
    public static void SetVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        Instance?.ApplyVolumes();
    }

    /// <summary>
    /// Writes the three faders into the mixer's exposed parameters. Linear
    /// 0-1 becomes dB because that is what a mixer fader takes, and zero is
    /// mapped to the floor rather than negative infinity.
    /// </summary>
    public void ApplyVolumes()
    {
        if (library == null || library.Mixer == null) return;

        library.Mixer.SetFloat(MasterParam, ToDecibels(MasterVolume));
        library.Mixer.SetFloat(MusicParam, ToDecibels(MusicVolume));
        library.Mixer.SetFloat(SfxParam, ToDecibels(SfxVolume));
    }

    private static float ToDecibels(float linear) =>
        linear <= 0f ? SilenceDb : Mathf.Log10(Mathf.Max(linear, MinLinear)) * 20f;
}
