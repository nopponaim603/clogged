# OmniRoute Docker Setup

โฟลเดอร์นี้ประกอบด้วยการตั้งค่าและสคริปต์สำหรับรัน **OmniRoute** ผ่าน Docker เพื่อใช้เป็น AI Gateway (Proxy) ในการพัฒนาโปรเจกต์นี้

> ⚠️ **หมายเหตุ:** OmniRoute ใช้พอร์ต `20128` เดียวกับ [9router](../9router) จึงรันพร้อมกันทั้งสองตัวไม่ได้ในเครื่องเดียวกัน (เว้นแต่จะแก้ mapping พอร์ตในไฟล์ `docker-compose.yml`) ให้เลือกใช้งานอย่างใดอย่างหนึ่งเป็นหลัก

---

## OmniRoute คืออะไร?
**OmniRoute** เป็น AI Gateway/Proxy แบบ Open-Source ฟรี ที่รวมการเชื่อมต่อกับ AI Provider กว่า 250 รายการ (เช่น OpenAI, Anthropic, Gemini, DeepSeek) ผ่านจุดเชื่อมต่อเดียว โดยมีความสามารถหลักดังนี้:
* **Auto-fallback Routing:** สลับโมเดล/ผู้ให้บริการอัตโนมัติแบบ 4 ระดับ (Subscription → API → Cheap → Free) เมื่อเกิดข้อผิดพลาดหรือติดโควตา
* **Token Compression:** มี compression engine 10 แบบ (RTK, Caveman, LLMLingua-2 ฯลฯ) ช่วยประหยัด Token ได้ 15–95%
* **Free-tier Providers:** รองรับผู้ให้บริการที่มี free tier กว่า 90 ราย ไม่ต้องผูกบัตรเครดิต
* **Universal Compatibility:** มี API Endpoint ที่ใช้งานร่วมกับมาตรฐาน OpenAI ได้ ใช้กับ Coding Agent ได้กว่า 20 ตัว (Claude Code, Cursor, Cline, Copilot ฯลฯ)

อ้างอิง: [omniroute.online](https://omniroute.online/) | [GitHub: diegosouzapw/OmniRoute](https://github.com/diegosouzapw/OmniRoute)

---

## โครงสร้างไฟล์ในโฟลเดอร์นี้
* [docker-compose.yml](file:///c:/Users/noppon/source/01-DG/clogged/ai-gateway/omniroute/docker-compose.yml) - ไฟล์กำหนดโครงสร้าง Container ของ OmniRoute
* [setup.cmd](file:///c:/Users/noppon/source/01-DG/clogged/ai-gateway/omniroute/setup.cmd) - สคริปต์แบบคลิกเดียวสำหรับตรวจสอบ Docker, เปิดใช้งาน OmniRoute และเข้าสู่ Dashboard อัตโนมัติ (แนะนำสำหรับ Windows CMD)
* [setup.ps1](file:///c:/Users/noppon/source/01-DG/clogged/ai-gateway/omniroute/setup.ps1) - สคริปต์ทำงานแบบเดียวกันสำหรับผู้ใช้งาน PowerShell

---

## วิธีเริ่มต้นใช้งาน (Quick Start)

### ข้อกำหนดเบื้องต้น (Prerequisites)
* ต้องติดตั้งและเปิดใช้งาน **Docker Desktop** บนเครื่องคอมพิวเตอร์ของคุณ

### วิธีใช้งานสคริปต์แบบขั้นตอนเดียว (ง่ายที่สุด)
1. ดับเบิ้ลคลิกที่ไฟล์ [setup.cmd](file:///c:/Users/noppon/source/01-DG/clogged/ai-gateway/omniroute/setup.cmd) หรือรันคำสั่งด้านล่างนี้ใน Terminal:
   ```powershell
   .\ai-gateway\omniroute\setup.cmd
   ```
2. สคริปต์จะเริ่มทำงาน รัน Container และเปิดบราวเซอร์เข้าสู่ Dashboard ของ OmniRoute ที่ [http://localhost:20128/dashboard](http://localhost:20128/dashboard) ให้โดยอัตโนมัติ
3. ในหน้า Dashboard ให้เชื่อมต่อ Provider ที่ต้องการ (เช่น เลือก free-tier provider ที่ไม่ต้องสมัครสมาชิก) แล้วคัดลอก API Key จาก Dashboard → Endpoints
4. ตั้งค่าเครื่องมือ/Coding Agent ให้ชี้ไปที่ Base URL `http://localhost:20128/v1` พร้อม API Key ที่ได้

---

## ทางเลือกอื่นแทน Docker (npm)

หากไม่ต้องการใช้ Docker สามารถติดตั้งผ่าน npm ได้โดยตรง:
```bash
npm install -g omniroute
omniroute setup      # guided wizard สำหรับตั้งค่าครั้งแรก
omniroute            # รันเซิร์ฟเวอร์
omniroute doctor     # ตรวจสอบสถานะ provider และการเชื่อมต่อ
```

---

## คำสั่งควบคุมแบบแมนนวล (Manual Commands)

คุณสามารถควบคุม Container ผ่าน Docker CLI ได้ด้วยการเข้าไปที่โฟลเดอร์นี้ (`cd ai-gateway/omniroute`) และใช้คำสั่งทั่วไป:

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

## Environment Variables ที่รองรับ (สำหรับการรันแบบ from source / npm)

| ตัวแปร | ค่าเริ่มต้น | คำอธิบาย |
|---|---|---|
| `PORT` | `20128` | พอร์ตของ API และ Dashboard |
| `REQUIRE_API_KEY` | `false` | บังคับให้ต้องยืนยันตัวตนด้วย API Key |
| `DATA_DIR` | `~/.omniroute` | ตำแหน่งจัดเก็บฐานข้อมูล/การตั้งค่า |

---

## การเก็บข้อมูลและการรักษาความปลอดภัย (Data Directory)
* ข้อมูลการตั้งค่าและ API Keys ของ OmniRoute จะถูกจัดเก็บไว้ในโฟลเดอร์ `ai-gateway/omniroute/data` บนเครื่องของคุณ
* โฟลเดอร์ `ai-gateway/omniroute/data/` นี้ถูกระบุไว้ในไฟล์ [.gitignore](file:///c:/Users/noppon/source/01-DG/clogged/.gitignore) แล้ว เพื่อป้องกันไม่ให้ข้อมูลสำคัญเหล่านี้หลุดลอยขึ้นไปบน Git Repository หรือ GitHub ของคุณ
