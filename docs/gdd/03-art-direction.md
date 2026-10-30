# clogged — Art Direction & Visual Style

**Version:** 2.0 (Unity V2) | **Last Updated:** 2026-10-14
> อธิบายสภาพภาพปัจจุบันของ `V2/Scenes/Core.unity` (placeholder) และทิศทางที่วางไว้ — สไตล์ของ Web Prototype ดูที่ [archive](../wiki/archive/gdd-v1-prototype/03-art-direction.md)

## 1. Current State: PROTOTYPE (placeholder)

| ส่วน | สภาพปัจจุบัน |
|---|---|
| ระบบภาพ | Unity UGUI ล้วน บน Canvas (Screen Space - Overlay) — ไม่มี world sprite / tilemap ใน scene |
| แมป | `MapPanel` เป็น Image สีพื้นเต็มจอ ไม่มีภาพพื้นหลัง |
| ถ้ำ | `Dungeon Prefab` — ไอคอนจาก tileset 16 px ย้อมสีตามระดับ + กรอบ `SelectedHighlight` เมื่อถูกเลือก |
| Scout / การ์ดยูนิต | `ScoutIcon.prefab` — ไอคอนจาก tileset + ข้อความชื่อ/สถานะ |
| Tileset | `Assets/Day/Sprites/tilemap_packed*.png` (pixel art 16 px สไตล์ Kenney, ยืมจากระบบ Day เดิม) |
| ปุ่ม / dropdown / toggle | UI default ของ Unity (TMP Dropdown, Toggle ใช้ `Text` แบบ legacy) |
| Animation | มีแค่ Scout เลื่อนตำแหน่งแบบ linear 0.6 วินาที ไม่มี animation ทอยเต๋า |

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
| ถนัด / ดี | `#5BD65B` | ชื่อยูนิตที่ถนัด Cooking ในรายชื่อผู้ทำงาน |
| ไม่ถนัด / เตือน / ผิดพลาด | `#E65C5C` | ยูนิตไม่ถนัด, เหตุผลที่กดไม่ได้, Food ไม่พอ, "last day!" |
| ใช้งานไม่ได้ | `#8A8A8A` | ยูนิตที่ไม่ Ready ในรายชื่อ |
| บาดเจ็บ | `#FF8080` (tint) | ไอคอนยูนิตที่ Hurt |

### ความทึบของการ์ดยูนิต

| สถานะ | Alpha |
|---|---|
| Ready (Idle) | 100% |
| Resting / Hurt / Away | 50% |
| Missing (Lost Signal) | 20% |

## 3. Typography

- ฟอนต์: **LiberationSans SDF** (ฟอนต์ default ของ TextMeshPro) ทุกข้อความ
- ⚠️ ฟอนต์นี้ **ไม่มีอักษรไทย** — ถ้าจะทำเกมภาษาไทยต้องเพิ่ม TMP Font Asset ภาษาไทย (เช่น Noto Sans Thai / Sarabun) เป็น fallback
- ใช้ rich text `<color=#...>` ของ TMP สำหรับเน้นสีในข้อความ

## 4. Screen Layout (1920×1080 reference)

```
┌──────────────────────── StatusBar: Day | Food | Cooking Material | Medicine | Materials | Core Item ─┐
│                                                                        MessageUi (ผลการสำรวจ)        │
│                                                                                                      │
│       ● Easy        ● Hard                                                  ┌──────────────────┐     │
│                                    ◆ ฐาน / Scout                            │  BaseWorkPanel   │     │
│   ● Normal                  ● Easy                                          │  [Cook][Heal][Craft]   │
│                 ● Hard            ● Normal                                  │  Worker ▼        │     │
│                                                                             │  Patient ▼ / − n +│    │
│                                                                             │  Preview         │     │
│                                                                             │  [Confirm]       │     │
│                                                                             └──────────────────┘     │
│ [Ann][Boon][Cia][Dan]  ← การ์ดยูนิต      ☐Ann ☐Boon ☐Cia ☐Dan  ← เลือกส่งสำรวจ                       │
│                                               [Dispatch]          [EndDay]                           │
└──────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

ถ้ำสุ่มตำแหน่งทุก run แต่เว้นขอบขวา 420 px ไว้ให้ BaseWorkPanel และขอบล่าง 260 px ไว้ให้การ์ด/ปุ่ม — รายละเอียดดู [Core Scene Hierarchy](../software/06-unity-core-scene-hierarchy.md#32-canvas--ui-layer)

## 5. Target Visual Direction (ยังไม่ทำ)

1. **Pixel art 16×16** ต่อยอดจาก tileset ที่ใช้อยู่ — ฐาน, ปากถ้ำ 3 แบบตามระดับ, ไอคอนทรัพยากร
2. **ภาพพื้นหลังแมป** (tilemap หรือภาพวาด) แทนพื้นสีเรียบ
3. **Portrait ตัวละคร 4 คน** สำหรับการ์ดยูนิต — แสดงสถานะ Hurt / Hungry / Lost Signal ด้วยภาพ ไม่ใช่แค่ข้อความ
4. **Animation ทอยเต๋า D100** (`DiceRollView`) — แสดงเลขที่ทอยเทียบเส้น Hard Success / Success / Fumble ให้ผู้เล่นเห็นความเสี่ยง
5. **Scout เดินแบบ sprite sheet** แทนการเลื่อนตำแหน่ง
6. **หน้าจอจบเกม** Win / Lose
7. **ไอคอนทรัพยากร** บน StatusBar แทนข้อความล้วน

## 6. UI Feedback (ปัจจุบัน)

| เหตุการณ์ | Feedback |
|---|---|
| เลือกถ้ำ | กรอบ `SelectedHighlight` ขึ้นที่ถ้ำนั้น |
| เลือกยูนิตเกิน 2 | toggle ที่เพิ่งติ๊กถูกยกเลิกเอง |
| ส่งสำรวจ | ปุ่ม Dispatch ปิด, ข้อความ "Traveling...", Scout เดินไป-กลับ |
| ผลการสำรวจ | ข้อความหลายบรรทัด: ระดับผล + เลขทอย/SV, ของที่ได้, สถานะแต่ละคน, trait ที่ได้, Core Item |
| งานในฐาน | preview ต้นทุน/ผลได้ก่อนกด, เหตุผลสีแดงเมื่อกดไม่ได้, ข้อความผลหลัง Confirm |
| Food ไม่พอสำหรับคืนนี้ | ข้อความ Food บน StatusBar เป็นสีแดง |

## 7. Accessibility Notes

- ระดับถ้ำสื่อด้วย **สีอย่างเดียว** (เขียว/เหลือง/แดง) — ผู้เล่นตาบอดสีแดง-เขียวแยก Easy กับ Hard ยาก → ควรเพิ่มรูปทรงหรือตัวอักษร (E/N/H) บนไอคอน
- ความถนัดในรายชื่อผู้ทำงานใช้สีเขียว/แดงเช่นกัน → ควรเพิ่มสัญลักษณ์ (★ / ✕)
- ยังไม่มีตัวเลือกขนาดตัวอักษร

## Related Documents
- [Game Concept](00-concept.md)
- [Core Scene Hierarchy](../software/06-unity-core-scene-hierarchy.md)
