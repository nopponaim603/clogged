// src/scenes/PrestigeScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { Systems } from '../main';

export class PrestigeScene extends Scene {
    private container!: Phaser.GameObjects.Container;
    
    constructor() {
        super('PrestigeScene');
    }
    
    create(): void {
        // ✅ Background
        this.cameras.main.setBackgroundColor('#0a0a15');
        
        // ✅ Title
        const centerX = GAME_CONFIG.WIDTH / 2;
        
        this.add.text(centerX, 40, '⭐ PRESTIGE TREE', {
            fontSize: '40px',
            color: '#f9ca24',
            fontFamily: 'monospace',
            fontStyle: 'bold',
            stroke: '#000000',
            strokeThickness: 6,
        }).setOrigin(0.5);
        
        // ✅ Available Points
        const available = Systems.prestige.getAvailablePoints();
        this.add.text(centerX, 90, 
            `Available Points: ${available} ⭐`,
            {
                fontSize: '24px',
                color: available > 0 ? '#00b894' : '#b2bec3',
                fontFamily: 'monospace',
            }
        ).setOrigin(0.5);
        
        // ✅ Stats
        const stats = Systems.prestige.getStats();
        this.add.text(20, 20, 
            `Runs: ${stats.totalRuns}  |  Best Day: ${stats.bestDay}  |  Wins: ${stats.totalWins}  |  Losses: ${stats.totalLosses}`,
            {
                fontSize: '14px',
                color: '#636e72',
                fontFamily: 'monospace',
            }
        );
        
        // ✅ Upgrades List
        this.renderUpgrades();
        
        // ✅ Close Button
        this.createCloseButton();
        
        // ✅ Reset Button
        this.createResetButton();
        
        // ✅ ESC to close
        this.input.keyboard?.on('keydown-ESC', () => this.close());
    }
    
    // ============================================================
    // RENDER UPGRADES
    // ============================================================
    private renderUpgrades(): void {
        const upgrades = Systems.prestige.getUpgrades();
        const startY = 140;
        const rowHeight = 50;
        const centerX = GAME_CONFIG.WIDTH / 2;
        
        upgrades.forEach((upgrade, index) => {
            const y = startY + index * rowHeight;
            
            this.renderUpgradeRow(upgrade, centerX, y);
        });
    }
    
    private renderUpgradeRow(upgrade: any, centerX: number, y: number): void {
        const level = Systems.prestige.getUpgradeLevel(upgrade.id);
        const maxLevel = upgrade.maxLevel;
        const cost = Systems.prestige.getUpgradeCost(upgrade.id);
        const canPurchase = Systems.prestige.canPurchaseUpgrade(upgrade.id);
        const isMaxed = level >= maxLevel;
        
        // ✅ Background
        const bg = this.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.8);
        bg.fillRoundedRect(centerX - 500, y - 20, 1000, 45, 8);
        bg.lineStyle(1, canPurchase ? 0x00b894 : 0x4a4a5a);
        bg.strokeRoundedRect(centerX - 500, y - 20, 1000, 45, 8);
        
        // ✅ Icon
        this.add.text(centerX - 470, y, upgrade.icon, {
            fontSize: '24px',
        });
        
        // ✅ Name
        this.add.text(centerX - 430, y - 12, upgrade.name, {
            fontSize: '16px',
            color: '#ffffff',
            fontFamily: 'monospace',
        });
        
        // ✅ Description
        this.add.text(centerX - 430, y + 5, upgrade.description, {
            fontSize: '12px',
            color: '#b2bec3',
            fontFamily: 'monospace',
        });
        
        // ✅ Level
        const levelText = isMaxed 
            ? `MAX (${level}/${maxLevel})`
            : `Lv.${level}/${maxLevel}`;
        
        this.add.text(centerX + 100, y, levelText, {
            fontSize: '16px',
            color: isMaxed ? '#f9ca24' : '#4ecdc4',
            fontFamily: 'monospace',
        });
        
        // ✅ Effect
        const effectText = upgrade.getEffectText(level);
        this.add.text(centerX + 100, y + 18, effectText, {
            fontSize: '12px',
            color: '#636e72',
            fontFamily: 'monospace',
        });
        
        // ✅ Cost / Button
        if (isMaxed) {
            this.add.text(centerX + 350, y, '✅ MAXED', {
                fontSize: '18px',
                color: '#f9ca24',
                fontFamily: 'monospace',
                fontStyle: 'bold',
            });
        } else {
            const btn = this.add.text(centerX + 350, y, 
                `⭐ ${cost}`, {
                fontSize: '18px',
                color: canPurchase ? '#00b894' : '#636e72',
                fontFamily: 'monospace',
                backgroundColor: canPurchase ? '#2d3436' : '#1a1a2e',
                padding: { x: 15, y: 5 },
            }).setInteractive({ useHandCursor: canPurchase });
            
            if (canPurchase) {
                btn.on('pointerover', () => {
                    btn.setStyle({ color: '#ffffff' });
                });
                btn.on('pointerout', () => {
                    btn.setStyle({ color: '#00b894' });
                });
                btn.on('pointerdown', () => {
                    this.purchaseUpgrade(upgrade.id);
                });
            }
        }
    }
    
    private purchaseUpgrade(upgradeId: string): void {
        const success = Systems.prestige.purchaseUpgrade(upgradeId);
        
        if (success) {
            // ✅ Refresh
            this.scene.restart();
        }
    }
    
    // ============================================================
    // CLOSE
    // ============================================================
    private createCloseButton(): void {
        const closeBtn = this.add.text(
            GAME_CONFIG.WIDTH - 60,
            30,
            '✕',
            {
                fontSize: '32px',
                color: '#ff6b6b',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 15, y: 10 },
            }
        ).setOrigin(0.5).setInteractive();
        
        closeBtn.on('pointerover', () => closeBtn.setStyle({ color: '#ffffff' }));
        closeBtn.on('pointerout', () => closeBtn.setStyle({ color: '#ff6b6b' }));
        closeBtn.on('pointerdown', () => this.close());
    }
    
    private close(): void {
        this.scene.stop();
        this.scene.start('MenuScene');
    }
    
    // ============================================================
    // RESET
    // ============================================================
    private createResetButton(): void {
        const resetBtn = this.add.text(
            20,
            GAME_CONFIG.HEIGHT - 40,
            '🗑️ Reset All',
            {
                fontSize: '14px',
                color: '#ff6b6b',
                fontFamily: 'monospace',
            }
        ).setInteractive({ useHandCursor: true });
        
        resetBtn.on('pointerover', () => resetBtn.setStyle({ color: '#ffffff' }));
        resetBtn.on('pointerout', () => resetBtn.setStyle({ color: '#ff6b6b' }));
        resetBtn.on('pointerdown', () => {
            if (confirm('Reset ALL prestige progress? This cannot be undone!')) {
                Systems.prestige.resetAll();
                this.scene.restart();
            }
        });
    }
}