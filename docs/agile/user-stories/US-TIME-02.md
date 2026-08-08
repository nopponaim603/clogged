# User Story: US-TIME-02 - Day Cycle Bug Fix & Clean State Reset

**Status:** 🟢 DONE
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ให้การนับวันดำเนินไปทีละ 1 วันอย่างถูกต้อง และสามารถเริ่มเล่นใหม่ (Restart) ได้อย่างสะอาด
**เพื่อให้** เกมวนลูป 30 วันได้ครบถ้วนตามกฎ Win Condition และไม่มี Bug ค่า State ตกค้างเมื่อแพ้หรือชนะแล้วกดเริ่มใหม่

---

## ✅ Acceptance Criteria
1. [x] วันในเกมเพิ่มขึ้นทีละ 1 วันหลังผ่าน Night Phase (Day 1 → Day 2 → Day 3 ... → Day 30)
2. [x] เมื่อชนะในวันที่ 30 (`Day > 30`) แสดงหน้าจอ GameOverScene พร้อมข้อความชนะ (YOU WIN)
3. [x] เมื่อกด Restart จาก GameOverScene ค่าตัวแปรใน `TimeSystem`, `ResourceManager`, และ `CrewManager` ถูก Reset สะอาด
4. [x] เมื่อภารกิจในคิว (`missionQueue`) ที่เตรียมไว้ทำเสร็จสิ้นทั้งหมดแล้ว ให้ระบบส่งต่อไปยัง Night Phase ทันทีอัตโนมัติ (`this.timeSystem.endDay()`) โดยไม่ค้างเวลาค้างเฟส

---

## 🛠 Technical Tasks (Git Log Updates)
- [x] `systems/TimeSystem.ts` — ตรวจสอบและแก้ไขการเรียก `day++` ซ้ำซ้อนระหว่าง `TimeSystem.endNight()` และ `GameScene.startNewDay()`
- [x] `scenes/MainScene.ts` / `scenes/DayScene.ts` — แก้ไข `finishExecution()` ให้เรียก `this.timeSystem.endDay()` ทันทีเมื่อ `missionQueue` เสร็จสิ้นทั้งหมด เพื่อป้องกันเกมค้าง
- [x] `scenes/DayScene.ts` — ปรับปรุงการ Reset state ใน `initSystems()` และการเปลี่ยน Scene ไปยัง `GameOverScene`

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Game Loop](../../gdd/00-concept.md#2-game-loop)
- Software: [System Design — TimeSystem](../../software/01-system-design.md)
