// src/ui/CrewPanel.ts
import { Scene } from 'phaser';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Crew } from '../entities/Crew';

export class CrewPanel {
    private scene: Scene;
    private container: Phaser.GameObjects.Container;
    private crewTexts: Phaser.GameObjects.Text[] = [];
    
    private readonly PANEL_X = 20;
    private readonly PANEL_Y = 80;
    private readonly PANEL_WIDTH = 280;
    private readonly PANEL_HEIGHT = 400;
    
    constructor(scene: Scene) {
        this.scene = scene;
        
        this.container = scene.add.container(0, 0);
        this.container.setDepth(1000);
        this.container.setScrollFactor(0);
        
        this.createPanel();
        this.update();
        
        EventBus.on('crew:hired', this.update, this);
        EventBus.on('crew:fired', this.update, this);
        EventBus.on(EVENTS.CREW_DIED, this.update, this);
    }
    
    private createPanel(): void {
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.9);
        bg.fillRect(this.PANEL_X, this.PANEL_Y, this.PANEL_WIDTH, this.PANEL_HEIGHT);
        bg.lineStyle(2, 0x4a4a5a);
        bg.strokeRect(this.PANEL_X, this.PANEL_Y, this.PANEL_WIDTH, this.PANEL_HEIGHT);
        this.container.add(bg);
        
        const title = this.scene.add.text(
            this.PANEL_X + 10,
            this.PANEL_Y + 10,
            '👥 CREW',
            {
                fontSize: '16px',
                color: '#ffffff',
                fontFamily: 'monospace',
                fontStyle: 'bold',
            }
        );
        this.container.add(title);
    }
    
    update(): void {
        // ✅ Clear
        this.crewTexts.forEach(t => t.destroy());
        this.crewTexts = [];
        
        const state = GameState;
        const crew = state.getCrew();
        
        // ✅ Defensive check
        if (!crew || !Array.isArray(crew)) {
            console.warn('⚠️ CrewPanel: crew is not an array');
            return;
        }
        
        let y = this.PANEL_Y + 40;
        let displayedCount = 0;
        
        crew.forEach(c => {
            if (!c || !c.isAlive()) return;
            
            // ✅ เช็ค bounds ก่อนสร้าง text
            if (y > this.PANEL_Y + this.PANEL_HEIGHT - 20) {
                const more = this.scene.add.text(
                    this.PANEL_X + 10,
                    y,
                    `... +${crew.filter(x => x.isAlive()).length - displayedCount} more`,
                    {
                        fontSize: '12px',
                        color: '#636e72',
                        fontFamily: 'monospace',
                    }
                );
                this.container.add(more);
                this.crewTexts.push(more);
                return;
            }
            
            const stateIcon = this.getStateIcon(c.state);
            const text = this.scene.add.text(
                this.PANEL_X + 10,
                y,
                `${stateIcon} ${c.name} (${c.hp}/${c.maxHp})`,
                {
                    fontSize: '12px',
                    color: this.getStateColor(c.state),
                    fontFamily: 'monospace',
                }
            );
            this.container.add(text);
            this.crewTexts.push(text);
            
            y += 20;
            displayedCount++;
        });
    }
    
    private getStateIcon(state: string): string {
        switch (state) {
            case 'idle': return '🟢';
            case 'dispatched': return '🚗';
            case 'assigned': return '🏭';
            case 'stranded': return '🌙';
            case 'dead': return '💀';
            default: return '❓';
        }
    }
    
    private getStateColor(state: string): string {
        switch (state) {
            case 'idle': return '#4ecdc4';
            case 'dispatched': return '#f9ca24';
            case 'assigned': return '#6c5ce7';
            case 'stranded': return '#ff6b6b';
            case 'dead': return '#636e72';
            default: return '#ffffff';
        }
    }
    
    destroy(): void {
        this.container.destroy();
        EventBus.off('crew:hired', this.update, this);
        EventBus.off('crew:fired', this.update, this);
        EventBus.off(EVENTS.CREW_DIED, this.update, this);
    }
}