# clogged

> โปรเจกต์พัฒนาเกมแนว **Resource Management + Action Survival** ด้วย Phaser 3 + TypeScript

**clogged** เป็นเกมที่เน้นการ **ออกแบบและพัฒนาระบบ** (systems-driven) มากกว่าเนื้อเรื่องหรือกราฟิก ผู้เล่นบริหารฐานและทีมลูกเรือที่มีจำกัด ส่งออกไปเก็บทรัพยากร ค้นหาโบราณวัตถุ และล่ามอนสเตอร์ในตอนกลางวัน แล้วป้องกันฐานจากการโจมตีในตอนกลางคืน

ชื่อ *clogged* (อุดตัน) คือธีมหลักของเกม — ว่าด้วย **"การไหลของทรัพยากร" ที่ต้องไม่ให้ตัน** ทรัพยากรที่เก็บเกินความจำเป็นจะเน่าเสียเมื่อจบวัน ส่วนที่ขาดจะทำให้ลูกเรืออดอาหาร

## แนวเกม (Genre)

- **แกนหลัก:** Resource Management + Action Survival (crew dispatch, day/night cycle)
- รายละเอียดกลไกเต็ม: [Core Mechanics](docs/gdd/01-mechanics.md)

> หมายเหตุ: ทิศทางนี้เป็นผลจากการปรับแผนระหว่างพัฒนา — ร่างไอเดียตั้งต้น (factory/conveyor pipeline) ถูกเก็บไว้ที่ [wiki/archive](docs/wiki/archive/idea-design-draft.md) ดู [Design Pivot](docs/gdd/00-concept.md#2-️-design-pivot--บันทึกไว้เพื่อความชัดเจน)

## สถานะโปรเจกต์

🔵 **เฟส 1 — Prototype / Vertical Slice** (2026-07-13 → 2026-08-09) ดู [Sprint 01](docs/agile/sprint-backlogs/sprint-01.md)

## เอกสาร

ดูสารบัญเอกสารทั้งหมดที่ [docs/index.md](docs/index.md) — ไฮไลต์:

- [GDD — Concept & Architecture](docs/gdd/00-concept.md) / [Core Mechanics](docs/gdd/01-mechanics.md)
- [Software — System Design](docs/software/01-system-design.md) / [Class Diagram](docs/software/02-class-diagram.md)
- [Product Backlog](docs/agile/01-product-backlog.md) / [Sprint Planning & Roadmap](docs/agile/02-sprint-planning.md)
- [ทีมพัฒนา](docs/agile/team.md)

## การพัฒนา

- **Engine:** [Phaser 3](https://phaser.io/) (TypeScript + Vite)
- โค้ด prototype อยู่ที่ [`prototype_resource_game/`](prototype_resource_game/)
  ```bash
  cd prototype_resource_game
  npm install
  npm run dev
  ```
