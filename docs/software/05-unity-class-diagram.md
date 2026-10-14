# clogged (Unity V2) — Class Diagram

**Version:** 2.0 | **Last Updated:** 2026-10-14
> สร้างจากโค้ดจริงใน `Unity-Projects/Assets/V2/Scripts/` — ภาพรวมระบบและ flow ดูที่ [Unity V2 System Design](04-unity-system-design.md)

## 1. Layer Overview

```mermaid
flowchart TD
    subgraph UI["UI/ — แสดงผล + input"]
        SBV[StatusBarView]
        URV[UnitRosterView] --> UIV[UnitIconView]
        GDP[GatherDispatchPanel]
        BWP[BaseWorkPanel]
        MNV[MapNodeView]
        SWV[ScoutWalkerView]
    end

    subgraph CTRL["Controllers/ — กฎของเกม"]
        DM[DayManager]
        GMC[GatherMapController]
        GDC[GatherDispatchController]
        BWC[BaseWorkController]
        STC[StarvationController]
    end

    subgraph SYS["System/ — state กลาง + คำนวณ"]
        UM[UnitManager]
        RR[RunResources]
        GZD[GatherZoneDatabase]
        GR["GatherResolver «static»"]
        TL["TraitLibrary «static»"]
    end

    subgraph ENT["Entities/ — ข้อมูล"]
        UC[UnitCharacter]
        GZC[GatherZoneConfig]
        MND[MapNodeData]
    end

    GDP --> GMC
    GDP --> GDC
    BWP --> BWC
    MNV -. OnAnyNodeClicked .-> GMC
    GMC --> SWV
    GMC --> MNV
    GMC --> GDC
    GDC --> GR
    GDC --> TL
    GDC --> GZD
    GDC --> RR
    GDC --> DM
    BWC --> RR
    STC --> RR
    DM --> UM
    DM --> RR
    SBV --> DM
    SBV --> RR
    URV --> UM
    UM --> UC
    GZD --> GZC
    GMC --> MND
```

## 2. Entities & Data Types

```mermaid
classDiagram
    class UnitCharacter {
        <<Serializable>>
        +const int MaxStarvationStacks = 3
        +string unitName
        +UnitState state
        +int maxActionPoints = 3
        +int currentActionPoints
        +List~AptitudeTag~ aptitudes
        +List~AptitudeTag~ nonAptitudes
        +List~UnitTrait~ traits
        +int starvationStacks
        +bool HasGatheredToday
        +bool HasBeenFedManuallyToday
        +bool IsAlive
        +bool CanBeDispatched
        +bool CanBeSentGathering
        +GetActualApCost(baseCost) int
        +TrySpendActionPoints(baseCost) bool
        +RefreshForNewDay() void
        +ApplyStarvationStack() void
        +ReduceStarvationStack() void
        +MarkGatheredToday() void
        +MarkFedManuallyToday() void
        +HasAptitude(tag) bool
        +HasNonAptitude(tag) bool
    }

    class UnitTrait {
        <<Serializable>>
        +string traitName
        +TraitPolarity polarity
    }

    class UnitState {
        <<enum>>
        Idle
        Resting
        Hurt
        Away
        Missing
    }

    class AptitudeTag {
        <<enum>>
        Easy
        Normal
        Hard
        Cooking
        Combat
        Gathering
        Speed
        Endurance
    }

    class TraitPolarity {
        <<enum>>
        Positive
        Negative
    }

    class GatherZoneConfig {
        <<Serializable>>
        +GatherZone zone
        +int apCost
        +int baseSuccessValue
        +LootAmount mainLoot
        +List~LootAmount~ hardSuccessBonusPool
        +AptitudeTag ZoneTag
    }

    class LootAmount {
        <<struct>>
        +InventoryItemType type
        +int amount
    }

    class GatherZone {
        <<enum>>
        Easy
        Normal
        Hard
    }

    class InventoryItemType {
        <<enum>>
        CookingMaterial
        Medicine
        Materials
    }

    class MapNodeData {
        <<Serializable>>
        +string nodeId
        +GatherZone zone
        +Vector2 anchoredPosition
    }

    UnitCharacter --> UnitState
    UnitCharacter "1" *-- "*" UnitTrait
    UnitCharacter ..> AptitudeTag
    UnitTrait --> TraitPolarity
    GatherZoneConfig --> GatherZone
    GatherZoneConfig "1" *-- "1..*" LootAmount
    GatherZoneConfig ..> AptitudeTag : ZoneTag
    LootAmount --> InventoryItemType
    MapNodeData --> GatherZone
```

## 3. Systems & Controllers

```mermaid
classDiagram
    class UnitManager {
        <<MonoBehaviour, Singleton>>
        -List~UnitCharacter~ units
        +IReadOnlyList~UnitCharacter~ Units
        +AliveCount() int
        +AllUnitsLost() bool
        -SetupDefaultRoster() void
    }

    class RunResources {
        <<MonoBehaviour, Singleton>>
        -int food = 8
        -int cookingMaterial = 2
        -int medicine = 1
        -int materials = 0
        -bool hasCoreItemUnlock
        -bool hasCoreItem
        +TryConsumeFood() bool
        +AddFood(amount) void
        +AddItem(type, amount) void
        +TryConsumeItem(type, amount) bool
        +GetItemAmount(type) int
        +UnlockCoreItemMaterial() void
        +ObtainCoreItem() void
    }

    class GatherZoneDatabase {
        <<MonoBehaviour, Singleton>>
        -List~GatherZoneConfig~ zones
        +GetConfig(zone) GatherZoneConfig
        -SetupDefaults() void
    }

    class GatherResolver {
        <<static>>
        +ComputeSuccessValue(config, sentUnits) int
        +Roll(successValue) GatherOutcome
    }

    class TraitLibrary {
        <<static>>
        +CreateRandom(polarity, owner) UnitTrait
    }

    class GatherOutcome {
        +GatherResultTier tier
        +int successValue
        +int rollValue
    }

    class GatherResultTier {
        <<enum>>
        HardSuccess
        Regular
        Failure
        Fumble
    }

    class DayManager {
        <<MonoBehaviour, Singleton>>
        -int currentDay = 1
        -int deadlineDay = 7
        +RunOutcome Outcome
        +event OnDayStarted(int)
        +event OnRunEnded(RunOutcome)
        +EndDay() void
        +CheckImmediateWin() void
        -FeedEligibleUnitsInOrder() void
        -RefreshEligibleUnitsForNewDay() void
    }

    class RunOutcome {
        <<enum>>
        InProgress
        Win
        LoseDeadline
        LoseAllUnitsLost
    }

    class GatherDispatchController {
        <<MonoBehaviour, Singleton>>
        +event OnGatherResolved(GatherDispatchResult)
        +CanDispatch(zone, units, out reason) bool
        +Dispatch(zone, units) GatherDispatchResult
        -ApplyOutcome(...) GatherDispatchResult
        -GrantMainLoot / GrantBonusLoot()
        -MaybeAwardTrait / InjureRandomUnit / LoseRandomUnit()
        -HandleCoreItemProgress(zone, result) void
    }

    class GatherDispatchResult {
        +GatherZone zone
        +List~UnitCharacter~ sentUnits
        +GatherResultTier tier
        +int rollValue
        +int successValue
        +LootAmount? mainLootGranted
        +LootAmount? bonusLootGranted
        +UnitCharacter hurtUnit
        +UnitCharacter missingUnit
        +bool traitAwarded
        +string traitName
        +UnitCharacter traitRecipient
        +bool unlockedCoreItemMaterial
        +bool obtainedCoreItem
    }

    class GatherMapController {
        <<MonoBehaviour, Singleton>>
        -List~MapNodeData~ nodeData
        -List~MapNodeView~ nodeViews
        +MapNodeView SelectedNode
        +bool TripInProgress
        +event OnTripResolved(GatherDispatchResult)
        +ConfirmTrip(units) void
        -SetupDefaultNodes() void
        -FindScatterPosition(min, max) Vector2
        -ReturnToBase(result) void
    }

    class BaseWorkController {
        <<MonoBehaviour, Singleton>>
        +CanCook(worker) bool
        +Cook(worker, rounds) BaseWorkResult
        +CanHeal(healer, patient) bool
        +Heal(healer, patient) BaseWorkResult
        +CanUseWorkshop(worker) bool
        +UseWorkshop(worker, rounds) BaseWorkResult
        +GetMaxCookRounds(worker) int
        +GetMaxWorkshopRounds(worker) int
        +GetHealPlan(healer, out useMedicine, out apCost) void
    }

    class BaseWorkResult {
        +BaseWorkType workType
        +UnitCharacter worker
        +bool blocked
        +int repeatsPerformed
        +int foodProduced
        +int cookingMaterialConsumed
        +UnitCharacter healedUnit
        +bool usedMedicine
        +int medicineProduced
    }

    class BaseWorkType {
        <<enum>>
        Cooking
        Healing
        Workshop
    }

    class StarvationController {
        <<MonoBehaviour, Singleton>>
        -int foodCostPerFeed = 1
        +GetBlockReason(unit) string
        +CanFeedUnit(unit) bool
        +FeedUnit(unit) FeedResult
    }

    class FeedResult {
        +UnitCharacter unit
        +bool blocked
        +int stacksBefore
        +int stacksAfter
    }

    UnitManager "1" o-- "4" UnitCharacter
    GatherZoneDatabase "1" o-- "3" GatherZoneConfig
    GatherResolver ..> GatherOutcome : creates
    GatherOutcome --> GatherResultTier
    DayManager --> RunOutcome
    DayManager ..> UnitManager
    DayManager ..> RunResources
    GatherDispatchController ..> GatherZoneDatabase
    GatherDispatchController ..> GatherResolver
    GatherDispatchController ..> TraitLibrary
    GatherDispatchController ..> RunResources
    GatherDispatchController ..> DayManager : CheckImmediateWin
    GatherDispatchController ..> GatherDispatchResult : creates
    GatherMapController "1" o-- "*" MapNodeData
    GatherMapController ..> GatherDispatchController
    BaseWorkController ..> RunResources
    BaseWorkController ..> BaseWorkResult : creates
    BaseWorkResult --> BaseWorkType
    StarvationController ..> RunResources
    StarvationController ..> FeedResult : creates
```

## 4. UI Classes

```mermaid
classDiagram
    class StatusBarView {
        <<MonoBehaviour>>
        -TMP_Text dayText, foodText, cookingMaterialText
        -TMP_Text medicineText, materialsText, coreItemStatusText
        -Update() poll DayManager + RunResources
    }

    class UnitRosterView {
        <<MonoBehaviour>>
        -RectTransform container
        -UnitIconView iconPrefab
        -Update() refresh ทุก icon
    }

    class UnitIconView {
        <<MonoBehaviour>>
        +Bind(unit) void
        +Refresh() void
    }

    class GatherDispatchPanel {
        <<MonoBehaviour>>
        -Button dispatchButton
        -Toggle[] unitToggles
        -TMP_Text messageText
        -OnDispatchClicked() void
        -HandleTripResolved(result) void
        -RefreshToggles() void
    }

    class MapNodeView {
        <<MonoBehaviour>>
        +MapNodeData Data
        +static event OnAnyNodeClicked(MapNodeView)
        +Initialize(data) void
        +SetSelected(bool) void
    }

    class ScoutWalkerView {
        <<MonoBehaviour>>
        -float moveDuration = 0.6
        +MoveTo(pos, onArrived) void
        +SnapTo(pos) void
    }

    class BaseWorkPanel {
        <<MonoBehaviour>>
        -BaseWorkType? job
        -int rounds
        -TMP_Dropdown workerDropdown, patientDropdown
        -SelectJob(type) void
        -RefreshPreview() void
        -OnConfirm() void
    }

    class DiceRollView {
        <<stub>>
    }

    UnitRosterView "1" *-- "*" UnitIconView : spawns
    UnitIconView ..> UnitCharacter
    StatusBarView ..> DayManager
    StatusBarView ..> RunResources
    GatherDispatchPanel ..> GatherMapController : ConfirmTrip / OnTripResolved
    GatherDispatchPanel ..> GatherDispatchController : CanDispatch
    GatherDispatchPanel ..> DayManager : OnDayStarted
    BaseWorkPanel ..> BaseWorkController
    BaseWorkPanel ..> GatherMapController : TripInProgress / OnTripResolved
    BaseWorkPanel ..> DayManager : OnDayStarted
    GatherMapController "1" o-- "*" MapNodeView : spawns
    GatherMapController --> ScoutWalkerView
```

## Notes

- `UnitCharacter`, `GatherZoneConfig`, `MapNodeData` เป็น plain `[Serializable]` class (ไม่ใช่ MonoBehaviour / ScriptableObject) — แก้ค่าได้ใน Inspector ของ `UnitManager` / `GatherZoneDatabase` ถ้า list ว่างจะใช้ค่า default ในโค้ด
- ระบบทั้งหมดสื่อสารผ่าน **singleton `Instance`** + **C# event** — ไม่มี dependency injection หรือ ScriptableObject event channel
- `GatherResolver` เป็น pure function (ไม่มี side effect) — จุดที่ทำ unit test ได้ง่ายที่สุดในโปรเจกต์
- `*Result` class (`GatherDispatchResult`, `BaseWorkResult`, `FeedResult`) เป็นรายงานสำหรับ UI เท่านั้น ไม่มี logic

## Related Documents
- [Unity V2 System Design](04-unity-system-design.md)
- [Web Prototype Class Diagram](02-class-diagram.md) (ระยะที่ 1)
