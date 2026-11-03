using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Verification pass for the UI polish batch: the cook/craft steppers, the
/// right-click cancel, the hover feedback and the red fog. Run in play mode:
///   unity command script run --file AgentScripts/TestPolish.cs --entry TestPolish.Run
/// </summary>
public static class TestPolish
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

        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "FAILED: no Canvas - is Core.unity in play mode?";

        var roster = UnityEngine.Object.FindAnyObjectByType<UnitRosterView>();
        var flow = UnityEngine.Object.FindAnyObjectByType<SkillFlowController>();
        var skill = UnityEngine.Object.FindAnyObjectByType<SkillTriggerPanel>(FindObjectsInactive.Include);
        var map = GatherMapController.Instance;

        ResetRun(roster);

        Section("1. cook/craft counter");
        var cook = Find<RectTransform>(skill.transform, "CookButton");
        var cookRow = cook != null ? cook.parent as RectTransform : null;
        var count = cook != null ? cook.Find("Count") as RectTransform : null;

        Check("the cook button was moved into its own counter row",
            cookRow != null && cookRow.name == "CookRow",
            cookRow != null ? cookRow.name : "no CookButton parent");
        Check("the round number no longer lives inside the button frame",
            count == null && cookRow != null && cookRow.Find("Count") != null,
            count != null ? "Count is still a child of CookButton" : "ok");
        var rowCount = cookRow != null ? cookRow.Find("Count") as RectTransform : null;
        Check("the number sits to the left of the button",
            rowCount != null && rowCount.anchoredPosition.x < 0f &&
            rowCount.GetComponent<LayoutElement>() != null &&
            rowCount.GetComponent<LayoutElement>().ignoreLayout,
            rowCount != null
                ? $"x={rowCount.anchoredPosition.x:0} ignoreLayout={rowCount.GetComponent<LayoutElement>()?.ignoreLayout}"
                : "no count");
        Check("the button carries the + affordance in its right gutter",
            cook.transform.Find("Plus") != null &&
            cook.transform.Find("Plus").GetComponent<TMP_Text>().text == "+",
            cook.transform.Find("Plus") != null ? "found" : "missing");
        Check("no separate + button is left over after the move",
            cookRow != null && cookRow.Find("CookPlusButton") == null,
            "CookPlusButton");

        // The number needs a plate of its own, or it reads as a digit dropped on
        // the map instead of the "[ 2 ]" half of "[ 2 ] [ COOK + ]".
        var slot = cookRow != null ? cookRow.Find("CountSlot") : null;
        var slotImage = slot != null ? slot.GetComponent<Image>() : null;
        Check("the number is drawn on its own slot plate behind the button",
            slotImage != null && slotImage.color.a > 0.5f &&
            slot != null && rowCount != null && slot.GetSiblingIndex() < rowCount.GetSiblingIndex(),
            slotImage != null ? $"color={slotImage.color} behind={slot.GetSiblingIndex() < rowCount.GetSiblingIndex()}" : "no slot");

        var craft = Find<RectTransform>(skill.transform, "CraftButton");
        var craftRow = craft != null ? craft.parent as RectTransform : null;
        Check("craft shows the same [count] [CRAFT +] shape",
            craftRow != null && craftRow.name == "CraftRow" &&
            craftRow.Find("Count") != null && craft != null && craft.Find("Plus") != null,
            craft != null ? "CraftButton children: " + craft.childCount : "no craft button");

        // The whole button adds a round; the + is the visible promise of that.
        ClickCard(roster);
        int before = flow.PendingApCost;
        cook.GetComponent<Button>().onClick.Invoke();
        int afterOne = flow.PendingApCost;
        cook.GetComponent<Button>().onClick.Invoke();
        Check("each press queues one more round",
            before == 0 && afterOne > 0 && flow.PendingApCost > afterOne,
            $"ap {before} -> {afterOne} -> {flow.PendingApCost}");
        Check("the number beside the button shows the queued rounds",
            CountText(cookRow) == "2", $"text='{CountText(cookRow)}'");

        // The whole point of the layout: the number appearing must not move the
        // button. Layout groups re-align on activation, so this is measured, not
        // assumed.
        Vector2 withCount = PositionedCookPosition(skill, cook, 2);
        Vector2 withoutCount = PositionedCookPosition(skill, cook, 0);
        Check("the number appearing never shifts the skill button",
            Mathf.Abs(withCount.x - withoutCount.x) < 0.5f && Mathf.Abs(withCount.y - withoutCount.y) < 0.5f,
            $"0 rounds {withoutCount}, 2 rounds {withCount}");

        skill.SetCookCount(0);
        Check("the slot plate hides with the number",
            slot != null && !slot.gameObject.activeSelf,
            slot != null ? "active=" + slot.gameObject.activeSelf : "no slot");

        Section("2. right-click cancels");
        var canceller = UnityEngine.Object.FindAnyObjectByType<RightClickCancel>();
        Check("the scene carries the right-click cancel", canceller != null, canceller != null ? "found" : "missing");

        ResetRun(roster);
        ClickCard(roster);
        ClickCave(map);
        Check("setup: a card and a cave are pending",
            roster.SelectedCount == 1 && map.SelectedNode != null,
            $"sel={roster.SelectedCount} cave={(map.SelectedNode != null)}");
        canceller.CancelPending();
        Check("a right click drops the cave and the cards",
            roster.SelectedCount == 0 && map.SelectedNode == null,
            $"sel={roster.SelectedCount} cave={(map.SelectedNode != null)}");

        ResetRun(roster);
        ClickCard(roster);
        Find<Button>(skill.transform, "CookButton").onClick.Invoke();
        Check("setup: a skill is armed", flow.IsSkillActive, $"armed={flow.Armed}");
        canceller.CancelPending();
        Check("a right click disarms the skill and returns the hand to dispatch",
            !flow.IsSkillActive && roster.HandPanel.Mode == CharacterHandPanel.SelectionMode.Dispatch &&
            roster.SelectedCount == 0,
            $"armed={flow.Armed} mode={roster.HandPanel.Mode} sel={roster.SelectedCount}");

        Check("the skill buttons have no private right-click meaning left",
            Find<Button>(skill.transform, "CookButton").GetComponent("RightClickHandler") == null &&
            Find<Button>(skill.transform, "HealButton").GetComponent("RightClickHandler") == null,
            "RightClickHandler removed");

        Section("3. hover feedback");
        var cookHover = Find<RectTransform>(skill.transform, "CookButton").GetComponent<HoverHighlight>();
        Check("buttons carry the hover component", cookHover != null, cookHover != null ? "found" : "missing");

        // The column is grey until somebody is picked, and a grey button does not
        // glow - that is the point - so the hover is measured on an unlocked one.
        ResetRun(roster);
        ClickCard(roster);
        Check("setup: the skill column is unlocked for the hover check", !skill.IsLocked);

        var director = AudioDirector.Instance;
        int hovers = director != null ? director.HoverCount : -1;
        director?.ResetHoverThrottle();
        cookHover.OnPointerEnter(null);
        Check("hovering plays the hover tick",
            director != null && director.HoverCount == hovers + 1,
            director == null ? "no director" : $"{hovers} -> {director.HoverCount}");
        for (int i = 0; i < 40; i++) Invoke(cookHover, "Update");
        Check("hovering lights the clickable mask", cookHover.Amount > 0.99f, $"amount={cookHover.Amount:0.00}");
        cookHover.OnPointerExit(null);
        for (int i = 0; i < 40; i++) Invoke(cookHover, "Update");
        Check("leaving fades it back out", cookHover.Amount < 0.01f, $"amount={cookHover.Amount:0.00}");

        // A locked control must NOT advertise itself as clickable.
        roster.ClearSelection();
        cookHover.OnPointerEnter(null);
        Check("a greyed button refuses the hover glow",
            skill.IsLocked && cookHover.Amount < 0.01f, $"locked={skill.IsLocked} amount={cookHover.Amount:0.00}");

        var card = roster.Cards.Count > 0 ? roster.Cards[0] : null;
        Check("unit cards answer the pointer themselves",
            card != null && card is IPointerEnterHandler,
            card != null ? card.GetType().Name : "no cards");

        Section("4. risk + red fog");
        var fog = RedFogOverlay.Instance;
        var resources = RunResources.Instance;
        int startRisk = resources.Risk;

        Check("the map carries the fog overlay", fog != null, fog != null ? "found" : "missing");
        Check("a run starts at maximum risk", startRisk == RunResources.MaxRisk, $"{startRisk}/{RunResources.MaxRisk}");

        if (fog != null && resources != null)
        {
            resources.SetRisk(RunResources.MaxRisk);
            float thick = fog.Density;
            Check("the fog is painted straight from risk", Mathf.Abs(thick - 1f) < 0.001f,
                $"risk {RunResources.MaxRisk} -> {thick:0.000}");

            resources.SetRisk(20);
            float thin = fog.Density;
            Check("the fog thins with risk", Mathf.Abs(thin - 0.25f) < 0.001f,
                $"risk 20 -> {thin:0.000}");

            resources.SetRisk(0);
            Check("risk 0 clears the fog entirely", fog.Density <= 0.0001f && FogAlpha(fog) <= 0.0001f,
                $"{fog.Density:0.0000}");

            resources.SetRisk(startRisk);
            Check("nothing but risk can scale the fog",
                typeof(RedFogOverlay).GetProperty("Intensity") == null &&
                typeof(RedFogOverlay).GetMethod("SaveIntensity") == null,
                "no intensity knob");
        }

        Section("5. risk feeding the roll");
        if (resources != null)
        {
            var easy = GatherZoneDatabase.Instance.GetConfig(GatherZone.Easy);
            var normalZone = GatherZoneDatabase.Instance.GetConfig(GatherZone.Normal);
            var crew = new List<UnitCharacter> { roster.Cards[0].Unit };

            Check("each cave carries its own handicap",
                GatherResolver.ZoneHandicap(GatherZone.Easy) == 30 &&
                GatherResolver.ZoneHandicap(GatherZone.Normal) == 20 &&
                GatherResolver.ZoneHandicap(GatherZone.Hard) == 0,
                $"easy {GatherResolver.ZoneHandicap(GatherZone.Easy)} " +
                $"normal {GatherResolver.ZoneHandicap(GatherZone.Normal)} " +
                $"hard {GatherResolver.ZoneHandicap(GatherZone.Hard)}");

            // -Risk + Handicap, floored at 0: the handicap is an allowance, so a
            // cave stops feeling the fog once Risk has burned down to it.
            Check("the penalty is risk minus handicap, never negative",
                GatherResolver.RiskPenalty(GatherZone.Easy, 80) == 50 &&
                GatherResolver.RiskPenalty(GatherZone.Normal, 80) == 60 &&
                GatherResolver.RiskPenalty(GatherZone.Hard, 80) == 80 &&
                GatherResolver.RiskPenalty(GatherZone.Easy, 40) == 10 &&
                GatherResolver.RiskPenalty(GatherZone.Easy, 30) == 0 &&
                GatherResolver.RiskPenalty(GatherZone.Normal, 20) == 0 &&
                GatherResolver.RiskPenalty(GatherZone.Easy, 5) == 0,
                "easy 80-30, normal 80-20, hard 80-0, both floored");

            Check("the shallow caves free up before the deep one",
                GatherResolver.RiskPenalty(GatherZone.Easy, 25) == 0 &&
                GatherResolver.RiskPenalty(GatherZone.Normal, 25) == 5 &&
                GatherResolver.RiskPenalty(GatherZone.Hard, 25) == 25,
                "at risk 25: easy 0, normal 5, hard 25");

            resources.SetRisk(0);
            int cleanEasy = GatherResolver.ComputeSuccessValue(easy, crew);
            int cleanNormal = GatherResolver.ComputeSuccessValue(normalZone, crew);
            resources.SetRisk(80);
            int foggyEasy = GatherResolver.ComputeSuccessValue(easy, crew);
            int foggyNormal = GatherResolver.ComputeSuccessValue(normalZone, crew);
            resources.SetRisk(40);
            int easyAt40 = GatherResolver.ComputeSuccessValue(easy, crew);
            resources.SetRisk(startRisk);

            Check("the fog comes off each cave by -risk + handicap",
                cleanEasy - foggyEasy == 50 && cleanNormal - foggyNormal == 60,
                $"easy -{cleanEasy - foggyEasy}, normal -{cleanNormal - foggyNormal}");
            Check("half the risk is worth half the penalty",
                cleanEasy - easyAt40 == 10,
                $"risk 40 on easy costs {cleanEasy - easyAt40}");
        }

        Section("6. fog gauge");
        var gauge = UnityEngine.Object.FindAnyObjectByType<FogGaugeView>(FindObjectsInactive.Include);
        var firstCard = map != null && map.Nodes.Count > 0 ? (RectTransform)map.Nodes[0].transform : null;
        var gaugeRect = gauge != null ? (RectTransform)gauge.transform : null;

        Check("a fog gauge exists on the map", gauge != null, gauge != null ? "found" : "missing");
        if (gaugeRect != null && firstCard != null)
        {
            Check("the gauge is exactly as tall as the dungeon cards",
                Mathf.Abs(gaugeRect.rect.height - firstCard.rect.height) < 1f,
                $"gauge={gaugeRect.rect.height:0} card={firstCard.rect.height:0}");
            Check("the gauge sits to the left of the first card, outside it",
                gaugeRect.anchoredPosition.x + gaugeRect.rect.width * 0.5f <
                firstCard.anchoredPosition.x - firstCard.rect.width * 0.5f,
                $"gauge right {gaugeRect.anchoredPosition.x + gaugeRect.rect.width * 0.5f:0} " +
                $"card left {firstCard.anchoredPosition.x - firstCard.rect.width * 0.5f:0}");
            Check("the gauge is a narrow plate, not a second card",
                gaugeRect.rect.width < firstCard.rect.width * 0.2f,
                $"{gaugeRect.rect.width:0} vs {firstCard.rect.width:0}");
            Check("the gauge has a framed plate and a tube inside it",
                gaugeRect.Find("Plate") != null && gaugeRect.Find("Tube") != null &&
                gaugeRect.Find("FrameTop") != null && gaugeRect.Find("FrameLeft") != null,
                "Plate/Tube/Frame");
            Check("the tube is centred in the plate",
                gaugeRect.Find("Tube") is RectTransform tube && Mathf.Abs(tube.anchoredPosition.x) < 0.5f,
                "centred");

            var gaugeLabel = gaugeRect.Find("RiskLabel") as RectTransform;
            var gaugeText = gaugeLabel != null ? gaugeLabel.GetComponent<TMP_Text>() : null;
            float labelCentre = gaugeLabel != null
                ? gaugeLabel.anchoredPosition.x + gaugeLabel.rect.width * 0.5f
                : -1f;
            Check("the gauge is labelled RISK",
                gaugeText != null && gaugeText.text.Replace("\n", "") == "RISK" &&
                labelCentre > 0f && labelCentre < gaugeRect.rect.width * 0.25f,
                gaugeText != null ? $"label centre {labelCentre:0} of {gaugeRect.rect.width:0}" : "no label");
        }

        if (resources != null && gauge != null)
        {
            resources.SetRisk(20);
            gauge.SnapToRisk();
            float quarter = gauge.BarFraction;
            resources.SetRisk(68);
            gauge.SnapToRisk();
            float most = gauge.BarFraction;

            Check("the tube's column follows risk",
                Mathf.Abs(quarter - 0.25f) < 0.02f && Mathf.Abs(most - 0.85f) < 0.02f,
                $"risk 20 -> {quarter:0.00}, risk 68 -> {most:0.00}");

            resources.SetRisk(startRisk);
            gauge.SnapToRisk();
        }

        Section("7. options page");
        var options = OptionsPanelView.Create(canvas.transform, "BACK", () => { });
        Check("the options page no longer offers a fog control",
            Find<RectTransform>(options.transform, "FogSlider") == null &&
            Find<RectTransform>(options.transform, "FogValue") == null,
            "no FOG row");
        Check("the faders are all that is left on the audio side",
            Find<RectTransform>(options.transform, "MasterSlider") != null &&
            Find<RectTransform>(options.transform, "MusicSlider") != null &&
            Find<RectTransform>(options.transform, "SfxSlider") != null,
            "three faders");
        UnityEngine.Object.Destroy(options.gameObject);

        Section("8. risk spend + core items");
        var gather = GatherDispatchController.Instance;
        var normal = GatherZoneDatabase.Instance.GetConfig(GatherZone.Normal);
        var hard = GatherZoneDatabase.Instance.GetConfig(GatherZone.Hard);
        UnitCharacter scout = roster.Cards[0].Unit;

        if (resources != null && gather != null)
        {
            Check("a cleared Easy cave burns 5 risk",
                GatherResolver.RiskReduction(GatherZone.Easy) == 5 &&
                GatherResolver.RiskReduction(GatherZone.Normal) == 10 &&
                GatherResolver.RiskReduction(GatherZone.Hard) == 15);

            resources.SetRisk(80);
            gather.ApplyKnownOutcome(GatherZone.Easy, new List<UnitCharacter> { scout },
                GatherResultTier.Regular, 10, 50);
            Check("a Regular Success spends the cave's risk", resources.Risk == 75, $"risk={resources.Risk}");

            int beforeFail = resources.Risk;
            gather.ApplyKnownOutcome(GatherZone.Easy, new List<UnitCharacter> { scout },
                GatherResultTier.Failure, 90, 0);
            Check("a Failure leaves risk untouched", resources.Risk == beforeFail, $"risk={resources.Risk}");

            resources.SetRisk(3);
            gather.ApplyKnownOutcome(GatherZone.Hard, new List<UnitCharacter> { scout },
                GatherResultTier.Regular, 5, 50);
            Check("risk floors at 0 and cannot go negative", resources.Risk == 0, $"risk={resources.Risk}");

            resources.SetRisk(0);
            gather.ApplyKnownOutcome(GatherZone.Hard, new List<UnitCharacter> { scout },
                GatherResultTier.Regular, 5, 50);
            Check("no risk can be spent once it is 0", resources.Risk == 0, $"risk={resources.Risk}");

            // --- core items -------------------------------------------------
            ResetCoreItems();
            var result = gather.ApplyKnownOutcome(GatherZone.Normal, new List<UnitCharacter> { scout },
                GatherResultTier.HardSuccess, 40, 20);
            Check("a Normal Hard Success always hands over the key",
                resources.HasCoreItem(CoreItemKind.B) && !resources.HasCoreItem(CoreItemKind.A),
                $"{string.Join(",", result.coreItemsObtained)}");

            ResetCoreItems();
            result = gather.ApplyKnownOutcome(GatherZone.Hard, new List<UnitCharacter> { scout },
                GatherResultTier.HardSuccess, 40, 20);
            Check("a Hard Hard Success always hands over the box",
                resources.HasCoreItem(CoreItemKind.A) && !resources.HasCoreItem(CoreItemKind.B),
                $"{string.Join(",", result.coreItemsObtained)}");

            ResetCoreItems();
            result = gather.ApplyKnownOutcome(GatherZone.Easy, new List<UnitCharacter> { scout },
                GatherResultTier.HardSuccess, 40, 20);
            Check("an Easy Hard Success drops no core item",
                result.coreItemsObtained.Count == 0 && !resources.HasCoreItem(CoreItemKind.A) &&
                !resources.HasCoreItem(CoreItemKind.B),
                $"{result.coreItemsObtained.Count} pieces");

            ResetCoreItems();
            result = gather.ApplyKnownOutcome(GatherZone.Normal, new List<UnitCharacter> { scout },
                GatherResultTier.HardSuccess, 40, 79);
            Check("a natural 1 under SV 80 takes only the cave's own piece",
                result.coreItemsObtained.Count == 1 && !result.jackpot &&
                resources.HasCoreItem(CoreItemKind.B),
                $"sv=79 pieces={result.coreItemsObtained.Count} jackpot={result.jackpot}");

            ResetCoreItems();
            resources.ObtainCoreItem(CoreItemKind.B); // already holding the key
            result = gather.ApplyKnownOutcome(GatherZone.Normal, new List<UnitCharacter> { scout },
                GatherResultTier.HardSuccess, 1, 80);
            Check("a jackpot at SV 80 with the key already held grabs the box",
                result.jackpot && resources.HasBothCoreItems,
                $"jackpot={result.jackpot} both={resources.HasBothCoreItems}");

            ResetCoreItems();
            result = gather.ApplyKnownOutcome(GatherZone.Hard, new List<UnitCharacter> { scout },
                GatherResultTier.HardSuccess, 1, 80);
            Check("a jackpot on a fresh set takes both pieces",
                result.jackpot && resources.HasBothCoreItems && result.coreItemsObtained.Count == 2,
                $"pieces={result.coreItemsObtained.Count}");

            ResetCoreItems();
            RestoreOutcome();
            resources.SetRisk(startRisk);

            Check("the buff potion costs 5 materials",
                BaseWorkController.Instance != null &&
                BaseWorkController.Instance.WorkshopBaseMaterialCost(WorkshopMode.BuffPotion) == 5,
                BaseWorkController.Instance != null
                    ? BaseWorkController.Instance.WorkshopBaseMaterialCost(WorkshopMode.BuffPotion).ToString()
                    : "no controller");

            Check("pity is gone from the zone data",
                normal != null && hard != null &&
                typeof(GatherZoneConfig).GetField("coreItemPityTrips") == null &&
                typeof(GatherZoneConfig).GetField("coreItemChancePercent") == null);
        }

        Section("9. goal markers");
        var hud = UnityEngine.Object.FindAnyObjectByType<StatusBarView>();
        if (map != null && hud != null)
        {
            // The card promises the same art the HUD is collecting, so a player
            // can match "the thing in the corner" to "the cave that gives it".
            var keySlot = Find<Image>(map.Nodes[0].transform, "CorePiece");
            Sprite easyArt = map.Nodes.Count > 0 && keySlot != null ? keySlot.sprite : null;

            Sprite normalArt = null;
            Sprite hardArt = null;
            if (map.Nodes.Count > 2)
            {
                var a = Find<Image>(map.Nodes[1].transform, "CorePiece");
                var b = Find<Image>(map.Nodes[2].transform, "CorePiece");
                normalArt = a != null ? a.sprite : null;
                hardArt = b != null ? b.sprite : null;
            }

            Check("only the caves that hold a piece advertise one",
                easyArt == null && normalArt != null && hardArt != null,
                easyArt != null ? "Easy shows art" : "Easy empty, Normal and Hard show art");
            Check("Normal promises the key and Hard promises the box",
                normalArt == hud.CoreKeySprite && hardArt == hud.CoreBoxSprite &&
                normalArt != hardArt,
                "Normal=KEY Hard=BOX");

            var frame = (RectTransform)hud.transform.Find("CoreItemGroup/GoalFrame");
            var frameLabel = frame != null ? frame.Find("GoalLabel") : null;
            var frameText = frameLabel != null ? frameLabel.GetComponent<TMP_Text>() : null;
            var frames = frame != null ? frame.Find("FrameTop") != null && frame.Find("FrameLeft") != null : false;

            Check("the core item slots wear a framed plate",
                frame != null && frames && frame.GetComponent<Image>() != null,
                frame != null ? $"plate {frame.rect.size}" : "no frame");
            Check("the plate spells GOAL down its left rail",
                frameText != null && frameText.text.Replace("\n", "") == "GOAL" &&
                ((RectTransform)frameText.transform).anchoredPosition.x < frame.rect.width * 0.25f,
                frameText != null ? "vertical, inside the left edge" : "no label");
            Check("the plate wraps both slots without leaving the screen",
                frame != null &&
                frame.anchoredPosition.x < 0f &&
                frame.anchoredPosition.x + frame.rect.width <= ((RectTransform)frame.parent).rect.width + 8f,
                frame != null ? $"x {frame.anchoredPosition.x:0}..{frame.anchoredPosition.x + frame.rect.width:0}" : "no frame");
        }

        Section("10. card art + resource bar");
        if (map != null && map.Nodes.Count >= 3)
        {
            var arches = new Image[3];
            var badges = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                arches[i] = Find<Image>(map.Nodes[i].transform, "Cave");
                badges[i] = Find<Image>(map.Nodes[i].transform, "TierBadge");
            }

            Check("every card wears the same cave arch",
                arches[0] != null && arches[0].sprite != null &&
                arches[0].sprite == arches[1].sprite && arches[1].sprite == arches[2].sprite,
                arches[0] != null && arches[0].sprite != null ? arches[0].sprite.name : "missing");

            Check("each tier stamps its own badge on the arch",
                badges[0] != null && badges[0].sprite != null &&
                badges[1] != null && badges[1].sprite != null &&
                badges[2] != null && badges[2].sprite != null &&
                badges[0].sprite != badges[1].sprite &&
                badges[1].sprite != badges[2].sprite &&
                badges[0].sprite != badges[2].sprite,
                badges[2] != null && badges[2].sprite != null ? badges[2].sprite.name : "missing");

            Check("the hard cave wears the skull",
                badges[2] != null && badges[2].sprite != null &&
                badges[2].sprite.name.IndexOf("skull", System.StringComparison.OrdinalIgnoreCase) >= 0,
                badges[2] != null && badges[2].sprite != null ? badges[2].sprite.name : "missing");

            string readout = map.Nodes[0].ChanceLabel != null ? map.Nodes[0].ChanceLabel.text : "";
            Check("the success value is a bare number, not a percentage",
                readout.Length > 0 && readout.IndexOf('%') < 0 && char.IsDigit(readout[0]),
                readout);

            var hudView = UnityEngine.Object.FindAnyObjectByType<StatusBarView>();
            var easyDrops = map.Nodes[0].DropIcons;
            var easyConfig = map.Nodes[0].Config;
            bool dropsMatchHud = hudView != null && easyConfig != null && easyDrops.Count > 0;
            string dropNames = "";
            if (dropsMatchHud)
            {
                // Slot order is main loot first, then the bonus pool: build the
                // same list and compare each icon against the HUD's art.
                var expected = new System.Collections.Generic.List<Sprite>();
                System.Action<LootAmount> add = loot =>
                {
                    var art = hudView.IconFor(loot.type);
                    if (art != null && !expected.Contains(art)) expected.Add(art);
                };

                add(easyConfig.mainLoot);
                foreach (var loot in easyConfig.hardSuccessBonusPool) add(loot);

                for (int i = 0; i < easyDrops.Count && i < expected.Count; i++)
                {
                    var drawn = easyDrops[i] != null ? easyDrops[i].sprite : null;
                    dropNames += (drawn != null ? drawn.name : "none") + ";";
                    if (drawn == null || drawn != expected[i]) dropsMatchHud = false;
                }
            }

            Check("the loot row repeats the HUD's resource icons", dropsMatchHud, dropNames);
        }

        var status = UnityEngine.Object.FindAnyObjectByType<StatusBarView>();
        var resourceRow = status != null ? status.transform.Find("TopBar/ResourceRow") as RectTransform : null;
        if (resourceRow != null)
        {
            var corners = new Vector3[4];
            resourceRow.GetWorldCorners(corners);
            float centre = (corners[0].x + corners[2].x) * 0.5f;
            var canvasRect = (RectTransform)status.transform.parent;
            float screenCentre = canvasRect.rect.width * 0.5f;

            Check("the resource bar is centred on the screen",
                Mathf.Abs(centre - screenCentre) < 40f,
                $"row centre {centre:0} vs screen {screenCentre:0}");

            var foodIcon = Find<Image>(resourceRow, "FoodIcon");
            var foodLabel = Find<TMP_Text>(resourceRow, "Food");
            if (foodIcon != null && foodLabel != null)
            {
                foodIcon.rectTransform.GetWorldCorners(corners);
                float iconRight = corners[2].x;
                foodLabel.rectTransform.GetWorldCorners(corners);
                float labelLeft = corners[0].x;

                Check("a resource icon hugs its own number",
                    labelLeft - iconRight >= 0f && labelLeft - iconRight <= 16f,
                    $"gap {labelLeft - iconRight:0.#}px");
            }
        }

        Sb.AppendLine();
        Sb.AppendLine($"passed {passed}, failed {failed}");
        return Sb.ToString();
    }

    // --------------------------------------------------------------- helpers

    static float FogAlpha(RedFogOverlay fog)
    {
        // The layers are private; the painted alpha is the observable number.
        var field = typeof(RedFogOverlay).GetField("layers", Any);
        var list = field.GetValue(fog) as System.Collections.IList;
        if (list == null || list.Count == 0) return -1f;

        float max = 0f;
        foreach (var entry in list)
        {
            var image = entry.GetType().GetField("image", Any).GetValue(entry) as RawImage;
            if (image != null) max = Mathf.Max(max, image.color.a);
        }
        return max;
    }

    /// <summary>Sets the queued-round count, lets the canvas lay out, and reports where the button landed.</summary>
    static Vector2 PositionedCookPosition(SkillTriggerPanel panel, RectTransform button, int count)
    {
        panel.SetCookCount(count);
        Canvas.ForceUpdateCanvases();
        return button.anchoredPosition;
    }

    static void ResetCoreItems()
    {
        var resources = RunResources.Instance;
        if (resources == null) return;

        foreach (string name in new[] { "coreItemA", "coreItemB" })
        {
            var field = typeof(RunResources).GetField(name, Any);
            if (field != null) field.SetValue(resources, false);
        }
    }

    /// <summary>
    /// A jackpot completes the set, which ends the run: put the day back to
    /// in-progress, hide the result screen and restart the music. Without this
    /// the next suite inherits a dead jukebox and a full-screen overlay.
    /// </summary>
    static void RestoreOutcome()
    {
        var days = DayManager.Instance;
        if (days != null)
        {
            var field = typeof(DayManager).GetField("<Outcome>k__BackingField", Any);
            if (field != null) field.SetValue(days, RunOutcome.InProgress);
        }

        var result = UnityEngine.Object.FindAnyObjectByType<RunResultView>(FindObjectsInactive.Include);
        var group = result != null ? result.GetComponent<CanvasGroup>() : null;
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        AudioDirector.Instance?.BeginRun(AudioDirector.PendingRunTrack);
    }

    static string CountText(Transform row)
    {
        if (row == null) return null;
        var label = row.Find("Count");
        if (label == null) return null;

        var text = label.GetComponent<TMP_Text>();
        if (text == null || !text.gameObject.activeSelf) return "0";
        return text.text;
    }

    static void ClickCave(GatherMapController map)
    {
        var node = UnityEngine.Object.FindAnyObjectByType<MapNodeView>();
        if (node == null || map == null) return;
        node.GetComponent<Button>().onClick.Invoke();
    }

    static void ClickCard(UnitRosterView roster)
    {
        var card = roster.Cards.Count > 0 ? roster.Cards[0] : null;
        if (card == null) return;

        ExecuteEvents.Execute(card.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
    }

    static T Find<T>(Transform root, string name) where T : Component
    {
        foreach (var candidate in root.GetComponentsInChildren<T>(true))
        {
            if (candidate.name == name) return candidate;
        }
        return null;
    }

    static void Invoke(object target, string method)
    {
        var info = target.GetType().GetMethod(method, Any);
        if (info != null) info.Invoke(target, null);
    }

    static void ResetRun(UnitRosterView roster)
    {
        var resources = RunResources.Instance;
        resources.AddFood(10);
        resources.AddItem(InventoryItemType.CookingMaterial, 20);
        resources.AddItem(InventoryItemType.Materials, 20);
        resources.AddItem(InventoryItemType.Medicine, 5);

        // A fresh run opens at full risk; matching that here keeps the fog
        // checks independent of whatever an earlier pass left behind.
        resources.SetRisk(RunResources.MaxRisk);

        var days = DayManager.Instance;
        if (days != null && days.Outcome != RunOutcome.InProgress)
        {
            var field = typeof(DayManager).GetField("<Outcome>k__BackingField", Any);
            if (field != null) field.SetValue(days, RunOutcome.InProgress);
        }

        if (GatherMapController.Instance != null) GatherMapController.Instance.ClearSelection();

        foreach (var unit in UnitManager.Instance.Units)
        {
            unit.RefreshForNewDay();
            unit.state = UnitState.Idle;
        }

        SkillFlowController.Instance?.CancelActiveSkill();
        roster.ClearSelection();
        roster.SetMode(CharacterHandPanel.SelectionMode.Dispatch);
        roster.RefreshAvailability();
        RunLog.Clear();
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
