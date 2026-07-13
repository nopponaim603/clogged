// src/ui/PlanningPanel.ts
import { Scene } from 'phaser';
import { Crew } from '../entities/Crew';
import { ResourceNode } from '../entities/ResourceNode';

export class PlanningPanel {
    private scene: Scene;
    private statusText: Phaser.GameObjects.Text;
    private selectedCrewText: Phaser.GameObjects.Text;
    private selectedTargetText: Phaser.GameObjects.Text;
    private confirmBtn: Phaser.GameObjects.Text;
    private executeBtn: Phaser.GameObjects.Text;
    private resetBtn: Phaser.GameObjects.Text;  // ✅ เพิ่มปุ่ม Reset
    private undoBtn: Phaser.GameObjects.Text;   // ✅ เพิ่มปุ่ม Undo
    private onConfirm: () => void;
    private onExecute: (() => void) | null = null;
    private onReset: (() => void) | null = null;    // ✅ Callback สำหรับ Reset
    private onUndo: (() => void) | null = null;     // ✅ Callback สำหรับ Undo
    private panelX: number;
    private panelY: number;

    constructor(
        scene: Scene,
        onConfirm: () => void,
        x: number = 30,
        y: number = 680
    ) {
        this.scene = scene;
        this.onConfirm = onConfirm;
        this.panelX = x;
        this.panelY = y;
        this.createPanel();
    }

    private createPanel(): void {
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.9);
        bg.fillRect(this.panelX - 10, this.panelY - 10, 1240, 55);
        bg.lineStyle(1, 0x4a4a5a);
        bg.strokeRect(this.panelX - 10, this.panelY - 10, 1240, 55);

        this.statusText = this.scene.add.text(this.panelX, this.panelY + 5,
            '🎯 Click a crew → Click nodes to build a chain → ADD TO QUEUE',
            {
                fontSize: '14px',
                color: '#b2bec3',
                fontFamily: 'monospace'
            }
        );

        this.selectedCrewText = this.scene.add.text(this.panelX, this.panelY + 25,
            'Selected: None',
            {
                fontSize: '12px',
                color: '#f9ca24',
                fontFamily: 'monospace'
            }
        );

        this.selectedTargetText = this.scene.add.text(this.panelX + 250, this.panelY + 25,
            'Chain: 0 nodes',
            {
                fontSize: '12px',
                color: '#4ecdc4',
                fontFamily: 'monospace'
            }
        );

        // ✅ ปุ่ม Reset (ยกเลิกทั้งหมด)
        this.resetBtn = this.scene.add.text(this.panelX + 560, this.panelY + 5,
            '🔄 RESET',
            {
                fontSize: '14px',
                color: '#ff6b6b',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 10, y: 5 }
            }
        );
        this.resetBtn.setInteractive();
        this.resetBtn.on('pointerdown', () => {
            if (this.onReset) {
                this.onReset();
            }
        });
        this.resetBtn.on('pointerover', () => {
            this.resetBtn.setStyle({ color: '#ffffff' });
        });
        this.resetBtn.on('pointerout', () => {
            this.resetBtn.setStyle({ color: '#ff6b6b' });
        });
        this.resetBtn.setVisible(false);

        // ✅ ปุ่ม Undo (ลบ node สุดท้าย)
        this.undoBtn = this.scene.add.text(this.panelX + 680, this.panelY + 5,
            '↩️ UNDO',
            {
                fontSize: '14px',
                color: '#f9ca24',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 10, y: 5 }
            }
        );
        this.undoBtn.setInteractive();
        this.undoBtn.on('pointerdown', () => {
            if (this.onUndo) {
                this.onUndo();
            }
        });
        this.undoBtn.on('pointerover', () => {
            this.undoBtn.setStyle({ color: '#ffffff' });
        });
        this.undoBtn.on('pointerout', () => {
            this.undoBtn.setStyle({ color: '#f9ca24' });
        });
        this.undoBtn.setVisible(false);

        // ✅ Confirm button
        this.confirmBtn = this.scene.add.text(this.panelX + 800, this.panelY + 5,
            '➕ ADD TO QUEUE',
            {
                fontSize: '14px',
                color: '#00b894',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 10, y: 5 }
            }
        );
        this.confirmBtn.setInteractive();
        this.confirmBtn.on('pointerdown', () => {
            this.onConfirm();
        });
        this.confirmBtn.on('pointerover', () => {
            this.confirmBtn.setStyle({ color: '#ffffff' });
        });
        this.confirmBtn.on('pointerout', () => {
            this.confirmBtn.setStyle({ color: '#00b894' });
        });
        this.confirmBtn.setVisible(false);

        // ✅ Execute button
        this.executeBtn = this.scene.add.text(this.panelX + 1020, this.panelY + 5,
            '▶ EXECUTE ALL',
            {
                fontSize: '16px',
                color: '#f9ca24',
                fontFamily: 'monospace',
                backgroundColor: '#2d3436',
                padding: { x: 15, y: 5 }
            }
        );
        this.executeBtn.setInteractive();
        this.executeBtn.on('pointerdown', () => {
            if (this.onExecute) {
                this.onExecute();
            }
        });
        this.executeBtn.on('pointerover', () => {
            this.executeBtn.setStyle({ color: '#ffffff' });
        });
        this.executeBtn.on('pointerout', () => {
            this.executeBtn.setStyle({ color: '#f9ca24' });
        });
        this.executeBtn.setVisible(false);
    }

    // ✅ ตั้งค่า Callbacks
    setupCallbacks(
        onReset: () => void,
        onUndo: () => void
    ): void {
        this.onReset = onReset;
        this.onUndo = onUndo;
    }

    updateSelection(crew: Crew | null, target: ResourceNode | null, chainCount: number = 0): void {
        this.selectedCrewText.setText(`Selected: ${crew ? crew.name + ' ⭐' : 'None'}`);
        
        if (chainCount > 0) {
            this.selectedTargetText.setText(`Chain: ${chainCount} nodes`);
            this.resetBtn.setVisible(true);
            this.undoBtn.setVisible(true);
        } else {
            this.selectedTargetText.setText(`Chain: 0 nodes`);
            this.resetBtn.setVisible(false);
            this.undoBtn.setVisible(false);
        }

        if (crew && chainCount > 0) {
            this.statusText.setText(`✅ Chain ready (${chainCount} nodes)! Click ADD TO QUEUE`);
            this.statusText.setColor('#00b894');
            this.confirmBtn.setVisible(true);
        } else if (crew) {
            this.statusText.setText('🎯 Click nodes to build a chain → ADD TO QUEUE');
            this.statusText.setColor('#b2bec3');
            this.confirmBtn.setVisible(false);
        } else {
            this.statusText.setText('🎯 Click a crew → Click nodes to build a chain → ADD TO QUEUE');
            this.statusText.setColor('#b2bec3');
            this.confirmBtn.setVisible(false);
        }
    }

    setStatus(message: string, color: string = '#b2bec3'): void {
        this.statusText.setText(message);
        this.statusText.setColor(color);
    }

    setConfirmVisible(visible: boolean): void {
        this.confirmBtn.setVisible(visible);
    }

    // ✅ แสดงปุ่ม Execute (เรียกจากภายนอก)
    showExecuteButton(onExecute: () => void): void {
        this.onExecute = onExecute;
        this.executeBtn.setVisible(true);
    }

    // ✅ ซ่อนปุ่ม Execute
    hideExecuteButton(): void {
        this.executeBtn.setVisible(false);
        this.onExecute = null;
    }

    // ✅ ตรวจสอบว่ามีปุ่ม Execute แสดงอยู่หรือไม่
    isExecuteButtonVisible(): boolean {
        return this.executeBtn.visible;
    }

    show(): void {
        this.statusText.setVisible(true);
        this.selectedCrewText.setVisible(true);
        this.selectedTargetText.setVisible(true);
        this.confirmBtn.setVisible(false);
        this.executeBtn.setVisible(false);
        this.resetBtn.setVisible(false);
        this.undoBtn.setVisible(false);
    }

    hide(): void {
        this.statusText.setVisible(false);
        this.selectedCrewText.setVisible(false);
        this.selectedTargetText.setVisible(false);
        this.confirmBtn.setVisible(false);
        this.executeBtn.setVisible(false);
        this.resetBtn.setVisible(false);
        this.undoBtn.setVisible(false);
    }
}