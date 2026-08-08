# User Story: US-MISSION-02 - Collaborative Mission System

**Status:** 🟢 DONE
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ส่งลูกเรือมากกว่า 1 คนไปยังจุดหมาย (Resource Node) เดียวกันพร้อมกัน
**เพื่อให้** ความถนัด (Proficiency) ของลูกเรือรวมกัน ช่วยลดเวลาการทำงาน (Action Time) และเพิ่มโอกาสสำเร็จในภารกิจยากๆ

---

## ✅ Acceptance Criteria
1. [x] ผู้เล่นสามารถมอบหมายลูกเรือหลายคนไปยังเป้าหมาย (Node) เดียวกันในรอบการวางแผนเดียวกันได้
2. [x] เมื่อลูกเรือซ้อนทับกันที่ Node เดียวกัน ระบบคำนวณ `Combined Proficiency = Proficiency_Crew1 + Proficiency_Crew2`
3. [x] เวลาการทำงาน (`Action Time`) ของ Node นั้นลดลงตามสัดส่วนของ Combined Proficiency
4. [x] มี Visual Indicator / UI และ Travel Phase แสดงบน UI ของ Node ที่มี Collaborative Mission

---

## 🛠 Technical Tasks (Git Log Updates)
- [x] `systems/MissionSystem.ts` / `scenes/DayScene.ts` — เพิ่มฟังก์ชันตรวจสอบ Multi-crew บน Target เดียวกันและคำนวณ Combined Proficiency
- [x] `scenes/TravelScene.ts` — เพิ่ม Scene แสดงผลการเดินทางของลูกเรือไปยัง Node
- [x] `ui/ResultPopup.ts` — เพิ่มการแสดงผลสรุปผลลัพธ์ Collaborative Mission หลังเดินทางกลับ

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Unique Selling Points](../../gdd/00-concept.md#unique-selling-points-usp)
- Mechanics: [Core Mechanics — Mission System](../../gdd/01-mechanics.md)
