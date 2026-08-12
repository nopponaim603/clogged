// src/scenes/NightScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { Crew } from '../entities/Crew';
import { CrewManager } from '../systems/CrewManager';
import { ResourceManager } from '../systems/ResourceManager';
import { MapGenerator } from '../systems/MapGenerator';
import { TimeSystem } from '../systems/TimeSystem';
import { NotificationSystem } from '../ui/NotificationSystem';
import { SpeedControl } from '../ui/SpeedControl';

interface DefenseTower {
    x: number;
    y: number;
    type: 'gun' | 'cannon' | 'laser';
    level: number;
    damage: number;
    range: number;
    cooldown: number;
    cooldownTimer: number;
    sprite: Phaser.GameObjects.Arc | null;
    target: Monster | null;
}

interface Monster {
    id: string;
    x: number;
    y: number;
    hp: number;
    maxHp: number;
    speed: number;
    damage: number;
    reward: number;
    type: 'normal' | 'fast' | 'tank' | 'boss';
    sprite: Phaser.GameObjects.Arc | null;
    icon: Phaser.GameObjects.Text | null;
    direction: { x: number; y: number };
    progress: number;
}

export class NightScene extends Scene {
    // === Systems ===
    private crewManager!: CrewManager;
    private resourceManager!: ResourceManager;
    private mapGenerator!: MapGenerator;
    private timeSystem!: TimeSystem;
    private notificationSystem!: NotificationSystem;
    private speedControl!: SpeedControl;
    private isSkipping: boolean = false;
    
    // === Defense ===
    private towers: DefenseTower[] = [];
    private monsters: Monster[] = [];
    private selectedTowerType: string = 'gun';
    private isPlacingTower: boolean = false;
    private towerPreview: Phaser.GameObjects.Arc | null = null;
    
    // === UI ===
    private dayText!: Phaser.GameObjects.Text;
    private enemyCounterText!: Phaser.GameObjects.Text;
    private baseHPText!: Phaser.GameObjects.Text;
    private resourceTexts: Map<string, Phaser.GameObjects.Text> = new Map();
    private towerButtons: Phaser.GameObjects.Text[] = [];
    private buildModeText!: Phaser.GameObjects.Text;
    private popupElements: Phaser.GameObjects.GameObject[] = [];
    
    // === State ===
    private day: number = 1;
    private baseHP: number = 100;
    private maxBaseHP: number = 100;
    private gameOver: boolean = false;
    private isNightComplete: boolean = false;
    private isPopupShowing: boolean = false;
    
    // === Enemy Tracking ===
    private totalEnemies: number = 0;
    private completeEnemies: number = 0;
    private enemiesSpawned: number = 0;
    private spawnTimer: Phaser.Time.TimerEvent | null = null;
    
    // === Enemy Spawn Config ===
    private spawnPoints: { x: number; y: number; direction: { x: number; y: number; label: string } }[] = [];
    private spawnInterval: number = 2000;
    private enemiesPerWave: number = 5;
    private currentWave: number = 1;
    private totalWaves: number = 3;

    private travelState: any = null;

    constructor() {
        super('NightScene');
    }

    init(data: any): void {
        this.day = data.day || 1;
        this.crewManager = data.crewManager;
        this.resourceManager = data.resourceManager;
        this.mapGenerator = data.mapGenerator;
        this.timeSystem = data.timeSystem;
        this.baseHP = this.resourceManager?.baseHP || 100;
        this.maxBaseHP = this.resourceManager?.maxBaseHP || 100;
        this.travelState = data.travelState || null;
        
        // ✅ คำนวณจำนวนศัตรูทั้งหมดในคืนนี้
        this.totalEnemies = 10 + this.day * 3;
        this.enemiesPerWave = Math.min(5 + Math.floor(this.day / 2), 15);
        this.totalWaves = Math.ceil(this.totalEnemies / this.enemiesPerWave);
        this.spawnInterval = Math.max(1000, 3000 - this.day * 50);
        this.completeEnemies = 0;
        this.isNightComplete = false;
        this.isPopupShowing = false;
        
        // ✅ สร้าง spawn points (4 ทิศทาง)
        this.generateSpawnPoints();
        
        // ✅ สร้าง NotificationSystem
        this.notificationSystem = new NotificationSystem(this);
    }

    create(): void {
        // ✅ Setup UI (ต้องมาก่อน Speed Control)
        this.setupUI();
        
        // ✅ เพิ่ม Speed Control (มุมขวาบน)
        this.speedControl = new SpeedControl(
            this,
            GAME_CONFIG.WIDTH - 270,
            60,
            (speed: number) => {
                // ✅ ปรับความเร็วการเคลื่อนที่ของมอนสเตอร์
                this.setMonsterSpeed(speed);
                this.notificationSystem?.showInfo(`Speed: ${speed}x`, 1000);
            },
            () => {
                // ✅ Skip Night
                this.skipNight();
            }
        );
        
        // ✅ Setup towers
        this.setupTowerSystem();
        
        // ✅ Start spawning enemies
        this.startNight();
    }

    private generateSpawnPoints(): void {
        const padding = 30;
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        
        // ✅ 4 ทิศทาง: บน, ล่าง, ซ้าย, ขวา
        const positions = [
            { x: centerX, y: padding, label: '⬆️' },
            { x: centerX, y: GAME_CONFIG.HEIGHT - padding, label: '⬇️' },
            { x: padding, y: centerY, label: '⬅️' },
            { x: GAME_CONFIG.WIDTH - padding, y: centerY, label: '➡️' }
        ];
        
        const directions = [
            { x: 0, y: 1, label: '⬆️ Top' },
            { x: 0, y: -1, label: '⬇️ Bottom' },
            { x: 1, y: 0, label: '⬅️ Left' },
            { x: -1, y: 0, label: '➡️ Right' }
        ];
        
        this.spawnPoints = positions.map((pos, index) => ({
            x: pos.x,
            y: pos.y,
            direction: directions[index]
        }));
        
        // ✅ แสดงจุดเกิด
        this.spawnPoints.forEach((spawn, index) => {
            const color = [0x4ecdc4, 0xf9ca24, 0xff6b6b, 0xa29bfe][index];
            const arc = this.add.arc(spawn.x, spawn.y, 12, 0, 360, false, color, 0.6);
            arc.setStrokeStyle(2, 0xffffff);
            
            this.add.text(spawn.x - 8, spawn.y - 25, spawn.direction.label, {
                fontSize: '10px', color: '#ffffff', fontFamily: 'monospace'
            });
        });
        
        // ✅ วาด Base ตรงกลาง
        const baseArc = this.add.arc(centerX, centerY, 40, 0, 360, false, 0x00b894, 0.8);
        baseArc.setStrokeStyle(3, 0x00d2a0);
        this.add.text(centerX - 25, centerY - 10, '🏠 BASE', {
            fontSize: '16px', color: '#ffffff', fontFamily: 'monospace'
        });
        
        // ✅ วาดรัศมี Base
        const rangeCircle = this.add.circle(centerX, centerY, 120, 0x00b894, 0.05);
        rangeCircle.setStrokeStyle(1, 0x00b894, 0.3);
    }

    private setupUI(): void {
        // ✅ Background
        this.cameras.main.setBackgroundColor('#0a0a15');
        
        // ✅ Top bar
        this.dayText = this.add.text(20, 20, 
            `🌙 Night ${this.day}`,
            { fontSize: '24px', color: '#ffffff', fontFamily: 'monospace' }
        );

        this.enemyCounterText = this.add.text(200, 20,
            `👾 Enemy: ${this.completeEnemies}/${this.totalEnemies}`,
            { fontSize: '20px', color: '#f9ca24', fontFamily: 'monospace' }
        );

        this.baseHPText = this.add.text(500, 20,
            `🏠 HP: ${Math.floor(this.baseHP)}/${this.maxBaseHP}`,
            { fontSize: '20px', color: '#ff6b6b', fontFamily: 'monospace' }
        );

        // ✅ Resource display
        this.createResourceUI();

        // ✅ Build mode indicator
        this.buildModeText = this.add.text(20, 680,
            '🔧 Select a tower from the bottom panel to build',
            { fontSize: '16px', color: '#b2bec3', fontFamily: 'monospace' }
        );

        // ✅ Tower selection panel
        this.createTowerSelectionUI();
    }

    private createResourceUI(): void {
        const x = GAME_CONFIG.WIDTH - 200;
        let y = 60;
        const resourceTypes = ['wood', 'stone', 'iron', 'food'];
        resourceTypes.forEach((type) => {
            const amount = this.resourceManager?.getResource(type) || 0;
            const text = this.add.text(x, y, 
                `${this.getIcon(type)} ${Math.floor(amount)}`,
                { fontSize: '16px', color: '#ffffff', fontFamily: 'monospace' }
            );
            this.resourceTexts.set(type, text);
            y += 30;
        });
    }

    private createTowerSelectionUI(): void {
        const y = GAME_CONFIG.HEIGHT - 60;
        const towers = [
            { type: 'gun', label: '🔫 Gun', cost: 50 },
            { type: 'cannon', label: '💥 Cannon', cost: 100 },
            { type: 'laser', label: '⚡ Laser', cost: 150 }
        ];

        towers.forEach((tower, index) => {
            const x = 100 + index * 200;
            const btn = this.add.text(x, y, 
                `${tower.label} (${tower.cost})`,
                { fontSize: '16px', color: '#4ecdc4', fontFamily: 'monospace', backgroundColor: '#2d3436', padding: { x: 10, y: 5 } }
            );
            btn.setInteractive();
            btn.on('pointerdown', () => this.selectTowerType(tower.type, tower.cost));
            btn.on('pointerover', () => btn.setStyle({ color: '#ffffff' }));
            btn.on('pointerout', () => btn.setStyle({ color: '#4ecdc4' }));
            this.towerButtons.push(btn);
        });
    }

    private getIcon(type: string): string {
        const icons: { [key: string]: string } = {
            wood: '🪵', stone: '🪨', iron: '⛏️', food: '🍖'
        };
        return icons[type] || '📦';
    }

    private selectTowerType(type: string, cost: number): void {
        const resources = this.resourceManager?.resources;
        if (!resources) return;
        
        if (resources.wood < cost) {
            this.notificationSystem?.showError(`Not enough resources! Need ${cost} wood`, 2000);
            return;
        }
        
        this.selectedTowerType = type;
        this.isPlacingTower = true;
        this.buildModeText.setText(`🔧 Placing ${type} tower (${cost} wood) - Click on map to place`);
        this.buildModeText.setColor('#f9ca24');
        
        this.towerPreview = this.add.arc(0, 0, 20, 0, 360, false, 0x4ecdc4, 0.3);
        this.towerPreview.setStrokeStyle(2, 0x4ecdc4);
        this.towerPreview.setVisible(false);
        
        this.input.on('pointermove', (pointer: Phaser.Input.Pointer) => {
            if (this.towerPreview && this.isPlacingTower) {
                this.towerPreview.x = pointer.x;
                this.towerPreview.y = pointer.y;
                this.towerPreview.setVisible(true);
            }
        });
        
        this.input.on('pointerdown', (pointer: Phaser.Input.Pointer) => {
            if (this.isPlacingTower && this.towerPreview) {
                this.placeTower(pointer.x, pointer.y, type, cost);
            }
        });
    }

    private placeTower(x: number, y: number, type: string, cost: number): void {
        const resources = this.resourceManager?.resources;
        if (!resources) return;
        
        if (resources.wood < cost) {
            this.notificationSystem?.showError(`Not enough resources!`, 2000);
            return;
        }
        
        resources.wood -= cost;
        this.updateResourceUI();
        
        const tower: DefenseTower = {
            x, y,
            type: type as 'gun' | 'cannon' | 'laser',
            level: 1,
            damage: type === 'gun' ? 10 : type === 'cannon' ? 25 : 15,
            range: type === 'gun' ? 150 : type === 'cannon' ? 200 : 180,
            cooldown: type === 'gun' ? 500 : type === 'cannon' ? 1000 : 800,
            cooldownTimer: 0,
            sprite: null,
            target: null
        };
        
        const color = type === 'gun' ? 0x4ecdc4 : type === 'cannon' ? 0xf9ca24 : 0xff6b6b;
        const sprite = this.add.arc(x, y, 15, 0, 360, false, color, 0.8);
        sprite.setStrokeStyle(2, 0xffffff);
        tower.sprite = sprite;
        
        this.add.text(x - 10, y - 30, type.toUpperCase(), {
            fontSize: '10px', color: '#ffffff', fontFamily: 'monospace'
        });
        
        this.towers.push(tower);
        this.isPlacingTower = false;
        this.buildModeText.setText('🔧 Select a tower to build');
        this.buildModeText.setColor('#b2bec3');
        
        if (this.towerPreview) {
            this.towerPreview.setVisible(false);
        }
        
        this.notificationSystem?.showSuccess(`${type} tower placed!`, 1500);
    }

    private setupTowerSystem(): void {
        // Tower system will be updated in update loop
    }

    // ✅ ตั้งค่าความเร็วให้มอนสเตอร์
    private setMonsterSpeed(speed: number): void {
        // ✅ เก็บ speed ไว้ใช้ใน updateMonsters
        (this as any).monsterSpeedMultiplier = speed;
    }

    // ✅ Skip Night
    private skipNight(): void {
        if (this.isSkipping || this.isNightComplete) return;
        this.isSkipping = true;
        
        this.notificationSystem?.showWarning('⏭️ Skipping night...', 1500);
        
        // ✅ ทำให้มอนสเตอร์ทั้งหมดตาย
        for (const monster of this.monsters) {
            monster.hp = 0;
            if (monster.sprite) monster.sprite.destroy();
            if (monster.icon) monster.icon.destroy();
            if ((monster as any).hpBarBg) (monster as any).hpBarBg.destroy();
            if ((monster as any).hpBar) (monster as any).hpBar.destroy();
            this.completeEnemies++;
        }
        this.monsters = [];
        
        // ✅ กระโดดไปจบคืน
        this.completeEnemies = this.totalEnemies;
        this.updateEnemyCounter();
        this.isNightComplete = true;
        
        if (this.spawnTimer) {
            this.spawnTimer.remove();
            this.spawnTimer = null;
        }
        
        this.time.delayedCall(500, () => {
            this.isSkipping = false;
            this.showNightComplete();
        });
    }

    // ✅ แก้ไข spawnInterval ใน startNight
    private startNight(): void {
        this.notificationSystem?.showWarning(`🌙 Night ${this.day} begins! ${this.totalEnemies} enemies approaching!`, 3000);
        this.enemiesSpawned = 0;
        this.completeEnemies = 0;
        this.isNightComplete = false;
        this.isPopupShowing = false;
        this.isSkipping = false;
        
        // ✅ spawnInterval ปรับตาม speed
        const currentSpeed = this.speedControl?.getSpeed() || 1;
        const interval = this.spawnInterval / currentSpeed;
        
        this.spawnTimer = this.time.addEvent({
            delay: interval,
            callback: this.spawnEnemy,
            callbackScope: this,
            loop: true
        });
        
        this.spawnEnemy();
    }

    private spawnEnemy(): void {
        if (this.isNightComplete || this.gameOver || this.isPopupShowing || this.isSkipping) {
            if (this.spawnTimer) {
                this.spawnTimer.remove();
                this.spawnTimer = null;
            }
            return;
        }
        
        if (this.enemiesSpawned >= this.totalEnemies) {
            if (this.spawnTimer) {
                this.spawnTimer.remove();
                this.spawnTimer = null;
            }
            this.checkNightComplete();
            return;
        }
        
        // ✅ เลือก spawn point แบบสุ่ม
        const spawnPoint = this.spawnPoints[Math.floor(Math.random() * this.spawnPoints.length)];
        
        // ✅ สุ่มประเภทมอนสเตอร์
        const difficulty = 1 + Math.floor(this.day / 3);
        const types: ('normal' | 'fast' | 'tank' | 'boss')[] = ['normal', 'normal', 'fast', 'tank'];
        if (this.day % 3 === 0 && this.enemiesSpawned % 10 === 0) types.push('boss');
        
        const type = types[Math.floor(Math.random() * types.length)];
        const hp = type === 'normal' ? 30 : type === 'fast' ? 20 : type === 'tank' ? 80 : 150;
        const speed = type === 'normal' ? 0.8 : type === 'fast' ? 1.6 : type === 'tank' ? 0.4 : 0.6;
        const damage = type === 'normal' ? 5 : type === 'fast' ? 3 : type === 'tank' ? 10 : 20;
        
        const monster: Monster = {
            id: `monster_${Date.now()}_${this.enemiesSpawned}`,
            x: spawnPoint.x,
            y: spawnPoint.y,
            hp: hp * difficulty,
            maxHp: hp * difficulty,
            speed: speed,
            damage: damage,
            reward: 5 + difficulty * 2,
            type: type,
            sprite: null,
            icon: null,
            direction: spawnPoint.direction,
            progress: 0
        };
        
        // ✅ Create sprite
        const color = type === 'normal' ? 0xe74c3c : type === 'fast' ? 0xf9ca24 : type === 'tank' ? 0x8e44ad : 0xff0000;
        const size = type === 'boss' ? 25 : 15;
        const sprite = this.add.arc(monster.x, monster.y, size, 0, 360, false, color, 0.9);
        sprite.setStrokeStyle(2, 0xffffff);
        monster.sprite = sprite;
        
        const icon = type === 'boss' ? '👑' : '👹';
        const iconText = this.add.text(monster.x - 10, monster.y - 12, icon, { fontSize: '20px' });
        monster.icon = iconText;
        
        // ✅ HP bar
        const hpBarBg = this.add.graphics();
        hpBarBg.fillStyle(0x333333);
        hpBarBg.fillRect(monster.x - 20, monster.y - 28, 40, 4);
        
        const hpBar = this.add.graphics();
        hpBar.fillStyle(0x4ecdc4);
        hpBar.fillRect(monster.x - 20, monster.y - 28, 40, 4);
        
        (monster as any).hpBarBg = hpBarBg;
        (monster as any).hpBar = hpBar;
        
        this.monsters.push(monster);
        this.enemiesSpawned++;
        
        this.notificationSystem?.showInfo(`👾 ${type} enemy spawned! (${this.enemiesSpawned}/${this.totalEnemies})`, 1000);
    }

    private updateMonsters(): void {
        const speedMultiplier = (this as any).monsterSpeedMultiplier || 1;
        const baseSpeed = 0.8 * speedMultiplier;
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        const baseRadius = 40;
        
        for (const monster of this.monsters) {
            if (monster.hp <= 0) continue;
            
            const dx = centerX - monster.x;
            const dy = centerY - monster.y;
            const distance = Math.sqrt(dx * dx + dy * dy);
            
            if (distance < baseRadius) {
                // ✅ ถึงฐาน! → completeEnemy++
                this.baseHP -= monster.damage;
                this.baseHPText.setText(`🏠 HP: ${Math.floor(this.baseHP)}/${this.maxBaseHP}`);
                this.completeEnemies++;
                monster.hp = 0;
                
                // ✅ Remove monster
                if (monster.sprite) monster.sprite.destroy();
                if (monster.icon) monster.icon.destroy();
                if ((monster as any).hpBarBg) (monster as any).hpBarBg.destroy();
                if ((monster as any).hpBar) (monster as any).hpBar.destroy();
                
                this.notificationSystem?.showError(`💥 Enemy reached base! (${this.completeEnemies}/${this.totalEnemies})`, 1500);
                this.updateEnemyCounter();
                
                // ✅ ตรวจสอบว่าจบคืนหรือยัง
                this.checkNightComplete();
                
                if (this.baseHP <= 0) {
                    this.baseHP = 0;
                    this.showGameOver('💀 Base Destroyed!');
                }
                continue;
            }
            
            const moveSpeed = monster.speed * baseSpeed;
            monster.x += (dx / distance) * moveSpeed;
            monster.y += (dy / distance) * moveSpeed;
            
            // ✅ Update sprite position
            if (monster.sprite) {
                monster.sprite.x = monster.x;
                monster.sprite.y = monster.y;
            }
            if (monster.icon) {
                monster.icon.x = monster.x - 10;
                monster.icon.y = monster.y - 12;
            }
            
            // ✅ Update HP bar
            const hpPercent = monster.hp / monster.maxHp;
            if ((monster as any).hpBarBg) {
                (monster as any).hpBarBg.x = monster.x - 20;
                (monster as any).hpBarBg.y = monster.y - 28;
            }
            if ((monster as any).hpBar) {
                (monster as any).hpBar.x = monster.x - 20;
                (monster as any).hpBar.y = monster.y - 28;
                (monster as any).hpBar.clear();
                (monster as any).hpBar.fillStyle(hpPercent > 0.5 ? 0x4ecdc4 : 0xff6b6b);
                (monster as any).hpBar.fillRect(monster.x - 20, monster.y - 28, 40 * hpPercent, 4);
            }
        }
        
        // ✅ Remove dead monsters
        this.monsters = this.monsters.filter(m => m.hp > 0);
    }

    private updateTowers(): void {
        for (const tower of this.towers) {
            if (tower.cooldownTimer > 0) {
                tower.cooldownTimer -= 16;
                continue;
            }
            
            // ✅ Find target
            let closestMonster: Monster | null = null;
            let closestDistance = Infinity;
            
            for (const monster of this.monsters) {
                if (monster.hp <= 0) continue;
                const dx = monster.x - tower.x;
                const dy = monster.y - tower.y;
                const dist = Math.sqrt(dx * dx + dy * dy);
                if (dist < tower.range && dist < closestDistance) {
                    closestMonster = monster;
                    closestDistance = dist;
                }
            }
            
            if (closestMonster) {
                closestMonster.hp -= tower.damage;
                tower.cooldownTimer = tower.cooldown;
                this.showAttackEffect(tower.x, tower.y, closestMonster.x, closestMonster.y);
                
                if (closestMonster.hp <= 0) {
                    // ✅ ถูกฆ่า! → completeEnemy++
                    const reward = closestMonster.reward || 5;
                    this.resourceManager?.addResource('wood', reward);
                    this.updateResourceUI();
                    this.completeEnemies++;
                    this.updateEnemyCounter();
                    this.notificationSystem?.showInfo(`💰 +${reward} wood! (${this.completeEnemies}/${this.totalEnemies})`, 1000);
                    
                    // ✅ ตรวจสอบว่าจบคืนหรือยัง
                    this.checkNightComplete();
                }
            }
        }
    }

    private updateEnemyCounter(): void {
        this.enemyCounterText.setText(
            `👾 Enemy: ${this.completeEnemies}/${this.totalEnemies}`
        );
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
            onComplete: () => graphics.destroy()
        });
    }

    private updateResourceUI(): void {
        if (!this.resourceManager) return;
        this.resourceTexts.forEach((text, type) => {
            const amount = this.resourceManager?.getResource(type) || 0;
            text.setText(`${this.getIcon(type)} ${Math.floor(amount)}`);
        });
    }

    private checkNightComplete(): void {
        // ✅ ป้องกันการเรียกซ้ำ
        if (this.isNightComplete || this.isPopupShowing) {
            console.log('⚠️ Night already complete or popup showing');
            return;
        }
        
        // ✅ ตรวจสอบว่ามอนสเตอร์ครบทุกตัว และไม่มีมอนสเตอร์เหลือบนแผนที่
        const allSpawned = this.enemiesSpawned >= this.totalEnemies;
        const allProcessed = this.completeEnemies >= this.totalEnemies;
        const noMonstersLeft = this.monsters.length === 0;
        
        // ✅ Debug log
        console.log(`🔍 checkNightComplete: allSpawned=${allSpawned} (${this.enemiesSpawned}/${this.totalEnemies}), allProcessed=${allProcessed} (${this.completeEnemies}/${this.totalEnemies}), noMonstersLeft=${noMonstersLeft} (${this.monsters.length})`);
        
        // ✅ ใช้เงื่อนไขที่แน่นขึ้น: completeEnemies ต้องเท่ากับ totalEnemies
        if (allSpawned && this.completeEnemies === this.totalEnemies && noMonstersLeft) {
            console.log('✅ Night complete! Showing popup...');
            this.isNightComplete = true;
            if (this.spawnTimer) {
                this.spawnTimer.remove();
                this.spawnTimer = null;
            }
            this.showNightComplete();
        } else if (allSpawned && this.completeEnemies === this.totalEnemies && this.monsters.length > 0) {
            console.log(`⏳ Waiting for ${this.monsters.length} monsters to be processed...`);
        }
    }

    private showNightComplete(): void {
        if (this.isPopupShowing) return;
        this.isPopupShowing = true;
        
        // ✅ สร้าง Popup แสดงผล
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2;
        
        // Overlay
        const overlay = this.add.graphics();
        overlay.fillStyle(0x000000, 0.7);
        overlay.fillRect(0, 0, this.cameras.main.width, this.cameras.main.height);
        overlay.setDepth(500);
        this.popupElements.push(overlay);
        
        // Popup background
        const popupBg = this.add.graphics();
        popupBg.fillStyle(0x1a1a2e, 0.95);
        popupBg.fillRoundedRect(centerX - 300, centerY - 200, 600, 400, 16);
        popupBg.lineStyle(2, 0x4ecdc4);
        popupBg.strokeRoundedRect(centerX - 300, centerY - 200, 600, 400, 16);
        popupBg.setDepth(501);
        this.popupElements.push(popupBg);
        
        // Title
        const title = this.add.text(centerX, centerY - 160, `🌅 Night ${this.day} Complete!`, {
            fontSize: '32px', color: '#4ecdc4', fontFamily: 'monospace', stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(502);
        this.popupElements.push(title);
        
        // Stats
        const stats = [
            `👾 Total Enemies: ${this.totalEnemies}`,
            `✅ Enemies Completed: ${this.completeEnemies}/${this.totalEnemies}`,
            `🏠 Base HP Lost: ${Math.floor(100 - this.baseHP)} HP`,
            `💰 Resources Gained: ${this.completeEnemies * 5} wood`
        ];
        
        stats.forEach((stat, index) => {
            const text = this.add.text(centerX, centerY - 80 + index * 40, stat, {
                fontSize: '18px', color: '#ffffff', fontFamily: 'monospace'
            }).setOrigin(0.5).setDepth(502);
            this.popupElements.push(text);
        });
        
        // Continue button
        const continueBtn = this.add.text(centerX, centerY + 160, '▶ CONTINUE', {
            fontSize: '24px', color: '#00b894', fontFamily: 'monospace',
            backgroundColor: '#2d3436', padding: { x: 30, y: 15 }
        }).setOrigin(0.5).setDepth(502).setInteractive();
        this.popupElements.push(continueBtn);
        
        continueBtn.on('pointerover', () => continueBtn.setStyle({ color: '#ffffff' }));
        continueBtn.on('pointerout', () => continueBtn.setStyle({ color: '#00b894' }));
        continueBtn.on('pointerdown', () => {
            // Cleanup popup
            this.popupElements.forEach(el => el.destroy());
            this.popupElements = [];
            this.isPopupShowing = false;
            this.finishNight();
        });
    }

    private showGameOver(message: string): void {
        this.gameOver = true;
        if (this.spawnTimer) {
            this.spawnTimer.remove();
            this.spawnTimer = null;
        }
        this.scene.start('GameOverScene', { message, isWin: false, day: this.day });
    }

    private finishNight(): void {
        this.notificationSystem?.showSuccess(`🌅 Night ${this.day} survived!`, 2000);
        
        const currentDay = (this as any).actualDay || this.day;
        const nextDay = currentDay + 1;
        
        this.scene.start('TravelScene', {
            crewManager: this.crewManager,
            resourceManager: this.resourceManager,
            mapGenerator: this.mapGenerator,
            timeSystem: this.timeSystem,
            day: nextDay,
            actualDay: currentDay,
            travelState: this.travelState,
            isReturningFromNight: true
        });
    }

    update(time: number, delta: number): void {
        if (this.gameOver || this.isNightComplete || this.isPopupShowing) return;
        
        // ✅ Update monsters
        this.updateMonsters();
        
        // ✅ Update towers
        this.updateTowers();
        
        // ✅ Check base HP
        if (this.baseHP <= 0) {
            this.baseHP = 0;
            this.showGameOver('💀 Base Destroyed!');
        }
    }
}