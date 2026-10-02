// src/scenes/MenuScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';  // ✅ import
import { Systems } from '../main';

export class MenuScene extends Scene {
    constructor() {
        super('MenuScene');
    }
    
    create(): void {
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        
        // ✅ Background (สว่างขึ้น)
        this.cameras.main.setBackgroundColor(GAME_CONFIG.COLORS.BG_DARK);
        
        // ✅ Decorative Arcs
        for (let i = 0; i < 3; i++) {
            const arc = this.add.arc(
                centerX + (i - 1) * 200,
                centerY + 100,
                150 + i * 30,
                0,
                360,
                false,
                0x00e5cc,
                0.05
            );
            arc.setStrokeStyle(2, 0x00e5cc, 0.3);
        }
        
        // ✅ Title (ชื่อใหม่ CLOGGED)
        this.add.text(centerX, centerY - 200, `🚂 ${GAME_CONFIG.GAME_NAME}`, {
            fontSize: '72px',
            color: '#00e5cc',
            fontFamily: 'monospace',
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 10,
        }).setOrigin(0.5);
        
        this.add.text(centerX, centerY - 120, GAME_CONFIG.GAME_TAGLINE, {
            fontSize: '20px',
            color: '#b0c4de',  // ✅ สว่างขึ้น
            fontFamily: 'monospace',
            fontStyle: 'italic',
        }).setOrigin(0.5);
        
        // ✅ Prestige Points
        const prestigePoints = Systems.prestige.getAvailablePoints();
        if (prestigePoints > 0) {
            this.add.text(centerX, centerY - 80, 
                `⭐ ${prestigePoints} Prestige Points Available`, {
                fontSize: '16px',
                color: '#ffd93d',
                fontFamily: 'monospace',
            }).setOrigin(0.5);
        }
        
        // ✅ Buttons
        let btnY = centerY - 10;
        
        this.createButton(centerX, btnY, '▶ NEW GAME', () => this.newGame());
        btnY += 70;
        
        if (Systems.save.hasSave()) {
            const info = Systems.save.getSaveInfo();
            const label = info 
                ? `💾 CONTINUE (Day ${info.day})`
                : '💾 CONTINUE';
            this.createButton(centerX, btnY, label, () => this.continueGame());
            btnY += 70;
        }
        
        this.createButton(centerX, btnY, '⭐ PRESTIGE TREE', () => this.showPrestige());
        btnY += 70;
        
        // ✅ Stats
        const stats = Systems.prestige.getStats();
        if (stats.totalRuns > 0) {
            this.add.text(centerX, GAME_CONFIG.HEIGHT - 80, 
                `📊 Runs: ${stats.totalRuns} | Best: Day ${stats.bestDay} | Wins: ${stats.totalWins}`, {
                fontSize: '14px',
                color: '#8a9aab',  // ✅ สว่างขึ้น
                fontFamily: 'monospace',
            }).setOrigin(0.5);
        }
        
        // ✅ Version
        this.add.text(centerX, GAME_CONFIG.HEIGHT - 40, 'CLOGGED v1.0 - Prototype', {
            fontSize: '12px',
            color: '#5a6a7a',
            fontFamily: 'monospace',
        }).setOrigin(0.5);
        
        // ✅ Input
        this.input.keyboard?.on('keydown-ENTER', () => this.newGame());
    }
    
    private createButton(x: number, y: number, label: string, callback: () => void): void {
        const btn = this.add.text(x, y, label, {
            fontSize: '24px',
            color: '#00e5cc',  // ✅
            fontFamily: 'monospace',
            fontStyle: 'bold',
            backgroundColor: '#243040',  // ✅ สว่างขึ้น
            padding: { x: 30, y: 15 },
        }).setOrigin(0.5).setInteractive({ useHandCursor: true });
        
        btn.on('pointerover', () => {
            btn.setStyle({ 
                color: '#ffffff',
                backgroundColor: '#2f3d4f',  // ✅
            });
            btn.setScale(1.05);
        });
        
        btn.on('pointerout', () => {
            btn.setStyle({ 
                color: '#00e5cc',
                backgroundColor: '#243040',
            });
            btn.setScale(1);
        });
        
        btn.on('pointerdown', callback);
    }
    
    private newGame(): void {
        // ✅ Reset GameState
        GameState.reset();  // ✅ ใช้ได้แล้ว
        
        // ✅ Apply Prestige Effects
        Systems.prestige.applyEffects();
        
        // ✅ Re-init systems
        const seed = GameState.getData().seed;  // ✅ ใช้ได้แล้ว
        Systems.map.init(seed);
        Systems.baseMovement.init(this);
        Systems.crew.init();
        Systems.vehicle.init();
        Systems.raid.init();
        Systems.base.init();
        Systems.economy.init();
        
        // ✅ Start Game
        this.scene.start('WorldScene');
    }
    
    private continueGame(): void {
        const success = Systems.save.load();
        
        if (success) {
            this.scene.start('WorldScene');
        } else {
            console.error('❌ Load failed');
        }
    }
    
    private showPrestige(): void {
        this.scene.start('PrestigeScene');
    }
}