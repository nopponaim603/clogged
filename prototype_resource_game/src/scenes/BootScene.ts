// src/scenes/BootScene.ts
import { Scene } from 'phaser';
import { initializeSystems, Systems } from '../main';
import { GameState } from '../state/GameState';
import { GAME_CONFIG } from '../config';  // ✅ เพิ่มบรรทัดนี้

export class BootScene extends Scene {
    constructor() {
        super('BootScene');
    }
    
    preload(): void {
        this.cameras.main.setBackgroundColor('#1a2332');
        
        const loadingText = this.add.text(
            this.cameras.main.width / 2,
            this.cameras.main.height / 2 - 30,
            `${GAME_CONFIG.GAME_NAME}`,  // ✅ ใช้ได้แล้ว
            {
                fontSize: '48px',
                color: '#00e5cc',
                fontFamily: 'monospace',
                fontStyle: 'bold',
            }
        ).setOrigin(0.5);
        
        this.add.text(
            this.cameras.main.width / 2,
            this.cameras.main.height / 2 + 30,
            'Loading...',
            {
                fontSize: '24px',
                color: '#b0c4de',
                fontFamily: 'monospace',
            }
        ).setOrigin(0.5);
    }
    
    create(): void {
        // ✅ Initialize Systems
        initializeSystems(this);
        
        // ✅ Go to Menu
        this.scene.start('MenuScene');
    }
}