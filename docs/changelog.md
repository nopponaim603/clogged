# Documentation Changelog — clogged

<<<<<<< HEAD
## 2026-10-29

- **Unity V2 design documentation refresh** — ปรับ GDD ทั้ง 5 ฉบับและเอกสาร Unity V2 system design, class diagram, Core/Main Menu scene hierarchy และ UI layout snapshot ให้สอดคล้องกับ `Unity-Projects/Assets/V2`: แยกกล่อง/กุญแจที่สุ่มดรอปอิสระ, scene flow และ checkpoint save/restore, ผลจบ run/crystal reveal, ลำดับให้อาหาร และสิ่งที่ยังไม่มี UI/audio; ปรับดัชนีเอกสารให้ชี้ไปยังข้อมูลปัจจุบัน แยก Web Prototype เป็นเอกสารอ้างอิง
- **UI layout spec diagrams (draw.io)** — `docs/software/07-unity-v2-ui-layout-redesign.md`: แทน ASCII แผนผังโซน §3.1 และ Mermaid flow §6.1–§6.3 (Cook/Craft, Heal, สำรวจ) ด้วยรูป PNG; เก็บต้นฉบับ `.drawio` (+ `.svg`) ใน `docs/software/diagrams/`
- **Repository layout** — เปลี่ยนชื่อโฟลเดอร์โปรเจกต์ Phaser จาก `prototype_resource_game/` เป็น `src/` และย้าย AI Gateway ไปไว้ที่ `scripts/ai-gateway/`; ปรับคำสั่งใช้งาน, เอกสาร, skill references และ ignore rules ตามตำแหน่งใหม่

=======
## 2026-11-03 (รอบ 24)

- **v0.47 (แก้บั๊ก: heal แล้วยูนิตที่ AP หมดกลับมากดได้)** —
  - **อาการ:** Dan ถูกส่งไป Hard (3 AP → 0) แล้วเจ็บ Ann รักษาให้ พอ heal เสร็จการ์ด Dan กลับกดได้ทั้งที่ AP = 0
  - **ต้นเหตุ:** `BaseWorkController.Heal()` ตั้ง `patient.state = UnitState.Idle` ตรง ๆ — การรักษาล้าง `Hurt` แต่ไม่ได้คืน AP จึงได้สถานะ `Idle` + AP 0 ซึ่ง `CanBeDispatched` (เดิม = `state == Idle`) มองว่าเลือกได้
  - **แก้ที่ต้นเหตุ + รวมกฎไว้ที่เดียว:** เพิ่ม `UnitCharacter.ReturnToPlay()` → `AP > 0 ? Idle : Resting` ใช้ทั้งใน `Heal()` และตอนจบทริป (`GatherDispatchController` ที่เคยเขียน ternary ซ้ำ)
  - **เสริมความปลอดภัย:** `CanBeDispatched` เพิ่มเงื่อนไข `currentActionPoints > 0` เพื่อไม่ให้ `Idle` + AP 0 หลุดมากดได้อีกจากทางอื่น
  - **ผลลัพธ์:** Dan กลับมาเป็น **Resting (R)** การ์ดจาง กดไม่ได้ จนถึงวันใหม่ — ยืนยันด้วยภาพและ probe (`state=Resting ap=0 selectable=False`)
  - `TestUiRedesign` เพิ่ม 3 เช็ค: `a healed unit with an empty AP bar comes back Resting` · `a healed unit with no AP cannot be picked again` · `a healed unit with AP left returns Idle and playable` → **ALL PASS 210**

## 2026-11-03 (รอบ 23)

- **v0.46 (แก้เสียง Win เล่น 2 เพลงพร้อมกัน — ต้นเหตุอยู่ที่ไฟล์ ไม่ใช่โค้ด)** —
  - **อาการ:** ผู้เล่นได้ยินเพลงชนะ 2 เพลงซ้อนกัน
  - **ตรวจแล้วโค้ดถูก:** `AudioDirector.PlayWin()` ถูกเรียกครั้งเดียว และ `PlaySfx` เล่นคลิปเดียว (probe: `SfxCount 0→1`, มี `AudioDirector` ตัวเดียว, 2 `AudioSource`) → ปัญหาอยู่ที่ **ไฟล์ต้นฉบับ**
  - **ต้นเหตุจริง:** `Win_1.mp3` และ `Win 2.mp3` เป็น **stereo ที่ช่องซ้าย/ขวาบรรจุคนละเพลง** — วัด correlation ได้ **0.117** (Win_1) และ **0.626** (Win 2) และทดสอบ cross-correlation ทุก lag (±400 samples) แล้วได้สูงสุดแค่ 0.451/0.640 → ไม่ใช่ delay/phase effect แต่เป็นคนละเนื้อหา · ประกอบกับ import setting `forceToMono = true` ที่ **บวกสองช่องเข้าด้วยกัน** จึงได้ยิน 2 เพลงพร้อมกัน
  - **วิธีแก้:** แยกช่องที่ดังกว่า (+RMS) ออกมาเขียนเป็น mono WAV ใหม่ — `Win_1.wav` (4.49s) และ `Win_2.wav` (3.91s) ที่ 24 kHz 16-bit, `ch=1`, peak 0.904/0.891 (ไม่ clip)
  - **ปรับระดับเสียงให้เท่ากัน:** เดิม peak ต่างกันเกือบ 2.2 เท่า (0.925 vs 0.415) ทำให้สุ่มแล้วรู้สึกเหมือนเสียงดับ → normalize ทั้งคู่เป็น −1 dBFS (0.89) ด้วย gain ×0.96 และ ×2.14
  - **ย้าย mp3 ต้นฉบับออกจาก `Assets/`** ไป `asset-sources/win-source/` เพื่อกันการผูกกลับเข้าหาผลิตภัณฑ์โดยไม่ตั้งใจ (เก็บไฟล์ไว้ตรวจสอบ)
  - **`PlayWin()` ตัดเสียง SFX ที่ค้างอยู่ก่อน** (`sfxSource.Stop()`) — เสียงชนะมาหลังเสียงเก็บของเสมอ ถ้าไม่ตัดจะทับกัน
  - `TestUiRedesign` เพิ่ม 2 เช็ค: `every win sting is mono, so one file cannot hold two tracks` (กัน Stereo dual-track กลับมา) และ `one win call puts one sting on the source` → **ALL PASS 207**
  - **ยังไม่แก้ (รายงานให้ตัดสินใจ):** `Select.mp3` มีอาการเดียวกัน — correlation **−0.20** คือช่องซ้าย/ขวาเป็นคนละเสียง (ใช้อยู่ทุกครั้งที่เลือกการ์ด/กดยืนยัน)

## 2026-11-03 (รอบ 22)

- **v0.45 (เสียง Error ตอนกดแล้วขึ้น warning + แก้เทสต์ที่พึ่งดวง)** —
  - **คัดเสียง error ด้วยการวัด:** เทียบ 5 ตัวเลือก (`error_01`, `locked_01` จาก fantasy-ui และ `negative`/`negative_2` จาก Lokif) ด้วย band spectrum + centroid + active length + decay → เลือก **`error/error_01.wav`** (0.76s, centroid 593 Hz, 66% ของพลังงานอยู่ใต้ 250 Hz) เพราะเป็นโทน "ปฏิเสธ" แบบทึบต่ำ ไม่แสบหูเมื่อกดพลาดซ้ำ
    - `locked_01` (3725 Hz) สว่างเกิน, `negative.wav` (active 1.20s) ยาวเกิน, `negative_2` (237 Hz) ทุ้มจนอาจกลืน BGM
    - **Lokif ไม่ได้ใช้ทั้งชุดเหมือนเดิม** — แต่หนนี้พบว่า `negative*.wav` มีระดับเสียงใช้ได้ (peak 0.37/0.49) ต่างจาก `click.wav` ที่เกือบเงียบ
  - **`Error.wav`** เพิ่มใน `Assets/V2/Sounds` (DecompressOnLoad + preload + mono), ผูกเป็น `GameAudioLibrary.error` + `AudioDirector.PlayError()`
  - **ยิงจาก `WarningView.Show()`** — จุดเดียวที่ทุก warning ผ่าน (blocked skill / selection ผิด / end-day) จึงไม่มีทางลืม
  - **ปัญหาเสียงทับกันที่เจอตอนทำ:** ปุ่มที่กดไม่ติดจะเล่นคลิกของตัวเอง *หลัง* warning ในเฟรมเดียวกัน → error ถูกกลบ จึงเพิ่ม `UiClickSound.SuppressThisFrameClick()` (ผูกกับ `Time.frameCount` ไม่ใช่ flag ค้าง) ทำให้ปุ่มที่ถูกปฏิเสธดัง **แค่เสียง error ตัวเดียว**
  - **throttle 0.2s** ใน `PlayError` กันเสียงรัวตอนกดปุ่มที่บล็อกค้าง
  - **แก้เทสต์ที่พึ่งดวง (บั๊กที่มีมาก่อน):** ชุดเทสต์ยิงทริปจริงบนถ้ำที่มีโอกาสดรอป Core Item → บางรอบได้ครบ 2 ชิ้นแล้ว **ชนะรันกลางชุด** ทำให้หน้าผลลัพธ์บัง raycast, เพลงหยุด, End Day ไม่เดิน (ล้ม 4 เช็คที่ดูเหมือนของใหม่) → เพิ่ม `ResetOutcome()` ใน `ResetRun` (สะท้อนค่า `Outcome` กลับเป็น InProgress + ซ่อน result screen) และตัด guard "ต้อง restart play mode" ออก
  - `TestUiRedesign`: `198 → 205` เช็ค (`the library carries an error sting`, `a refused click shows a warning and plays the error sting` ตรวจว่าเสียงที่ดังคือ error ตัวเดียว, `the error sting is throttled so a held button does not buzz`) → **ALL PASS 205**

## 2026-11-02 (รอบ 21)

- **v0.44 (เสียงคลิกแยกรายปุ่ม 6 แบบ จาก SFX sample)** —
  - **คัดเสียงด้วย fingerprint ไม่ใช่ชื่อไฟล์:** วัด band spectrum (10 log-spaced bands, FFT แบบ adaptive window), spectral centroid, duration, active length, ZCR แล้วจัดอันดับความคล้ายกับ 6 ตัวอย่าง → ยืนยันว่า `Select`/`Craft`/`Food Ready` มีตัวแทนในแพ็ก fantasy-ui ที่คะแนน 0.83–0.90 แต่ `Click`/`UI Popup` แพ็กไม่มีของสั้นพอ (สั้นสุด 0.11s) และ `Heal` แพ็กสว่างเกิน (0.441) → **ผู้ใช้เลือกใช้ไฟล์ตัวอย่างทั้ง 6**
  - **`UiClickKind` ขยายจาก 2 → 6 ค่า:** `Plain` / `Select` / `Cook` / `Heal` / `Craft` / `Popup` + `GameAudioLibrary.ForKind(kind)` เป็นตัวแมปเดียว
  - **แมปปุ่ม:**
    - `Plain` → `Click.wav` (0.20s) — menu / options / help / close / mode toggle / CONTINUE / สไลเดอร์
    - `Select` → `Select.mp3` (1.07s) — การ์ดยูนิต / การ์ดดันเจี้ยน / ปุ่มยืนยัน
    - `Cook` → `Food Ready.wav` (1.59s) — ปุ่ม COOK
    - `Heal` → `Heal.mp3` (8.04s) — ปุ่ม HEAL
    - `Craft` → `Craft.mp3` (1.70s) — ปุ่ม CRAFT
    - `Popup` → `UI Popup.wav` (0.18s) — เปิดหน้า OPTIONS / HELP (เล่นเพิ่มจากคลิกของปุ่ม)
  - **Import setting:** 5 คลิปสั้นเป็น DecompressOnLoad + preload + mono; `Heal.mp3` เป็น **CompressedInMemory** เพราะ 8.04s (หาง reverb ยาว) ถ้า decompress ค้างจะกิน ~690 KB ตลอด session
  - **ลบ `Ui_Click.wav` / `Ui_Click_Playable.wav`** ที่ถูกแทนที่แล้ว (ไม่เหลือ dead asset)
  - **`UiClickSound.ResetThrottle()`** เพิ่มสำหรับเทสต์ — throttle 0.1s ยังทำงานจริงกับการลากสไลเดอร์
  - `TestUiRedesign` ปรับ/เพิ่มเช็ค (คลิปครบ 6, cook/heal/craft ใช้คลิปของตัวเอง, mode toggle เป็น plain, confirm + การ์ดดันเจี้ยน + การ์ดยูนิตเป็น Select, เปิดหน้าเป็น Popup พร้อมคลิกของปุ่ม = 2 เสียง, ปิดหน้าเป็น plain) → **ALL PASS 202**

## 2026-11-02 (รอบ 20)

- **v0.43 (เสียงคลิก UI 2 แบบ + rename ไฟล์ Win + ย้าย archive ออกจาก Assets)** —
  - **Rename:** `WIn1.mp3` → **`Win_1.mp3`** ผ่าน `AssetDatabase.MoveAsset` (GUID คงเดิม `363a8d77…` → reference ใน `GameAudioLibrary` ไม่หลุด) — แก้ทำให้ชื่อ `Win_1` / `Win 2` ไม่ชนกันแล้ว
  - **แตก archive 2 ชุดที่เพิ่มมา:**
    - `fantasy-ui-sfx-free-assets.zip` → 30 ไฟล์ WAV (16 หมวด: clicks / confirm / hover / slider / toggles / cancel / coins / crafting / …)
    - `GUI_Sound_Effects_by_Lokif.7z` → 13 ไฟล์ 1.72–3.43s
  - **ผลตรวจคุณภาพเสียง (วัด peak / RMS / zero-crossing / active length):** **Lokif ใช้ไม่ได้** — `click.wav` peak 0.08 / RMS 0.002, `click_2.wav` peak 0.04 = เงียบเกือบสนิท และไฟล์อื่นยาว 1.7s ไม่ใช่เสียงคลิก จึงเลือกจาก fantasy-ui ทั้งหมด
  - **`Ui_Click.wav`** ← `click_iron_01` (0.42s, active 0.08s, ZCR 0.165 = แหลม สว่าง) ใช้กับ **ปุ่ม chrome**: menu / options / help / close / mode toggle / slider / ปุ่ม CONTINUE
  - **`Ui_Click_Playable.wav`** ← `click_stone_01` (0.49s, active 0.04s, ZCR 0.064 = ทึบ ต่ำ) ใช้กับ **ปุ่มที่ทำงานจริง**: cook / heal / craft / การ์ดดันเจี้ยน / การ์ดยูนิต / ปุ่มยืนยัน
  - **`UiClickKind` (ใหม่)** + **`UiClickSound` (ใหม่)** — `UiFactory.CreateButton` ติด plain click ให้ทุกปุ่มอัตโนมัติ (chrome ไม่มีทางลืม), ปุ่มที่ทำงานจริงเรียก `MarkPlayable()`; ปุ่มที่สร้างมือ (HudTopBar, DiceRollView CONTINUE) ติดเอง; `UiClickSound.Play()` สำหรับ slider (throttle 0.1s กันเสียงรัวตอนลาก) และการ์ดยูนิต (`UnitIconView.OnPointerClick`)
  - **ย้าย archive ออกจาก `Assets/`:** zip/7z ที่วางใน `Assets/V2/Sounds` ทำให้ Unity import เป็น asset + สร้าง `.meta` โดยไม่จำเป็น → ย้ายไป `asset-sources/` ทั้งคู่
  - **กันกับดักในเทสต์:** `TestUiRedesign` เริ่มด้วยการเช็ค `DayManager.Outcome` — ถ้ารันไม่จบ (รันซ้ำใน play session เดิม) จะบอกให้ restart play mode แทนที่จะ fail 17 เช็คหลอก
  - `TestUiRedesign` เพิ่ม 8 เช็ค (คลิปคลิกครบ, ปุ่ม chrome ใช้ plain, cook/heal/craft + confirm + การ์ดดันเจี้ยนใช้ playable, mode toggle เป็น plain, เล่นจริงได้คลิปถูก, throttle กันเสียงรัว) → **ALL PASS 198**

## 2026-11-02 (รอบ 19)

- **v0.42 (คลิป Win / Fail + เปลี่ยนป้าย BACK TO GAME)** —
  - **คลิปใหม่ 3 ไฟล์** ใน `Assets/V2/Sounds`: `WIn1.mp3` (4.49s) · `Win 2.mp3` (3.91s) · `Fail.wav` (1.95s) — import เป็น **DecompressOnLoad + preload + mono** (one-shot สั้น ต้องไม่รอ disk ตอนเล่นครั้งแรก) ต่างจาก BGM ที่เป็น Streaming
  - **`GameAudioLibrary`** เพิ่ม `fail` และ `win[]` (2 คลิป) + accessor `Fail` / `WinClipCount` / `WinClip(i)`
  - **`AudioDirector`** เพิ่ม `PlayFail()` (ทอย Failure หรือ Fumble) และ `PlayWin()` (หยุดเพลงแล้วสุ่ม 1 ใน 2 คลิป — ชนะติดกันจะไม่ sounded เหมือนกัน) ; ถ้าไม่มีคลิป win เลยจะ fallback ไป `Success`
  - **`DiceRollView`** — เดิม Failure/Fumble เงียบ ตอนนี้แยกเป็น 2 ทาง: Regular/HardSuccess → `Success`, Failure/Fumble → `Fail`
  - **`RunResultView`** — ชนะ → `PlayWin()`, แพ้ → `PlayGameOver()`
  - **ป้ายปุ่ม:** `RETURN TO GAME` → **`BACK TO GAME`** (ในรันเกม, หน้า Options) — `RETURN TO MENU` คงเดิม; อัปเดต doc comment + ชื่อเช็คเทสต์ตาม
  - `TestUiRedesign` เพิ่ม 6 เช็ค (fail/win clip ครบ, Hard Success ยังใช้ Success, Failure/Fumble ใช้ Fail, ชนะใช้ 1 ใน 2 คลิปและหยุดเพลง, สุ่มได้ทั้ง 2 คลิปจริงใน 40 ครั้ง, ป้าย BACK TO GAME) → **ALL PASS 190**

## 2026-11-02 (รอบ 18)

- **v0.41 (ระบบเสียง: Master/Music/SFX + BGM สุ่มต่อรัน + เงื่อนไขแพ้ Hurt ครบทีม)** —
  - **`AudioMixer` ใหม่** `Assets/V2/Audio/GameMixer.mixer` — Master → Music / SFX, expose parameter `MasterVolume` / `MusicVolume` / `SfxVolume` (สร้างผ่าน `UnityEditor.Audio.AudioMixerController` เพราะ Unity ไม่มี public API สร้าง mixer)
  - **`AudioDirector` (ใหม่)** — `DontDestroyOnLoad`, `AudioSource` 2 ตัว (Music loop / SFX `PlayOneShot`) route เข้า group ตามลำดับ, `ApplyVolumes()` แปลง linear 0–1 → dB (0 = −80 dB); API: `PlayTitleMusic`, `BeginRun(index)`, `PlaySuccess`, `PlayGameOver`, `StopMusic`
  - **`GameAudioLibrary` (ใหม่)** — `ScriptableObject` เก็บ 8 คลิป + mixer ไว้ที่ `Assets/V2/Resources/GameAudioLibrary.asset` (Resources เพราะ director สร้างตัวเองในโค้ด)
  - **เพลง:** `Title_Screen` หน้าเมนู; ตอนเริ่มรันสุ่ม `BGM_1..5` **ครั้งเดียว** (เก็บ index ใน `SaveData.musicTrack`) แล้ววนจนจบรัน — `BeginRun` ไม่ restart ถ้าเป็นคลิปเดิมที่เล่นอยู่ เซฟเก่าที่ไม่มีฟิลด์นี้จะสุ่มครั้งเดียวแล้วจำต่อ
  - **SFX:** `Success` ตอนทอยได้ Regular / Hard Success + ตอนได้ของ (cook / heal / craft / Core Item); `Game Over` ทุกเงื่อนไขแพ้ (หยุดเพลงก่อน); ชนะใช้ `Success` ไปพลางก่อน (ผู้ใช้จะเพิ่มคลิปทีหลัง)
  - **Import setting:** BGM + Title เป็น **Streaming** (จาก DecompressOnLoad ที่กิน RAM ~14 MB/เพลง) quality 0.7; `Success` / `Game_Over` เป็น **DecompressOnLoad + preload**, mono
  - **หน้า Options:** `VOLUME` แถวเดียว → **MASTER / MUSIC / SFX** คนละสไลเดอร์ + % readout, ผูกผ่าน `AudioDirector.SetVolume` → PlayerPrefs → mixer; ลบข้อความ "AUDIO IS WIRED BUT HAS NO CLIPS YET"
  - **เงื่อนไขแพ้ใหม่:** `RunOutcome.LoseAllUnitsHurt` + `UnitManager.AllUnitsIncapacitated()` (ทุกคน Hurt/Death = ไม่มีใครรักษาใครได้) ตรวจทันทีหลังจบทริป (`DayManager.CheckImmediateLoss`) และตอนจบวัน; `RunResultView` มีข้อความเฉพาะ "THE WHOLE CREW IS HURT…"
  - **Help page:** เพิ่มแถว `LOSE THE RUN` (3 ทางแพ้) + ขยายคำอธิบาย HURT
  - `TestUiRedesign` เพิ่ม 21 เช็ค (mixer 3 group, source routing, เพลงรันไม่เปลี่ยนกลางรัน, Title track, เสียงทอย/ของ/GameOver, fader ถึง mixer, เงื่อนไขแพ้ Hurt ครบทีม + หน้าผลลัพธ์) → **ALL PASS 184**

## 2026-11-02 (รอบ 17)

- **v0.40 (ดาบเป็นทรัพยากรของ dispatch + ไอค่อนในหน้า HELP)** —
  - **โมเดลดาบใหม่:** `UnitCharacter` มี `maxSwords`/`currentSwords` (=1) เพิ่ม `HasSword` / `TrySpendSword()`; `HasGatheredToday` เปลี่ยนเป็นค่าอนุพันธ์ (`currentSwords <= 0`) → ตัวกันทริปที่ 2 มาจากดาบเล่มเดียว ไม่ใช่ flag แยก; `RefreshForNewDay()` คืนดาบพร้อม AP; `CanBeSentGathering = Idle && HasSword`
  - **dispatch:** `CanDispatch` รายงาน "X has no sword left today." เมื่อดาบหมด, `Dispatch()` หัก **1 ดาบต่อคน** และ **ไม่แตะงานในฐาน**; ปุ่มยืนยันขึ้น `N AP + M SWORD(S)` ผ่าน `DispatchActionPanel.Show(..., costText)` / `SetCostText()`
  - **UI:** badge ดาบบนการ์ดโชว์ตลอดตอน Idle และ **จาง alpha 0.28** เมื่อใช้แล้ว; tooltip แยก READY / SWORD SPENT
  - **save:** `currentSwords` serialize ใน `SaveData`/`SaveSystem`, restore แบบ clamp กับ `maxSwords`, `UnitManager` ตั้งค่าเริ่มต้นตอน `Awake` — เซฟเก่าโหลดได้ (ดาบเต็ม 1)
  - **หน้า HELP:** `Entry` รองรับ `WithIcon(sprite)` / `WithGlyph(char)` → badge 28×28 หน้าข้อความ; เพิ่ม `Rebuild()` ที่เรียกทุกครั้งที่เปิดหน้าเพื่อดึง art สดจาก `StatusBarView`/`UnitIconView`/`MapNodeView`/`DispatchActionPanel`; STATES เขียนใหม่ครบ (READY / SWORD SPENT / RESTING / HURT / AWAY / DEATH / HUNGRY / trait / blessed / potion / starving) พร้อมไอค่อน; **ลบแถว "NO SOLO PENALTY"** ตามที่ผู้ใช้ขอ (ระบบยังคิดโบนัส duo/mismatch แต่ไม่ประกาศ)
  - **ฟอนต์:** glyph `*` ไม่มีใน PixelTileset → เปลี่ยนเป็น `W` สำหรับ SWORD SPENT
  - **เอกสาร:** `01-mechanics.md` เพิ่ม §2.2b Sword, แก้ตารางสถานะ/รีเฟรช/§4/§8.1; `04-unity-system-design.md` อัปเดตกติกาดาบ; `07-unity-v2-ui-layout-redesign.md` §8.12
  - `TestUiRedesign` เพิ่มชุดตรวจดาบ (`CheckSwordRules`: 1 เล่ม/วัน, ทริปที่ 2 ถูกปฏิเสธ, งานในฐานยังทำได้, รีเฟรชรายวัน, backend ปฏิเสธเอง) + ดาบจางบนการ์ด + help icons → **ALL PASS 163**

## 2026-11-02 (รอบ 16)

- **v0.39 (หน้า HELP ตัวจริง: 8 หมวด + copy ตามสเปก + ระบบที่สเปกยังไม่พูดถึง)** —
  - **`HelpPanelView` (ใหม่):** แทน body ที่ขึ้นว่า "(how-to-play content pending)" — เลย์เอาต์เป็น **rail ซ้าย 8 หมวด (300px) + pane ขวาเลื่อนได้ (ScrollRect + scrollbar)** ในการ์ด 1200×720, หัวเรื่อง `N / 8`, ปุ่ม CLOSE มุมขวาบน; แต่ละแถวเป็น "การ์ด" มีแท่งสีซ้าย (เขียว = ดี, แดง = อันตราย, เทา = ข้อมูล) + หัวข้อ 22px + เนื้อ 19px
  - **8 หมวด:** 1 BASICS · 2 RESOURCES · 3 UNITS · 4 STATES · 5 DUNGEONS · 6 SKILLS · 7 SUCCESS VALUE · 8 THE ROLL
  - **ตัวเลขทุกตัวอ่านจาก controller ตอนสร้างหน้า** (`BaseWorkController`, `GatherZoneDatabase`, `DayManager`, `RunResources`, `GatherResolver`, `UnitCharacter`) — ไม่มีค่าตายตัวให้ค้างเก่าเหมือน tooltip เดิม
  - **เนื้อหาที่สเปกยังไม่ครอบ ใส่ครบ:** AP ต่อถ้ำ 1/2/3 · talent ของทั้ง 4 ตัว · pity Core Item · สถานะ Resting / Away / Gathered Today · ที่มาของ trait ดี (Hard Success 50%) · trait นับครั้งเดียวต่อทีม · duo +25 / mismatch −10 ต่อชั้น / ไม่มีโทษ Hard คนเดียวแล้ว · Fumble cliff ที่ SV 50 · Sword ไม่ใช่ทรัพยากร · วิธีอ่านการ์ด (hover ทุก badge)
  - **บั๊กที่เจอระหว่างทำ (แก้แล้ว):** scrollbar ใช้ anchor ของ pane ทั้งที่ pane เป็นแบบ stretch → track ยืดคลุมการ์ดทั้งใบทำให้ทุกอย่างในหน้าโดนฟิล์มขาว 6% + handle 35% (จอออกมาเป็นเทาอ่อนอ่านยาก) — เปลี่ยนเป็น anchor มุมขวา + ขนาดคงที่ตาม `PaneHeight`
  - `TestUiRedesign` เพิ่ม 10 ข้อ (help เปิด/ปิดได้, มี rail+pane+counter, copy อ้างราคา Cook/Heal จริง, แถวมี layout ที่โตได้) → **ALL PASS 156 ข้อ**


## 2026-11-02 (รอบ 15)

- **v0.38 (ปรับระบบให้ตรงสเปก help: Fumble + สถานะ Death)** — ตรวจสเปก help ที่จะเขียนใหม่ทั้งชุด เทียบกับโค้ดจริงแล้วแก้ 2 ข้อตามที่ตกลง (Core Item คงแบบเดิม, Sword เป็นแค่ไอคอน READY)
  - **Fumble ใหม่:** เดิมถ้ำ Hard สุ่ม 1 คน **หายตัวถาวร** และถ้ำอื่นไม่มีอะไรเกิดขึ้น → ตอนนี้ **ยูนิตทุกคนที่เข้าทริป Hurt + ได้ trait เสียคนละ 1** ทุกถ้ำ ไม่มีใครตายจากการทอย ([GatherDispatchController.cs](../../Unity-Projects/Assets/V2/Scripts/Controllers/GatherDispatchController.cs))
    - `GatherDispatchResult`: `missingUnit` ถูกถอด, เพิ่ม `hurtUnits` + `traitRecipients` (list) และ `hurtUnit` เป็น read-only ตัวแรก; `ApplyKnownOutcome()` เป็น public seam ให้เทสต์บังคับ tier ได้
    - `DiceRollView` / `RunLogNarrator` / `RunStats` ปรับตาม (log ขึ้น "X was hurt." ทุกคน, popup ขึ้น "All N units were hurt")
  - **`UnitState.Missing` → `UnitState.Death`** (ค่า enum คงเดิม = 4, save เก่าโหลดได้): UI เปลี่ยน `X` → `D`, tooltip "DEATH", log "died.", หน้าจอจบเกม "CREW DEAD"; `DayEndReport.lostSignal` → `died`. **Death มาจาก starvation 3 stack เท่านั้น**
  - `TestUiRedesign` เพิ่มหมวด `fumble` 4 ข้อ → **ALL PASS 146 ข้อ**

## 2026-11-02 (รอบ 14)

- **v0.37 (แก้แท่งทอยเต๋ายังใช้ Hard Success 1/2 อยู่)** —
  - **บั๊ก:** `DiceRollView.BuildBar()` ยังคำนวณ `successValue / 2` ตอนวาดแท่งและ legend ทำให้ SV 75 ขึ้น `HARD 1-37` · `REGULAR 38-75` ทั้งที่ `GatherResolver.Roll()` ใช้ ⌊SV÷3⌋ ไปแล้วตั้งแต่ v0.35 (ผู้เล่นเห็นช่วงผลไม่ตรงกับที่ทอยจริง)
  - **แก้:** `BuildBar()` เรียก `GatherResolver.HardSuccessThreshold(successValue)` เป็นแหล่งเดียว — ตอนนี้ SV 75 = `HARD 1-25 · REGULAR 26-75 · FAILURE 76-99 · FUMBLE 100` และ SV 105 = `HARD 1-35`
  - เพิ่ม `DiceRollView.CurrentSuccessValue` (read-only) ให้เทสต์เทียบ legend กับ resolver ได้
  - `TestUiRedesign` เพิ่มข้อ `the dice bar paints Hard Success from the top third` → **ALL PASS 142 ข้อ**
  - หมายเหตุสภาพแวดล้อม: หลังไฟดับ Editor เปิดแบบหน้าต่างไม่ active ทำให้ `playerLoopTicking = false` และ screenshot ค้างที่เฟรมเก่า — ต้อง `editor.playmode set_run_in_background --enable true` ชั่วคราวก่อนถ่ายภาพ แล้วปิดคืน (`false`) เมื่อเสร็จ

## 2026-11-02 (รอบ 13)

- **v0.36 (tooltip ใหญ่ขึ้น + แยกคำอธิบายรายองค์ประกอบ)** —
  - **ขนาด/ตัวอักษร:** `HudTooltip` กว้าง 290 → **360 px**, title 16 → **21**, body 15 → **19**, hint 14 → **16**, padding 10 → 14 — อ่านง่ายขึ้นชัดเจน
  - **การ์ดตัวละครแยกทีละ badge:** ป้าย Aptitude (E/N/H) อธิบายโบนัส +25 / โทษ −10 ต่อชั้น · ไอค่อน Talent อธิบายผลของ talent นั้น · มุมซ้ายบน (READY/R/H/A/M) อธิบายสถานะ · **lamp ทั้ง 6 ดวง** (Hurt / Hungry / Good trait / Bad trait / Blessed / Buff potion) มีคำอธิบายของตัวเองพร้อมบอกว่าติดอยู่หรือไม่ · **AP pip ทีละเม็ด** บอกว่าเม็ดนั้นยังว่าง / กำลังจะถูกใช้ / ใช้ไปแล้ว
  - **การ์ดดันเจี้ยนแยกทีละ readout:** ชื่อถ้ำ (SV ฐาน + AP + กติกา aptitude) · **SV%** (เกณฑ์ Hard Success จาก `HardSuccessThreshold`) · **Pity** (% ต่อชิ้น + จำนวนที่การันตี) · **ไอเทมดรอปทุกช่อง** (main loot = ทุกทริปที่สำเร็จ, "+" = เฉพาะ Hard Success pool)
  - **ลบคำอธิบายรวม:** ไม่มีการ์ดไหนผูก tooltip ที่ root อีกแล้ว (เดิมhover ที่ไหนในการ์ดก็ขึ้นก้อนเดียว)
  - **OPTIONS / HELP ไม่มี tooltip** ตามที่ขอ (ปุ่มอื่นยังมี)
  - **ไม่กระทบการคลิก:** `HudTooltipTrigger.Attach()` เปิด raycast ให้เฉพาะ graphic ของ element นั้น — ทดสอบแล้วว่า raycast ที่ lamp/drop label ยัง resolve ไปที่ handler ของการ์ด (คลิกเลือกได้เหมือนเดิม)
  - `TestUiRedesign` เพิ่ม 9 ข้อ → **ALL PASS 141 ข้อ**

## 2026-11-02 (รอบ 12)

- **v0.35 (AP บนปุ่มยืนยัน + tooltip ทั่ว HUD + Hard Success 1/3)** —
  - **ปุ่มยืนยันแสดง AP จริง:** `DispatchActionPanel` เพิ่มบรรทัด `Cost` ("N AP") ใต้ label (prefab `DispatchActionPanel.prefab`); `SkillFlowController.PendingActualApCost` รวม starvation surcharge แล้วส่งเข้า `Show(label, icon, interactable, apCost, tooltip)` — Cook/Craft/Heal ขึ้นตามจำนวนที่ใช้จริง, gather ใช้ `config.apCost`, ค่า 0 ซ่อนบรรทัด
  - **แก้ tooltip ที่ค้างค่าเก่า:** เดิม Heal เขียนตายว่า "AP USES 2 AP" ทั้งที่ค่าจริงคือ 3 — ตอนนี้ tooltip ทั้งชุดสร้างจาก provider (`Func<HudTooltipContent>`) ทุกครั้งที่ hover และอ่านค่าจาก `BaseWorkController`
  - **`SkillTooltip`/`SkillTooltipTrigger` → `HudTooltip`/`HudTooltipTrigger`:** ใช้ร่วมกันทั้ง HUD, กว้างขึ้น 250 → 290 px, เพิ่ม `Preview()` ให้เทสต์อ่านเนื้อหาได้, `Attach()` เปิด raycast ของ Graphic เดิมให้เองเมื่อ element ยังไม่รับ pointer
  - **tooltip ครอบทุกส่วน:** Food / Medicine / Cooking / Materials / Core Item, Day frame, OPTIONS + HELP, LOG header, การ์ดดันเจี้ยน (SV, กติกา 1D100, AP, loot, pity), การ์ดตัวละคร (aptitude, talent, AP, state, trait, blessed, starvation), ปุ่มสกิลทั้ง 4 และปุ่มยืนยัน
  - **Hard Success = ⌊SV ÷ 3⌋** (เดิม ÷2) ใน `GatherResolver.Roll` + เพิ่ม `HardSuccessThreshold()`; ปรับตารางโอกาสใน GDD §4.5 และตาราง Core Item ของ `07-unity-v2-ui-layout-redesign.md` ตามสูตรใหม่
  - `TestUiRedesign` เพิ่ม 9 ข้อ → **ALL PASS 132 ข้อ** (ครอบ tooltip 7 ข้อ + สูตร Hard Success 1 ข้อ + AP บนปุ่มยืนยัน 1 ข้อ)

## 2026-11-02 (รอบ 11)

- **v0.34 (แถว Craft เป็นของพรีแฟบจริง — เลิกประกอบตอน play)** —
  - **เหลือโค้ดชุดเดียวสำหรับปุ่มสลับโหมด:** เดิม Heal ผูกปุ่มโหมดจากพรีแฟบ แต่ Craft ถูกประกอบใหม่ใน `Awake` (`EnsureCraftModeToggle`) → เลือกทาง "แบ่งช่องตั้งแต่ใน scene" แล้ว apply ให้ทั้งคู่
  - `SkillTriggerPanel.prefab`: เพิ่ม `CraftRow` (`HorizontalLayoutGroup` 220×64) แบบเดียวกับ `HealRow` — `CraftButton` 156×64 + `CraftModeButton` 56×64 ("MED"/"BUFF") พร้อมผูก `craftModeButton` / `craftModeLabel`
  - `SkillTriggerPanel.cs`: ลบ `EnsureCraftModeToggle()` (~80 บรรทัด) และเรียกใช้เฉพาะ reference จากพรีแฟบ; เพิ่ม tooltip ของ `CraftModeButton`
  - `SkillFlowController.RefreshPanel()` เพิ่ม `SetCraftMode(workshopMode)` ให้ label ตรงกับ state ตั้งแต่เฟรมแรก
  - `Core.unity`: ย่อ label ของ `CraftButton` (72 px, x = 18) และตัวเลขคิว (20 px ชิดขวา) ไม่ให้ทับคำ `CRAFT` — ตรวจด้วยภาพใน play mode (สลับ MED → BUFF ได้, คิว "2" ไม่ทับตัวอักษร)

## 2026-10-31 (รอบ 10)

- **v0.33 (ดาบจากแพ็ก + ปุ่ม OPTIONS/HELP เป็นไอค่อนล้วน ทรงจัตุรัส)** —
  - **ดาบ:** `state_sword.png` เปลี่ยนจากดาบตั้งที่วาดเอง → **ดาบเฉียงจากแพ็ก `board-game-icons`** (16×16, เส้นขอบดำ + เนื้อขาว) ตามภาพที่ผู้ใช้ส่งมา — คง GUID เดิม (`2d5299…`) จึงไม่ต้องแก้ reference ใน `ScoutIcon.prefab` และ `DispatchActionPanel.prefab`
  - **ปุ่มมุมซ้ายบนเหลือแต่สัญลักษณ์:** `HudTopBar.ButtonSize` 136×38 → **48×48** (จัตุรัส), ไม่สร้าง `Label` เลยเมื่อ `label` เป็น `null`/ว่าง, ไอค่อน **28×28** anchor กลาง (เดิม 20×20 ชิดซ้าย + label)
  - `BuildButton()` ยังรองรับปุ่มมีข้อความ (ปุ่ม `CLOSE` ในหน้ากดตัวเลือกใช้ label ตามเดิม) — แค่เพิ่มเส้นทาง "icon-only"
  - `TestUiRedesign` เพิ่ม/แก้ 6 ข้อ (**ไม่มี Label**, **กรอบจัตุรัส**, **ไอค่อนอยู่กลาง**) แทนสองข้อเดิมที่วัด label → **ALL PASS 123 ข้อ**, console 0 error

## 2026-10-31 (รอบ 9)

- **v0.32 (แตก asset pack เข้าโปรเจกต์ + ย้าย zip ออก + gitignore)** —
  - **แตกครบ 4 แพ็กเข้าโปรเจกต์** ที่ `Unity-Projects/Assets/V2/Sprites/Packs/`:
    | โฟลเดอร์ | PNG | ขนาด |
    |---|---|---|
    | `board-game-icons/` | 513 | 0.66 MB |
    | `cursor-pack/` | 729 | 0.37 MB |
    | `cursor-pixel-pack/` | 223 | 0.06 MB |
    | `game-icons/` | 425 | 3.54 MB |
    | **รวม** | **1,890** | **4.63 MB** |
  - **ย้าย zip 4 อันออกจาก `Assets/`** → `asset-sources/` (ที่เดียวกับ `kenney_input-prompts-*.zip` ที่มีอยู่แล้ว) เพราะ `asset-sources/` ถูก ignore อยู่แล้วโดยธรรมเนียมของ repo นี้ (raw third-party downloads ไม่เข้า git)
  - **`.gitignore` เพิ่ม `Packs/` + `Packs.meta`** — 1,890 PNG ไม่เข้า git; ไอค่อนที่เกมใช้จริง (3 ไฟล์ใน `Icons/`) ยัง track ตามปกติ
  - **เจอ error ตอน import:** `ArgumentOutOfRangeException: Attempting to allocate 83479 vertices which exceeds the limit of 65535` จาก `SVGImporter.BuildVectorImageThumbnail` — สาเหตุคือโฟลเดอร์ `Vector/` ในแพ็ก (622 SVG + 2 AI + 1 SWF) → **ย้ายไป `asset-sources/_extracted/*-vector/`** (ไม่ลบ) แล้ว import ผ่านหมด
  - **เพิ่ม `Assets/V2/Editor/PacksSpriteImporter.cs`** — `AssetPostprocessor` ที่บังคับเฉพาะ path `Assets/V2/Sprites/Packs/` ให้ import เป็น **Sprite** (Point filter, 16 PPU, mipmap off, max 512) เพราะโปรเจกต์อยู่โหมด 3D (`m_DefaultBehaviorMode: 0`) ทำให้ PNG ใหม่กลายเป็น `Texture2D` ธรรมดาและลากใส่ `Image.sprite` ไม่ได้ — ก่อนเพิ่มตัวนี้ `LoadAssetAtPath<Sprite>` คืน `null` ทั้ง 1,890 ไฟล์ หลังเพิ่มคืนครบ
  - **เจอ gotcha ของเทสต์:** เมื่อ Editor ไม่ได้ focus player loop จะหยุดเดิน → Canvas ไม่ layout → raycast คืน 0 hit ทำให้ **13 เทสต์แดงหลอก ๆ** (`nothing hit`, pop ไม่เกิด) แก้โดย `unity command editor focus` ก่อนรัน; เพิ่ม warning บรรทัดแรกของ `TestUiRedesign` เมื่อ `Time.unscaledDeltaTime == 0`
  - ยืนยันผล: **ALL PASS 121 ข้อ**, console **0 error**

## 2026-10-31 (รอบ 8)

- **v0.31 (ไอค่อนจริงสำหรับ OPTIONS / HELP / READY / ปุ่ม DISPATCH-CONFIRM)** —
  - **ค้น asset:** ไอค่อนทั้งสามตัวมีอยู่ในชุด Kenney ที่เพิ่มไว้แล้ว (CC0 ทั้งคู่):
    | ใช้ที่ | ต้นทาง | ไฟล์ในโปรเจกต์ |
    |---|---|---|
    | ปุ่ม OPTIONS | `kenney_game-icons/PNG/White/1x/gear.png` | `ui_gear.png` |
    | ปุ่ม HELP | `kenney_game-icons/PNG/White/1x/question.png` | `ui_question.png` |
    | READY state + ปุ่ม DISPATCH | `kenney_board-game-icons/PNG/Default (64px)/sword.png` | `state_sword.png` |
  - ทั้งสามถูก **trace ลงกริด 16×16 แบบ 1-bit** (ขาวบนโปร่งใส) เพื่อให้เข้าชุดกับไอค่อนเดิม (`ap_45` 10×12, `heal_a_cabinet` 16×16, `cook_a_stove_arch` 32×16) — ใช้ต้นฉบับ 50×50/64×64 ตรง ๆ ไม่ได้เพราะเป็น anti-alias แล้วจะแตกเป็นขอบเทาเมื่อตั้ง Point filter
  - **READY state:** `ScoutIcon.prefab` → `swordSprite` ชี้ `state_sword` (เดิมเป็น `PlaceholderIcons.Sword` ที่วาดในโค้ด)
  - **OPTIONS / HELP:** `HudTopBar` รับ `Sprite` ผ่าน `EnsureExists(optionsIcon, helpIcon)` และ `RunBootstrap` มีฟิลด์ `optionsIcon` / `helpIcon` ที่ผูกใน `Core.unity` (แถบนี้สร้างจากโค้ด จึงต้องส่งเข้ามา ไม่มี Inspector ของตัวเอง)
  - **ปุ่ม DISPATCH / CONFIRM มีรูป:** `DispatchActionPanel.Show(label, Sprite, interactable)` — ทริปเดินทางใช้ **ดาบ**, COOK/HELP/CRAFT ใช้ **ไอค่อนเดียวกับปุ่มสกิลที่เลือก** (`SkillTriggerPanel.CookIcon/HealIcon/CraftIcon`); ไอค่อนเดิมใน prefab เป็น alpha `0.1` และไม่มี sprite จึงมองไม่เห็น — แก้เป็น alpha `1`, `preserveAspect`, ขนาด 64×64 ที่ y=23 (เหนือ label)
  - **เจอบั๊กตอนใส่ไอค่อน:** ปุ่ม 92 px เดิมใส่ไอค่อน 20 px ไม่พอ — label "OPTIONS" ถูกตัดเป็น "OPT ION" → ขยายปุ่มเป็น **136×38** + เปิด `enableAutoSizing` (12–22) และ inset ขวา 4 px
  - `TestUiRedesign` เพิ่ม 8 ข้อ (ไอค่อน gear/question บนปุ่ม, label ไม่ทับและไม่ถูกตัด, READY เป็น `state_sword`, ปุ่ม dispatch ใส่ดาบ, ปุ่ม cook ใช้ไอค่อน cook, ดาบกลับมาเมื่อยกเลิกสกิล) → **ALL PASS 121 ข้อ**, console 0 error

## 2026-10-31 (รอบ 7)

- **v0.30 (บีบการ์ดดันเจี้ยน + ย่อปุ่ม DISPATCH + แก้ log ทับกัน + รายงาน balance baseSV)** —
  - **บีบพื้นที่การ์ดดันเจี้ยนโซนล่าง:** `MapNodeView.LayoutCard()` ปรับ `oddsBand 44 → 40`, `dropBand 56 → 46`, ช่องว่าง 10/8 → 6/6 แล้วเพิ่มค่าคงที่ **`bottomSafeBand = 98`** (แถบตายใต้ `Drops`) แทนการย่อการ์ด
  - **คืนความสูงการ์ดดันเจี้ยน:** ทดลองยก `marginBottom 330 → 420` (การ์ดเหลือ 550) แล้ว **ย้อนกลับเป็น 330** ตามคำขอ → การ์ดกลับมาสูง **640** (`y[330..970]`) โดย `Drops` ยังอยู่ที่ `y[428..474]` เท่าเดิม
  - **ที่มาของ 98:** ขอบล่างการ์ด `330` + การ์ดตัวละครที่ยกขึ้นถึง `401` = 71 px + clearance 27 px; พื้นที่นี้มีแต่พื้นหลัง/highlight ไม่มีข้อความ จึงถูกทับได้ตามที่ต้องการ
  - **ย่อปุ่ม DISPATCH:** `DispatchActionPanel.prefab` 300×200 → **230×152**, Icon 120×120 → **80×80** (y=10), Label 44 → 40 (y=6); ต้องแก้ **override ในซีน** (`m_SizeDelta` ของ `fileID 958683726539179671`) ด้วย → ปุ่มอยู่ที่ `x[1666..1896] y[24..176]` ไม่ทับการ์ดขวาสุด (`x[1428..1646]`)
  - **log ทับกัน (root cause จริง):** `VerticalLayoutGroup.m_ChildControlHeight` เป็น 0 **และ** `EntryTemplate` มี `LayoutElement.m_PreferredHeight: 24` ปีนทุกแถวไว้ 24 px → แก้เป็น `1` และ `−1` ตามลำดับ (เดิมแก้แค่ตัวแรกจึงยังทับ)
  - `TestUiRedesign` เพิ่ม 4 ข้อ (**ไม่มี label ดันเจี้ยนถูกบัง**, **การ์ดดันเจี้ยนสูง ≥ 620**, **ปุ่ม dispatch ไม่ทับการ์ด**, **แถว log สูงพอสำหรับข้อความที่ wrap**) → **ALL PASS 111 ข้อ**, console 0 error
  - **รายงาน balance V2.1:** `Assets/V2/test/gather-balance-basedsv-matrix.md` — เพิ่ม baseSV เป็น **4 ชุด** (80/55/30, 70/45/25, 65/40/20, 50/30/10) × HS 1/2·1/3 × Core 25/50·5/15 = **16 กติกา × 10 ซ้ำ × 3 policy × 10,000 เกม = 4.8 ล้านเกม** + sensitivity 1.6 ล้านเกม
  - ข้อสรุปใหม่: **มีเส้นตาย 2 เส้น** — `base_H < 15` ทำให้ทริป Hard หายไป (55% → 1%) และ `base_H < 25` ทำให้ fumble ของ Hard เดี่ยวถนัดกระโดด 1% → 5%; ตัวเลือกที่แนะนำคือ **`70/45/25`** (S13 win 81.13%, Δ −5.52 pp) หรือ **`70/45/25 · 1/3 · 25/50`** (S15 win 61.47%, median day 3)

## 2026-10-31 (รอบ 6)

- **v0.29 (ย้าย pity + SV เป็นสี + pop ตอนเลือกดันเจี้ยน)** —
  - **pity ย้ายขึ้นใต้ชื่อดันเจี้ยน:** ลำดับการ์ดใหม่เป็น **ชื่อ → pity → sprite ถ้ำ → SV/AP → ของที่ดรอป** (เดิม pity อยู่ล่างสุดซึ่งเป็นตำแหน่งที่การ์ดตัวละครที่ถูกยกขึ้นมาทับพอดี) — `MapNodeView.LayoutCard()` เปลี่ยนไปใช้ cursor วัดจากขอบบน/ล่าง; แถบ pity จองพื้นที่ไว้เสมอ (แม้ Easy ที่ไม่มี pity) เพื่อให้ sprite ถ้ำของการ์ดทั้งสามใบอยู่ในระดับเดียวกัน
  - **SV แสดงเป็นสี:** `MapNodeView.ColorForChance(int)` — ramp แดง (0) → ส้ม (40) → เหลือง (70) → เขียว (100) แบบ piecewise lerp; `RefreshReadout()` เปลี่ยนสีทุกครั้งที่ค่าเปลี่ยน (รวมถึงตอนเลือกยูนิตแล้ว SV ของทีมเปลี่ยน) → หน่วงด้วย `−90` จากความกว้างการ์ด
  - **pop ตอนเลือก:** การ์ดที่เลือกจะขยาย SV เป็น **34pt (จาก 26)** + สเกลขึ้น **1.18×** (`PopSpeed 6.5`/วิ, unscaled) + มีแถบพื้นหลังสีอ่อนกว่าตัวการ์ด (`SelectedBackdrop`) ค่อย ๆ ขึ้นมาข้างหลัง — เด่นกว่าข้อความของอีกสองใบชัดเจน; `SnapPop()` ไว้ให้เทสต์ที่รันในเฟรมเดียวกระโดดไปสถานะสุดท้าย
  - แถบพื้นหลังต้องเป็น **sibling ที่ถูกสร้างก่อน** ข้อความ (ไม่ใช่ลูก) เพราะ Unity UI วาด graphic ของ parent ก่อนเสมอ — ถ้าเป็นลูกจะทับตัวเลข
  - `TestUiRedesign` เพิ่ม 5 ข้อ (ramp / แดงกว่าเมื่อ SV ต่ำ / pity ใต้ชื่อ / pop ตอนเลือก / สีตาม SV จริง) → **ALL PASS 107 ข้อ**, console 0 error 0 warning

## 2026-10-30 (รอบ 5)

- **v0.28 (ต่อ DiceRollView เข้า flow จริง + ยกเลิกเลือกถ้ำหลัง dispatch)** —
  - **DiceRollView ใช้งานได้แล้ว:** `DiceRollView.EnsureExists()` สร้างป๊อปอัปให้เองครั้งเดียวต่อซีน (เรียกจาก `RunBootstrap.Awake`) — มี **Canvas ของตัวเอง + `overrideSorting` `sortingOrder = 500` + `GraphicRaycaster`** เพราะแผนที่อยู่คนละ canvas ทำให้ป๊อปอัปเดิมแพ้ทั้ง draw order และ raycast
  - **การไหล:** `GatherDispatchPanel.OnDispatchClicked()` → `ConfirmTrip()` (resolve ทันที) → `HandleTripResolved` เก็บ `pendingRoll` → `DiceRollView.Show(result, null)` โชว์อนิเมชันเต๋า (tumble 1.5s → settle → marker → ผล) + ปุ่ม CONTINUE; คลิกนอกกล่อง = ข้ามอนิเมชัน
  - **บั๊กที่แก้:** ป๊อปอัปสร้างแบบ inactive ทำให้ `graphic.depth = -1` และ Graphic Raycaster ข้าม graphic พวกนี้ → ปุ่ม CONTINUE กดไม่ได้ในเฟรมแรก เปลี่ยนไปใช้ **CanvasGroup** (alpha / blocksRaycasts) แบบเดียวกับ overlay อื่น
  - **ปรับหน้าตา:** panel 680×560, legend บรรทัดเดียวไม่ตัดบรรทัด, ตัดช่วงที่ว่างออกจาก legend (เช่น SV ≥ 100 ไม่มีช่วง Failure), "FUMBLE 100" เมื่อไม่มีช่วง fumble, ปุ่ม CONTINUE ใช้ `PlaceholderIcons.RoundedSquare` และฟอนต์ pixel ผ่าน `TmpFont.Apply`
  - **`RevealImmediately()`** — เปิดผลทันทีโดยไม่ต้องรออนิเมชัน (ใช้ในเทสต์ที่รันในเฟรมเดียว)
  - **ยกเลิกเลือกถ้ำ:** `GatherMapController.ConfirmTrip()` เรียก `ClearSelection()` หลัง `Dispatch()` → การ์ดดันเจี้ยนคลายไฮไลต์ทันทีที่กด DISPATCH
  - `TestUiRedesign` เพิ่ม 4 ข้อ (ถ้ำถูกยกเลิกเลือก, ป๊อปอัปขึ้น, ปุ่ม CONTINUE โผล่, กดแล้วปิด) → **ALL PASS 99 ข้อ**, console 0 error 0 warning

## 2026-10-30 (รอบ 4)

- **v0.27 (หน้ากาก OPTIONS ใช้ร่วมกับ MainMenu + สแกนชุด asset ใหม่)** —
  - **`OptionsPanelView` (ใหม่):** ยกหน้ากาก options ออกจาก `MainMenuController` มาเป็นคอมโพเนนต์เดียว สร้างด้วย `OptionsPanelView.Create(parent, primaryLabel, primaryAction, secondaryLabel?, secondaryAction?)` — ประกอบด้วย **RESOLUTION** (dropdown เลื่อนได้ + caret), **DISPLAY** (WINDOW / FULLSCREEN), **VOLUME** (slider + %) และหมายเหตุเสียง; ค่าเก็บใน `PlayerPrefs` คีย์เดิม (`clogged.resolution` / `clogged.fullscreen` / `clogged.volume`) ทั้งเมนูและในเกมจึงอ่านค่าเดียวกัน
  - **ในเกม:** ปุ่ม OPTIONS เปิดหน้าเดียวกันนี้ โดย footer เป็น **RETURN TO GAME** + **RETURN TO MENU** (ปุ่มคู่ล่างสุด) แทน CLOSE เดิม; MainMenu ยังเป็น **BACK** ปุ่มเดียว
  - `MainMenuController` หดจาก ~520 เหลือ ~108 บรรทัด (ลบโค้ด resolution list/slider/prefs ที่ซ้ำออกทั้งหมด)
  - `TestUiRedesign` เพิ่ม 3 ข้อ (ตรวจว่าหน้ามี resolution/display/volume จริง) → **ALL PASS 95 ข้อ**, console 0 error 0 warning
  - **สแกน asset ใหม่ 4 ชุด** (Kenney, CC0): board-game-icons (513 PNG 64/128px), game-icons (210 ไอคอนขาว/ดำ 50/100px), cursor-pack (182 ไอคอน 32/64px), cursor-pixel-pack (220 ไทล์ 16px) — ยังไม่ได้ import เข้าโปรเจกต์ รอเลือกชุดที่ใช้

## 2026-10-30 (รอบ 3)

- **v0.26 (เก็บงานละเอียดรอบสาม 4 ข้อ)** —
  - **การ์ดเลื่อนทั้งใบ:** `Background` + `Frame0..3` ย้ายเข้าไปอยู่ใต้ `Lift` (เดิมอยู่ใต้ root) ทำให้พื้นหลัง/ขอบเลื่อนตามข้อความเป็นชิ้นเดียวกัน และ `iconImage` ของ root กลายเป็นแค่พื้นกดใส (raycast target) ไม่แสดงผล; เพิ่ม `RefreshBackground()` ให้สีพื้นหลัง/ขอบอัปเดตพร้อมกัน
  - **พื้นหลังการ์ดทึบ:** ค่าในพรีแฟบเดิมเป็น `a = 0.90/0.95` ทำให้เห็นการ์ดดันเจี้ยนทะลุผ่าน → ตั้งเป็นทึบเต็ม (การ์ดที่ยกขึ้นจึงบังการ์ดดันเจี้ยนได้สนิท)
  - **การ์ดที่เลือกอยู่คนละเลเยอร์:** `CharacterHandPanel` วาดเหนือ `GatherMapController` → การ์ดที่ยกขึ้นทับการ์ดดันเจี้ยนได้ (เพิ่มเทสต์ยืนยัน draw order)
  - **ยกแผงมือขึ้น:** `CharacterHandPanel` y 20 → **92** เพื่อให้การ์ดที่พัก (จมลง 70 px) ยังอยู่ในจอ
  - **ดันเจี้ยนกินพื้นที่แนวตั้งเต็ม:** `GatherMapController` marginTop **300 → 110**, maxCardHeight **470 → 700** (แก้ทั้งค่าในสคริปต์และ override ใน `Core.unity`) → การ์ดสูง ≈**480×630** เริ่มที่ราว 75 px จากขอบบน และจบชิด LogPanel
  - **Core Item เป็นแนวตั้ง 2×1:** `CoreItemGroup` 118×240 วางซ้อนกันแนวตั้ง (y = 0 / −122) ตัดข้อความ `CoreItemStatus` ออก และ `RelocateCoreItemGroup` จัดขวาชิด DayFrame โดยไม่สเกล
  - `TestUiRedesign` เพิ่มเช็ก Lift/ลำดับการวาด/Core Item → **ALL PASS 92 ข้อ**, console 0 error 0 warning

## 2026-10-30 (รอบ 2)

- **v0.25 (แก้ตามรีวิวรอบสอง 6 ข้อ + บั๊กฟอนต์)** —
  - **บั๊ก: MainMenu ข้อความหาย** — มี `TMP Settings` 3 ชุด (`Assets/TextMesh Pro`, `Assets/Day/…`, `Assets/Night/…`) และชุดที่ Unity โหลดมี `defaultFontAsset = NULL` → ตั้ง `PixelTileset` ให้ทั้งสามชุด; เพิ่ม `TmpFont.Apply` ใน `UiFactory.CreateText` ด้วย
  - **ขนาดการ์ด:** player card 195×265 → **218×300** (สูงเท่า LogPanel) และขยาย `CharacterHandPanel` เป็น 936×300; dungeon card ขยายเป็น ≈**480×450** โดย `GatherMapController` คำนวณจาก margin (ซ้าย 32 / ขวา 380 / บน 300 / ล่าง 330) และ `MapNodeView.LayoutCard()` จัดสัดส่วนใหม่ตามขนาดการ์ด
  - **อนิเมชันหยิบการ์ด:** `restingOffset` 16 → **70 px**, `liftSpeed` 420 → 900
  - **Core Item:** ย้ายไปคอลัมน์เดียวกับ DayFrame แล้ววางใต้ DayFrame (เหนือแถบสกิล) และย่อ scale ให้กว้างเท่า DayFrame
  - **Craft mode:** ปุ่มสลับโหมดย้ายเข้า `CraftRow` แบบเดียวกับ `HealRow` (ปุ่มหลัก 156×64 + ปุ่มโหมด 56×64)
  - **MENU → OPTIONS:** ปุ่มซ้ายบนเปลี่ยนเป็น OPTIONS เปิดหน้า options ซึ่งมี **RETURN TO MENU** อยู่ข้างใน (Help ยังแยกปุ่ม)
  - **บั๊ก: overlay กดไม่ได้/ไม่ขึ้นบนสุด** — overlay ที่สร้างแบบ inactive ไม่ถูก batch เข้า canvas ทำให้ depth ค้าง -1 และการ์ดดันเจี้ยนกิน click → เปลี่ยนทั้ง Options/Help ไปใช้ `CanvasGroup` (alpha/blocksRaycasts) แทน `SetActive`
  - `TestUiRedesign` เพิ่ม 12 ข้อ → **ALL PASS 87 ข้อ**

## 2026-10-30

- **v0.24 (sprint review รอบ 16 ข้อ — P1–P4 ลงโค้ดครบ)** — ปรับตาม checklist + สเก็ตช์ 4 รูป:
  - **สูตร SV ใหม่ (§4.3):** `+25` ถนัดตรงระดับถ้ำ (ครั้งเดียว) · `−10 × ระยะห่างต่อคน` (E↔N 1 ชั้น, E↔H 2 ชั้น) · `+25` duo · **ตัดโทษ Hard คนเดียว −20** · trait ±10 · `+10`/คน Blessed · Buff Potion stack `+10` — เปลี่ยน `UnitCharacter` จาก `AptitudeTag` list เป็น `primaryAptitude` + `talent`; ตารางยูนิตใหม่ Ann(E/Heal) · Boon(H/Crafting) · Cia(N/Cooking) · Dan(H/Exploring)
  - **Talent:** Cooking ×2 อาหาร · Crafting ×2 ยา · Heal ให้ผู้ป่วย Blessed (+10 ทริปถัดไป) · Exploring ไอเทม ×2 (ยกเว้น Core Item) + `coreItemChancePercent +25`
  - **Core Item:** อัตรา Normal 25% / Hard 50% + **pity** (`coreItemPityTrips` 3/2, การันตี = สุ่มชิ้นที่ยังขาด, รีเซ็ตเมื่อดรอป) — บันทึกลง SaveData พร้อม `partySuccessBonus` และ `blessed`
  - **Base work:** Heal ไม่มียา 2→**3 AP** · Craft มี 2 โหมด (Medicine 1 AP + 2 Mat / Buff Potion 2 AP + 4 Mat → +10 SV ทั้งทีมถาวร) พร้อมปุ่มสลับโหมด
  - **การ์ดตัวละคร:** rebuild 6 ช่องตามภาพสเก็ตช์ — ชื่อ · E/N/H + ไอคอน talent · AP pip · ดาบ (state พร้อม) · portrait · แถวไอคอน buff/debuff แบบไฟหน้าปัด (เทา = ยังไม่มี) + พื้นหลังแดง/ส้มตาม Hurt/Hungry + outline สีตามความถนัด + อนิเมชันหยิบการ์ด (สไลด์ขึ้น)
  - **แก้บั๊ก:** AP preview ตอนเลือกผู้ป่วย heal ขึ้นบนคนเจ็บแทนคนรักษา (เพิ่ม `SetApCostPayer`) · สกิลกับถ้ำเลือกค้างพร้อมกัน (mutual exclusion) · `DiceRollView` อ้างฟิลด์ที่ถูกลบแล้ว (compile ไม่ผ่าน)
  - **แมปใหม่:** 3 การ์ดตายตัว Easy/Normal/Hard (เลิกสุ่ม 6 จุด) แต่ละใบมีโอกาส realtime / AP / ไอคอนของที่ดรอป / pity + ลูกศรชี้ใบที่เลือก
  - **HUD:** ปุ่ม MENU + HELP ซ้ายบน, หน้า Help เปล่า (รอ content), ย้าย Core Item ไปข้าง DayFrame, ปุ่มสลับโหมด Craft
  - **อื่น ๆ:** `TmpFont` แก้ label ที่สร้างจากโค้ดไม่มีฟอนต์ (ทำให้ `TMP_Settings.defaultFontAsset` = null ตอนรัน) · คืน shader `TextMeshPro/Bitmap` ให้ `PixelTileset.mat` (เดิมตกเป็น `Hidden/InternalErrorShader` ทำให้ข้อความทั้งเกมเป็นสีม่วง) · `TestUiRedesign` เพิ่ม 13 ข้อ → **ALL PASS 75 ข้อ**, console 0 error 0 warning

>>>>>>> origin/Kagerou
## 2026-10-28

- **v0.23 (คริสตัลเป็น popup กลางจอ — §7.20 ปรับ, §7.21)** — รีวิว: การเอาคริสตัลไปแทนช่องกล่องทำให้ HUD อ่านไม่เป็น inventory (ของที่ถืออยู่หายไปกลายเป็นอย่างอื่น) → ช่องกล่องยังเป็น**กล่อง**แต่เปลี่ยนเป็น **ฝาเปิด** (`coreitem_panel_orange_bar` / `coreitem_panel_yellow_bar`) และย้ายคริสตัลไปเป็น popup กลางจอ · เพิ่ม `Scripts/UI/CrystalRevealView.cs` — สร้าง UI เองในโค้ด (backdrop ทึบ 0.82, คริสตัล `192×192` = 16× ของ 12×12, caption "THE BOX IS OPEN" จากฟอนต์ pixel), เฝ้า edge `HasBothCoreItems` ใน `Update()`, fade ด้วย `Mathf.MoveTowards` ไม่ใช้ coroutine (เทสต์ในเฟรมเดียว/ตอน pause ยังได้ค่าเดิม), `wasOpen` เริ่มจากค่าจริงใน `Awake` กันเด้งซ้ำตอนโหลดซีนที่ชนะแล้ว, คลิกที่ไหนก็ปิด · `CoreItemArt` เพิ่ม field `openBox`, `StatusBarView` ใช้ `_bar` แทนคริสตัล · `TestUiRedesign` เพิ่ม 3 ข้อ และข้อ raycast ข้าม popup ตอนปิด → **ALL PASS 52 ข้อ** · AC ข้อ 33–34
- **v0.22 (Core Item ใช้สไปรต์จริง + `ap_30` เฉพาะการ์ดที่เลือก — §7.19 ภาคผนวก, §7.20)** — `ap_30` เดิมขึ้นบน**ทุกการ์ด**ทั้งที่ไม่ได้เลือก → ย้ายเงื่อนไขไปที่ `UnitIconView.ApCostPreview => IsSelected ? pendingApCost : 0`, `RefreshApPips()` อ่านค่านี้ และ `SetSelected()` เรียก `RefreshApPips()` ให้เปลี่ยนในเฟรมเดียว · ช่อง Core Item เปลี่ยนจากกรอบเปล่า+tint เป็นสไปรต์จริงตามคู่สีของ run: `CoreItemVariant { Orange, Yellow }` ทอยครั้งเดียวใน `RunResources.Awake` (ปิดได้ด้วย `randomiseVariant`, บังคับได้ด้วย `SetVariant`) แล้วทั้งกล่องและกุญแจอ่านคู่เดียวกัน → กุญแจที่ได้เปิดกล่องได้เสมอ · กล่องส้ม = `coreitem_panel_orange` + กุญแจส้ม `coreitem_key_lit` + คริสตัล `coreitem_crystal` · กล่องเหลือง = `coreitem_panel_yellow` + กุญแจเทา `coreitem_key_plain` + คริสตัล `coreitem_crystal_b` · `HasBothCoreItems` (เงื่อนไขชนะเดิม) เป็นสวิตช์ "เปิดกล่อง" → ช่องกล่องกลายเป็นคริสตัล · `StatusBarView` เพิ่ม inner class `CoreItemArt` + ตาราง `orangePair`/`yellowPair`, `ApplyCoreItemIcon` รับทั้ง placeholder กับ art (มี art → tint ขาว ให้สีจริงแสดง) · ข้อความ HUD `CORE A/B` → `BOX/KEY` · log `Found Core Item A!` → `Found the sealed box!` / `Found the key!` · ตั้งไอคอน 96×96 กลางกรอบ (6×/8× ของต้นฉบับ = จำนวนเต็มเท่า พิกเซลไม่เพี้ยน) · `TestUiRedesign` เพิ่ม 3 ข้อ (1 pip + 2 คู่สี) → **ALL PASS 49 ข้อ** · AC ข้อ 32–33
- **v0.21 (AP บนการ์ดเป็น pip — §7.19)** — `statusText` เดิมพิมพ์ `AP 3/3` เป็นตัวเลข เปลี่ยนเป็น **1 pip ต่อ 1 AP** จากไอคอนที่ตัดไว้ใน §7.18: `ap_45` = ยังมี · `ap_30` = action ที่กำลังจะยืนยันจะกิน pip นี้ · `ap_15` = ใช้ไปแล้ว · `UnitIconView` เพิ่ม `BuildApPips()`/`RefreshApPips()`/`SetApCostPreview()` และย้าย `statusText` ลง `apPipRowHeight + 2` (สูง 36 → 32) — บรรทัด AP ถูกแทนที่ จึงไม่ต้องขยายการ์ด · พรีวิวต้นทุนให้ `UnitRosterView.CurrentPendingApCost()` **อ่าน** จาก `SkillFlowController.PendingApCost` (base × จำนวนรอบ) หรือ cave ที่เลือกบน map แทนการ push จาก panel เพราะสองเจ้าของเขียนค่าเดียวกันจะตบกัน · `BaseWorkController` เพิ่ม `CookBaseApCost` / `WorkshopBaseApCost` / `HealBaseApCost(bool)` (ต้นทุนก่อน starvation เพราะต้องเพนต์หลายการ์ด) แล้ว `UnitIconView` บวก `GetActualApCost` เอง · ผูกสไปรต์บน `ScoutIcon.prefab` ด้วย `AgentScripts/WireApPips.cs` · `TestUiRedesign` เพิ่ม 3 ข้อ + ขยาย 1 ข้อ → **ALL PASS 46 ข้อ** · AC ข้อ 30–31
- **v0.20 (ตัดไอคอน AP + CoreItem จาก tileset — §7.18)** — เพิ่ม `Assets/V2/Sprites/Icons/` 11 สไปรต์จากสองชีตเดิม: **AP 3 สถานะ** จาก slice ที่มีชื่ออยู่แล้วใน `.meta` ของ `tilemap_packed.png` (`tilemap_packed_45` = มี AP · `_30` = กำลังจะถูกใช้ (พรีวิวต้นทุน) · `_15` = ใช้ไปแล้ว) ตัดตาม `rect` ตรง ๆ 10×12 · **CoreItem 8 ไอคอน** จาก `tilemap_packed 1.png` (คริสตัล 2 แบบ 12×12 · กุญแจหัวเงิน/หัวส้ม 16×10 · แผงส้ม/เหลือง และรุ่นมีแถบดำ 16×14/16×16) ตัดด้วย bbox ของ alpha เพราะชีตเป็น packed sheet ที่ไอคอนล้ำเซลล์ · ตั้ง import เป็น pixel-art sprite (`Point`, `FullRect`, `Uncompressed`, no mipmap, PPU 100) ด้วย `AgentScripts/ImportIconSprites.cs` · **ยังไม่ต่อเข้า UI** — รอเลือกตำแหน่ง pip บนการ์ดและจับคู่ Core Item A/B · ไม่มีการย้าย/คัดลอกไฟล์ tileset เดิม (reference เดิมไม่กระทบ)
- **v0.19 (สี Warning ตรงกับ log — §7.17)** — ข้อความเดียวกันเคยเป็นสองสี: log เหลืองอำพัน แต่แบนเนอร์ขาว (+ outline แดง) เพราะสองฝั่งมีตารางสีและ enum ของตัวเอง (`LogKind` 6 ค่า vs `WarningKind` 3 ค่า) · เพิ่ม **`Scripts/UI/UiPalette.cs`** เป็นตารางสีเดียว (`For(LogKind)`) ให้ทั้ง `LogMessagePanel` และ `WarningView` อ่าน · **ลบ `WarningKind`** และเปลี่ยน API เป็น `WarningView.Show(text, LogKind)`, ลบ `ShowWarning/ShowError/ShowInfo` · ลบ outline material ที่ `WarningView` เพราะ shader `TextMeshPro/Bitmap` **ไม่มี property outline เลย** (ที่ผ่านมาจึงเป็น no-op) → สีมาจาก `label.color` ตรง ๆ · `SkillFlowController.Warn()` เพิ่ม `RunLog.Add(LogKind.Warning, …)` ให้คู่ banner↔log ครบตาม AC ข้อ 1 · `LogMessagePanel` เลิกใช้ 6 color field ใน Inspector · วัดจากภาพ: แบนเนอร์ `#FFB840` = log `#FFB840` · `TestUiRedesign` เพิ่ม 3 ข้อ (เทียบสีที่ render จริงของทั้งสองฝั่ง) → **ALL PASS 42 ข้อ** · AC ข้อ 29 · อัปเดต [04](./software/04-unity-system-design.md) / [05](./software/05-unity-class-diagram.md) / [06](./software/06-unity-core-scene-hierarchy.md)
- **v0.18 (จัดกลุ่ม `Warning` ให้ parent/child ตรงกัน — §7.16)** — หลังรวม `WarningBackdrop` + `WarningText` ไว้ใต้ parent `Warning` ลูกทั้งคู่ยังถือ `pivot`/`anchoredPosition` เดิมจากตอนเป็น object อิสระ (backdrop pivot (0,0) offset (300,60) · text pivot (.5,.5) offset (250,60)) ทำให้หลุดออกนอก parent · แก้เป็น stretch + pivot .5 + offset 0 (backdrop inset 0, text inset 16×12) · รวม `CanvasGroup` เหลือ **ตัวเดียวบน `Warning`** (เดิมมี 3 → alpha คูณกันเป็นกำลังสองตอน fade) และย้าย `WarningView` ไป root พร้อม repoint `warningView` 2 จุด (`GatherDispatchPanel`, `SkillFlowController`) · ขนาด group 200×60 → **620×120** เพราะประโยคเต็มขึ้น 4 บรรทัด · `WarningView.SetAlpha` ข้าม `backdropGroup` ถ้าเป็นตัวเดียวกับ `group` และเปลี่ยน `messageText` → `label` ในเอกสาร · `TestUiRedesign` เพิ่ม 2 ข้อ → **ALL PASS 39 ข้อ** · AC ข้อ 28 · [06-unity-core-scene-hierarchy.md](./software/06-unity-core-scene-hierarchy.md) อัปเดต hierarchy + ตาราง Inspector ของ `WarningView`
- **v0.17 (เปลี่ยน TMP ทั้ง V2 UI มาใช้ฟอนต์ pixel — §7.15)** — `Tools > V2 > Apply Pixel Font To V2 UI` (`Assets/V2/Editor/ApplyPixelFont.cs`): ตั้ง `TMP_Settings.m_defaultFontAsset` = PixelTileset (ครอบ label ที่สร้างจากโค้ดอย่าง `ZoneLabel`), ไล่แก้ TMP ทุกตัวใน `Assets/V2/Prefabs/**` + `Core.unity` ผ่าน `SerializedObject` (อ่าน/เขียน `m_fontAsset` + `m_sharedMaterial`) เพราะ `text.font` โยน `MissingReferenceException` เมื่อ reference ตาย · builder เพิ่ม **วรรคตอน 22 ตัวแบบ procedural** (`: ( ) / [ ] , . – — …`) ในสไตล์ 2px เท่ากัน, เปลี่ยน `pointSize` 16→**11** ให้ cap/em = 0.727 เท่า LiberationSans, ตัด padding 5px ของไทล์ออกจาก `bearingX` (advance 13→7), snap ตัวเลขที่ต่ำกว่า cap 1px ให้อยู่บน cap line, `AdvanceGap` 2→1 · ขยาย label `Food` +80px และเลื่อน label ที่เหลือตาม เพื่อไม่ให้ตัดบรรทัด · ฟอนต์รวม **88 glyph / 88 character** · `TestUiRedesign` เพิ่ม 2 ข้อ → **ALL PASS 37 ข้อ**, console 0 error 0 warning · AC ข้อ 26–27 · **ไม่แตะ** `Assets/Day` / `Assets/Night`
- **v0.16 (ไฮไลต์คนไข้ + ฟอนต์จาก tileset — §7.13, §7.14)** — เพิ่ม `UnitIconView.SetTargetable()` + `targetableColor` (มินต์) และ `UnitRosterView.IsHealTargeting()`: ตอนกด Heal การ์ดของคนที่เจ็บจะได้กรอบหนา alpha เต็มและเต้น (เดิมจาง 0.55 เพราะ `state != Idle` ทำให้คนที่ต้องเลือกดู "กดไม่ได้" ที่สุด) · `AlphaForState()` แยกออกมาให้ `targetable` override alpha ได้ · เพิ่ม `Assets/V2/Editor/PixelFontBuilder.cs` (+ เมนู `Tools > V2 > Rebuild Pixel Font`) สร้าง `Assets/V2/Fonts/PixelTileset.asset` จากไทล์ตัวอักษรใน `tilemap_packed.png` — 39 glyph (A–Z, 0–9, `% + -`) + พับ lowercase + space = 66 character, mask ขาวล้วนเพราะ TMP tint ด้วย alpha (ถ้าคัดลอกทั้งไทล์จะได้บล็อกทึบ), fallback เป็น LiberationSans SDF · `TestUiRedesign` เพิ่ม 3 ข้อ → **ALL PASS 35 ข้อ** · AC ข้อ 24–25
- **ตรวจ tileset:** `tilemap_packed.png` 288×176 กริด 16×16 ไม่มี gutter · ตัวอักษรอยู่แถว 6–8 (A–M, N–Z, `%+-0–9` แบบมีเงา) และแถว 9–10 (A–Z แบบแบน ไม่มีตัวเลข) → เลือกแถว 6–8 เพราะครบตัวเลข

## 2026-10-27

- **UI Layout Redesign Spec (Draft v0.2)** — ร่างสเปก UI ใหม่จากแบบร่างกระดาษ 3 โซน:
  - สร้าง [07-unity-v2-ui-layout-redesign.md](./software/07-unity-v2-ui-layout-redesign.md): layout ใหม่ทั้งจอ (CoreItemA/B, DayFrame+EndDay, LogMessagePanel, CharacterHandPanel, DispatchActionPanel, SkillTriggerPanel), interaction model (คลิกซ้าย select/เพิ่มจำนวน, คลิกขวาล้างจำนวน), เงื่อนไขชนะ/แพ้จาก Core Item 2 ชิ้น, contracts L1–L5, ผลกระทบรายไฟล์, phases P0–P5, คำถามเปิด Q1–Q8
  - **v0.13 (P5 implemented — ครบทุก phase):** ลบของเดิมออกจาก `Core.unity` ทั้งหมด — `EndDay button`, `BaseWorkPanel` (+ ลบไฟล์ `.cs`), `CharCard`, `MapPanel/MessageUi`, `MapPanel/Dispatch`, `MapPanel/ScoutIcon` (+ ลบ `ScoutWalkerView.cs` และ property `TripInProgress`) · `DayFrame` เป็นปุ่มจบวันเองผ่าน `EndDayTrigger` (ไม่มี confirm) · เพิ่ม `CoreItemFrameB` + ผูก `StatusBarView.coreItemBIcon` · ลบ missing script ของ `ScoutWalkerView` ออกจาก `ScoutIcon.prefab` · เปลี่ยน `✓` เป็น ASCII เพราะฟอนต์ไม่มี glyph · `GatherDispatchPanel` เลิกใช้ `messageText` (ข้อมูลถ้ำ + คำเตือน ลง log/warning แทน) · ปรับ map margin กันถ้ำไปอยู่ใต้ panel · LogPanel ทึบ 100% — ทดสอบผ่าน 19/19 (P5) + 35/35 (P4) + 17/17 (P3), console 0 error/warning · เพิ่ม §7.10, AC ข้อ 20–22
- **v0.14 (แก้ bug: กด DayFrame ไม่ได้ — §7.11):** ทุก Graphic ใต้ `DayFrame` มี `raycastTarget = false` ทำให้ raycast ทะลุไป `MapPanel` → เปิด `raycastTarget` ที่ `DayBackground`, ปิดที่ `Day`, เพิ่ม `EndDayTrigger.EnsureClickSurface()` กัน bug กลับมา + warning "The run is already over." · พบถ้ำ 1 ใบ rect คาบเกี่ยวกรอบ DAY → ขยาย margin เป็น R400/T300 · เพิ่มการตรวจ clickability ด้วย `EventSystem.RaycastAll` จริง (ไม่ใช่เรียก handler ตรง ๆ) · `TestCleanup` เพิ่ม 3 ข้อ (raycast hit, handler resolve, ไม่มีถ้ำใต้กรอบ) — ยอดรวม P5 = 22/22 · AC ข้อ 23
  - **v0.15 (รวมชุดทดสอบ — §7.12):** ยุบ 4 สคริปต์ (`TestCleanup`, `TestSkillFlow`, `TestDispatchGating`, `TestClickTargets`) เป็น **`AgentScripts/TestUiRedesign.cs`** ไฟล์เดียว **32 ข้อ รันครั้งเดียวจบ** (เข้า play mode รอบเดียวแทน 4 รอบ) · แก้ความสามารถในการรันซ้ำ: `ResetUnits`/`Revive` แยกกันเพราะบาดแผลคงอยู่ข้ามวัน, `FirstSelectable` แทน `Cards[0]`, `ForceSkillPanelRefresh` เพราะ UI รีเฟรชใน `Update` · ลบสคริปต์ one-shot ที่ทำเสร็จแล้วทั้งหมด (กันรันซ้ำไปทับค่าที่แก้) · เพิ่ม `ProjectSettings/Packages/com.unity.pipeline/` ใน `.gitignore`
  - **เขียน [06-unity-core-scene-hierarchy.md](./software/06-unity-core-scene-hierarchy.md) ใหม่เป็น v3.0** — hierarchy จริงหลัง P5 (`MapPanel` เหลือแค่พื้นหลัง, `EndDayTrigger` บน `DayFrame`, 2 ช่อง Core Item, `CharacterHandPanel` / `DispatchActionPanel` / `LogPanel` / `SkillTriggerPanel`), ตาราง Inspector reference ของทุก UI script, ตาราง "สิ่งที่ถูกลบไปแล้ว" กันเอกสารอื่นอ้างของเก่า, `TestDispatch` → `GatherUI`
  - **v0.12 (P3 + P4 implemented):** ต่อ hand + ปุ่มยืนยันตัวเดียว + สกิลทั้งชุดเข้า `Core.unity` — `CharacterHandPanel` (โหมด `Dispatch` / `Skill` / `HealTarget`), `DispatchActionPanel` (ซ่อนเมื่อว่าง, label ตามบริบท, gate ข้าม flow ด้วย `IsSkillActive`), `UnitRosterView` คำนวณ selectability ใหม่ทุกเฟรม, `GatherMapController.ConfirmTrip` resolve ทันที; เพิ่ม `SkillTriggerPanel` (Cook/Craft counter + คลิกขวาล้าง, Heal ไม่มี queue + toggle `MED`/`AP`), `RightClickHandler` และ `SkillFlowController`; ปิด `BaseWorkPanel` เดิมในซีน — ทดสอบ play mode ผ่าน 35/35 (สกิล) + 16/16 (regression การสำรวจ) · เพิ่ม §7.8/§7.9 และปรับ §4.5/§4.6 ให้ตรงที่ลงมือจริง (กฎเลือกการ์ดใช้ `CanBeDispatched` ร่วมกันทั้ง 2 flow)
  - **v0.11 (P3 started):** ปรับ `CharacterHandPanel` / `DispatchActionPanel` / `UnitRosterView` / `UnitIconView` ให้ hand ใช้งานได้จริง, `GatherMapController` เลิกผูก `TripInProgress`, `BaseWorkPanel` เลิก gate ระหว่างทริป
  - **v0.10 (P2 log wiring):** `LogMessagePanel` render จาก `RunLog` ด้วย row pool + ย่อ/กางพร้อมย่อความสูง panel (300→64) · เพิ่ม `RunLogNarrator` แปลง event เป็น log (`OnBaseWorkCompleted`, `OnDayEnded`, `OnGatherResolved`, `OnRunEnded`) · `BaseWorkController` เพิ่ม `OnBaseWorkCompleted` + overload รับ `useMedicine` · `DayManager` เพิ่ม `DayEndReport` / `PreviewFeeding()` / `OnDayEnded` · ทดสอบใน play mode ผ่าน (capacity 16, minimize, narrator) — เพิ่ม §7.7
  - **v0.9 (P1 prefabs):** สร้าง prefab skeleton 4 ตัวใน `Assets/V2/Prefabs/UI/` (`LogPanel`, `CharacterHandPanel`, `DispatchActionPanel`, `SkillTriggerPanel`) ด้วย `AgentScripts/BuildUIPrefabs.cs` — ตรวจด้วยภาพผ่าน Canvas ชั่วคราวแล้ว แก้ 2 จุด (hand กว้าง 800 + การ์ด 190 กันทับ Log, ปุ่มสลับ Heal เปลี่ยนจาก glyph `⇄` เป็นข้อความ `MED`/`AP`), เพิ่ม §3.3 ตัวเลข layout ที่ล็อก และ §7.6 รายละเอียด prefab · ไม่แตะ `Core.unity`
  - **v0.8:** ปิด Q8 — **เกมไม่มีช่วง in-progress**: dispatch resolve ทันที ผลขึ้น log ทันที ตัวละครเป็น Busy (`HasGatheredToday`) แต่ยังทำสกิลได้ถ้ามี AP, **End Day กดได้ทุกเมื่อ** (รีเซ็ตสถานะ + คืน AP); ลบ `TripInProgress`/`ScoutWalkerView`/ฐานบน map ออกจากสเปก, เพิ่ม selectability ตามโหมด (dispatch = `CanBeSentGathering`, skill = `CanBeDispatched`), เพิ่ม contract L7/L8, ปรับ AC เป็น 19 ข้อ
  - **v0.7:** ปิดคำถาม Q1–Q7, Q9 ตามการตัดสินใจ — ResourceStrip คงกลางบน · โหมดสกิลเลือกการ์ด **1 ใบ** · skill panel แสดงตลอด · **Log อยู่มุมซ้ายล่างและห้ามล้ำเข้า MAP AREA** (เก็บ 16 entry, แสดงเท่าที่ช่องใส่ได้) · **Heal ไม่มี queue** (เลือกผู้ทำ → Heal → เลือกผู้ป่วย → ยืนยัน) · **ไม่มี confirm dialog ก่อนจบวัน** · Easy ดรอป 0% — เหลือ Q8 (warning ระหว่างทริป) และบันทึกว่าต้องเพิ่ม overload `BaseWorkController` รับ `useMedicine`
  - **v0.6 (P0 implemented):** ลงมือส่วน data & logic แล้ว — เพิ่ม `CoreItemKind`, `RunLog`/`LogEntry`/`LogKind` (static, capacity 200), `RunResources` (A/B + `HasBothCoreItems` + `OnChanged`), `GatherZoneConfig.coreItemChancePercent` (Easy 0 · Normal 5 · Hard 15), `TryRollCoreItems` (เฉพาะ HardSuccess), `DayManager` ใช้ `HasBothCoreItems`; อัปเดต `StatusBarView`/`GatherDispatchPanel` ให้ compile และแสดง A/B · compile ผ่าน, ทดสอบใน Editor แล้ว (0 error)
  - **v0.5:** ลด Hard Core Item chance จาก 25% → **15%** (ผลจริงต่อทริป ≈ 8.9% เทียบ Normal ≈ 4.4%)
  - **v0.4:** ปรับโมเดล Core Item drop ให้ตรงความหมาย "โอกาสต่อทริป" — ฟิลด์เดียว `coreItemChancePercent` **ทอยเฉพาะ tier `HardSuccess`**, ทอยแยกต่อชิ้นและข้ามชิ้นที่มีแล้ว (มีครบ 2 = ไม่ทอย); ค่าเริ่มต้น Easy 0% · Normal 5% · Hard 25% (จริงต่อทริป ≈ 4.4% vs 14%) ลบ `CoreItemDrop`/`hardSuccessChance`/`regularChance`
  - **v0.3:** Core Item ดรอปผ่าน **drop table ใน `GatherZoneConfig`** (แบบ 2) — ต่อ entry มี `hardSuccessChance` / `regularChance`, ทอยแยกอิสระต่อชิ้น, เฉพาะ `HardSuccess`/`Regular`; ค่าเริ่มต้น Normal 40/15 และ **Hard 75/25** (Hard ดรอปมากกว่า) เพิ่ม contract L6
  - **v0.2:** Core Item ทั้ง 2 ชิ้น (A = escape, B = key) **ดรอปอิสระจากกัน ไม่มีลำดับ** — ตัดกฎ "ต้องได้ key ก่อน" และบันทึกผลกระทบต่อ `GatherDispatchController.HandleCoreItemProgress` / `GatherDispatchResult` (เจ้าของ = กลุ่ม 2 ต้องประสาน)
  - อัปเดต [index.md](./index.md) ส่วน Software Design ให้ลิงก์เอกสาร Unity V2 ครบชุด
  - **สถานะ: รออนุมัติ — ยังไม่มีการแก้ code หรือ scene**
- **ถอด MCP Unity ออกทั้งหมด** (เลิกใช้ MCP bridge, ใช้ `unity` CLI / unity-pipeline แทน):
  - ลบ `mcp-unity/` (README + `setup.ps1` / `setup.cmd` / `setup.sh` + `cloned/` ~375 MB), `com.gamelovers.mcp-unity` ถูกถอดออกจาก `Unity-Projects/Packages/manifest.json` แล้วโดย commit `53a2807`.
  - ลบ `.agents/skills/unity-mcp-tools/`, `Unity-Projects/ProjectSettings/McpUnitySettings.json`, `Unity-Projects/AgentScripts/RestartMcp.cs`, `Unity-Projects/opencode.json`.
  - ลบ `mcp` block ออกจาก `.kilo/kilo.json` และกฎ ignore ของ `mcp-unity/cloned/` ออกจาก `.gitignore`.
  - ลบ `docs/wiki/agentic-ai/unity-mcp-setup.md` และลิงก์อ้างอิงใน [wiki hub](./wiki/wiki.md), [Naplab AI Provider](./wiki/agentic-ai/ai-provider-naplab.md).
  - อัปเดต skill `unity-csharp-conventions` และ `unity-scene-workflow` ให้ใช้คำสั่ง `unity command …` แทนเครื่องมือ MCP เดิม.

## 2026-10-14

- **Unity V2 Documentation** — เขียนจากโค้ดจริงใน `Unity-Projects/Assets/V2/` (scene `Core.unity`):
  - Software: เพิ่ม [04-unity-system-design.md](./software/04-unity-system-design.md) (โครงสร้าง, Scene Flow, ระบบหลัก, data flow), [05-unity-class-diagram.md](./software/05-unity-class-diagram.md), [06-unity-core-scene-hierarchy.md](./software/06-unity-core-scene-hierarchy.md) (Hierarchy ทั้งหมดของ `Core.unity`); ใส่หมายเหตุใน 01–03 ว่าเป็นเอกสาร Web Prototype
  - GDD v2.0: เขียน [00-concept](./gdd/00-concept.md), [01-mechanics](./gdd/01-mechanics.md), [02-narrative](./gdd/02-narrative.md), [03-art-direction](./gdd/03-art-direction.md), [04-audio-direction](./gdd/04-audio-direction.md) ใหม่ตามระบบ V2
  - ย้าย GDD v1.0 (Web Prototype) ไป [wiki/archive/gdd-v1-prototype](./wiki/archive/gdd-v1-prototype/README.md)
  - อัปเดต [index.md](./index.md)
- **V2 Task Split (3 Developers)** — เพิ่ม [V2-TASK-OVERVIEW](./agile/user-stories/V2-TASK-OVERVIEW.md) (file ownership matrix, interface contracts, กติกาแก้ `Core.unity`), [US-V2-FACTORY-01](./agile/user-stories/US-V2-FACTORY-01.md), [US-V2-SCENE-01](./agile/user-stories/US-V2-SCENE-01.md), [US-V2-UI-01](./agile/user-stories/US-V2-UI-01.md); เพิ่ม section Unity V2 ใน [Product Backlog](./agile/01-product-backlog.md)

## 2026-08-21

- **Task & Progress Tracker Weekly Report (W34)**:
  - Generated [weekly-2026-W34.md](./agile/reports/weekly/weekly-2026-W34.md) under `docs/agile/reports/weekly/` documenting individual contributions and technical milestones in Sprint 02a.
  - Documented Unity 2D Day Scene Navigation (`ShipNavigatorUI.cs`, `DayEndSummaryUI.cs`, `RecruitEventUI.cs`), Phaser State Persistence (`TravelScene.ts`, `NightScene.ts`), and Software Class Design updates.
  - Updated [05-report-backlog.md](./agile/05-report-backlog.md).

## 2026-08-08

- **Task & Progress Tracker Weekly Report (W32)**:
  - Generated [weekly-2026-W32.md](./agile/reports/weekly/weekly-2026-W32.md) under `docs/agile/reports/weekly/` using the `task-tracker` skill.
  - Updated User Stories status: [US-TIME-02](./agile/user-stories/US-TIME-02.md) (🟢 DONE), [US-MISSION-02](./agile/user-stories/US-MISSION-02.md) (🟢 DONE), [US-BASE-02](./agile/user-stories/US-BASE-02.md) (🟢 DONE).
  - Updated [Product Backlog](./agile/01-product-backlog.md), [Sprint 01 Backlog](./agile/sprint-backlogs/sprint-01.md), [Report Backlog](./agile/05-report-backlog.md), [Kanban Board](./agile/kanban.md), and [Index](./index.md).

## 2026-08-07

- **Sprint 01 System & Progress Audit Report**:
  - Created [2026-08-07-sprint-01-progress-report.md](./agile/reports/2026-08-07-sprint-01-progress-report.md) under `docs/agile/report-logs/` documenting updates in Phaser prototype and Unity C# architecture (`Assets/Day` & `Assets/Night`).
  - Updated [05-report-backlog.md](./agile/05-report-backlog.md) hub to index the new progress report.
  - Updated [kanban.md](./agile/kanban.md) and [index.md](./index.md) to reflect current project status ahead of Decision Gate (2026-08-09).

## 2026-08-03


- **Game Loop Audit & User Story Breakdown** — Audit `src` against [gdd/00-concept.md](./gdd/00-concept.md):
  - Created [US-TIME-02.md](./agile/user-stories/US-TIME-02.md) — Day Cycle Bug Fix & Clean State Reset
  - Created [US-MISSION-02.md](./agile/user-stories/US-MISSION-02.md) — Collaborative Mission System
  - Created [US-BASE-02.md](./agile/user-stories/US-BASE-02.md) — Night Phase Base Defense & Escalation
  - Created [US-FOOD-02.md](./agile/user-stories/US-FOOD-02.md) — Starvation & Crew Health Decay
  - Created [US-MAP-01.md](./agile/user-stories/US-MAP-01.md) — Dynamic Map Generation & Difficulty Scaling
  - Created [US-EQUIP-01.md](./agile/user-stories/US-EQUIP-01.md) — Equipment Crafting & Perk Synergy
  - Updated [01-product-backlog.md](./agile/01-product-backlog.md) with updated status and links

## 2026-07-30


- **GDD v1.0 Reverse-Engineered from prototype source code** — Documented all mechanics as implemented in `src/src/`:
  - [00-concept.md](./gdd/00-concept.md) — Full game concept with scene flow, systems architecture, mission chaining
  - [01-mechanics.md](./gdd/01-mechanics.md) — Day/Night cycle, crew stats, perks, resources, map gen, time system
  - [02-narrative.md](./gdd/02-narrative.md) — World setting, monster ecology, character backgrounds
  - [03-art-direction.md](./gdd/03-art-direction.md) — Color palette, UI layout, visual elements, future art direction
  - [04-audio-direction.md](./gdd/04-audio-direction.md) — Audio plan (currently no audio in prototype)
- Updated `index.md` to link to new GDD file structure

## 2026-07-24
- **GDD อัปเกรดเป็น v0.3 (Day Phase Clarified)** — แปลงจาก Google Doc ต้นฉบับ:
  - ปรับ `gdd/00-concept.md` เป็นโครงสร้าง 3 Phase (Ship → Day → Night), Node Map Roguelike, Tower Defense Hybrid
  - ปรับ `gdd/01-mechanics.md` เป็นกลไกฉบับสมบูรณ์ของ v0.3 (Time System, Unit Specialty, ระบบป้องกัน 4 ทิศทาง, Facility, Crafting)
  - เพิ่ม Design Evolution section ใน concept doc เชื่อมโยงระหว่าง Draft เดิม → Prototype → v0.3
- อัปเดต `docs/index.md` header แจ้งเตือนการเปลี่ยนแปลง

## 2026-07-18
- **Refactored MCP Unity server**:
  - Moved `mcp-unity` out of the AI Gateway folder to the workspace root `mcp-unity/` to make it modular and separate from API routing.
  - Added automation setup scripts (`setup.ps1` and `setup.cmd`) to handle cloning, installing, and building of the external `CoderGamester/mcp-unity` server.
  - Updated configuration files (`kilo.json` and `Unity-Projects/opencode.json`) to use a deterministic static path, resolving the issue with variable commit hashes in the Unity PackageCache.
  - Configured git ignore rules to ignore only the cloned external repository folder (`mcp-unity/cloned/`) while tracking project-specific setup and documentation files.
  - Updated wiki documentation (`docs/wiki/unity-mcp-setup.md`) and added it to the central knowledge hub (`docs/wiki/wiki.md`).

## 2026-07-13
- **Restructured** entire doc suite to the `docs/gdd/`, `docs/software/`, `docs/agile/`, `docs/wiki/` layout.
- **Created** `gdd/00-concept.md`, `gdd/01-mechanics.md` — derived from the original `Idea-design.md` draft, reconciled against the actual `src/` codebase. Flagged an unresolved **design pivot**: the shipped prototype is a crew-dispatch/day-night survival game, not the factory-pipeline concept in the original draft.
- **Created** `software/01-system-design.md`, `software/02-class-diagram.md` — reverse-engineered from `src/src/` (entities, systems, scenes, ui).
- **Created** `agile/01-product-backlog.md` with Must/Should/Nice-have items, status-tagged against what's actually implemented.
- **Created** `agile/02-sprint-planning.md` (migrated from `plan.md`) and `agile/sprint-backlogs/sprint-01.md` for the Phase 1 / Prototype sprint (2026-07-13 → 2026-08-09).
- **Migrated** `team.md` → `agile/team.md`; `Meeting Notes/*.md` → `agile/meeting-backlogs/*.md` + new hub `agile/03-meeting-backlogs.md`.
- **Created** stub hubs: `agile/04-retrospectives-backlog.md`, `agile/05-report-backlog.md`, `agile/kanban.md` (no entries yet — sprint 01 just started).
- **Archived** the original `Idea-design.md` verbatim to `wiki/archive/idea-design-draft.md` for historical reference.
- **Created** `wiki/wiki.md` knowledge hub and `wiki/guidelines/system-test-guideline.md` starter guideline.
- **Created** `index.md` as the project's doc inventory and entry point.
- **Flagged for follow-up:** root `README.md` states Unity as the engine; the actual prototype uses Phaser 3 + TypeScript + Vite.
