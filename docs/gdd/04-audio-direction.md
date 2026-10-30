# clogged — Audio Direction

**Version:** 2.0 (Unity V2) | **Last Updated:** 2026-10-14
> แผนเสียงสำหรับระบบ V2 — แผนเดิมของ Web Prototype (เสียงกลางคืน/monster) ดูที่ [archive](../wiki/archive/gdd-v1-prototype/04-audio-direction.md)

## 1. Current State: NO AUDIO

`Core.unity` มีแค่ `AudioListener` บน Main Camera — **ไม่มี AudioSource, เพลง หรือ SFX** ใดๆ

---

## 2. Proposed Audio Design

### 2.1 Music

| ช่วง | อารมณ์ | Tempo | หมายเหตุ |
|---|---|---|---|
| วางแผนในฐาน (วันที่ 1–5) | ตึงเครียดแต่มีสมาธิ | ช้า–กลาง | loop ยาว ฟังซ้ำได้ไม่เบื่อ เพราะผู้เล่นคิดได้ไม่จำกัดเวลา |
| วันท้ายๆ (วันที่ 6–7) | กดดันขึ้น | กลาง | เพิ่มชั้นเครื่องดนตรี/จังหวะ บอกว่าเส้นตายใกล้ |
| Scout เดินทาง | ลุ้น | — | ลด volume เพลงหลัก + เสียงฝีเท้า/ambient ถ้ำ |
| ชนะ (ได้ Core Item) | โล่งใจ มีความหวัง | กลาง | |
| แพ้ | เศร้า เงียบ | ช้ามาก | หมดเวลา vs ทีมหายหมด อาจใช้ cue ต่างกัน |

### 2.2 Sound Effects (ผูกกับเหตุการณ์ที่มีอยู่ในระบบแล้ว)

| เหตุการณ์ | SFX ที่เสนอ | จุดเชื่อมในโค้ด |
|---|---|---|
| คลิกถ้ำ | blip สั้น (ระดับเสียงต่างกันตามระดับถ้ำ) | `MapNodeView.OnAnyNodeClicked` |
| กด Dispatch / Scout ออกเดิน | ฝีเท้า / whoosh | `GatherMapController.ConfirmTrip` |
| **ทอยเต๋า** | เสียงเต๋ากลิ้ง → เสียงผล | `GatherDispatchController.OnGatherResolved` |
| Hard Success | chime สว่าง | `result.tier` |
| Regular | tone บวกสั้นๆ | |
| Failure + ยูนิตบาดเจ็บ | กระแทก / ร้องเจ็บ | `result.hurtUnit` |
| Fumble / **Lost Signal** | สัญญาณวิทยุขาดหาย (static → เงียบ) | `result.missingUnit`, starvation ครบ 3 |
| ได้ Trait | ดี: sparkle · เสีย: เสียงต่ำ | `result.traitAwarded` |
| ปลดล็อก / ได้ Core Item | stinger พิเศษ | `result.unlockedCoreItemMaterial` / `obtainedCoreItem` |
| Cook / Heal / Craft | กระทะ / ผ้าพันแผล / ค้อน | `BaseWorkPanel.OnConfirm` |
| End Day — กินอาหาร | เสียงกิน (1 ครั้ง/คน) | `DayManager.EndDay` |
| ยูนิตหิว (+1 stack) | ท้องร้อง | `UnitCharacter.ApplyStarvationStack` |
| เริ่มวันใหม่ | ระฆัง / นกร้อง | `DayManager.OnDayStarted` |
| ชนะ / แพ้ | stinger จบเกม | `DayManager.OnRunEnded` |

### 2.3 UI Sounds
- กดปุ่ม: คลิกเบาๆ
- ปุ่มที่กดไม่ได้ / เหตุผลสีแดงขึ้น: buzz ต่ำ
- เลือก toggle / dropdown: tick

---

## 3. Implementation Notes (Unity)

- ทำ `AudioManager` (singleton แบบเดียวกับระบบอื่นใน V2) ที่ subscribe event ในตาราง §2.2 — **ไม่ต้องแก้ controller เดิม** เพราะ event ส่วนใหญ่มีอยู่แล้ว
- เหตุการณ์ที่ยังไม่มี event (Base Work, starvation stack, คลิกปุ่ม) เรียกจาก UI โดยตรง หรือเพิ่ม event ใน controller
- ใช้ **AudioMixer** แยก group `Music` / `SFX` / `UI` เพื่อทำ volume slider ทีหลัง
- เพลง: OGG Vorbis, Load Type = Streaming · SFX: WAV, Load Type = Decompress On Load
- ค่าเริ่มต้น: Music 70%, SFX 100%

---

## 4. Audio Priorities

| Priority | Audio | เหตุผล |
|---|---|---|
| **P0** | เสียงทอยเต๋า + ผล 4 ระดับ | ช่วงเวลาที่ลุ้นที่สุดของเกม |
| **P0** | Lost Signal | การเสียยูนิตถาวรต้องรู้สึกหนัก |
| **P1** | UI click / error | feedback พื้นฐาน |
| **P1** | Core Item stinger + เพลงชนะ/แพ้ | ปิด run ให้มีความหมาย |
| **P2** | เพลงวางแผน (loop) | บรรยากาศ |
| **P2** | SFX งานในฐาน, End Day | จังหวะของวัน |
| **P3** | เพลงวันท้ายๆ แบบเพิ่มชั้น | ความกดดันของเส้นตาย |

---

## 5. Reference Games

| เกม | สิ่งที่เรียนรู้ |
|---|---|
| **Darkest Dungeon** | เสียงผลลัพธ์ที่หนักแน่น + เสียงบรรยายสถานะความเครียด |
| **FTL** | ambient ตึงเครียด SFX น้อยแต่ชัด |
| **This War of Mine** | บรรยากาศหดหู่ของการเอาชีวิตรอดในฐาน |
| **Call of Cthulhu (เกม/TRPG)** | การทอย D100 ที่ลุ้นและเสียงตอบสนองผล |

## Related Documents
- [Game Concept](00-concept.md)
- [Core Mechanics](01-mechanics.md)
