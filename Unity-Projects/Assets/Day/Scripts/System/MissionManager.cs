using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public ResourceManager ResourceManager;
    public UIManager UIManager;
    public TimeManager TimeManager;
    public CrewManager CrewManager;

    [Header("Monster Encounters")]
    [Tooltip("Chance an encounter is rolled after each leg of the journey (per node approached). Independent of which node it is.")]
    [Range(0f, 1f)]
    public float EncounterChancePerLeg = 0.15f;
    public int MinEncounterDamage = 10;
    public int MaxEncounterDamage = 30;
    [Tooltip("Each point of effective hunting proficiency reduces incoming damage by this much.")]
    public float HuntingDamageReduction = 5f;

    [Header("Node Cooperation")]
    [Tooltip("Yield multiplier applied when another crew is present (arrived while gathering wasn't finished yet) at the same node.")]
    [Range(1f, 3f)]
    public float HelpYieldMultiplier = 1.5f;

    // Tracks who is currently gathering at each node, across all crews'
    // independent mission coroutines, so overlapping visits can be detected.
    private class WorkerEntry
    {
        public Crew Crew;
        public bool HadHelp;
    }

    private readonly Dictionary<ResourceNode, List<WorkerEntry>> _activeWorkers =
        new Dictionary<ResourceNode, List<WorkerEntry>>();

    /// <summary>
    /// Clears any leftover worker-presence tracking. Call this at the start
    /// of each new day as a safety net (e.g. from GameManager.BeginDay()).
    /// </summary>
    public void ClearActiveWorkers()
    {
        _activeWorkers.Clear();
    }

    public void StartMissionChain(Crew crew, List<ResourceNode> targets, Transform baseTransform)
    {
        if (crew == null || targets == null || targets.Count == 0 || baseTransform == null)
        {
            UIManager?.ShowMessage("Mission chain cannot start. Check crew, nodes, and base.");
            return;
        }

        if (TimeManager != null && !TimeManager.RoundActive)
        {
            UIManager?.ShowMessage("Time is over. Start a new day.");
            return;
        }

        if (crew.IsDown)
        {
            UIManager?.ShowMessage(crew.Name + " is down and needs to recover first.");
            return;
        }

        if (CrewManager != null && !CrewManager.AssignMission(crew.Id))
        {
            UIManager?.ShowMessage(crew.Name + " could not be assigned right now.");
            return;
        }

        if (TimeManager != null)
            TimeManager.IsMissionRunning = true;

        var workOrder = new List<ResourceNode>(targets);
        StartCoroutine(ExecuteMissionChain(crew, workOrder, baseTransform));
    }

    private IEnumerator ExecuteMissionChain(Crew crew, List<ResourceNode> targets, Transform baseTransform)
    {
        crew.IsBusy = true;
        bool wentDown = false;
        var visited = new List<(ResourceNode node, bool hadHelp)>();

        try
        {
            foreach (var target in targets)
            {
                yield return MoveTo(crew.transform, target.transform.position, crew.Speed);

                if (RollEncounter(crew))
                {
                    wentDown = true;
                    UIManager?.ShowMessage(crew.Name + " was ambushed by a monster and knocked down!");
                    break;
                }

                WorkerEntry entry = RegisterWorker(target, crew);

                float actionTime = target.GetActionTime(crew.GetEffectiveGathering());
                yield return new WaitForSeconds(actionTime);

                // Read HadHelp AFTER waiting — another crew may have joined
                // partway through and retroactively flagged this entry.
                bool hadHelp = entry.HadHelp;
                UnregisterWorker(target, entry);

                visited.Add((target, hadHelp));
            }

            yield return MoveTo(crew.transform, baseTransform.position, crew.Speed);
            crew.Position = baseTransform.position;

            if (wentDown)
            {
                UIManager?.ShowMessage(crew.Name + " was carried back to base and needs to recover.");
            }
            else
            {
                string message = "";
                foreach (var visit in visited)
                {
                    MissionResult result = ExecuteMission(crew, visit.node, visit.hadHelp);
                    message += result.Message + " ";
                }

                UIManager?.ShowMessage(message.Trim() + " " + crew.Name + " returned to base.");
            }
        }
        finally
        {
            if (TimeManager != null)
                TimeManager.IsMissionRunning = false;

            if (CrewManager != null)
            {
                if (wentDown || crew.IsDown)
                    CrewManager.MarkCrewDown(crew.Id);
                else
                    CrewManager.CompleteMission(crew.Id);
            }
            else
            {
                crew.IsBusy = false;
            }
        }
    }

    /// <summary>
    /// Registers a crew as currently gathering at a node. If someone is
    /// already there, both the newcomer and everyone already present get
    /// flagged as having had help. Returns the entry to check after waiting.
    /// </summary>
    private WorkerEntry RegisterWorker(ResourceNode node, Crew crew)
    {
        if (!_activeWorkers.TryGetValue(node, out var workers))
        {
            workers = new List<WorkerEntry>();
            _activeWorkers[node] = workers;
        }

        // A helper just showed up for anyone still working here.
        foreach (var existing in workers)
            existing.HadHelp = true;

        var entry = new WorkerEntry { Crew = crew, HadHelp = workers.Count > 0 };
        workers.Add(entry);
        return entry;
    }

    private void UnregisterWorker(ResourceNode node, WorkerEntry entry)
    {
        if (_activeWorkers.TryGetValue(node, out var workers))
        {
            workers.Remove(entry);
            if (workers.Count == 0)
                _activeWorkers.Remove(node);
        }
    }

    /// <summary>
    /// Rolls whether a monster encounter happens on this leg of the trip, and
    /// applies damage if it does. Not tied to any specific node's danger level.
    /// Returns true if the crew was knocked Down as a result.
    /// </summary>
    private bool RollEncounter(Crew crew)
    {
        if (Random.value >= EncounterChancePerLeg) return false;

        int rawDamage = Random.Range(MinEncounterDamage, MaxEncounterDamage + 1);
        int mitigated = Mathf.Max(1, Mathf.RoundToInt(rawDamage - crew.GetEffectiveHunting() * HuntingDamageReduction));

        return crew.TakeDamage(mitigated);
    }

    private IEnumerator MoveTo(Transform mover, Vector3 destination, float speed)
    {
        while (Vector3.Distance(mover.position, destination) > 0.1f)
        {
            mover.position = Vector3.MoveTowards(mover.position, destination, speed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Backward-compatible overload — no cooperation bonus.
    /// </summary>
    public MissionResult ExecuteMission(Crew crew, ResourceNode target)
    {
        return ExecuteMission(crew, target, false);
    }

    public MissionResult ExecuteMission(Crew crew, ResourceNode target, bool hadHelp)
    {
        int amount = Mathf.FloorToInt(target.Amount * (0.5f + crew.GetEffectiveGathering() * 0.3f));

        if (hadHelp)
            amount = Mathf.FloorToInt(amount * HelpYieldMultiplier);

        ResourceManager.AddResource(target.Type.ToString().ToLower(), amount);

        string helpNote = hadHelp ? " (with help!)" : "";
        return new MissionResult(
            true,
            crew.Name + " gained " + amount + " " + target.Type + helpNote + "."
        );
    }
}
