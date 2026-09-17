// src/scenes/GameOverScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { Systems } from '../main';

export class GameOverScene extends Scene {
    private message: string = '';
    private isWin: boolean = false;
    private day: number = 1;
    private prestigeEarned: number = 0;
    
    constructor() {
        super('GameOverScene');
    }
    
    init(data: any): void {
        this.message = data.message || '';
        this.isWin = data.isWin || false;
        this.day = data.day || 1;
        
        const gameOver = GameState.getData().gameOver;
        this.prestigeEarned = gameOver?.prestigeEarned || 0;
    }
    
    create(): void {
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        
        // ✅ Background
        this.cameras.main.setBackgroundColor(this.isWin ? '#0a3d0a' : '#3d0a0a');
        
        // ✅ Decorative Arcs
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
        
        // ✅ Title
        this.add.text(centerX, centerY - 180, 
            this.isWin ? '🎉 VICTORY!' : '💀 GAME OVER',
            {
                fontSize: '64px',
                color: this.isWin ? '#4ecdc4' : '#ff6b6b',
                fontFamily: 'monospace',
                fontStyle: 'bold',
                stroke: '#000000',
                strokeThickness: 8,
            }
        ).setOrigin(0.5);
        
        // ✅ Reason
        this.add.text(centerX, centerY - 100, this.message, {
            fontSize: '28px',
            color: '#ffffff',
            fontFamily: 'monospace',
        }).setOrigin(0.5);
        
        // ✅ Stats
        this.add.text(centerX, centerY - 40, 
            `Survived: ${this.day} days`,
            {
                fontSize: '24px',
                color: '#b2bec3',
                fontFamily: 'monospace',
            }
        ).setOrigin(0.5);
        
        // ✅ Prestige
        this.add.text(centerX, centerY + 20, 
            `⭐ Prestige Earned: +${this.prestigeEarned}`,
            {
                fontSize: '28px',
                color: '#f9ca24',
                fontFamily: 'monospace',
                fontStyle: 'bold',
            }
        ).setOrigin(0.5);
        
        // ✅ Total Prestige
        const totalPrestige = Systems.prestige.getAvailablePoints();
        this.add.text(centerX, centerY + 60, 
            `Total Available: ${totalPrestige} ⭐`,
            {
                fontSize: '18px',
                color: '#636e72',
                fontFamily: 'monospace',
            }
        ).setOrigin(0.5);
        
        // ✅ Buttons
        this.createButton(centerX, centerY + 130, '🔄 PLAY AGAIN', () => this.playAgain());
        this.createButton(centerX, centerY + 190, '⭐ PRESTIGE TREE', () => this.showPrestige());
        this.createButton(centerX, centerY + 250, '🏠 MAIN MENU', () => this.mainMenu());
    }
    
    private createButton(x: number, y: number, label: string, callback: () => void): void {
        const btn = this.add.text(x, y, label, {
            fontSize: '22px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 25, y: 12 },
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        
        btn.on('pointerover', () => {
            btn.setStyle({ color: '#ffffff' });
            btn.setScale(1.05);
        });
        
        btn.on('pointerout', () => {
            btn.setStyle({ color: '#4ecdc4' });
            btn.setScale(1);
        });
        
        btn.on('pointerdown', callback);
    }
    
    private playAgain(): void {
        // ✅ Reset GameState
        GameState.reset();
        
        // ✅ Apply Prestige Effects
        Systems.prestige.applyEffects();
        
        // ✅ Re-init systems
        const seed = GameState.getData().seed;
        Systems.map.init(seed);
        Systems.baseMovement.init(this);
        Systems.crew.init();
        Systems.vehicle.init();
        Systems.raid.init();
        Systems.base.init();
        Systems.economy.init();
        
        // ✅ Start
        this.scene.start('WorldScene');
    }
    
    private showPrestige(): void {
        this.scene.start('PrestigeScene');
    }
    
    private mainMenu(): void {
        this.scene.start('MenuScene');
    }
}