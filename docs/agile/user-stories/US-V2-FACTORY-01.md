# User Story: US-V2-FACTORY-01 - ระบบโรงงานและ UI โรงงาน (Base Work)

**Status:** ⬜ To Do
**Epic:** [Product Backlog](../01-product-backlog.md) · [V2 Task Overview](V2-TASK-OVERVIEW.md)
**Owner:** Dev A — _TBD_ (กลุ่ม 1)
**Estimate:** L

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ใช้โรงงานในฐาน (ครัว, ห้องพยาบาล, Workshop) ผ่านหน้าจอที่เห็นชัดว่าต้องจ่ายอะไรและจะได้อะไร
**เพื่อให้** ตัดสินใจได้เร็วว่าจะใช้ AP ที่เหลือของแต่ละคนผลิตอาหาร/ยา หรือรักษาเพื่อนอย่างไร

**สถานะปัจจุบัน:** ตรรกะครบแล้วใน `BaseWorkController` (ดู [GDD §5](../../gdd/01-mechanics.md#5-base-work--งานในฐาน)) แต่ตัวเลขฝังเป็น field แยกตามงาน และ UI เป็น `BaseWorkPanel` แบบ dropdown/ข้อความล้วน

---

## 📂 ไฟล์ที่ต้องรู้ก่อนเริ่ม

### ต้องอ่านให้เข้าใจ (เจ้าของ = กลุ่มนี้)
| ไฟล์ | สิ่งที่ต้องรู้ |
|---|---|
| `Assets/V2/Scripts/Controllers/BaseWorkController.cs` | ตัวเลข 3 งาน (`cookApCost`, `healApCostWithMedicine`, `workshopMaterialsCost`, …), helper สำหรับ UI (`GetMaxCookRounds`, `GetHealPlan`, `GetMaxWorkshopRounds`), รูปแบบ `CanX()` → `X()` → `BaseWorkResult` |
| `Assets/V2/Scripts/UI/BaseWorkPanel.cs` | flow ปัจจุบัน: เลือกงาน → ผู้ทำ → รอบ/ผู้ป่วย → preview → Confirm; ล็อกเมื่อ `TripInProgress`; rebuild เมื่อ `OnDayStarted` / `OnTripResolved`; ตรวจ reference 13 ช่องใน `HasRequiredReferences()` |
| `Assets/V2/Scripts/BaseWorkType.cs` | enum `Cooking / Healing / Workshop` |
| `Assets/V2/Scripts/BaseWorkResult.cs` | ผลลัพธ์ที่ส่งให้ UI (`repeatsPerformed`, `foodProduced`, `medicineProduced`, `usedMedicine`, `blocked`) |

### ต้องเรียกใช้ (ไม่ใช่เจ้าของ)
| ไฟล์ | ใช้อะไร |
|---|---|
| `Assets/V2/Scripts/System/RunResources.cs` | `Food`, `CookingMaterial`, `Medicine`, `Materials`, `TryConsumeItem`, `AddItem`, `AddFood`, enum `InventoryItemType` (**ไม่มี Food** — Food แยก field) |
| `Assets/V2/Scripts/Entities/UnitCharacter.cs` | `CanBeDispatched`, `currentActionPoints`, `GetActualApCost` (บวก starvation stack), `TrySpendActionPoints` (ตั้ง Resting เมื่อ AP = 0), `HasAptitude` / `HasNonAptitude`, `UnitState.Hurt` |
| `Assets/V2/Scripts/System/UnitManager.cs` | `Units` (ลำดับ roster) |
| `Assets/V2/Scripts/Controllers/GatherMapController.cs` | `TripInProgress`, `OnTripResolved` |
| `Assets/V2/Scripts/Controllers/DayManager.cs` | `OnDayStarted` |
| `Assets/V2/Scripts/Entities/GatherZoneData.cs` | struct `LootAmount` (ใช้ซ้ำในสูตรได้) |

### Scene / Prefab
| ไฟล์ | ส่วนที่เกี่ยว |
|---|---|
| `Assets/V2/Scenes/Core.unity` | `GameSystem/BaseworkController` (ค่าใน Inspector), `Canvas/BaseWorkPanel` ทั้ง subtree (JobRow, WorkerDropdown, PatientRoot, RoundsRoot, PreviewText, ConfirmButton, MessageText) |
| `Assets/V2/Prefabs/Factory/` (สร้างใหม่) | prefab ของ panel โรงงาน |

### เอกสาร
- [GDD §5 Base Work](../../gdd/01-mechanics.md#5-base-work--งานในฐาน), [§9 Tunable Values](../../gdd/01-mechanics.md#9-tunable-values--ตัวเลขที่ปรับได้)
- [System Design — BaseWorkController, §4.2](../../software/04-unity-system-design.md)
- [Scene Hierarchy §3.3 BaseWorkPanel](../../software/06-unity-core-scene-hierarchy.md#33-การอ้างอิงใน-inspector-ของ-ui-script)

---

## 🛠 Technical Tasks

### F1 — Contract C1: event ผลงานโรงงาน (S) — ทำก่อนอย่างอื่น
- [ ] `BaseWorkController.cs` — เพิ่ม `public event Action<BaseWorkResult> OnBaseWorkCompleted` ยิงท้าย `Cook`, `Heal`, `UseWorkshop` ทุกกรณี (รวม `blocked`)
- [ ] แจ้งกลุ่ม 2 ว่าพร้อมใช้ (สำหรับเสียง/effect)

### F2 — สูตรการผลิตเป็นข้อมูล (M)
- [ ] สร้าง `Assets/V2/Scripts/Factory/FacilityRecipe.cs` (ScriptableObject) — `BaseWorkType type`, `int apCost`, `List<LootAmount> inputs`, ผลผลิต, `AptitudeTag? aptitude`, ผลผลิตเมื่อถนัด, `bool repeatable`
- [ ] สร้าง asset 2 ตัวใน `Assets/V2/Data/Factory/`: `Recipe_Cook`, `Recipe_Workshop` ค่าตรงกับปัจจุบัน (Cook 1 AP + 1 CM → 1/2 Food; Workshop 1 AP + 2 Materials → 1 Medicine)
- [ ] `BaseWorkController.cs` — ให้ Cook / Workshop อ่านค่าจาก recipe แทน `[SerializeField]` แยกตัว; Heal คงเป็นกรณีพิเศษ (มีผู้ป่วย + ยาเป็น optional)
- [ ] **ตัดสินใจก่อนเขียน:** ผลผลิตเป็น Food ซึ่งไม่อยู่ใน `InventoryItemType` → (ก) เพิ่ม `Food` เข้า enum แล้วให้ `RunResources.AddItem` รองรับ (ต้องแจ้งกลุ่ม 3 เพราะแตะ `RunResources`) หรือ (ข) ให้ recipe มี field `foodOutput` แยก — แนะนำ (ข) เพื่อไม่แตะระบบกลาง

### F3 — Factory Panel UI (L)
- [ ] สร้าง `Assets/V2/Prefabs/Factory/FactoryPanel.prefab` แทน subtree `BaseWorkPanel` เดิม
- [ ] การ์ดโรงงาน 3 ใบ (ครัว / ห้องพยาบาล / Workshop) — แต่ละใบแสดง **ต้นทุน → ผลผลิต** พร้อมจำนวนในคลังตอนนี้ (เช่น `Cooking Material 2/1`)
- [ ] เลือกผู้ทำเป็นปุ่มยูนิต (แทน dropdown) แสดง AP คงเหลือ; ยูนิตที่ทำไม่ได้เป็นสีเทาพร้อมเหตุผล; ไฮไลต์ถนัด/ไม่ถนัด **ตาม aptitude ของ recipe** (ไม่ฝัง Cooking ไว้แบบตอนนี้)
- [ ] Stepper จำนวนรอบ จำกัดด้วย `GetMax*Rounds` + ปุ่ม "Max"
- [ ] ห้องพยาบาล: แสดงเฉพาะยูนิต Hurt (ไม่รวมผู้ทำ), บอกชัดว่าจะใช้ยาหรือไม่ (`GetHealPlan`)
- [ ] รักษาพฤติกรรมเดิม: ล็อก Confirm ระหว่าง `TripInProgress`, rebuild เมื่อ `OnDayStarted` / `OnTripResolved`, ไม่ reset dropdown ทุกเฟรม
- [ ] ย้ายข้อความ/ตัวเลขทั้งหมดให้มาจาก `BaseWorkController` / recipe (ไม่มีเลขฝังใน UI)
- [ ] เอา `BaseWorkPanel` เดิมออกจาก `Core.unity` (หรือปิดไว้) หลัง prefab ใหม่ใช้ได้ — **แจ้งก่อนแก้ scene** ตามกติกา [Overview §4](V2-TASK-OVERVIEW.md#4-กติกาการทำงานร่วมกันใน-coreunity)

### F4 — แก้ edge case ที่พบในโค้ด (S)
- [ ] `Heal()` ตั้งผู้ป่วยเป็น `Idle` เสมอ แม้ AP = 0 → ควรเป็น `Resting` ถ้า `currentActionPoints == 0` (ไม่งั้นยูนิตจะดูเหมือน Ready แต่ทำอะไรไม่ได้)
- [ ] `Cook()` ไม่ตั้ง `blocked = true` เมื่อทำได้ 0 รอบ (ต่างจาก `UseWorkshop`) → ทำให้สอดคล้องกัน
- [ ] ทดสอบด้วยยูนิตที่มี starvation stack 1–2 (ต้นทุน AP ต่อรอบต้องเพิ่มตาม `GetActualApCost`)

---

## ✅ Acceptance Criteria
1. [ ] ทำ Cook / Heal / Craft ผ่าน `FactoryPanel` ได้ครบ ผลลัพธ์ตรงกับ [GDD §5](../../gdd/01-mechanics.md#5-base-work--งานในฐาน)
2. [ ] เปลี่ยนตัวเลขใน recipe asset แล้ว UI และผลลัพธ์เปลี่ยนตาม โดยไม่แก้โค้ด
3. [ ] ผู้เล่นเห็นต้นทุน + ผลผลิต + จำนวนรอบสูงสุดก่อนกด Confirm และเห็นเหตุผลเมื่อกดไม่ได้
4. [ ] `OnBaseWorkCompleted` ยิงทุกครั้ง และกลุ่ม 2 ต่อเสียง/effect ได้
5. [ ] ระหว่างทริปสำรวจ กด Confirm ไม่ได้
6. [ ] ยูนิตหิว 1 stack ทำอาหาร 1 รอบเสีย 2 AP

---

## 🔗 Coordination
- **ให้กลุ่ม 2:** C1 `OnBaseWorkCompleted`
- **ใช้จากกลุ่ม 3:** C7 `RunResources.OnChanged` (ถ้าต้องการ refresh จำนวนในคลังแบบ event)
- **ห้ามแก้:** `DayManager`, `GatherMapController`, object อื่นใน `Canvas`

## 📝 Docs to Update
- [gdd/01-mechanics.md §5, §9](../../gdd/01-mechanics.md) · [software/04 BaseWorkController, §4.2](../../software/04-unity-system-design.md) · [software/05](../../software/05-unity-class-diagram.md) (FacilityRecipe) · [software/06](../../software/06-unity-core-scene-hierarchy.md) (BaseWorkPanel → FactoryPanel)

## 🔗 Related Files
- [V2 Task Overview](V2-TASK-OVERVIEW.md)
- [US-V2-SCENE-01](US-V2-SCENE-01.md) · [US-V2-UI-01](US-V2-UI-01.md)
