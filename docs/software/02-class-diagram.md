# clogged — Class Diagram

**Version:** 1.0 | **Last Updated:** 2026-07-13
> ครอบคลุม `entities/` และ `systems/` เท่านั้น (ไม่รวม Phaser scene/UI classes ซึ่งเป็น glue code) — สร้างจากโค้ดจริงใน `prototype_resource_game/src/`

```mermaid
classDiagram
    class Crew {
        +number id
        +string name
        +number hp
        +number maxHp
        +number speed
        +number gatheringProficiency
        +number searchingProficiency
        +number huntingProficiency
        +string[] perks
        +number hireCost
        +boolean isBusy
        +boolean isAlive
        +Equipment equipment
        +getEffectiveGathering() number
        +getEffectiveSearching() number
        +getEffectiveHunting() number
        +getEffectiveSpeed() number
        +hasPerk(id) boolean
        +calculateTravelTime(distance) number
        +takeDamage(dmg) boolean
    }

    class Base {
        +number hp
        +number maxHp
        +takeDamage(dmg) boolean
        +isDestroyed() boolean
        +getHpPercentage() number
    }

    class Monster {
        +string id
        +string name
        +number hp
        +number damage
        +number difficulty
        +takeDamage(dmg) boolean
        +isDead() boolean
    }

    class ResourceNode {
        +string id
        +string type
        +number amount
        +boolean isRelic
        +boolean isMonster
        +number difficulty
        +getActionTime(proficiency) number
    }

    class CrewManager {
        +Crew[] crews
        +Crew[] availableCrews
        +Crew[] busyCrews
        +generateRandomCrew(cost) Crew
        +hireCrew(crew, points) Result
        +assignMission(crewId, target) Result
        +completeMission(crewId) void
        +getAllAlive() Crew[]
    }

    class ResourceManager {
        +ResourceInventory resources
        +number baseHP
        +addResource(type, amount) void
        +addMonsterPart(type, amount) void
        +consumeFood(crewCount) Result
        +loseDayResources() Record
        +takeBaseDamage(dmg) boolean
    }

    class MissionSystem {
        -ResourceManager resourceManager
        +executeMission(crew, target) MissionResult
        +executeCollaborativeMission(crews, target, travelTime) MissionResult
        -executeGathering(crew, target, ...) MissionResult
        -executeRelicSearch(crew, target, ...) MissionResult
        -executeMonsterHunt(crew, target, ...) MissionResult
    }

    class TimeSystem {
        +number day
        +number dayTimeLimit
        +boolean isPlanningPhase
        +boolean isExecuting
        +boolean isNightPhase
        +startDay(scene) void
        +startExecution(scene) void
        +startNight(scene) void
        +endDay() void
        +endNight() void
    }

    class MapGenerator {
        +ResourceNode[] resourceNodes
        +generateResources() ResourceNode[]
        +getBasePosition() Position
    }

    CrewManager "1" o-- "*" Crew : manages
    MissionSystem ..> Crew : uses
    MissionSystem ..> ResourceNode : targets
    MissionSystem --> ResourceManager : writes results to
    MapGenerator "1" o-- "*" ResourceNode : generates
    ResourceManager --> Base : baseHP mirrors
```

## Notes

- `MissionSystem` ไม่ hold state ของ `Crew`/`ResourceNode` เอง — รับเป็น parameter ทุกครั้ง (stateless orchestrator)
- `Base.hp` และ `ResourceManager.baseHP` เป็นสอง representation ของค่าเดียวกันในโค้ดปัจจุบัน (`Base` entity ถูกสร้างแต่ scene ใช้ `resourceManager.baseHP` เป็นค่าจริงที่แสดงผล) — ควร unify เป็นแหล่งเดียวเมื่อ refactor เฟส 2 เพื่อลดความเสี่ยง state ไม่ตรงกัน
- `Equipment` (weapon/armor/accessory) นิยามไว้ใน `Crew.ts` แต่ยังไม่มี system ใดสร้าง/มอบ equipment ให้ลูกเรือ — เป็น stub รอฟีเจอร์ในอนาคต

## Related Documents
- [System Design](01-system-design.md)
- [Core Mechanics](../gdd/01-mechanics.md)
