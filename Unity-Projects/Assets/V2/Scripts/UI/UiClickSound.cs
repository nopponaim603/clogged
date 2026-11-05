using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts a click on a control. <see cref="UiFactory.CreateButton"/> attaches the
/// plain clip to every button it builds, so chrome gets a sound for free; the
/// controls that do something specific name their own <see cref="UiClickKind"/>.
/// Everything that is not a uGUI Button (the roster cards, the sliders) calls
/// <see cref="Play"/> directly.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UiClickSound : MonoBehaviour
{
    /// <summary>
    /// A drag fires onValueChanged on every frame; without a floor a slider would
    /// machine-gun the clip into noise.
    /// </summary>
    public const float MinInterval = 0.1f;

    [SerializeField] private UiClickKind kind = UiClickKind.Plain;

    private Button button;
    private static float lastPlayTime = -10f;

    /// <summary>
    /// Frame in which the next click must stay silent. A refused click should
    /// play the error sting instead of its own sound - two clips starting in the
    /// same frame turn into mud. Scoped to a frame so a warning raised with no
    /// click behind it cannot silence a later press.
    /// </summary>
    private static int suppressFrame = -1;

    public UiClickKind Kind => kind;

    /// <summary>
    /// Tells the click that belongs to this refusal to stay quiet. Called by the
    /// warning banner, which always runs inside the button's own onClick - so the
    /// game handler has already fired by the time the click sound would play.
    /// </summary>
    public static void SuppressThisFrameClick() => suppressFrame = Time.frameCount;

    private static bool ConsumeSuppressed()
    {
        if (suppressFrame != Time.frameCount) return false;

        suppressFrame = -1;
        return true;
    }

    /// <summary>
    /// Attaches to a button, or switches the kind when one is already attached.
    /// Safe to call twice - the listener is only ever added once.
    /// </summary>
    public static UiClickSound Attach(Button target, UiClickKind newKind)
    {
        if (target == null) return null;

        var sound = target.GetComponent<UiClickSound>();
        if (sound == null) sound = target.gameObject.AddComponent<UiClickSound>();

        sound.kind = newKind;
        sound.Hook(target);
        return sound;
    }

    /// <summary>Switches an existing button to one of the action clips.</summary>
    public static void MarkAction(Button target, UiClickKind kind) => Attach(target, kind);

    /// <summary>
    /// Clears the throttle. Only the test suite needs it: a real drag should be
    /// throttled, but a check that fires two clicks in one frame must not be.
    /// </summary>
    public static void ResetThrottle() => lastPlayTime = -10f;

    /// <summary>Plays a click with no button attached - for cards and sliders.</summary>
    public static void Play(UiClickKind kind)
    {
        if (ConsumeSuppressed()) return;
        if (Time.unscaledTime - lastPlayTime < MinInterval) return;

        lastPlayTime = Time.unscaledTime;
        AudioDirector.Instance?.PlayClick(kind);
    }

    private void Hook(Button target)
    {
        if (button == target) return;

        if (button != null) button.onClick.RemoveListener(OnClicked);

        button = target;
        if (button != null) button.onClick.AddListener(OnClicked);
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClicked);
    }

    private void OnClicked()
    {
        if (ConsumeSuppressed()) return;

        lastPlayTime = Time.unscaledTime;
        AudioDirector.Instance?.PlayClick(kind);
    }
}
