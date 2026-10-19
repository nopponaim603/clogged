# User Story: US-V2-UI-01 - ระบบการแสดง UI หลักของเกม

**Status:** ⬜ To Do
**Epic:** [Product Backlog](../01-product-backlog.md) · [V2 Task Overview](V2-TASK-OVERVIEW.md)
**Owner:** Dev C — _TBD_ (กลุ่ม 3)
**Estimate:** L

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** เห็นสถานะของทีม ทรัพยากร วันที่เหลือ และโอกาสสำเร็จก่อนส่งสำรวจ พร้อมปุ่มหลักที่ป้องกันการกดพลาด
**เพื่อให้** วางแผนแต่ละวันได้จากหน้าจอเดียว และรู้ชัดว่า run จบด้วยผลอะไร

**สถานะปัจจุบัน:**
- `StatusBarView` แสดงข้อความล้วน poll ทุกเฟรม; `UnitRosterView` spawn **การ์ดยูนิต** (195×265, กรอบสีประจำตัว แดง/เหลือง/เขียว/น้ำเงิน) ที่คลิกเลือกได้เอง แทน toggle เดิม (ลบออกจาก scene แล้ว)
- `GatherDispatchPanel` อยู่บน GameObject ชื่อ `TestDispatch` นอก Canvas, อ่านยูนิตที่เลือกจาก `UnitRosterView` (สูงสุด 2), ผลทริปเป็นข้อความหลายบรรทัดใน `MessageUi` (ย้ายมาอยู่ข้างการ์ด), ข้อผิดพลาด/เกินขีดจำกัดแสดงผ่าน `WarningView`
- `Dispatch` / `EndDay` ถูกย้ายไปมุมขวาล่างชั่วคราว และมี `ButtonVisual` เตรียมไว้สลับข้อความ ↔ รูป icon
- `StarvationController` (ให้อาหารลด stack) **ไม่มี UI**
- ปุ่ม End Day **ไม่ล็อก** ระหว่างทริป และไม่ปิดเมื่อ run จบ; `DayManager.OnRunEnded` **ไม่มีผู้ฟัง** → ไม่มีหน้าจอจบเกม

---

## 📂 ไฟล์ที่ต้องรู้ก่อนเริ่ม

### ต้องอ่านให้เข้าใจ (เจ้าของ = กลุ่มนี้)
| ไฟล์ | สิ่งที่ต้องรู้ |
|---|---|
| `Assets/V2/Scripts/UI/StatusBarView.cs` | ข้อความวัน/deadline, Food เทียบ "need tonight" (`CountMouthsTonight`), สถานะ Core Item 3 ระดับ |
| `Assets/V2/Scripts/UI/UnitRosterView.cs`, `UI/UnitIconView.cs` | spawn การ์ดตาม roster, `Bind` / `Refresh`, alpha ตามสถานะ, `Hungry xN` |
| `Assets/V2/Scripts/UI/GatherDispatchPanel.cs` | toggle ≤ 2 คน, `CanDispatch` แสดงเหตุผล, `ConfirmTrip`, จัดรูปผลใน `HandleTripResolved` (trait ใหม่เช็กจาก `traitsBefore`) |
| `Assets/V2/Scripts/Controllers/StarvationController.cs` | `GetBlockReason` (ไม่หิว / ให้แล้ววันนี้ / Food ไม่พอ), `FeedUnit` → `FeedResult` |
| `Assets/V2/Scripts/Controllers/DayManager.cs` | **เจ้าของ** — ลำดับ `EndDay()` (เช็ก Core Item → กินตามลำดับ roster → วัน+1 → เช็กแพ้ → refresh), `CheckImmediateWin`, `Outcome`, `OnDayStarted`, `OnRunEnded` |

### ต้องเรียกใช้ (ไม่ใช่เจ้าของ)
| ไฟล์ | ใช้อะไร |
|---|---|
| `Assets/V2/Scripts/Entities/UnitCharacter.cs` | `state`, AP, `aptitudes` / `nonAptitudes`, `traits`, `starvationStacks`, `CanBeSentGathering`, `HasBeenFedManuallyToday` |
| `Assets/V2/Scripts/System/UnitManager.cs` | `Units` |
| `Assets/V2/Scripts/System/RunResources.cs` | ค่าทรัพยากร, `HasCoreItemUnlock` / `HasCoreItem` — (เพิ่ม C7 `OnChanged`) |
| `Assets/V2/Scripts/Controllers/GatherDispatchController.cs` | `CanDispatch`, `GatherDispatchResult` |
| `Assets/V2/Scripts/System/GatherResolver.cs` | `ComputeSuccessValue(config, units)` (static, ไม่มี side effect) — ใช้คำนวณ odds preview |
| `Assets/V2/Scripts/System/GatherZoneDatabase.cs`, `Entities/GatherZoneData.cs` | `GetConfig(zone)` → AP cost, base SV, loot |
| `Assets/V2/Scripts/Controllers/GatherMapController.cs` | `SelectedNode`, `TripInProgress`, `ConfirmTrip`, `OnTripResolved`, (ใหม่) C2 `OnNodeSelected` |
| `Assets/V2/Scripts/Scene/SceneFlowController.cs` (กลุ่ม 2 สร้าง) | C5 `RestartRun()` |

### Scene / Prefab
| ไฟล์ | ส่วนที่เกี่ยว |
|---|---|
| `Assets/V2/Scenes/Core.unity` | `Canvas/StatusBar`, `Canvas/CharCard`, `Canvas/EndDay button` (onClick → `DayManager.EndDay`), `Canvas/MapPanel/{Ann,Boon,Cia,Dan,Dispatch,MessageUi}`, root `TestDispatch` |
| `Assets/V2/Prefabs/ScoutIcon.prefab` | ตอนนี้ใช้เป็นการ์ดยูนิตด้วย → แยกเป็น `UnitCard.prefab` (C6) |
| `Assets/V2/Prefabs/UI/` (สร้างใหม่) | UnitCard, DispatchPanel, GatherResultPanel, EndDayConfirm, RunEndPanel |

### เอกสาร
- [GDD §2 Unit, §3 Day, §4.3–4.5 odds, §6 Starvation, §7 Core Item](../../gdd/01-mechanics.md)
- [Art Direction §2 Color, §4 Layout](../../gdd/03-art-direction.md)
- [System Design — UI Layer, §4.3, §6 Known Gaps](../../software/04-unity-system-design.md)
- [Scene Hierarchy §3.2–3.3](../../software/06-unity-core-scene-hierarchy.md)

---

## 🛠 Technical Tasks

### U0 — Contracts C4, C7 (S) — ทำก่อน
- [ ] `DayManager.cs` — เพิ่ม class `DayEndReport` (dayBefore, dayAfter, `fed`, `starved`, `lostSignal`), `PreviewFeeding()` (จำลองการกินโดยไม่แก้ state) และ `event Action<DayEndReport> OnDayEnded` ยิงหลังกินอาหารใน `EndDay()`
- [ ] `RunResources.cs` — เพิ่ม `event Action OnChanged` ยิงใน `AddFood`, `AddItem`, `TryConsumeFood`, `UnlockCoreItemMaterial`, `ObtainCoreItem` (แจ้งกลุ่ม 1 ว่าแตะไฟล์นี้)

### U1 — การ์ดยูนิต + ปุ่มให้อาหาร (M)
- [ ] สร้าง `Prefabs/UI/UnitCard.prefab` (C6) ใช้ `UnitIconView` ที่ขยายแล้ว: ชื่อ/portrait, AP เป็นจุด (pips), สถานะ, Hunger stack, trait (ดี/เสีย), aptitude (tooltip), ป้าย "ออกสำรวจแล้ววันนี้"
- [ ] `UnitRosterView` — ใช้ `UnitCard.prefab` แทน `ScoutIcon.prefab`; เปลี่ยนจาก poll ทุกเฟรมเป็น refresh ตาม event (`OnDayStarted`, `OnTripResolved`, C1, C7) ถ้าทำได้
- [ ] ปุ่ม **Feed** บนการ์ด เรียก `StarvationController.FeedUnit`; ปิดปุ่มพร้อม tooltip จาก `GetBlockReason`

### U2 — Panel ส่งสำรวจ + odds preview (M)
- [ ] สร้าง `Prefabs/UI/DispatchPanel.prefab` วางใต้ `Canvas` แทน `TestDispatch` + toggle 4 อันที่ผูกด้วย index — สร้างรายการยูนิตจาก `UnitManager.Units` ตอน runtime
- [ ] เมื่อเลือกถ้ำ (C2 `OnNodeSelected`) แสดงระดับ, AP ต่อคน, ของหลัก, bonus pool
- [ ] เมื่อเลือกยูนิต แสดง **Success Value และ % ของ 4 tier** (คำนวณด้วย `GatherResolver.ComputeSuccessValue` + กติกาใน [GDD §4.4](../../gdd/01-mechanics.md#44-การทอย-1d100-roll-under-และระดับผล)) พร้อมเตือนเมื่อ SV < 50 ที่ถ้ำ Hard (Fumble 5% = เสี่ยงเสียยูนิต)
- [ ] คงพฤติกรรมเดิม: สูงสุด 2 คน, แสดงเหตุผลจาก `CanDispatch`, ปิดปุ่มระหว่างทริป
- [ ] ลบ `TestDispatch` และ toggle เดิมออกจาก scene (แจ้งก่อนแก้ scene)

### U3 — ผลการสำรวจ, End Day, จบเกม (L)
- [ ] `Prefabs/UI/GatherResultPanel.prefab` — เปิดหลัง `OnTripResolved` (**ไม่ใช่** `OnGatherResolved` — ดู C3) แสดง tier, roll/SV, ของที่ได้, สถานะแต่ละคน, trait ใหม่, ความคืบหน้า Core Item แทนข้อความใน `MessageUi`
- [ ] ปุ่ม End Day — ล็อกเมื่อ `GatherMapController.TripInProgress` หรือ `DayManager.Outcome != InProgress` (แก้ known gap ที่กดระหว่างทริปได้)
- [ ] Dialog ยืนยัน End Day จาก `PreviewFeeding()`: "Food 3 / ต้องใช้ 4 — Dan จะหิว (stack 2 → 3 = Lost Signal)"
- [ ] `Prefabs/UI/RunEndPanel.prefab` subscribe `OnRunEnded` — Win / หมดเวลา / ทีมหายหมด + สรุป (วันที่จบ, ใครรอด, trait) + ปุ่ม Restart → C5 `SceneFlowController.RestartRun()`
- [ ] `StatusBarView` — ไอคอนทรัพยากร, แถบ Core Item 3 ขั้น, ใช้ C7 แทน poll

---

## ✅ Acceptance Criteria
1. [ ] การ์ดยูนิตแสดง AP, สถานะ, ความหิว, trait ครบ และให้อาหารลด stack ได้จากการ์ด (1 Food, 1 ครั้ง/คน/วัน)
2. [ ] ก่อนกด Dispatch ผู้เล่นเห็น % ของทั้ง 4 tier ซึ่งตรงกับตารางใน [GDD §4.5](../../gdd/01-mechanics.md#45-ตารางโอกาสตัวอย่าง-ทีมยังไม่มี-trait)
3. [ ] เปลี่ยนลำดับ roster แล้ว panel ส่งสำรวจยังเลือกยูนิตถูกคน (ไม่พึ่ง index)
4. [ ] กด End Day ระหว่างทริป หรือหลัง run จบไม่ได้; มี dialog เตือนเมื่ออาหารไม่พอ
5. [ ] จบ run ทุกแบบแสดง `RunEndPanel` และ Restart ได้
6. [ ] ไม่มี UI ใด poll `Update()` เพื่ออ่านค่าที่มี event ให้แล้ว (ยกเว้นที่ระบุเหตุผล)

---

## 🔗 Coordination
- **ให้กลุ่ม 2:** C4 `OnDayEnded(DayEndReport)` · C6 `UnitCard.prefab`
- **ให้กลุ่ม 1:** C7 `RunResources.OnChanged`
- **ใช้จากกลุ่ม 2:** C2 `OnNodeSelected`, C3 ลำดับทริป, C5 `SceneFlowController`
- **ใช้จากกลุ่ม 1:** C1 `OnBaseWorkCompleted` (refresh การ์ด)
- **ห้ามแก้:** `GatherMapController`, `BaseWorkController`, subtree `BaseWorkPanel`, `MapPanel` (ยกเว้นลบ toggle/Dispatch/MessageUi ของกลุ่มนี้)

## 📝 Docs to Update
- [gdd/01-mechanics.md §6](../../gdd/01-mechanics.md#6-starvation--ความหิว) (ปุ่มให้อาหารมี UI แล้ว), [§10](../../gdd/01-mechanics.md) · [gdd/03-art-direction.md §4](../../gdd/03-art-direction.md)
- [software/04 UI Layer, §4.3, §5, §6](../../software/04-unity-system-design.md) · [software/05](../../software/05-unity-class-diagram.md) · [software/06](../../software/06-unity-core-scene-hierarchy.md) (Canvas, ลบ TestDispatch)

## 🔗 Related Files
- [V2 Task Overview](V2-TASK-OVERVIEW.md)
- [US-V2-FACTORY-01](US-V2-FACTORY-01.md) · [US-V2-SCENE-01](US-V2-SCENE-01.md)
