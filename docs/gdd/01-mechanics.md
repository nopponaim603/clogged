# clogged — Core Mechanics

**Version:** 0.3 (Day Phase Clarified) | **Last Updated:** 2026-07-24
> เอกสารนี้แปลงจาก [Clogged_GDD_v0.3 (Google Doc)](https://docs.google.com/document/d/1SGxMLKs7FlRq_E-0OHyskQbingBxaU5L/edit)

---

## Core Loop (1 Cycle = 1 Node)

1. **Phase ยาน:** เลือก Node จาก Node Graph (3 ชั้น × 5 Block) → เดินทางไปยัง Node นั้น
2. **Phase กลางวัน:** Map แบบต่อเนื่องเกิด resource points แบบสุ่ม → วางแผน Queue ของ Units (ใช้ระบบเวลาแบบ Distance / Speed) → เก็บของ/Interact → กลับยาน หรือเสีย Unit
3. **Phase กลางคืน:** รู้ทิศทาง + จำนวนศัตรู → วางป้อม/กับดัก/Units → ป้องกัน Wave (ผู้เล่นควบคุม Main Gun) → สรุปผล
4. กลับไป Phase ยาน เพื่อเลือก Node ถัดไป (หรือเจอบอส)

### Win / Lose Conditions
- **Win:** ปราบบอสทั้ง 3 ตัวสำเร็จ (แต่ละชั้นมี Node บอส 1 ตัว)
- **Lose (เบื้องต้น):** ยานถูกทำลาย (HP ยาน = 0) หรือ Units หมด (ไม่สามารถเก็บของ/ป้องกันได้)

---

## Phase 1: Ship / Navigation Phase

Phase นี้คือการเลือกเส้นทางแบบ Node Graph คล้าย Integrated Strategies (IS) ในเกม Arknights หรือ Roguelike Map

### โครงสร้าง Map
- ทั้งหมด 3 ชั้น (Layer)
- แต่ละชั้นมี 5 Block (คอลัมน์)
- ผู้เล่นเริ่มที่จุด Start ของแต่ละชั้น
- เลือก Node ใน Block ถัดไปได้เฉพาะที่เชื่อมต่อด้วยลูกศร/เส้นทาง
- จบชั้นเมื่อถึง Node บอสและปราบสำเร็จ → ไปชั้นถัดไป

### ประเภท Node

| Type | รายละเอียด |
|------|-----------|
| Node ธรรมดา | มี Resource พื้นฐาน, พื้นที่ล่าสัตว์, โบราณสถาน, Encounter (?), ร้านค้า ปรากฏแบบสุ่ม |
| Encounter | เหตุการณ์สุ่ม (เครื่องหมาย ?) — Choice, Battle, Reward, หรือ Trap |
| ร้านค้า | แลกเปลี่ยน / ซื้อขาย / Recruit ลูกเรือ (Units ใหม่) |
| Node พิเศษ: ล่าสัตว์ | รับประกันพื้นที่ล่าสัตว์ + ระดับสัตว์สูงขึ้น (Reward ดีขึ้น, Risk สูง) |
| Node พิเศษ: โบราณสถาน | รับประกันโบราณสถาน + ระดับ Relic สูงขึ้น |
| Node พิเศษ: ทรัพยากรเฟื่องฟู | สุ่ม 1 ประเภท Resource พื้นฐานให้เกิดจำนวนมากผิดปกติ |
| Node บอส | บอสจะปรากฏใน Phase กลางคืน |

### ข้อเสนอสำหรับ Node บอส (Day Phase)

| แนวทาง | รายละเอียด |
|--------|-----------|
| **A — Prep & Sabotage (แนะนำ)** | Map กลางวันมี "Boss Perimeter" หรือ "Weak Point Nodes" — Units ไป Interact เพื่อลด Max HP/Armor, ปลดล็อก Weakness, หรือได้ Boss Material สำหรับ Craft อาวุธพิเศษ (ความเสี่ยงสูง — Enemy Patrols ทำให้ Time หมดเร็ว) |
| B — High Risk High Reward | Resource ทั้งหมดเป็นประเภทหายาก + "Corrupted Zones" ให้ Bonus มากแต่ Units เสี่ยงติด Debuff |
| C — Minimal | ไม่มีอะไรพิเศษใน Day (เหมือน Node ธรรมดา) แต่ Night มี Boss Wave ที่ hard กว่า + บอสมี Phase พิเศษ |

---

## Phase 2: Day Phase — Time Management

หลังจากเลือก Node แล้ว เกมจะ generate Map แบบ Top-down แบบต่อเนื่อง (Continuous Coordinate) ที่มีตำแหน่งยาน + จุดสนใจ

### องค์ประกอบบน Map
- ตำแหน่งยาน (จุดเริ่มและจุดกลับของ Units เสมอ)
- Resource Points (พื้นฐาน) — หลายประเภท
- พื้นที่ล่าสัตว์ (Hunting Ground)
- โบราณสถาน (Ruins) → Relic
- Encounter (?)
- ร้านค้า

### ขั้นตอนการเล่น
1. ผู้เล่นสามารถ Upgrade / Craft ก่อนเริ่มวางแผนจากทรัพยากรที่มี (Planning Phase)
2. วางแผน Queue ของ Units แต่ละตัว (เลือกจุดหมายเป็นลำดับ) (Planning Phase)
3. กด "Execute" → Units ทั้งหมดเริ่มเคลื่อนที่พร้อมกัน (Parallel Simulation)
4. Units ที่กลับยานทันเวลา → ได้ Resource / Item / Relic ตามที่เก็บ
5. Units ที่ Time หมดก่อนกลับ → เสีย Unit นั้น + ไม่ได้ของ
6. จบ Day → เข้า Night Phase (ไม่สามารถ Interrupt / Cancel ระหว่าง Execute ได้)

### ลักษณะแผนที่
แผนที่เป็นแบบต่อเนื่อง (Continuous Coordinate) ไม่ใช่ Grid
- การเคลื่อนที่ = จากพิกัดจุด A ไปยังพิกัดจุด B
- ผู้เล่นเลือกจุดหมายแล้วเพิ่มเข้า Queue ของ Unit นั้น
- Action สุดท้ายของทุก Queue จะเป็นการกลับยานเสมอ
- ตัวอย่าง Queue: ยาน → เหมือง A → เหมือง B → ยาน
- คิดระยะทางจากยาน → เหมือง A → เหมือง B → ยาน แล้วคำนวณ Time แต่ละช่วง

### ระบบเวลา (Time System) — หัวใจของ Day Phase

ทุก Unit มี **Time Pool** เท่ากันต่อวัน (ตัวอย่าง = 12 หน่วย)
Time Cost คำนวณจาก **ระยะทาง / ความเร็ว** (หรือ **ปริมาณ / อัตรา**)

#### ความถนัด (Specialty) = ตัวคูณ
- Unit แต่ละตัวมีความถนัดที่เป็นตัวคูณ (Multiplier)
- ความเร็วพื้นฐาน (Base Speed) = 100 หน่วย
- อัตราการเก็บพื้นฐาน (Base Gather Rate) = 100 หน่วย
- Specialty Movement 1.5x → ความเร็วจริง = 100 × 1.5 = 150
- Specialty Gathering 1.5x → อัตราเก็บจริง = 100 × 1.5 = 150

#### ตัวอย่างการคำนวณ

| Unit | Specialty | Action | การคำนวณ | Time Cost |
|------|-----------|--------|-----------|-----------|
| Unit A | Movement 1.5x | เดินทาง 1000 หน่วยไปเหมือง A | 1000 ÷ (100 × 1.5) = 1000 ÷ 150 | 6.67 |
| Unit A | — | เก็บแร่ 1000 หน่วย (base 100) | 1000 ÷ 100 | 10.00 |
| Unit B | Movement 0.8x, Mining 1.5x | เดินทาง 1000 หน่วยไปเหมือง A | 1000 ÷ (100 × 0.8) = 1000 ÷ 80 | 12.50 |
| Unit B | Mining 1.5x | ขุดแร่ 1000 หน่วย | 1000 ÷ (100 × 1.5) = 1000 ÷ 150 | 6.67 |

**สูตร:**
- Time เคลื่อนที่ = ระยะทาง ÷ (Base Speed × Multiplier ความถนัดเคลื่อนที่)
- Time เก็บของ = ปริมาณ ÷ (Base Gather Rate × Multiplier ความถนัดเก็บ)
- Total Time ของ Unit = ผลรวม Time ของทุก Action ใน Queue (รวมกลับยาน)
- ถ้า Total Time > Time Pool → เสีย Unit + ไม่ได้ของ

### การเคลื่อนที่ของ Units
- **Parallel:** เมื่อกด Execute ทุก Unit เริ่มเคลื่อนที่พร้อมกัน
- แต่ละ Unit มี Queue Action ของตัวเอง (ไปจุดไหนก่อน-หลัง)
- **ไม่สามารถ Interrupt / Cancel ระหว่าง Execute ได้**

### Encounter และ ร้านค้า
- ทั้งสองมี Time Cost สำหรับ Interact + Time Cost การเดินทางไปถึง
- Encounter: เมื่อถึงจุด → ขึ้น Choice Popup
- ร้านค้า: เมื่อถึงจุด → เปิดหน้า UI ร้านค้า

---

## Phase 3: Night Phase — Tower Defense

มุมมอง Top-down ป้องกันยานจาก Wave ศัตรูที่บุกจาก 4 ทิศทาง (N/S/E/W)

### ข้อมูลที่ผู้เล่นรู้ก่อนวางแผน
- จำนวนศัตรูที่จะมา (โดยประมาณ)
- ทิศทางที่ศัตรูจะบุก (ลูกศรสีแดง)
- ขนาด Wave (จำนวนลูกศรซ้อนกัน)
- ศัตรูพิเศษ / บอส (ดาวสีแดง)

### เครื่องมือป้องกัน

| ประเภท | รายละเอียด |
|--------|-----------|
| ป้อมปืนหลัก (Main Gun) | ผู้เล่นควบคุมเอง เลือกยิงได้ทั้ง 4 ทิศ — รูปแบบการยิงเปลี่ยนตาม Unit ที่ประจำการ (Facility) |
| ป้อมปืนรอง | Automation ยิงทิศที่กำหนดไว้ |
| ป้อมชั่วคราว (Temp Turret) | Craft ได้ ใช้ได้คืนเดียว |
| อุปกรณ์ป้องกัน | กับระเบิด, ลวดหนาม, สิ่งกีดขวางเส้นทาง |
| Units วางนอกยาน | Automation โจมตีศัตรูในเส้นทาง (สามารถสั่งย้ายได้ แต่ใช้เวลา + ระหว่างย้ายโจมตีไม่ได้) |

### พื้นที่วาง
- ผู้เล่นวางป้อมชั่วคราว + Units ภายในพื้นที่สี่เหลี่ยมสีฟ้า (Safe/Placement Zone)
- ลูกศรแดง = ทิศทางบุก + ขนาด Wave
- ดาวแดง = ศัตรูพิเศษ/บอส

### หลังจบ Night
- เข้าช่วงสรุปผล (ได้รับ XP / Resource เพิ่ม / ความเสียหายที่เกิดกับยาน)
- กลับไป Phase ยาน เพื่อเลือก Node ถัดไป

---

## Core Systems

### Unit System
Units = ทรัพยากรคนที่สำคัญที่สุด (ใช้ทั้ง Day เก็บของ + Night ป้องกัน + Facility)
- **ความสามารถ (Abilities / Traits):** Passive Bonus เช่น +โอกาส Relic, Double Resource chance, +% ATK ใน Night, ได้ Resource X เพิ่ม
- **ความถนัด (Specialty):** ตัวคูณความเร็วการเคลื่อนที่ หรืออัตราการเก็บ/Interact (เช่น Movement 1.5x, Mining 1.5x, Movement 0.8x)
- **Equipment + Relic:** เสริมค่าพลังได้

**ต้องกำหนดเพิ่มเติม:**
- รายชื่อ Unit ตัวอย่าง 6–8 ตัวพร้อม Trait + Specialty Multiplier
- จำนวน Unit เริ่มต้น, Max Capacity, วิธี Recruit (ร้านค้า / Event)
- การตายถาวร vs บาดเจ็บ (permadeath หรือไม่?)

### Resource & Economy

| ประเภท | ตัวอย่างชื่อ | 用法 |
|--------|-------------|------|
| Resource A | Energy / Fuel | เคลื่อนที่ยาน, Craft บางอย่าง |
| Resource B | Material / Scrap | Craft ป้อม + Equipment |
| Resource C | Bio / Food / Organic | รักษา Units / Recruit |
| Resource D | Rare / Crystal | Upgrade ระดับสูง / Relic |
| Boss Material | — | เฉพาะจาก Node บอส สำหรับ Craft สุดท้าย |

### Upgrade System
- อุปกรณ์สวมใส่ให้ Units (จาก Resource)
- เก็บ Relic จากโบราณสถาน (Passive ถาวรหรือมีเงื่อนไข)
- Upgrade ยาน / Main Gun / Facility

### Crafting System
- อุปกรณ์สวมใส่ Units
- ป้อมปืนชั่วคราว (Temp Turrets)
- กับดัก / ลวดหนาม / สิ่งกีดขวาง
- ควรมี Crafting Tree หรือ Recipe ที่ unlock ได้ตาม Progress

### Facility System
ผู้เล่นสามารถ Assign Units เข้า Facility บนยาน เพื่อรับ Bonus ถาวรในช่วง Day/Night

| Facility | Bonus |
|----------|-------|
| Bridge | Main Gun Bonus |
| Workshop | +Craft Speed/Quality |
| Medbay | ลดโอกาสเสีย Unit |
| Scout Bay | +Time หรือ Vision |

> Unit ที่ Assign จะไม่สามารถส่งออก Day หรือวาง Night ได้ (Trade-off ชัดเจน)

---

## Progression

### ภายใน Run (Single Run)
เก็บ Resource → Craft/Upgrade → Units แข็งแกร่งขึ้น → ปราบบอสได้

### ระหว่าง Run (Roguelike Meta)
Unlock Unit ใหม่, Facility ใหม่, Starting Bonus, Difficulty modifiers

### Win Condition
ปราบบอส 3 ตัว → Ending / Score / Unlock

---

## Open Questions

| หัวข้อ | สถานะ |
|--------|--------|
| Phase กลางวันของ Node บอส → ใช้แนวทางไหน? (A/B/C) | เปิด |
| จำนวน Unit เริ่มต้น / Max / วิธีได้เพิ่ม | เปิด |
| Resource มีกี่ชนิด และชื่ออะไร? | เปิด |
| Theme / Art style ต้องการแนวไหน? | เปิด |
| Base Speed / Base Gather Rate ที่แน่นอน และ Time Pool มาตรฐาน | เปิด |
| การตายถาวร (permadeath) หรือ บาดเจ็บ? | เปิด |

## Next Steps

1. ตกลง Theme / Setting เบื้องต้น
2. ออกแบบ Unit 6 ตัวแรก + Resource 4 ชนิด + Specialty Multiplier ตัวอย่าง
3. เขียน Boss 1 ตัวแรก (รวม Day Prep)
