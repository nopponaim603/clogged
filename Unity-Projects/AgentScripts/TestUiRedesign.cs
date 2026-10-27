using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Single verification pass for the redesigned UI. Runs every check in one go so
/// a play-mode session is only needed once:
///   1. structure   - the old widgets are gone, both Core Item slots are wired
///   2. clickability- each interactive element survives a real EventSystem raycast
///   3. skill flow  - Cook/Craft queues, right-click clear, Heal in both modes
///   4. dispatch    - the shared confirm button and the instant trip
///   5. end day     - clicking the Day frame
///
/// Run in play mode with:
///   unity command script.run --file AgentScripts/TestUiRedesign.cs --entry TestUiRedesign.Run
/// </summary>
public static class TestUiRedesign
{
    static readonly StringBuilder Log = new StringBuilder();
    static int passed;
    static int failed;

    public static string Run()
    {
        Log.Clear();
        passed = 0;
        failed = 0;

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "FAILED: no Canvas - is Core.unity in play mode?";

        var roster = Object.FindAnyObjectByType<UnitRosterView>();
        var flow = Object.FindAnyObjectByType<SkillFlowController>();
        var action = Object.FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);
        var skill = Object.FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include);
        var dayFrame = canvas.transform.Find("StatusBar/DayFrame") as RectTransform;

        if (roster == null || flow == null || action == null || skill == null || dayFrame == null)
        {
            return "FAILED: missing UI (roster/flow/action/skill/dayFrame).";
        }

        ResetRun(roster);

        // --- 1. structure -------------------------------------------------
        Section("structure");
        CheckLegacyGone(canvas);
        CheckCoreItemSlots(canvas);
        Check("Day frame carries EndDayTrigger", dayFrame.GetComponent<EndDayTrigger>() != null);

        // --- 2. clickability ----------------------------------------------
        Section("clickability");
        CheckClickSurfaces(canvas);
        Check("no cave node sits under the Day frame", !NodeUnder(dayFrame));
        Check("raycast over the Day frame resolves to EndDayTrigger", DayFrameClickable(dayFrame));

        // --- 3. skill flow -------------------------------------------------
        Section("skill flow");
        // The panel repaints from Update(); this whole test runs inside one frame,
        // so nudge it before reading the button state.
        ForceSkillPanelRefresh(flow);
        Check("heal is disabled while nobody is hurt", !ButtonFor(skill, "HealButton").interactable);
        Check("hand starts in Dispatch mode with nothing armed",
            roster.HandPanel.Mode == CharacterHandPanel.SelectionMode.Dispatch && flow.Armed == SkillKind.None && !action.IsVisible);

        UnitCharacter chef = ByName("Boon");
        UnitCharacter patient = ByName("Cia");
        patient.state = UnitState.Hurt;
        roster.RefreshAvailability();

        ClickCard(roster, chef);
        Check("card click selects the chef", roster.SelectedUnit == chef);

        int apBefore = chef.currentActionPoints;
        int foodBefore = RunResources.Instance.Food;

        ClickButton(skill, "CookButton");
        Check("cook arms and queues 1 round, panel shows COOK",
            flow.Armed == SkillKind.Cook && PendingRounds(flow) == 1 &&
            roster.HandPanel.MaxSelection == 1 && action.IsVisible && action.CurrentLabel == "COOK");

        ClickButton(skill, "CookButton");
        Check("cook queues a second round", PendingRounds(flow) == 2);

        RightClickButton(skill, "CookButton");
        Check("right click clears the queue, disarms, keeps the card",
            PendingRounds(flow) == 0 && flow.Armed == SkillKind.None && roster.SelectedUnit == chef);

        ClickButton(skill, "CookButton");
        ClickButton(skill, "CookButton");
        Confirm(action);
        Check("cook spends AP, makes food, disarms and logs",
            chef.currentActionPoints < apBefore && RunResources.Instance.Food > foodBefore &&
            flow.Armed == SkillKind.None && LastEntryContains("cooked"));

        ClickCard(roster, chef);
        ClickButton(skill, "CookButton");
        ClickButton(skill, "CraftButton");
        Check("switching skill restarts the counter on the new one",
            flow.Armed == SkillKind.Craft && PendingRounds(flow) == 1);
        int materialsBefore = RunResources.Instance.Materials;
        Confirm(action);
        Check("craft consumes materials", RunResources.Instance.Materials < materialsBefore);

        // Spending the last AP puts a unit into Resting, so revive before reuse.
        Revive(chef);
        roster.RefreshAvailability();
        ClickCard(roster, chef);
        ClickButton(skill, "HealButton");
        Check("heal arms, hand targets the wounded, confirm waits for a patient",
            flow.Armed == SkillKind.Heal &&
            roster.HandPanel.Mode == CharacterHandPanel.SelectionMode.HealTarget &&
            roster.SelectedCount == 0 && !action.IsVisible);

        // The roster paints on Update(), which does not tick inside one test frame.
        roster.RefreshAvailability();
        var healable = roster.Cards.Where(c => c.Unit != null && c.Unit.state == UnitState.Hurt).ToList();
        var notHealable = roster.Cards.Where(c => c.Unit != null && c.Unit.state != UnitState.Hurt).ToList();
        Check("every hurt card is highlighted as a heal target",
            healable.Count > 0 && healable.All(c => c.IsTargetable));
        Check("cards that cannot be healed are not highlighted",
            notHealable.All(c => !c.IsTargetable));

        ClickCard(roster, patient);
        Check("hurt unit is pickable and the button offers HEAL",
            roster.SelectedUnit == patient && action.IsVisible && action.CurrentLabel == "HEAL");

        int medicineBefore = RunResources.Instance.Medicine;
        Confirm(action);
        Check("heal with medicine cures and spends 1 medicine",
            patient.state == UnitState.Idle && RunResources.Instance.Medicine == medicineBefore - 1 &&
            roster.HandPanel.Mode == CharacterHandPanel.SelectionMode.Dispatch);

        roster.RefreshAvailability();
        Check("the heal highlight clears once the action resolves",
            roster.Cards.All(c => !c.IsTargetable));

        // AP-only mode: no medicine involved, costs 2 AP.
        patient.state = UnitState.Hurt;
        chef.state = UnitState.Idle;
        // Spending the last AP puts a unit into Resting, so revive before reuse.
        Revive(chef);
        roster.RefreshAvailability();
        ClickCard(roster, chef);
        ClickButton(skill, "HealButton");
        ClickButton(skill, "HealModeButton");
        Check("mode toggle switches to AP only", HealModeLabel(skill) == "AP");

        int medicineBeforeAp = RunResources.Instance.Medicine;
        int apBeforeAp = chef.currentActionPoints;
        ClickCard(roster, patient);
        Confirm(action);
        Check("AP-only heal leaves medicine alone and spends 2 AP",
            RunResources.Instance.Medicine == medicineBeforeAp &&
            chef.currentActionPoints == apBeforeAp - 2 && patient.state == UnitState.Idle);

        // --- 4. dispatch ---------------------------------------------------
        Section("dispatch");
        ResetUnits(roster);
        RunLog.Clear();
        Check("confirm button hidden with nothing picked", !action.IsVisible);

        var node = Object.FindAnyObjectByType<MapNodeView>();
        Check("a cave node exists", node != null);
        if (node != null) node.GetComponent<Button>().onClick.Invoke();
        Check("still hidden with only a cave selected", !action.IsVisible);

        UnitIconView first = FirstSelectable(roster);
        Check("a selectable card exists", first != null);
        ClickCardView(first);
        Check("picking a card offers DISPATCH", action.IsVisible && action.CurrentLabel == "DISPATCH");

        ClickButton(skill, "CookButton");
        Check("a pending skill takes the button over", action.CurrentLabel == "COOK");
        RightClickButton(skill, "CookButton");
        Check("clearing the skill gives the button back", action.CurrentLabel == "DISPATCH");

        int dispatchAp = roster.SelectedUnit.currentActionPoints;
        UnitCharacter sent = roster.SelectedUnit;
        Confirm(action);
        Check("trip resolves instantly, spends AP, clears and logs",
            sent.HasGatheredToday && sent.currentActionPoints < dispatchAp &&
            roster.SelectedCount == 0 && !action.IsVisible && LastEntryContains("trip"));

        // The roll can wound or lose the squad, so revive before testing the
        // pick rule itself. HasGatheredToday must survive the revive.
        foreach (var unit in UnitManager.Instance.Units) Revive(unit);
        roster.RefreshAvailability();
        Check("a unit that already gathered stays pickable for base work",
            first.Unit.HasGatheredToday && first.CanBeSelected);

        // --- 5. end day ----------------------------------------------------
        Section("end day");
        int dayBefore = DayManager.Instance.CurrentDay;
        ClickThroughRaycast(dayFrame);
        Check("clicking the Day frame advances the day and logs it",
            DayManager.Instance.CurrentDay == dayBefore + 1 && LastEntryContains("Day "));

        return Summarize();
    }

    static string Summarize()
    {
        string header = failed == 0
            ? $"ALL PASS ({passed} checks)"
            : $"{failed} FAILED / {passed} passed";
        return header + "\n" + Log;
    }

    // ------------------------------------------------------------- sections

    static void Section(string name) => Log.AppendLine($"-- {name} --");

    static void CheckLegacyGone(GameObject canvas)
    {
        var found = new List<string>();
        foreach (string path in new[]
                 {
                     "EndDay button", "BaseWorkPanel", "CharCard",
                     "MapPanel/MessageUi", "MapPanel/Dispatch", "MapPanel/ScoutIcon"
                 })
        {
            if (canvas.transform.Find(path) != null) found.Add(path);
        }

        Check(found.Count == 0
            ? "every legacy widget is gone"
            : "legacy widgets still present: " + string.Join(", ", found), found.Count == 0);
    }

    static void CheckCoreItemSlots(GameObject canvas)
    {
        Transform a = canvas.transform.Find("StatusBar/CoreItemGroup/CoreItemFrame");
        Transform b = canvas.transform.Find("StatusBar/CoreItemGroup/CoreItemFrameB");

        Check("both Core Item slots exist and do not overlap",
            a != null && b != null &&
            Mathf.Abs(((RectTransform)b).anchoredPosition.x - ((RectTransform)a).anchoredPosition.x) >=
            ((RectTransform)a).sizeDelta.x);

        var statusBar = canvas.transform.Find("StatusBar").GetComponent<StatusBarView>();
        var so = new UnityEditor.SerializedObject(statusBar);
        Check("coreItemBIcon is wired", so.FindProperty("coreItemBIcon").objectReferenceValue != null);

        // Tint reacts to the obtained flag. Clear first so repeat runs agree.
        var iconB = b != null ? b.Find("CoreItemBIcon").GetComponent<Image>() : null;
        ClearCoreItemFlags();
        InvokeUpdate(statusBar);
        Color locked = iconB != null ? iconB.color : Color.clear;

        RunResources.Instance.ObtainCoreItem(CoreItemKind.B);
        InvokeUpdate(statusBar);
        Check("slot B changes tint once the item is obtained",
            iconB != null && locked != iconB.color);
    }

    static void CheckClickSurfaces(GameObject canvas)
    {
        // Collect every interactive element once, then hit-test its centre the
        // way a real pointer would. Calling the handler directly would miss a
        // dead click surface, which is exactly how the Day frame broke.
        var targets = new HashSet<GameObject>();

        foreach (var behaviour in canvas.GetComponentsInChildren<MonoBehaviour>(false))
        {
            if (behaviour is IPointerClickHandler && behaviour.gameObject.activeInHierarchy && behaviour.isActiveAndEnabled)
                targets.Add(behaviour.gameObject);
        }
        foreach (var button in canvas.GetComponentsInChildren<Button>(false))
        {
            if (button.isActiveAndEnabled) targets.Add(button.gameObject);
        }
        foreach (var card in canvas.GetComponentsInChildren<UnitIconView>(false))
        {
            if (card.isActiveAndEnabled) targets.Add(card.gameObject);
        }
        foreach (var node in Object.FindObjectsByType<MapNodeView>())
        {
            targets.Add(node.gameObject);
        }

        int checkedTargets = 0;
        var broken = new List<string>();

        foreach (var target in targets)
        {
            var rt = target.transform as RectTransform;
            if (rt == null) continue;

            checkedTargets++;
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = centre }, hits);

            if (hits.Count == 0)
            {
                broken.Add($"{Path(rt)} (nothing hit)");
                continue;
            }

            GameObject resolved = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
            if (resolved != target)
            {
                broken.Add($"{Path(rt)} -> {(resolved != null ? resolved.name : "no handler")}");
            }
        }

        Check(broken.Count == 0
            ? $"all {checkedTargets} interactive elements pass a real raycast"
            : $"{broken.Count}/{checkedTargets} unreachable: " + string.Join("; ", broken),
            broken.Count == 0);
    }

    // -------------------------------------------------------------- helpers

    static void ResetRun(UnitRosterView roster)
    {
        var resources = RunResources.Instance;
        resources.AddFood(10);
        resources.AddItem(InventoryItemType.CookingMaterial, 20);
        resources.AddItem(InventoryItemType.Materials, 20);
        resources.AddItem(InventoryItemType.Medicine, 5);

        ClearCoreItemFlags();
        if (GatherMapController.Instance != null) GatherMapController.Instance.ClearSelection();
        ResetUnits(roster);
        RunLog.Clear();
    }

    static void ResetUnits(UnitRosterView roster)
    {
        foreach (var unit in UnitManager.Instance.Units)
        {
            unit.RefreshForNewDay();      // clears HasGatheredToday + fed-today + AP
            unit.state = UnitState.Idle;  // wounds and lost signal survive a day; tests need a clean slate
        }
        roster.ClearSelection();
        roster.SetMode(CharacterHandPanel.SelectionMode.Dispatch);
        roster.RefreshAvailability();
    }

    /// <summary>Full AP and Idle, without touching HasGatheredToday (unlike RefreshForNewDay).</summary>
    static void Revive(UnitCharacter unit)
    {
        unit.state = UnitState.Idle;
        unit.currentActionPoints = unit.maxActionPoints;
    }

    static void ClearCoreItemFlags()
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        foreach (string name in new[] { "coreItemA", "coreItemB" })
        {
            var field = typeof(RunResources).GetField(name, flags);
            if (field != null) field.SetValue(RunResources.Instance, false);
        }
    }

    static void InvokeUpdate(StatusBarView view)
    {
        var method = typeof(StatusBarView).GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (method != null) method.Invoke(view, null);
    }

    static void ForceSkillPanelRefresh(SkillFlowController flow)
    {
        var method = typeof(SkillFlowController).GetMethod("RefreshPanel",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (method != null) method.Invoke(flow, null);
    }

    static int PendingRounds(SkillFlowController flow)
    {
        var field = typeof(SkillFlowController).GetField("pendingRounds",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        return field != null ? (int)field.GetValue(flow) : -1;
    }

    static UnitCharacter ByName(string name)
    {
        foreach (var unit in UnitManager.Instance.Units)
        {
            if (unit.unitName == name) return unit;
        }
        return UnitManager.Instance.Units[0];
    }

    static Button ButtonFor(SkillTriggerPanel panel, string childName)
    {
        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.name == childName) return button;
        }
        return null;
    }

    static string HealModeLabel(SkillTriggerPanel panel)
    {
        foreach (var text in panel.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
        {
            if (text.transform.parent != null && text.transform.parent.name == "HealModeButton") return text.text;
        }
        return "?";
    }

    static UnitIconView FirstSelectable(UnitRosterView roster)
    {
        foreach (var card in roster.Cards)
        {
            if (card.CanBeSelected) return card;
        }
        return null;
    }

    static UnitIconView CardFor(UnitRosterView roster, UnitCharacter unit)
    {
        foreach (var view in roster.Cards)
        {
            if (view.Unit == unit) return view;
        }
        return null;
    }

    static void ClickCard(UnitRosterView roster, UnitCharacter unit) => ClickCardView(CardFor(roster, unit));

    static void ClickCardView(UnitIconView card)
    {
        if (card == null) return;

        ExecuteEvents.Execute(card.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
    }

    static void ClickButton(SkillTriggerPanel panel, string childName)
    {
        var button = ButtonFor(panel, childName);
        if (button != null) button.onClick.Invoke();
    }

    static void RightClickButton(SkillTriggerPanel panel, string childName)
    {
        var button = ButtonFor(panel, childName);
        if (button == null) return;

        ExecuteEvents.Execute(button.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Right },
            ExecuteEvents.pointerClickHandler);
    }

    static void Confirm(DispatchActionPanel panel)
    {
        var button = panel.GetComponent<Button>();
        if (button != null) button.onClick.Invoke();
    }

    static void ClickThroughRaycast(RectTransform rt)
    {
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = centre }, hits);
        if (hits.Count == 0) return;

        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
    }

    static bool DayFrameClickable(RectTransform frame)
    {
        Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, frame.TransformPoint(frame.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = centre }, hits);
        if (hits.Count == 0) return false;

        return ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == frame.gameObject;
    }

    static bool NodeUnder(RectTransform frame)
    {
        Vector3[] corners = new Vector3[4];
        frame.GetWorldCorners(corners);

        foreach (var node in Object.FindObjectsByType<MapNodeView>())
        {
            Vector3[] c = new Vector3[4];
            ((RectTransform)node.transform).GetWorldCorners(c);

            bool overlap = !(c[2].x < corners[0].x || c[0].x > corners[2].x ||
                             c[2].y < corners[0].y || c[0].y > corners[2].y);
            if (overlap) return true;
        }
        return false;
    }

    static bool LastEntryContains(string needle)
    {
        var entries = RunLog.Entries;
        for (int i = entries.Count - 1; i >= 0 && i >= entries.Count - 4; i--)
        {
            if (entries[i].Text.Contains(needle)) return true;
        }
        return false;
    }

    static string Path(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (Transform p = t.parent; p != null && p.name != "Canvas"; p = p.parent) sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    static void Check(string label, bool ok)
    {
        if (ok) passed++;
        else failed++;

        Log.AppendLine((ok ? "PASS  " : "FAIL  ") + label);
    }
}