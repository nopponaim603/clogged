# clogged — Core Mechanics

**Version:** 1.0 | **Last Updated:** 2026-07-13
> เอกสารนี้สร้างจากการอ่านโค้ด `prototype_resource_game/src/` จริง (ดู [Design Pivot](00-concept.md#2-️-design-pivot--บันทึกไว้เพื่อความชัดเจน)) — ตัวเลขทั้งหมดคือค่าปัจจุบันใน `src/config.ts` และปรับได้ระหว่าง balancing (เฟส 3)

## Core Loop

1. **วางแผน (Planning phase, กลางวัน)** — เลือกลูกเรือที่ว่าง → คลิก resource node / relic / monster เพื่อสร้าง "chain" ภารกิจ (ส่งลูกเรือคนเดียวหรือหลายคนร่วมกันได้)
2. **ดำเนินภารกิจ (Execution)** — ระบบจำลองเวลาเดินทางไป-กลับ + เวลาปฏิบัติภารกิจ ภายในเวลาที่จำกัดต่อวัน (`DAY_TIME_LIMIT = 12000` หน่วย) ถ้าเวลาไม่พอ ภารกิจให้ผลลัพธ์บางส่วนแทนที่จะล้มเหลวทั้งหมด
3. **จบวัน** — ทรัพยากรที่เก็บได้ในวันนั้นแต่ยังไม่ถูกใช้ **เสียไปครึ่งหนึ่ง** (`loseDayResources`) → นี่คือกลไกหลักที่สื่อธีม "clogged" (สะสมเกินจำเป็น = เสียของ)
4. **กลางคืน (Night phase)** — มอนสเตอร์บุกฐานเป็นระลอก (`MONSTER_SPAWN_INTERVAL`), ฐานรับดาเมจ
5. **เริ่มวันใหม่** — กินอาหาร (`2 × จำนวนลูกเรือ`), ถ้าอาหารไม่พอเกิด shortage, จ้างลูกเรือใหม่ด้วย point, วนกลับไปข้อ 1
6. ทำซ้ำจนครบ **30 วัน** (`MAX_DAYS`) เพื่อชนะ

## Player Actions

| Action | Input | Result | Notes |
|--------|-------|--------|-------|
| เลือกลูกเรือ | คลิกไอคอนลูกเรือ | เข้าโหมดเลือกเป้าหมาย | ใช้ไม่ได้ตอน busy/กลางคืน/กำลังรัน execution |
| สร้าง mission chain | คลิก node ซ้ำๆ | เพิ่ม node เข้า chain ของลูกเรือคนนั้น | คลิกซ้ำ node เดิม = ยกเลิก |
| ยกเลิกการเลือก node ล่าสุด | Ctrl+Z | ลบ node สุดท้ายออกจาก chain | |
| ล้างการเลือกทั้งหมด | R | เคลียร์ selection | |
| ยกเลิกทั้งหมด | ESC | deselect | |
| ยืนยัน/รันภารกิจ | ปุ่ม Execute (PlanningPanel) | เริ่ม simulate การเดินทาง+ปฏิบัติภารกิจของทุก chain พร้อมกัน | |
| จ้างลูกเรือใหม่ | ใช้ point ผ่าน UI | เพิ่มลูกเรือเข้า roster | ราคาแปรผันตาม `hireCost` ของแต่ละคน |

## Game Systems

### Crew System
ลูกเรือแต่ละคนมีค่า `hp`, `speed`, `gatheringProficiency`, `searchingProficiency`, `huntingProficiency`, `perks[]`, `equipment` (weapon/armor/accessory) ค่า effective (`getEffectiveGathering/Searching/Hunting/Speed`) คำนวณจาก base stat + perk bonus + equipment bonus

**Perks:** fast_hands (gathering time -50%), night_vision (search efficiency +50%), gunslinger (hunting +100% เมื่อถือปืน), blacksmith (iron gathering x3), quick_reflex (speed +30%), tough (HP +50%)

ลูกเรือที่ตายถาวร (`isAlive = false`) — เป็นความเสี่ยงจากภารกิจล่ามอนสเตอร์โดยเฉพาะ

**Linked to Software Design:** [System Design — Entities/Crew](../software/01-system-design.md)

### Resource System
7 ชนิดทรัพยากร: wood, stone, iron, food, water, circuit, aluminum + monster parts (fangs/hides/claws, จากการล่ามอนสเตอร์) + relics (จากการค้นหา ให้ food/wood/stone)

- **ขาด (Starvation):** ลูกเรือกินอาหารคนละ 2 หน่วย/วัน ถ้าไม่พอ เกิด shortage (ยังไม่มีผลลบชัดเจนใน prototype ปัจจุบัน — เป็นช่องว่างที่ต้องออกแบบเพิ่ม ดู [Idea-design ข้อ 5](00-concept.md))
- **ตัน (Clogging):** ทรัพยากรที่เก็บได้ระหว่างวันแต่ไม่ได้ใช้ จะหายไปครึ่งหนึ่งตอนจบวัน (`loseDayResources`) — เป็น proxy ของธีม "ของสะสมเกินจะเสีย"

### Mission System
3 ประเภทภารกิจ: **Gathering** (เก็บทรัพยากร), **Relic Search** (สุ่มเจอ relic หรือทรัพยากรรอง), **Monster Hunt** (สู้เพื่อ monster parts, เสี่ยงลูกเรือบาดเจ็บ/ตาย) — แต่ละแบบมีเวอร์ชัน **Collaborative** (หลายลูกเรือร่วมภารกิจเดียว → โบนัสจากจำนวนคน แต่หารความสามารถเฉลี่ย)

เวลาภารกิจ = travel time (ไป-กลับ, ขึ้นกับระยะทาง+speed) + action time (ขึ้นกับ proficiency+difficulty) ถ้ารวมแล้วเกิน `DAY_TIME_LIMIT` ระบบคำนวณผลลัพธ์แบบสัดส่วนเวลาที่เหลือแทนการ fail เต็มรูปแบบ

**Linked to Software Design:** [System Design — MissionSystem](../software/01-system-design.md)

### Time / Day-Night System
- Day time limit: 12,000 หน่วย (จำลอง 1 วินาทีจริง = 100 หน่วยเกม)
- Night duration: 5,000 ms (fixed real-time)
- Max days: 30

### Base Defense System
ฐานมี HP (`BASE_HP = 100`) ถูกโจมตีจากมอนสเตอร์ที่ spawn รอบฐานตอนกลางคืน (จำนวน spawn ต่อคืนยังไม่ผูกกับ day number ใน prototype — เป็นจุดที่ต้อง balance เพิ่มเติมเฟส 3)

## Win / Lose Conditions

- **Win:** อยู่รอดครบ 30 วัน (`day > MAX_DAYS`)
- **Lose:** ลูกเรือตายหมด (`aliveCrews.length === 0`) **หรือ** ฐานถูกทำลาย (`baseHP <= 0`)

## ส่วนที่ยังต้องตัดสินใจ (ต่อยอดจาก Idea-design เดิม)

| หัวข้อ | สถานะ |
|---|---|
| Setting/Theme (โรงงาน? ท่อประปา? ฐานอวกาศ?) | ยังไม่ระบุชัด — code ปัจจุบันใช้ generic base/crew ไม่มี narrative framing |
| ตัวละครผู้เล่นเป็นคนหรือยานอวกาศ | ค้างจาก [Meeting Note รอบที่ 2](../agile/meeting-backlogs/2026-06-29-resource-management.md) |
| Platform & Control | Prototype เป็น web (Phaser+Vite) ควบคุมด้วยเมาส์ — ยังไม่ยืนยันเป็นทางการว่าเป็น target platform สุดท้าย |
| Production/crafting (สมการเคมี ตามไอเดียเดิม) | ยังไม่มีใน prototype — ต้องตัดสินใจว่าจะเพิ่มหรือปล่อยผ่าน |
| ผลลบของ food shortage | ยังไม่ implement (คำนวณ shortage ไว้แต่ไม่มี consequence) |
