using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// One escape hatch for the whole run: a right click anywhere drops everything
/// pending - the armed skill, the cave picked on the map, the cards held in the
/// hand. There are no exceptions per control, so the player learns one gesture
/// and it always means the same thing.
///
/// It reads the Input System's <see cref="Mouse"/> directly rather than going
/// through uGUI: the gesture has to work over the map, over empty space and over
/// a button, and a per-object pointer handler cannot cover the background.
/// </summary>
public class RightClickCancel : MonoBehaviour
{
    [Tooltip("Clip played when a right click actually cancelled something. Silent when nothing was pending.")]
    [SerializeField] private UiClickKind cancelSound = UiClickKind.Plain;

    [Tooltip("Set false to leave the hand selection alone and only drop map/skill plans.")]
    [SerializeField] private bool clearCards = true;

    private UnitRosterView rosterView;

    /// <summary>How many right clicks reached this component, so a test can prove the input path is wired.</summary>
    public int RightClicksSeen { get; private set; }

    private void Start()
    {
        if (rosterView == null) rosterView = FindAnyObjectByType<UnitRosterView>();
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.rightButton.wasPressedThisFrame) return;

        RightClicksSeen++;
        CancelPending();
    }

    /// <summary>Drops the armed skill, the selected cave and the held cards. Safe to call from anywhere.</summary>
    public void CancelPending()
    {
        bool cancelled = false;

        var skills = SkillFlowController.Instance;
        if (skills != null && skills.IsSkillActive)
        {
            skills.CancelActiveSkill();
            cancelled = true;
        }

        var map = GatherMapController.Instance;
        if (map != null && map.SelectedNode != null)
        {
            map.ClearSelection();
            cancelled = true;
        }

        if (rosterView == null) rosterView = FindAnyObjectByType<UnitRosterView>();
        if (clearCards && rosterView != null && rosterView.HasSelection)
        {
            rosterView.ClearSelection();
            cancelled = true;
        }

        if (cancelled) UiClickSound.Play(cancelSound);
    }
}
