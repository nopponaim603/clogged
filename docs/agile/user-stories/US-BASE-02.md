# User Story: US-BASE-02 - Night Phase Base Defense & Escalation

**Status:** 🏗️ In-Progress
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ให้ลูกเรือที่ประจำอยู่ที่ฐานมีส่วนร่วมในการป้องกันมอนสเตอร์บุกตอนกลางคืน และมีความท้าทายเพิ่มขึ้นตามจำนวนวัน
**เพื่อให้** คืนกลางคืนมีความกดดันและใช้พลังของกองกำลังลูกเรืออย่างมีประสิทธิภาพ

---

## ✅ Acceptance Criteria
1. [ ] ลูกเรือที่อยู่ในฐานช่วง Night Phase จะใช้วุฒิภาวะต่อสู้ (`Hunting` / `Attack Power`) ช่วยยิงสกัดมอนสเตอร์ก่อนถึงฐาน
2. [ ] ความยากของฝูงมอนสเตอร์ (Monster Wave) เพิ่มขึ้นตามจำนวนวัน (Day 1–10: เล็กน้อย, Day 11–20: ปานกลาง, Day 21–30: มอนสเตอร์ระดับบอส)
3. [ ] ความเสียหายของฐาน (`Base HP Loss`) และการสูญเสียทรัพยากร ถูกคำนวณจากพลังโจมตีมอนสเตอร์ที่ทะลวงผ่านการป้องกันเข้ามาได้จริง

---

## 🛠 Technical Tasks (Git Log Updates)
- [ ] `entities/Monster.ts` — เพิ่มคุณสมบัติ Attack Power, Speed และ Resource Loot Type ให้มอนสเตอร์แต่ละประเภท
- [ ] `scenes/MainScene.ts` — เพิ่มระบบสุ่มการสกัดจับของลูกเรือในฐาน (`spawnNightMonsters` & Defense Calculation)
- [ ] `systems/ResourceManager.ts` — ปรับปรุงสูตรคำนวณการสูญเสียทรัพยากรตามประเภทมอนสเตอร์ที่บุกเข้าถึงฐาน

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Game Loop](../../gdd/00-concept.md#2-game-loop)
- Mechanics: [Core Mechanics — Night Phase](../../gdd/01-mechanics.md)
