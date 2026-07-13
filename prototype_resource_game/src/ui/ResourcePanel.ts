// src/ui/ResourcePanel.ts
import { Scene } from 'phaser';
import { ResourceManager } from '../systems/ResourceManager';
import { RESOURCE_DATA } from '../data/ResourceData';

export class ResourcePanel {
    private scene: Scene;
    private resourceManager: ResourceManager;
    private resourceTexts: Map<string, Phaser.GameObjects.Text> = new Map();
    private panelX: number;
    private panelY: number;

    constructor(scene: Scene, resourceManager: ResourceManager, x: number = 1090, y: number = 60) {
        this.scene = scene;
        this.resourceManager = resourceManager;
        this.panelX = x;
        this.panelY = y;
        this.createPanel();
    }

    private createPanel(): void {
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.8);
        bg.fillRect(this.panelX - 10, this.panelY - 10, 180, 230);
        bg.lineStyle(1, 0x4a4a5a);
        bg.strokeRect(this.panelX - 10, this.panelY - 10, 180, 230);

        this.scene.add.text(this.panelX, this.panelY, '📦 RESOURCES', {
            fontSize: '16px',
            color: '#ffffff',
            fontFamily: 'monospace'
        });

        const resourceTypes = ['wood', 'stone', 'iron', 'food', 'water', 'circuit', 'aluminum'];
        let y = this.panelY + 30;

        resourceTypes.forEach((type) => {
            const data = RESOURCE_DATA[type];
            if (data) {
                const amount = this.resourceManager.getResource(type);
                const text = this.scene.add.text(this.panelX + 5, y, 
                    `${data.icon} ${data.name}: ${Math.floor(amount)}`,
                    {
                        fontSize: '14px',
                        color: '#b2bec3',
                        fontFamily: 'monospace'
                    }
                );
                this.resourceTexts.set(type, text);
                y += 28;
            }
        });
    }

    update(): void {
        this.resourceTexts.forEach((text, type) => {
            const amount = this.resourceManager.getResource(type);
            const data = RESOURCE_DATA[type];
            if (data) {
                text.setText(`${data.icon} ${data.name}: ${Math.floor(amount)}`);
            }
        });
    }

    show(): void {
        this.resourceTexts.forEach(text => text.setVisible(true));
    }

    hide(): void {
        this.resourceTexts.forEach(text => text.setVisible(false));
    }
}