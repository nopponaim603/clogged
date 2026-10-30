# clogged — Game Concept

**Version:** 2.0 (Unity V2) | **Last Updated:** 2026-10-14 | **Owner:** ทีม clogged
> อ้างอิงจากระบบที่ทำงานจริงใน `Unity-Projects/Assets/V2/` (scene `Core.unity`) — GDD v1.0 ของ Web Prototype (Phaser) เก็บไว้ที่ [wiki/archive/gdd-v1-prototype](../wiki/archive/gdd-v1-prototype/README.md)

## 1. Introduction

### Elevator Pitch
ผู้เล่นคุมทีมยูนิต 4 คนที่ติดอยู่ในฐาน และต้องหา **Core Item** ให้ได้ภายใน **7 วัน** ในแต่ละวันต้องตัดสินใจว่าจะใช้ **Action Point (AP)** ที่มีอยู่น้อยนิดไปกับ **การออกสำรวจถ้ำ** (ได้ของดีแต่เสี่ยงบาดเจ็บหรือหายตัว ตัดสินด้วยการทอย D100) หรือกับ **งานในฐาน** (ทำอาหาร รักษา ทำยา) ทั้งหมดนี้ภายใต้แรงกดดันของอาหารที่ต้องจ่ายทุกคืน

**คำถามหลักของผู้เล่น:** วันนี้จะเสี่ยงส่งใครไปไหน แล้วจะเหลือ AP ไว้เลี้ยงทุกคนให้รอดถึงวันที่หา Core Item เจอได้อย่างไร

### Target Audience
- ผู้เล่นที่ชอบ resource management แบบ turn-based และการตัดสินใจภายใต้ความเสี่ยง (Darkest Dungeon, FTL, This War of Mine)
- ชอบระบบทอยเต๋าแบบ TRPG (roll-under D100 แบบ Call of Cthulhu)

### Genre & Platform
- **Genre:** Turn-based Resource Management + Risk/Dice Expedition
- **Perspective:** UI แมปมุมบน (ฐานกลางจอ ถ้ำกระจายรอบๆ)
- **Engine / Platform:** Unity (UGUI + TextMeshPro, URP 2D) — PC
- **Session Length:** 1 run = 7 วัน (เกมวัน) ≈ 10–15 นาที

### Design Pillars

| Pillar | ความหมาย | ระบบที่รองรับ |
|---|---|---|
| **AP ทุกแต้มมีค่า** | ทุกการกระทำแย่ง AP ก้อนเดียวกัน (3/คน/วัน) | AP, Base Work vs Gather, Starvation (+AP cost) |
| **ความเสี่ยงที่อ่านออก** | ผู้เล่นเห็นโอกาสก่อนตัดสินใจ และรับผลได้ทั้งดีและร้าย | D100 roll-under, Aptitude, Duo bonus, Trait |
| **อาหารไม่เคยพอ** | ทุกคืนต้องจ่ายอาหาร ขาดแล้วทุกอย่างแพงขึ้นจนเสียคน | Food, Starvation stack, Lost Signal |
| **เส้นตายบีบให้เสี่ยง** | ถ้ำง่ายปลอดภัยแต่ไม่พาไปถึง Core Item | 7-day deadline, Core Item ต้องผ่าน Normal → Hard |

### Unique Selling Points
- **ทอย D100 แบบ roll-under 4 ระดับผล** (Hard Success / Regular / Failure / Fumble) — ผลแต่ละระดับให้ทั้งของ, บาดแผล และ trait ถาวร
- **ยูนิตเปลี่ยนไปตามประสบการณ์** — ทริปที่ดีหรือแย่ให้ trait ที่ติดตัวไปตลอด run
- **ความหิวเป็นภาษีของ AP** — หิว 1 stack = ทุกการกระทำแพงขึ้น 1 AP, 3 stack = ยูนิตขาดการติดต่อ (Lost Signal)

---

## 2. Game Loop

### Core Loop (1 วัน)

```mermaid
flowchart TD
    Start([เริ่มวัน — ทุกคน AP 3]) --> Plan{ผู้เล่นเลือกทำ}
    Plan -->|ส่งสำรวจ| G[เลือกถ้ำ + 1–2 ยูนิต → Dispatch]
    G --> Roll[ทอย D100 → ได้ของ / บาดเจ็บ / trait]
    Roll --> Plan
    Plan -->|งานในฐาน| B[ทำอาหาร / รักษา / ทำยา]
    B --> Plan
    Plan -->|End Day| Eat[ทุกคนในฐานกินอาหารคนละ 1<br/>ไม่มีกิน → Starvation +1]
    Eat --> Check{ตรวจผล}
    Check -->|มี Core Item| Win([ชนะ])
    Check -->|ทุกคน Lost Signal| Lose1([แพ้])
    Check -->|เลยวันที่ 7| Lose2([แพ้ — หมดเวลา])
    Check -->|ยังไม่จบ| Start
```

### Run Loop (เป้าหมายระยะยาว)

```mermaid
flowchart LR
    A[ถ้ำ Easy<br/>วัตถุดิบอาหาร] --> F[ทำอาหาร<br/>เลี้ยงทีม]
    B[ถ้ำ Normal<br/>ยา] --> H[รักษา]
    B -->|Hard Success| U[ปลดล็อก<br/>วัสดุ Core Item]
    C[ถ้ำ Hard<br/>Materials] --> W[Workshop ทำยา]
    U --> C2[ถ้ำ Hard<br/>Hard Success]
    C2 --> Core([ได้ Core Item → ชนะทันที])
```

### Win / Lose Conditions

| เงื่อนไข | ผล |
|---|---|
| ได้ Core Item ภายในวันที่ 7 | **WIN** (ชนะทันทีที่ได้) |
| กด End Day ในวันที่ 7 แล้วยังไม่มี Core Item | **LOSE** — หมดเวลา |
| ยูนิตทั้ง 4 คน Lost Signal (Missing) | **LOSE** — ไม่เหลือใคร |

---

## 3. Technical Stack

| Layer | Technology | Notes |
|---|---|---|
| Engine | Unity (URP) | โฟลเดอร์ระบบหลัก `Assets/V2/` |
| Language | C# | MonoBehaviour singleton + plain serializable data class |
| UI | Unity UGUI + TextMeshPro | Canvas 1920×1080 Scale With Screen Size |
| Input | Unity Input System (`InputSystemUIInputModule`) | |
| Scene | `V2/Scenes/Core.unity` | scene เดียวสำหรับทั้ง run |

รายละเอียดสถาปัตยกรรม: [Unity V2 System Design](../software/04-unity-system-design.md) · [Class Diagram](../software/05-unity-class-diagram.md) · [Core Scene Hierarchy](../software/06-unity-core-scene-hierarchy.md)

---

## 4. System Overview

| ระบบ | สรุป | รายละเอียด |
|---|---|---|
| **Unit** | 4 ยูนิตคงที่ มี AP, Aptitude/Non-aptitude, Trait, สถานะ 5 แบบ | [Mechanics §2](01-mechanics.md#2-unit-system) |
| **Day / AP** | วันละ 3 AP ต่อคน, End Day = กินอาหาร + รีเฟรช AP | [Mechanics §3](01-mechanics.md#3-day-system) |
| **Send-to-Gather** | ส่ง 1–2 คนไปถ้ำ 3 ระดับ ทอย D100 ตัดสินผล | [Mechanics §4](01-mechanics.md#4-send-to-gather--การออกสำรวจ) |
| **Base Work** | ทำอาหาร / รักษา / Workshop ทำซ้ำได้ไม่จำกัด | [Mechanics §5](01-mechanics.md#5-base-work--งานในฐาน) |
| **Starvation** | ไม่ได้กิน = +1 stack (+1 AP ทุกการกระทำ), 3 stack = Lost Signal | [Mechanics §6](01-mechanics.md#6-starvation--ความหิว) |
| **Core Item** | Hard Success ที่ Normal → ปลดล็อก, Hard Success ที่ Hard → ได้ของ | [Mechanics §7](01-mechanics.md#7-core-item--เงื่อนไขชนะ) |

---

## 5. Design Evolution

| Version | Concept | สถานะ | Source |
|---|---|---|---|
| **v2.0 Unity V2** | Turn-based AP + D100 expedition, 4 ยูนิตคงที่, หา Core Item ใน 7 วัน, ไม่มีช่วงกลางคืน | **ปัจจุบัน** | `Unity-Projects/Assets/V2/` |
| v1.5 Unity Day/Night | Ship Navigator + Day scene (crew mission, real-time clock) ↔ Night scene (tower defense) | เลิกใช้ (legacy) | `Unity-Projects/Assets/Day/`, `Assets/Night/` |
| v1.0 Web Prototype | Real-time mission chain + night monster attack, รอด 30 วัน | เลิกใช้ (ระยะที่ 1) | `prototype_resource_game/`, [GDD archive](../wiki/archive/gdd-v1-prototype/00-concept.md) |
| v0.3 Concept | Turn-based Strategy + Tower Defense — Ship → Day → Night + Node Map Roguelike | แนวคิด | [Clogged_GDD_v0.3](https://docs.google.com/document/d/1SGxMLKs7FlRq_E-0OHyskQbingBxaU5L/edit) |

**สิ่งที่เปลี่ยนจาก v1.0 → v2.0**
- ตัด real-time clock และ mission chain ออก → เปลี่ยนเป็น **AP ต่อวัน** (turn-based)
- ตัด **ช่วงกลางคืน / ป้องกันฐาน** และ Base HP ออกทั้งหมด
- ผลการสำรวจจาก proficiency + สูตรต่อเนื่อง → **ทอย D100 แบบแบ่งระดับผล**
- ลูกเรือสุ่ม/จ้างได้ → **4 ยูนิตคงที่** ที่โตขึ้นด้วย Trait
- รอด 30 วัน → **หา Core Item ใน 7 วัน** (เป้าหมายเชิงรุก ไม่ใช่แค่ประคองตัว)
- ทรัพยากร 7 ชนิด + ชิ้นส่วนสัตว์ → **4 ชนิด** (Food, Cooking Material, Medicine, Materials)

---

## 6. Open Questions

- หน้าจอจบเกม (Win/Lose) ยังไม่มี — ต้องออกแบบสิ่งที่แสดงหลังจบ run
- ปุ่ม "ให้อาหารลด Starvation" มีระบบแล้วแต่ยังไม่มี UI
- Aptitude `Speed`, `Endurance`, `Gathering`, `Combat` ยังไม่มีผล — จะผูกกับกลไกอะไร
- Trait ทุกตัวให้ ±10 เท่ากัน — จะให้ trait แต่ละชื่อมีผลเฉพาะหรือไม่
- Lore ของโลก, ฐาน และ Core Item ยังไม่กำหนด (ดู [Narrative](02-narrative.md))

---

## Related Documents
- [Core Mechanics](01-mechanics.md)
- [Narrative & World](02-narrative.md) · [Art Direction](03-art-direction.md) · [Audio Direction](04-audio-direction.md)
- [Unity V2 System Design](../software/04-unity-system-design.md)
- [Product Backlog](../agile/01-product-backlog.md)
