// src/ui/NotificationSystem.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';

interface Notification {
    id: number;
    text: Phaser.GameObjects.Text;
    bg: Phaser.GameObjects.Graphics;
    targetY: number;
}

export class NotificationSystem {
    private scene: Scene;
    private notifications: Notification[] = [];
    private nextId: number = 1;
    
    private readonly MAX_NOTIFICATIONS = 5;
    private readonly NOTIFICATION_X = GAME_CONFIG.WIDTH - 320;
    private readonly NOTIFICATION_START_Y = 80;
    private readonly NOTIFICATION_WIDTH = 300;
    private readonly NOTIFICATION_HEIGHT = 40;
    private readonly NOTIFICATION_SPACING = 45;
    
    constructor(scene: Scene) {
        this.scene = scene;
    }
    
    // ============================================================
    // SHOW
    // ============================================================
    show(message: string, duration: number = 3000, color: string = '#4ecdc4'): void {
        const id = this.nextId++;
        const y = this.NOTIFICATION_START_Y + this.notifications.length * this.NOTIFICATION_SPACING;
        
        // ✅ Background
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.95);
        bg.fillRoundedRect(
            this.NOTIFICATION_X,
            y,
            this.NOTIFICATION_WIDTH,
            this.NOTIFICATION_HEIGHT,
            8
        );
        bg.lineStyle(2, this.hexToNumber(color));
        bg.strokeRoundedRect(
            this.NOTIFICATION_X,
            y,
            this.NOTIFICATION_WIDTH,
            this.NOTIFICATION_HEIGHT,
            8
        );
        bg.setScrollFactor(0);
        bg.setDepth(2000);
        
        // ✅ Text
        const text = this.scene.add.text(
            this.NOTIFICATION_X + this.NOTIFICATION_WIDTH / 2,
            y + this.NOTIFICATION_HEIGHT / 2,
            message,
            {
                fontSize: '14px',
                color,
                fontFamily: 'monospace',
                align: 'center',
                wordWrap: { width: this.NOTIFICATION_WIDTH - 20 },
            }
        ).setOrigin(0.5);
        text.setScrollFactor(0);
        text.setDepth(2001);
        
        // ✅ Slide In
        bg.setAlpha(0);
        text.setAlpha(0);
        
        this.scene.tweens.add({
            targets: [bg, text],
            alpha: 1,
            duration: 200,
        });
        
        const notification: Notification = {
            id,
            text,
            bg,
            targetY: y,
        };
        
        this.notifications.push(notification);
        
        // ✅ Remove after duration
        this.scene.time.delayedCall(duration, () => {
            this.remove(id);
        });
        
        // ✅ Limit
        while (this.notifications.length > this.MAX_NOTIFICATIONS) {
            const oldest = this.notifications[0];
            this.remove(oldest.id);
        }
    }
    
    // ============================================================
    // REMOVE
    // ============================================================
    private remove(id: number): void {
        const index = this.notifications.findIndex(n => n.id === id);
        if (index === -1) return;
        
        const notification = this.notifications[index];
        
        // ✅ Slide Out
        this.scene.tweens.add({
            targets: [notification.bg, notification.text],
            alpha: 0,
            x: GAME_CONFIG.WIDTH + 100,
            duration: 200,
            onComplete: () => {
                notification.bg.destroy();
                notification.text.destroy();
            },
        });
        
        this.notifications.splice(index, 1);
        
        // ✅ Reposition
        this.reposition();
    }
    
    private reposition(): void {
        this.notifications.forEach((n, index) => {
            const targetY = this.NOTIFICATION_START_Y + index * this.NOTIFICATION_SPACING;
            
            this.scene.tweens.add({
                targets: [n.bg, n.text],
                y: targetY - n.targetY + n.bg.y,
                duration: 200,
            });
            
            n.targetY = targetY;
        });
    }
    
    // ============================================================
    // HELPERS
    // ============================================================
    private hexToNumber(hex: string): number {
        return parseInt(hex.replace('#', ''), 16);
    }
    
    // ============================================================
    // PRESETS
    // ============================================================
    showInfo(message: string, duration: number = 3000): void {
        this.show(message, duration, '#4ecdc4');
    }
    
    showSuccess(message: string, duration: number = 3000): void {
        this.show('✅ ' + message, duration, '#00b894');
    }
    
    showError(message: string, duration: number = 3000): void {
        this.show('❌ ' + message, duration, '#ff6b6b');
    }
    
    showWarning(message: string, duration: number = 3000): void {
        this.show('⚠️ ' + message, duration, '#f9ca24');
    }
    
    showRaid(message: string, duration: number = 5000): void {
        this.show('⚔️ ' + message, duration, '#e74c3c');
    }
    
    showFuel(message: string, duration: number = 4000): void {
        this.show('⛽ ' + message, duration, '#f39c12');
    }
    
    // ============================================================
    // CLEAR
    // ============================================================
    clearAll(): void {
        this.notifications.forEach(n => {
            n.bg.destroy();
            n.text.destroy();
        });
        this.notifications = [];
    }
}