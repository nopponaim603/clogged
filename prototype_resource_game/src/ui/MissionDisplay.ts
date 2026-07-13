// src/ui/MissionDisplay.ts
import { Scene } from 'phaser';
import { Crew } from '../entities/Crew';
import { ResourceNode } from '../entities/ResourceNode';
import { GAME_CONFIG } from '../config';

export class MissionDisplay {
    private scene: Scene;
    private line: Phaser.GameObjects.Graphics | null = null;
    private infoText: Phaser.GameObjects.Text | null = null;
    private timer: Phaser.Time.TimerEvent | null = null;

    constructor(scene: Scene) {
        this.scene = scene;
    }

    showMission(crew: Crew, target: ResourceNode): void {
        // Clear old display
        this.clear();

        // ✅ คำนวณเวลา (ไม่ต้องคูณ TIME_UNIT_PER_SECOND เพราะได้ค่าเป็นหน่วยเวลาอยู่แล้ว)
        const distance = this.getDistance(crew.position, target.position);
        const travelTime = crew.calculateTravelTime(distance);
        const actionTime = target.getActionTime(crew.getEffectiveGathering());
        const totalTime = travelTime * 2 + actionTime;

        // Draw line from crew to target
        this.line = this.scene.add.graphics();
        this.line.lineStyle(2, 0x4ecdc4, 0.8);
        this.line.beginPath();
        this.line.moveTo(crew.position.x, crew.position.y);
        this.line.lineTo(target.position.x, target.position.y);
        this.line.strokePath();

        // Add arrow effect (dashed line animation)
        this.animateLine(crew.position, target.position);

        // Show info text
        const midX = (crew.position.x + target.position.x) / 2;
        const midY = (crew.position.y + target.position.y) / 2 - 30;

        this.infoText = this.scene.add.text(midX, midY, 
            `🚀 ${crew.name}\n` +
            `⏱️ Travel: ${Math.floor(travelTime)} units\n` +
            `⚡ Action: ${Math.floor(actionTime)} units\n` +
            `📊 Total: ${Math.floor(totalTime)} units`,
            {
                fontSize: '14px',
                color: '#4ecdc4',
                fontFamily: 'monospace',
                backgroundColor: '#1a1a2e',
                padding: { x: 10, y: 5 },
                align: 'center',
                stroke: '#000000',
                strokeThickness: 3
            }
        ).setOrigin(0.5).setDepth(100);

        // Show time remaining warning if not enough time
        const timeSystem = this.scene.scene.get('GameScene') as any;
        if (timeSystem && timeSystem.timeSystem) {
            const remaining = timeSystem.timeSystem.getRemainingTime();
            if (totalTime > remaining) {
                const warning = this.scene.add.text(
                    midX,
                    midY + 60,
                    `⚠️ NOT ENOUGH TIME! (Need ${Math.floor(totalTime)}, Have ${Math.floor(remaining)})`,
                    {
                        fontSize: '14px',
                        color: '#ff6b6b',
                        fontFamily: 'monospace',
                        backgroundColor: '#1a1a2e',
                        padding: { x: 10, y: 5 },
                        stroke: '#000000',
                        strokeThickness: 3
                    }
                ).setOrigin(0.5).setDepth(101);
                
                // Auto remove after 3 seconds
                this.scene.time.delayedCall(3000, () => {
                    warning.destroy();
                });
            }
        }
    }

    showMissionChain(crew: Crew, targets: ResourceNode[]): void {
        this.clear();

        if (targets.length === 0) return;

        const basePos = { x: crew.position.x, y: crew.position.y };
        let totalTime = 0;
        let lastPos = basePos;
        let infoText = `🚀 ${crew.name} Chain (${targets.length} nodes)\n`;

        for (let i = 0; i < targets.length; i++) {
            const target = targets[i];
            const distance = this.getDistance(lastPos, target.position);
            const travelTime = crew.calculateTravelTime(distance);
            const actionTime = target.getActionTime(crew.getEffectiveGathering());
            const total = travelTime * 2 + actionTime;
            
            totalTime += total;
            infoText += `  #${i+1}: ${target.icon} ${target.type} (${Math.floor(total)} units)\n`;
            lastPos = target.position;
        }

        infoText += `📊 Total: ${Math.floor(totalTime)} units`;

        const centerX = this.scene.cameras.main.width / 2;
        const centerY = this.scene.cameras.main.height / 2 - 80;

        this.infoText = this.scene.add.text(centerX, centerY, infoText, {
            fontSize: '16px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            backgroundColor: '#1a1a2e',
            padding: { x: 15, y: 10 },
            align: 'center',
            stroke: '#000000',
            strokeThickness: 3
        }).setOrigin(0.5).setDepth(100);

        // ✅ ตรวจสอบเวลา
        const timeSystem = this.scene.scene.get('GameScene') as any;
        if (timeSystem && timeSystem.timeSystem) {
            const remaining = timeSystem.timeSystem.getRemainingTime();
            if (totalTime > remaining) {
                const warning = this.scene.add.text(
                    centerX,
                    centerY + 100,
                    `⚠️ NOT ENOUGH TIME! (Need ${Math.floor(totalTime)}, Have ${Math.floor(remaining)})`,
                    {
                        fontSize: '16px',
                        color: '#ff6b6b',
                        fontFamily: 'monospace',
                        backgroundColor: '#1a1a2e',
                        padding: { x: 10, y: 5 },
                        stroke: '#000000',
                        strokeThickness: 3
                    }
                ).setOrigin(0.5).setDepth(101);
                
                this.scene.time.delayedCall(3000, () => {
                    warning.destroy();
                });
            }
        }
    }

    private animateLine(from: { x: number; y: number }, to: { x: number; y: number }): void {
        // Create moving dot along the line
        const dot = this.scene.add.circle(from.x, from.y, 4, 0x4ecdc4);
        
        this.timer = this.scene.time.addEvent({
            delay: 50,
            callback: () => {
                if (!dot.active) return;
                const progress = (Date.now() % 2000) / 2000;
                const x = from.x + (to.x - from.x) * progress;
                const y = from.y + (to.y - from.y) * progress;
                dot.x = x;
                dot.y = y;
            },
            loop: true
        });

        // Store dot for cleanup
        (this as any).dot = dot;
    }

    private getDistance(pos1: { x: number; y: number }, pos2: { x: number; y: number }): number {
        return Math.sqrt(Math.pow(pos1.x - pos2.x, 2) + Math.pow(pos1.y - pos2.y, 2));
    }

    clear(): void {
        if (this.line) {
            this.line.destroy();
            this.line = null;
        }
        if (this.infoText) {
            this.infoText.destroy();
            this.infoText = null;
        }
        if (this.timer) {
            this.timer.remove();
            this.timer = null;
        }
        if ((this as any).dot) {
            (this as any).dot.destroy();
            (this as any).dot = null;
        }
    }

    update(): void {
        // Update info text position if needed
    }
}