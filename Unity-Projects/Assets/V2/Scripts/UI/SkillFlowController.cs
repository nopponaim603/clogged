using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the skill-trigger flow: pick a card in the hand, press Cook / Heal /
/// Craft, then confirm on the shared <see cref="DispatchActionPanel"/>.
///
/// Cook and Craft queue rounds (left click adds one, right click clears);
/// Heal is one shot and asks for a patient afterwards. All numbers come from
/// <see cref="BaseWorkController"/> - nothing is hard-coded here.
///
/// While a skill owns the confirm button this controller is the only one
/// allowed to react to it; <see cref="GatherDispatchPanel"/> gates on
/// <see cref="IsSkillActive"/> so the two flows never fight.
/// </summary>
public class SkillFlowController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UnitRosterView rosterView;
    [SerializeField] private DispatchActionPanel actionPanel;
    [SerializeField] private SkillTriggerPanel skillPanel;
    [SerializeField] private WarningView warningView;

    /// <summary>Fired when the pending skill starts or ends, so the gather flow can re-take the button.</summary>
    public event Action StateChanged;

    public static SkillFlowController Instance { get; private set; }

    public SkillKind Armed => armed;
    public bool IsSkillActive => armed != SkillKind.None;

    private SkillKind armed = SkillKind.None;
    private int pendingRounds;
    private bool healUseMedicine;
    private UnitCharacter worker;
    private UnitCharacter patient;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (skillPanel != null)
        {
            skillPanel.CookClicked -= HandleCookClicked;
            skillPanel.CookCleared -= HandleCookCleared;
            skillPanel.CraftClicked -= HandleCraftClicked;
            skillPanel.CraftCleared -= HandleCraftCleared;
            skillPanel.HealClicked -= HandleHealClicked;
            skillPanel.HealCancelled -= HandleHealCancelled;
            skillPanel.HealModeToggled -= HandleHealModeToggled;
        }
        if (actionPanel != null) actionPanel.Clicked -= HandleConfirm;
        if (rosterView != null) rosterView.OnSelectionChanged -= HandleSelectionChanged;
    }

    private void Start()
    {
        if (rosterView == null) rosterView = FindAnyObjectByType<UnitRosterView>();
        if (actionPanel == null) actionPanel = FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);
        if (skillPanel == null) skillPanel = FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include);
        if (warningView == null) warningView = FindAnyObjectByType<WarningView>();

        if (skillPanel != null)
        {
            skillPanel.CookClicked += HandleCookClicked;
            skillPanel.CookCleared += HandleCookCleared;
            skillPanel.CraftClicked += HandleCraftClicked;
            skillPanel.CraftCleared += HandleCraftCleared;
            skillPanel.HealClicked += HandleHealClicked;
            skillPanel.HealCancelled += HandleHealCancelled;
            skillPanel.HealModeToggled += HandleHealModeToggled;
        }
        if (actionPanel != null) actionPanel.Clicked += HandleConfirm;
        if (rosterView != null) rosterView.OnSelectionChanged += HandleSelectionChanged;

        healUseMedicine = HasMedicine();
        RefreshPanel();
        RefreshConfirm();
        RaiseStateChanged();
    }

    private void Update()
    {
        // Resources, hurt units and AP all move underneath us (gathering, day
        // end, another heal), so the cheap checks are re-run rather than cached.
        if (skillPanel != null)
        {
            skillPanel.SetHealAvailable(AnyHurtUnit());
            skillPanel.SetHealMode(healUseMedicine, HasMedicine());
        }

        if (!HasMedicine() && healUseMedicine)
        {
            healUseMedicine = false;
            if (armed != SkillKind.None) RefreshConfirm();
        }

        if (!IsSkillActive) return;

        if (armed == SkillKind.Heal)
        {
            UnitCharacter newPatient = SelectedUnit();
            if (newPatient != patient)
            {
                patient = newPatient;
                RefreshConfirm();
            }
            return;
        }

        // Cook / Craft act on the single card the skill mode allows. Swapping
        // cards re-targets the queue, clearing it drops the plan entirely.
        UnitCharacter picked = SelectedUnit();
        if (picked == worker) return;

        if (picked == null)
        {
            Cancel();
            return;
        }

        worker = picked;
        ClampQueueToAffordable();
        RefreshPanel();
        RefreshConfirm();
    }

    // ------------------------------------------------------------------ input

    private void HandleCookClicked() => QueueRound(SkillKind.Cook);
    private void HandleCraftClicked() => QueueRound(SkillKind.Craft);
    private void HandleCookCleared() => ClearQueue(SkillKind.Cook);
    private void HandleCraftCleared() => ClearQueue(SkillKind.Craft);
    private void HandleHealClicked() => ArmHeal();
    private void HandleHealCancelled() => Cancel();

    private void HandleHealModeToggled()
    {
        if (!HasMedicine()) return;

        healUseMedicine = !healUseMedicine;
        skillPanel.SetHealMode(healUseMedicine, HasMedicine());
        if (armed == SkillKind.Heal) RefreshConfirm();
    }

    private void HandleSelectionChanged()
    {
        if (!IsSkillActive) return;

        if (armed == SkillKind.Heal)
        {
            patient = SelectedUnit();
            RefreshConfirm();
        }
    }

    private void HandleConfirm()
    {
        if (!IsSkillActive) return;

        var controller = BaseWorkController.Instance;
        if (controller == null) return;

        switch (armed)
        {
            case SkillKind.Cook:
                if (pendingRounds > 0) controller.Cook(worker, pendingRounds);
                break;
            case SkillKind.Craft:
                if (pendingRounds > 0) controller.UseWorkshop(worker, pendingRounds);
                break;
            case SkillKind.Heal:
                if (worker != null && patient != null) controller.Heal(worker, patient, healUseMedicine);
                break;
        }

        Reset();
    }

    // ------------------------------------------------------------------ flow

    private void QueueRound(SkillKind kind)
    {
        var controller = BaseWorkController.Instance;
        if (controller == null) return;

        UnitCharacter unit = SelectedUnit();
        if (unit == null)
        {
            Warn("Pick a character card first.");
            return;
        }

        bool isCook = kind == SkillKind.Cook;
        if (isCook ? !controller.CanCook(unit) : !controller.CanUseWorkshop(unit))
        {
            Warn(isCook
                ? $"{unit.unitName} cannot cook right now (needs AP and cooking material)."
                : $"{unit.unitName} cannot craft right now (needs AP and materials).");
            return;
        }

        if (armed != kind)
        {
            // Switching skills drops the other queue, and narrows the hand to a single card.
            armed = kind;
            pendingRounds = 0;
            worker = unit;
            patient = null;
            EnterSkillMode();
            RaiseStateChanged();
        }

        int max = isCook ? controller.GetMaxCookRounds(worker) : controller.GetMaxWorkshopRounds(worker);
        if (pendingRounds >= max)
        {
            Warn(isCook
                ? "No AP or cooking material left for another round."
                : "No AP or materials left for another round.");
            RefreshPanel();
            RefreshConfirm();
            return;
        }

        pendingRounds++;
        RefreshPanel();
        RefreshConfirm();
    }

    private void ClearQueue(SkillKind kind)
    {
        if (armed != kind) return;

        // Right click only drops the queued rounds - the card stays selected.
        pendingRounds = 0;
        Disarm(clearSelection: false);
    }

    private void ArmHeal()
    {
        var controller = BaseWorkController.Instance;
        if (controller == null) return;

        if (!AnyHurtUnit())
        {
            Warn("Nobody needs healing.");
            return;
        }

        UnitCharacter healer = SelectedUnit();
        if (healer == null)
        {
            Warn("Pick the character who will heal first.");
            return;
        }
        if (!healer.CanBeDispatched)
        {
            Warn($"{healer.unitName} cannot do base work right now.");
            return;
        }

        armed = SkillKind.Heal;
        worker = healer;
        patient = null;
        pendingRounds = 0;
        healUseMedicine = HasMedicine();

        // The hand now targets the patient: only hurt units stay clickable.
        if (rosterView != null)
        {
            rosterView.ClearSelection();
            rosterView.SetMode(CharacterHandPanel.SelectionMode.HealTarget);
        }

        RefreshPanel();
        RefreshConfirm();
        RaiseStateChanged();
    }

    private void Cancel()
    {
        if (!IsSkillActive) return;
        Disarm(clearSelection: true);
    }

    /// <summary>Tears the pending skill down and hands the confirm button back to the gather flow.</summary>
    private void Reset() => Disarm(clearSelection: true);

    private void Disarm(bool clearSelection)
    {
        armed = SkillKind.None;
        pendingRounds = 0;
        worker = null;
        patient = null;

        if (rosterView != null)
        {
            if (clearSelection) rosterView.ClearSelection();
            rosterView.SetMode(CharacterHandPanel.SelectionMode.Dispatch);
            rosterView.RefreshAvailability();
        }

        RefreshPanel();
        RefreshConfirm();
        RaiseStateChanged();
    }

    private void EnterSkillMode()
    {
        rosterView?.SetMode(CharacterHandPanel.SelectionMode.Skill);
    }

    private void RaiseStateChanged() => StateChanged?.Invoke();

    // --------------------------------------------------------------- painting

    private void RefreshPanel()
    {
        if (skillPanel == null) return;

        skillPanel.SetCookCount(armed == SkillKind.Cook ? pendingRounds : 0);
        skillPanel.SetCraftCount(armed == SkillKind.Craft ? pendingRounds : 0);
        skillPanel.SetArmed(armed);
        skillPanel.SetHealAvailable(AnyHurtUnit());
        skillPanel.SetHealMode(healUseMedicine, HasMedicine());
    }

    /// <summary>The confirm button only exists while a skill has something to confirm.</summary>
    private void RefreshConfirm()
    {
        if (actionPanel == null) return;

        switch (armed)
        {
            case SkillKind.Cook:
            case SkillKind.Craft:
                if (pendingRounds <= 0) actionPanel.Hide();
                else actionPanel.Show(armed == SkillKind.Cook ? "COOK" : "CRAFT", true);
                break;

            case SkillKind.Heal:
                if (patient == null) actionPanel.Hide();
                else actionPanel.Show("HEAL", BaseWorkController.Instance != null &&
                                             BaseWorkController.Instance.CanHeal(worker, patient, healUseMedicine));
                break;

            default:
                actionPanel.Hide();
                break;
        }
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Keeps a queued count inside what the current worker can still afford.</summary>
    private void ClampQueueToAffordable()
    {
        var controller = BaseWorkController.Instance;
        if (controller == null || worker == null)
        {
            pendingRounds = 0;
            return;
        }

        int max = armed == SkillKind.Cook
            ? controller.GetMaxCookRounds(worker)
            : controller.GetMaxWorkshopRounds(worker);
        pendingRounds = Mathf.Clamp(pendingRounds, 0, Mathf.Max(0, max));
    }

    private UnitCharacter SelectedUnit()
    {
        if (rosterView == null) return null;

        List<UnitCharacter> selected = rosterView.GetSelectedUnits();
        return selected.Count > 0 ? selected[0] : null;
    }

    private static bool HasMedicine()
    {
        return RunResources.Instance != null && RunResources.Instance.Medicine >= 1;
    }

    private static bool AnyHurtUnit()
    {
        if (UnitManager.Instance == null) return false;

        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit != null && unit.IsAlive && unit.state == UnitState.Hurt) return true;
        }
        return false;
    }

    private void Warn(string message)
    {
        if (warningView != null) warningView.Show(message, WarningKind.Error);
        Debug.LogWarning("[SkillFlowController] " + message);
    }
}
