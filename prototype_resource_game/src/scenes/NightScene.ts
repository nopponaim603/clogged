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
import { RESOURCE_DATA } from '../data/ResourceData';

interface DefenseTower {
    x: number;
    y: number;
    type: 'gun' | 'cannon' | 'laser' | 'shield';
    level: number;
    damage: number;
    range: number;
    cooldown: number;
    cooldownTimer: number;
    sprite: Phaser.GameObjects.Arc | null;
    target: Monster | null;
    cost: Record<string, number>;
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
    private selectedTowerType: string | null = null;
    private isPlacingTower: boolean = false;
    private towerPreview: Phaser.GameObjects.Arc | null = null;
    private selectedTowerCost: Record<string, number> = {};
    private isTowerSelected: boolean = false;
    private isWaitingForPlace: boolean = false; // ✅ เพิ่ม: รอการคลิกวาง
    
    // === UI ===
    private dayText!: Phaser.GameObjects.Text;
    private enemyCounterText!: Phaser.GameObjects.Text;
    private baseHPText!: Phaser.GameObjects.Text;
    private resourceTexts: Map<string, Phaser.GameObjects.Text> = new Map();
    private towerButtons: Phaser.GameObjects.Text[] = [];
    private buildModeText!: Phaser.GameObjects.Text;
    private popupElements: Phaser.GameObjects.GameObject[] = [];
    private prepareStatusText!: Phaser.GameObjects.Text;
    private confirmBtn!: Phaser.GameObjects.Text;
    private placedTowerCount: number = 0;
    private totalTowerCost: Record<string, number> = {};
    
    // === State ===
    private day: number = 1;
    private baseHP: number = 100;
    private maxBaseHP: number = 100;
    private gameOver: boolean = false;
    private isNightComplete: boolean = false;
    private isPopupShowing: boolean = false;
    private isPreparationPhase: boolean = true;
    private isDefensePhase: boolean = false;
    
    // === Enemy Tracking ===
    private totalEnemies: number = 0;
    private completeEnemies: number = 0;
    private enemiesSpawned: number = 0;
    private spawnTimer: Phaser.Time.TimerEvent | null = null;
    
    private enemyInfoPerDirection: Map<string, { count: number; hasElite: boolean; hasBoss: boolean }> = new Map();
    private spawnPoints: { x: number; y: number; direction: { x: number; y: number; label: string; id: string } }[] = [];
    private spawnInterval: number = 2000;
    
    private travelState: any = null;
    private enemyCountLabels: Phaser.GameObjects.Text[] = [];

    private towerTypes: { 
        type: string; 
        label: string; 
        damage: number; 
        range: number; 
        cooldown: number; 
        cost: Record<string, number>;
        isDefensive?: boolean;
    }[] = [
        { 
            type: 'gun', 
            label: '🔫 Gun', 
            damage: 15, 
            range: 160, 
            cooldown: 400,
            cost: { wood: 25, iron: 10 }
        },
        { 
            type: 'cannon', 
            label: '💥 Cannon', 
            damage: 35, 
            range: 220, 
            cooldown: 900,
            cost: { wood: 30, iron: 20, stone: 15 }
        },
        { 
            type: 'laser', 
            label: '⚡ Laser', 
            damage: 20, 
            range: 190, 
            cooldown: 600,
            cost: { wood: 20, circuit: 15, aluminum: 10 }
        },
        { 
            type: 'shield', 
            label: '🛡️ Shield', 
            damage: 0, 
            range: 100, 
            cooldown: 0,
            cost: { stone: 25, iron: 15, wood: 10 },
            isDefensive: true
        }
    ];

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
        
        this.totalEnemies = 10 + this.day * 3;
        this.spawnInterval = Math.max(1000, 3000 - this.day * 50);
        this.completeEnemies = 0;
        this.isNightComplete = false;
        this.isPopupShowing = false;
        this.isPreparationPhase = true;
        this.isDefensePhase = false;
        this.placedTowerCount = 0;
        this.totalTowerCost = {};
        this.isTowerSelected = false;
        this.isPlacingTower = false;
        this.isWaitingForPlace = false;
        
        this.generateSpawnPoints();
        this.generateEnemyInfo();
        
        this.notificationSystem = new NotificationSystem(this);
    }

    create(): void {
        this.setupUI();
        this.setupPreparationUI();
        this.drawBaseAndEnemyInfo();
        this.showPreparationPhase();
        
        // ✅ ตั้งค่า Right Click เพื่อยกเลิกการเลือก
        this.input.on('pointerdown', (pointer: Phaser.Input.Pointer) => {
            if (pointer.rightButtonDown() && (this.isTowerSelected || this.isPlacingTower)) {
                this.cancelPlacement();
                this.notificationSystem?.showInfo('❌ Placement cancelled', 1000);
            }
        });
        
        // ✅ คลิกซ้ายบนแผนที่เพื่อวางป้อม (เฉพาะเมื่ออยู่ในโหมดวาง)
        this.input.on('pointerdown', (pointer: Phaser.Input.Pointer) => {
            if (pointer.rightButtonDown()) return;
            
            // ✅ ถ้ากำลังเลือก tower และอยู่ในโหมดวาง ให้วาง
            if (this.isTowerSelected && this.isPlacingTower) {
                // ✅ เช็คว่าคลิกที่ปุ่มหรือไม่ (ไม่ให้วางทับปุ่ม)
                const clickedOnButton = this.towerButtons.some(btn => {
                    const bounds = btn.getBounds();
                    return pointer.x >= bounds.x && pointer.x <= bounds.x + bounds.width &&
                        pointer.y >= bounds.y && pointer.y <= bounds.y + bounds.height;
                });
                
                // ✅ เช็คว่าคลิกที่ปุ่มลบหรือไม่
                let clickedOnRemove = false;
                for (const tower of this.towers) {
                    const removeBtn = (tower as any).removeBtn;
                    if (removeBtn) {
                        const bounds = removeBtn.getBounds();
                        if (pointer.x >= bounds.x && pointer.x <= bounds.x + bounds.width &&
                            pointer.y >= bounds.y && pointer.y <= bounds.y + bounds.height) {
                            clickedOnRemove = true;
                            break;
                        }
                    }
                }
                
                if (!clickedOnButton && !clickedOnRemove) {
                    this.placeTower(pointer.x, pointer.y, this.selectedTowerType!, this.selectedTowerCost);
                }
            }
        });
    }

    // ============================================================
    // ENEMY INFO GENERATION
    // ============================================================
    private generateEnemyInfo(): void {
        const directions = ['top', 'bottom', 'left', 'right'];
        let remainingEnemies = this.totalEnemies;
        
        directions.forEach((dir, index) => {
            let count: number;
            if (index === directions.length - 1) {
                count = remainingEnemies;
            } else {
                const maxCount = Math.floor(remainingEnemies / (directions.length - index) * (0.6 + Math.random() * 0.4));
                count = Math.max(1, Math.floor(maxCount));
                remainingEnemies -= count;
            }
            
            const hasElite = count > 3 && Math.random() < 0.3 + this.day * 0.02;
            const hasBoss = (this.day % 3 === 0) && Math.random() < 0.3 + this.day * 0.01;
            
            this.enemyInfoPerDirection.set(dir, {
                count: count,
                hasElite: hasElite,
                hasBoss: hasBoss
            });
        });
    }

    private generateSpawnPoints(): void {
        const padding = 30;
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        
        const positions = [
            { x: centerX, y: padding, label: '⬆️', id: 'top' },
            { x: centerX, y: GAME_CONFIG.HEIGHT - padding, label: '⬇️', id: 'bottom' },
            { x: padding, y: centerY, label: '⬅️', id: 'left' },
            { x: GAME_CONFIG.WIDTH - padding, y: centerY, label: '➡️', id: 'right' }
        ];
        
        const directions = [
            { x: 0, y: 1, label: '⬆️ Top', id: 'top' },
            { x: 0, y: -1, label: '⬇️ Bottom', id: 'bottom' },
            { x: 1, y: 0, label: '⬅️ Left', id: 'left' },
            { x: -1, y: 0, label: '➡️ Right', id: 'right' }
        ];
        
        this.spawnPoints = positions.map((pos, index) => ({
            x: pos.x,
            y: pos.y,
            direction: directions[index]
        }));
    }

    // ============================================================
    // DRAW BASE & ENEMY INFO
    // ============================================================
    private drawBaseAndEnemyInfo(): void {
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        
        const baseArc = this.add.arc(centerX, centerY, 45, 0, 360, false, 0x00b894, 0.9);
        baseArc.setStrokeStyle(4, 0x00d2a0);
        this.add.text(centerX - 30, centerY - 12, '🏠 BASE', {
            fontSize: '20px', color: '#ffffff', fontFamily: 'monospace'
        });
        
        const rangeCircle = this.add.circle(centerX, centerY, 150, 0x00b894, 0.05);
        rangeCircle.setStrokeStyle(1, 0x00b894, 0.3);
        
        this.enemyCountLabels.forEach(label => label.destroy());
        this.enemyCountLabels = [];
        
        const directionLabels: { [key: string]: string } = {
            'top': '⬆️',
            'bottom': '⬇️',
            'left': '⬅️',
            'right': '➡️'
        };
        
        this.enemyInfoPerDirection.forEach((info, dir) => {
            const spawnPoint = this.spawnPoints.find(sp => sp.direction.id === dir);
            if (!spawnPoint) return;
            
            const label = directionLabels[dir] || dir;
            const count = info.count;
            const hasElite = info.hasElite;
            const hasBoss = info.hasBoss;
            
            let displayText = `${label} ${count}`;
            if (hasElite) displayText += ' ⚡';
            if (hasBoss) displayText += ' 👑';
            
            const text = this.add.text(spawnPoint.x - 20, spawnPoint.y - 30, displayText, {
                fontSize: '18px',
                color: hasBoss ? '#ff6b6b' : hasElite ? '#f9ca24' : '#4ecdc4',
                fontFamily: 'monospace',
                backgroundColor: '#1a1a2e',
                padding: { x: 6, y: 3 },
                stroke: '#000000',
                strokeThickness: 3
            });
            this.enemyCountLabels.push(text);
            
            if (count > 5) {
                const warning = this.add.text(spawnPoint.x - 30, spawnPoint.y + 15, '⚠️ LARGE WAVE', {
                    fontSize: '12px',
                    color: '#ff6b6b',
                    fontFamily: 'monospace',
                    backgroundColor: '#1a1a2e',
                    padding: { x: 4, y: 2 }
                });
                this.enemyCountLabels.push(warning);
            }
        });
    }

    // ============================================================
    // UI SETUP
    // ============================================================
    private setupUI(): void {
        this.cameras.main.setBackgroundColor('#0a0a15');
        
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

        this.createResourceUI();
        this.createTowerSelectionUI();
    }

    private setupPreparationUI(): void {
        this.prepareStatusText = this.add.text(GAME_CONFIG.WIDTH / 2, 100,
            '🔧 PREPARATION PHASE - Place your towers!',
            {
                fontSize: '20px',
                color: '#4ecdc4',
                fontFamily: 'monospace',
                backgroundColor: '#1a1a2e',
                padding: { x: 15, y: 8 }
            }
        ).setOrigin(0.5);

        this.buildModeText = this.add.text(20, 680,
            '🔧 Select a tower from the panel, then click on the map to place it (Right click to cancel)',
            { fontSize: '16px', color: '#b2bec3', fontFamily: 'monospace' }
        );

        this.confirmBtn = this.add.text(GAME_CONFIG.WIDTH - 200, 680,
            '▶ START DEFENSE',
            {
                fontSize: '20px',
                color: '#00b894',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 20, y: 10 }
            }
        ).setInteractive();
        
        this.confirmBtn.on('pointerover', () => this.confirmBtn.setStyle({ color: '#ffffff' }));
        this.confirmBtn.on('pointerout', () => this.confirmBtn.setStyle({ color: '#00b894' }));
        this.confirmBtn.on('pointerdown', () => this.startDefensePhase());
    }

    private createResourceUI(): void {
        const x = GAME_CONFIG.WIDTH - 200;
        let y = 60;
        const resourceTypes = ['wood', 'stone', 'iron', 'water', 'circuit', 'aluminum'];
        resourceTypes.forEach((type) => {
            const amount = this.resourceManager?.getResource(type) || 0;
            const icon = RESOURCE_DATA[type]?.icon || '📦';
            const text = this.add.text(x, y, 
                `${icon} ${Math.floor(amount)}`,
                { fontSize: '16px', color: '#ffffff', fontFamily: 'monospace' }
            );
            this.resourceTexts.set(type, text);
            y += 28;
        });
    }

    private createTowerSelectionUI(): void {
        const y = GAME_CONFIG.HEIGHT - 70;
        
        this.towerTypes.forEach((tower, index) => {
            const x = 80 + index * 270;
            
            let costText = '';
            const costEntries = Object.entries(tower.cost);
            costEntries.forEach(([type, amount], i) => {
                const icon = RESOURCE_DATA[type]?.icon || type;
                costText += `${icon}${amount}`;
                if (i < costEntries.length - 1) costText += ' + ';
            });
            
            const btn = this.add.text(x, y, 
                `${tower.label}\n${costText}`,
                { 
                    fontSize: '14px', 
                    color: '#4ecdc4', 
                    fontFamily: 'monospace', 
                    backgroundColor: '#2d3436', 
                    padding: { x: 10, y: 5 },
                    align: 'center'
                }
            );
            btn.setInteractive();
            btn.on('pointerdown', () => {
                this.selectTowerType(tower.type, tower.cost);
            });
            btn.on('pointerover', () => btn.setStyle({ color: '#ffffff' }));
            btn.on('pointerout', () => {
                if (this.isTowerSelected && this.selectedTowerType === tower.type) {
                    btn.setStyle({ color: '#f9ca24' });
                } else {
                    btn.setStyle({ color: '#4ecdc4' });
                }
            });
            this.towerButtons.push(btn);
            
            const statsText = tower.isDefensive ? 
                `🛡️ Shield: Protects base` :
                `⚔️${tower.damage} 📏${tower.range} ⏱️${tower.cooldown}ms`;
            
            this.add.text(x + 70, y + 35, statsText, {
                fontSize: '10px', color: '#636e72', fontFamily: 'monospace'
            });
        });
    }

    private getIcon(type: string): string {
        return RESOURCE_DATA[type]?.icon || '📦';
    }

    private hasEnoughResources(cost: Record<string, number>): boolean {
        const resources = this.resourceManager?.resources;
        if (!resources) return false;
        
        for (const [type, amount] of Object.entries(cost)) {
            const current = resources[type as keyof typeof resources] || 0;
            if (typeof current === 'number' && current < amount) {
                return false;
            }
        }
        return true;
    }

    private spendResources(cost: Record<string, number>): boolean {
        const resources = this.resourceManager?.resources;
        if (!resources) return false;
        
        for (const [type, amount] of Object.entries(cost)) {
            const current = resources[type as keyof typeof resources] || 0;
            if (typeof current === 'number' && current >= amount) {
                (resources[type as keyof typeof resources] as number) -= amount;
            } else {
                return false;
            }
        }
        return true;
    }

    // ============================================================
    // PREPARATION PHASE
    // ============================================================
    private showPreparationPhase(): void {
        this.isPreparationPhase = true;
        this.isDefensePhase = false;
        this.prepareStatusText.setText('🔧 PREPARATION PHASE - Place your towers!');
        this.confirmBtn.setVisible(true);
        this.buildModeText.setText('🔧 Select a tower from the panel, then click on the map to place it (Right click to cancel)');
        this.enemyCounterText.setText(`👾 Enemy: ${this.completeEnemies}/${this.totalEnemies}`);
        
        this.notificationSystem?.showInfo(`🌙 Night ${this.day} - Place your towers!`, 2000);
    }

    private selectTowerType(type: string, cost: Record<string, number>): void {
        if (this.isDefensePhase) {
            this.notificationSystem?.showError('❌ Cannot place towers during defense phase!', 2000);
            return;
        }
        
        // ✅ ถ้าเลือก tower เดิม ให้ยกเลิกการเลือก
        if (this.isTowerSelected && this.selectedTowerType === type) {
            this.cancelPlacement();
            this.notificationSystem?.showInfo('❌ Deselected tower', 1000);
            return;
        }
        
        // ✅ ตรวจสอบทรัพยากร
        if (!this.hasEnoughResources(cost)) {
            let needText = '';
            for (const [type, amount] of Object.entries(cost)) {
                const icon = RESOURCE_DATA[type]?.icon || type;
                const current = this.resourceManager?.getResource(type) || 0;
                needText += `${icon}${current}/${amount} `;
            }
            this.notificationSystem?.showError(`Not enough resources! Need ${needText}`, 2000);
            return;
        }
        
        // ✅ เลือก tower และเข้าสู่โหมดวางทันที
        this.selectedTowerType = type;
        this.selectedTowerCost = cost;
        this.isTowerSelected = true;
        this.isPlacingTower = true;  // ✅ เข้าโหมดวางทันที
        
        this.updateTowerButtons();
        
        // ✅ สร้าง Preview (แสดงรูปป้อมที่กำลังเลือก)
        if (this.towerPreview) {
            this.towerPreview.destroy();
        }
        this.towerPreview = this.add.arc(0, 0, 20, 0, 360, false, 0x4ecdc4, 0.3);
        this.towerPreview.setStrokeStyle(2, 0x4ecdc4);
        this.towerPreview.setVisible(false);
        
        // ✅ Mouse move for preview
        this.input.on('pointermove', (pointer: Phaser.Input.Pointer) => {
            if (this.towerPreview && this.isPlacingTower) {
                this.towerPreview.x = pointer.x;
                this.towerPreview.y = pointer.y;
                this.towerPreview.setVisible(true);
            }
        });
        
        let costText = '';
        for (const [type, amount] of Object.entries(cost)) {
            const icon = RESOURCE_DATA[type]?.icon || type;
            costText += `${icon}${amount} `;
        }
        this.buildModeText.setText(`✅ Selected ${type} tower (${costText}) - Click on map to place (Right click to cancel)`);
        this.buildModeText.setColor('#f9ca24');
        
        this.notificationSystem?.showInfo(`✅ ${type} tower selected! Click on map to place.`, 1500);
    }

    private updateTowerButtons(): void {
        this.towerButtons.forEach((btn, index) => {
            const tower = this.towerTypes[index];
            if (!tower) return;
            
            if (this.isTowerSelected && this.selectedTowerType === tower.type) {
                btn.setStyle({ color: '#f9ca24' });
            } else {
                btn.setStyle({ color: '#4ecdc4' });
            }
        });
    }

    // ✅ placeTower - วางป้อม (แก้ไขปุ่มลบ)
    private placeTower(x: number, y: number, type: string, cost: Record<string, number>): void {
        if (this.isDefensePhase) {
            this.notificationSystem?.showError('❌ Cannot place towers during defense phase!', 2000);
            this.cancelPlacement();
            return;
        }
        
        if (!this.isTowerSelected || !this.isPlacingTower) {
            return;
        }
        
        if (!type || !cost || Object.keys(cost).length === 0) {
            return;
        }
        
        if (!this.hasEnoughResources(cost)) {
            this.notificationSystem?.showError(`Not enough resources!`, 2000);
            return;
        }
        
        this.spendResources(cost);
        this.updateResourceUI();
        
        const towerDef = this.towerTypes.find(t => t.type === type);
        if (!towerDef) return;
        
        const tower: DefenseTower = {
            x, y,
            type: type as 'gun' | 'cannon' | 'laser' | 'shield',
            level: 1,
            damage: towerDef.damage || 0,
            range: towerDef.range || 0,
            cooldown: towerDef.cooldown || 0,
            cooldownTimer: 0,
            sprite: null,
            target: null,
            cost: cost
        };
        
        const isShield = type === 'shield';
        const color = isShield ? 0x4ecdc4 : 
                    type === 'gun' ? 0x4ecdc4 : 
                    type === 'cannon' ? 0xf9ca24 : 0xff6b6b;
        
        const sprite = this.add.arc(x, y, isShield ? 20 : 15, 0, 360, false, color, 0.8);
        sprite.setStrokeStyle(isShield ? 3 : 2, 0xffffff);
        tower.sprite = sprite;
        
        if (isShield) {
            const shieldCircle = this.add.circle(x, y, 50, 0x4ecdc4, 0.1);
            shieldCircle.setStrokeStyle(2, 0x4ecdc4, 0.3);
            (tower as any).shieldCircle = shieldCircle;
        }
        
        this.add.text(x - 10, y - 30, type.toUpperCase(), {
            fontSize: '10px', color: '#ffffff', fontFamily: 'monospace'
        });
        
        // ✅ ปุ่มลบ - แก้ไขแล้ว (ไม่มี e.stopPropagation)
        const removeBtn = this.add.text(x + 15, y - 25, '✕', {
            fontSize: '14px', color: '#ff6b6b', fontFamily: 'monospace', backgroundColor: '#2d3436'
        }).setInteractive();
        removeBtn.on('pointerdown', () => {
            // ✅ ลบป้อมเท่านั้น ไม่มีการสร้างใหม่
            this.removeTower(tower);
            // ✅ ถ้ากำลังเลือกป้อมอยู่ ให้ยังคงอยู่ในโหมดวาง
            if (this.isTowerSelected && this.isPlacingTower) {
                this.buildModeText.setText(`✅ Still placing ${this.selectedTowerType} - Click on map to place (Right click to cancel)`);
            }
        });
        (tower as any).removeBtn = removeBtn;
        
        this.towers.push(tower);
        this.placedTowerCount++;
        
        for (const [type, amount] of Object.entries(cost)) {
            this.totalTowerCost[type] = (this.totalTowerCost[type] || 0) + amount;
        }
        
        this.notificationSystem?.showSuccess(`${type} tower placed! (${this.placedTowerCount} towers)`, 1500);
        
        // ✅ ยังคงอยู่ในโหมดเลือก tower เดิม (เพื่อวางต่อ)
        this.isTowerSelected = true;
        this.isPlacingTower = true;
        this.buildModeText.setText(`✅ ${type} placed! Click again to place more (Right click to cancel)`);
    }

    // ✅ removeTower - ลบป้อมเท่านั้น
    private removeTower(tower: DefenseTower): void {
        if (this.isDefensePhase) {
            this.notificationSystem?.showError('❌ Cannot remove towers during defense phase!', 2000);
            return;
        }
        
        const resources = this.resourceManager?.resources;
        if (resources) {
            for (const [type, amount] of Object.entries(tower.cost)) {
                (resources[type as keyof typeof resources] as number) += amount;
                this.totalTowerCost[type] = (this.totalTowerCost[type] || 0) - amount;
            }
            this.placedTowerCount--;
            this.updateResourceUI();
        }
        
        // ✅ ลบ sprite
        if (tower.sprite) tower.sprite.destroy();
        if ((tower as any).removeBtn) (tower as any).removeBtn.destroy();
        if ((tower as any).shieldCircle) (tower as any).shieldCircle.destroy();
        
        // ✅ ลบ label
        this.children.list.forEach(child => {
            if (child.type === 'Text' && 
                (child as any).x === tower.x - 10 && 
                (child as any).y === tower.y - 30) {
                child.destroy();
            }
        });
        
        this.towers = this.towers.filter(t => t !== tower);
        
        this.notificationSystem?.showInfo(`↩️ Tower removed! (${this.placedTowerCount} towers left)`, 1500);
    }

    // ✅ cancelPlacement - ยกเลิกการเลือกและซ่อน Preview
    private cancelPlacement(): void {
        this.isTowerSelected = false;
        this.isPlacingTower = false;
        this.isWaitingForPlace = false;
        this.selectedTowerType = null;
        this.selectedTowerCost = {};
        
        // ✅ ลบ Preview
        if (this.towerPreview) {
            this.towerPreview.destroy();
            this.towerPreview = null;
        }
        
        // ✅ ลบ event listener pointermove
        this.input.off('pointermove');
        
        this.updateTowerButtons();
        this.buildModeText.setText('🔧 Select a tower from the panel, then click on the map to place it (Right click to cancel)');
        this.buildModeText.setColor('#b2bec3');
    }

    // ✅ startDefensePhase - ยกเลิกการเลือกและซ่อน Preview
    private startDefensePhase(): void {
        if (this.isDefensePhase) return;
        
        // ✅ ยกเลิกการเลือกก่อนเริ่ม
        if (this.isTowerSelected || this.isPlacingTower) {
            this.cancelPlacement();
        }
        
        // ✅ ไม่บังคับให้มีป้อม
        if (this.towers.length === 0) {
            this.notificationSystem?.showWarning('⚠️ No towers placed! You can still start defense, but it will be harder.', 3000);
        }
        
        this.isPreparationPhase = false;
        this.isDefensePhase = true;
        this.isPlacingTower = false;
        this.isTowerSelected = false;
        
        // ✅ ซ่อน Preview
        if (this.towerPreview) {
            this.towerPreview.destroy();
            this.towerPreview = null;
        }
        
        this.prepareStatusText.setText('⚔️ DEFENSE PHASE - Defend the base!');
        this.confirmBtn.setVisible(false);
        this.buildModeText.setText('⚔️ Fight!');
        this.buildModeText.setColor('#ff6b6b');
        
        // ✅ ซ่อนปุ่มลบทั้งหมด
        this.towers.forEach(tower => {
            if ((tower as any).removeBtn) {
                (tower as any).removeBtn.destroy();
            }
        });
        
        this.enemyCountLabels.forEach(label => label.destroy());
        this.enemyCountLabels = [];
        
        this.speedControl = new SpeedControl(
            this,
            GAME_CONFIG.WIDTH - 270,
            60,
            (speed: number) => {
                this.setMonsterSpeed(speed);
                this.notificationSystem?.showInfo(`Speed: ${speed}x`, 1000);
            },
            () => {
                this.skipNight();
            }
        );
        
        this.notificationSystem?.showWarning(`⚔️ Defense Phase ${this.day} begins! ${this.totalEnemies} enemies approaching!`, 3000);
        this.enemiesSpawned = 0;
        this.completeEnemies = 0;
        this.isNightComplete = false;
        this.isPopupShowing = false;
        this.isSkipping = false;
        
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

    // ============================================================
    // ENEMY SPAWN
    // ============================================================
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
        
        const spawnPoint = this.spawnPoints[Math.floor(Math.random() * this.spawnPoints.length)];
        
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
        
        const color = type === 'normal' ? 0xe74c3c : type === 'fast' ? 0xf9ca24 : type === 'tank' ? 0x8e44ad : 0xff0000;
        const size = type === 'boss' ? 25 : 15;
        const sprite = this.add.arc(monster.x, monster.y, size, 0, 360, false, color, 0.9);
        sprite.setStrokeStyle(2, 0xffffff);
        monster.sprite = sprite;
        
        const icon = type === 'boss' ? '👑' : '👹';
        const iconText = this.add.text(monster.x - 10, monster.y - 12, icon, { fontSize: '20px' });
        monster.icon = iconText;
        
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

    // ============================================================
    // UPDATE MONSTERS & TOWERS
    // ============================================================
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
                this.baseHP -= monster.damage;
                this.baseHPText.setText(`🏠 HP: ${Math.floor(this.baseHP)}/${this.maxBaseHP}`);
                this.completeEnemies++;
                monster.hp = 0;
                
                if (monster.sprite) monster.sprite.destroy();
                if (monster.icon) monster.icon.destroy();
                if ((monster as any).hpBarBg) (monster as any).hpBarBg.destroy();
                if ((monster as any).hpBar) (monster as any).hpBar.destroy();
                
                this.notificationSystem?.showError(`💥 Enemy reached base! (${this.completeEnemies}/${this.totalEnemies})`, 1500);
                this.updateEnemyCounter();
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
            
            if (monster.sprite) {
                monster.sprite.x = monster.x;
                monster.sprite.y = monster.y;
            }
            if (monster.icon) {
                monster.icon.x = monster.x - 10;
                monster.icon.y = monster.y - 12;
            }
            
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
        
        this.monsters = this.monsters.filter(m => m.hp > 0);
    }

    private updateTowers(): void {
        for (const tower of this.towers) {
            if (tower.type === 'shield') continue;
            
            if (tower.cooldownTimer > 0) {
                tower.cooldownTimer -= 16;
                continue;
            }
            
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
                    const reward = closestMonster.reward || 5;
                    this.resourceManager?.addResource('wood', reward);
                    this.updateResourceUI();
                    this.completeEnemies++;
                    this.updateEnemyCounter();
                    this.notificationSystem?.showInfo(`💰 +${reward} wood! (${this.completeEnemies}/${this.totalEnemies})`, 1000);
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
            const icon = RESOURCE_DATA[type]?.icon || '📦';
            text.setText(`${icon} ${Math.floor(amount)}`);
        });
    }

    // ============================================================
    // CHECK NIGHT COMPLETE
    // ============================================================
    private checkNightComplete(): void {
        if (this.isNightComplete || this.isPopupShowing) {
            return;
        }
        
        const allSpawned = this.enemiesSpawned >= this.totalEnemies;
        const allProcessed = this.completeEnemies >= this.totalEnemies;
        const noMonstersLeft = this.monsters.length === 0;
        
        if (allSpawned && this.completeEnemies === this.totalEnemies && noMonstersLeft) {
            this.isNightComplete = true;
            if (this.spawnTimer) {
                this.spawnTimer.remove();
                this.spawnTimer = null;
            }
            this.showNightComplete();
        }
    }

    // ============================================================
    // NIGHT COMPLETE POPUP
    // ============================================================
    private showNightComplete(): void {
        if (this.isPopupShowing) return;
        this.isPopupShowing = true;
        
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2;
        
        const overlay = this.add.graphics();
        overlay.fillStyle(0x000000, 0.7);
        overlay.fillRect(0, 0, this.cameras.main.width, this.cameras.main.height);
        overlay.setDepth(500);
        this.popupElements.push(overlay);
        
        const popupBg = this.add.graphics();
        popupBg.fillStyle(0x1a1a2e, 0.95);
        popupBg.fillRoundedRect(centerX - 300, centerY - 200, 600, 400, 16);
        popupBg.lineStyle(2, 0x4ecdc4);
        popupBg.strokeRoundedRect(centerX - 300, centerY - 200, 600, 400, 16);
        popupBg.setDepth(501);
        this.popupElements.push(popupBg);
        
        const title = this.add.text(centerX, centerY - 160, `🌅 Night ${this.day} Complete!`, {
            fontSize: '32px', color: '#4ecdc4', fontFamily: 'monospace', stroke: '#000000', strokeThickness: 4
        }).setOrigin(0.5).setDepth(502);
        this.popupElements.push(title);
        
        const stats = [
            `👾 Total Enemies: ${this.totalEnemies}`,
            `✅ Enemies Completed: ${this.completeEnemies}/${this.totalEnemies}`,
            `🏠 Base HP Lost: ${Math.floor(100 - this.baseHP)} HP`,
            `💰 Resources Gained: ${this.completeEnemies * 5} wood`,
            `🗼 Towers Built: ${this.placedTowerCount}`
        ];
        
        stats.forEach((stat, index) => {
            const text = this.add.text(centerX, centerY - 80 + index * 40, stat, {
                fontSize: '18px', color: '#ffffff', fontFamily: 'monospace'
            }).setOrigin(0.5).setDepth(502);
            this.popupElements.push(text);
        });
        
        const continueBtn = this.add.text(centerX, centerY + 160, '▶ CONTINUE', {
            fontSize: '24px', color: '#00b894', fontFamily: 'monospace',
            backgroundColor: '#2d3436', padding: { x: 30, y: 15 }
        }).setOrigin(0.5).setDepth(502).setInteractive();
        this.popupElements.push(continueBtn);
        
        continueBtn.on('pointerover', () => continueBtn.setStyle({ color: '#ffffff' }));
        continueBtn.on('pointerout', () => continueBtn.setStyle({ color: '#00b894' }));
        continueBtn.on('pointerdown', () => {
            this.popupElements.forEach(el => el.destroy());
            this.popupElements = [];
            this.isPopupShowing = false;
            this.finishNight();
        });
    }

    // ============================================================
    // SPEED CONTROL
    // ============================================================
    private setMonsterSpeed(speed: number): void {
        (this as any).monsterSpeedMultiplier = speed;
    }

    private skipNight(): void {
        if (this.isSkipping || this.isNightComplete) return;
        this.isSkipping = true;
        
        this.notificationSystem?.showWarning('⏭️ Skipping night...', 1500);
        
        for (const monster of this.monsters) {
            monster.hp = 0;
            if (monster.sprite) monster.sprite.destroy();
            if (monster.icon) monster.icon.destroy();
            if ((monster as any).hpBarBg) (monster as any).hpBarBg.destroy();
            if ((monster as any).hpBar) (monster as any).hpBar.destroy();
            this.completeEnemies++;
        }
        this.monsters = [];
        
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

    // ============================================================
    // GAME OVER & FINISH
    // ============================================================
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

    // ============================================================
    // UPDATE
    // ============================================================
    update(time: number, delta: number): void {
        if (this.gameOver || this.isNightComplete || this.isPopupShowing) return;
        
        if (this.isPreparationPhase) return;
        
        this.updateMonsters();
        this.updateTowers();
        
        if (this.baseHP <= 0) {
            this.baseHP = 0;
            this.showGameOver('💀 Base Destroyed!');
        }
    }
}