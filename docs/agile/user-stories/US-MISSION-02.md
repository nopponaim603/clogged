# User Story: US-MISSION-02 - Collaborative Mission System

**Status:** 🏗️ In-Progress
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ส่งลูกเรือมากกว่า 1 คนไปยังจุดหมาย (Resource Node) เดียวกันพร้อมกัน
**เพื่อให้** ความถนัด (Proficiency) ของลูกเรือรวมกัน ช่วยลดเวลาการทำงาน (Action Time) และเพิ่มโอกาสสำเร็จในภารกิจยากๆ

---

## ✅ Acceptance Criteria
1. [ ] ผู้เล่นสามารถมอบหมายลูกเรือหลายคนไปยังเป้าหมาย (Node) เดียวกันในรอบการวางแผนเดียวกันได้
2. [ ] เมื่อลูกเรือซ้อนทับกันที่ Node เดียวกัน ระบบคำนวณ `Combined Proficiency = Proficiency_Crew1 + Proficiency_Crew2`
3. [ ] เวลาการทำงาน (`Action Time`) ของ Node นั้นลดลงตามสัดส่วนของ Combined Proficiency
4. [ ] มี Visual Indicator (เช่น ไอคอน 🤝) แสดงบน UI ของ Node ที่มี Collaborative Mission

---

## 🛠 Technical Tasks (Git Log Updates)
- [ ] `systems/MissionSystem.ts` — เพิ่มฟังก์ชันตรวจสอบ Multi-crew บน Target เดียวกันและคำนวณ Combined Proficiency
- [ ] `scenes/MainScene.ts` — ปรับปรุงการประมวลผล Queue และการอัปเดตสไปรต์ของลูกเรือที่ทำภารกิจร่วมกัน
- [ ] `ui/MissionDisplay.ts` — เพิ่มการแสดงผลสถานะ Collaborative Mission บน UI Panel

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Unique Selling Points](../../gdd/00-concept.md#unique-selling-points-usp)
- Mechanics: [Core Mechanics — Mission System](../../gdd/01-mechanics.md)
