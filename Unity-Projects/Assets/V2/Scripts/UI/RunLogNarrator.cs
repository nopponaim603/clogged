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
                if (result.blessedApplied)
                {
                    RunLog.Add(LogKind.Success, $"{patient} is Blessed (+10 on the next trip).");
                }
                break;

            case BaseWorkType.Workshop:
                if (result.workshopMode == WorkshopMode.BuffPotion)
                {
                    RunLog.Add(LogKind.Success,
                        $"{who} brewed {result.repeatsPerformed}x buff potion — +{result.successBonusGranted} success value for the party.");
                }
                else
                {
                    RunLog.Add(LogKind.Success,
                        $"{who} crafted {result.repeatsPerformed}x — +{result.medicineProduced} Medicine.");
                }
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

        foreach (var died in report.died)
        {
            RunLog.Add(LogKind.Fail, $"{died.unitName} died.");
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

        if (result.riskReduced > 0)
        {
            int risk = RunResources.Instance != null ? RunResources.Instance.Risk : 0;
            RunLog.Add(LogKind.System, $"The red fog thins — risk -{result.riskReduced} (now {risk}/{RunResources.MaxRisk}).");
        }

        foreach (var hurt in result.hurtUnits)
        {
            RunLog.Add(LogKind.Fail, $"{hurt.unitName} was hurt.");
        }

        if (result.jackpot)
        {
            RunLog.Add(LogKind.Success, "JACKPOT — a natural 1 took everything the cave had.");
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
                RunLog.Add(LogKind.Fail, "Everyone is dead. LOSE.");
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
