# clogged — Audio Direction

**Version:** 2.1 (Unity V2) | **Last Updated:** 2026-10-29
> แผนเสียงสำหรับระบบ V2 — แผนเดิมของ Web Prototype (เสียงกลางคืน/monster) ดูที่ [archive](../wiki/archive/gdd-v1-prototype/04-audio-direction.md)

## 1. Current State: NO GAMEPLAY AUDIO

ใน `Core.unity` มี `AudioListener` บน Main Camera แต่ไม่พบ `AudioSource`, music หรือ gameplay SFX ใน V2. Main Menu มี volume slider ที่ปรับ `AudioListener.volume` และบันทึก preference; slider ยังไม่ต่อกับ asset เสียงหรือ AudioMixer.

---

## 2. Audio Design Proposal

### 2.1 Music

| ช่วง | อารมณ์ | Tempo | หมายเหตุ |
|---|---|---|---|
| วางแผนในฐาน | ตึงเครียดแต่มีสมาธิ | ช้า–กลาง | loop ที่ไม่รบกวนการตัดสินใจ |
| วันท้ายๆ (วันที่ 6–7) | กดดันขึ้น | กลาง | เพิ่มชั้นเครื่องดนตรี/จังหวะ บอกว่าเส้นตายใกล้ |
| ยืนยันออกสำรวจ / ผล D100 | ลุ้น | — | cue สั้น; การ resolve ในเกมเกิดทันที |
| ได้กล่องและกุญแจครบ | โล่งใจ มีความหวัง | กลาง | ประกอบกับ crystal reveal |
| แพ้ | เศร้า เงียบ | ช้ามาก | หมดเวลา vs ทีมหายหมด อาจใช้ cue ต่างกัน |

### 2.2 Sound Effects

| เหตุการณ์ | SFX ที่เสนอ | จุดเชื่อมในโค้ด |
|---|---|---|
| คลิก cave / ปุ่ม UI | blip / click | `MapNodeView`, UI controls |
| ผลการสำรวจ | cue แยก Hard Success / Regular / Failure / Fumble | `GatherDispatchController.OnGatherResolved(result)` |
| บาดเจ็บ / Lost Signal | impact / radio static | `result.hurtUnit`, `result.missingUnit`; starvation loss ผ่าน `DayManager.OnDayEnded(report)` |
| ได้ trait | sparkle / tone ต่ำ | `result.traitAwarded`, polarity |
| ได้กล่อง / กุญแจ | cue แยกสองชิ้น | `result.coreItemsObtained` |
| Cook / Heal / Craft | stove / bandage / hammer | `BaseWorkController.OnBaseWorkCompleted(result)` |
| End Day / ขาดอาหาร | day transition / warning | `DayManager.OnDayEnded(report)` |
| ชนะ / แพ้ | ending stinger | `DayManager.OnRunEnded(outcome)` |
| Resume / เริ่ม run | short system cue | menu/scene flow (หากต้องการ) |

### 2.3 UI Sounds
- กดปุ่ม: คลิกเบาๆ
- action ที่ทำไม่ได้ / warning ขึ้น: buzz ต่ำ
- เลือก toggle / dropdown: tick

---

## 3. Implementation Notes (Unity)

- สร้าง audio service ที่ subscribe gameplay events (`OnGatherResolved`, `OnBaseWorkCompleted`, `OnDayEnded`, `OnRunEnded`) แทนการเพิ่ม playback logic ลงใน UI
- การคลิกปุ่มและ starvation state ยังไม่มี event สำหรับเสียงทุกกรณี; เพิ่ม event ใน owner ของ state เมื่อเริ่ม implement
- เชื่อม volume preference ใน `MainMenuController` กับ mixer/audio groups เมื่อมี audio assets
- เพลง: OGG Vorbis, Load Type = Streaming · SFX: WAV, Load Type = Decompress On Load
- ค่า volume ปัจจุบันใน Main Menu: 80% สำหรับ `AudioListener`; ค่า Music/SFX แยกกลุ่มยังไม่ได้ implement

---

## 4. Audio Priorities

| Priority | Audio | เหตุผล |
|---|---|---|
| **P0** | UI click + feedback ผล gather | ให้ action แบบ resolve ทันทีอ่านผลได้ชัด |
| **P0** | Lost Signal และ Core Item pair | ผลถาวรและจุดเปลี่ยน run ต้องรับรู้ได้ |
| **P1** | Ending cues | ปิด run ให้แยก win/deadline/crew lost |
| **P2** | SFX งานในฐานและ End Day | เพิ่มน้ำหนักให้วงจร resource |
| **P3** | เพลงวางแผนและวันท้ายๆ | เสริมบรรยากาศโดยไม่รบกวนการวางแผน |

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
- [Unity V2 System Design](../software/04-unity-system-design.md)
