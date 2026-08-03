using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public MissionManager MissionManager;
    public UIManager UIManager;
    public Transform BaseTransform;

    public Crew SelectedCrew;
    public List<ResourceNode> SelectedTargets = new List<ResourceNode>();

    public int MaxQueueSize = 2;

    [Header("Path Preview")]
    [Tooltip("LineRenderer used to preview the crew currently being planned (not yet locked in).")]
    public LineRenderer PathLine;

    [Tooltip("Template LineRenderer cloned once per crew that gets locked into the dispatch queue, so you can see every planned route at once.")]
    public LineRenderer QueuedPathLinePrefab;

    private class QueuedPlan
    {
        public Crew Crew;
        public List<ResourceNode> Targets;
        public LineRenderer PreviewLine;
    }

    private readonly List<QueuedPlan> _queuedPlans = new List<QueuedPlan>();

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Crew crew = hit.collider.GetComponent<Crew>();
                if (crew != null)
                {
                    if (crew.IsBusy)
                    {
                        UIManager?.ShowMessage(crew.Name + " is already busy.");
                        return;
                    }

                    if (IsAlreadyQueued(crew))
                    {
                        UIManager?.ShowMessage(crew.Name + " is already queued for dispatch.");
                        return;
                    }

                    SelectedCrew = crew;
                    UIManager?.ShowMessage("Selected crew: " + crew.Name);
                    UpdatePathPreview();
                    return;
                }

                ResourceNode target = hit.collider.GetComponent<ResourceNode>();
                if (target != null)
                {
                    AddTargetToQueue(target);
                }
            }
        }

        // Space: lock the current crew + node queue in as one planned mission.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LockInCurrentPlan();
        }

        // Enter: send every locked-in crew out at once.
        if (Input.GetKeyDown(KeyCode.Return))
        {
            DispatchAll();
        }

        // Escape: cancel whatever crew/nodes are being planned right now
        // (does not touch crew already locked into the dispatch queue).
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearCurrentSelection();
        }
    }

    private bool IsAlreadyQueued(Crew crew)
    {
        foreach (var plan in _queuedPlans)
        {
            if (plan.Crew == crew) return true;
        }
        return false;
    }

    private void AddTargetToQueue(ResourceNode target)
    {
        if (SelectedCrew == null)
        {
            UIManager?.ShowMessage("Select a crew first.");
            return;
        }

        if (SelectedTargets.Count >= MaxQueueSize)
        {
            UIManager?.ShowMessage("Queue full. Max " + MaxQueueSize + " nodes.");
            return;
        }

        if (SelectedTargets.Contains(target))
        {
            UIManager?.ShowMessage("This node is already queued.");
            return;
        }

        SelectedTargets.Add(target);
        UIManager?.ShowMessage("Queued " + target.Type + " for " + SelectedCrew.Name);
        UpdatePathPreview();
    }

    /// <summary>
    /// Locks the currently selected crew and their node queue into the
    /// dispatch list. Does NOT start the mission yet — that only happens
    /// when DispatchAll() runs. Lets the player line up several crew before
    /// sending everyone out together.
    /// </summary>
    private void LockInCurrentPlan()
    {
        if (SelectedCrew == null || SelectedTargets.Count == 0)
        {
            UIManager?.ShowMessage("Choose a crew and add at least one node.");
            return;
        }

        var plan = new QueuedPlan
        {
            Crew = SelectedCrew,
            Targets = new List<ResourceNode>(SelectedTargets),
            PreviewLine = CreateQueuedPreviewLine(SelectedCrew, SelectedTargets)
        };

        _queuedPlans.Add(plan);
        UIManager?.ShowMessage(SelectedCrew.Name + " queued (" + _queuedPlans.Count + " ready). Select another crew, or press Enter to dispatch all.");

        ClearCurrentSelection();
    }

    /// <summary>
    /// Starts every locked-in crew's mission chain at the same time.
    /// </summary>
    private void DispatchAll()
    {
        if (_queuedPlans.Count == 0)
        {
            UIManager?.ShowMessage("No crew queued yet. Press Space to lock one in first.");
            return;
        }

        foreach (var plan in _queuedPlans)
        {
            MissionManager.StartMissionChain(plan.Crew, plan.Targets, BaseTransform);

            if (plan.PreviewLine != null)
                Destroy(plan.PreviewLine.gameObject);
        }

        _queuedPlans.Clear();
        ClearCurrentSelection();
    }

    private void ClearCurrentSelection()
    {
        SelectedCrew = null;
        SelectedTargets.Clear();
        ClearPathPreview();
    }

    private LineRenderer CreateQueuedPreviewLine(Crew crew, List<ResourceNode> targets)
    {
        if (QueuedPathLinePrefab == null) return null;

        LineRenderer line = Instantiate(QueuedPathLinePrefab, transform);
        line.gameObject.SetActive(true);

        var points = new List<Vector3> { crew.transform.position };
        foreach (var target in targets)
            points.Add(target.transform.position);
        if (BaseTransform != null)
            points.Add(BaseTransform.position);

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());

        return line;
    }

    /// <summary>
    /// Draws crew position -> queued node 1 -> queued node 2 -> ... -> base
    /// for the crew currently being planned (before locking them in).
    /// </summary>
    private void UpdatePathPreview()
    {
        if (PathLine == null) return;

        if (SelectedCrew == null)
        {
            ClearPathPreview();
            return;
        }

        var points = new List<Vector3> { SelectedCrew.transform.position };

        foreach (var target in SelectedTargets)
        {
            points.Add(target.transform.position);
        }

        if (SelectedTargets.Count > 0 && BaseTransform != null)
        {
            points.Add(BaseTransform.position);
        }

        PathLine.positionCount = points.Count;
        PathLine.SetPositions(points.ToArray());
        PathLine.enabled = points.Count > 1;
    }

    private void ClearPathPreview()
    {
        if (PathLine == null) return;
        PathLine.positionCount = 0;
        PathLine.enabled = false;
    }
}
