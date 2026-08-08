# User Story: US-BASE-02 - Night Phase Base Defense & Escalation

**Status:** 🟢 DONE
**Epic:** [Product Backlog](../01-product-backlog.md)
**Owner:** ทีม clogged

---

## 📖 Description
**ในฐานะ** ผู้เล่น
**ฉันต้องการ** ให้ลูกเรือที่ประจำอยู่ที่ฐานมีส่วนร่วมในการป้องกันมอนสเตอร์บุกตอนกลางคืน และมีความท้าทายเพิ่มขึ้นตามจำนวนวัน
**เพื่อให้** คืนกลางคืนมีความกดดันและใช้พลังของกองกำลังลูกเรืออย่างมีประสิทธิภาพ

---

## ✅ Acceptance Criteria
1. [x] ลูกเรือที่อยู่ในฐานช่วง Night Phase มีส่วนร่วมในการยิงสกัดมอนสเตอร์ก่อนถึงฐาน
2. [x] ความยากของฝูงมอนสเตอร์ (Monster Wave) เพิ่มขึ้นตามจำนวนวัน
3. [x] ความเสียหายของฐาน (`Base HP Loss`) คำนวณจากพลังโจมตีมอนสเตอร์ที่เล็ดลอดทะลวงการป้องกันเข้ามาได้จริง

---

## 🛠 Technical Tasks (Git Log Updates)
- [x] `prototype_resource_game/src/scenes/NightScene.ts` — เพิ่มระบบ Night Defense, Monster Wave Spawning, Turrets & Result Popup
- [x] `Unity-Projects/Assets/Night/VS/Enemy/` — เพิ่ม `EnemyManager.cs`, `EnemySpawner.cs`, `EnemyMove&Attack.cs`, `EnemyHealth.cs`
- [x] `Unity-Projects/Assets/Night/VS/PlayerUnit/` — เพิ่ม `Turret.cs`, `PlayerUnit.cs`, `BaseHealth.cs`, `PlayerHealth.cs`

---

## 🔗 Related Files
- Backlog: [Product Backlog](../01-product-backlog.md)
- GDD: [Concept — Game Loop](../../gdd/00-concept.md#2-game-loop)
- Mechanics: [Core Mechanics — Night Phase](../../gdd/01-mechanics.md)
