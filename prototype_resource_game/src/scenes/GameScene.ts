// src/scenes/GameScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { Crew } from '../entities/Crew';
import { ResourceNode } from '../entities/ResourceNode';
import { CrewManager } from '../systems/CrewManager';
import { ResourceManager } from '../systems/ResourceManager';
import { MapGenerator } from '../systems/MapGenerator';
import { MissionSystem } from '../systems/MissionSystem';
import { TimeSystem } from '../systems/TimeSystem';
import { ResourcePanel } from '../ui/ResourcePanel';
import { CrewPanel } from '../ui/CrewPanel';
import { PlanningPanel } from '../ui/PlanningPanel';
import { NotificationSystem } from '../ui/NotificationSystem';
import { MissionDisplay } from '../ui/MissionDisplay';

interface QueuedMission {
    crew: Crew;
    target: ResourceNode;
    travelTime: number;
    actionTime: number;
    totalTime: number;
    phase: 'travel_out' | 'action' | 'travel_back' | 'complete';
    progress: number;
    travelOutDone: boolean;
    actionDone: boolean;
    travelBackDone: boolean;
    usedTime: number;
    completed: boolean;
    chainIndex: number;
    isChained: boolean;
    nextMission: QueuedMission | null;
}

export class GameScene extends Scene {
    // Systems
    private crewManager!: CrewManager;
    private resourceManager!: ResourceManager;
    private mapGenerator!: MapGenerator;
    private missionSystem!: MissionSystem;
    private timeSystem!: TimeSystem;
    
    // UI Components
    private resourcePanel!: ResourcePanel;
    private crewPanel!: CrewPanel;
    private planningPanel!: PlanningPanel;
    private notificationSystem!: NotificationSystem;
    private missionDisplay!: MissionDisplay;

    // ✅ Selection - รองรับหลายโหนด
    private selectedCrew: Crew | null = null;
    private selectedTargets: ResourceNode[] = []; // ✅ ใช้ Array แทน
    private tempMissions: { crew: Crew; target: ResourceNode }[] = [];

    // ✅ เพิ่ม property สำหรับเก็บ selected sprites ที่เป็นไฮไลท์
    private selectedNodeHighlights: Phaser.GameObjects.Arc[] = [];
    
    // Mission Queue
    private missionQueue: QueuedMission[] = [];
    private isProcessingQueue: boolean = false;

    // ✅ เพิ่มสำหรับ multi-crew collaboration
    private activeMissions: Map<string, { 
        crewIds: number[]; 
        totalProficiency: number;
        actionTime: number;
        startTime: number;
    }> = new Map();
    
    // State
    private gameOver: boolean = false;
    private isWin: boolean = false;
    
    // UI Elements
    private dayText!: Phaser.GameObjects.Text;
    private timeText!: Phaser.GameObjects.Text;
    private baseHPText!: Phaser.GameObjects.Text;
    private baseSprite!: Phaser.GameObjects.Arc;
    private resourceNodeSprites: Phaser.GameObjects.Arc[] = [];
    private queueText!: Phaser.GameObjects.Text;
    private missionSprites: Map<number, Phaser.GameObjects.Text> = new Map();
    private crewTimeTexts: Map<number, Phaser.GameObjects.Text> = new Map();
    private selectedPathLines: Phaser.GameObjects.Graphics[] = [];

    constructor() {
        super('GameScene');
    }

    create(): void {
        // Initialize systems
        this.crewManager = new CrewManager();
        this.resourceManager = new ResourceManager();
        this.mapGenerator = new MapGenerator();
        this.missionSystem = new MissionSystem(this.resourceManager);
        this.timeSystem = new TimeSystem();

        this.timeSystem.onSimulateStep = () => this.simulateStep();

        this.notificationSystem = new NotificationSystem(this);
        this.missionDisplay = new MissionDisplay(this);

        this.timeSystem.onDayEnd = () => this.startNightPhase();
        this.timeSystem.onNightEnd = () => this.startNewDay();

        this.mapGenerator.generateResources();
        const basePos = this.mapGenerator.getBasePosition();

        this.baseSprite = this.add.arc(basePos.x, basePos.y, 30, 0, 360, false, 0x00b894);
        this.baseSprite.setStrokeStyle(3, 0x00d2a0);
        
        this.add.text(basePos.x - 30, basePos.y - 10, '🏠 BASE', {
            fontSize: '14px',
            color: '#ffffff',
            fontFamily: 'monospace'
        });

        this.createInitialCrews(basePos);
        this.setupUI();
        this.drawMap();

        this.timeSystem.startDay(this);

        this.notificationSystem.showInfo('Click a crew, then click nodes to build a chain!', 4000);

        // ✅ เพิ่ม Keyboard Shortcuts
        this.input.keyboard?.on('keydown-ESC', () => {
            this.deselectAll();
        });

        this.input.keyboard?.on('keydown-Z', (event: KeyboardEvent) => {
            if (event.ctrlKey) {
                this.removeLastNode();
            }
        });

        this.input.keyboard?.on('keydown-R', () => {
                this.clearAllSelection();
        });
    }

    private setupUI(): void {
        this.createTopBar();

        this.resourcePanel = new ResourcePanel(
            this,
            this.resourceManager,
            GAME_CONFIG.WIDTH - 190,
            60
        );

        this.crewPanel = new CrewPanel(
            this,
            this.crewManager,
            (crew: Crew) => this.selectCrew(crew),
            30,
            520
        );

        this.planningPanel = new PlanningPanel(
            this,
            () => this.confirmChain(),
            30,
            680
        );

        // ✅ ตั้งค่า Callbacks สำหรับปุ่ม Reset และ Undo
        this.planningPanel.setupCallbacks(
            () => this.clearAllSelection(),   // Reset
            () => this.removeLastNode()        // Undo
        );

        this.queueText = this.add.text(30, 500,
            `📋 Missions: 0 | Total Time: 0 units | ⏱️ Elapsed: 0/${this.timeSystem.dayTimeLimit} units`,
            {
                fontSize: '16px',
                color: '#f9ca24',
                fontFamily: 'monospace'
            }
        );
    }

    private createTopBar(): void {
        this.dayText = this.add.text(20, 20, 
            `Day ${this.timeSystem.day}/${GAME_CONFIG.MAX_DAYS}`,
            { fontSize: '24px', color: '#ffffff', fontFamily: 'monospace' }
        );

        this.timeText = this.add.text(250, 20,
            `⏱️ Elapsed: ${Math.floor(this.timeSystem.elapsedTime)} / ${this.timeSystem.dayTimeLimit} units`,
            { fontSize: '20px', color: '#4ecdc4', fontFamily: 'monospace' }
        );

        this.baseHPText = this.add.text(600, 20,
            `🏠 HP: ${Math.floor(this.resourceManager.baseHP)}`,
            { fontSize: '24px', color: '#ff6b6b', fontFamily: 'monospace' }
        );

        const remaining = this.timeSystem.dayTimeLimit - this.timeSystem.elapsedTime;
        this.add.text(850, 20,
            `⏳ Left: ${Math.floor(remaining)} units`,
            { fontSize: '16px', color: '#f9ca24', fontFamily: 'monospace' }
        );
    }

    private drawMap(): void {
        this.resourceNodeSprites.forEach(sprite => sprite.destroy());
        this.resourceNodeSprites = [];

        const graphics = this.add.graphics();
        graphics.lineStyle(1, 0x4a4a5a, 0.3);
        for (let i = 0; i < 10; i++) {
            graphics.beginPath();
            graphics.moveTo(20 + i * 60, 60);
            graphics.lineTo(20 + i * 60, GAME_CONFIG.HEIGHT - 160);
            graphics.strokePath();
            graphics.beginPath();
            graphics.moveTo(20, 60 + i * 50);
            graphics.lineTo(GAME_CONFIG.WIDTH - 180, 60 + i * 50);
            graphics.strokePath();
        }

        this.mapGenerator.resourceNodes.forEach(node => {
            const color = node.getColor();
            const arc = this.add.arc(
                node.position.x, 
                node.position.y, 
                18, 
                0, 
                360, 
                false, 
                color, 
                0.7
            );
            arc.setStrokeStyle(2, color, 0.5);
            arc.setInteractive();
            arc.on('pointerdown', () => {
                this.selectTarget(node);
            });
            node.sprite = arc as unknown as Phaser.GameObjects.Arc;
            this.resourceNodeSprites.push(arc);

            const text = this.add.text(
                node.position.x - 12,
                node.position.y - 10,
                node.icon,
                { fontSize: '20px' }
            );
            node.label = text;

            const label = this.add.text(
                node.position.x - 25,
                node.position.y + 25,
                node.isRelic ? 'RELIC' : 
                node.isMonster ? `Lv.${node.difficulty}` : 
                `${node.amount}`,
                {
                    fontSize: '10px',
                    color: node.isRelic ? '#a29bfe' : 
                           node.isMonster ? '#ff6b6b' : '#ffffff',
                    fontFamily: 'monospace'
                }
            );
            node.label = label;
        });
    }

    private createInitialCrews(basePos: { x: number; y: number }): void {
        const crewTemplates = [
            { name: 'Sarah', hp: 100, speed: 1.2, gathering: 1.5, searching: 0.8, hunting: 0.9, perks: ['fast_hands'], cost: 25 },
            { name: 'John', hp: 120, speed: 0.9, gathering: 0.7, searching: 1.8, hunting: 1.2, perks: ['night_vision'], cost: 30 },
            { name: 'Emma', hp: 90, speed: 1.5, gathering: 1.2, searching: 1.0, hunting: 1.5, perks: ['gunslinger'], cost: 35 },
        ];

        let points = GAME_CONFIG.CREW_POINTS;
        const hired: Crew[] = [];

        crewTemplates.forEach((template, index) => {
            const crew = new Crew({
                id: index + 1,
                name: template.name,
                hp: template.hp,
                speed: template.speed,
                gatheringProficiency: template.gathering,
                searchingProficiency: template.searching,
                huntingProficiency: template.hunting,
                perks: template.perks,
                hireCost: template.cost,
                position: { 
                    x: basePos.x - 60 + index * 40, 
                    y: basePos.y + 40 
                }
            });

            const result = this.crewManager.hireCrew(crew, points);
            if (result.success) {
                points = result.remainingPoints;
                hired.push(crew);
            }
        });

        hired.forEach((crew, index) => {
            const text = this.add.text(
                crew.position.x,
                crew.position.y,
                '🧑‍🤝‍🧑',
                { fontSize: '24px' }
            );
            crew.sprite = text;

            this.add.text(
                crew.position.x - 20,
                crew.position.y + 30,
                crew.name,
                { fontSize: '10px', color: '#ffffff', fontFamily: 'monospace' }
            );
        });
    }

    // ✅ เลือก Crew
    private selectCrew(crew: Crew): void {
        if (crew.isBusy || this.gameOver || this.timeSystem.isNightPhase) return;
        if (this.timeSystem.isExecuting) return;
        if (this.isProcessingQueue) return;
        
        if (this.selectedCrew && this.selectedCrew.id !== crew.id) {
            this.clearSelection();
        }
        
        this.selectedCrew = crew;
        this.selectedTargets = [];
        this.tempMissions = [];
        this.clearPathLines();
        
        this.planningPanel.updateSelection(crew, null);
        this.notificationSystem.showInfo(`Selected ${crew.name} - Click nodes to build chain`, 2000);
        this.missionDisplay.clear();
    }

    // ✅ เลือก Target (สามารถเลือกหลายอันได้) - ปรับปรุงให้คลิกซ้ำเพื่อยกเลิก
    private selectTarget(target: ResourceNode): void {
        if (!this.selectedCrew || this.gameOver || this.timeSystem.isNightPhase) return;
        if (this.timeSystem.isExecuting) return;
        if (this.isProcessingQueue) return;
        
        // ✅ ถ้าเลือก node นี้ไปแล้ว → ยกเลิกการเลือก (คลิกซ้ำ)
        const existingIndex = this.selectedTargets.indexOf(target);
        if (existingIndex !== -1) {
            // ✅ ลบ node ออกจาก list
            this.selectedTargets.splice(existingIndex, 1);
            this.tempMissions.splice(existingIndex, 1);
            
            // ✅ อัพเดท highlight
            this.updateNodeHighlights();
            
            // ✅ อัพเดทเส้นทาง
            this.drawPathLines();
            
            // ✅ อัพเดทข้อมูลเวลา
            if (this.selectedTargets.length > 0) {
                this.missionDisplay.showMissionChain(this.selectedCrew, this.selectedTargets);
            } else {
                this.missionDisplay.clear();
            }
            
            // ✅ อัพเดท UI
            this.planningPanel.updateSelection(this.selectedCrew, null, this.selectedTargets.length);
            this.notificationSystem.showInfo(`❌ Removed ${target.icon} ${target.type} from chain`, 1500);
            return;
        }
        
        // ✅ เพิ่ม target เข้าไปใน list
        this.selectedTargets.push(target);
        this.tempMissions.push({
            crew: this.selectedCrew,
            target: target
        });
        
        // ✅ อัพเดท highlight
        this.updateNodeHighlights();
        
        // ✅ แสดงเส้นทาง
        this.drawPathLines();
        
        // ✅ แสดงข้อมูลเวลา
        this.missionDisplay.showMissionChain(this.selectedCrew, this.selectedTargets);
        
        // ✅ อัพเดท UI
        this.planningPanel.updateSelection(this.selectedCrew, target, this.selectedTargets.length);
        this.notificationSystem.showInfo(`📍 Added ${target.icon} ${target.type} (${this.selectedTargets.length} in chain)`, 1500);
    }

    // ✅ อัพเดท highlight ของ node ที่ถูกเลือก
    private updateNodeHighlights(): void {
        // ลบ highlight เดิม
        this.selectedNodeHighlights.forEach(h => h.destroy());
        this.selectedNodeHighlights = [];
        
        // เพิ่ม highlight ใหม่
        for (const target of this.selectedTargets) {
            if (target.sprite) {
                const highlight = this.add.arc(
                    target.position.x,
                    target.position.y,
                    24,
                    0,
                    360,
                    false,
                    0xf9ca24,
                    0.3
                );
                highlight.setStrokeStyle(3, 0xf9ca24, 0.8);
                highlight.setDepth(5);
                this.selectedNodeHighlights.push(highlight);
            }
        }
    }

    // ✅ เคลียร์การเลือกทั้งหมด (ปุ่ม Reset)
    private clearAllSelection(): void {
        if (!this.selectedCrew) return;
        
        this.selectedTargets = [];
        this.tempMissions = [];
        this.clearPathLines();
        this.selectedNodeHighlights.forEach(h => h.destroy());
        this.selectedNodeHighlights = [];
        this.missionDisplay.clear();
        this.planningPanel.updateSelection(this.selectedCrew, null, 0);
        this.notificationSystem.showInfo(`🔄 Cleared all selections for ${this.selectedCrew.name}`, 1500);
    }

    // ✅ ยกเลิกการเลือกทั้งหมด (รวม crew)
    private deselectAll(): void {
        this.selectedCrew = null;
        this.selectedTargets = [];
        this.tempMissions = [];
        this.clearPathLines();
        this.selectedNodeHighlights.forEach(h => h.destroy());
        this.selectedNodeHighlights = [];
        this.missionDisplay.clear();
        this.planningPanel.updateSelection(null, null, 0);
        this.notificationSystem.showInfo('🔄 Selection cleared', 1500);
    }

    // ✅ ลบ node สุดท้ายออกจาก chain
    private removeLastNode(): void {
        if (this.selectedTargets.length === 0) return;
        
        const removed = this.selectedTargets.pop();
        this.tempMissions.pop();
        
        if (removed) {
            this.updateNodeHighlights();
            this.drawPathLines();
            
            if (this.selectedTargets.length > 0) {
                this.missionDisplay.showMissionChain(this.selectedCrew!, this.selectedTargets);
            } else {
                this.missionDisplay.clear();
            }
            
            this.planningPanel.updateSelection(this.selectedCrew, null, this.selectedTargets.length);
            this.notificationSystem.showInfo(`↩️ Removed last node (${removed.icon} ${removed.type})`, 1500);
        }
    }

    // ✅ วาดเส้นทาง
    private drawPathLines(): void {
        this.clearPathLines();
        
        if (this.selectedTargets.length === 0) return;
        
        const basePos = this.mapGenerator.getBasePosition();
        let startPos = { x: basePos.x, y: basePos.y };
        
        for (let i = 0; i < this.selectedTargets.length; i++) {
            const target = this.selectedTargets[i];
            const endPos = { x: target.position.x, y: target.position.y };
            
            const line = this.add.graphics();
            line.lineStyle(2, 0x4ecdc4, 0.8);
            line.beginPath();
            line.moveTo(startPos.x, startPos.y);
            line.lineTo(endPos.x, endPos.y);
            line.strokePath();
            
            this.drawArrow(line, startPos, endPos);
            this.selectedPathLines.push(line);
            
            const midX = (startPos.x + endPos.x) / 2;
            const midY = (startPos.y + endPos.y) / 2 - 20;
            const orderText = this.add.text(midX, midY, `#${i + 1}`, {
                fontSize: '14px',
                color: '#f9ca24',
                fontFamily: 'monospace',
                backgroundColor: '#1a1a2e',
                padding: { x: 4, y: 2 }
            }).setOrigin(0.5);
            this.selectedPathLines.push(orderText as any);
            
            startPos = endPos;
        }
        
        const lastPos = { x: this.selectedTargets[this.selectedTargets.length - 1].position.x, 
                          y: this.selectedTargets[this.selectedTargets.length - 1].position.y };
        const line = this.add.graphics();
        line.lineStyle(1, 0xff6b6b, 0.5);
        line.lineStyle(2, 0xff6b6b, 0.5);
        line.beginPath();
        line.moveTo(lastPos.x, lastPos.y);
        line.lineTo(basePos.x, basePos.y);
        line.strokePath();
        this.selectedPathLines.push(line);
    }

    private drawArrow(graphics: Phaser.GameObjects.Graphics, from: {x: number, y: number}, to: {x: number, y: number}): void {
        const angle = Math.atan2(to.y - from.y, to.x - from.x);
        const arrowSize = 8;
        const midX = (from.x + to.x) / 2;
        const midY = (from.y + to.y) / 2;
        
        graphics.fillStyle(0x4ecdc4, 0.8);
        graphics.beginPath();
        graphics.moveTo(midX + Math.cos(angle) * arrowSize, midY + Math.sin(angle) * arrowSize);
        graphics.lineTo(midX + Math.cos(angle + 2.5) * arrowSize, midY + Math.sin(angle + 2.5) * arrowSize);
        graphics.lineTo(midX + Math.cos(angle - 2.5) * arrowSize, midY + Math.sin(angle - 2.5) * arrowSize);
        graphics.closePath();
        graphics.fillPath();
    }

    private clearPathLines(): void {
        this.selectedPathLines.forEach(line => line.destroy());
        this.selectedPathLines = [];
    }

    private clearSelection(): void {
        // ✅ ไม่ต้องเช็ค selectedCrew เพราะอาจเป็น null ได้
        this.selectedCrew = null;
        this.selectedTargets = [];
        this.tempMissions = [];
        this.clearPathLines();
        this.selectedNodeHighlights.forEach(h => h.destroy());
        this.selectedNodeHighlights = [];
        this.missionDisplay.clear();
        this.planningPanel.updateSelection(null, null, 0);
    }

    // ✅ Confirm Chain - เพิ่มทุกภารกิจใน chain เข้า queue (พร้อมคำเตือน)
    private confirmChain(): void {
        // ✅ ตรวจสอบให้แน่ใจว่า selectedCrew ไม่เป็น null
        if (!this.selectedCrew) {
            this.notificationSystem.showError('❌ No crew selected! Please select a crew first.', 2000);
            return;
        }
        
        if (this.selectedTargets.length === 0) {
            this.notificationSystem.showWarning('⚠️ No nodes selected! Click nodes to build a chain.', 2000);
            return;
        }
        
        if (this.timeSystem.isExecuting || this.gameOver) return;
        if (this.timeSystem.isNightPhase) return;
        if (this.isProcessingQueue) return;

        // ✅ เก็บ reference ของ selectedCrew ไว้ใช้
        const crew = this.selectedCrew;

        let totalChainTime = 0;
        const missionsToAdd: { travelTime: number; actionTime: number; totalTime: number; target: ResourceNode }[] = [];
        let lastPosition = crew.position;

        for (let i = 0; i < this.selectedTargets.length; i++) {
            const target = this.selectedTargets[i];
            const distance = this.getDistance(lastPosition, target.position);
            const travelTime = crew.calculateTravelTime(distance);
            const actionTime = target.getActionTime(crew.getEffectiveGathering());
            const totalTime = travelTime * 2 + actionTime;
            
            missionsToAdd.push({ travelTime, actionTime, totalTime, target });
            totalChainTime += totalTime;
            lastPosition = target.position;
        }

        // ✅ คำนวณเวลาที่ใช้ไปแล้วของ crew นี้
        const crewUsedTime = this.missionQueue
            .filter(q => q.crew.id === crew.id && !q.completed)
            .reduce((sum, q) => sum + q.totalTime, 0);
        
        const remainingTime = this.timeSystem.dayTimeLimit - this.timeSystem.elapsedTime - crewUsedTime;
        
        // ✅ ถ้าเวลาไม่พอ → แค่เตือน ไม่ใช่ห้าม
        let isOverTime = false;
        if (totalChainTime > remainingTime) {
            isOverTime = true;
            this.notificationSystem.showWarning(
                `⚠️ ${crew.name}: Chain needs ${Math.floor(totalChainTime)} units but only ${Math.floor(remainingTime)} units left! (Will exceed by ${Math.floor(totalChainTime - remainingTime)} units)`,
                5000
            );
            this.notificationSystem.showWarning(
                `💡 You can still add this chain, but ${crew.name} may not complete all missions!`,
                4000
            );
        }

        // ✅ เพิ่มภารกิจทั้งหมดใน chain เข้า queue (แม้จะเกินเวลา)
        const existingMissions = this.missionQueue.filter(q => q.crew.id === crew.id && !q.completed);

        for (let i = 0; i < missionsToAdd.length; i++) {
            const data = missionsToAdd[i];
            const isChained = i > 0 || existingMissions.length > 0;
            
            const mission: QueuedMission = {
                crew: crew,
                target: data.target,
                travelTime: data.travelTime,
                actionTime: data.actionTime,
                totalTime: data.totalTime,
                phase: 'travel_out',
                progress: 0,
                travelOutDone: false,
                actionDone: false,
                travelBackDone: false,
                usedTime: 0,
                completed: false,
                chainIndex: existingMissions.length + i,
                isChained: isChained,
                nextMission: null
            };
            
            if (i > 0) {
                const prevMission = this.missionQueue[this.missionQueue.length - 1];
                if (prevMission) {
                    prevMission.nextMission = mission;
                }
            }
            
            this.missionQueue.push(mission);
        }

        // ✅ Mark crew as busy
        crew.isBusy = true;
        
        // ✅ อัพเดท UI
        this.updateQueueDisplay();
        this.crewPanel.update();
        this.clearSelection(); // ✅ clearSelection จะตั้ง selectedCrew = null
        
        const chainText = missionsToAdd.length > 1 ? ` (${missionsToAdd.length} nodes chained)` : '';
        const overTimeText = isOverTime ? ' ⚠️ (May exceed time limit!)' : '';
        
        this.planningPanel.setStatus(
            `✅ ${crew.name} assigned${chainText}!${overTimeText} (${this.missionQueue.length} total missions)`, 
            isOverTime ? '#f9ca24' : '#00b894'
        );
        
        this.notificationSystem.showInfo(
            `📋 ${crew.name} assigned ${missionsToAdd.length} mission(s) in chain! Total: ${Math.floor(totalChainTime)} units${isOverTime ? ' ⚠️ (May not finish all!)' : ''}`,
            4000
        );

        // ✅ เช็คว่ายังมี crew ที่ว่างอยู่ไหม
        const availableCrews = this.crewManager.getAvailableCount();
        if (availableCrews > 0 && this.timeSystem.dayTimeLimit - this.timeSystem.elapsedTime > 0) {
            this.notificationSystem.showInfo(`📋 ${availableCrews} crew(s) still available. Assign more or execute!`, 3000);
        }

        // ✅ แสดงปุ่ม Execute เสมอ (ถ้ามีภารกิจในคิว)
        if (this.missionQueue.length > 0) {
            this.planningPanel.showExecuteButton(() => this.executeAllMissions());
        }
    }

    // ✅ จัดกลุ่มภารกิจตาม target node
    private groupMissionsByTarget(): Map<string, QueuedMission[]> {
        const groups = new Map<string, QueuedMission[]>();
        
        for (const mission of this.missionQueue) {
            if (mission.completed) continue;
            const key = mission.target.id;
            if (!groups.has(key)) {
                groups.set(key, []);
            }
            groups.get(key)!.push(mission);
        }
        
        return groups;
    }

    // ✅ executeAllMissions - ทำงานแม้จะเกินเวลา (แต่จะเตือน)
    private executeAllMissions(): void {
        if (this.missionQueue.length === 0) return;
        if (this.isProcessingQueue) return;
        if (this.timeSystem.isExecuting) return;

        // ✅ คำนวณเวลาทั้งหมดและเช็คว่าเกินหรือไม่
        let totalMissionTime = 0;
        let hasOverTimeMission = false;
        const overTimeMissions: string[] = [];

        for (const mission of this.missionQueue) {
            const remainingTime = this.timeSystem.dayTimeLimit - this.timeSystem.elapsedTime;
            if (mission.totalTime > remainingTime) {
                hasOverTimeMission = true;
                overTimeMissions.push(`${mission.crew.name} (needs ${Math.floor(mission.totalTime)}, has ${Math.floor(remainingTime)})`);
            }
            totalMissionTime += mission.totalTime;
        }

        // ✅ ถ้ามีภารกิจที่เกินเวลา → แจ้งเตือน แต่ยังให้ทำได้
        if (hasOverTimeMission) {
            this.notificationSystem.showWarning(
                `⚠️ Some missions may not finish within the day limit!`,
                4000
            );
            // ✅ แสดงรายละเอียด
            for (const msg of overTimeMissions) {
                this.notificationSystem.showWarning(`   ${msg}`, 3000);
            }
            this.notificationSystem.showInfo(
                `💡 Missions will still be executed until time runs out!`,
                3000
            );
        }

        // ✅ เริ่ม execution
        this.isProcessingQueue = true;
        this.createMissionSprites();
        this.timeSystem.startExecution(this);

        this.planningPanel.setStatus(
            `⏳ Executing ${this.missionQueue.length} missions${hasOverTimeMission ? ' ⚠️ (Some may not finish)' : ''}...`, 
            hasOverTimeMission ? '#f9ca24' : '#f9ca24'
        );
        this.planningPanel.hideExecuteButton();
        this.notificationSystem.showInfo(`🚀 Starting ${this.missionQueue.length} missions!${hasOverTimeMission ? ' (Some may not complete)' : ''}`, 2000);
        
        this.updateQueueDisplay();
        this.crewPanel.update();
        this.simulateStep();
    }

    // ✅ simulateStep
    private simulateStep(): void {
        const elapsedTime = this.timeSystem.elapsedTime;
        
        this.timeText.setText(
            `⏱️ Elapsed: ${Math.floor(elapsedTime)} / ${this.timeSystem.dayTimeLimit} units`
        );

        if (this.missionQueue.length === 0) return;

        const groups = this.groupMissionsByTarget();
        let allComplete = true;

        for (const [targetId, missions] of groups) {
            if (missions.length > 1) {
                this.processCollaborativeMissions(missions, elapsedTime);
            } else {
                this.processSingleMission(missions[0], elapsedTime);
            }
            
            for (const mission of missions) {
                if (!mission.completed) {
                    allComplete = false;
                }
            }
        }

        this.updateQueueDisplay();
        this.updateCrewTimeDisplay();

        if (allComplete || elapsedTime >= this.timeSystem.dayTimeLimit) {
            this.finishExecution();
        }
    }

    // ✅ processSingleMission
    private processSingleMission(mission: QueuedMission, elapsedTime: number): void {
        if (mission.completed) return;
        
        mission.usedTime = Math.min(elapsedTime, mission.totalTime);
        const progress = mission.usedTime / mission.totalTime;
        
        this.updateMissionSprite(mission, progress);
        
        const travelOutEnd = mission.travelTime / mission.totalTime;
        const actionEnd = (mission.travelTime + mission.actionTime) / mission.totalTime;
        
        if (progress < travelOutEnd) {
            mission.phase = 'travel_out';
            const p = progress / travelOutEnd;
            this.updateCrewPosition(mission.crew, mission.target, p, 'out');
        } else if (progress < actionEnd) {
            mission.phase = 'action';
            if (!mission.actionDone) {
                mission.actionDone = true;
                this.notificationSystem.showInfo(`📍 ${mission.crew.name} reached ${mission.target.icon} ${mission.target.type}`, 1500);
            }
            this.updateActionSprite(mission, (progress - travelOutEnd) / (actionEnd - travelOutEnd));
        } else if (progress < 1) {
            mission.phase = 'travel_back';
            const p = (progress - actionEnd) / (1 - actionEnd);
            this.updateCrewPosition(mission.crew, mission.target, p, 'back');
        } else {
            mission.phase = 'complete';
            mission.completed = true;
            this.completeMission(mission);
        }
    }

    // ✅ processCollaborativeMissions
    private processCollaborativeMissions(missions: QueuedMission[], elapsedTime: number): void {
        const firstMission = missions[0];
        if (firstMission.completed) return;
        
        const crews = missions.map(m => m.crew);
        const target = firstMission.target;
        
        const totalTravelTime = crews.reduce((sum, c, i) => sum + missions[i].travelTime, 0);
        const avgTravelTime = totalTravelTime / crews.length;
        
        const totalTime = firstMission.totalTime;
        const usedTime = Math.min(elapsedTime, totalTime);
        const progress = usedTime / totalTime;
        
        for (let i = 0; i < missions.length; i++) {
            const mission = missions[i];
            const crew = crews[i];
            
            mission.usedTime = usedTime;
            
            const travelOutEnd = mission.travelTime / totalTime;
            const actionEnd = (mission.travelTime + mission.actionTime) / totalTime;
            
            if (progress < travelOutEnd) {
                mission.phase = 'travel_out';
                const p = progress / travelOutEnd;
                this.updateCrewPosition(crew, target, p, 'out');
            } else if (progress < actionEnd) {
                mission.phase = 'action';
                if (!mission.actionDone) {
                    mission.actionDone = true;
                    this.notificationSystem.showInfo(`📍 ${crew.name} reached ${target.icon} ${target.type}`, 1500);
                }
                this.updateCollaborativeActionSprite(mission, (progress - travelOutEnd) / (actionEnd - travelOutEnd));
            } else if (progress < 1) {
                mission.phase = 'travel_back';
                const p = (progress - actionEnd) / (1 - actionEnd);
                this.updateCrewPosition(crew, target, p, 'back');
            } else {
                mission.phase = 'complete';
                mission.completed = true;
            }
        }
        
        if (progress >= 1) {
            const allCrews = missions.map(m => m.crew);
            const target = firstMission.target;
            
            const result = this.missionSystem.executeCollaborativeMission(
                allCrews,
                target,
                avgTravelTime
            );
            
            for (const mission of missions) {
                mission.completed = true;
                mission.phase = 'complete';
                
                const crew = mission.crew;
                const basePos = this.mapGenerator.getBasePosition();
                crew.position.x = basePos.x - 60 + (crew.id - 1) * 40;
                crew.position.y = basePos.y + 40;
                if (crew.sprite) {
                    crew.sprite.x = crew.position.x;
                    crew.sprite.y = crew.position.y;
                }
                
                this.crewManager.completeMission(crew.id);
                
                const sprite = this.missionSprites.get(crew.id);
                if (sprite) {
                    sprite.destroy();
                    this.missionSprites.delete(crew.id);
                }
            }
            
            if (result.success) {
                this.notificationSystem.showSuccess(result.message, 3000);
            } else {
                this.notificationSystem.showError(result.message, 3000);
            }
            
            this.resourcePanel.update();
            this.crewPanel.update();
            this.checkGameOver();
        }
    }

    private createMissionSprites(): void {
        this.missionSprites.forEach(sprite => sprite.destroy());
        this.missionSprites.clear();

        for (const mission of this.missionQueue) {
            const crew = mission.crew;
            const startX = crew.position.x;
            const startY = crew.position.y;
            
            const sprite = this.add.text(startX, startY, '🧑‍🤝‍🧑', {
                fontSize: '24px',
                backgroundColor: '#1a1a2e',
                padding: { x: 2, y: 2 }
            });
            sprite.setDepth(10);
            this.missionSprites.set(crew.id, sprite);
        }
    }

    private updateMissionSprite(mission: QueuedMission, progress: number): void {
        const sprite = this.missionSprites.get(mission.crew.id);
        if (!sprite) return;
        
        const crew = mission.crew;
        const target = mission.target;
        const basePos = this.mapGenerator.getBasePosition();
        
        let x, y;
        const travelOutEnd = mission.travelTime / mission.totalTime;
        const actionEnd = (mission.travelTime + mission.actionTime) / mission.totalTime;
        
        if (progress < travelOutEnd) {
            const p = progress / travelOutEnd;
            x = crew.position.x + (target.position.x - crew.position.x) * p;
            y = crew.position.y + (target.position.y - crew.position.y) * p;
            sprite.setText('🚶');
        } else if (progress < actionEnd) {
            x = target.position.x;
            y = target.position.y;
            if (mission.target.isRelic) {
                sprite.setText('🔍');
            } else if (mission.target.isMonster) {
                sprite.setText('⚔️');
            } else {
                sprite.setText('⛏️');
            }
        } else if (progress < 1) {
            const p = (progress - actionEnd) / (1 - actionEnd);
            const targetX = basePos.x - 60 + (crew.id - 1) * 40;
            const targetY = basePos.y + 40;
            x = target.position.x + (targetX - target.position.x) * p;
            y = target.position.y + (targetY - target.position.y) * p;
            sprite.setText('🚶');
        } else {
            const targetX = basePos.x - 60 + (crew.id - 1) * 40;
            const targetY = basePos.y + 40;
            x = targetX;
            y = targetY;
            sprite.setText('✅');
        }
        
        sprite.x = x;
        sprite.y = y;
    }

    private updateCrewPosition(crew: Crew, target: ResourceNode, progress: number, direction: 'out' | 'back'): void {
        const basePos = this.mapGenerator.getBasePosition();
        
        if (direction === 'out') {
            const x = crew.position.x + (target.position.x - crew.position.x) * progress;
            const y = crew.position.y + (target.position.y - crew.position.y) * progress;
            if (crew.sprite) {
                crew.sprite.x = x;
                crew.sprite.y = y;
            }
        } else {
            const targetX = basePos.x - 60 + (crew.id - 1) * 40;
            const targetY = basePos.y + 40;
            const x = target.position.x + (targetX - target.position.x) * progress;
            const y = target.position.y + (targetY - target.position.y) * progress;
            if (crew.sprite) {
                crew.sprite.x = x;
                crew.sprite.y = y;
            }
        }
    }

    private updateActionSprite(mission: QueuedMission, progress: number): void {
        const sprite = this.missionSprites.get(mission.crew.id);
        if (!sprite) return;
        
        if (mission.target.isRelic) {
            sprite.setText(progress > 0.7 ? '✨' : '🔍');
        } else if (mission.target.isMonster) {
            sprite.setText(progress > 0.7 ? '💥' : '⚔️');
        } else {
            sprite.setText(progress > 0.7 ? '✅' : '⛏️');
        }
    }

    private updateCollaborativeActionSprite(mission: QueuedMission, progress: number): void {
        const sprite = this.missionSprites.get(mission.crew.id);
        if (!sprite) return;
        
        if (mission.target.isRelic) {
            sprite.setText(progress > 0.7 ? '✨' : '🔍');
        } else if (mission.target.isMonster) {
            sprite.setText(progress > 0.7 ? '💥' : '⚔️');
        } else {
            sprite.setText(progress > 0.7 ? '✅' : '⛏️');
        }
    }

    private completeMission(mission: QueuedMission): void {
        const crew = mission.crew;
        const target = mission.target;
        
        const result = this.missionSystem.executeMission(crew, target);
        
        const basePos = this.mapGenerator.getBasePosition();
        crew.position.x = basePos.x - 60 + (crew.id - 1) * 40;
        crew.position.y = basePos.y + 40;
        if (crew.sprite) {
            crew.sprite.x = crew.position.x;
            crew.sprite.y = crew.position.y;
        }
        
        this.crewManager.completeMission(crew.id);
        
        if (result.success) {
            this.notificationSystem.showSuccess(`${crew.name}: ${result.message}`, 2000);
        } else {
            this.notificationSystem.showError(`${crew.name}: ${result.message}`, 2000);
        }
        
        if (!crew.isAlive) {
            this.notificationSystem.showError(`💀 ${crew.name} has died!`, 3000);
            this.checkGameOver();
        }
        
        this.resourcePanel.update();
        this.crewPanel.update();
        
        const sprite = this.missionSprites.get(crew.id);
        if (sprite) {
            sprite.destroy();
            this.missionSprites.delete(crew.id);
        }
    }

    private finishExecution(): void {
        this.timeSystem.endExecution();
        this.missionQueue = [];
        this.isProcessingQueue = false;
        
        this.updateQueueDisplay();
        this.crewTimeTexts.forEach(text => text.destroy());
        this.crewTimeTexts.clear();
        this.planningPanel.setStatus('✅ All missions complete!', '#00b894');
        this.crewPanel.update();
        this.resourcePanel.update();
        
        // ✅ ซ่อนปุ่ม Execute หลังจากเสร็จ
        this.planningPanel.hideExecuteButton();
        
        this.checkGameOver();
        
        if (!this.gameOver && this.timeSystem.elapsedTime < this.timeSystem.dayTimeLimit) {
            this.notificationSystem.showInfo(`⏱️ ${Math.floor(this.timeSystem.dayTimeLimit - this.timeSystem.elapsedTime)} units remaining. Plan more missions!`, 3000);
            
            // ✅ ถ้ายังมีเวลาอยู่ และยังมี crew ที่ว่าง ให้แสดงปุ่ม Execute อีกครั้ง
            const availableCrews = this.crewManager.getAvailableCount();
            if (availableCrews > 0) {
                this.notificationSystem.showInfo(`📋 ${availableCrews} crew(s) available. Assign missions!`, 3000);
            }
        }
    }

    private cancelAllMissions(): void {
        for (const mission of this.missionQueue) {
            const crew = mission.crew;
            crew.isBusy = false;
            this.crewManager.completeMission(crew.id);
            
            const sprite = this.missionSprites.get(crew.id);
            if (sprite) {
                sprite.destroy();
                this.missionSprites.delete(crew.id);
            }
        }
        
        this.missionQueue = [];
        this.isProcessingQueue = false;
        this.timeSystem.endExecution();
        this.updateQueueDisplay();
        this.crewTimeTexts.forEach(text => text.destroy());
        this.crewTimeTexts.clear();
        this.crewPanel.update();
        this.planningPanel.setStatus('❌ Missions cancelled!', '#ff6b6b');
        this.notificationSystem.showError('❌ All missions cancelled due to insufficient time!', 3000);
    }

    private updateQueueDisplay(): void {
        const totalTime = this.missionQueue.reduce((sum, q) => sum + q.totalTime, 0);
        const remainingTime = this.timeSystem.dayTimeLimit - this.timeSystem.elapsedTime;
        
        const chainCount = this.missionQueue.filter(q => q.isChained && q.chainIndex === 0).length;
        const chainText = chainCount > 0 ? ` | 🔗 Chains: ${chainCount}` : '';
        
        this.queueText.setText(
            `📋 Missions: ${this.missionQueue.length} | Total Time: ${Math.floor(totalTime)} units${chainText} | ` +
            `⏱️ Elapsed: ${Math.floor(this.timeSystem.elapsedTime)}/${this.timeSystem.dayTimeLimit} units | ` +
            `⏳ Remaining: ${Math.floor(remainingTime)} units`
        );
    }

    private updateCrewTimeDisplay(): void {
        this.crewTimeTexts.forEach(text => text.destroy());
        this.crewTimeTexts.clear();

        let y = 70;
        for (const mission of this.missionQueue) {
            const crew = mission.crew;
            const elapsed = Math.min(mission.usedTime, mission.totalTime);
            const progress = (elapsed / mission.totalTime * 100);
            
            let statusText = '⏳ Waiting';
            if (mission.phase === 'travel_out') statusText = '🚶 Traveling to target';
            else if (mission.phase === 'action') statusText = '⚡ Performing action';
            else if (mission.phase === 'travel_back') statusText = '🚶 Returning to base';
            else if (mission.phase === 'complete') statusText = '✅ Complete';

            const text = this.add.text(
                GAME_CONFIG.WIDTH - 420,
                y,
                `${crew.name}: ${Math.floor(elapsed)}/${Math.floor(mission.totalTime)} units (${Math.floor(progress)}%) - ${statusText}`,
                {
                    fontSize: '14px',
                    color: mission.phase === 'complete' ? '#00b894' : '#4ecdc4',
                    fontFamily: 'monospace',
                    backgroundColor: '#1a1a2e',
                    padding: { x: 5, y: 2 }
                }
            );
            this.crewTimeTexts.set(crew.id, text);
            y += 28;
        }
    }

    private getDistance(pos1: { x: number; y: number }, pos2: { x: number; y: number }): number {
        return Math.sqrt(Math.pow(pos1.x - pos2.x, 2) + Math.pow(pos1.y - pos2.y, 2));
    }

    private startNightPhase(): void {
        this.timeSystem.startNight(this);
        this.planningPanel.setStatus('🌙 NIGHT PHASE - Defend the base!', '#ff6b6b');
        this.planningPanel.setConfirmVisible(false);
        this.cameras.main.setBackgroundColor('#0a0a15');

        const monsterCount = 2 + Math.floor(this.timeSystem.day / 3);
        this.notificationSystem.showWarning(`🌙 Night falls! ${monsterCount} monsters attacking!`, 2000);
        this.spawnNightMonsters(monsterCount);
    }

    private spawnNightMonsters(count: number): void {
        const basePos = this.mapGenerator.getBasePosition();
        let monstersSpawned = 0;

        const spawnMonster = () => {
            if (monstersSpawned >= count || this.gameOver) return;

            const angle = Math.random() * Math.PI * 2;
            const distance = 80 + Math.random() * 120;
            const x = basePos.x + Math.cos(angle) * distance;
            const y = basePos.y + Math.sin(angle) * distance;

            const monster = this.add.arc(x, y, 15, 0, 360, false, 0xe74c3c);
            monster.setStrokeStyle(2, 0xff0000, 0.5);
            
            const monsterIcon = this.add.text(x - 10, y - 12, '👹', { fontSize: '24px' });

            this.tweens.add({
                targets: [monster, monsterIcon],
                x: basePos.x + (Math.random() - 0.5) * 40,
                y: basePos.y + (Math.random() - 0.5) * 40,
                duration: 3000 + Math.random() * 2000,
                ease: 'Linear',
                onComplete: () => {
                    monster.destroy();
                    monsterIcon.destroy();

                    const damage = 5 + Math.floor(Math.random() * 10);
                    const destroyed = this.resourceManager.takeBaseDamage(damage);
                    
                    this.showDamageEffect(damage);
                    this.baseHPText.setText(`🏠 HP: ${Math.floor(this.resourceManager.baseHP)}`);

                    const lost = this.resourceManager.loseDayResources();
                    if (Object.keys(lost).length > 0) {
                        this.notificationSystem.showError(`⚔️ Lost resources to monsters!`, 2000);
                        this.resourcePanel.update();
                    }

                    if (destroyed) {
                        this.showGameOver('💀 Base Destroyed!');
                    }
                }
            });

            monstersSpawned++;
        };

        this.time.addEvent({
            delay: GAME_CONFIG.MONSTER_SPAWN_INTERVAL,
            callback: spawnMonster,
            callbackScope: this,
            repeat: count - 1
        });
    }

    private showDamageEffect(damage: number): void {
        const basePos = this.mapGenerator.getBasePosition();
        const text = this.add.text(
            basePos.x - 30,
            basePos.y - 60,
            `-${Math.floor(damage)} HP!`,
            {
                fontSize: '28px',
                color: '#ff0000',
                fontFamily: 'monospace'
            }
        ).setOrigin(0.5);

        this.tweens.add({
            targets: text,
            y: text.y - 50,
            alpha: 0,
            duration: 1500,
            onComplete: () => text.destroy()
        });

        this.cameras.main.shake(200, 0.02);
    }

    private startNewDay(): void {
        this.timeSystem.day++;
        
        if (this.timeSystem.day > GAME_CONFIG.MAX_DAYS) {
            this.showGameOver('🎉 YOU WIN! Survived 30 days!', true);
            return;
        }

        const aliveCrews = this.crewManager.getAllAlive();
        const foodResult = this.resourceManager.consumeFood(aliveCrews.length);
        if (!foodResult.success) {
            this.showGameOver(`🍽️ No Food! (Need ${foodResult.needed})`);
            return;
        }

        this.resourceManager.resetDay();
        this.mapGenerator.generateResources();
        const basePos = this.mapGenerator.getBasePosition();

        this.baseSprite.x = basePos.x;
        this.baseSprite.y = basePos.y;

        aliveCrews.forEach((crew, index) => {
            crew.position.x = basePos.x - 60 + index * 40;
            crew.position.y = basePos.y + 40;
            if (crew.sprite) {
                crew.sprite.x = crew.position.x;
                crew.sprite.y = crew.position.y;
            }
        });

        this.drawMap();

        this.resourcePanel.update();
        this.crewPanel.update();
        this.dayText.setText(`Day ${this.timeSystem.day}/${GAME_CONFIG.MAX_DAYS}`);
        this.baseHPText.setText(`🏠 HP: ${Math.floor(this.resourceManager.baseHP)}`);
        this.cameras.main.setBackgroundColor('#1a1a2e');

        this.timeSystem.startDay(this);
        this.planningPanel.setConfirmVisible(true);
        this.planningPanel.setStatus(`🌞 Day ${this.timeSystem.day} - Plan your missions!`, '#4ecdc4');
        this.notificationSystem.showInfo(`🌞 Day ${this.timeSystem.day} begins!`, 2000);
        
        // ✅ รีเซ็ตปุ่ม Execute (ซ่อนไว้ก่อน)
        this.planningPanel.hideExecuteButton();
    }

    private checkGameOver(): void {
        if (this.gameOver) return;

        const aliveCrews = this.crewManager.getAllAlive();
        if (aliveCrews.length === 0) {
            this.showGameOver('💀 All Crew Dead!');
            return;
        }

        if (this.resourceManager.resources.food <= 0) {
            this.showGameOver('🍽️ No Food!');
            return;
        }
    }

    private showGameOver(message: string, isWin: boolean = false): void {
        this.gameOver = true;
        this.isWin = isWin;
        this.scene.start('GameOverScene', { message, isWin, day: this.timeSystem.day });
    }

    update(): void {
        if (this.timeSystem.isExecuting && this.isProcessingQueue) {
            this.timeText.setText(
                `⏱️ Elapsed: ${Math.floor(this.timeSystem.elapsedTime)} / ${this.timeSystem.dayTimeLimit} units`
            );
        }
        
        this.baseHPText.setText(`🏠 HP: ${Math.floor(this.resourceManager.baseHP)}`);
        
        if (!this.timeSystem.isExecuting) {
            this.updateQueueDisplay();
            
            // ✅ ถ้ามีภารกิจในคิวและยังไม่กำลัง execute ให้แสดงปุ่ม Execute
            if (this.missionQueue.length > 0 && !this.isProcessingQueue && !this.timeSystem.isExecuting) {
                // ตรวจสอบว่าปุ่ม Execute ถูกแสดงอยู่หรือยัง
                if (!this.planningPanel.isExecuteButtonVisible()) {
                    this.planningPanel.showExecuteButton(() => this.executeAllMissions());
                }
            }
        }
    }
}