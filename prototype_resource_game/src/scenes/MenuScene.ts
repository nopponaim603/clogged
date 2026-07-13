// src/scenes/MenuScene.ts
import { Scene } from 'phaser';

export class MenuScene extends Scene {
    constructor() {
        super('MenuScene');
    }

    create(): void {
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2;

        // Background
        this.cameras.main.setBackgroundColor('#1a1a2e');

        // Decorative arcs
        for (let i = 0; i < 3; i++) {
            const arc = this.add.arc(
                centerX + (i - 1) * 200,
                centerY + 100,
                150 + i * 30,
                0,
                360,
                false,
                0x4ecdc4,
                0.05
            );
            arc.setStrokeStyle(2, 0x4ecdc4, 0.3);
        }

        // Title
        this.add.text(centerX, centerY - 120, '🏚️ SURVIVAL BASE', {
            fontSize: '52px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            stroke: '#000000',
            strokeThickness: 8
        }).setOrigin(0.5);

        this.add.text(centerX, centerY - 50, 'Survive 30 Days in the Wasteland', {
            fontSize: '20px',
            color: '#b2bec3',
            fontFamily: 'monospace'
        }).setOrigin(0.5);

        // Decorative line
        const line = this.add.graphics();
        line.lineStyle(2, 0x4ecdc4, 0.5);
        line.beginPath();
        line.moveTo(centerX - 200, centerY - 10);
        line.lineTo(centerX + 200, centerY - 10);
        line.strokePath();

        // Features
        const features = [
            '🎯 Send crew to gather resources',
            '🏛️ Search for ancient relics',
            '⚔️ Hunt dangerous monsters',
            '🌙 Defend your base at night'
        ];

        features.forEach((text, index) => {
            this.add.text(centerX - 150, centerY + 30 + index * 30, text, {
                fontSize: '16px',
                color: '#b2bec3',
                fontFamily: 'monospace'
            });
        });

        // Start button
        const startBtn = this.add.text(centerX, centerY + 200, '▶ START GAME', {
            fontSize: '28px',
            color: '#00b894',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 30, y: 15 }
        }).setOrigin(0.5).setInteractive();

        // Button hover effects
        startBtn.on('pointerover', () => {
            startBtn.setStyle({ color: '#ffffff' });
            startBtn.setScale(1.05);
        });

        startBtn.on('pointerout', () => {
            startBtn.setStyle({ color: '#00b894' });
            startBtn.setScale(1);
        });

        startBtn.on('pointerdown', () => {
            this.cameras.main.fadeOut(500, 0, 0, 0);
            this.cameras.main.once('camerafadeoutcomplete', () => {
                this.scene.start('GameScene');
            });
        });

        // Version
        this.add.text(centerX, centerY + 280, 'v1.0 - Prototype', {
            fontSize: '12px',
            color: '#2d3436',
            fontFamily: 'monospace'
        }).setOrigin(0.5);
    }
}