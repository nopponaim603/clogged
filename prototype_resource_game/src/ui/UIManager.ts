// src/ui/UIManager.ts
import { Scene } from 'phaser';
import { GAME_CONFIG, RESOURCE_ICONS } from '../config';
import { Crew } from '../entities/Crew';
import { ResourceNode } from '../entities/ResourceNode';
import { CrewManager } from '../systems/CrewManager';
import { ResourceManager } from '../systems/ResourceManager';
import { TimeSystem } from '../systems/TimeSystem';
import { MapGenerator } from '../systems/MapGenerator';

export class UIManager {
    private scene: Scene;
    
    private dayText!: Phaser.GameObjects.Text;
    private timeText!: Phaser.GameObjects.Text;
    private baseHPText!: Phaser.GameObjects.Text;
    private pointsText!: Phaser.GameObjects.Text;
    private statusText!: Phaser.GameObjects.Text;
    private resourceTexts: Map<string, Phaser.GameObjects.Text> = new Map();
    private crewStatusTexts: Phaser.GameObjects.Text[] = [];
    
    private selectedCrewText!: Phaser.GameObjects.Text;
    private selectedTargetText!: Phaser.GameObjects.Text;
    private planStatusText!: Phaser.GameObjects.Text;
    private confirmBtn!: Phaser.GameObjects.Text;

    private onCrewSelect: (crew: Crew) => void = () => {};
    private onTargetSelect: (target: ResourceNode) => void = () => {};
    private onConfirm: () => void = () => {};

    constructor(scene: Scene) {
        this.scene = scene;
    }

    setupUI(
        crewManager: CrewManager,
        resourceManager: ResourceManager,
        timeSystem: TimeSystem,
        mapGenerator: MapGenerator
    ): void {
        this.scene.add.graphics()
            .fillStyle(0x1a1a2e, 0.95)
            .fillRect(0, 0, GAME_CONFIG.WIDTH, GAME_CONFIG.HEIGHT);

        this.createTopBar(timeSystem, resourceManager);
        this.createResourcePanel(resourceManager);
        this.createMapArea(mapGenerator);
        this.createBottomPanel(crewManager);
        this.createPlanningPanel();
    }

    private createTopBar(timeSystem: TimeSystem, resourceManager: ResourceManager): void {
        this.dayText = this.scene.add.text(20, 20, 
            `Day ${timeSystem.day}/${GAME_CONFIG.MAX_DAYS}`,
            { fontSize: '24px', color: '#ffffff', fontFamily: 'monospace' }
        );

        this.timeText = this.scene.add.text(250, 20,
            `⏱️ ${timeSystem.getTimeString()}`,
            { fontSize: '24px', color: '#4ecdc4', fontFamily: 'monospace' }
        );

        this.baseHPText = this.scene.add.text(450, 20,
            `🏠 HP: ${Math.floor(resourceManager.baseHP)}`,
            { fontSize: '24px', color: '#ff6b6b', fontFamily: 'monospace' }
        );

        this.pointsText = this.scene.add.text(650, 20,
            `⭐ ${GAME_CONFIG.CREW_POINTS}`,
            { fontSize: '24px', color: '#f9ca24', fontFamily: 'monospace' }
        );
    }

    private createResourcePanel(resourceManager: ResourceManager): void {
        const x = GAME_CONFIG.WIDTH - 180;
        let y = 60;

        const resourceList = ['wood', 'stone', 'iron', 'food', 'water', 'circuit', 'aluminum'];
        
        resourceList.forEach((key) => {
            const icon = RESOURCE_ICONS[key as keyof typeof RESOURCE_ICONS] || '📦';
            const text = this.scene.add.text(x, y, 
                `${icon} ${Math.floor(resourceManager.getResource(key))}`,
                { fontSize: '16px', color: '#ffffff', fontFamily: 'monospace' }
            );
            this.resourceTexts.set(key, text);
            y += 28;
        });
    }

    private createMapArea(mapGenerator: MapGenerator): void {
        const basePos = mapGenerator.getBasePosition();

        const graphics = this.scene.add.graphics();
        graphics.lineStyle(1, 0x4a4a5a, 0.3);
        for (let i = 0; i < 10; i++) {
            graphics.beginPath();
            graphics.moveTo(20 + i * 60, 60);
            graphics.lineTo(20 + i * 60, GAME_CONFIG.HEIGHT - 160);
            graphics.strokePath();
            graphics.beginPath();
            graphics.moveTo(20, 60 + i * 50);
            graphics.lineTo(GAME_CONFIG.WIDTH - 180, 60 + i * 50);
            graphics.strokePath();
        }

        // ✅ เปลี่ยนจาก Circle เป็น Arc
        const base = this.scene.add.arc(basePos.x, basePos.y, 30, 0, 360, false, 0x00b894);
        base.setStrokeStyle(3, 0x00d2a0);
        base.setInteractive();
        base.on('pointerdown', () => {
            this.onCrewSelect(null as any);
            this.onTargetSelect(null as any);
        });

        this.scene.add.text(basePos.x - 30, basePos.y - 10, '🏠 BASE', {
            fontSize: '14px',
            color: '#ffffff',
            fontFamily: 'monospace'
        });

        mapGenerator.resourceNodes.forEach(node => {
            const color = node.getColor();
            // ✅ เปลี่ยนจาก Circle เป็น Arc
            const arc = this.scene.add.arc(node.position.x, node.position.y, 18, 0, 360, false, color, 0.7);
            arc.setStrokeStyle(2, color, 0.5);
            arc.setInteractive();
            arc.on('pointerdown', () => {
                this.onTargetSelect(node);
            });
            node.sprite = arc as unknown as Phaser.GameObjects.Arc;

            const text = this.scene.add.text(
                node.position.x - 12,
                node.position.y - 10,
                node.icon,
                { fontSize: '20px' }
            );
            node.label = text;

            const label = this.scene.add.text(
                node.position.x - 25,
                node.position.y + 25,
                node.isRelic ? 'RELIC' : 
                node.isMonster ? `Lv.${node.difficulty}` : 
                `${node.amount}`,
                {
                    fontSize: '10px',
                    color: node.isRelic ? '#a29bfe' : 
                           node.isMonster ? '#ff6b6b' : '#ffffff',
                    fontFamily: 'monospace'
                }
            );
            node.label = label;
        });
    }

    private createBottomPanel(crewManager: CrewManager): void {
        const y = GAME_CONFIG.HEIGHT - 140;
        
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.95);
        bg.fillRect(20, y, GAME_CONFIG.WIDTH - 40, 120);
        bg.lineStyle(2, 0x4a4a5a);
        bg.strokeRect(20, y, GAME_CONFIG.WIDTH - 40, 120);

        this.statusText = this.scene.add.text(40, y + 10,
            '🌞 Plan your missions for today!',
            { fontSize: '16px', color: '#b2bec3', fontFamily: 'monospace' }
        );

        let crewX = 40;
        this.crewStatusTexts = [];
        
        crewManager.getAllAlive().forEach((crew) => {
            const text = this.scene.add.text(crewX, y + 40,
                `${crew.name} (❤️${Math.floor(crew.hp)}) ${crew.isBusy ? '🔴 Busy' : '🟢 Ready'}`,
                {
                    fontSize: '14px',
                    color: crew.isBusy ? '#ff6b6b' : '#4ecdc4',
                    fontFamily: 'monospace'
                }
            );
            text.setInteractive();
            text.on('pointerdown', () => {
                this.onCrewSelect(crew);
            });
            this.crewStatusTexts.push(text);
            crewX += 170;
        });
    }

    private createPlanningPanel(): void {
        const y = GAME_CONFIG.HEIGHT - 140;

        this.planStatusText = this.scene.add.text(40, y + 75,
            'Click a crew → Click a resource → Confirm Plan',
            {
                fontSize: '14px',
                color: '#b2bec3',
                fontFamily: 'monospace'
            }
        );

        this.selectedCrewText = this.scene.add.text(40, y + 95,
            'Selected: None',
            {
                fontSize: '14px',
                color: '#f9ca24',
                fontFamily: 'monospace'
            }
        );

        this.selectedTargetText = this.scene.add.text(280, y + 95,
            'Target: None',
            {
                fontSize: '14px',
                color: '#4ecdc4',
                fontFamily: 'monospace'
            }
        );

        this.confirmBtn = this.scene.add.text(GAME_CONFIG.WIDTH - 160, y + 80,
            '▶ CONFIRM PLAN',
            {
                fontSize: '16px',
                color: '#00b894',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 10, y: 5 }
            }
        );
        this.confirmBtn.setInteractive();
        this.confirmBtn.on('pointerdown', () => {
            this.onConfirm();
        });
    }

    setupInteractions(
        onCrewSelect: (crew: Crew) => void,
        onTargetSelect: (target: ResourceNode) => void,
        onConfirm: () => void
    ): void {
        this.onCrewSelect = onCrewSelect;
        this.onTargetSelect = onTargetSelect;
        this.onConfirm = onConfirm;
    }

    updateSelection(crew: Crew | null, target: ResourceNode | null): void {
        this.selectedCrewText.setText(`Selected: ${crew ? crew.name : 'None'} ${crew ? '⭐' : ''}`);
        this.selectedTargetText.setText(`Target: ${target ? target.icon + ' ' + target.type.toUpperCase() : 'None'}`);
    }

    updatePlanStatus(): void {
        const hasSelection = this.selectedCrewText.text.includes('⭐') && 
                           !this.selectedTargetText.text.includes('None');
        
        if (hasSelection) {
            this.planStatusText.setText('✅ Ready to confirm! Click CONFIRM PLAN');
            this.planStatusText.setColor('#00b894');
        } else {
            this.planStatusText.setText('Click a crew → Click a resource → Confirm Plan');
            this.planStatusText.setColor('#b2bec3');
        }
    }

    updateCrewStatus(): void {
        // This would need proper implementation with crew references
    }

    updateResourceDisplay(): void {
        // This would need access to resource manager
    }

    updateBaseHP(): void {
        // This would need access to resource manager
    }

    updateAll(): void {
        this.updateResourceDisplay();
        this.updateCrewStatus();
        this.updateBaseHP();
    }

    setStatus(message: string): void {
        this.statusText.setText(message);
    }

    showTutorial(): void {
        this.scene.time.delayedCall(2000, () => {
            this.setStatus('💡 Click a crew, then a resource, then CONFIRM PLAN!');
        });
    }

    showNotification(message: string): void {
        const text = this.scene.add.text(
            GAME_CONFIG.WIDTH / 2,
            GAME_CONFIG.HEIGHT / 2 - 40,
            message,
            {
                fontSize: '20px',
                color: '#4ecdc4',
                fontFamily: 'monospace',
                backgroundColor: '#1a1a2e',
                padding: { x: 20, y: 10 }
            }
        ).setOrigin(0.5);

        this.scene.time.delayedCall(2500, () => {
            text.destroy();
        });
    }

    showDamageEffect(damage: number): void {
        const basePos = { x: this.scene.cameras.main.centerX, y: this.scene.cameras.main.centerY };
        const text = this.scene.add.text(
            basePos.x - 30,
            basePos.y - 60,
            `-${Math.floor(damage)} HP!`,
            {
                fontSize: '28px',
                color: '#ff0000',
                fontFamily: 'monospace'
            }
        ).setOrigin(0.5);

        this.scene.tweens.add({
            targets: text,
            y: text.y - 50,
            alpha: 0,
            duration: 1500,
            onComplete: () => text.destroy()
        });

        this.scene.cameras.main.shake(200, 0.02);
    }

    showNightPhase(): void {
        this.scene.cameras.main.setBackgroundColor('#0a0a15');
        this.confirmBtn.setVisible(false);
        this.setStatus('🌙 NIGHT PHASE - Defend the base!');
    }
}