// src/ui/HUD.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';

export class HUD {
    private scene: Scene;
    private container: Phaser.GameObjects.Container;
    
    private dayText!: Phaser.GameObjects.Text;
    private timeText!: Phaser.GameObjects.Text;
    private phaseText!: Phaser.GameObjects.Text;
    private speedText!: Phaser.GameObjects.Text;
    private fuelText!: Phaser.GameObjects.Text;
    private engineText!: Phaser.GameObjects.Text;
    
    constructor(scene: Scene) {
        this.scene = scene;
        
        // ✅ สร้าง container ที่ (0,0) และตั้ง scrollFactor(0)
        this.container = scene.add.container(0, 0);
        this.container.setDepth(1000);
        this.container.setScrollFactor(0);
        
        // ✅ ให้ UI camera เห็นเท่านั้น
        scene.cameras.main.ignore(this.container);
        
        this.createTopBar();
        
        EventBus.on(EVENTS.TIME_HOUR_TICK, this.update, this);
        EventBus.on(EVENTS.FUEL_CHANGED, this.update, this);
        EventBus.on(EVENTS.ENGINE_CONDITION_CHANGED, this.update, this);
        EventBus.on(EVENTS.TIME_SCALE_CHANGE, this.update, this);
        
        this.update();
    }
    
    private createTopBar(): void {
        const barX = 10;
        const barY = 10;
        const barWidth = 720;
        const barHeight = 50;
        
        // ✅ Background
        const bg = this.scene.add.graphics();
        bg.fillStyle(GAME_CONFIG.COLORS.BG_MEDIUM, 0.95);
        bg.fillRoundedRect(barX, barY, barWidth, barHeight, 8);
        bg.lineStyle(2, GAME_CONFIG.COLORS.PATH_ACTIVE, 0.8);
        bg.strokeRoundedRect(barX, barY, barWidth, barHeight, 8);
        this.container.add(bg);
        
        // ✅ Day
        this.dayText = this.scene.add.text(barX + 15, barY + 13, 'Day 1', {
            fontSize: '20px',
            color: '#ffffff',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        });
        this.container.add(this.dayText);
        
        // ✅ Time
        this.timeText = this.scene.add.text(barX + 130, barY + 13, '06:00', {
            fontSize: '20px',
            color: '#00e5cc',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        });
        this.container.add(this.timeText);
        
        // ✅ Phase
        this.phaseText = this.scene.add.text(barX + 240, barY + 13, '☀️ DAY', {
            fontSize: '20px',
            color: '#ffd93d',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        });
        this.container.add(this.phaseText);
        
        // ✅ Speed
        this.speedText = this.scene.add.text(barX + 390, barY + 13, '⚡ x1', {
            fontSize: '20px',
            color: '#b0c4de',
            fontFamily: 'monospace',
        });
        this.container.add(this.speedText);
        
        // ✅ Fuel
        this.fuelText = this.scene.add.text(barX + 500, barY + 13, '⛽ 500', {
            fontSize: '18px',
            color: '#ffffff',
            fontFamily: 'monospace',
        });
        this.container.add(this.fuelText);
        
        // ✅ Engine
        this.engineText = this.scene.add.text(barX + 620, barY + 13, '🔧 100%', {
            fontSize: '18px',
            color: '#ffffff',
            fontFamily: 'monospace',
        });
        this.container.add(this.engineText);
    }
    
    update(): void {
        const state = GameState;
        const time = state.getTime();
        const fuel = state.getFuel();
        const engine = state.getEngine();
        
        this.dayText.setText(`Day ${time.gameDay}`);
        
        const hh = String(time.gameHour).padStart(2, '0');
        const mm = String(time.gameMinute).padStart(2, '0');
        this.timeText.setText(`${hh}:${mm}`);
        
        if (time.phase === 'day') {
            this.phaseText.setText('☀️ DAY');
            this.phaseText.setColor('#ffd93d');
        } else {
            this.phaseText.setText('🌙 NIGHT');
            this.phaseText.setColor('#6c5ce7');
        }
        
        this.speedText.setText(`⚡ x${time.timeScale}`);
        
        this.fuelText.setText(`⛽ ${Math.floor(fuel.current)}/${fuel.max}`);
        this.fuelText.setColor(fuel.isWarning ? '#ff6b6b' : '#ffffff');
        
        this.engineText.setText(`🔧 ${Math.floor(engine.condition)}%`);
        if (engine.condition < 20) {
            this.engineText.setColor('#ff6b6b');
        } else if (engine.condition < 50) {
            this.engineText.setColor('#f9ca24');
        } else {
            this.engineText.setColor('#ffffff');
        }
    }
    
    destroy(): void {
        this.container.destroy();
        
        EventBus.off(EVENTS.TIME_HOUR_TICK, this.update, this);
        EventBus.off(EVENTS.FUEL_CHANGED, this.update, this);
        EventBus.off(EVENTS.ENGINE_CONDITION_CHANGED, this.update, this);
        EventBus.off(EVENTS.TIME_SCALE_CHANGE, this.update, this);
    }
}