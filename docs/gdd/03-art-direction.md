# clogged — Art Direction & Visual Style

**Version:** 2.1 (Unity V2) | **Last Updated:** 2026-10-29
> อธิบายสภาพภาพปัจจุบันของ `Unity-Projects/Assets/V2/` และแยกงานต่อยอดออกจากของที่มีแล้ว — สไตล์ของ Web Prototype ดูที่ [archive](../wiki/archive/gdd-v1-prototype/03-art-direction.md)

## 1. Current State

| ส่วน | สภาพปัจจุบัน |
|---|---|
| Presentation | Unity UGUI บน Canvas แบบ Screen Space - Overlay; map, hand, log, controls และ HUD อยู่ใน `Core.unity` |
| Main menu | `MainMenu.unity`; `MainMenuController` สร้าง Start, Continue, Options และ Exit runtime |
| Map / cave | พื้นที่แผนที่เป็น UI; cave prefab แสดง zone และ AP cost พร้อมสีและ selected highlight |
| Character hand | `ScoutIcon.prefab` ใช้เป็นการ์ดยูนิต มีชื่อ, state/AP pips, สีกรอบรายตัว และ feedback การเลือก |
| Core Item | HUD แสดงกล่อง/กุญแจเป็นภาพคู่สีส้ม/เหลือง; ได้ครบแล้วแสดงกล่องเปิดและ crystal reveal overlay |
| Icons | `Assets/V2/Sprites/Icons/` มี AP pips, Core Item art และชุด candidate icons สำหรับ HUD/skills; การเชื่อม sprite รายช่องขึ้นกับ scene/Inspector |
| Typography | มี TMP font asset `Fonts/PixelTileset.asset` จาก tileset; ใช้กับ V2 UI ตามสถานะปัจจุบันของงาน redesign |
| Feedback | `LogPanel` เก็บประวัติ 16 รายการ, `Warning` ใช้ palette ร่วมกับ log; Run Result แสดงผลและสถิติ |
| Animation | ไม่มีเดินทางระหว่างฐานกับถ้ำหรือ dice-roll animation; dispatch คำนวณผลทันที มี crystal reveal แบบ fade |

## 2. Color Language (ใช้ในโค้ดตอนนี้)

### ระดับถ้ำ

| ระดับ | สี | Hex (โดยประมาณ) |
|---|---|---|
| Easy | เขียว | `#73BF73` |
| Normal (Mid) | เหลืองทอง | `#D9BF4D` |
| Hard (Far) | แดง | `#CC4D4D` |

### ความหมายของสีใน UI

| ความหมาย | Hex | ใช้ที่ |
|---|---|---|
| ความสำเร็จ | `UiPalette.Success` ≈ `#6BD973` | log และ feedback สำเร็จ |
| ผิดพลาด | `UiPalette.Fail` ≈ `#E86161` | log/ผลล้มเหลว |
| คำเตือน | `UiPalette.Warning` ≈ `#FFB840` | log และ warning banner |
| จบวัน | `UiPalette.DayEnd` ≈ `#8CC0FF` | log สรุปวัน |
| Action / System | `UiPalette.Action` / `System` | log ทั่วไป |
| สถานะยูนิต | tint/alpha ของการ์ดและกรอบรายตัว | แสดง readiness, selection, Hurt/Missing ตาม `UnitIconView` |

สีของ log และ warning ใช้ตารางเดียวจาก `UiPalette`; cave ยังใช้เขียว/เหลือง/แดง แต่มีชื่อ zone และ AP cost ประกอบ ไม่พึ่งสีอย่างเดียว

## 3. Typography

- ฟอนต์ V2 ที่เพิ่มเข้ามา: **PixelTileset TMP Font Asset** (`Assets/V2/Fonts/PixelTileset.asset`) สร้างจาก pixel tileset; ตรวจ glyph coverage ก่อนสรุปการรองรับภาษา
- ข้อความ UI และ game copy เป็นภาษาอังกฤษ; การรองรับไทยยังไม่ยืนยันและควรเพิ่ม/ทดสอบ fallback font ก่อนแปลเกม
- ใช้ rich text `<color=#...>` ของ TMP สำหรับเน้นสีในข้อความ

## 4. Screen Layout (1920×1080 reference)

```
┌──────────────────────── StatusBar: Day | Food | Cooking Material | Medicine | Materials | Core Item ─┐
│                                                                    Warning / Crystal reveal          │
│                                                                                                      │
│       ● Easy        ● Hard                                                  ┌──────────────────┐     │
│                                    ◆ ฐาน / cave markers                   │ Cook / Heal / Craft    │
│                                                                             │ SkillTriggerPanel      │
│ [LOG]       [Ann] [Boon] [Cia] [Dan]                 [Dispatch / Confirm]    │                       │
│             Character Hand                  คลิก DayFrame = End Day          │                       │
└──────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

ถ้ำ 6 จุดสุ่มตำแหน่งรอบฐานและเว้นพื้นที่ panel โดย `GatherMapController`; รายละเอียด layout ดู [Core Scene Hierarchy](../software/06-unity-core-scene-hierarchy.md#32-canvas--ui-layer) และ [UI Layout Redesign](../software/07-unity-v2-ui-layout-redesign.md)

## 5. Target Visual Direction (งานต่อยอด)

1. ทำภาพฉาก/พื้นหลังแมปให้สมบูรณ์กว่าพื้น UI ปัจจุบัน
2. สร้าง portrait หรือ character art ที่สอดคล้องกับการ์ด Ann, Boon, Cia และ Dan
3. เพิ่ม animation ทอย D100 (ถ้าต้องการเน้นช่วงเสี่ยง) โดยไม่เปลี่ยนผลคำนวณที่ resolve ทันที
4. ตรวจและปรับ icon assignments ใน scene ให้ครบ; candidate sprites ใน `Sprites/Icons/` ไม่ได้แปลว่าทุกช่องใช้งานแล้ว
5. ตรวจ pixel-font glyph, scaling และ contrast ที่ resolution/aspect ratio เป้าหมาย

## 6. UI Feedback (ปัจจุบัน)

| เหตุการณ์ | Feedback |
|---|---|
| เลือกถ้ำ | กรอบ selected highlight; cave แสดง zone/AP cost |
| เลือกยูนิต/เกินจำนวน | การ์ดแสดง selection; selection ที่ไม่ผ่านเงื่อนไขถูกปฏิเสธและมี warning/log |
| ส่งสำรวจ | resolve ทันที; log แสดง tier, roll/SV, loot, injury/loss, trait และ Core Item |
| งานในฐาน | เลือก Cook/Craft queue หรือ Heal mode; preview AP pips และ confirm ผ่านปุ่มร่วม |
| Food ไม่พอคืนนี้ | HUD แสดงจำนวนที่มีเทียบจำนวนที่ต้องใช้และเน้นเตือน |
| ได้ Core Item ครบ | กล่องเปิด, crystal reveal fade; คลิกเพื่อปิด |
| จบ run | Run Result overlay แสดงเหตุผล, art และสถิติ; มีทางเริ่มใหม่/กลับเมนู |

## 7. Accessibility Notes

- ตรวจ contrast และความชัดของข้อความ/ภาพเมื่อปรับ resolution และ scale
- ตรวจสถานะการ์ดและโอกาส action ด้วยสัญญาณมากกว่าสีเพียงอย่างเดียวในแต่ละโหมด input
- ยังไม่มีตัวเลือกขนาดตัวอักษร

## Related Documents
- [Game Concept](00-concept.md)
- [Core Scene Hierarchy](../software/06-unity-core-scene-hierarchy.md)
