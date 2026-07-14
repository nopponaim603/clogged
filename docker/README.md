# 9Router Docker Setup

โฟลเดอร์นี้ประกอบด้วยการตั้งค่าและสคริปต์สำหรับรัน **9Router** ผ่าน Docker เพื่อใช้เป็น AI Gateway (Proxy) ในการพัฒนาโปรเจกต์นี้

---

## 9Router คืออะไร?
**9Router** เป็น AI Gateway/Proxy แบบ Open-Source ที่ช่วยให้การเชื่อมต่อกับ AI Provider และโมเดลต่าง ๆ (เช่น Claude, OpenAI, Gemini) ผ่าน Coding Agents (เช่น Cursor, Cline, Claude Code) ทำได้อย่างมีประสิทธิภาพมากขึ้น โดยมีความสามารถหลักดังนี้:
* **Fallback Routing:** สลับโมเดลหรือ API Key สำรองให้อัตโนมัติเมื่อเกิดข้อผิดพลาดหรือติดขัดโควตา (Rate Limit)
* **Rust Token Killer (RTK):** เทคโนโลยีบีบอัดข้อมูล Token (เช่น Logs, Git Diff) ให้เล็กลง 20–40% ช่วยให้ประหยัดโควตาและประหยัดค่าใช้จ่ายได้มากขึ้น
* **Universal Compatibility:** มี API Endpoint ที่ใช้งานร่วมกับมาตรฐาน OpenAI ได้ ทำให้ใช้กับเครื่องมือพัฒนาเกือบทุกตัวได้ทันที

---

## โครงสร้างไฟล์ในโฟลเดอร์นี้
* [docker-compose.yml](file:///c:/Users/noppon/sources/01_DG/clogged/docker/docker-compose.yml) - ไฟล์กำหนดโครงสร้าง Container ของ 9Router
* [setup.cmd](file:///c:/Users/noppon/sources/01_DG/clogged/docker/setup.cmd) - สคริปต์แบบคลิกเดียวสำหรับตรวจสอบ Docker, เปิดใช้งาน 9Router และเข้าสู่ Dashboard อัตโนมัติ (แนะนำสำหรับ Windows CMD)
* [setup.ps1](file:///c:/Users/noppon/sources/01_DG/clogged/docker/setup.ps1) - สคริปต์ทำงานแบบเดียวกันสำหรับผู้ใช้งาน PowerShell

---

## วิธีเริ่มต้นใช้งาน (Quick Start)

### ข้อกำหนดเบื้องต้น (Prerequisites)
* ต้องติดตั้งและเปิดใช้งาน **Docker Desktop** บนเครื่องคอมพิวเตอร์ของคุณ

### วิธีใช้งานสคริปต์แบบขั้นตอนเดียว (ง่ายที่สุด)
1. ดับเบิ้ลคลิกที่ไฟล์ [setup.cmd](file:///c:/Users/noppon/sources/01_DG/clogged/docker/setup.cmd) หรือรันคำสั่งด้านล่างนี้ใน Terminal:
   ```powershell
   .\docker\setup.cmd
   ```
2. สคริปต์จะเริ่มทำงาน รัน Container และเปิดบราวเซอร์เข้าสู่ Dashboard ของ 9Router ที่ [http://localhost:20128](http://localhost:20128) ให้โดยอัตโนมัติ

---

## คำสั่งควบคุมแบบแมนนวล (Manual Commands)

คุณสามารถควบคุม Container ผ่าน Docker CLI ได้ด้วยการเข้าไปที่โฟลเดอร์นี้ (`cd docker`) และใช้คำสั่งทั่วไป:

### เปิดใช้งาน
```bash
docker compose up -d
```

### ปิดใช้งาน
```bash
docker compose down
```

### ตรวจสอบสถานะการทำงาน
```bash
docker compose ps
```

---

## การเก็บข้อมูลและการรักษาความปลอดภัย (Data Directory)
* ข้อมูลการตั้งค่าและ API Keys ของ 9Router จะถูกจัดเก็บไว้ในโฟลเดอร์ `docker/data` บนเครื่องเครื่องของคุณ
* โฟลเดอร์ `docker/data/` นี้ถูกระบุไว้ในไฟล์ [.gitignore](file:///c:/Users/noppon/sources/01_DG/clogged/.gitignore) แล้ว เพื่อป้องกันไม่ให้ข้อมูลสำคัญเหล่านี้หลุดลอยขึ้นไปบน Git Repository หรือ GitHub ของคุณ
