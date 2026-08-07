import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { CrewManager } from '../systems/CrewManager';
import { ResourceManager } from '../systems/ResourceManager';
import { MapGenerator } from '../systems/MapGenerator';
import { TimeSystem } from '../systems/TimeSystem';

export class TravelScene extends Scene {
    private crewManager!: CrewManager;
    private resourceManager!: ResourceManager;
    private mapGenerator!: MapGenerator;
    private timeSystem!: TimeSystem;
    private day: number = 1;
    private isFirstTime: boolean = true;

    constructor() {
        super('TravelScene');
    }

    init(data: any): void {
        if (data) {
            this.crewManager = data.crewManager;
            this.resourceManager = data.resourceManager;
            this.mapGenerator = data.mapGenerator;
            this.timeSystem = data.timeSystem;
            this.day = data.day || 1;
            this.isFirstTime = false;
        }
    }

    create(): void {
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2;

        // Background
        this.cameras.main.setBackgroundColor('#0a0a1a');

        // ✅ Title
        this.add.text(centerX, centerY - 150, '🚀 NAVIGATION PHASE', {
            fontSize: '36px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            stroke: '#000000',
            strokeThickness: 4
        }).setOrigin(0.5);

        // ✅ Day info
        this.add.text(centerX, centerY - 80, `Day ${this.day} / ${GAME_CONFIG.MAX_DAYS}`, {
            fontSize: '24px',
            color: '#ffffff',
            fontFamily: 'monospace'
        }).setOrigin(0.5);

        // ✅ Crew status
        const aliveCrews = this.crewManager?.getAllAlive() || [];
        const crewStatus = aliveCrews.map(c => `${c.name} (❤️${Math.floor(c.hp)}/${c.maxHp})`).join(' | ');
        this.add.text(centerX, centerY - 30, `👥 ${crewStatus || 'No crew'}`, {
            fontSize: '16px',
            color: '#b2bec3',
            fontFamily: 'monospace'
        }).setOrigin(0.5);

        // ✅ Resources summary
        if (this.resourceManager) {
            const resources = this.resourceManager.resources;
            const resourceText = `🪵 ${Math.floor(resources.wood)} | 🪨 ${Math.floor(resources.stone)} | ⛏️ ${Math.floor(resources.iron)} | 🍖 ${Math.floor(resources.food)}`;
            this.add.text(centerX, centerY + 20, resourceText, {
                fontSize: '16px',
                color: '#f9ca24',
                fontFamily: 'monospace'
            }).setOrigin(0.5);
        }

        // ✅ Map path preview (placeholder)
        this.drawPathPreview();

        // ✅ Start Day button
        const startBtn = this.add.text(centerX, centerY + 120, '▶ START DAY', {
            fontSize: '28px',
            color: '#00b894',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 30, y: 15 }
        }).setOrigin(0.5).setInteractive();

        startBtn.on('pointerover', () => {
            startBtn.setStyle({ color: '#ffffff' });
        });

        startBtn.on('pointerout', () => {
            startBtn.setStyle({ color: '#00b894' });
        });

        startBtn.on('pointerdown', () => {
            this.goToDayScene();
        });

        // ✅ Version
        this.add.text(centerX, centerY + 280, 'v1.0 - Navigation Phase (Placeholder)', {
            fontSize: '12px',
            color: '#2d3436',
            fontFamily: 'monospace'
        }).setOrigin(0.5);
    }

    private drawPathPreview(): void {
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2 + 70;

        // ✅ Draw simple path preview (placeholder)
        const graphics = this.add.graphics();
        graphics.lineStyle(2, 0x4a4a5a, 0.5);
        
        // Draw 5 nodes in a line
        const nodes = [
            { x: centerX - 200, y: centerY, label: '🏠' },
            { x: centerX - 100, y: centerY, label: '🌲' },
            { x: centerX, y: centerY, label: '🏛️' },
            { x: centerX + 100, y: centerY, label: '👹' },
            { x: centerX + 200, y: centerY, label: '🏁' }
        ];

        // Draw lines between nodes
        for (let i = 0; i < nodes.length - 1; i++) {
            graphics.beginPath();
            graphics.moveTo(nodes[i].x, nodes[i].y);
            graphics.lineTo(nodes[i + 1].x, nodes[i + 1].y);
            graphics.strokePath();
        }

        // Draw nodes
        nodes.forEach((node, index) => {
            const color = index === 0 ? 0x00b894 : index === nodes.length - 1 ? 0xff6b6b : 0xf9ca24;
            const arc = this.add.arc(node.x, node.y, 12, 0, 360, false, color, 0.6);
            arc.setStrokeStyle(2, color);
            this.add.text(node.x - 8, node.y - 8, node.label, { fontSize: '14px' });
        });

        // ✅ Label
        this.add.text(centerX, centerY + 40, '📌 Path: Base → Resources → Relic → Monster → Goal', {
            fontSize: '12px',
            color: '#636e72',
            fontFamily: 'monospace'
        }).setOrigin(0.5);
    }

    private goToDayScene(): void {
        // ✅ ถ้าเป็นครั้งแรก ให้สร้าง systems ใหม่
        if (this.isFirstTime || !this.crewManager) {
            this.crewManager = new CrewManager();
            this.resourceManager = new ResourceManager();
            this.mapGenerator = new MapGenerator();
            this.timeSystem = new TimeSystem();
            this.isFirstTime = false;
        }

        // ✅ ส่งต่อไปยัง DayScene
        this.scene.start('DayScene', {
            crewManager: this.crewManager,
            resourceManager: this.resourceManager,
            mapGenerator: this.mapGenerator,
            timeSystem: this.timeSystem,
            day: this.day
        });
    }
}