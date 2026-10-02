// src/ui/JunctionPopup.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { MapNode } from '../entities/MapNode';
import { Systems } from '../main';

export interface JunctionChoice {
    nodeId: string;
    direction: 'left' | 'right' | 'straight';
    label: string;
}

export class JunctionPopup {
    private scene: Scene;
    private container: Phaser.GameObjects.Container | null = null;
    private timeoutEvent: Phaser.Time.TimerEvent | null = null;
    private junctionId: string | null = null;
    private choices: JunctionChoice[] = [];
    
    constructor(scene: Scene) {
        this.scene = scene;
    }
    
    // ============================================================
    // SHOW POPUP
    // ============================================================
    show(junction: MapNode, choices: JunctionChoice[]): void {
        // ✅ ถ้ามี popup อยู่แล้ว → ปิดก่อน
        this.hide();
        
        this.junctionId = junction.id;
        this.choices = choices;
        
        // ✅ สร้าง Container
        this.container = this.scene.add.container(0, 0);
        this.container.setDepth(2000);
        this.container.setScrollFactor(0);
        
        // ✅ Position: มุมขวาบน
        const popupX = GAME_CONFIG.WIDTH - 380;
        const popupY = 80;
        const popupWidth = 360;
        const popupHeight = 80 + choices.length * 50;
        
        // ✅ Background
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.95);
        bg.fillRoundedRect(popupX, popupY, popupWidth, popupHeight, 10);
        bg.lineStyle(2, 0xf9ca24);
        bg.strokeRoundedRect(popupX, popupY, popupWidth, popupHeight, 10);
        this.container.add(bg);
        
        // ✅ Title
        const title = this.scene.add.text(
            popupX + 15,
            popupY + 10,
            '🔀 JUNCTION',
            {
                fontSize: '18px',
                color: '#f9ca24',
                fontFamily: 'monospace',
                fontStyle: 'bold',
            }
        );
        this.container.add(title);
        
        // ✅ Timeout text
        const timeoutText = this.scene.add.text(
            popupX + popupWidth - 15,
            popupY + 10,
            '5s',
            {
                fontSize: '16px',
                color: '#ff6b6b',
                fontFamily: 'monospace',
            }
        ).setOrigin(1, 0);
        this.container.add(timeoutText);
        
        // ✅ Choices (Buttons)
        choices.forEach((choice, index) => {
            const buttonY = popupY + 45 + index * 50;
            this.createChoiceButton(choice, popupX + 15, buttonY, popupWidth - 30, timeoutText);
        });
        
        // ✅ Start Timeout (5 วินาทีจริง)
        let remainingSeconds = GAME_CONFIG.JUNCTION_AUTO_SELECT_SECONDS;
        
        this.timeoutEvent = this.scene.time.addEvent({
            delay: 1000,
            callback: () => {
                remainingSeconds--;
                timeoutText.setText(`${remainingSeconds}s`);
                
                if (remainingSeconds <= 0) {
                    this.autoSelect();
                }
            },
            loop: true,
        });
    }
    
    private createChoiceButton(
        choice: JunctionChoice,
        x: number,
        y: number,
        width: number,
        timeoutText: Phaser.GameObjects.Text
    ): void {
        // ✅ ใช้ Rectangle เป็น interactive object
        const btnBg = this.scene.add.rectangle(
            x + width / 2,
            y + 20,
            width,
            40,
            0x2d3436
        );
        btnBg.setStrokeStyle(1, 0x4ecdc4);
        btnBg.setInteractive({ useHandCursor: true });
        this.container!.add(btnBg);
        
        const btnText = this.scene.add.text(
            x + width / 2,
            y + 20,
            choice.label,
            {
                fontSize: '16px',
                color: '#4ecdc4',
                fontFamily: 'monospace',
            }
        ).setOrigin(0.5);
        this.container!.add(btnText);
        
        // ✅ Interactive ที่ Rectangle
        btnBg.on('pointerover', () => {
            btnText.setColor('#ffffff');
            btnBg.setFillStyle(0x4ecdc4, 0.3);
            btnBg.setStrokeStyle(2, 0x4ecdc4);
        });
        
        btnBg.on('pointerout', () => {
            btnText.setColor('#4ecdc4');
            btnBg.setFillStyle(0x2d3436, 1);
            btnBg.setStrokeStyle(1, 0x4ecdc4);
        });
        
        btnBg.on('pointerdown', () => {
            this.selectChoice(choice);
        });
    }
    
    // ============================================================
    // SELECT CHOICE
    // ============================================================
    private selectChoice(choice: JunctionChoice): void {
        if (!this.junctionId) return;
        
        // ✅ Emit event
        EventBus.emit(EVENTS.BASE_PATH_SELECTED, {
            fromNodeId: this.junctionId,
            toNodeId: choice.nodeId,
            direction: choice.direction,
        });
        
        // ✅ ซ่อน popup
        this.hide();
    }
    
    // ============================================================
    // AUTO SELECT (Timeout)
    // ============================================================
    private autoSelect(): void {
        if (this.choices.length === 0) return;
        
        // ✅ สุ่ม choice
        const randomChoice = this.choices[Math.floor(Math.random() * this.choices.length)];
        
        console.log(`⏰ Auto-selecting: ${randomChoice.label}`);
        
        this.selectChoice(randomChoice);
    }
    
    // ============================================================
    // HIDE
    // ============================================================
    hide(): void {
        if (this.timeoutEvent) {
            this.timeoutEvent.remove();
            this.timeoutEvent = null;
        }
        
        if (this.container) {
            this.container.destroy();
            this.container = null;
        }
        
        this.junctionId = null;
        this.choices = [];
    }
    
    // ============================================================
    // CHECK
    // ============================================================
    isShowing(): boolean {
        return this.container !== null;
    }
    
    // ============================================================
    // DESTROY
    // ============================================================
    destroy(): void {
        this.hide();
    }
}