// src/ui/CrewPanel.ts
import { Scene } from 'phaser';
import { Crew } from '../entities/Crew';
import { CrewManager } from '../systems/CrewManager';

export class CrewPanel {
    private scene: Scene;
    private crewManager: CrewManager;
    private crewTexts: Phaser.GameObjects.Text[] = [];
    private onCrewSelect: (crew: Crew) => void;
    private panelX: number;
    private panelY: number;
    private panelWidth: number;
    private panelHeight: number;
    
    // Pagination
    private currentPage: number = 0;
    private crewsPerPage: number = 3;
    private totalPages: number = 1;
    private pageText!: Phaser.GameObjects.Text;
    private prevBtn!: Phaser.GameObjects.Text;
    private nextBtn!: Phaser.GameObjects.Text;
    private allCrews: Crew[] = [];
    private isInitialized: boolean = false; // ✅ เพิ่มตัวแปรนี้

    constructor(
        scene: Scene, 
        crewManager: CrewManager, 
        onCrewSelect: (crew: Crew) => void,
        x: number = 30,
        y: number = 520
    ) {
        this.scene = scene;
        this.crewManager = crewManager;
        this.onCrewSelect = onCrewSelect;
        this.panelX = x;
        this.panelY = y;
        this.panelWidth = 1240;
        this.panelHeight = 130;
        this.createPanel();
    }

    private createPanel(): void {
        // Panel background
        const bg = this.scene.add.graphics();
        bg.fillStyle(0x1a1a2e, 0.9);
        bg.fillRect(this.panelX - 10, this.panelY - 10, this.panelWidth, this.panelHeight);
        bg.lineStyle(1, 0x4a4a5a);
        bg.strokeRect(this.panelX - 10, this.panelY - 10, this.panelWidth, this.panelHeight);

        // Title
        this.scene.add.text(this.panelX, this.panelY, '👥 CREW STATUS', {
            fontSize: '14px',
            color: '#ffffff',
            fontFamily: 'monospace'
        });

        // Pagination controls
        this.prevBtn = this.scene.add.text(this.panelX + 1100, this.panelY, '◀', {
            fontSize: '18px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 8, y: 2 }
        });
        this.prevBtn.setInteractive();
        this.prevBtn.on('pointerdown', () => this.prevPage());
        this.prevBtn.on('pointerover', () => {
            this.prevBtn.setStyle({ color: '#ffffff' });
        });
        this.prevBtn.on('pointerout', () => {
            this.prevBtn.setStyle({ color: '#4ecdc4' });
        });

        this.pageText = this.scene.add.text(this.panelX + 1160, this.panelY, '1/1', {
            fontSize: '14px',
            color: '#b2bec3',
            fontFamily: 'monospace'
        });

        this.nextBtn = this.scene.add.text(this.panelX + 1200, this.panelY, '▶', {
            fontSize: '18px',
            color: '#4ecdc4',
            fontFamily: 'monospace',
            backgroundColor: '#2d3436',
            padding: { x: 8, y: 2 }
        });
        this.nextBtn.setInteractive();
        this.nextBtn.on('pointerdown', () => this.nextPage());
        this.nextBtn.on('pointerover', () => {
            this.nextBtn.setStyle({ color: '#ffffff' });
        });
        this.nextBtn.on('pointerout', () => {
            this.nextBtn.setStyle({ color: '#4ecdc4' });
        });

        this.isInitialized = true;
        this.update();
    }

    update(): void {
        if (!this.isInitialized) return; // ✅ ป้องกันการเรียกก่อนสร้าง
        
        this.crewTexts.forEach(text => text.destroy());
        this.crewTexts = [];

        this.allCrews = this.crewManager.getAllAlive();
        this.totalPages = Math.max(1, Math.ceil(this.allCrews.length / this.crewsPerPage));
        
        if (this.currentPage >= this.totalPages) {
            this.currentPage = Math.max(0, this.totalPages - 1);
        }

        // ✅ อัพเดท Pagination
        this.updatePaginationVisibility();

        const start = this.currentPage * this.crewsPerPage;
        const end = Math.min(start + this.crewsPerPage, this.allCrews.length);
        const pageCrews = this.allCrews.slice(start, end);

        // ✅ ถ้าไม่มี Crew ให้แสดงข้อความ
        if (this.allCrews.length === 0) {
            const text = this.scene.add.text(this.panelX + 10, this.panelY + 50,
                'No crew available',
                {
                    fontSize: '16px',
                    color: '#b2bec3',
                    fontFamily: 'monospace'
                }
            );
            this.crewTexts.push(text);
            return;
        }

        const startX = this.panelX + 10;
        const y = this.panelY + 30;
        const spacing = 380;

        const gameScene = this.scene.scene.get('GameScene') as any;
        let chainCounts = new Map<number, number>();
        if (gameScene && gameScene.missionQueue) {
            for (const mission of gameScene.missionQueue) {
                if (!mission.completed) {
                    const count = chainCounts.get(mission.crew.id) || 0;
                    chainCounts.set(mission.crew.id, count + 1);
                }
            }
        }

        pageCrews.forEach((crew, index) => {
            const status = crew.isBusy ? '🔴 Busy' : '🟢 Ready';
            const color = crew.isBusy ? '#ff6b6b' : '#4ecdc4';
            
            const perkText = crew.perks.length > 0 ? ` [${crew.perks.join(', ')}]` : '';
            const chainCount = chainCounts.get(crew.id) || 0;
            const chainText = chainCount > 0 ? ` 📋x${chainCount}` : '';
            
            const x = startX + index * spacing;
            
            // ✅ ชื่อและสถานะ
            const text = this.scene.add.text(x, y,
                `${crew.name} (❤️${Math.floor(crew.hp)}/${crew.maxHp}) ${status}${chainText}`,
                {
                    fontSize: '16px',
                    color: color,
                    fontFamily: 'monospace'
                }
            );

            // ✅ Perk
            this.scene.add.text(x, y + 25,
                `⚡ ${perkText || 'No perk'}`,
                {
                    fontSize: '12px',
                    color: '#b2bec3',
                    fontFamily: 'monospace'
                }
            );

            // ✅ Stats พร้อม Rank (ใช้ getEffective)
            const effSpeed = crew.getEffectiveSpeed();
            const effGather = crew.getEffectiveGathering();
            const effSearch = crew.getEffectiveSearching();
            const effHunt = crew.getEffectiveHunting();
            
            const speedRank = crew.getRankDisplay(effSpeed);
            const gatherRank = crew.getRankDisplay(effGather);
            const searchRank = crew.getRankDisplay(effSearch);
            const huntRank = crew.getRankDisplay(effHunt);
            
            this.scene.add.text(x, y + 45,
                `🏃 ${effSpeed.toFixed(0)} [${speedRank.rank}] | ⛏️ ${effGather.toFixed(0)} [${gatherRank.rank}] | 🔍 ${effSearch.toFixed(0)} [${searchRank.rank}] | ⚔️ ${effHunt.toFixed(0)} [${huntRank.rank}]`,
                {
                    fontSize: '11px',
                    color: '#636e72',
                    fontFamily: 'monospace'
                }
            );

            // ✅ Make clickable
            if (!crew.isBusy && crew.isAlive) {
                text.setInteractive();
                text.on('pointerdown', () => {
                    this.onCrewSelect(crew);
                });
                text.on('pointerover', () => {
                    text.setStyle({ color: '#f9ca24' });
                });
                text.on('pointerout', () => {
                    text.setStyle({ color: color });
                });
            }

            this.crewTexts.push(text);
        });

        this.updatePaginationVisibility();
    }

    // ✅ Method สำหรับอัพเดท Pagination Visibility
    private updatePaginationVisibility(): void {
        const show = this.totalPages > 1;
        this.prevBtn.setVisible(show);
        this.nextBtn.setVisible(show);
        this.pageText.setVisible(show);
    }

    // ✅ Method สำหรับเปลี่ยนหน้า
    private prevPage(): void {
        if (this.currentPage > 0) {
            this.currentPage--;
            this.update();
        }
    }

    // ✅ Method สำหรับเปลี่ยนหน้า
    private nextPage(): void {
        if (this.currentPage < this.totalPages - 1) {
            this.currentPage++;
            this.update();
        }
    }

    show(): void {
        this.crewTexts.forEach(text => text.setVisible(true));
        this.prevBtn.setVisible(true);
        this.nextBtn.setVisible(true);
        this.pageText.setVisible(true);
    }

    hide(): void {
        this.crewTexts.forEach(text => text.setVisible(false));
        this.prevBtn.setVisible(false);
        this.nextBtn.setVisible(false);
        this.pageText.setVisible(false);
    }
}