// src/scenes/BootScene.ts
import { Scene } from 'phaser';

export class BootScene extends Scene {
    constructor() {
        super('BootScene');
    }

    preload(): void {
        this.cameras.main.setBackgroundColor('#1a1a2e');
        
        const loadingText = this.add.text(
            this.cameras.main.width / 2,
            this.cameras.main.height / 2,
            'Loading...',
            {
                fontSize: '32px',
                color: '#4ecdc4',
                fontFamily: 'monospace'
            }
        ).setOrigin(0.5);

        // Animation for loading
        let dots = 0;
        this.time.addEvent({
            delay: 500,
            callback: () => {
                dots = (dots + 1) % 4;
                loadingText.setText('Loading' + '.'.repeat(dots));
            },
            loop: true
        });

        // Simulate loading
        this.time.delayedCall(1500, () => {
            loadingText.destroy();
            this.scene.start('MenuScene');
        });
    }

    create(): void {
        this.scene.start('MenuScene');
    }
}