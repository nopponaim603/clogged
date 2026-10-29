using System.Text;
using UnityEngine;

/// <summary>
/// Turns gameplay events into log lines. Kept separate from LogMessagePanel so
/// the panel stays a pure renderer and the wording lives in one place.
///
/// Only "can't do that" situations get a Warning kind, which is what puts them
/// on the centre-screen warning as well as the log. Everything else is
/// informational and stays in the log.
/// </summary>
public class RunLogNarrator : MonoBehaviour
{
    private void Start()
    {
        if (BaseWorkController.Instance != null)
        {
            BaseWorkController.Instance.OnBaseWorkCompleted += HandleBaseWork;
        }
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayEnded += HandleDayEnded;
            DayManager.Instance.OnRunEnded += HandleRunEnded;
        }
        if (GatherDispatchController.Instance != null)
        {
            GatherDispatchController.Instance.OnGatherResolved += HandleGatherResolved;
        }

        RunLog.Add(LogKind.System, "Run started.");
    }

    private void OnDestroy()
    {
        if (BaseWorkController.Instance != null)
        {
            BaseWorkController.Instance.OnBaseWorkCompleted -= HandleBaseWork;
        }
        if (DayManager.Instance != null)
        {
            DayManager.Instance.OnDayEnded -= HandleDayEnded;
            DayManager.Instance.OnRunEnded -= HandleRunEnded;
        }
        if (GatherDispatchController.Instance != null)
        {
            GatherDispatchController.Instance.OnGatherResolved -= HandleGatherResolved;
        }
    }

    private void HandleBaseWork(BaseWorkResult result)
    {
        if (result == null) return;

        string who = result.worker != null ? result.worker.unitName : "Someone";

        if (result.blocked)
        {
            RunLog.Add(LogKind.Warning, $"{who} can't do that right now ({JobName(result.workType)}).");
            return;
        }

        switch (result.workType)
        {
            case BaseWorkType.Cooking:
                RunLog.Add(LogKind.Success,
                    $"{who} cooked {result.repeatsPerformed}x — +{result.foodProduced} Food, -{result.cookingMaterialConsumed} Cooking Material.");
                break;

            case BaseWorkType.Healing:
                string patient = result.healedUnit != null ? result.healedUnit.unitName : "patient";
                string method = result.usedMedicine ? "with Medicine" : "without Medicine (longer)";
                RunLog.Add(LogKind.Success, $"{who} healed {patient} {method}.");
                break;

            case BaseWorkType.Workshop:
                RunLog.Add(LogKind.Success,
                    $"{who} crafted {result.repeatsPerformed}x — +{result.medicineProduced} Medicine.");
                break;
        }
    }

    private void HandleDayEnded(DayEndReport report)
    {
        if (report == null) return;

        if (report.starved.Count == 0)
        {
            RunLog.Add(LogKind.DayEnd, $"Day {report.dayAfter} — everyone ate ({report.fed.Count} fed).");
            return;
        }

        var sb = new StringBuilder();
        sb.Append($"Day {report.dayAfter} — food ran out: ");
        for (int i = 0; i < report.starved.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(report.starved[i].unitName);
            sb.Append(" starved");
        }
        RunLog.Add(LogKind.Warning, sb.ToString());

        foreach (var lost in report.lostSignal)
        {
            RunLog.Add(LogKind.Fail, $"{lost.unitName} is Lost Signal.");
        }
    }

    private void HandleGatherResolved(GatherDispatchResult result)
    {
        if (result == null) return;

        var sb = new StringBuilder();
        sb.Append($"{result.zone} trip — {result.tier} (roll {result.rollValue}/{result.successValue})");

        if (result.mainLootGranted.HasValue)
        {
            var loot = result.mainLootGranted.Value;
            sb.Append($", +{loot.amount} {loot.type}");
        }
        if (result.bonusLootGranted.HasValue)
        {
            var bonus = result.bonusLootGranted.Value;
            sb.Append($", bonus +{bonus.amount} {bonus.type}");
        }

        bool bad = result.tier == GatherResultTier.Failure || result.tier == GatherResultTier.Fumble;
        RunLog.Add(bad ? LogKind.Fail : LogKind.Success, sb.ToString());

        if (result.hurtUnit != null)
        {
            RunLog.Add(LogKind.Fail, $"{result.hurtUnit.unitName} was hurt.");
        }
        if (result.missingUnit != null)
        {
            RunLog.Add(LogKind.Fail, $"{result.missingUnit.unitName} is Lost Signal.");
        }

        foreach (var kind in result.coreItemsObtained)
        {
            RunLog.Add(LogKind.Success,
                kind == CoreItemKind.A ? "Found the sealed box!" : "Found the key!");
        }
    }

    private void HandleRunEnded(RunOutcome outcome)
    {
        switch (outcome)
        {
            case RunOutcome.Win:
                RunLog.Add(LogKind.Success, "Both Core Items secured — you escape. WIN.");
                break;
            case RunOutcome.LoseDeadline:
                RunLog.Add(LogKind.Fail, "Day limit reached without both Core Items. LOSE.");
                break;
            case RunOutcome.LoseAllUnitsLost:
                RunLog.Add(LogKind.Fail, "Everyone is Lost Signal. LOSE.");
                break;
        }
    }

    private static string JobName(BaseWorkType type)
    {
        switch (type)
        {
            case BaseWorkType.Cooking: return "Cook";
            case BaseWorkType.Healing: return "Heal";
            case BaseWorkType.Workshop: return "Craft";
            default: return type.ToString();
        }
    }
}
