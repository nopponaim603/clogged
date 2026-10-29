// src/scenes/DefenseScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG, RESOURCE_ICONS } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Systems } from '../main';
import { Monster, MonsterType } from '../entities/Monster';
import { Crew } from '../entities/Crew';

export class DefenseScene extends Scene {
    // Config
    private readonly BASE_X = 640;
    private readonly BASE_Y = 360;
    private readonly BASE_RADIUS = 120;
    
    // State
    private raidId: string = '';
    private monsters: Monster[] = [];
    private defenders: Crew[] = [];
    private totalEnemies: number = 0;
    private killedEnemies: number = 0;
    private escapedEnemies: number = 0;
    private baseDamageTaken: number = 0;
    
    // Spawn
    private spawnTimer: Phaser.Time.TimerEvent | null = null;
    private spawnInterval: number = 2000;
    private enemiesSpawned: number = 0;
    
    // UI
    private container!: Phaser.GameObjects.Container;
    private dayText!: Phaser.GameObjects.Text;
    private timeText!: Phaser.GameObjects.Text;
    private enemyText!: Phaser.GameObjects.Text;
    private engineText!: Phaser.GameObjects.Text;
    private resultText!: Phaser.GameObjects.Text;
    
    // Defense
    private turrets: Phaser.GameObjects.Arc[] = [];
    private isComplete: boolean = false;
    private defenseTimer: Phaser.Time.TimerEvent | null = null;
    private remainingHours: number = 0;
    
    constructor() {
        super('DefenseScene');
    }
    
    init(data: any): void {
        this.raidId = data.raidId || '';
        this.remainingHours = data.durationHours || 3;
        
        // ✅ Reset
        this.monsters = [];
        this.defenders = [];
        this.killedEnemies = 0;
        this.escapedEnemies = 0;
        this.baseDamageTaken = 0;
        this.enemiesSpawned = 0;
        this.isComplete = false;
        this.turrets = [];
    }
    
    create(): void {
        // ✅ Background
        this.cameras.main.setBackgroundColor('#0a0a15');
        
        // ✅ Get Raid Info
        const state = GameState;
        const raid = state.getRaids().find(r => r.id === this.raidId);
        
        if (!raid) {
            console.error('❌ Raid not found!');
            this.scene.stop();
            return;
        }
        
        this.totalEnemies = raid.enemies + raid.eliteCount + (raid.hasBoss ? 1 : 0);
        this.spawnInterval = 3000 / Math.max(1, state.getTime().timeScale);
        
        // ✅ Get Defenders (Crew in base)
        this.defenders = state.getCrew().filter(c => 
            c.state === 'idle' && c.isAlive()
        );
        
        console.log(`⚔️ Defense Scene Started! ${this.totalEnemies} enemies, ${this.defenders.length} defenders`);
        
        // ✅ Setup UI
        this.setupUI();
        
        // ✅ Draw Base
        this.drawBase();
        
        // ✅ Draw Turrets
        this.drawTurrets();
        
        // ✅ Draw Defenders
        this.drawDefenders();
        
        // ✅ Start Spawning
        this.startSpawning();
        
        // ✅ Start Defense Timer (เวลาเกม)
        this.startDefenseTimer();
        
        // ✅ Listen Events
        this.setupEventListeners();
    }
    
    // ============================================================
    // UI
    // ============================================================
    private setupUI(): void {
        const centerX = GAME_CONFIG.WIDTH / 2;
        
        // ✅ Top Bar
        const topBg = this.add.graphics();
        topBg.fillStyle(0x1a1a2e, 0.9);
        topBg.fillRect(0, 0, GAME_CONFIG.WIDTH, 60);
        topBg.lineStyle(2, 0xff6b6b);
        topBg.strokeRect(0, 0, GAME_CONFIG.WIDTH, 60);
        
        // ✅ Day
        this.dayText = this.add.text(20, 15, `Day ${GameState.getTime().gameDay}`, {
            fontSize: '24px',
            color: '#ffffff',
            fontFamily: 'monospace',
        });
        
        // ✅ Time
        this.timeText = this.add.text(180, 15, `🌙 ${this.getTimeString()}`, {
            fontSize: '24px',
            color: '#6c5ce7',
            fontFamily: 'monospace',
        });
        
        // ✅ Enemy Count
        this.enemyText = this.add.text(380, 15, `👾 0/${this.totalEnemies}`, {
            fontSize: '24px',
            color: '#ff6b6b',
            fontFamily: 'monospace',
        });
        
        // ✅ Engine Condition
        const engine = GameState.getEngine();
        this.engineText = this.add.text(620, 15, `🔧 ${Math.floor(engine.condition)}%`, {
            fontSize: '20px',
            color: engine.condition < 20 ? '#ff6b6b' : '#ffffff',
            fontFamily: 'monospace',
        });
        
        // ✅ Title
        this.add.text(GAME_CONFIG.WIDTH - 300, 15, '⚔️ RAID!', {
            fontSize: '28px',
            color: '#ff6b6b',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        });
        
        // ✅ Timer Bar
        this.createTimerBar();
        
        // ✅ Result Text (hidden)
        this.resultText = this.add.text(
            GAME_CONFIG.WIDTH / 2,
            GAME_CONFIG.HEIGHT - 80,
            '',
            {
                fontSize: '28px',
                color: '#ffffff',
                fontFamily: 'monospace',
                backgroundColor: '#1a1a2e',
                padding: { x: 20, y: 10 },
            }
        ).setOrigin(0.5);
        this.resultText.setVisible(false);
    }
    
    private createTimerBar(): void {
        const barX = GAME_CONFIG.WIDTH / 2 - 200;
        const barY = 70;
        const barWidth = 400;
        const barHeight = 20;
        
        // ✅ Background
        const bg = this.add.graphics();
        bg.fillStyle(0x2d3436, 0.8);
        bg.fillRoundedRect(barX, barY, barWidth, barHeight, 5);
        bg.lineStyle(2, 0x4a4a5a);
        bg.strokeRoundedRect(barX, barY, barWidth, barHeight, 5);
        
        // ✅ Fill
        const fill = this.add.graphics();
        fill.fillStyle(0xff6b6b, 0.8);
        fill.fillRoundedRect(barX, barY, barWidth, barHeight, 5);
        
        // ✅ Timer Text
        const timerText = this.add.text(
            GAME_CONFIG.WIDTH / 2,
            barY + 10,
            `${this.remainingHours.toFixed(1)} hrs remaining`,
            {
                fontSize: '14px',
                color: '#ffffff',
                fontFamily: 'monospace',
            }
        ).setOrigin(0.5);
        
        // ✅ Store
        (this as any).timerFill = fill;
        (this as any).timerBarX = barX;
        (this as any).timerBarY = barY;
        (this as any).timerBarWidth = barWidth;
        (this as any).timerBarHeight = barHeight;
        (this as any).timerText = timerText;
    }
    
    // ============================================================
    // DRAW BASE
    // ============================================================
    private drawBase(): void {
        const graphics = this.add.graphics();
        
        // ✅ Base Circle
        graphics.fillStyle(0x00b894, 0.9);
        graphics.fillCircle(this.BASE_X, this.BASE_Y, this.BASE_RADIUS);
        graphics.lineStyle(4, 0x00d2a0);
        graphics.strokeCircle(this.BASE_X, this.BASE_Y, this.BASE_RADIUS);
        
        // ✅ Base Interior (แสดง turret)
        graphics.fillStyle(0x1a1a2e, 0.8);
        graphics.fillCircle(this.BASE_X, this.BASE_Y, this.BASE_RADIUS - 20);
        
        // ✅ Base Label
        this.add.text(this.BASE_X - 40, this.BASE_Y - 15, '🏠 BASE', {
            fontSize: '24px',
            color: '#ffffff',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        });
        
        // ✅ Engine Condition Ring
        const engine = GameState.getEngine();
        const enginePercent = engine.condition / 100;
        
        const engineRing = this.add.graphics();
        engineRing.lineStyle(6, 0x2d3436, 0.8);
        engineRing.strokeCircle(this.BASE_X, this.BASE_Y, this.BASE_RADIUS + 15);
        
        engineRing.lineStyle(6, engine.condition < 20 ? 0xff6b6b : 0xf9ca24);
        engineRing.beginPath();
        engineRing.arc(
            this.BASE_X,
            this.BASE_Y,
            this.BASE_RADIUS + 15,
            -Math.PI / 2,
            -Math.PI / 2 + Math.PI * 2 * enginePercent,
            false
        );
        engineRing.strokePath();
    }
    
    // ============================================================
    // DRAW TURRETS
    // ============================================================
    private drawTurrets(): void {
        // ✅ ดึง Turret จาก BaseSystem (ยังไม่มี)
        // TODO: implement turrets
        
        // ✅ Placeholder: วาด turrets 4 มุม
        const turretPositions = [
            { x: this.BASE_X - 60, y: this.BASE_Y - 60 },
            { x: this.BASE_X + 60, y: this.BASE_Y - 60 },
            { x: this.BASE_X - 60, y: this.BASE_Y + 60 },
            { x: this.BASE_X + 60, y: this.BASE_Y + 60 },
        ];
        
        turretPositions.forEach((pos, i) => {
            const turret = this.add.circle(pos.x, pos.y, 15, 0x4ecdc4, 0.8);
            turret.setStrokeStyle(2, 0xffffff);
            this.turrets.push(turret);
            
            // ✅ Turret Label
            this.add.text(pos.x - 10, pos.y - 8, '🔫', {
                fontSize: '16px',
            });
        });
    }
    
    // ============================================================
    // DRAW DEFENDERS
    // ============================================================
    private drawDefenders(): void {
        const positions = [
            { x: this.BASE_X - 40, y: this.BASE_Y - 40 },
            { x: this.BASE_X + 40, y: this.BASE_Y - 40 },
            { x: this.BASE_X - 40, y: this.BASE_Y + 40 },
            { x: this.BASE_X + 40, y: this.BASE_Y + 40 },
            { x: this.BASE_X, y: this.BASE_Y - 60 },
            { x: this.BASE_X, y: this.BASE_Y + 60 },
        ];
        
        this.defenders.forEach((crew, i) => {
            const pos = positions[i % positions.length];
            
            const sprite = this.add.text(pos.x - 10, pos.y - 10, '🧑‍🤝‍🧑', {
                fontSize: '20px',
            });
            
            // ✅ Crew Name
            this.add.text(pos.x - 20, pos.y + 15, crew.name, {
                fontSize: '10px',
                color: '#4ecdc4',
                fontFamily: 'monospace',
            });
        });
    }
    
    // ============================================================
    // SPAWNING
    // ============================================================
    private startSpawning(): void {
        this.spawnTimer = this.time.addEvent({
            delay: this.spawnInterval,
            callback: this.spawnMonster,
            callbackScope: this,
            loop: true,
        });
        
        // ✅ Spawn ทันที 2 ตัว
        this.spawnMonster();
        this.time.delayedCall(500, () => this.spawnMonster());
    }
    
    private spawnMonster(): void {
        if (this.isComplete) return;
        if (this.enemiesSpawned >= this.totalEnemies) return;
        
        const state = GameState;
        const raid = state.getRaids().find(r => r.id === this.raidId);
        if (!raid) return;
        
        // ✅ สุ่มฝั่ง (ซ้าย/ขวา)
        const side = Math.random() < 0.5 ? 'left' : 'right';
        
        // ✅ ตำแหน่งเกิด
        const x = side === 'left' ? -50 : GAME_CONFIG.WIDTH + 50;
        const y = this.BASE_Y + (Math.random() - 0.5) * 200;
        
        // ✅ สุ่ม type
        const types: MonsterType[] = ['A', 'B', 'C', 'D', 'E'];
        const typeIndex = Math.min(
            Math.floor(Math.random() * (1 + state.getTime().gameDay / 3)),
            types.length - 1
        );
        const monsterType = types[typeIndex];
        
        // ✅ สร้าง Monster
        const monster = Monster.create(monsterType, x, y);
        
        // ✅ วาด Sprite
        monster.sprite = this.add.circle(x, y, monster.getSize(), monster.getColor(), 0.9);
        monster.sprite.setStrokeStyle(2, 0xffffff);
        
        // ✅ Icon
        monster.icon = this.add.text(x - 10, y - 10, monster.getIcon(), {
            fontSize: '20px',
        });
        
        // ✅ HP Bar
        monster.hpBarBg = this.add.graphics();
        monster.hpBarBg.fillStyle(0x333333);
        monster.hpBarBg.fillRect(x - 20, y - 30, 40, 4);
        
        monster.hpBar = this.add.graphics();
        monster.hpBar.fillStyle(0x4ecdc4);
        monster.hpBar.fillRect(x - 20, y - 30, 40, 4);
        
        this.monsters.push(monster);
        this.enemiesSpawned++;
        
        this.updateEnemyText();
    }
    
    // ============================================================
    // DEFENSE TIMER
    // ============================================================
    private startDefenseTimer(): void {
        // ✅ Timer ที่นับถอยหลังทุก 1 วินาทีจริง
        // 1 นาทีจริง = 1 ชม.เกม
        this.defenseTimer = this.time.addEvent({
            delay: 1000,
            callback: this.tickDefenseTime,
            callbackScope: this,
            loop: true,
        });
    }
    
    private tickDefenseTime(): void {
        if (this.isComplete) return;
        
        const timeScale = GameState.getTime().timeScale;
        this.remainingHours -= (1 / 60) * timeScale;  // 1 วินาที = 1/60 ชม.
        
        // ✅ อัพเดท UI
        const timerText = (this as any).timerText as Phaser.GameObjects.Text;
        if (timerText) {
            timerText.setText(`${Math.max(0, this.remainingHours).toFixed(1)} hrs remaining`);
        }
        
        // ✅ อัพเดท Bar
        const raid = GameState.getRaids().find(r => r.id === this.raidId);
        if (raid) {
            const percent = Math.max(0, this.remainingHours / raid.durationHours);
            const fill = (this as any).timerFill as Phaser.GameObjects.Graphics;
            const barX = (this as any).timerBarX;
            const barY = (this as any).timerBarY;
            const barWidth = (this as any).timerBarWidth;
            const barHeight = (this as any).timerBarHeight;
            
            fill.clear();
            fill.fillStyle(0xff6b6b, 0.8);
            fill.fillRoundedRect(barX, barY, barWidth * percent, barHeight, 5);
        }
        
        // ✅ เช็คจบ
        if (this.remainingHours <= 0) {
            this.endDefense(true);  // หมดเวลา = รอด
        }
    }
    
    // ============================================================
    // UPDATE
    // ============================================================
    update(time: number, delta: number): void {
        if (this.isComplete) return;
        
        const deltaSeconds = delta / 1000;
        const timeScale = GameState.getTime().timeScale;
        
        // ✅ ขยับ Monsters
        this.monsters.forEach(monster => {
            if (monster.isDead()) return;
            
            // ✅ เคลื่อนที่เข้าหาฐาน
            const dx = this.BASE_X - monster.x;
            const dy = this.BASE_Y - monster.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            // ✅ Speed (ช้ากว่าปกติ)
            const speed = 50 + monster.rank * 10;  // px/sec
            const moveDistance = speed * deltaSeconds;
            
            if (distance < this.BASE_RADIUS) {
                // ✅ ถึงฐาน → ตี
                this.onMonsterReachBase(monster);
                return;
            }
            
            monster.x += (dx / distance) * moveDistance;
            monster.y += (dy / distance) * moveDistance;
            
            // ✅ อัพเดท Visual
            if (monster.sprite) {
                monster.sprite.x = monster.x;
                monster.sprite.y = monster.y;
            }
            if (monster.icon) {
                monster.icon.x = monster.x - 10;
                monster.icon.y = monster.y - 10;
            }
            if (monster.hpBarBg) {
                monster.hpBarBg.clear();
                monster.hpBarBg.fillStyle(0x333333);
                monster.hpBarBg.fillRect(monster.x - 20, monster.y - 30, 40, 4);
            }
            if (monster.hpBar) {
                const hpPercent = monster.hp / monster.maxHp;
                monster.hpBar.clear();
                monster.hpBar.fillStyle(hpPercent > 0.5 ? 0x4ecdc4 : 0xff6b6b);
                monster.hpBar.fillRect(monster.x - 20, monster.y - 30, 40 * hpPercent, 4);
            }
        });
        
        // ✅ Tower Attack
        this.turrets.forEach((turret, i) => {
            this.turretAttack(turret, deltaSeconds);
        });
        
        // ✅ ลบ Monster ที่ตายแล้ว
        this.monsters = this.monsters.filter(m => {
            if (m.isDead()) {
                if (m.sprite) m.sprite.destroy();
                if (m.icon) m.icon.destroy();
                if (m.hpBar) m.hpBar.destroy();
                if (m.hpBarBg) m.hpBarBg.destroy();
                return false;
            }
            return true;
        });
        
        // ✅ เช็คจบ
        if (this.enemiesSpawned >= this.totalEnemies && this.monsters.length === 0) {
            this.endDefense(true);
        }
    }
    
    private onMonsterReachBase(monster: Monster): void {
        // ✅ Damage Engine
        const state = GameState;
        const engine = state.getEngine();
        
        // ✅ Damage Engine
        const damage = Math.min(engine.condition, 5);  // ตี 5% condition
        engine.condition = Math.max(0, engine.condition - damage);
        this.baseDamageTaken += damage;
        
        // ✅ ลบ Monster
        if (monster.sprite) monster.sprite.destroy();
        if (monster.icon) monster.icon.destroy();
        if (monster.hpBar) monster.hpBar.destroy();
        if (monster.hpBarBg) monster.hpBarBg.destroy();
        
        monster.hp = 0;
        this.escapedEnemies++;
        
        console.log(`💥 Monster reached base! Engine -${damage}%`);
        
        // ✅ เช็ค Engine Failure
        if (engine.condition <= 0) {
            state.triggerGameOver('engine_failure', false);
        }
        
        this.updateEngineText();
    }
    
    private turretAttack(turret: Phaser.GameObjects.Arc, deltaSeconds: number): void {
        // ✅ หา Monster ใกล้สุด
        let closestMonster: Monster | null = null;
        let closestDist: number = Infinity;
        
        for (const monster of this.monsters) {
            if (monster.isDead()) continue;
            
            const dx = monster.x - turret.x;
            const dy = monster.y - turret.y;
            const dist = Math.sqrt(dx * dx + dy * dy);
            
            if (dist < 300 && dist < closestDist) {
                closestMonster = monster;
                closestDist = dist;
            }
        }
        
        if (closestMonster) {
            // ✅ ยิงทุกๆ 1 วินาที
            if (Math.random() < deltaSeconds * 1.5) {
                const damage = 100 + Math.random() * 100;
                closestMonster.takeDamage(damage);
                
                // ✅ Effect
                this.showAttackEffect(turret.x, turret.y, closestMonster.x, closestMonster.y);
                
                if (closestMonster.isDead()) {
                    this.killedEnemies++;
                    this.updateEnemyText();
                }
            }
        }
    }
    
    private showAttackEffect(fromX: number, fromY: number, toX: number, toY: number): void {
        const graphics = this.add.graphics();
        graphics.lineStyle(2, 0x4ecdc4, 0.8);
        graphics.beginPath();
        graphics.moveTo(fromX, fromY);
        graphics.lineTo(toX, toY);
        graphics.strokePath();
        
        this.tweens.add({
            targets: graphics,
            alpha: 0,
            duration: 200,
            onComplete: () => graphics.destroy(),
        });
    }
    
    // ============================================================
    // END DEFENSE
    // ============================================================
    private endDefense(survived: boolean): void {
        if (this.isComplete) return;
        this.isComplete = true;
        
        // ✅ หยุด Timer
        if (this.spawnTimer) {
            this.spawnTimer.remove();
            this.spawnTimer = null;
        }
        if (this.defenseTimer) {
            this.defenseTimer.remove();
            this.defenseTimer = null;
        }
        
        // ✅ คำนวณผล
        const won = survived;
        const damageTaken = this.baseDamageTaken;
        
        // ✅ Complete Raid
        Systems.raid.completeRaid(this.raidId, won, damageTaken);
        
        // ✅ แสดง Result
        this.resultText.setText(
            won 
                ? `🎉 RAID SURVIVED! +${Math.floor(100)} credits`
                : `💀 RAID FAILED!`
        );
        this.resultText.setColor(won ? '#00b894' : '#ff6b6b');
        this.resultText.setVisible(true);
        
        // ✅ กลับ WorldScene หลัง 3 วินาที
        this.time.delayedCall(3000, () => {
            // ✅ Stop DefenseScene
            this.scene.stop();
            
            // ✅ Emit event
            EventBus.emit('defense:complete', { won, damageTaken });
        });
    }
    
    // ============================================================
    // EVENT LISTENERS
    // ============================================================
    private setupEventListeners(): void {
        EventBus.on(EVENTS.ENGINE_CONDITION_CHANGED, this.updateEngineText, this);
    }
    
    // ============================================================
    // UI UPDATE
    // ============================================================
    private updateEnemyText(): void {
        this.enemyText.setText(`👾 ${this.killedEnemies}/${this.totalEnemies}`);
    }
    
    private updateEngineText(): void {
        const engine = GameState.getEngine();
        this.engineText.setText(`🔧 ${Math.floor(engine.condition)}%`);
        this.engineText.setColor(engine.condition < 20 ? '#ff6b6b' : '#ffffff');
    }
    
    private getTimeString(): string {
        const time = GameState.getTime();
        const hh = String(time.gameHour).padStart(2, '0');
        const mm = String(time.gameMinute).padStart(2, '0');
        return `${hh}:${mm}`;
    }
}