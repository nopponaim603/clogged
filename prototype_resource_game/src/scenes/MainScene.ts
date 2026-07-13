// src/scenes/MenuScene.ts
import { Scene } from 'phaser';

export class MenuScene extends Scene {
    constructor() {
        super('MenuScene');
    }

    create(): void {
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2;

        // Title
        this.add.text(centerX, centerY - 100, '🏚️ SURVIVAL BASE', {
            fontSize: '48px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            stroke: '#000000',
            strokeThickness: 6
        }).setOrigin(0.5);

        this.add.text(centerX, centerY - 40, 'Survive 30 Days in the Wasteland', {
            fontSize: '20px',
            color: '#b2bec3',
            fontFamily: 'monospace'
        }).setOrigin(0.5);

        // Start button
        const startBtn = this.add.text(centerX, centerY + 60, '▶ START GAME', {
            fontSize: '28px',
            color: '#00b894',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 30, y: 15 }
        }).setOrigin(0.5).setInteractive();

        startBtn.on('pointerdown', () => {
            this.scene.start('GameScene');
        });

        startBtn.on('pointerover', () => {
            startBtn.setStyle({ color: '#ffffff' });
        });

        startBtn.on('pointerout', () => {
            startBtn.setStyle({ color: '#00b894' });
        });

        // Instructions
        this.add.text(centerX, centerY + 160, '🎯 Send crew to gather resources, search relics, and hunt monsters', {
            fontSize: '14px',
            color: '#636e72',
            fontFamily: 'monospace'
        }).setOrigin(0.5);

        this.add.text(centerX, centerY + 185, '🌙 Defend your base at night from monster attacks', {
            fontSize: '14px',
            color: '#636e72',
            fontFamily: 'monospace'
        }).setOrigin(0.5);

        // Version
        this.add.text(centerX, centerY + 280, 'v1.0 - Prototype', {
            fontSize: '12px',
            color: '#2d3436',
            fontFamily: 'monospace'
        }).setOrigin(0.5);
    }
}