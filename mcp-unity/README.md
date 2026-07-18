# 🛠️ Local MCP Unity Bridge

โฟลเดอร์นี้เป็นศูนย์กลางการติดตั้งและใช้งาน **MCP Unity Server** เพื่อเชื่อมต่อ AI Clients (เช่น Kilo, Claude Code, Cursor, OpenCode) เข้ากับ Unity Editor ในการพัฒนาเกม **clogged**

> **ทำไมต้องแยกจาก ai-gateway?**
> 1. เพื่อลดความซ้ำซ้อนใน `ai-gateway` ซึ่งทำหน้าที่เป็น Router/Gateway สำหรับ API
> 2. เพื่อใช้ Path การรันเซิร์ฟเวอร์แบบคงที่ (Deterministic Static Path) ที่ `mcp-unity/cloned/Server~/build/index.js` แทนการใช้ PackageCache ของ Unity ที่มี commit hash เปลี่ยนแปลงตลอดเวลาเมื่อมีการอัปเดต package

---

## 📂 โครงสร้างโฟลเดอร์

```
clogged/
├── kilo.json                       # Config ของ AI Client (Kilo)
├── mcp-unity/
│   ├── setup.cmd                   # รันติดตั้งออโต้ (Windows Double-Click)
│   ├── setup.ps1                   # สคริปต์หลักในการติดตั้งผ่าน PowerShell
│   ├── README.md                   # เอกสารฉบับนี้
│   └── cloned/ [IGNORED]           # โค้ดของ CoderGamester/mcp-unity ที่โคลนจากภายนอก
└── Unity-Projects/
    └── Packages/manifest.json      # ระบุ com.gamelovers.mcp-unity package
```

> [!IMPORTANT]
> **Git Ignore Policy:**
> โฟลเดอร์ `clogged/mcp-unity/cloned/` จะถูกละเว้นโดย Git (`.gitignore`) เนื่องจากเป็นซอร์สโค้ดจาก Repository ภายนอก ส่วนสคริปต์ `setup.cmd`, `setup.ps1` และ `README.md` จะถูกบันทึกลงในระบบควบคุมเวอร์ชัน (Git) ของเราตามปกติ

---

## ⚡ วิธีการติดตั้ง (Quick Setup)

สำหรับสมาชิกในทีมทุกคน สามารถติดตั้งได้ง่ายๆ ดังนี้:

### สำหรับ Windows
1. เปิดโฟลเดอร์ `mcp-unity`
2. ดับเบิ้ลคลิกไฟล์ `setup.cmd`
3. สคริปต์จะทำการ:
   - ตรวจสอบ git, node และ npm
   - โคลน Repository `CoderGamester/mcp-unity` มาไว้ที่ `cloned`
   - รัน `npm install` และ `npm run build` ในเซิร์ฟเวอร์ย่อย
   - แก้ไขไฟล์ตั้งค่า `kilo.json` และ `Unity-Projects/opencode.json` ให้ชี้มาที่พาธใหม่โดยอัตโนมัติ!

### สำหรับ macOS / Linux หรือการติดตั้งด้วยตนเอง (Manual Setup)
รันคำสั่งเหล่านี้ใน Terminal จากโฟลเดอร์ root ของโปรเจกต์:
```bash
# 1. เข้าไปที่โฟลเดอร์ mcp-unity
cd mcp-unity

# 2. โคลนโปรเจกต์ภายนอก
git clone https://github.com/CoderGamester/mcp-unity.git cloned

# 3. ติดตั้งและคอมไพล์เซิร์ฟเวอร์
cd cloned/Server~
npm install
npm run build
```

---

## ⚙️ การตั้งค่า AI Client

หากต้องการเขียนไฟล์ตั้งค่าด้วยตนเอง ให้ใช้พาธคงที่ต่อไปนี้ในการเรียกใช้เซิร์ฟเวอร์:

### 1. สำหรับ Kilo (`kilo.json` ที่ root)
```json
{
  "$schema": "https://app.kilo.ai/config.json",
  "mcp": {
    "mcp-unity": {
      "type": "local",
      "enabled": true,
      "command": ["node", "mcp-unity/cloned/Server~/build/index.js"]
    }
  }
}
```

### 2. สำหรับ OpenCode (`Unity-Projects/opencode.json`)
```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "mcp-unity": {
      "type": "local",
      "enabled": true,
      "command": [
        "node",
        "../mcp-unity/cloned/Server~/build/index.js"
      ],
      "environment": {}
    }
  }
}
```

### 3. สำหรับ Claude Code (`Unity-Projects/.mcp.json`)
```json
{
  "mcpServers": {
    "mcp-unity": {
      "command": "node",
      "args": ["../mcp-unity/cloned/Server~/build/index.js"]
    }
  }
}
```

---

## 🎮 การใช้งานร่วมกับ Unity Editor

1. ตรวจสอบว่าไฟล์ `Unity-Projects/Packages/manifest.json` มีการระบุ dependency ของ package เรียบร้อยแล้ว (จะถูกดึงลงมาอัตโนมัติเมื่อเปิด Unity):
   ```json
   "com.gamelovers.mcp-unity": "https://github.com/CoderGamester/mcp-unity.git"
   ```
2. เปิดโปรเจกต์ Unity ด้วย Unity Editor
3. ไปที่เมนู **Tools → MCP Unity → Server Window**
4. กดปุ่ม **"Start Server"** (จะทำงานบนพอร์ตเริ่มต้น `8090`)
5. เปิด AI Client (เช่น Kilo หรือรัน Claude Code) เพื่อเริ่มต้นใช้งานคำสั่งควบคุม Unity ผ่าน AI!

---

## 🔍 ข้อมูลอ้างอิงต้นทาง
- [CoderGamester/mcp-unity (GitHub)](https://github.com/CoderGamester/mcp-unity)
- [Model Context Protocol Specification](https://modelcontextprotocol.io/)
