// src/scenes/GameOverScene.ts
import { Scene } from 'phaser';

export class GameOverScene extends Scene {
    private message: string = '';
    private isWin: boolean = false;
    private day: number = 1;

    constructor() {
        super('GameOverScene');
    }

    init(data: { message: string; isWin: boolean; day: number }): void {
        this.message = data.message;
        this.isWin = data.isWin;
        this.day = data.day;
    }

    create(): void {
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2;

        this.cameras.main.setBackgroundColor(this.isWin ? '#0a3d0a' : '#3d0a0a');

        // Decorative arcs
        for (let i = 0; i < 3; i++) {
            const arc = this.add.arc(
                centerX + (i - 1) * 150,
                centerY,
                100 + i * 30,
                0,
                360,
                false,
                this.isWin ? 0x4ecdc4 : 0xff6b6b,
                0.05
            );
            arc.setStrokeStyle(2, this.isWin ? 0x4ecdc4 : 0xff6b6b, 0.3);
        }

        // Title
        const title = this.add.text(centerX, centerY - 100, 
            this.isWin ? '🎉 VICTORY!' : '💀 GAME OVER',
            {
                fontSize: '56px',
                color: this.isWin ? '#4ecdc4' : '#ff6b6b',
                fontFamily: 'monospace',
                stroke: '#000000',
                strokeThickness: 6
            }
        ).setOrigin(0.5);

        // Message
        this.add.text(centerX, centerY - 30, this.message, {
            fontSize: '28px',
            color: '#ffffff',
            fontFamily: 'monospace'
        }).setOrigin(0.5);

        // Stats
        this.add.text(centerX, centerY + 30, 
            `Survived: ${this.day} days`,
            {
                fontSize: '20px',
                color: '#b2bec3',
                fontFamily: 'monospace'
            }
        ).setOrigin(0.5);

        // Restart button
        const restartBtn = this.add.text(centerX, centerY + 120, '🔄 RESTART', {
            fontSize: '24px',
            color: '#f9ca24',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 20, y: 10 }
        }).setOrigin(0.5).setInteractive();

        restartBtn.on('pointerdown', () => {
            this.cameras.main.fadeOut(500, 0, 0, 0);
            this.cameras.main.once('camerafadeoutcomplete', () => {
                this.scene.start('GameScene');
            });
        });

        restartBtn.on('pointerover', () => {
            restartBtn.setStyle({ color: '#ffffff' });
            restartBtn.setScale(1.05);
        });

        restartBtn.on('pointerout', () => {
            restartBtn.setStyle({ color: '#f9ca24' });
            restartBtn.setScale(1);
        });
    }
}