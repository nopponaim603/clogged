// src/ui/NotificationSystem.ts
import { Scene } from 'phaser';

export class NotificationSystem {
    private scene: Scene;
    private notifications: Phaser.GameObjects.Text[] = [];
    private maxNotifications: number = 5;
    private notificationY: number = 100;

    constructor(scene: Scene) {
        this.scene = scene;
    }

    show(message: string, duration: number = 3000, color: string = '#4ecdc4'): void {
        const text = this.scene.add.text(
            this.scene.cameras.main.width / 2,
            this.notificationY,
            message,
            {
                fontSize: '20px',
                color: color,
                fontFamily: 'monospace',
                backgroundColor: '#1a1a2e',
                padding: { x: 20, y: 10 },
                stroke: '#000000',
                strokeThickness: 3
            }
        ).setOrigin(0.5).setDepth(1000);

        this.notifications.push(text);

        // Limit notifications
        if (this.notifications.length > this.maxNotifications) {
            const old = this.notifications.shift();
            if (old) old.destroy();
        }

        // Auto remove
        this.scene.time.delayedCall(duration, () => {
            text.destroy();
            this.notifications = this.notifications.filter(n => n !== text);
        });

        // Slide up animation
        this.scene.tweens.add({
            targets: text,
            y: text.y - 30,
            alpha: { from: 0, to: 1 },
            duration: 300,
            ease: 'Power2'
        });
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

    showInfo(message: string, duration: number = 3000): void {
        this.show('ℹ️ ' + message, duration, '#4ecdc4');
    }

    clearAll(): void {
        this.notifications.forEach(n => n.destroy());
        this.notifications = [];
    }
}