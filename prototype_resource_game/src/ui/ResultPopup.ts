import { Scene } from 'phaser';

export interface DayResult {
    day: number;
    resources: {
        wood: number;
        stone: number;
        iron: number;
        food: number;
        water: number;
        circuit: number;
        aluminum: number;
    };
    monsterParts: {
        fangs: number;
        hides: number;
        claws: number;
    };
    relicsFound: string[];
    missionsCompleted: number;
    totalTimeUsed: number;
    remainingTime: number;
    baseHPLost: number;
    crewStatus: { name: string; hp: number; maxHp: number; isAlive: boolean }[];
}

export class ResultPopup {
    private scene: Scene;
    private popupContainer: Phaser.GameObjects.Container | null = null;
    private onConfirm: () => void;
    private isVisible: boolean = false;

    constructor(scene: Scene, onConfirm: () => void) {
        this.scene = scene;
        this.onConfirm = onConfirm;
    }

    show(result: DayResult): void {
        if (this.isVisible) return;
        this.isVisible = true;

        const centerX = this.scene.cameras.main.width / 2;
        const centerY = this.scene.cameras.main.height / 2;

        // ✅ Overlay background
        const overlay = this.scene.add.graphics();
        overlay.fillStyle(0x000000, 0.7);
        overlay.fillRect(0, 0, this.scene.cameras.main.width, this.scene.cameras.main.height);
        overlay.setDepth(500);

        // ✅ Popup background
        const popupBg = this.scene.add.graphics();
        popupBg.fillStyle(0x1a1a2e, 0.95);
        popupBg.fillRoundedRect(centerX - 450, centerY - 300, 900, 600, 16);
        popupBg.lineStyle(2, 0x4ecdc4);
        popupBg.strokeRoundedRect(centerX - 450, centerY - 300, 900, 600, 16);
        popupBg.setDepth(501);

        // ✅ Title
        const title = this.scene.add.text(centerX, centerY - 270, `📊 Day ${result.day} Summary`, {
            fontSize: '32px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            stroke: '#000000',
            strokeThickness: 4
        }).setOrigin(0.5).setDepth(502);

        // ✅ Time info
        const timeInfo = this.scene.add.text(centerX, centerY - 220,
            `⏱️ Time Used: ${Math.floor(result.totalTimeUsed)} / ${Math.floor(result.totalTimeUsed + result.remainingTime)} units  |  ⏳ Remaining: ${Math.floor(result.remainingTime)} units`,
            {
                fontSize: '16px',
                color: '#b2bec3',
                fontFamily: 'monospace'
            }
        ).setOrigin(0.5).setDepth(502);

        // ✅ Resources section
        const resourcesY = centerY - 180;
        this.scene.add.text(centerX - 380, resourcesY, '📦 Resources:', {
            fontSize: '18px',
            color: '#f9ca24',
            fontFamily: 'monospace'
        }).setDepth(502);

        const resourceList = [
            { key: 'wood', icon: '🪵', label: 'Wood' },
            { key: 'stone', icon: '🪨', label: 'Stone' },
            { key: 'iron', icon: '⛏️', label: 'Iron' },
            { key: 'food', icon: '🍖', label: 'Food' },
            { key: 'water', icon: '💧', label: 'Water' },
            { key: 'circuit', icon: '⚡', label: 'Circuit' },
            { key: 'aluminum', icon: '🔩', label: 'Aluminum' }
        ];

        resourceList.forEach((res, index) => {
            const x = centerX - 380 + (index % 4) * 200;
            const y = resourcesY + 35 + Math.floor(index / 4) * 30;
            const amount = (result.resources as any)[res.key] || 0;
            this.scene.add.text(x, y,
                `${res.icon} ${res.label}: ${amount}`,
                {
                    fontSize: '14px',
                    color: amount > 0 ? '#ffffff' : '#636e72',
                    fontFamily: 'monospace'
                }
            ).setDepth(502);
        });

        // ✅ Monster Parts
        const partsY = resourcesY + 100;
        this.scene.add.text(centerX + 50, partsY, '🧬 Monster Parts:', {
            fontSize: '18px',
            color: '#f9ca24',
            fontFamily: 'monospace'
        }).setDepth(502);

        const partsList = [
            { key: 'fangs', icon: '🦷', label: 'Fangs' },
            { key: 'hides', icon: '🧵', label: 'Hides' },
            { key: 'claws', icon: '🔪', label: 'Claws' }
        ];

        partsList.forEach((part, index) => {
            const x = centerX + 50 + index * 150;
            const y = partsY + 35;
            const amount = (result.monsterParts as any)[part.key] || 0;
            this.scene.add.text(x, y,
                `${part.icon} ${part.label}: ${amount}`,
                {
                    fontSize: '14px',
                    color: amount > 0 ? '#ffffff' : '#636e72',
                    fontFamily: 'monospace'
                }
            ).setDepth(502);
        });

        // ✅ Relics
        const relicsY = partsY + 70;
        if (result.relicsFound && result.relicsFound.length > 0) {
            this.scene.add.text(centerX + 50, relicsY, '🏛️ Relics Found:', {
                fontSize: '18px',
                color: '#a29bfe',
                fontFamily: 'monospace'
            }).setDepth(502);

            result.relicsFound.forEach((relic, index) => {
                this.scene.add.text(centerX + 50, relicsY + 35 + index * 25,
                    `  ✨ ${relic}`,
                    {
                        fontSize: '14px',
                        color: '#a29bfe',
                        fontFamily: 'monospace'
                    }
                ).setDepth(502);
            });
        }

        // ✅ Crew Status
        const crewY = centerY - 180;
        this.scene.add.text(centerX + 300, crewY, '👥 Crew Status:', {
            fontSize: '18px',
            color: '#4ecdc4',
            fontFamily: 'monospace'
        }).setDepth(502);

        result.crewStatus.forEach((crew, index) => {
            const y = crewY + 35 + index * 30;
            const color = crew.isAlive ? '#4ecdc4' : '#ff6b6b';
            const status = crew.isAlive ? `❤️ ${Math.floor(crew.hp)}/${crew.maxHp}` : '💀 Dead';
            this.scene.add.text(centerX + 300, y,
                `${crew.name}: ${status}`,
                {
                    fontSize: '14px',
                    color: color,
                    fontFamily: 'monospace'
                }
            ).setDepth(502);
        });

        // ✅ Missions completed
        this.scene.add.text(centerX + 300, crewY + 35 + result.crewStatus.length * 30 + 15,
            `📋 Missions Completed: ${result.missionsCompleted}`,
            {
                fontSize: '16px',
                color: '#f9ca24',
                fontFamily: 'monospace'
            }
        ).setDepth(502);

        // ✅ Base HP
        const baseHpColor = result.baseHPLost > 0 ? '#ff6b6b' : '#4ecdc4';
        const baseHpText = result.baseHPLost > 0 ? `-${Math.floor(result.baseHPLost)} HP` : 'No damage';
        this.scene.add.text(centerX + 300, crewY + 35 + result.crewStatus.length * 30 + 50,
            `🏠 Base HP: ${baseHpText}`,
            {
                fontSize: '16px',
                color: baseHpColor,
                fontFamily: 'monospace'
            }
        ).setDepth(502);

        // ✅ Continue button
        const continueBtn = this.scene.add.text(centerX, centerY + 260, '🌙 CONTINUE TO NIGHT', {
            fontSize: '24px',
            color: '#00b894',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 30, y: 15 }
        }).setOrigin(0.5).setDepth(502).setInteractive();

        continueBtn.on('pointerover', () => {
            continueBtn.setStyle({ color: '#ffffff' });
        });

        continueBtn.on('pointerout', () => {
            continueBtn.setStyle({ color: '#00b894' });
        });

        continueBtn.on('pointerdown', () => {
            // ✅ Cleanup
            overlay.destroy();
            popupBg.destroy();
            title.destroy();
            timeInfo.destroy();
            continueBtn.destroy();
            // Remove all text objects (in a real implementation, you'd store references)
            this.scene.children.list.forEach(child => {
                if (child.type === 'Text' && (child as any).depth === 502) {
                    child.destroy();
                }
            });
            this.isVisible = false;
            this.onConfirm();
        });

        // ✅ Store references for cleanup
        this.popupContainer = this.scene.add.container(0, 0, [
            overlay, popupBg, title, timeInfo, continueBtn
        ]);
        this.popupContainer.setDepth(500);
    }

    isShowing(): boolean {
        return this.isVisible;
    }

    hide(): void {
        if (this.popupContainer) {
            this.popupContainer.destroy();
            this.popupContainer = null;
        }
        this.isVisible = false;
    }
}