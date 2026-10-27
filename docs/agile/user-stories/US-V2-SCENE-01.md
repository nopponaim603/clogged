# User Story: US-V2-SCENE-01 - ระบบฉากและ Feedback ภายในเกม

**Status:** 🔵 In Progress
**Epic:** [Product Backlog](../01-product-backlog.md) · [V2 Task Overview](V2-TASK-OVERVIEW.md)
**Owner:** Dev B — _TBD_ (กลุ่ม 2)
**Estimate:** L

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** เห็นแมป ถ้ำ และทีมสำรวจเป็นภาพที่อ่านง่าย ลุ้นไปกับการทอยเต๋า และได้ยิน/เห็นผลของทุกการกระทำ
**เพื่อให้** รู้สึกถึงความเสี่ยงและผลลัพธ์ของการตัดสินใจ ไม่ใช่แค่อ่านข้อความ

**สถานะปัจจุบัน:**
- `Core.unity` **ไม่อยู่ใน Build Settings** (ยังชี้ไปที่ scene legacy และ `Setup.unity` ที่ไม่มีไฟล์แล้ว) และไม่มีการเปลี่ยน/โหลด scene ซ้ำ
- แมปเป็นพื้นสีเรียบ, ถ้ำเป็นไอคอนย้อมสี, Scout เลื่อนแบบ linear 0.6 วินาที
- ผลทอยแสดงเป็นข้อความทันที — `DiceRollView.cs` เป็น class ว่าง
- ไม่มีเสียงเลย, ไม่มี effect ตอนได้ของ/บาดเจ็บ/Lost Signal, ไม่มี transition ตอนขึ้นวันใหม่

---

## 📂 ไฟล์ที่ต้องรู้ก่อนเริ่ม

### ต้องอ่านให้เข้าใจ (เจ้าของ = กลุ่มนี้)
| ไฟล์ | สิ่งที่ต้องรู้ |
|---|---|
| `Assets/V2/Scripts/Controllers/GatherMapController.cs` | สุ่มวางถ้ำ (`SetupDefaultNodes`, `FindScatterPosition`, margin L150/R360/T150/B420, `baseClearRadius`), เลือกถ้ำผ่าน `MapNodeView.OnAnyNodeClicked`, **`ConfirmTrip` resolve ทันทีแล้วยิง `OnTripResolved`** — ไม่มี `ReturnToBase`/`TripInProgress` (ลบแล้ว 2026-10-28) |
| `Assets/V2/Scripts/UI/MapNodeView.cs` | สีตาม zone, `SetSelected`, static event `OnAnyNodeClicked` |
| ~~`Assets/V2/Scripts/UI/ScoutWalkerView.cs`~~ | ~~`MoveTo` / `SnapTo`~~ **ลบแล้ว (2026-10-28)** — เกมไม่มีเวลาเดินทาง ทริป resolve ในเฟรมเดียว; `Core.unity` ไม่มี `MapPanel/ScoutIcon` แล้ว |
| `Assets/V2/Scripts/Entities/MapNodeData.cs` | `nodeId`, `zone`, `anchoredPosition` |
| `Assets/V2/Scripts/DiceRollView.cs` | stub ว่าง — จะ implement |

### ต้องเรียกใช้ / subscribe (ไม่ใช่เจ้าของ)
| ไฟล์ | ใช้อะไร |
|---|---|
| `Assets/V2/Scripts/Controllers/GatherDispatchController.cs` | `Dispatch()` คืน `GatherDispatchResult` (tier, rollValue, successValue, loot, hurtUnit, missingUnit, trait, core item), `OnGatherResolved` |
| `Assets/V2/Scripts/System/GatherResolver.cs` | กติกา tier: Fumble (SV<50 → 96–100, ไม่งั้น 100), Hard Success ≤ ⌊SV/2⌋, Regular ≤ SV — ใช้วาดเส้นแบ่งบนหน้าปัดเต๋า |
| `Assets/V2/Scripts/Controllers/DayManager.cs` | `OnDayStarted`, `OnRunEnded`, (ใหม่) `OnDayEnded(DayEndReport)` จากกลุ่ม 3 |
| `Assets/V2/Scripts/Controllers/BaseWorkController.cs` | (ใหม่) `OnBaseWorkCompleted` จากกลุ่ม 1 |
| `Assets/V2/Scripts/Entities/GatherZoneData.cs` | `GatherZone`, `LootAmount` |
| `Assets/V2/Scripts/Entities/UnitCharacter.cs` | `UnitState` (Hurt / Missing) สำหรับ effect |

### Scene / Prefab / Settings
| ไฟล์ | ส่วนที่เกี่ยว |
|---|---|
| `Assets/V2/Scenes/Core.unity` | **เจ้าของ scene** — root objects, `GameSystem`, `Canvas/MapPanel` (เหลือแค่พื้นหลัง + ถ้ำ); ค่า Inspector ของ `GatherMapController` = margin L150 / R360 / T150 / B420 (กันชน panel ใหม่) |
| `Assets/V2/Prefabs/Dungeon Prefab.prefab` | `MapNodeView` + `SelectedHighlight` |
| `Assets/V2/Prefabs/ScoutIcon.prefab` | เหลือแค่ `UnitIconView` + `CanvasGroup` (ถอด `ScoutWalkerView` ออกแล้ว) — ยังใช้เป็นการ์ดยูนิตในมือ |
| `ProjectSettings/EditorBuildSettings.asset` | รายชื่อ scene ใน build |
| `Assets/Day/Sprites/tilemap_packed*.png` | tileset 16 px ที่ prefab ใช้อยู่ |
| `Assets/V2/Prefabs/Feedback/` (สร้างใหม่) | DiceRoll, popup, effect |

### เอกสาร
- [GDD §4 Send-to-Gather](../../gdd/01-mechanics.md#4-send-to-gather--การออกสำรวจ) (ตาราง tier + odds)
- [Art Direction](../../gdd/03-art-direction.md) · [Audio Direction](../../gdd/04-audio-direction.md) (ตาราง SFX ↔ event)
- [System Design §2 Scene Flow, §4.1, §5 Event Map](../../software/04-unity-system-design.md)
- [Scene Hierarchy](../../software/06-unity-core-scene-hierarchy.md)

---

## 🛠 Technical Tasks

### S1 — Scene flow + Build Settings + Contract C5 (S) — ทำก่อน
- [ ] `EditorBuildSettings.asset` — เพิ่ม `V2/Scenes/Core.unity` เป็น index 0, เอา `Setup.unity` (ไฟล์ไม่มี) และ scene legacy ออก (หรือปิด)
- [ ] สร้าง `Assets/V2/Scripts/Scene/SceneFlowController.cs` — `RestartRun()` (reload Core), `GoToTitle()`; เตรียม `Title.unity` แบบเรียบง่าย (ชื่อเกม + Start) ถ้าทีมตกลงว่าจะมี
- [ ] ตรวจว่า reload แล้ว singleton ทุกตัว (`Instance`) และ static event `MapNodeView.OnAnyNodeClicked` ไม่ค้างของรอบเก่า (`GatherMapController.OnDestroy` unsubscribe แล้ว — เช็กตัวอื่นที่เพิ่มใหม่ด้วย)
- [ ] เพิ่ม Contract C2: `GatherMapController.OnNodeSelected : event Action<MapNodeData>` ใน `HandleNodeClicked`

### S2 — ภาพแมปและถ้ำ (M)
- [ ] พื้นหลังแมป (tilemap หรือภาพ) ใน `MapPanel` + ไอคอนฐานที่ตำแหน่ง `baseOffset`
- [ ] `Dungeon Prefab` — ไอคอนต่างกัน 3 แบบตามระดับ + ตัวอักษร/รูปทรงกำกับ (ไม่ใช้สีอย่างเดียว — accessibility ใน [Art §7](../../gdd/03-art-direction.md#7-accessibility-notes)); hover + selected state
- [ ] Contract C6: ลบ `UnitIconView` ออกจาก `ScoutIcon.prefab` หลังกลุ่ม 3 สร้าง `UnitCard.prefab`
- [ ] Scout เดินแบบ sprite sheet / ease แทน linear — **คง signature `MoveTo(Vector2, Action)` เดิม**

### S3 — DiceRollView: ทอยเต๋า D100 (M) — Contract C3 *(ลำดับทริปถูกยกเลิก — เหลือแค่ animation)*
- [ ] Implement `DiceRollView` เป็น MonoBehaviour + prefab `Feedback/DiceRoll.prefab`: แถบ/หน้าปัด 1–100 แสดงช่วง Hard Success / Regular / Failure / Fumble ตาม `successValue`, ตัวเลขหมุนแล้วหยุดที่ `rollValue`, ป้ายผล 4 แบบ
- [ ] API: `Play(GatherDispatchResult result, Action onDone)` + กดข้ามได้
- [ ] ~~`GatherMapController.ReturnToBase` — แทรก `DiceRollView.Play(...)` ก่อนเดินกลับ~~ **ยกเลิก (2026-10-27):** ไม่มีเวลาเดินทาง ทริป resolve ในเฟรมเดียว จึงต้องหาที่แสดงผลแบบอื่น (เช่น overlay บน `DispatchActionPanel` หรือแยก panel) และ **ห้าม** ผูกกับลำดับ `ConfirmTrip → OnTripResolved`
- [ ] ถ้า `DiceRollView` ไม่ถูก assign ให้ข้ามไปเดินกลับเลย (ไม่พังเกม)

### S4 — Feedback, เสียง และ transition (M)
- [ ] `Assets/V2/Scripts/Feedback/FeedbackManager.cs` — popup ลอยที่ฐาน/การ์ด: `+2 Cooking Material`, `HURT`, `LOST SIGNAL`, trait ใหม่, Core Item unlock/obtain (จาก `OnTripResolved`); ผลโรงงาน (จาก C1 `OnBaseWorkCompleted`)
- [ ] `Assets/V2/Scripts/Feedback/AudioManager.cs` + AudioMixer (Music / SFX / UI) — ผูก SFX ตาม [Audio §2.2](../../gdd/04-audio-direction.md#22-sound-effects-ผูกกับเหตุการณ์ที่มีอยู่ในระบบแล้ว); ไฟล์เสียง placeholder ได้
- [ ] Day transition — fade + แบนเนอร์ `Day N / 7` ตอน `OnDayStarted`; สรุปการกินจาก C4 `OnDayEnded(DayEndReport)` (ใครกิน ใครหิว ใคร Lost Signal)
- [ ] Effect จบ run (ก่อน `RunEndPanel` ของกลุ่ม 3 ขึ้น) จาก `OnRunEnded`

---

## ✅ Acceptance Criteria
1. [ ] Build (File → Build) แล้วเปิดเข้า `Core.unity` ได้ และ Restart run ได้โดยไม่มี error / state รอบเก่าค้าง
2. [ ] แยกระดับถ้ำได้โดยไม่ต้องดูสี
3. [ ] ทุกการสำรวจมี animation ทอย D100 ที่เห็นช่วงของแต่ละ tier และเลขที่ออก ก่อนผลจะปรากฏ
4. [ ] ได้ของ / บาดเจ็บ / Lost Signal / ได้ trait / Core Item มี feedback ทั้งภาพและเสียง
5. [ ] ขึ้นวันใหม่มี transition บอกเลขวันและสรุปการกิน
6. [ ] กลุ่ม 3 ใช้ `OnNodeSelected` และ `SceneFlowController.RestartRun()` ได้

---

## 🔗 Coordination
- **ให้กลุ่ม 3:** C2 `OnNodeSelected`, C3 ลำดับทริป (ผล UI เปิดหลัง `OnTripResolved`), C5 `SceneFlowController`, C6 แยก prefab
- **ใช้จากกลุ่ม 1:** C1 `OnBaseWorkCompleted`
- **ใช้จากกลุ่ม 3:** C4 `OnDayEnded(DayEndReport)`
- **แก้ได้แค่ event:** `GatherDispatchController.cs` (ห้ามแก้ตรรกะทอย/ผล)
- **เจ้าของ `Core.unity`:** ช่วยตัดสินเมื่อมีคนแก้ scene ชนกัน ([Overview §4](V2-TASK-OVERVIEW.md#4-กติกาการทำงานร่วมกันใน-coreunity))

## 📝 Docs to Update
- [software/04 §2 Scene Flow, §4.1, §5](../../software/04-unity-system-design.md) · [software/05](../../software/05-unity-class-diagram.md) (SceneFlowController, DiceRollView, FeedbackManager, AudioManager) · [software/06](../../software/06-unity-core-scene-hierarchy.md) (root + MapPanel)
- [gdd/03-art-direction.md §1, §5, §6](../../gdd/03-art-direction.md) · [gdd/04-audio-direction.md §1, §3](../../gdd/04-audio-direction.md)

## 🔗 Related Files
- [V2 Task Overview](V2-TASK-OVERVIEW.md)
- [US-V2-FACTORY-01](US-V2-FACTORY-01.md) · [US-V2-UI-01](US-V2-UI-01.md)
