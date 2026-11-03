using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
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

        // An unfocused Editor stops ticking the player loop, so the canvas never
        // lays out and every raycast resolves to nothing: a dozen red checks that
        // look like regressions. Say so once instead of failing mysteriously.
        if (Mathf.Approximately(Time.unscaledDeltaTime, 0f))
        {
            Log.AppendLine("WARN  player loop is not ticking - run 'unity command editor focus' and retry, "
                + "or the raycast/pop checks below fail for environmental reasons.");
        }

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
        CheckWarningGroup(canvas);
        CheckWarningColour(canvas);
        CheckLogRowsFit(canvas);
        CheckFont(canvas);

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

        // --- 3b. ap pips ---------------------------------------------------
        Section("ap pips");
        UnitIconView chefCard = roster.Cards.FirstOrDefault(c => c.Unit == chef);
        Check("a card exists for the chef", chefCard != null);
        if (chefCard != null)
        {
            chefCard.SetApCostPreview(0);
            chefCard.Refresh();
            Check("card paints one pip per action point",
                PipStates(chefCard) == ExpectedPips(chef, 0), PipStates(chefCard));

            chefCard.SetApCostPreview(2);
            chefCard.Refresh();
            Check("pending cost flips the top pips to the about-to-be-spent state",
                chefCard.ApCostPreview == 2 && PipStates(chefCard) == ExpectedPips(chef, 2),
                PipStates(chefCard));

            // Only the selected card may preview a cost: an unselected card that
            // is handed the same number must ignore it.
            var otherCard = roster.Cards.FirstOrDefault(c => c != chefCard && c.Unit != null);
            if (otherCard != null)
            {
                otherCard.SetApCostPreview(2);
                otherCard.Refresh();
            }
            Check("a card that is not selected shows no pending pips",
                otherCard != null && otherCard.ApCostPreview == 0 &&
                PipStates(otherCard) == ExpectedPips(otherCard.Unit, 0),
                otherCard == null ? "no second card" : PipStates(otherCard));

            if (otherCard != null) otherCard.SetApCostPreview(0);
            chefCard.SetApCostPreview(0);
            chefCard.Refresh();
        }

        int apBefore = chef.currentActionPoints;
        int foodBefore = RunResources.Instance.Food;

        ClickButton(skill, "CookButton");
        Check("cook arms and queues 1 round, panel shows COOK",
            flow.Armed == SkillKind.Cook && PendingRounds(flow) == 1 &&
            roster.HandPanel.MaxSelection == 1 && action.IsVisible && action.CurrentLabel == "COOK");

        ClickButton(skill, "CookButton");
        Check("cook queues a second round", PendingRounds(flow) == 2 && flow.PendingApCost == 2,
            $"rounds={PendingRounds(flow)} cost={flow.PendingApCost}");

        Canceller().CancelPending();
        Check("right click cancels the queue, the skill and the card",
            PendingRounds(flow) == 0 && flow.Armed == SkillKind.None && roster.SelectedCount == 0,
            $"rounds={PendingRounds(flow)} armed={flow.Armed} sel={roster.SelectedCount}");

        // Cancelling is total now, so the flow needs its card back to continue.
        ClickCard(roster, chef);

        ClickButton(skill, "CookButton");
        ClickButton(skill, "CookButton");
        Confirm(action);
        Check("cook spends AP, makes food, disarms and logs",
            chef.currentActionPoints < apBefore && RunResources.Instance.Food > foodBefore &&
            flow.Armed == SkillKind.None && LastEntryContains("cooked"));

        if (chefCard != null)
        {
            chefCard.SetApCostPreview(0);
            chefCard.Refresh();
            Check("spent AP turns the pips into the spent state",
                PipStates(chefCard) == ExpectedPips(chef, 0) && PipStates(chefCard).Contains("ap_15"),
                PipStates(chefCard));
        }

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

        // AP-only mode: no medicine involved, costs 3 AP.
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
        // A 3 AP heal needs a full meter; earlier steps may have spent some.
        chef.currentActionPoints = chef.maxActionPoints;
        chef.state = UnitState.Idle;
        roster.RefreshAvailability();
        int apBeforeAp = chef.currentActionPoints;
        ClickCard(roster, patient);
        Confirm(action);
        Check("AP-only heal leaves medicine alone and spends 3 AP",
            RunResources.Instance.Medicine == medicineBeforeAp &&
            chef.currentActionPoints == apBeforeAp - 3 && patient.state == UnitState.Idle,
            $"ap {apBeforeAp}->{chef.currentActionPoints} med {medicineBeforeAp}->{RunResources.Instance.Medicine} state={patient.state} armed={flow.Armed}");

        // --- 4. dispatch ---------------------------------------------------
        Section("dispatch");
        ResetUnits(roster);
        RunLog.Clear();
        CheckPatientComesBackResting();
        Check("confirm button hidden with nothing picked", !action.IsVisible);

        // --- dungeon card readout: colour ramp, core-drop line, selection pop
        Section("dungeon readout");
        Check("the success value uses a red-to-green ramp",
            MapNodeView.ColorForChance(0).g < 0.4f && MapNodeView.ColorForChance(0).r > 0.8f &&
            MapNodeView.ColorForChance(100).g > 0.8f && MapNodeView.ColorForChance(100).r < 0.5f,
            $"sv0={MapNodeView.ColorForChance(0)} sv100={MapNodeView.ColorForChance(100)}");

        var allNodes = Object.FindObjectsByType<MapNodeView>();
        Check("three dungeon cards exist", allNodes.Length >= 3, $"nodes={allNodes.Length}");

        MapNodeView[] sorted = SortNodesByTitle(allNodes);
        MapNodeView easyCard = sorted[0];
        MapNodeView hardCard = sorted[sorted.Length - 1];

        // A card is only clickable once somebody is holding a sword, so the row
        // starts grey and the readout work below picks a unit first.
        Check("dungeon cards are grey until a unit is picked",
            easyCard.IsLocked && hardCard.IsLocked,
            $"easy={easyCard.IsLocked} hard={hardCard.IsLocked}");
        Check("skill buttons are grey until a unit is picked",
            skill.IsLocked, $"locked={skill.IsLocked}");

        UnitIconView readoutUnit = FirstSelectable(roster);
        ClickCardView(readoutUnit);
        Check("picking a unit unlocks the row and the skill column",
            !easyCard.IsLocked && !hardCard.IsLocked && !skill.IsLocked,
            $"cards={easyCard.IsLocked} skills={skill.IsLocked}");

        // Risk is subtracted from every cave, so at full Risk all three read 0%
        // and the colour ramp has nothing to work with. These checks are about
        // the ramp, so they run on a clear map and put risk back afterwards.
        var runResources = RunResources.Instance;
        int riskBeforeRamp = runResources != null ? runResources.Risk : 0;
        if (runResources != null) runResources.SetRisk(0);
        GatherMapController.Instance.RefreshReadouts();

        Check("a lower success value reads redder than a higher one",
            hardCard.ChanceLabel.color.g < easyCard.ChanceLabel.color.g,
            $"easy={easyCard.ChanceLabel.color} hard={hardCard.ChanceLabel.color}");

        var titleRt = (RectTransform)easyCard.transform.Find("Title");
        var coreRt = (RectTransform)easyCard.transform.Find("CoreDrop");
        var caveRt = (RectTransform)easyCard.transform.Find("Cave");
        Check("the core-drop line sits under the title, above the cave art",
            titleRt != null && coreRt != null && caveRt != null &&
            coreRt.anchoredPosition.y < titleRt.anchoredPosition.y &&
            coreRt.anchoredPosition.y > caveRt.anchoredPosition.y,
            $"title={titleRt?.anchoredPosition.y:0} core={coreRt?.anchoredPosition.y:0} cave={caveRt?.anchoredPosition.y:0}");

        float popBefore = hardCard.PopScale;
        ClickThroughRaycast((RectTransform)hardCard.transform);
        SettlePop(hardCard);
        Check("selecting a dungeon pops its success value",
            hardCard.PopScale > popBefore + 0.1f,
            $"before={popBefore:0.00} after={hardCard.PopScale:0.00}");
        Check("the popped label is larger than the unselected ones",
            hardCard.ChanceLabel.fontSize > easyCard.ChanceLabel.fontSize,
            $"selected={hardCard.ChanceLabel.fontSize} other={easyCard.ChanceLabel.fontSize}");

        ClickThroughRaycast((RectTransform)hardCard.transform);
        SettlePop(hardCard);
        Check("deselecting puts the success value back",
            Mathf.Approximately(hardCard.PopScale, 1f) &&
            Mathf.Approximately(hardCard.ChanceLabel.fontSize, easyCard.ChanceLabel.fontSize),
            $"pop={hardCard.PopScale:0.00} size={hardCard.ChanceLabel.fontSize}");

        // The ramp must follow the live value, not just the zone: picking units
        // while a cave is selected rewrites the number and therefore its colour.
        ClickThroughRaycast((RectTransform)hardCard.transform);
        Color baseColour = hardCard.ChanceLabel.color;
        int baseValue = ReadChance(hardCard);

        // A *second* unit, not the one already picked: clicking a selected card
        // would toggle it off and grey the row instead of changing the value.
        UnitIconView rampUnit = null;
        foreach (var candidate in roster.Cards)
        {
            if (candidate.CanBeSelected && !candidate.IsSelected) { rampUnit = candidate; break; }
        }

        if (rampUnit != null)
        {
            ClickCardView(rampUnit);
            Color liveColour = hardCard.ChanceLabel.color;
            int liveValue = ReadChance(hardCard);

            Check("the success value colour follows the live team value",
                liveValue != baseValue && !Mathf.Approximately(liveColour.g, baseColour.g) &&
                Mathf.Approximately(liveColour.g, MapNodeView.ColorForChance(liveValue).g),
                $"sv {baseValue}→{liveValue} colour {baseColour}→{liveColour}");

            roster.ClearSelection();
        }

        // Dropping the crew greys the row again - the run cannot pay for a trip -
        // so the row has to be unlocked before the dissolve click below.
        Check("dropping the crew greys the row again",
            hardCard.IsLocked && !hardCard.IsSelected,
            $"locked={hardCard.IsLocked} selected={hardCard.IsSelected}");
        if (roster.SelectedCount == 0) ClickCardView(FirstSelectable(roster));

        ClickThroughRaycast((RectTransform)hardCard.transform);
        SettlePop(hardCard);

        // The ramp checks above needed a clear map; the rest of the run keeps
        // whatever risk the save/scene started with.
        if (runResources != null) runResources.SetRisk(riskBeforeRamp);
        GatherMapController.Instance.RefreshReadouts();

        // Dropping the crew at the end of the readout section left the row grey;
        // that is the state the next section starts from.
        if (roster.SelectedCount > 0) roster.ClearSelection();

        Section("dispatch");
        var node = Object.FindAnyObjectByType<MapNodeView>();
        Check("a cave node exists", node != null);
        Check("the row is grey while nobody is picked", node != null && node.IsLocked);
        if (node != null) node.GetComponent<Button>().onClick.Invoke();
        Check("still hidden with only a cave selected",
            !action.IsVisible && GatherMapController.Instance.SelectedNode == null);

        UnitIconView first = FirstSelectable(roster);
        Check("a selectable card exists", first != null);
        ClickCardView(first);
        Check("picking a card offers DISPATCH", action.IsVisible && action.CurrentLabel == "DISPATCH");

        // The cost line describes a whole trip, so the cave has to be picked too.
        if (node != null) ClickThroughRaycast((RectTransform)node.transform);

        // The cost line must quote the sword, not just the AP: a trip spends both.
        Check("the dispatch cost line quotes AP plus one sword",
            action.CurrentCostText == "1 AP + 1 SWORD" || action.CurrentCostText == "2 AP + 1 SWORD"
            || action.CurrentCostText == "3 AP + 1 SWORD",
            action.CurrentCostText ?? "none");

        // The confirm button repeats the art: the sword while planning a trip,
        // the skill's own icon once a skill owns the action.
        Check("the dispatch button wears the ready-state sword",
            action.CurrentIcon != null && action.CurrentIcon.name == "state_sword",
            action.CurrentIcon != null ? action.CurrentIcon.name : "none");

        ClickButton(skill, "CookButton");
        Check("a pending skill takes the button over", action.CurrentLabel == "COOK");
        Check("the cook confirm reuses the cook icon",
            action.CurrentIcon != null && action.CurrentIcon == skill.CookIcon,
            action.CurrentIcon != null ? action.CurrentIcon.name : "none");

        Canceller().CancelPending();
        Check("right click clears the whole plan and hides the button",
            !action.IsVisible && roster.SelectedCount == 0 && flow.Armed == SkillKind.None,
            $"visible={action.IsVisible} sel={roster.SelectedCount} armed={flow.Armed}");

        // Cancelling dropped the card as well, so the trip is re-planned from the
        // cards: pick one again, then the cave.
        UnitIconView tripCard = first != null && first.CanBeSelected ? first : FirstSelectable(roster);
        if (tripCard != null) ClickCardView(tripCard);
        Check("picking a card again offers DISPATCH", action.IsVisible && action.CurrentLabel == "DISPATCH");
        Check("the sword comes back when the skill is cleared",
            action.CurrentIcon != null && action.CurrentIcon.name == "state_sword",
            action.CurrentIcon != null ? action.CurrentIcon.name : "none");

        // Arming a skill now drops the cave (they are exclusive plans), so the
        // trip needs the dungeon picked again before confirming.
        var nodeForTrip = Object.FindAnyObjectByType<MapNodeView>();
        ClickThroughRaycast((RectTransform)nodeForTrip.transform);

        int dispatchAp = roster.SelectedUnit.currentActionPoints;
        int dispatchSwords = roster.SelectedUnit.currentSwords;
        UnitCharacter sent = roster.SelectedUnit;

        // The dice bar is painted from the trip's real success value, and Risk is
        // taken off every trip. At full Risk all three caves read 0%, so clear it
        // for this leg - the bands under test are the resolver's, not the fog's.
        var tripResources = RunResources.Instance;
        int riskBeforeTrip = tripResources != null ? tripResources.Risk : 0;
        if (tripResources != null) tripResources.SetRisk(0);

        Confirm(action);
        Check("trip resolves instantly, spends AP, clears and logs",
            sent.HasGatheredToday && sent.currentActionPoints < dispatchAp &&
            roster.SelectedCount == 0 && !action.IsVisible && LastEntryContains("trip"));

        Check("a trip spends the unit's sword",
            dispatchSwords == 1 && sent.currentSwords == 0 && !sent.CanBeSentGathering,
            $"before={dispatchSwords} after={sent.currentSwords}");

        Check("dispatching drops the cave selection",
            GatherMapController.Instance.SelectedNode == null && !nodeForTrip.IsSelected);

        var diceView = Object.FindAnyObjectByType<DiceRollView>();
        Check("the roll is revealed by the dice popup", diceView != null && diceView.IsShowing);

        // The popup owns the screen until Continue, so close it before going on.
        if (diceView != null && diceView.IsShowing)
        {
            diceView.RevealImmediately();

            var overlay = diceView.transform.Find("Overlay");
            var panel = overlay != null ? overlay.Find("Panel") : null;
            var continueButton = panel != null ? panel.Find("ContinueButton") as RectTransform : null;

            // The bar must paint the same bands the resolver rolls: Hard Success
            // is the top THIRD of the success value (2026-11-02), not the half.
            var legend = panel != null ? panel.Find("Legend") : null;
            var legendText = legend != null ? legend.GetComponent<TMPro.TextMeshProUGUI>() : null;
            int shownSv = diceView.CurrentSuccessValue;
            int expectedHard = GatherResolver.HardSuccessThreshold(shownSv);
            Check("the dice bar paints Hard Success from the top third",
                legendText != null && legendText.text.Contains($"HARD 1-{expectedHard}"),
                $"sv={shownSv} expected=1-{expectedHard} legend={legendText?.text}");
            Check("the dice popup reveals a CONTINUE button",
                continueButton != null && continueButton.gameObject.activeSelf);
            if (continueButton != null) ClickThroughRaycast(continueButton);
            Check("CONTINUE closes the dice popup", !diceView.IsShowing,
                $"showing={diceView.IsShowing} hits={DescribeHit(continueButton)}");
        }

        // The roll can wound or lose the squad, so revive before testing the
        // pick rule itself. HasGatheredToday must survive the revive.
        if (tripResources != null) tripResources.SetRisk(riskBeforeTrip);

        foreach (var unit in UnitManager.Instance.Units) Revive(unit);
        roster.RefreshAvailability();
        Check("a unit that already gathered stays pickable for base work",
            first.Unit.HasGatheredToday && first.CanBeSelected);

        CheckSwordRules(roster);

        // --- 4b. fumble ----------------------------------------------------
        Section("fumble");
        CheckFumble();

        // --- 5. end day ----------------------------------------------------
        Section("end day");
        int dayBefore = DayManager.Instance.CurrentDay;
        ClickThroughRaycast(dayFrame);
        Check("clicking the Day frame advances the day and logs it",
            DayManager.Instance.CurrentDay == dayBefore + 1 && LastEntryContains("Day "));

        // --- 6. sprint rules ------------------------------------------------
        Section("sprint rules");
        CheckSuccessValue();
        CheckPity();
        CheckTalents(skill, action);
        CheckSkillAndDungeonAreExclusive(roster, flow, skill, action);
        CheckCardReadouts(roster);
        CheckHudChrome(roster, skill, action);
        CheckTooltips(roster, flow, skill, action, dayFrame);

        // --- 7. audio -------------------------------------------------------
        CheckAudio();

        // --- 8. the new lose condition --------------------------------------
        // Last on purpose: it ends the run, which deletes the save and puts the
        // result screen up, so nothing may follow it.
        CheckHurtLose();

        return Summarize();
    }

    // --------------------------------------------------------------- audio (2026-11-02)

    /// <summary>
    /// One Music source and one SFX source through a Master/Music/SFX mixer, a
    /// run track that never changes mid-run, and the three stings on the events
    /// the design names.
    /// </summary>
    static void CheckAudio()
    {
        Section("audio");

        var director = AudioDirector.EnsureExists();
        var library = GameAudioLibrary.Load();

        Check("the clip library is in Resources and holds five run tracks",
            library != null && library.TitleMusic != null && library.Success != null &&
            library.GameOver != null && library.RunTrackCount == 5,
            library == null ? "no library" : $"tracks={library.RunTrackCount}");

        Check("the library carries a fail sting and two win stings",
            library != null && library.Fail != null && library.WinClipCount == 2,
            library == null ? "no library" : $"fail={library.Fail != null} wins={library.WinClipCount}");

        // The source mp3s were stereo with genuinely different audio in each
        // channel (correlation 0.12), so summing them to mono played two tracks
        // at once. Every win sting must be a single-channel clip.
        if (library != null)
        {
            var multi = new List<string>();
            for (int i = 0; i < library.WinClipCount; i++)
            {
                var clip = library.WinClip(i);
                if (clip == null || clip.channels != 1) multi.Add(clip != null ? clip.name + "(" + clip.channels + "ch)" : "null");
            }
            Check("every win sting is mono, so one file cannot hold two tracks",
                multi.Count == 0,
                multi.Count > 0 ? string.Join(", ", multi) : $"{library.WinClipCount} mono clips");
        }

        Check("the library carries an error sting for rejected clicks",
            library != null && library.Error != null,
            library == null ? "no library" : $"error={(library.Error != null ? library.Error.name : "none")}");

        // The warning banner is the game's "click refused" channel: every caller
        // (blocked skill, bad selection, end-day warning) funnels through it, so
        // the error sting belongs there and nowhere else. Driven through the real
        // button, because a banner shown by hand proves nothing about the flow.
        var cookButton = FindButton(Object.FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include), "CookButton");
        var roster = Object.FindAnyObjectByType<UnitRosterView>();
        if (cookButton != null && roster != null)
        {
            // With no card selected, cooking is refused with "pick a character
            // first" - the simplest refusal that cannot be made legal by state
            // left over from an earlier section.
            roster.ClearSelection();
            roster.SetMode(CharacterHandPanel.SelectionMode.Dispatch);
            roster.RefreshAvailability();

            int errorsBefore = director.ErrorCount;
            int sfxBefore = director.SfxCount;
            director.ResetErrorThrottle();
            cookButton.onClick.Invoke();
            Check("a refused click shows a warning and plays the error sting",
                director.ErrorCount == errorsBefore + 1 &&
                director.SfxCount == sfxBefore + 1 &&
                director.LastSfx == library.Error,
                $"errors {errorsBefore}->{director.ErrorCount} sfx {sfxBefore}->{director.SfxCount} " +
                $"last={(director.LastSfx != null ? director.LastSfx.name : "none")}");

            // The floor that keeps a held button from buzzing.
            director.ResetErrorThrottle();
            int afterFirst = director.ErrorCount;
            WarningView warn = Object.FindAnyObjectByType<WarningView>(FindObjectsInactive.Include);
            warn?.Show("SECOND", LogKind.Warning);
            warn?.Show("THIRD", LogKind.Warning);
            Check("the error sting is throttled so a held button does not buzz",
                director.ErrorCount <= afterFirst + 1,
                $"count={director.ErrorCount}");
        }

        Check("the library carries every ui click clip",
            library != null && library.Click != null && library.Select != null &&
            library.Cook != null && library.Heal != null &&
            library.Craft != null && library.Popup != null,
            library == null ? "no library" : $"plain={library.Click != null} select={library.Select != null} " +
                $"cook={library.Cook != null} heal={library.Heal != null} craft={library.Craft != null} popup={library.Popup != null}");

        var mixer = library != null ? library.Mixer : null;
        var groupNames = mixer != null
            ? mixer.FindMatchingGroups(string.Empty).Select(g => g.name).ToList()
            : new List<string>();
        Check("the mixer splits audio into Master, Music and SFX",
            groupNames.Contains("Master") && groupNames.Contains("Music") && groupNames.Contains("SFX"),
            string.Join(", ", groupNames));

        var sources = director.GetComponents<AudioSource>();
        var music = sources.Length > 0 ? sources[0] : null;
        var sfx = sources.Length > 1 ? sources[1] : null;
        Check("music and sfx each have their own routed source",
            music != null && sfx != null &&
            music.outputAudioMixerGroup != null && music.outputAudioMixerGroup.name == "Music" &&
            sfx.outputAudioMixerGroup != null && sfx.outputAudioMixerGroup.name == "SFX",
            music == null ? "no music source" : $"music={music.outputAudioMixerGroup?.name} sfx={sfx?.outputAudioMixerGroup?.name}");

        Check("the run loops one of the five tracks",
            director.CurrentMusic != null && music != null && music.isPlaying && music.loop &&
            RunTrackIndexOf(director.CurrentMusic, library) >= 0,
            director.CurrentMusic != null ? director.CurrentMusic.name : "silent");

        // Re-asking for the track already playing must not restart it: that is
        // what keeps the song steady across a scene reload or a resumed save.
        float before = music != null ? music.time : -1f;
        director.BeginRun(RunTrackIndexOf(director.CurrentMusic, library));
        Check("the run track is never re-rolled mid-run",
            director.CurrentMusic != null && music != null && music.isPlaying && music.time >= before,
            $"time {before} -> {(music != null ? music.time : -1f)}");

        director.PlayTitleMusic();
        Check("the menu track is Title_Screen",
            director.CurrentMusic != null && director.CurrentMusic.name == "Title_Screen",
            director.CurrentMusic != null ? director.CurrentMusic.name : "silent");

        director.BeginRun(RunTrackIndexOf(library.TitleMusic, library) >= 0 ? 0 : AudioDirector.PendingRunTrack);
        Check("leaving the menu brings the run track back",
            director.CurrentMusic != null && director.CurrentMusic.name.StartsWith("BGM_"),
            director.CurrentMusic != null ? director.CurrentMusic.name : "silent");

        // A good roll chirps, a bad one thuds.
        var dice = Object.FindAnyObjectByType<DiceRollView>();
        if (dice != null)
        {
            int sfxBefore = director.SfxCount;
            dice.Show(RollResult(GatherResultTier.Regular, 12), () => { });
            dice.RevealImmediately();
            Check("a Success roll plays the success sting",
                director.SfxCount == sfxBefore + 1 && director.LastSfx == library.Success,
                $"count={director.SfxCount} last={(director.LastSfx != null ? director.LastSfx.name : "none")}");
            CloseDice(dice);

            sfxBefore = director.SfxCount;
            dice.Show(RollResult(GatherResultTier.HardSuccess, 6), () => { });
            dice.RevealImmediately();
            Check("a Hard Success roll plays the success sting too",
                director.SfxCount == sfxBefore + 1 && director.LastSfx == library.Success,
                $"count={director.SfxCount}");
            CloseDice(dice);

            sfxBefore = director.SfxCount;
            dice.Show(RollResult(GatherResultTier.Failure, 88), () => { });
            dice.RevealImmediately();
            Check("a Failure roll plays the fail sting",
                director.SfxCount == sfxBefore + 1 && director.LastSfx == library.Fail,
                $"count={director.SfxCount} last={(director.LastSfx != null ? director.LastSfx.name : "none")}");
            CloseDice(dice);

            sfxBefore = director.SfxCount;
            dice.Show(RollResult(GatherResultTier.Fumble, 99), () => { });
            dice.RevealImmediately();
            Check("a Fumble roll plays the fail sting",
                director.SfxCount == sfxBefore + 1 && director.LastSfx == library.Fail,
                $"count={director.SfxCount} last={(director.LastSfx != null ? director.LastSfx.name : "none")}");
            CloseDice(dice);
        }

        // A core item landing in the bag uses the same clip.
        var resources = RunResources.Instance;
        ClearCoreItemFlags();
        int itemBefore = director.SfxCount;
        resources.ObtainCoreItem(CoreItemKind.A);
        Check("getting a core item plays the success sting",
            director.SfxCount == itemBefore + 1 && director.LastSfx == library.Success,
            $"count={director.SfxCount}");
        ClearCoreItemFlags();

        director.PlayGameOver();
        Check("a lose condition plays Game Over and stops the music",
            director.LastSfx == library.GameOver &&
            (music == null || !music.isPlaying) && director.CurrentMusic == null,
            $"last={(director.LastSfx != null ? director.LastSfx.name : "none")}");

        // Win draws one of the two win clips, and always one of them.
        director.BeginRun(AudioDirector.PendingRunTrack);
        director.PlayWin();
        Check("a win plays exactly one of the two win stings and stops the music",
            director.LastSfx != null && IsWinClip(director.LastSfx, library) &&
            director.CurrentMusic == null,
            $"last={(director.LastSfx != null ? director.LastSfx.name : "none")} music={(director.CurrentMusic != null ? director.CurrentMusic.name : "none")}");

        // One PlayWin call, one clip: anything else would be two jingles at once.
        int winBefore = director.SfxCount;
        director.PlayWin();
        Check("one win call puts one sting on the source",
            director.SfxCount == winBefore + 1,
            $"count {winBefore} -> {director.SfxCount}");

        // Both clips have to be reachable, or the random pick would be a lie.
        var drawnNames = new HashSet<string>();
        for (int i = 0; i < 40 && drawnNames.Count < library.WinClipCount; i++)
        {
            director.BeginRun(AudioDirector.PendingRunTrack);
            director.PlayWin();
            if (director.LastSfx != null) drawnNames.Add(director.LastSfx.name);
        }
        Check("the win sting is drawn from both clips",
            drawnNames.Count == library.WinClipCount,
            string.Join(", ", drawnNames));

        // Put the run's music back for whatever runs after this block.
        director.BeginRun(AudioDirector.PendingRunTrack);
        Check("the run track can be started again after an ending",
            director.CurrentMusic != null && music != null && music.isPlaying,
            director.CurrentMusic != null ? director.CurrentMusic.name : "silent");

        // Faders: three independent values, each pushed into the mixer.
        AudioDirector.SetVolume(AudioDirector.MusicVolumeKey, 0.25f);
        float musicDb = FloatParam(mixer, "MusicVolume");
        float sfxDb = FloatParam(mixer, "SfxVolume");
        Check("the music fader reaches the mixer and leaves sfx alone",
            Mathf.Abs(musicDb - Mathf.Log10(0.25f) * 20f) < 0.2f && Mathf.Abs(sfxDb - Mathf.Log10(0.8f) * 20f) < 0.2f,
            $"music={musicDb:0.0}dB sfx={sfxDb:0.0}dB");
        AudioDirector.SetVolume(AudioDirector.MusicVolumeKey, 0.8f);

        CheckClickSounds(director, library);
    }

    // ------------------------------------------------------- ui clicks (2026-11-02)

    /// <summary>
    /// Each control names its own sound: a bright tick for chrome, a chime for
    /// cook, the heal sting, a metallic craft, a warm select for cards, and a
    /// popup for a page opening. Chrome is wired by UiFactory so nothing gets
    /// forgotten; everything more specific opts in explicitly.
    /// </summary>
    static void CheckClickSounds(AudioDirector director, GameAudioLibrary library)
    {
        // Chrome: a button built by the factory carries the plain clip.
        var plainButton = Object.FindAnyObjectByType<HudTopBar>()?.transform.Find("OptionsButton")?.GetComponent<Button>();
        var plainSound = plainButton != null ? plainButton.GetComponent<UiClickSound>() : null;
        Check("the options button carries the plain chrome click",
            plainSound != null && plainSound.Kind == UiClickKind.Plain,
            plainSound != null ? plainSound.Kind.ToString() : "no UiClickSound");

        // Each skill wears its own sound.
        var skill = Object.FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include);
        Check("cook wears the food-ready click",
            IsKind(skill, "CookButton", UiClickKind.Cook), KindOf(skill, "CookButton"));
        Check("heal wears the heal click",
            IsKind(skill, "HealButton", UiClickKind.Heal), KindOf(skill, "HealButton"));
        Check("craft wears the craft click",
            IsKind(skill, "CraftButton", UiClickKind.Craft), KindOf(skill, "CraftButton"));

        Check("the craft mode toggle stays a plain click",
            IsKind(skill, "CraftModeButton", UiClickKind.Plain),
            KindOf(skill, "CraftModeButton"));

        var action = Object.FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);
        var confirm = action != null ? action.GetComponentInChildren<Button>(true) : null;
        var confirmSound = confirm != null ? confirm.GetComponent<UiClickSound>() : null;
        Check("the confirm button uses the select click",
            confirmSound != null && confirmSound.Kind == UiClickKind.Select,
            confirmSound != null ? confirmSound.Kind.ToString() : "none");

        var nodes = Object.FindObjectsByType<MapNodeView>(FindObjectsInactive.Include);
        int selectNodes = 0;
        foreach (var node in nodes)
        {
            var sound = node.GetComponent<UiClickSound>();
            if (sound != null && sound.Kind == UiClickKind.Select) selectNodes++;
        }
        Check("every dungeon card uses the select click",
            nodes.Length > 0 && selectNodes == nodes.Length,
            $"{selectNodes}/{nodes.Length}");

        // Cards get theirs from the pointer handler, not a Button listener.
        var roster = Object.FindAnyObjectByType<UnitRosterView>();
        var card = roster != null && roster.Cards.Count > 0 ? roster.Cards[0] : null;
        if (card != null)
        {
            roster.SetMode(CharacterHandPanel.SelectionMode.Dispatch);
            roster.RefreshAvailability();
            UiClickSound.ResetThrottle();

            int cardBefore = director.SfxCount;
            card.OnPointerClick(new PointerEventData(EventSystem.current));
            Check("tapping a unit card uses the select click",
                director.SfxCount == cardBefore + 1 && director.LastSfx == library.Select,
                $"selectable={card.CanBeSelected} count={director.SfxCount} " +
                $"last={(director.LastSfx != null ? director.LastSfx.name : "none")}");
        }

        // The Options button opens a page, so it fires twice: its own plain click
        // and then the popup. Asserting "the plain clip" on it would be wrong.
        int before = director.SfxCount;
        plainButton.onClick.Invoke();
        Check("opening a page plays the popup clip",
            director.SfxCount == before + 2 && director.LastSfx == library.Popup,
            $"count={director.SfxCount} last={(director.LastSfx != null ? director.LastSfx.name : "none")}");

        // Closing it is plain only, which is the clean way to prove the chrome clip.
        var closeButton = Object.FindAnyObjectByType<HudTopBar>()?.transform
            .Find("OptionsOverlay/OptionsPanel/Card/ReturnToGameButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            before = director.SfxCount;
            closeButton.onClick.Invoke();
            Check("clicking a chrome button plays the plain clip",
                director.SfxCount == before + 1 && director.LastSfx == library.Click,
                $"count={director.SfxCount} last={(director.LastSfx != null ? director.LastSfx.name : "none")}");
        }
        CloseOptionsIfOpen();

        // The throttle means two clicks in the same frame must not stack up. The
        // close button above already played one, so the honest assertion is "at
        // most one more", which is exactly what a drag needs.
        before = director.SfxCount;
        UiClickSound.Play(UiClickKind.Select);
        UiClickSound.Play(UiClickKind.Select);
        Check("the click throttle stops a slider drag machine-gunning",
            director.SfxCount - before <= 1,
            $"before={before} after={director.SfxCount}");
    }

    static bool IsKind(SkillTriggerPanel panel, string childName, UiClickKind kind) =>
        panel != null && KindOf(panel, childName) == kind.ToString();

    static Button FindButton(SkillTriggerPanel panel, string childName)
    {
        if (panel == null) return null;

        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.name == childName) return button;
        }
        return null;
    }

    static string KindOf(SkillTriggerPanel panel, string childName)
    {
        if (panel == null) return "no panel";

        foreach (var button in panel.GetComponentsInChildren<Button>(true))
        {
            if (button.name != childName) continue;

            var sound = button.GetComponent<UiClickSound>();
            return sound != null ? sound.Kind.ToString() : "none";
        }
        return "missing " + childName;
    }

    static void CloseOptionsIfOpen()
    {
        var bar = Object.FindAnyObjectByType<HudTopBar>();
        var overlay = bar != null ? bar.transform.Find("OptionsOverlay") : null;
        var group = overlay != null ? overlay.GetComponent<CanvasGroup>() : null;
        if (group == null || group.alpha < 0.5f) return;

        var close = overlay.Find("OptionsPanel/Card/ReturnToGameButton") as RectTransform;
        if (close != null) ClickThroughRaycast(close);
    }

    static float FloatParam(AudioMixer mixer, string name)
    {
        if (mixer == null) return 0f;
        mixer.GetFloat(name, out float value);
        return value;
    }

    static int RunTrackIndexOf(AudioClip clip, GameAudioLibrary library)
    {
        if (clip == null || library == null) return -1;
        for (int i = 0; i < library.RunTrackCount; i++)
        {
            if (library.RunTrack(i) == clip) return i;
        }
        return -1;
    }

    static bool IsWinClip(AudioClip clip, GameAudioLibrary library)
    {
        if (clip == null || library == null) return false;
        for (int i = 0; i < library.WinClipCount; i++)
        {
            if (library.WinClip(i) == clip) return true;
        }
        return false;
    }

    static GatherDispatchResult RollResult(GatherResultTier tier, int roll) => new GatherDispatchResult
    {
        zone = GatherZone.Easy,
        tier = tier,
        rollValue = roll,
        successValue = 60,
        sentUnits = new List<UnitCharacter>()
    };

    static void CloseDice(DiceRollView dice)
    {
        if (dice == null || !dice.IsShowing) return;

        var panel = dice.transform.Find("Overlay/Panel");
        var continueButton = panel != null ? panel.Find("ContinueButton") as RectTransform : null;
        if (continueButton != null) ClickThroughRaycast(continueButton);
    }

    // --------------------------------------------------- the hurt lose condition

    /// <summary>
    /// Healing needs a Ready healer, so a crew where nobody can stand can never
    /// act again. That is a loss, not a run that limps on with the day counter
    /// still ticking.
    /// </summary>
    static void CheckHurtLose()
    {
        Section("hurt lose");

        var days = DayManager.Instance;
        var units = UnitManager.Instance.Units;
        if (days == null || units.Count < 2) return;

        Check("a Resting unit still counts as able",
            !UnitManager.Instance.AllUnitsIncapacitated());

        // One unit left standing is still a run: it can heal the others.
        for (int i = 0; i < units.Count - 1; i++) units[i].state = UnitState.Hurt;
        Check("three hurt units is not a loss yet",
            !UnitManager.Instance.AllUnitsIncapacitated(),
            string.Join(",", units.Select(u => u.state.ToString())));

        // The last one going down ends it.
        units[units.Count - 1].state = UnitState.Hurt;
        Check("the whole crew hurt counts as incapacitated",
            UnitManager.Instance.AllUnitsIncapacitated());

        units[0].state = UnitState.Death;
        Check("dead units count towards the same loss",
            UnitManager.Instance.AllUnitsIncapacitated());

        // Mutating state directly does not declare anything - the check has to be
        // asked, which is what the dispatch flow and End Day both do.
        days.CheckImmediateLoss();
        Check("the run is declared lost the moment nobody can act",
            days.Outcome == RunOutcome.LoseAllUnitsHurt,
            days.Outcome.ToString());

        var result = Object.FindAnyObjectByType<RunResultView>(FindObjectsInactive.Include);
        Check("the result screen appears for a whole-crew hurt loss",
            result != null && result.GetComponent<CanvasGroup>() != null &&
            result.GetComponent<CanvasGroup>().alpha > 0.5f,
            result == null ? "no result view" : $"alpha={result.GetComponent<CanvasGroup>().alpha}");
    }

    // ------------------------------------------------------ tooltips (2026-11-02)

    /// <summary>
    /// Every part of the HUD that shows a number should explain it on hover, and
    /// the numbers it quotes must come from the live systems - a tooltip that
    /// repeats a hard-coded cost is worse than none.
    /// </summary>
    static void CheckTooltips(UnitRosterView roster, SkillFlowController flow, SkillTriggerPanel skill,
        DispatchActionPanel action, RectTransform dayFrame)
    {
        Section("tooltips");

        Check("every skill button carries a hover popup",
            HasHint(skill, "CookButton") && HasHint(skill, "HealButton") &&
            HasHint(skill, "HealModeButton") && HasHint(skill, "CraftButton") &&
            HasHint(skill, "CraftModeButton"));

        var healMode = PreviewOf(skill, "HealModeButton");
        int withoutMedicine = BaseWorkController.Instance.HealBaseApCost(false);
        Check($"heal tooltip quotes the live AP-only cost ({withoutMedicine} AP)",
            healMode != null && healMode.body.Contains($"{withoutMedicine} AP"),
            healMode != null ? healMode.body : "no tooltip");

        var cook = PreviewOf(skill, "CookButton");
        Check("cook tooltip quotes the live material cost",
            cook != null && cook.body.Contains($"{BaseWorkController.Instance.CookMaterialPerRound} COOKING MATERIAL"),
            cook != null ? cook.body : "no tooltip");

        Check("the top readouts carry popups",
            HasHintByName("FoodIcon") && HasHintByName("MedicineIcon") &&
            HasHintByName("CookingIcon") && HasHintByName("MaterialsIcon"),
            $"food={HasHintByName("FoodIcon")} medicine={HasHintByName("MedicineIcon")}");

        Check("the day frame carries a popup",
            dayFrame.GetComponent<HudTooltipTrigger>() != null);

        Check("the confirm button carries a popup",
            action.GetComponent<HudTooltipTrigger>() != null);

        Check("OPTIONS and HELP stay tooltip-free",
            !HasHintByName("OptionsButton") && !HasHintByName("HelpButton"),
            $"options={HasHintByName("OptionsButton")} help={HasHintByName("HelpButton")}");

        // 2026-11-02: the popups are per element now - no single popup covering
        // a whole card, so a player can point at one badge or one number.
        Check("no card-level aggregate popup",
            AllCardsLackRootHint(roster),
            "a card root still carries the old catch-all tooltip");

        Check("unit-card badges each explain themselves",
            HasChildHint(roster.Cards[0], "Lift/AptitudeBadge") &&
            HasChildHint(roster.Cards[0], "Lift/AptitudeBadge/AptitudeLetter") &&
            HasChildHint(roster.Cards[0], "Lift/TalentIcon") &&
            HasChildHint(roster.Cards[0], "Lift/StateIcon"),
            "aptitude / talent / state badge is missing its popup");

        Check("all six unit lamps explain themselves",
            LampHintsPresent(roster.Cards[0]),
            "a lamp is missing its popup");

        Check("unit AP pips explain themselves",
            HasChildHint(roster.Cards[0], "Lift/ApPips/ApPip0"),
            "the AP pips carry no popup");

        var aptitude = PreviewOfChild(roster.Cards[0], "Lift/AptitudeBadge");
        Check("aptitude popup quotes the live bonus",
            aptitude != null && aptitude.body.Contains($"+{GatherResolver.AptitudeBonus}"),
            aptitude != null ? aptitude.body : "no tooltip");

        var talent = PreviewOfChild(roster.Cards[0], "Lift/TalentIcon");
        Check("talent popup names the talent",
            talent != null && talent.title.Contains("TALENT"),
            talent != null ? talent.title : "no tooltip");

        Check("dungeon card readouts each explain themselves",
            DungeonHintsPresent(),
            "title / success value / core drop / drops are missing a popup");

        var chanceTip = PreviewOfChild(Object.FindObjectsByType<MapNodeView>()[0], "Chance");
        Check("success-value popup quotes the hard-success threshold",
            chanceTip != null && chanceTip.body.Contains("HARD SUCCESS"),
            chanceTip != null ? chanceTip.body : "no tooltip");

        Check("the popup is large enough to read",
            HudTooltipWidthAtLeast(340),
            "tooltip column is narrower than 340px");

        // The confirm button must quote what the pending action really spends.
        var chef = ByName("Cia");
        var chefCard = CardFor(roster, chef);
        if (chefCard != null)
        {
            RunResources.Instance.AddItem(InventoryItemType.CookingMaterial, 2);
            chef.currentActionPoints = chef.maxActionPoints;
            chef.state = UnitState.Idle;
            roster.ClearSelection();
            roster.RefreshAvailability();
            ClickCardView(chefCard);
            ClickButton(skill, "CookButton");

            int expected = flow.PendingActualApCost;
            Check($"confirm button paints '{expected} AP' for the queued cook",
                action.IsVisible && action.CurrentApCost == expected && CostLabelText(action) == $"{expected} AP",
                $"visible={action.IsVisible} cost={action.CurrentApCost} text='{CostLabelText(action)}' expected={expected}");

            Canceller()?.CancelPending();
            roster.ClearSelection();
            roster.RefreshAvailability();
        }
    }

    /// <summary>No card root should carry a catch-all popup any more.</summary>
    static bool AllCardsLackRootHint(UnitRosterView roster)
    {
        foreach (var card in roster.Cards)
        {
            if (card.GetComponent<HudTooltipTrigger>() != null) return false;
        }

        foreach (var node in Object.FindObjectsByType<MapNodeView>())
        {
            if (node.GetComponent<HudTooltipTrigger>() != null) return false;
        }

        return true;
    }

    static bool LampHintsPresent(UnitIconView card)
    {
        if (card == null) return false;

        for (int i = 0; i < 6; i++)
        {
            if (!HasChildHint(card, "Lift/Lamps/Lamp" + i)) return false;
        }
        return true;
    }

    static bool DungeonHintsPresent()
    {
        foreach (var node in Object.FindObjectsByType<MapNodeView>())
        {
            if (!HasChildHint(node, "Title")) return false;
            if (!HasChildHint(node, "Chance")) return false;
            if (!HasChildHint(node, "CoreDrop")) return false;
            if (!HasChildHint(node, "Drops/Drop0")) return false;
            if (!HasChildHint(node, "Drops/DropLabel0")) return false;
        }
        return true;
    }

    static bool HasChildHint(Component root, string path)
    {
        if (root == null) return false;

        var child = root.transform.Find(path);
        return child != null && child.GetComponent<HudTooltipTrigger>() != null;
    }

    static HudTooltipContent PreviewOfChild(Component root, string path)
    {
        if (root == null) return null;

        var child = root.transform.Find(path);
        var trigger = child != null ? child.GetComponent<HudTooltipTrigger>() : null;
        return trigger != null ? trigger.Preview() : null;
    }

    /// <summary>Pops the shared popup once to measure the column it builds.</summary>
    static bool HudTooltipWidthAtLeast(float min)
    {
        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return false;

        var anchor = (RectTransform)canvas.transform;
        HudTooltip.ShowNow(canvas, anchor, new HudTooltipContent("MEASURE", "MEASURE"));

        var host = Object.FindAnyObjectByType<HudTooltip>(FindObjectsInactive.Include);
        bool wide = host != null && ((RectTransform)host.transform).sizeDelta.x >= min;

        HudTooltip.Hide();
        return wide;
    }

    static bool HasHint(SkillTriggerPanel panel, string childName)
    {
        var button = ButtonFor(panel, childName);
        return button != null && button.GetComponent<HudTooltipTrigger>() != null;
    }

    static bool HasHintByName(string objectName)
    {
        var go = GameObject.Find(objectName);
        return go != null && go.GetComponent<HudTooltipTrigger>() != null;
    }

    static HudTooltipContent PreviewOf(SkillTriggerPanel panel, string childName)
    {
        var button = ButtonFor(panel, childName);
        var trigger = button != null ? button.GetComponent<HudTooltipTrigger>() : null;
        return trigger != null ? trigger.Preview() : null;
    }

    static string CostLabelText(DispatchActionPanel panel)
    {
        var cost = panel.transform.Find("Cost");
        var label = cost != null ? cost.GetComponent<TMPro.TextMeshProUGUI>() : null;
        return label != null ? label.text : "?";
    }

    static string Summarize()
    {
        string header = failed == 0
            ? $"ALL PASS ({passed} checks)"
            : $"{failed} FAILED / {passed} passed";
        return header + "\n" + Log;
    }

    // ------------------------------------------------- sprint rules (2026-10-30)

    /// <summary>
    /// New success-value table: +25 matching aptitude (once), -10 per tier of
    /// mismatch per unit, +25 duo, no solo-Hard penalty, traits still +/-10.
    /// </summary>
    static void CheckSuccessValue()
    {
        var normal = new GatherZoneConfig { zone = GatherZone.Normal, baseSuccessValue = 55 };
        var hard = new GatherZoneConfig { zone = GatherZone.Hard, baseSuccessValue = 30 };

        var ann = new UnitCharacter { unitName = "Ann", primaryAptitude = ZoneAptitude.Easy };
        var cia = new UnitCharacter { unitName = "Cia", primaryAptitude = ZoneAptitude.Normal };
        var boon = new UnitCharacter { unitName = "Boon", primaryAptitude = ZoneAptitude.Hard };

        var solo = new List<UnitCharacter> { ann };
        Check("aptitude mismatch costs 10 per tier (Ann E on Normal = 45)",
            GatherResolver.ComputeSuccessValue(normal, solo, 0) == 45);

        var duo = new List<UnitCharacter> { ann, cia };
        Check("matching aptitude +25 once and duo +25 (Ann+Cia on Normal = 95)",
            GatherResolver.ComputeSuccessValue(normal, duo, 0) == 95);

        var hardSolo = new List<UnitCharacter> { boon };
        Check("solo Hard no longer carries the -20 penalty (Boon H on Hard = 55)",
            GatherResolver.ComputeSuccessValue(hard, hardSolo, 0) == 55);

        var ham = new List<UnitCharacter> { boon };
        Check("two tiers away costs 20 (Boon H on Easy hypothetical)",
            GatherResolver.AptitudeMismatchPenalty(ZoneAptitude.Hard, ZoneAptitude.Easy) == -20);

        var blessed = new List<UnitCharacter> { new UnitCharacter { unitName = "T", primaryAptitude = ZoneAptitude.Normal, blessed = true } };
        Check("Blessed adds +10", GatherResolver.ComputeSuccessValue(normal, blessed, 0) == 90);

        Check("buff potion stacks add to the team value",
            GatherResolver.ComputeSuccessValue(normal, blessed, 30) == 120);

        // 2026-11-02: Hard Success narrowed from the top half to the top third.
        Check("hard success is the top third of the value (75 -> 25, 55 -> 18)",
            GatherResolver.HardSuccessThreshold(75) == 25 && GatherResolver.HardSuccessThreshold(55) == 18);
    }

    /// <summary>
    /// The HELP page: a rail of sections, one scrolling pane, and copy that
    /// quotes the live numbers instead of hard-coded ones.
    /// </summary>
    static void CheckHelpPage(HudTopBar bar, RectTransform helpButton)
    {
        var overlay = bar.transform.Find("HelpOverlay");
        var group = overlay != null ? overlay.GetComponent<CanvasGroup>() : null;
        Check("help starts hidden", group != null && group.alpha < 0.5f);

        if (helpButton != null) ClickThroughRaycast(helpButton);
        Check("HELP opens the how-to-play page", group != null && group.alpha > 0.5f && group.blocksRaycasts,
            group != null ? $"alpha={group.alpha}" : "no overlay");

        var page = overlay != null ? overlay.GetComponentInChildren<HelpPanelView>(true) : null;
        Check("the help page is a real page, not placeholder text", page != null,
            "no HelpPanelView under HelpOverlay");

        var rail = overlay != null ? overlay.Find("HelpPanel/Card/Rail") : null;
        Check("the help page has a section rail", rail != null && rail.childCount >= 6,
            rail != null ? $"sections={rail.childCount}" : "no rail");

        var pane = overlay != null ? overlay.Find("HelpPanel/Card/Pane") : null;
        Check("the help page has a scrolling pane", pane != null && pane.GetComponent<ScrollRect>() != null);

        var counter = overlay != null ? overlay.Find("HelpPanel/Card/SectionCounter") : null;
        var counterText = counter != null ? counter.GetComponent<TMPro.TextMeshProUGUI>() : null;
                Check("the help page numbers its sections",
            counterText != null && System.Text.RegularExpressions.Regex.IsMatch(counterText.text, @"^\d+ / \d+$"),
            counterText != null ? counterText.text : "none");

        // The rail switches the pane; pick the Skills section and read the copy.
        if (rail != null)
        {
            var skills = rail.Find("Section5");
            var button = skills != null ? skills.GetComponent<Button>() : null;
            if (button != null) button.onClick.Invoke();
        }

        string paneText = AllText(pane);
        int cookAp = BaseWorkController.Instance.CookBaseApCost;
        int healAp = BaseWorkController.Instance.HealBaseApCost(false);

        Check("the skills section quotes the live cook cost",
            paneText.Contains($"COOK  ({cookAp} AP"), Head(paneText));
        Check("the skills section quotes the live AP-only heal cost",
            paneText.Contains($"{healAp} AP"), Head(paneText));

        // Every row must fit its own card; the copy is long, so a row that
        // forgot wrapping would silently clip instead of growing.
        var content = pane != null ? pane.Find("Content") : null;
        Check("help rows are laid out in a growing column",
            content != null && content.GetComponent<VerticalLayoutGroup>() != null &&
            content.GetComponent<ContentSizeFitter>() != null);

        var close = overlay != null ? overlay.Find("HelpPanel/Card/CloseButton") as RectTransform : null;
        if (close != null) ClickThroughRaycast(close);
        Check("CLOSE hides the help page", group != null && group.alpha < 0.5f);
    }

    static string AllText(Transform root)
    {
        if (root == null) return "";

        var sb = new StringBuilder();
        foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true)) sb.Append(text.text).Append('\n');
        return sb.ToString();
    }

    static string Head(string value)
    {
        if (string.IsNullOrEmpty(value)) return "no text";
        return value.Length > 90 ? value.Substring(0, 90) : value;
    }

    /// <summary>Core items are zone-bound: Normal hands over the key, Hard the box, on a Hard Success.</summary>
    static void CheckPity()
    {
        var gather = GatherDispatchController.Instance;
        Check("core items are bound to a cave, not rolled",
            GatherDispatchController.CoreItemForZone(GatherZone.Normal) == CoreItemKind.B &&
            GatherDispatchController.CoreItemForZone(GatherZone.Hard) == CoreItemKind.A &&
            GatherDispatchController.CoreItemForZone(GatherZone.Easy) == null,
            "Normal=Key Hard=Box Easy=none");

        Check("the pity fields are gone",
            typeof(GatherZoneConfig).GetField("coreItemPityTrips") == null &&
            typeof(GatherDispatchController).GetMethod("PityCounter") == null,
            "removed");
    }

    /// <summary>Talent effects: doubled output and the Heal blessing.</summary>
    static void CheckTalents(SkillTriggerPanel skill, DispatchActionPanel action)
    {
        var baseWork = BaseWorkController.Instance;
        var chef = ByName("Cia");
        var medic = ByName("Ann");
        var crafter = ByName("Boon");

        Check("roster uses the new aptitude/talent table",
            chef.primaryAptitude == ZoneAptitude.Normal && chef.HasTalent(UnitTalent.Cooking) &&
            medic.primaryAptitude == ZoneAptitude.Easy && medic.HasTalent(UnitTalent.Heal) &&
            crafter.HasTalent(UnitTalent.Crafting) && ByName("Dan").HasTalent(UnitTalent.Exploring));

        Revive(chef);
        Check("Cooking talent doubles the food per round", baseWork.CookFoodPerRound(chef) == 2);
        Check("a unit without the talent cooks the base amount",
            baseWork.CookFoodPerRound(ByName("Ann")) == 1);

        Check("buff potion costs 2 AP + 5 materials",
            baseWork.WorkshopBaseApCost(WorkshopMode.BuffPotion) == 2 &&
            baseWork.WorkshopBaseMaterialCost(WorkshopMode.BuffPotion) == 5);

        int before = RunResources.Instance.PartySuccessBonus;
        RunResources.Instance.AddPartySuccessBonus(baseWork.BuffPotionBonusPerRound);
        Check("buff potion raises the party success bonus",
            RunResources.Instance.PartySuccessBonus == before + baseWork.BuffPotionBonusPerRound);
        RunResources.Instance.SetPartySuccessBonus(before);
    }

    /// <summary>Arming a skill must drop the picked cave, and vice versa.</summary>
    static void CheckSkillAndDungeonAreExclusive(UnitRosterView roster, SkillFlowController flow, SkillTriggerPanel skill, DispatchActionPanel action)
    {
        ResetUnits(roster);
        var map = GatherMapController.Instance;
        var node = Object.FindAnyObjectByType<MapNodeView>();

        // Clicking an already-selected cave toggles it off, so start clean.
        map.ClearSelection();

        ClickCard(roster, ByName("Cia"));
        ClickThroughRaycast((RectTransform)node.transform);
        Check("clicking a dungeon selects it", map.SelectedNode == node);

        ClickButton(skill, "CookButton");
        Check("arming a skill clears the dungeon selection", map.SelectedNode == null);
        Check("confirm goes back to the skill", action.CurrentLabel == "COOK");

        Canceller()?.CancelPending();

        // Cancelling drops the crew too, so the row is grey until somebody is
        // picked again - the cave click below needs that.
        ClickCard(roster, ByName("Cia"));
        ClickThroughRaycast((RectTransform)node.transform);
        Check("clicking a dungeon drops the armed skill", map.SelectedNode == node && !flow.IsSkillActive);

        map.ClearSelection();
    }

    /// <summary>Card slots the sprint sketch asked for are actually built.</summary>
    static void CheckCardReadouts(UnitRosterView roster)
    {
        ResetUnits(roster);
        var card = CardFor(roster, ByName("Ann"));
        if (card == null)
        {
            Check("a card exists for Ann", false);
            return;
        }

        Transform lift = card.transform.Find("Lift");
        Check("card builds the Lift container", lift != null);
        Check("card has a state icon slot", lift != null && lift.Find("StateIcon") != null);

        // The READY state must be a sword drawn from real art, not the generated
        // placeholder - the Kenney board-game pack ships one. The badge stays
        // visible while the unit is Idle and dims once the sword is spent, so the
        // player can see "not today" without losing the icon.
        var stateIcon = lift != null ? lift.Find("StateIcon") : null;
        var stateImage = stateIcon != null ? stateIcon.GetComponent<Image>() : null;
        string swordName = stateImage != null && stateImage.sprite != null ? stateImage.sprite.name : "none";
        bool idle = card.Unit.state == UnitState.Idle;
        Check("the READY state shows the sword sprite",
            stateImage != null && stateImage.sprite != null && swordName == "state_sword"
            && stateIcon.gameObject.activeInHierarchy == idle,
            $"sprite={swordName} active={(stateIcon != null && stateIcon.gameObject.activeInHierarchy)} state={card.Unit.state}");

        Check("the sword badge dims once the sword is spent",
            stateImage == null || (card.Unit.HasSword ? stateImage.color.a > 0.9f : stateImage.color.a < 0.5f),
            $"swords={card.Unit.currentSwords} alpha={(stateImage != null ? stateImage.color.a : -1f)}");
        Check("card has an aptitude badge", lift != null && lift.Find("AptitudeBadge") != null);
        Check("card has a talent icon slot", lift != null && lift.Find("TalentIcon") != null);
        Check("card has a buff lamp row", lift != null && lift.Find("Lamps") != null);

        var lamps = lift.Find("Lamps");
        Check("all six buff/debuff lamps are present", lamps != null && lamps.childCount == 6);
    }

    /// <summary>Sprint round 2: HUD chrome (Options page, card lift, craft row).</summary>
    static void CheckHudChrome(UnitRosterView roster, SkillTriggerPanel skill, DispatchActionPanel action)
    {
        var bar = Object.FindAnyObjectByType<HudTopBar>();
        if (bar == null)
        {
            Check("the HUD top bar exists", false);
            return;
        }

        var optionsButton = bar.transform.Find("OptionsButton") as RectTransform;
        var helpButton = bar.transform.Find("HelpButton") as RectTransform;
        Check("the top-left chrome is OPTIONS + HELP",            optionsButton != null && helpButton != null);

        var overlay = bar.transform.Find("OptionsOverlay");
        var group = overlay != null ? overlay.GetComponent<CanvasGroup>() : null;
        Check("options starts hidden", group != null && group.alpha < 0.5f);

        if (optionsButton != null)
        {
            ClickThroughRaycast(optionsButton);
            Check("OPTIONS opens the options page", group != null && group.alpha > 0.5f && group.blocksRaycasts);

            var returnButton = overlay.Find("OptionsPanel/Card/ReturnToMenuButton") as RectTransform;
            Check("the options page is the menu page plus RETURN TO MENU", returnButton != null);

            var backButton = overlay.Find("OptionsPanel/Card/ReturnToGameButton");
            var backLabel = backButton != null ? backButton.GetComponentInChildren<TMP_Text>() : null;
            Check("the in-game way back reads BACK TO GAME",
                backLabel != null && backLabel.text == "BACK TO GAME",
                backLabel != null ? backLabel.text : "no label");

            if (returnButton != null)
            {
                // Hit-test it: the map must not steal the click.
                Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, returnButton.TransformPoint(returnButton.rect.center));
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = centre }, hits);
                GameObject resolved = hits.Count > 0
                    ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)
                    : null;
                Check("RETURN TO MENU is actually clickable", resolved != null && resolved.name == "ReturnToMenuButton",
                    resolved != null ? resolved.name : "none");
            }

            var closeButton = overlay.Find("OptionsPanel/Card/ReturnToGameButton") as RectTransform;
            if (closeButton != null) ClickThroughRaycast(closeButton);
            Check("BACK TO GAME hides the options page again", group != null && group.alpha < 0.5f);

            // The page must be the same one the menu shows, so it carries the
            // three real settings rather than a stand-in layout.
            Check("the options page has the resolution row",
                overlay.Find("OptionsPanel/Card/ResolutionField") != null);
            Check("the options page has the display choice",
                overlay.Find("OptionsPanel/Card/WindowedButton") != null
                && overlay.Find("OptionsPanel/Card/FullscreenButton") != null);
            Check("the options page splits audio into master, music and sfx faders",
                overlay.Find("OptionsPanel/Card/MasterSlider") != null
                && overlay.Find("OptionsPanel/Card/MusicSlider") != null
                && overlay.Find("OptionsPanel/Card/SfxSlider") != null);
            Check("each fader has its own percentage readout",
                overlay.Find("OptionsPanel/Card/MasterValue") != null
                && overlay.Find("OptionsPanel/Card/MusicValue") != null
                && overlay.Find("OptionsPanel/Card/SfxValue") != null);
        }

        CheckHelpPage(bar, helpButton);

        // Top-left chrome is art only: a square plate with a centred cog for
        // OPTIONS and a question mark for HELP, both from the Kenney packs.
        foreach (var pair in new[]
        {
            new { Button = "OptionsButton", Want = "ui_gear" },
            new { Button = "HelpButton", Want = "ui_question" },
        })
        {
            var plate = bar.transform.Find(pair.Button) as RectTransform;
            var icon = bar.transform.Find(pair.Button + "/" + pair.Button + "Icon") as RectTransform;
            var image = icon != null ? icon.GetComponent<Image>() : null;
            string actual = image != null && image.sprite != null ? image.sprite.name : "none";
            Check($"the {pair.Button} shows the {pair.Want} sprite", actual == pair.Want, $"got {actual}");

            Check($"the {pair.Button} has no text label", bar.transform.Find(pair.Button + "/Label") == null,
                "a Label object is still there");

            Check($"the {pair.Button} frame is square",
                plate != null && Mathf.Abs(plate.sizeDelta.x - plate.sizeDelta.y) < 0.5f && plate.sizeDelta.x > 0f,
                plate != null ? $"{plate.sizeDelta}" : "no plate");

            Check($"the {pair.Button} icon is centred in its frame",
                plate != null && icon != null
                && Vector2.Distance(icon.anchoredPosition, Vector2.zero) < 0.5f
                && icon.anchorMin == new Vector2(0.5f, 0.5f)
                && icon.sizeDelta.x <= plate.sizeDelta.x,
                icon != null ? $"pos={icon.anchoredPosition} size={icon.sizeDelta}" : "no icon");
        }

        // Card lift: selecting must raise the card well clear of its neighbours.
        ResetUnits(roster);
        var card = FirstSelectable(roster);
        if (card != null)
        {
            var lift = (RectTransform)card.transform.Find("Lift");

            ClickCardView(card);
            SettleLift(card);
            float raised = lift != null ? lift.anchoredPosition.y : 0f;

            card.SetSelected(false);
            SettleLift(card);
            float resting = lift != null ? lift.anchoredPosition.y : 0f;

            float travel = Mathf.Abs(resting - raised);
            Check("selecting a card lifts it 50-100px",
                card.IsSelected == false && travel >= 50f && travel <= 100f,
                $"raised={raised:0.0} resting={resting:0.0} travel={travel:0.0}");
            Check("a selected card sits at its slot, an idle one hangs below",
                Mathf.Abs(raised) < 1f && resting <= -49f,
                $"raised={raised:0.0} resting={resting:0.0}");
        }

        // The whole card (background + frame included) must travel with Lift.
        var anyCard = FirstSelectable(roster);
        if (anyCard != null)
        {
            var cardLift = anyCard.transform.Find("Lift");
            Check("card background rides with the lift",
                cardLift != null && cardLift.Find("Background") != null &&
                cardLift.Find("Frame0") != null);
        }

        // A lifted card is allowed to cover the dungeon row, so the hand must be
        // drawn after the map.
        var mapPanel = Object.FindAnyObjectByType<GatherMapController>();
        var handPanel = Object.FindAnyObjectByType<CharacterHandPanel>();
        if (mapPanel != null && handPanel != null &&
            mapPanel.transform.parent == handPanel.transform.parent)
        {
            Check("the hand draws on top of the dungeon row",
                handPanel.transform.GetSiblingIndex() > mapPanel.transform.GetSiblingIndex());
        }
        else
        {
            Check("the hand lives in the same canvas as the dungeon row",
                mapPanel != null && handPanel != null);
        }

        // A lifted card may cover the dungeon art, but never its labels.
        var liftProbe = FirstSelectable(roster);
        var dungeonNodes = SortNodesByTitle(Object.FindObjectsByType<MapNodeView>());
        if (liftProbe != null && dungeonNodes.Length > 0)
        {
            var probeLift = (RectTransform)liftProbe.transform.Find("Lift");
            Vector2 parked = probeLift.anchoredPosition;
            probeLift.anchoredPosition = new Vector2(parked.x, 0f);
            Canvas.ForceUpdateCanvases();

            Vector3[] liftCorners = new Vector3[4];
            probeLift.GetWorldCorners(liftCorners);
            float liftTop = liftCorners[2].y;

            var cover = new List<Rect>();
            foreach (var view in roster.Cards)
            {
                Vector3[] c = new Vector3[4];
                ((RectTransform)view.transform).GetWorldCorners(c);
                cover.Add(Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y));
            }

            var hidden = new List<string>();
            foreach (var node in dungeonNodes)
            {
                foreach (string child in new[] { "Title", "CoreDrop", "Chance", "Drops" })
                {
                    var label = ((RectTransform)node.transform).Find(child) as RectTransform;
                    if (label == null) continue;

                    Vector3[] lc = new Vector3[4];
                    label.GetWorldCorners(lc);
                    foreach (var box in cover)
                    {
                        bool sideBySide = lc[2].x < box.xMin || lc[0].x > box.xMax;
                        if (!sideBySide && lc[0].y < liftTop - 0.5f)
                        {
                            hidden.Add($"{node.Data.zone}/{child} bottom={lc[0].y:0} liftTop={liftTop:0}");
                            break;
                        }
                    }
                }
            }

            probeLift.anchoredPosition = parked;
            Canvas.ForceUpdateCanvases();
            Check("no dungeon label hides under a lifted card", hidden.Count == 0,
                string.Join("; ", hidden));

            var firstCard = (RectTransform)dungeonNodes[0].transform;
            Check("the dungeon card keeps its full height", firstCard.rect.height >= 620f,
                $"height={firstCard.rect.height:0}");
        }

        // The confirm button must not sit on top of the hand.
        var dispatchPanel = Object.FindAnyObjectByType<DispatchActionPanel>(FindObjectsInactive.Include);
        if (dispatchPanel != null)
        {
            Vector3[] dc = new Vector3[4];
            ((RectTransform)dispatchPanel.transform).GetWorldCorners(dc);
            var dispatchBox = Rect.MinMaxRect(dc[0].x, dc[0].y, dc[2].x, dc[2].y);

            var collisions = new List<string>();
            foreach (var view in roster.Cards)
            {
                Vector3[] c = new Vector3[4];
                ((RectTransform)view.transform).GetWorldCorners(c);
                var box = Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
                if (box.Overlaps(dispatchBox)) collisions.Add($"{view.Unit.unitName} {box} vs {dispatchBox}");
            }

            Check("the dispatch button clears the character cards", collisions.Count == 0,
                string.Join("; ", collisions));
        }

        // Core Item column: matches the Day frame's right edge, stacked 2x1, no text.
        var canvasRoot = GameObject.Find("Canvas").transform;
        var day = canvasRoot.Find("StatusBar/DayFrame") as RectTransform;
        var coreGroup = canvasRoot.Find("StatusBar/CoreItemGroup") as RectTransform;
        if (day != null && coreGroup != null)
        {
            float dayRight = day.anchoredPosition.x + day.sizeDelta.x;
            float coreRight = coreGroup.anchoredPosition.x;
            Check("Core Items share the Day frame's right edge", Mathf.Abs(dayRight - coreRight) < 1f,
                $"day={dayRight:0.0} core={coreRight:0.0}");
            Check("Core Items sit below the Day frame",
                coreGroup.anchoredPosition.y < day.anchoredPosition.y - day.sizeDelta.y);

            var status = coreGroup.Find("CoreItemStatus");
            Check("the Core Item slots carry no text", status == null || !status.gameObject.activeSelf);
        }

        // Craft row mirrors the heal row: wide action + narrow mode toggle.
        var craftRow = skill.transform.Find("CraftRow") as RectTransform;
        var healRow = skill.transform.Find("HealRow") as RectTransform;
        Check("craft got a row of its own like heal", craftRow != null && healRow != null);

        var craftMode = craftRow != null ? craftRow.Find("CraftModeButton") : null;
        Check("the craft mode toggle sits inside the row",
            craftMode != null && craftRow.Find("CraftButton") != null);

        if (craftRow != null && healRow != null && craftMode != null)
        {
            Check("craft row matches the heal row size", craftRow.sizeDelta == healRow.sizeDelta,
                $"craft={craftRow.sizeDelta} heal={healRow.sizeDelta}");
            Check("craft mode button has the heal toggle's size",
                ((RectTransform)craftMode).sizeDelta == ((RectTransform)healRow.Find("HealModeButton")).sizeDelta);
        }
    }

    // ------------------------------------------------- heal restore (2026-11-03)

    /// <summary>
    /// Healing clears Hurt, but it must not hand the patient a turn they cannot
    /// afford. Dan sent to Hard spends all 3 AP, gets hurt, and is healed: he
    /// used to come back Idle with an empty meter, so his card stayed clickable
    /// and every skill then refused him.
    /// </summary>
    static void CheckPatientComesBackResting()
    {
        var controller = BaseWorkController.Instance;
        var healer = ByName("Ann");
        var patient = ByName("Dan");
        if (controller == null || healer == null || patient == null) return;

        Revive(healer);
        patient.state = UnitState.Hurt;
        patient.currentActionPoints = 0;

        var result = controller.Heal(healer, patient, true);
        Check("a healed unit with an empty AP bar comes back Resting",
            result.healedUnit == patient && patient.state == UnitState.Resting,
            $"state={patient.state} ap={patient.currentActionPoints}");

        Check("a healed unit with no AP cannot be picked again",
            !patient.CanBeDispatched && !patient.CanBeSentGathering,
            $"state={patient.state} ap={patient.currentActionPoints}");

        // The other half of the rule: AP left means the unit is playable again.
        Revive(healer);
        patient.state = UnitState.Hurt;
        patient.currentActionPoints = 2;

        var healed = controller.Heal(healer, patient, true);
        Check("a healed unit with AP left returns Idle and playable",
            healed.healedUnit == patient && patient.state == UnitState.Idle && patient.CanBeDispatched,
            $"state={patient.state} ap={patient.currentActionPoints}");

        Revive(healer);
        Revive(patient);
    }

    // ------------------------------------------------------------- sections

    static void Section(string name) => Log.AppendLine($"-- {name} --");

    /// <summary>
    /// Every label must use the pixel font and every character it prints must
    /// exist in that atlas - a miss silently falls back to LiberationSans, which
    /// is the one way two typefaces end up on screen at once.
    /// </summary>
    /// <summary>
    /// The warning banner must read as one block: a single CanvasGroup on the
    /// group root (two groups multiply the fade) and the label sitting exactly
    /// inside the backdrop, centred. A stretched child that keeps its old pivot
    /// or offset drifts away from the parent, which is what happened before.
    /// </summary>
    /// <summary>
    /// A message must look the same in the log and on the banner. Both read
    /// UiPalette, so this compares the colours actually rendered by each: add a
    /// log entry, show the same text on the banner, then diff the two.
    ///
    /// Show() paints the first message synchronously (StartCoroutine runs to its
    /// first yield), so this works inside one frame.
    /// </summary>
    static void CheckWarningColour(GameObject canvas)
    {
        var view = canvas.GetComponentInChildren<WarningView>(true);
        var logPanel = canvas.GetComponentInChildren<LogMessagePanel>(true);
        if (view == null || logPanel == null)
        {
            Check("log panel and warning banner exist", false,
                $"view={(view != null)} log={(logPanel != null)}");
            return;
        }

        const string probe = "Colour probe.";

        foreach (var kind in new[] { LogKind.Warning, LogKind.Fail, LogKind.Success })
        {
            RunLog.Clear();
            RunLog.Add(kind, probe);
            view.Show(probe, kind);

            var row = FindText(logPanel.gameObject, probe);
            var banner = view.GetComponentInChildren<TMPro.TMP_Text>(true);

            bool ok = row != null && banner != null && SameRgb(row.color, banner.color);
            Check($"banner colour matches the log row ({kind})", ok,
                $"log={(row != null ? row.color.ToString() : "not found")} " +
                $"banner={(banner != null ? banner.color.ToString() : "none")}");

            view.Clear();
        }

        RunLog.Clear();
    }

    /// <summary>
    /// A wrapped log line must be taller than one line's worth of glyphs, or the
    /// second line draws on top of the next entry. The row's LayoutElement used
    /// to pin every entry to 24 px regardless of how many lines it needed.
    /// </summary>
    static void CheckLogRowsFit(GameObject canvas)
    {
        var panel = canvas.GetComponentInChildren<LogMessagePanel>(true);
        if (panel == null) { Check("log panel exists", false, "missing"); return; }

        RunLog.Clear();
        RunLog.Add(LogKind.Action, "The quick brown fox jumps over the lazy dog, twice over, then once more for good measure.");

        var content = panel.transform.Find("Expanded/Viewport/Content") as RectTransform;
        if (content == null) { Check("log content exists", false, "missing"); return; }

        var clipping = new List<string>();
        int wrapped = 0;
        for (int i = 0; i < content.childCount; i++)
        {
            var row = content.GetChild(i) as RectTransform;
            if (row == null || !row.gameObject.activeSelf) continue;

            var text = row.GetComponent<TMPro.TMP_Text>();
            if (text == null) continue;

            float needed = text.preferredHeight;
            if (needed > row.rect.height + 1f) clipping.Add($"{row.name} h={row.rect.height:0} needs={needed:0}");
            if (needed > 26f) wrapped++;
        }
        Check("a wrapped log line gets the height it needs", clipping.Count == 0 && wrapped > 0,
            wrapped == 0 ? "no wrapped row was produced" : string.Join("; ", clipping));

        RunLog.Clear();
    }

    static TMPro.TMP_Text FindText(GameObject root, string contains)
    {
        foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            if (text.text != null && text.text.Contains(contains)) return text;
        return null;
    }

    static bool SameRgb(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f;

    static void CheckWarningGroup(GameObject canvas)
    {
        var rootT = canvas.transform.Find("Warning");
        if (rootT == null) { Check("warning group exists", false, "Canvas/Warning missing"); return; }

        var groups = rootT.GetComponentsInChildren<CanvasGroup>(true);
        var view = rootT.GetComponent<WarningView>();
        Check("warning banner fades through one CanvasGroup on the group root",
            groups.Length == 1 && groups[0].gameObject == rootT.gameObject && view != null,
            $"groups={groups.Length} view={(view != null ? view.gameObject.name : "none")}");

        var backdrop = rootT.Find("WarningBackdrop") as RectTransform;
        var text = rootT.Find("WarningText") as RectTransform;
        if (backdrop == null || text == null) { Check("warning backdrop and text exist", false); return; }

        var b = new Vector3[4];
        var t = new Vector3[4];
        backdrop.GetWorldCorners(b);
        text.GetWorldCorners(t);

        float left = t[0].x - b[0].x, right = b[2].x - t[2].x;
        float bottom = t[0].y - b[0].y, top = b[2].y - t[2].y;
        bool inside = left >= 0f && right >= 0f && bottom >= 0f && top >= 0f;
        bool centred = Mathf.Abs(left - right) < 0.5f && Mathf.Abs(bottom - top) < 0.5f;

        Check("warning text sits centred inside the backdrop",
            inside && centred && (left + right + bottom + top) > 0f,
            $"insets L={left:F1} R={right:F1} B={bottom:F1} T={top:F1}");
    }

    static void CheckFont(GameObject canvas)
    {
        var texts = canvas.GetComponentsInChildren<TMPro.TMP_Text>(true);
        var wrongFont = new List<string>();
        var missing = new HashSet<char>();

        foreach (var text in texts)
        {
            if (text.font == null || text.font.name != "PixelTileset")
            {
                wrongFont.Add($"{text.gameObject.name}->{(text.font != null ? text.font.name : "null")}");
                continue;
            }

            if (string.IsNullOrEmpty(text.text)) continue;

            string plain = System.Text.RegularExpressions.Regex.Replace(text.text, "<[^>]*>", "");
            foreach (char c in plain)
            {
                if (c == '\n' || c == '\r') continue;
                if (!text.font.HasCharacter(c)) missing.Add(c);
            }
        }

        Check(wrongFont.Count == 0
                ? $"all {texts.Length} labels use the pixel font"
                : "labels not on PixelTileset: " + string.Join(", ", wrongFont),
            wrongFont.Count == 0);

        // Covers the text the panels print for the current state, which is the
        // set that actually renders during play.
        Check(missing.Count == 0
                ? "every printed character has a glyph"
                : "missing glyphs: " + string.Concat(missing),
            missing.Count == 0);
    }

    static void CheckLegacyGone(GameObject canvas)    {
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

    static void CheckCoreItemSlots(GameObject canvas)    {
        Transform a = canvas.transform.Find("StatusBar/CoreItemGroup/CoreItemFrame");
        Transform b = canvas.transform.Find("StatusBar/CoreItemGroup/CoreItemFrameB");

        // Slots are stacked vertically now (2x1), so they must not overlap in Y.
        Check("both Core Item slots exist and are stacked without overlap",
            a != null && b != null &&
            Mathf.Abs(((RectTransform)b).anchoredPosition.y - ((RectTransform)a).anchoredPosition.y) >=
            ((RectTransform)a).sizeDelta.y);

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

        CheckCoreItemArt(statusBar, a, b);
    }

    /// <summary>
    /// The two slots show the run's box/key pair. The box stays a box and only
    /// opens when both pieces are held; the crystal is a separate reveal, so the
    /// HUD must never paint it.
    /// </summary>
    static void CheckCoreItemArt(StatusBarView statusBar, Transform slotA, Transform slotB)
    {
        var iconA = slotA != null ? slotA.Find("CoreItemIcon")?.GetComponent<Image>() : null;
        var iconB = slotB != null ? slotB.Find("CoreItemBIcon")?.GetComponent<Image>() : null;
        if (iconA == null || iconB == null)
        {
            Check("both Core Item art slots are reachable", false, "icon images missing");
            return;
        }

        var res = RunResources.Instance;

        foreach (var pair in new[]
                 {
                     (variant: CoreItemVariant.Orange, closed: "coreitem_panel_orange", open: "coreitem_panel_orange_bar", key: "coreitem_key_lit", crystal: "coreitem_crystal"),
                     (variant: CoreItemVariant.Yellow, closed: "coreitem_panel_yellow", open: "coreitem_panel_yellow_bar", key: "coreitem_key_plain", crystal: "coreitem_crystal_b")
                 })
        {
            res.SetVariant(pair.variant);
            ClearCoreItemFlags();
            res.ObtainCoreItem(CoreItemKind.A);
            InvokeUpdate(statusBar);
            bool closedOk = iconA.sprite != null && iconA.sprite.name == pair.closed;

            res.ObtainCoreItem(CoreItemKind.B);
            InvokeUpdate(statusBar);
            bool openOk = iconA.sprite != null && iconA.sprite.name == pair.open;
            bool noCrystalInHud = iconA.sprite == null || iconA.sprite.name != pair.crystal;
            bool keyOk = iconB.sprite != null && iconB.sprite.name == pair.key;

            Check($"{pair.variant} pair: closed box, then the same box open (crystal stays out of the HUD)",
                closedOk && openOk && noCrystalInHud && keyOk,
                $"box={Name(iconA.sprite)} key={Name(iconB.sprite)} (expected {pair.closed} -> {pair.open} / {pair.key})");
        }

        CheckCrystalReveal();

        ClearCoreItemFlags();
        InvokeUpdate(statusBar);
    }

    /// <summary>The win opens a centre-screen reveal carrying the crystal for the run's pair.</summary>
    static void CheckCrystalReveal()
    {
        var reveal = Object.FindAnyObjectByType<CrystalRevealView>(FindObjectsInactive.Include);
        Check("a crystal reveal exists under the canvas", reveal != null);
        if (reveal == null) return;

        var res = RunResources.Instance;

        foreach (var pair in new[]
                 {
                     (variant: CoreItemVariant.Orange, crystal: "coreitem_crystal"),
                     (variant: CoreItemVariant.Yellow, crystal: "coreitem_crystal_b")
                 })
        {
            res.SetVariant(pair.variant);
            ClearCoreItemFlags();
            reveal.Hide(true);
            InvokeUpdate(reveal);

            // Only the box: nothing to reveal yet.
            res.ObtainCoreItem(CoreItemKind.A);
            InvokeUpdate(reveal);
            bool silentBeforeKey = !reveal.IsShowing;

            // The key completes the pair -> the crystal appears.
            res.ObtainCoreItem(CoreItemKind.B);
            InvokeUpdate(reveal);
            bool showingWithCrystal = reveal.IsShowing &&
                                      reveal.CurrentCrystal != null &&
                                      reveal.CurrentCrystal.name == pair.crystal;

            Check($"{pair.variant} pair reveals {pair.crystal} once the box is opened",
                silentBeforeKey && showingWithCrystal,
                $"silent={silentBeforeKey} showing={reveal.IsShowing} crystal={Name(reveal.CurrentCrystal)}");
        }

        // Leave the game as it was: closed again, overlay hidden.
        reveal.Hide(true);
        InvokeUpdate(reveal);
    }

    static string Name(Sprite sprite) => sprite != null ? sprite.name : "null";

    /// <summary>Runs the card's lift animation until it settles (Update does this in a real frame).</summary>
    static void SettleLift(UnitIconView card)
    {
        var animate = card.GetType().GetMethod("AnimateLift",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (animate == null) return;

        for (int i = 0; i < 60; i++) animate.Invoke(card, null);
    }

    /// <summary>True when the object sits under a CanvasGroup that is faded out (a closed overlay).</summary>
    static bool IsInsideHiddenOverlay(GameObject target)
    {
        if (target == null) return false;

        foreach (var group in target.GetComponentsInParent<CanvasGroup>(true))
        {
            if (group.alpha < 0.5f || !group.blocksRaycasts) return true;
        }
        return false;
    }

    static void CheckClickSurfaces(GameObject canvas)
    {
        // Collect every interactive element once, then hit-test its centre the
        // way a real pointer would. Calling the handler directly would miss a
        // dead click surface, which is exactly how the Day frame broke.
        var targets = new HashSet<GameObject>();

        foreach (var behaviour in canvas.GetComponentsInChildren<MonoBehaviour>(false))
        {
            // The crystal reveal is a modal: while it is closed it must let every
            // click through, so it is not an interactive surface.
            if (behaviour is CrystalRevealView reveal && !reveal.IsShowing) continue;
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

        // Closed overlays (Options/Help) fade out with a CanvasGroup instead of
        // being deactivated, so their buttons must be skipped while hidden.
        targets.RemoveWhere(IsInsideHiddenOverlay);

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

    // ------------------------------------------------------------ fumble (2026-11-02)

    /// <summary>
    /// Spec: a Fumble loots nothing, hurts EVERY unit that went, and hands each
    /// of them a bad trait. Nobody dies from a roll - Death is starvation only.
    /// Forced through ApplyKnownOutcome because a 1-in-100 roll cannot be waited for.
    /// </summary>
    static void CheckFumble()
    {
        var dispatcher = GatherDispatchController.Instance;
        var first = ByName("Ann");
        var second = ByName("Dan");
        Revive(first);
        Revive(second);

        int badBefore = CountTraits(first, TraitPolarity.Negative) + CountTraits(second, TraitPolarity.Negative);
        var resources = RunResources.Instance;
        int foodBefore = resources.Food;
        int cookBefore = resources.CookingMaterial;
        int medBefore = resources.Medicine;
        int matBefore = resources.Materials;

        var result = dispatcher.ApplyKnownOutcome(GatherZone.Normal,
            new List<UnitCharacter> { first, second }, GatherResultTier.Fumble);

        Check("a fumble hurts every unit on the trip",
            result != null && first.state == UnitState.Hurt && second.state == UnitState.Hurt,
            $"first={first.state} second={second.state}");
        Check("a fumble hands a bad trait to each unit",
            CountTraits(first, TraitPolarity.Negative) + CountTraits(second, TraitPolarity.Negative) == badBefore + 2,
            $"before={badBefore} now={CountTraits(first, TraitPolarity.Negative) + CountTraits(second, TraitPolarity.Negative)}");
        Check("a fumble grants no loot",
            result != null && !result.mainLootGranted.HasValue && !result.bonusLootGranted.HasValue &&
            resources.Food == foodBefore && resources.CookingMaterial == cookBefore &&
            resources.Medicine == medBefore && resources.Materials == matBefore,
            $"main={result?.mainLootGranted} bonus={result?.bonusLootGranted}");
        Check("a fumble never kills",
            first.state != UnitState.Death && second.state != UnitState.Death);

        Revive(first);
        Revive(second);
    }

    static int CountTraits(UnitCharacter unit, TraitPolarity polarity)
    {
        if (unit == null) return 0;

        int count = 0;
        foreach (var trait in unit.traits)
        {
            if (trait != null && trait.polarity == polarity) count++;
        }
        return count;
    }

    // ------------------------------------------------------------- swords (2026-11-02)

    /// <summary>
    /// The sword is the dispatch ticket: one per unit, refreshed with AP, spent
    /// only by entering a cave. A unit holding none cannot be sent, and a skill
    /// does not touch it.
    /// </summary>
    static void CheckSwordRules(UnitRosterView roster)
    {
        var unit = ByName("Ann");
        unit.state = UnitState.Idle;
        unit.currentActionPoints = unit.maxActionPoints;
        unit.currentSwords = unit.maxSwords;

        Check("a fresh unit holds one sword and can be dispatched",
            unit.maxSwords == 1 && unit.HasSword && unit.CanBeSentGathering,
            $"swords={unit.currentSwords}/{unit.maxSwords}");

        unit.TrySpendSword();
        Check("spending the sword blocks a second trip the same day",
            unit.currentSwords == 0 && !unit.HasSword && !unit.CanBeSentGathering && unit.HasGatheredToday,
            $"swords={unit.currentSwords}");
        Check("a unit with no sword can still do base work", unit.CanBeDispatched);

        // End Day refreshes it exactly like AP.
        unit.RefreshForNewDay();
        Check("the sword refreshes with the new day",
            unit.currentSwords == unit.maxSwords && unit.CanBeSentGathering && !unit.HasGatheredToday,
            $"swords={unit.currentSwords}/{unit.maxSwords}");

        // Backend guard: even a hand-built selection is rejected.
        var dispatcher = GatherDispatchController.Instance;
        unit.currentSwords = 0;
        bool blocked = !dispatcher.CanDispatch(GatherZone.Easy, new List<UnitCharacter> { unit }, out string why);
        Check("dispatch is refused when the unit has no sword",
            blocked && !string.IsNullOrEmpty(why) && why.Contains("sword"), why ?? "no reason");

        unit.RefreshForNewDay();
        roster.RefreshAvailability();
    }

    static void ResetRun(UnitRosterView roster)
    {
        var resources = RunResources.Instance;
        resources.AddFood(10);
        resources.AddItem(InventoryItemType.CookingMaterial, 20);
        resources.AddItem(InventoryItemType.Materials, 20);
        resources.AddItem(InventoryItemType.Medicine, 5);

        ClearCoreItemFlags();
        ResetOutcome();
        if (GatherMapController.Instance != null) GatherMapController.Instance.ClearSelection();
        ResetUnits(roster);
        RunLog.Clear();
    }

    /// <summary>
    /// Puts the run back to InProgress and hides the result screen.
    ///
    /// The suite drives real trips, and a trip on a zone with a core-item chance
    /// can legitimately drop both pieces and win the run mid-suite. When that
    /// happens the result screen covers the HUD (every later raycast fails), the
    /// music stops, and End Day refuses to advance - a dozen red checks that have
    /// nothing to do with the thing under test. Resetting keeps the suite honest
    /// about what it is verifying instead of depending on a lucky roll.
    /// </summary>
    static void ResetOutcome()
    {
        var days = DayManager.Instance;
        if (days == null) return;

        if (days.Outcome != RunOutcome.InProgress)
        {
            var field = typeof(DayManager).GetField("<Outcome>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null) field.SetValue(days, RunOutcome.InProgress);
        }

        var result = Object.FindAnyObjectByType<RunResultView>(FindObjectsInactive.Include);
        var group = result != null ? result.GetComponent<CanvasGroup>() : null;
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        // A win also stops the music (PlayWin cuts it and stops the loop), and
        // the later audio checks would then hear silence that has nothing to do
        // with the thing under test.
        AudioDirector.Instance?.BeginRun(AudioDirector.PendingRunTrack);
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

    /// <summary>Runs a private Update() once. UI that repaints per frame does not tick inside a single test frame.</summary>
    static void InvokeUpdate<T>(T view) where T : MonoBehaviour
    {
        if (view == null) return;

        var method = typeof(T).GetMethod("Update",
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

    /// <summary>Sprite names of the visible AP pips, left to right ("none" when the card has no row).</summary>
    static string PipStates(UnitIconView card)
    {
        // The card chrome lives under "Lift" now, with ApPips below it.
        var row = card.transform.Find("Lift/ApPips") ?? card.transform.Find("ApPips");
        if (row == null) return "none";

        var parts = new List<string>();
        for (int i = 0; i < row.childCount; i++)
        {
            var child = row.GetChild(i);
            if (!child.gameObject.activeSelf) continue;
            var image = child.GetComponent<Image>();
            parts.Add(image != null && image.sprite != null ? image.sprite.name : "?");
        }
        return parts.Count == 0 ? "none" : string.Join(",", parts);
    }

    /// <summary>The pip string the card should paint for this unit and pending base cost.</summary>
    static string ExpectedPips(UnitCharacter unit, int pendingBaseCost)
    {
        int total = unit.maxActionPoints;
        int left = Mathf.Clamp(unit.currentActionPoints, 0, total);
        int spending = pendingBaseCost > 0 ? Mathf.Min(unit.GetActualApCost(pendingBaseCost), left) : 0;

        var parts = new List<string>();
        for (int i = 0; i < total; i++)
        {
            parts.Add(i >= left ? "ap_15" : i >= left - spending ? "ap_30" : "ap_45");
        }
        return parts.Count == 0 ? "none" : string.Join(",", parts);
    }

    static UnitIconView FirstSelectable(UnitRosterView roster)    {
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

    /// <summary>The run-wide right click: one gesture that drops every pending pick.</summary>
    static RightClickCancel Canceller() => Object.FindAnyObjectByType<RightClickCancel>();
    static void Confirm(DispatchActionPanel panel)
    {
        var button = panel.GetComponent<Button>();
        if (button != null) button.onClick.Invoke();
    }

    /// <summary>Parses the leading number out of a dungeon card's "NN  --  N AP" readout.</summary>
    static int ReadChance(MapNodeView node)
    {
        if (node == null || node.ChanceLabel == null) return -1;

        string text = node.ChanceLabel.text ?? "";
        int end = text.IndexOf(' ');
        if (end <= 0) return -1;

        return int.TryParse(text.Substring(0, end), out int value) ? value : -1;
    }

    /// <summary>Orders dungeon cards Easy → Normal → Hard by their title, since spawn order is not guaranteed.</summary>
    static MapNodeView[] SortNodesByTitle(MapNodeView[] nodes)    {
        var list = new System.Collections.Generic.List<MapNodeView>(nodes);
        list.Sort((a, b) =>
        {
            if (a == null || a.Data == null) return 1;
            if (b == null || b.Data == null) return -1;
            return ((int)a.Data.zone).CompareTo((int)b.Data.zone);
        });
        return list.ToArray();
    }

    /// <summary>Runs a card's pop animation to its resting value, since tests resolve in one frame.</summary>
    static void SettlePop(MapNodeView node)
    {        if (node == null) return;

        var update = node.GetType().GetMethod("Update",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        bool animated = false;

        if (update != null)
        {
            float before = node.PopScale;
            for (int i = 0; i < 40; i++) update.Invoke(node, null);
            animated = !Mathf.Approximately(node.PopScale, before);
        }

        // A paused/unfocused editor reports a zero delta, so the animation may not
        // have advanced at all; snap instead of reporting a false failure.
        if (!animated) node.SnapPop();
    }

    /// <summary>Names whatever the raycast resolves to at a rect's centre, for failure messages.</summary>
    static string DescribeHit(RectTransform rt)
    {
        if (rt == null) return "no rect";

        Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = centre }, hits);
        if (hits.Count == 0) return "no hits @ " + centre;

        var text = new System.Text.StringBuilder();
        text.Append("centre=").Append(centre).Append(" :: ");
        for (int i = 0; i < hits.Count && i < 5; i++)
        {
            text.Append(hits[i].gameObject.name)
                .Append("(d").Append(hits[i].depth)
                .Append(",c").Append(hits[i].module != null ? hits[i].module.name : "-")
                .Append(") ");
        }
        return text.ToString();
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

    /// <summary>Same as <see cref="Check(string,bool)"/> but prints the measured values on failure.</summary>
    static void Check(string label, bool ok, string detail)
    {
        if (ok) passed++;
        else failed++;

        Log.AppendLine((ok ? "PASS  " : "FAIL  ") + label + (ok ? "" : $"  [{detail}]"));
    }
}