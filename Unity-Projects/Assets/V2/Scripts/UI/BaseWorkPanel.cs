using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Real UI for base work (doc sections 10 and 12-15). Flow: pick a job
/// (Cook / Heal / Workshop), pick the unit who does it, pick how many rounds
/// (Cook, Workshop) or which hurt unit to treat (Heal), read the cost/gain
/// preview, then confirm. Confirming can't be undone, per the doc.
///
/// All numbers come from BaseWorkController, so tuning them there updates
/// this UI automatically. Dropdown options are rebuilt only when something
/// relevant changes (job picked, action confirmed, new day, trip finished),
/// never every frame, so an open dropdown doesn't get reset under the player.
/// </summary>
public class BaseWorkPanel : MonoBehaviour
{
    [Header("Job buttons")]
    [SerializeField] private Button cookButton;
    [SerializeField] private Button healButton;
    [SerializeField] private Button workshopButton;

    [Header("Selection")]
    [SerializeField] private TMP_Dropdown workerDropdown;
    [Tooltip("Parent object of the patient label + dropdown. Shown only for Heal.")]
    [SerializeField] private GameObject patientRoot;
    [SerializeField] private TMP_Dropdown patientDropdown;

    [Header("Rounds (Cook / Workshop)")]
    [Tooltip("Parent object of the - / + buttons and the number. Hidden for Heal.")]
    [SerializeField] private GameObject roundsRoot;
    [SerializeField] private Button roundsMinusButton;
    [SerializeField] private Button roundsPlusButton;
    [SerializeField] private TMP_Text roundsText;

    [Header("Info")]
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;

    // Doc item 14: units good at the job are highlighted green, bad ones red.
    private const string AptitudeHex = "#5BD65B";
    private const string NonAptitudeHex = "#E65C5C";
    private const string UnavailableHex = "#8A8A8A";

    private BaseWorkType? job;
    private int rounds = 1;
    private int maxRounds;
    private bool lastTripState;
    private bool initialized;

    // Dropdown index -> unit. Kept in the same order as the dropdown's options.
    private readonly List<UnitCharacter> workerOptions = new List<UnitCharacter>();
    private readonly List<UnitCharacter> patientOptions = new List<UnitCharacter>();

    private UnitCharacter SelectedWorker =>
        workerDropdown != null && workerDropdown.value >= 0 && workerDropdown.value < workerOptions.Count
            ? workerOptions[workerDropdown.value]
            : null;

    private UnitCharacter SelectedPatient =>
        patientDropdown != null && patientDropdown.value >= 0 && patientDropdown.value < patientOptions.Count
            ? patientOptions[patientDropdown.value]
            : null;

    private void Start()
    {
        if (!HasRequiredReferences()) return;

        cookButton.onClick.AddListener(() => SelectJob(BaseWorkType.Cooking));
        healButton.onClick.AddListener(() => SelectJob(BaseWorkType.Healing));
        workshopButton.onClick.AddListener(() => SelectJob(BaseWorkType.Workshop));

        workerDropdown.onValueChanged.AddListener(_ => OnWorkerChanged());
        patientDropdown.onValueChanged.AddListener(_ => RefreshPreview());

        roundsMinusButton.onClick.AddListener(() => ChangeRounds(-1));
        roundsPlusButton.onClick.AddListener(() => ChangeRounds(1));
        confirmButton.onClick.AddListener(OnConfirm);

        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted += HandleDayStarted;
        }
        if (GatherMapController.Instance != null)
        {
            GatherMapController.Instance.OnTripResolved += HandleTripResolved;
        }

        patientRoot.SetActive(false);
        roundsRoot.SetActive(false);

        RebuildAll();
        SetMessage("Pick a job, then a unit");
        initialized = true;
    }

    private void OnDestroy()
    {
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayStarted -= HandleDayStarted;
        }
        if (GatherMapController.Instance != null)
        {
            GatherMapController.Instance.OnTripResolved -= HandleTripResolved;
        }
    }

    private void Update()
    {
        if (!initialized) return; // Start bailed out (missing Inspector references) - don't touch null UI

        // The confirm button must lock while a trip is walking out, so a unit about to be dispatched can't be given base work.
        var map = GatherMapController.Instance;
        bool tripNow = map != null && map.TripInProgress;
        if (tripNow != lastTripState)
        {
            lastTripState = tripNow;
            RefreshPreview();
        }
    }

    private bool HasRequiredReferences()
    {
        bool ok = true;
        void Check(Object obj, string name)
        {
            if (obj == null)
            {
                Debug.LogError($"[BaseWorkPanel] '{name}' is not assigned in the Inspector.");
                ok = false;
            }
        }

        Check(cookButton, nameof(cookButton));
        Check(healButton, nameof(healButton));
        Check(workshopButton, nameof(workshopButton));
        Check(workerDropdown, nameof(workerDropdown));
        Check(patientRoot, nameof(patientRoot));
        Check(patientDropdown, nameof(patientDropdown));
        Check(roundsRoot, nameof(roundsRoot));
        Check(roundsMinusButton, nameof(roundsMinusButton));
        Check(roundsPlusButton, nameof(roundsPlusButton));
        Check(roundsText, nameof(roundsText));
        Check(previewText, nameof(previewText));
        Check(messageText, nameof(messageText));
        Check(confirmButton, nameof(confirmButton));
        return ok;
    }

    // ---------------------------------------------------------------- events

    private void HandleDayStarted(int day)
    {
        rounds = 1;
        RebuildAll();
    }

    private void HandleTripResolved(GatherDispatchResult result)
    {
        RebuildAll();
    }

    private void OnWorkerChanged()
    {
        rounds = 1;
        RebuildPatientOptions();
        RefreshPreview();
    }

    private void SelectJob(BaseWorkType type)
    {
        job = type;
        rounds = 1;

        patientRoot.SetActive(type == BaseWorkType.Healing);
        roundsRoot.SetActive(type != BaseWorkType.Healing);

        RebuildAll();
        SetMessage("");
    }

    private void ChangeRounds(int delta)
    {
        rounds = Mathf.Clamp(rounds + delta, 1, Mathf.Max(1, maxRounds));
        RefreshPreview();
    }

    // -------------------------------------------------------------- dropdowns

    private void RebuildAll()
    {
        RebuildWorkerOptions();
        RebuildPatientOptions();
        RefreshPreview();
    }

    /// <summary>Doc item 14: usable units are listed first; the rest follow, greyed out.</summary>
    private void RebuildWorkerOptions()
    {
        UnitCharacter previous = SelectedWorker;
        workerOptions.Clear();

        var units = UnitManager.Instance != null ? UnitManager.Instance.Units : null;
        if (units != null)
        {
            foreach (var unit in units)
            {
                if (unit.IsAlive && unit.CanBeDispatched) workerOptions.Add(unit);
            }
            foreach (var unit in units)
            {
                if (unit.IsAlive && !unit.CanBeDispatched) workerOptions.Add(unit);
            }
        }

        var labels = new List<string>();
        foreach (var unit in workerOptions)
        {
            labels.Add(WorkerLabel(unit));
        }

        workerDropdown.ClearOptions();
        workerDropdown.AddOptions(labels);

        int index = previous != null ? workerOptions.IndexOf(previous) : 0;
        workerDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        workerDropdown.RefreshShownValue();
    }

    private string WorkerLabel(UnitCharacter unit)
    {
        string stateNote = unit.state == UnitState.Idle ? "" : $", {unit.state}";
        string text = $"{unit.unitName} (AP {unit.currentActionPoints}/{unit.maxActionPoints}{stateNote})";

        if (!unit.CanBeDispatched)
        {
            return $"<color={UnavailableHex}>{text}</color>";
        }

        // Only cooking has an aptitude tag in the doc, so only cooking gets the green/red highlight.
        if (job == BaseWorkType.Cooking)
        {
            if (unit.HasAptitude(AptitudeTag.Cooking)) return $"<color={AptitudeHex}>{text}</color>";
            if (unit.HasNonAptitude(AptitudeTag.Cooking)) return $"<color={NonAptitudeHex}>{text}</color>";
        }

        return text;
    }

    /// <summary>Doc item 15.2: the patient list holds only Hurt units (never the worker themself).</summary>
    private void RebuildPatientOptions()
    {
        UnitCharacter worker = SelectedWorker;
        UnitCharacter previous = SelectedPatient;
        patientOptions.Clear();

        var units = UnitManager.Instance != null ? UnitManager.Instance.Units : null;
        if (units != null)
        {
            foreach (var unit in units)
            {
                if (unit.IsAlive && unit.state == UnitState.Hurt && unit != worker) patientOptions.Add(unit);
            }
        }

        var labels = new List<string>();
        foreach (var unit in patientOptions)
        {
            labels.Add(unit.unitName);
        }
        if (labels.Count == 0)
        {
            labels.Add("(no one is hurt)");
        }

        patientDropdown.ClearOptions();
        patientDropdown.AddOptions(labels);

        int index = previous != null ? patientOptions.IndexOf(previous) : 0;
        patientDropdown.SetValueWithoutNotify(Mathf.Max(0, index));
        patientDropdown.RefreshShownValue();
    }

    // ---------------------------------------------------------------- preview

    /// <summary>Recomputes the cost/gain text, the rounds stepper and whether Confirm is allowed.</summary>
    private void RefreshPreview()
    {
        var controller = BaseWorkController.Instance;
        string preview = "";
        string reason = null;
        bool canConfirm = false;
        maxRounds = 0;

        if (controller == null)
        {
            reason = "BaseWorkController is missing from the scene";
        }
        else if (!job.HasValue)
        {
            preview = "Pick a job: Cook, Heal or Workshop";
        }
        else if (GatherMapController.Instance != null && GatherMapController.Instance.TripInProgress)
        {
            reason = "A trip is in progress";
        }
        else
        {
            UnitCharacter worker = SelectedWorker;
            if (worker == null)
            {
                reason = "No unit available";
            }
            else if (!worker.CanBeDispatched)
            {
                reason = $"{worker.unitName} can't work right now ({worker.state})";
            }
            else
            {
                switch (job.Value)
                {
                    case BaseWorkType.Cooking:
                        canConfirm = PreviewCooking(controller, worker, out preview, out reason);
                        break;
                    case BaseWorkType.Healing:
                        canConfirm = PreviewHealing(controller, worker, out preview, out reason);
                        break;
                    case BaseWorkType.Workshop:
                        canConfirm = PreviewWorkshop(controller, worker, out preview, out reason);
                        break;
                }
            }
        }

        previewText.text = reason == null ? preview : $"{preview}\n<color={NonAptitudeHex}>{reason}</color>".TrimStart('\n');
        confirmButton.interactable = canConfirm;

        roundsText.text = rounds.ToString();
        roundsMinusButton.interactable = canConfirm && rounds > 1;
        roundsPlusButton.interactable = canConfirm && rounds < maxRounds;
    }

    private bool PreviewCooking(BaseWorkController c, UnitCharacter worker, out string preview, out string reason)
    {
        preview = "";
        reason = null;

        maxRounds = c.GetMaxCookRounds(worker);
        if (maxRounds <= 0)
        {
            reason = worker.currentActionPoints < c.CookApPerRound(worker)
                ? $"{worker.unitName} needs {c.CookApPerRound(worker)} AP (has {worker.currentActionPoints})"
                : "Not enough Cooking Material";
            return false;
        }

        rounds = Mathf.Clamp(rounds, 1, maxRounds);
        string aptitudeNote = worker.HasAptitude(AptitudeTag.Cooking) ? " (cooking aptitude)" : "";
        preview =
            $"Cook x{rounds}\n" +
            $"Cost: {c.CookApPerRound(worker) * rounds} AP, {c.CookMaterialPerRound * rounds} Cooking Material\n" +
            $"Gain: {c.CookFoodPerRound(worker) * rounds} Food{aptitudeNote}";
        return true;
    }

    private bool PreviewHealing(BaseWorkController c, UnitCharacter worker, out string preview, out string reason)
    {
        preview = "";
        reason = null;

        UnitCharacter patient = SelectedPatient;
        if (patient == null)
        {
            reason = "No one else is hurt";
            return false;
        }

        c.GetHealPlan(worker, out bool useMedicine, out int apCost);
        preview =
            $"Heal {patient.unitName}\n" +
            (useMedicine
                ? $"Cost: {apCost} AP, 1 Medicine"
                : $"Cost: {apCost} AP (no Medicine in stock, so it takes longer)") +
            "\nResult: back to Ready";

        if (!c.CanHeal(worker, patient))
        {
            reason = $"{worker.unitName} needs {apCost} AP (has {worker.currentActionPoints})";
            return false;
        }
        return true;
    }

    private bool PreviewWorkshop(BaseWorkController c, UnitCharacter worker, out string preview, out string reason)
    {
        preview = "";
        reason = null;

        maxRounds = c.GetMaxWorkshopRounds(worker);
        if (maxRounds <= 0)
        {
            reason = worker.currentActionPoints < c.WorkshopApPerRound(worker)
                ? $"{worker.unitName} needs {c.WorkshopApPerRound(worker)} AP (has {worker.currentActionPoints})"
                : $"Not enough Materials (needs {c.WorkshopMaterialsPerRound})";
            return false;
        }

        rounds = Mathf.Clamp(rounds, 1, maxRounds);
        preview =
            $"Craft Medicine x{rounds} (max {maxRounds})\n" +
            $"Cost: {c.WorkshopApPerRound(worker) * rounds} AP, {c.WorkshopMaterialsPerRound * rounds} Materials\n" +
            $"Gain: {c.WorkshopMedicinePerRound * rounds} Medicine";
        return true;
    }

    // ---------------------------------------------------------------- confirm

    private void OnConfirm()
    {
        var controller = BaseWorkController.Instance;
        UnitCharacter worker = SelectedWorker;
        if (!job.HasValue || controller == null || worker == null) return;

        string message;

        switch (job.Value)
        {
            case BaseWorkType.Cooking:
            {
                BaseWorkResult r = controller.Cook(worker, rounds);
                message = r.repeatsPerformed > 0
                    ? $"{worker.unitName} cooked {r.repeatsPerformed} round(s): +{r.foodProduced} Food (used {r.cookingMaterialConsumed} Cooking Material)"
                    : "Couldn't cook - check AP and Cooking Material";
                break;
            }
            case BaseWorkType.Healing:
            {
                UnitCharacter patient = SelectedPatient;
                BaseWorkResult r = patient != null ? controller.Heal(worker, patient) : null;
                message = r != null && !r.blocked
                    ? $"{worker.unitName} healed {patient.unitName}" + (r.usedMedicine ? " (used 1 Medicine)" : " (no Medicine used)")
                    : "Couldn't heal - check AP and who is hurt";
                break;
            }
            default:
            {
                BaseWorkResult r = controller.UseWorkshop(worker, rounds);
                message = !r.blocked
                    ? $"{worker.unitName} crafted {r.repeatsPerformed} round(s): +{r.medicineProduced} Medicine"
                    : "Couldn't craft - check AP and Materials";
                break;
            }
        }

        rounds = 1;
        RebuildAll();
        SetMessage(message);
        Debug.Log("[BaseWorkPanel] " + message);
    }

    private void SetMessage(string text)
    {
        if (messageText != null) messageText.text = text;
    }
}
