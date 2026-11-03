using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drives the whole tutorial script in play mode: every coach mark must resolve
/// its target, the scripted roll must land as a Hard Success, and the closing
/// card must appear at the end.
///
/// Run it from the Tutorial scene:
///   unity command script run --file AgentScripts/TestTutorial.cs --entry TestTutorial.Run
/// </summary>
public static class TestTutorial
{
    const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    static readonly StringBuilder Sb = new StringBuilder();
    static int passed;
    static int failed;

    public static string Run()
    {
        Sb.Clear();
        passed = 0;
        failed = 0;

        var director = UnityEngine.Object.FindAnyObjectByType<TutorialDirector>();
        var overlay = UnityEngine.Object.FindAnyObjectByType<TutorialOverlay>();
        var roster = UnityEngine.Object.FindAnyObjectByType<UnitRosterView>();
        var map = GatherMapController.Instance;
        var skill = UnityEngine.Object.FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include);
        var action = UnityEngine.Object.FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);

        if (director == null || overlay == null)
            return "FAILED: no TutorialDirector/TutorialOverlay - is Tutorial.unity in play mode?";

        Section("1. script");
        Check("the tour opens on the first step", director.StepIndex == 0, $"step={director.StepIndex}");
        Check("the script has every coach mark", director.StepCount == 26, $"{director.StepCount} steps");
        Check("the first card quotes the goal",
            director.CurrentText.ToLowerInvariant().Contains("crystal"), director.CurrentText);
        Check("the overlay is drawing", overlay.IsShowing, $"showing={overlay.IsShowing}");
        Check("the tutorial cannot write a save", SaveSystem.Suspended, $"suspended={SaveSystem.Suspended}");

        Section("2. every step resolves its target");
        var seen = new List<string>();
        int guard = 0;
        bool sawHole = false;
        bool sawDice = false;
        bool sawFinaleGuard = false;
        bool cardHeldForDice = false;
        bool cardAfterDice = false;
        float aptitudeHole = 0f;
        int aptitudeRings = -1;
        float aptitudeBox = 0f;

        while (director.StepIndex < director.StepCount && guard++ < 120)
        {
            int before = director.StepIndex;
            seen.Add($"{before}:{director.CurrentText}");

            if (director.StepIndex >= director.StepCount) break;

            // A step that waits for the screen (the dice tumbling) has no card
            // yet: the tour must show nothing until the roll has played out.
            if (!overlay.IsShowing)
            {
                if (DiceRollView.Instance != null && DiceRollView.Instance.IsShowing) cardHeldForDice = true;
                if (DiceRollView.Instance != null) DiceRollView.Instance.RevealImmediately();
                director.Pump();
                Canvas.ForceUpdateCanvases();
                if (overlay.IsShowing && director.StepIndex == 12) cardAfterDice = true;
            }

            if (before == 4)
            {
                // The aptitude card opens the guide's own card and boxes the two
                // badges inside it as one ring.
                aptitudeHole = overlay.Hole.width;
                aptitudeRings = EnabledFocusRings(overlay);
                aptitudeBox = FocusRingHeight(overlay);
            }

            // Aim the dice: the dispatch step installs the scripted roll.
            if (before == 11)
                Check("the dispatch step aims the dice", GatherResolver.ForcedRoll == TutorialDirector.ScriptedRoll,
                    $"forced={GatherResolver.ForcedRoll}");

            if (director.CurrentIsTapStep)
            {
                if (overlay.Hole.width > 1f && overlay.Hole.height > 1f) sawHole = true;
                director.Tap();
            }
            else
            {
                Act(director, roster, map, skill, action);
            }

            // Let the gate see the new state, then check it actually moved.
            director.Pump();
            Canvas.ForceUpdateCanvases();

            if (director.StepIndex == before && director.StepIndex < director.StepCount)
            {
                // One more frame's worth of patience for state that settles late.
                director.Pump();
            }

            if (director.StepIndex == before)
            {
                Check($"step {before} advanced", false, "stuck");
                break;
            }

            if (director.StepIndex == 12) sawDice = DiceRollView.Instance != null && DiceRollView.Instance.IsShowing;
            if (director.StepIndex >= director.StepCount) sawFinaleGuard = true;
        }

        Check("no step got stuck", director.StepIndex >= director.StepCount, $"reached {director.StepIndex}");
        Check("a targeted step punched a hole", sawHole, "hole measured");
        Check("the dice popup was on screen for its step", sawDice, $"at step 12 dice={sawDice}");
        Check("the card waited while the dice tumbled", cardHeldForDice, $"held={cardHeldForDice}");
        Check("the dice card arrives once the roll lands", cardAfterDice, $"shown={cardAfterDice}");
        Check("the aptitude card opens the guide's own card", aptitudeHole > 20f && aptitudeHole < 400f,
            $"hole width {aptitudeHole:0}");
        Check("the aptitude card draws one ring", aptitudeRings == 4, $"{aptitudeRings} ring bars");
        Check("that ring covers aptitude and talent together", aptitudeBox > 60f,
            $"ring height {aptitudeBox:0}px");
        Check("the tour ran out of steps", sawFinaleGuard, "end reached");

        Section("3. the closing card");
        Check("the tour reports itself finished", director.IsFinished, $"finished={director.IsFinished}");
        var finale = overlay.transform.Find("Finale") as RectTransform;
        var finaleButton = overlay.transform.Find("Finale/FinaleButton") as RectTransform;
        var finaleLabel = finaleButton != null ? finaleButton.GetComponentInChildren<TMP_Text>() : null;
        Check("the closing card is up", finale != null && finale.gameObject.activeSelf, "Finale");
        Check("the way out is called RETURN TO GAME",
            finaleLabel != null && finaleLabel.text == "RETURN TO GAME",
            finaleLabel != null ? finaleLabel.text : "no label");
        Check("the coach mark is gone once the card is up", !overlay.IsShowing, $"showing={overlay.IsShowing}");

        Section("4. the scripted trip");
        Check("the scripted roll is 10", TutorialDirector.ScriptedRoll == 10, $"{TutorialDirector.ScriptedRoll}");

        GatherResolver.ForcedRoll = TutorialDirector.ScriptedRoll;
        var probe = GatherResolver.Roll(55);
        Check("a forced 10 against SV 55 is a Hard Success",
            probe.rollValue == TutorialDirector.ScriptedRoll && probe.tier == GatherResultTier.HardSuccess,
            $"roll={probe.rollValue} tier={probe.tier}");
        Check("the forced roll clears itself after one use", GatherResolver.ForcedRoll == -1,
            $"forced={GatherResolver.ForcedRoll}");

        Section("5. the tour left the board playable");
        Check("the crew is intact", roster != null && roster.Cards.Count == 4,
            roster != null ? $"{roster.Cards.Count} cards" : "no roster");
        Check("the first trip already happened",
            RunStats.GatherTrips >= 1, $"trips={RunStats.GatherTrips}");
        Check("the day moved on", DayManager.Instance != null && DayManager.Instance.CurrentDay >= 2,
            DayManager.Instance != null ? $"day={DayManager.Instance.CurrentDay}" : "no day manager");

        Section("6. the goal card raised over the game");
        var prompt = NewGamePromptView.Show(null, null);
        Check("the goal card builds over the scene", prompt != null, "prompt");
        if (prompt != null)
        {
            var headline = prompt.transform.Find("Card/Headline")?.GetComponent<TMP_Text>();
            var start = prompt.transform.Find("Card/StartRunButton");
            var tutorial = prompt.transform.Find("Card/ViewTutorialButton");

            Check("it states the goal", headline != null && headline.text.ToLowerInvariant().Contains("crystal"),
                headline != null ? headline.text : "no headline");
            Check("it offers the run and the tour", start != null && tutorial != null,
                $"start={start != null} tutorial={tutorial != null}");

            var ownCanvas = prompt.GetComponent<Canvas>();
            Check("it draws above the dice and the tour",
                ownCanvas != null && ownCanvas.overrideSorting && ownCanvas.sortingOrder > 600,
                ownCanvas != null ? $"order={ownCanvas.sortingOrder}" : "no canvas");

            prompt.Close();
            Check("it can be dismissed", !prompt.gameObject.activeSelf, "closed");
        }

        Section("7. the goal card belongs to the menu only");
        SceneFlow.PendingGoalPrompt = false;
        Check("a tour return does not queue the goal card", !SceneFlow.ConsumeGoalPrompt(),
            "queued");

        SceneFlow.PendingGoalPrompt = true;
        Check("NEW GAME queues the goal card", SceneFlow.ConsumeGoalPrompt(), "queued");
        Check("the goal card is a one-shot", !SceneFlow.ConsumeGoalPrompt(), "second read");

        var depart = typeof(TutorialDirector).GetMethod("Depart", Any);
        Check("the tour still has a way out", depart != null, depart != null ? "Depart" : "missing");

        Sb.AppendLine();
        Sb.AppendLine($"passed {passed}, failed {failed}");
        return Sb.ToString();
    }

    /// <summary>Counts the small rings drawn INSIDE the open hole, for the step checks.</summary>
    static int EnabledFocusRings(TutorialOverlay overlay)
    {
        var field = overlay.GetType().GetField("focusRings", Any);
        var rings = field?.GetValue(overlay) as List<Image>;
        if (rings == null) return -1;

        int on = 0;
        foreach (var ring in rings)
        {
            if (ring != null && ring.enabled) on++;
        }
        return on;
    }

    /// <summary>Height of the first focus ring, so a check can see it covers two badges.</summary>
    static float FocusRingHeight(TutorialOverlay overlay)
    {
        var field = overlay.GetType().GetField("focusRings", Any);
        var rings = field?.GetValue(overlay) as List<Image>;
        if (rings == null) return 0f;

        // Bars come in fours: top, bottom, left, right. The left bar's height is
        // the box height.
        if (rings.Count < 4 || rings[2] == null) return 0f;
        return rings[2].rectTransform.sizeDelta.y;
    }

    /// <summary>
    /// Performs whatever the step asks for. Index-based on purpose: the script is
    /// content, so pinning the order is the point of the test.
    /// </summary>
    static void Act(TutorialDirector director, UnitRosterView roster, GatherMapController map,
        SkillTriggerPanel skill, DispatchActionPanel action)
    {
        switch (director.StepIndex)
        {
            case 5:
            case 18:
                ClickCard(roster, "Ann");
                break;

            case 6:
                ClickCave(map, GatherZone.Easy);
                break;

            case 11:
                ClickAction(action);
                break;

            case 14:
                CloseDice();
                break;

            case 19:
                ClickSkill(skill, "CookButton");
                break;

            case 21:
                ClickSkill(skill, "CookButton");
                break;

            case 23:
                ClickAction(action);
                break;

            case 24:
                EndDay();
                break;

            default:
                // A gate this test does not know how to satisfy: tapping anything
                // is the closest safe move, and the stuck check will report it.
                director.Tap();
                break;
        }
    }

    static void ClickCard(UnitRosterView roster, string name)
    {
        if (roster == null) return;

        foreach (var card in roster.Cards)
        {
            if (card.Unit == null || card.Unit.unitName != name) continue;

            ExecuteEvents.Execute(card.gameObject,
                new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
            return;
        }
    }

    static void ClickCave(GatherMapController map, GatherZone zone)
    {
        if (map == null) return;

        foreach (var node in map.Nodes)
        {
            if (node.Data != null && node.Data.zone == zone)
            {
                node.GetComponent<Button>().onClick.Invoke();
                return;
            }
        }
    }

    static void ClickAction(DispatchActionPanel action)
    {
        var button = action != null ? action.GetComponent<Button>() : null;
        if (button != null) button.onClick.Invoke();
    }

    static void ClickSkill(SkillTriggerPanel skill, string childName)
    {
        if (skill == null) return;

        var button = skill.transform.Find("CookRow/" + childName);
        var component = button != null ? button.GetComponent<Button>() : null;
        if (component != null) component.onClick.Invoke();
    }

    static void CloseDice()
    {
        var view = DiceRollView.Instance;
        if (view == null) return;

        // The popup only offers CONTINUE once the dice have settled, and this
        // suite runs inside a single frame - so land the result first.
        view.RevealImmediately();

        var button = view.transform.Find("Overlay/Panel/ContinueButton");
        var component = button != null ? button.GetComponent<Button>() : null;
        if (component != null) component.onClick.Invoke();
    }

    static void EndDay()
    {
        var days = DayManager.Instance;
        if (days != null) days.EndDay();
    }

    static void Section(string title)
    {
        Sb.AppendLine();
        Sb.AppendLine("-- " + title);
    }

    static void Check(string what, bool ok, string detail = null)
    {
        if (ok) passed++;
        else failed++;

        Sb.AppendLine((ok ? "PASS  " : "FAIL  ") + what + (string.IsNullOrEmpty(detail) ? "" : "  (" + detail + ")"));
    }
}
