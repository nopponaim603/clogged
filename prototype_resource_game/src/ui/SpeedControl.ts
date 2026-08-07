// src/ui/SpeedControl.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';

export class SpeedControl {
    private scene: Scene;
    private speed: number = 1;
    private speedText!: Phaser.GameObjects.Text;
    private onSpeedChange: (speed: number) => void;
    private onSkip: () => void;
    private buttons: Phaser.GameObjects.Text[] = [];
    private container: Phaser.GameObjects.Container;

    constructor(
        scene: Scene,
        x: number,
        y: number,
        onSpeedChange: (speed: number) => void,
        onSkip: () => void
    ) {
        this.scene = scene;
        this.onSpeedChange = onSpeedChange;
        this.onSkip = onSkip;
        this.speed = GAME_CONFIG.DEFAULT_SPEED || 1;
        this.container = this.scene.add.container(x, y);
        this.createUI();
    }

    private createUI(): void {
        const padding = 10;
        const spacing = 50;
        let currentX = 0;

        // ✅ Background
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.8);
        bg.fillRoundedRect(0, -15, 250, 40, 8);
        bg.lineStyle(1, 0x4a4a5a);
        bg.strokeRoundedRect(0, -15, 250, 40, 8);
        this.container.add(bg);

        // ✅ Label
        const label = this.scene.add.text(5, -8, '⏱️', {
            fontSize: '18px',
            color: '#b2bec3',
            fontFamily: 'monospace'
        });
        this.container.add(label);

        // ✅ Speed buttons
        const speeds = GAME_CONFIG.SPEED_OPTIONS || [1, 2, 4];
        const labels = ['1x', '2x', '4x'];

        speeds.forEach((speed, index) => {
            const xPos = 35 + index * spacing;
            const isActive = speed === this.speed;
            
            const btn = this.scene.add.text(xPos, -5, labels[index], {
                fontSize: '16px',
                color: isActive ? '#f9ca24' : '#b2bec3',
                fontFamily: 'monospace',
                backgroundColor: isActive ? '#2d3436' : 'transparent',
                padding: { x: 6, y: 2 }
            });
            
            btn.setInteractive();
            btn.on('pointerdown', () => {
                this.setSpeed(speed);
            });
            btn.on('pointerover', () => {
                if (speed !== this.speed) {
                    btn.setStyle({ color: '#ffffff' });
                }
            });
            btn.on('pointerout', () => {
                if (speed !== this.speed) {
                    btn.setStyle({ color: '#b2bec3' });
                }
            });
            
            this.container.add(btn);
            this.buttons.push(btn);
        });

        // ✅ Skip button
        const skipBtn = this.scene.add.text(35 + speeds.length * spacing + 10, -5, '⏭️ SKIP', {
            fontSize: '16px',
            color: '#ff6b6b',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 8, y: 2 }
        });
        skipBtn.setInteractive();
        skipBtn.on('pointerdown', () => {
            this.onSkip();
        });
        skipBtn.on('pointerover', () => {
            skipBtn.setStyle({ color: '#ffffff' });
        });
        skipBtn.on('pointerout', () => {
            skipBtn.setStyle({ color: '#ff6b6b' });
        });
        this.container.add(skipBtn);

        // ✅ Store reference
        (this as any).skipBtn = skipBtn;
        (this as any).speedButtons = this.buttons;
    }

    setSpeed(speed: number): void {
        this.speed = speed;
        this.onSpeedChange(speed);
        
        // ✅ Update button colors
        this.buttons.forEach((btn, index) => {
            const speeds = GAME_CONFIG.SPEED_OPTIONS || [1, 2, 4];
            const isActive = speeds[index] === speed;
            btn.setStyle({ 
                color: isActive ? '#f9ca24' : '#b2bec3',
                backgroundColor: isActive ? '#2d3436' : 'transparent'
            });
        });
    }

    setVisible(visible: boolean): void {
        this.container.setVisible(visible);
    }

    getSpeed(): number {
        return this.speed;
    }
}