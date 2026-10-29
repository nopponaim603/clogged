// src/scenes/PauseScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Systems } from '../main';

export class PauseScene extends Scene {
    private container!: Phaser.GameObjects.Container;
    
    constructor() {
        super('PauseScene');
    }
    
    create(): void {
        // ✅ Background
        this.cameras.main.setBackgroundColor('#0a0a15');
        
        // ✅ Overlay
        const overlay = this.add.graphics();
        overlay.fillStyle(0x000000, 0.85);
        overlay.fillRect(0, 0, GAME_CONFIG.WIDTH, GAME_CONFIG.HEIGHT);
        
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT / 2;
        
        // ✅ Title
        this.add.text(centerX, centerY - 200, '⏸️ PAUSED', {
            fontSize: '56px',
            color: '#ffffff',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        }).setOrigin(0.5);
        
        // ✅ Info
        const state = GameState;
        const time = state.getTime();
        
        this.add.text(centerX, centerY - 120, 
            `Day ${time.gameDay} - ${this.getTimeString()}`, {
            fontSize: '24px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
        }).setOrigin(0.5);
        
        // ✅ Buttons
        this.createButton(centerX, centerY - 20, '▶ RESUME', () => this.resume());
        this.createButton(centerX, centerY + 50, '💾 SAVE', () => this.save());
        this.createButton(centerX, centerY + 120, '❓ HELP', () => this.showHelp());
        this.createButton(centerX, centerY + 190, '🏠 MAIN MENU', () => this.exitToMenu());
        
        // ✅ ESC to resume
        this.input.keyboard?.on('keydown-ESC', () => this.resume());
        this.input.keyboard?.on('keydown-P', () => this.resume());
    }
    
    private createButton(x: number, y: number, label: string, callback: () => void): void {
        const btn = this.add.text(x, y, label, {
            fontSize: '24px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 30, y: 15 },
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
    
    // ============================================================
    // ACTIONS
    // ============================================================
    private resume(): void {
        // ✅ Resume Time
        Systems.time.resume();
        
        // ✅ Resume WorldScene
        this.scene.resume('WorldScene');
        
        // ✅ Stop PauseScene
        this.scene.stop();
    }
    
    private save(): void {
        const success = Systems.save.save();
        
        if (success) {
            this.showNotification('✅ Game Saved!', '#00b894');
        } else {
            this.showNotification('❌ Save Failed!', '#ff6b6b');
        }
    }
    
    private showHelp(): void {
        // TODO: show help overlay
        this.showNotification('❓ Help coming soon...', '#f9ca24');
    }
    
    private exitToMenu(): void {
        // ✅ Stop all scenes
        this.scene.stop('WorldScene');
        this.scene.stop('DefenseScene');
        this.scene.stop('BaseInteriorScene');
        this.scene.stop('PauseScene');
        
        // ✅ Start Menu
        this.scene.start('MenuScene');
    }
    
    // ============================================================
    // NOTIFICATION
    // ============================================================
    private showNotification(message: string, color: string): void {
        const centerX = GAME_CONFIG.WIDTH / 2;
        const centerY = GAME_CONFIG.HEIGHT - 100;
        
        const text = this.add.text(centerX, centerY, message, {
            fontSize: '24px',
            color,
            fontFamily: 'monospace',
            backgroundColor: '#1a1a2e',
            padding: { x: 20, y: 10 },
        }).setOrigin(0.5);
        
        this.tweens.add({
            targets: text,
            y: text.y - 50,
            alpha: 0,
            duration: 2000,
            onComplete: () => text.destroy(),
        });
    }
    
    private getTimeString(): string {
        const time = GameState.getTime();
        const hh = String(time.gameHour).padStart(2, '0');
        const mm = String(time.gameMinute).padStart(2, '0');
        return `${hh}:${mm}`;
    }
}