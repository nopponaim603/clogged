// src/scenes/TravelScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { CrewManager } from '../systems/CrewManager';
import { ResourceManager } from '../systems/ResourceManager';
import { MapGenerator } from '../systems/MapGenerator';
import { TimeSystem } from '../systems/TimeSystem';
import { NotificationSystem } from '../ui/NotificationSystem';
import { RESOURCE_DATA } from '../data/ResourceData';

// ✅ Node Interface
interface MapNode {
    id: string;
    x: number;
    y: number;
    type: 'start' | 'resource' | 'relic' | 'monster' | 'shop' | 'end';
    label: string;
    icon: string;
    color: number;
    isVisited: boolean;
    isCurrent: boolean;
    isLocked: boolean;
    connections: string[];
    resourceType?: string;
    resourceAmount?: number;
    relicName?: string;
    relicRarity?: string;
    monsterName?: string;
    monsterLevel?: number;
    shopItems?: { name: string; cost: number; type: string }[];
    bonusChance?: number;
    bonusAmount?: number;
    dayIndex?: number;
}

// ✅ สถานะการเดินทาง
interface TravelState {
    currentNodeId: string;
    visitedNodeIds: string[];
    day: number;
    mapNodes: MapNode[];
    colXPositions: number[];
}

export class TravelScene extends Scene {
    // === Systems ===
    private crewManager!: CrewManager;
    private resourceManager!: ResourceManager;
    private mapGenerator!: MapGenerator;
    private timeSystem!: TimeSystem;
    private notificationSystem!: NotificationSystem;
    
    // === Map ===
    private nodes: MapNode[] = [];
    private currentNodeId: string = '';
    private visitedNodeIds: string[] = [];
    private pathLines: Phaser.GameObjects.Graphics[] = [];
    private nodeSprites: Map<string, Phaser.GameObjects.Arc> = new Map();
    private nodeLabels: Map<string, Phaser.GameObjects.Text> = new Map();
    private dayLines: Phaser.GameObjects.Graphics[] = [];
    private dayLabels: Phaser.GameObjects.Text[] = [];
    
    // === UI ===
    private dayText!: Phaser.GameObjects.Text;
    private nodeInfoText!: Phaser.GameObjects.Text;
    private resourceTexts: Map<string, Phaser.GameObjects.Text> = new Map();
    
    // === State ===
    private day: number = 1;
    private isTransitioning: boolean = false;
    private colXPositions: number[] = [];
    private travelState: TravelState | null = null;
    private isReturningFromNight: boolean = false;
    private isMapGenerated: boolean = false;
    private actualDay: number = 0;

    constructor() {
        super('TravelScene');
    }

    init(data: any): void {
        if (data) {
            this.crewManager = data.crewManager;
            this.resourceManager = data.resourceManager;
            this.mapGenerator = data.mapGenerator;
            this.timeSystem = data.timeSystem;
            this.day = data.day || 1;
            this.actualDay = data.actualDay || 0;
            
            this.travelState = data.travelState || null;
            this.isReturningFromNight = data.isReturningFromNight || false;
            
            // ✅ รีเซ็ต isTransitioning เมื่อกลับมา
            this.isTransitioning = false;
        }
        this.notificationSystem = new NotificationSystem(this);
    }

    create(): void {
        this.cameras.main.setBackgroundColor('#0a0a1a');
        
        // ✅ ถ้ากลับมาจาก NightScene และมี travelState → ใช้ map ที่มีอยู่
        if (this.isReturningFromNight && this.travelState) {
            this.loadTravelState(this.travelState);
            this.isMapGenerated = true;
            // ✅ รีเซ็ต isTransitioning
            this.isTransitioning = false;
        } 
        // ✅ ถ้ายังไม่มี map → สร้าง map ใหม่
        else if (!this.isMapGenerated) {
            this.generateMap();
            this.isMapGenerated = true;
            
            // ✅ ตั้งค่าเริ่มต้น
            const startNode = this.nodes.find(n => n.type === 'start');
            if (startNode) {
                startNode.isCurrent = true;
                startNode.isVisited = true;
                this.currentNodeId = startNode.id;
                this.visitedNodeIds.push(startNode.id);
            }
            this.updateNodeLockState();
        }
        
        // ✅ อัพเดท actualDay จาก current node
        const currentNode = this.nodes.find(n => n.id === this.currentNodeId);
        if (currentNode && currentNode.dayIndex !== undefined) {
            this.actualDay = currentNode.dayIndex;
            this.day = this.actualDay;
        }
        
        this.setupUI();
        this.drawMap();
        this.drawDayDividers();
        this.setupKeyboardShortcuts();
        
        this.showCurrentStatus();
        
        this.notificationSystem?.showInfo(`🗺️ Day ${this.actualDay} - Click a node to travel!`, 3000);
        
        // ✅ รีเซ็ต isTransitioning อีกครั้งหลังจากสร้าง UI เสร็จ
        this.isTransitioning = false;
    }

    // ============================================================
    // TRAVEL STATE MANAGEMENT
    // ============================================================
    private saveTravelState(): TravelState {
        return {
            currentNodeId: this.currentNodeId,
            visitedNodeIds: this.visitedNodeIds,
            day: this.actualDay,
            mapNodes: this.nodes,
            colXPositions: this.colXPositions
        };
    }

    private loadTravelState(state: TravelState): void {
        console.log('📂 Loading travel state:', state);
        
        if (state.mapNodes && state.mapNodes.length > 0) {
            this.nodes = state.mapNodes;
        }
        
        if (state.colXPositions && state.colXPositions.length > 0) {
            this.colXPositions = state.colXPositions;
        }
        
        this.currentNodeId = state.currentNodeId;
        this.visitedNodeIds = state.visitedNodeIds || [];
        this.actualDay = state.day;
        this.day = state.day;
        
        this.nodes.forEach(node => {
            if (node.id === this.currentNodeId) {
                node.isCurrent = true;
                node.isVisited = true;
            } else if (this.visitedNodeIds.includes(node.id)) {
                node.isVisited = true;
            }
        });
        
        this.updateNodeLockState();
        this.isTransitioning = false;
        
        console.log(`📍 Current Node: ${this.currentNodeId}, Visited: ${this.visitedNodeIds.length} nodes, Day: ${this.actualDay}`);
    }

    // ============================================================
    // MAP GENERATION
    // ============================================================
    private generateMap(): void {
        console.log('🗺️ Generating new map...');
        
        const centerX = this.cameras.main.width / 2;
        const centerY = this.cameras.main.height / 2 - 20;
        
        // ✅ 6 คอลัมน์: Day 0 (Start) → Day 1 → Day 2 → Day 3 → Day 4 → Day 5 (Goal)
        const cols = [
            { x: 60, count: 1, type: 'start', day: 0 },
            { x: 220, count: 3 + Math.floor(Math.random() * 2), types: ['resource', 'resource', 'resource', 'relic'], day: 1 },
            { x: 400, count: 4 + Math.floor(Math.random() * 2), types: ['resource', 'resource', 'relic', 'monster', 'shop'], day: 2 },
            { x: 580, count: 3 + Math.floor(Math.random() * 2), types: ['resource', 'resource', 'monster', 'relic'], day: 3 },
            { x: 760, count: 3 + Math.floor(Math.random() * 2), types: ['resource', 'relic', 'monster'], day: 4 },
            { x: 960, count: 1, type: 'end', day: 5 }
        ];
        
        this.colXPositions = cols.map(col => col.x);
        
        let nodeId = 0;
        let prevNodes: string[] = [];
        let day4Nodes: string[] = []; // ✅ เก็บโหนด Day 4
        let endNodeId: string = '';
        
        cols.forEach((col, colIndex) => {
            const currentNodes: string[] = [];
            const isFirstCol = colIndex === 0;
            const isLastCol = colIndex === cols.length - 1;
            
            let startY: number;
            if (isFirstCol || isLastCol) {
                startY = centerY;
            } else {
                const spacing = 120;
                const totalHeight = (col.count - 1) * spacing;
                startY = centerY - totalHeight / 2;
            }
            
            for (let i = 0; i < col.count; i++) {
                const id = `node_${nodeId}`;
                const x = col.x;
                const y = isFirstCol || isLastCol ? startY : startY + i * 120 + (Math.random() - 0.5) * 20;
                
                let type: MapNode['type'] = 'resource';
                let label = '🌲 Forest';
                let icon = '🌲';
                let color = 0x4ecdc4;
                let resourceType: string | undefined;
                let resourceAmount: number | undefined;
                let bonusChance: number | undefined;
                let bonusAmount: number | undefined;
                let dayIndex = col.day;
                
                if (isFirstCol) {
                    type = 'start';
                    label = '🚀 Start';
                    icon = '🚀';
                    color = 0x00b894;
                } else if (isLastCol) {
                    type = 'end';
                    label = '🏁 Goal';
                    icon = '🏁';
                    color = 0xff6b6b;
                    endNodeId = id; // ✅ เก็บ end node id
                } else {
                    const availableTypes = col.types || ['resource', 'resource', 'resource', 'relic', 'monster', 'shop'];
                    const typeIndex = Math.floor(Math.random() * availableTypes.length);
                    type = availableTypes[typeIndex] as MapNode['type'];
                    
                    switch(type) {
                        case 'resource':
                            const resourceKeys = Object.keys(RESOURCE_DATA);
                            const resKey = resourceKeys[Math.floor(Math.random() * resourceKeys.length)];
                            const resData = RESOURCE_DATA[resKey];
                            resourceType = resKey;
                            resourceAmount = 30 + Math.floor(Math.random() * 120);
                            bonusChance = 10 + Math.floor(Math.random() * 30);
                            bonusAmount = Math.floor(resourceAmount * (0.2 + Math.random() * 0.6));
                            label = `${resData.icon} ${resData.name}`;
                            icon = resData.icon;
                            color = this.getColorFromHex(resData.color);
                            break;
                        case 'relic':
                            label = '🏛️ Ruins';
                            icon = '🏛️';
                            color = 0x6c5ce7;
                            break;
                        case 'monster':
                            label = '👹 Monster Lair';
                            icon = '👹';
                            color = 0xe74c3c;
                            break;
                        case 'shop':
                            label = '🏪 Shop';
                            icon = '🏪';
                            color = 0xf9ca24;
                            break;
                    }
                }
                
                const node: MapNode = {
                    id,
                    x,
                    y,
                    type,
                    label,
                    icon,
                    color,
                    isVisited: false,
                    isCurrent: false,
                    isLocked: colIndex > 1,
                    connections: [],
                    resourceType,
                    resourceAmount,
                    bonusChance,
                    bonusAmount,
                    relicName: type === 'relic' ? ['Ancient Crown', 'Mystic Orb', 'Elder Staff', 'Phoenix Feather', 'Dragon Scale'][Math.floor(Math.random() * 5)] : undefined,
                    relicRarity: type === 'relic' ? ['Common', 'Uncommon', 'Rare', 'Epic', 'Legendary'][Math.floor(Math.random() * 5)] : undefined,
                    monsterName: type === 'monster' ? ['Goblin', 'Wolf', 'Giant Spider', 'Shadow Demon', 'Night Stalker'][Math.floor(Math.random() * 5)] : undefined,
                    monsterLevel: type === 'monster' ? 1 + Math.floor(Math.random() * 3) : undefined,
                    shopItems: type === 'shop' ? [
                        { name: 'Heal Potion', cost: 30, type: 'heal' },
                        { name: 'Speed Boost', cost: 50, type: 'speed' },
                        { name: 'Armor', cost: 80, type: 'armor' }
                    ] : undefined,
                    dayIndex: dayIndex
                };
                
                this.nodes.push(node);
                currentNodes.push(id);
                
                // ✅ เก็บโหนด Day 4
                if (dayIndex === 4) {
                    day4Nodes.push(id);
                }
                
                // ✅ เชื่อมต่อกับคอลัมน์ก่อนหน้า (ยกเว้นคอลัมน์แรก)
                if (!isFirstCol && prevNodes.length > 0) {
                    const connectCount = Math.min(prevNodes.length, 2);
                    for (let j = 0; j < connectCount; j++) {
                        const prevIdx = Math.floor(Math.random() * prevNodes.length);
                        const prevId = prevNodes[prevIdx];
                        if (!node.connections.includes(prevId)) {
                            node.connections.push(prevId);
                            const prevNode = this.nodes.find(n => n.id === prevId);
                            if (prevNode && !prevNode.connections.includes(id)) {
                                prevNode.connections.push(id);
                            }
                        }
                    }
                }
                
                nodeId++;
            }
            
            prevNodes = currentNodes;
        });
        
        // ✅ **บังคับเชื่อมต่อทุกโหนด Day 4 → End Node**
        console.log(`🔗 Connecting Day 4 nodes (${day4Nodes.length} nodes) to End Node (${endNodeId})...`);
        
        const endNode = this.nodes.find(n => n.id === endNodeId);
        if (endNode) {
            day4Nodes.forEach(day4Id => {
                const day4Node = this.nodes.find(n => n.id === day4Id);
                if (day4Node) {
                    // ✅ เชื่อมต่อ day4 → end
                    if (!day4Node.connections.includes(endNodeId)) {
                        day4Node.connections.push(endNodeId);
                    }
                    // ✅ เชื่อมต่อ end → day4 (双向)
                    if (!endNode.connections.includes(day4Id)) {
                        endNode.connections.push(day4Id);
                    }
                    console.log(`   ✅ ${day4Node.label} (${day4Id}) → ${endNode.label} (${endNodeId})`);
                }
            });
        }
        
        console.log(`🗺️ Map generated with ${this.nodes.length} nodes`);
        console.log(`📐 colXPositions: ${this.colXPositions}`);
    }

    private getColorFromHex(hex: string): number {
        const clean = hex.replace('#', '');
        return parseInt(clean, 16);
    }

    private updateNodeLockState(): void {
        this.nodes.forEach(node => {
            if (this.visitedNodeIds.includes(node.id)) {
                node.isLocked = false;
                return;
            }
            
            const hasVisitedConnection = node.connections.some(connId => 
                this.visitedNodeIds.includes(connId)
            );
            node.isLocked = !hasVisitedConnection;
        });
        
        const startNode = this.nodes.find(n => n.type === 'start');
        if (startNode) {
            startNode.isLocked = false;
        }
    }

    // ============================================================
    // DRAW MAP
    // ============================================================
    private drawMap(): void {
        this.pathLines.forEach(line => line.destroy());
        this.pathLines = [];
        this.nodeSprites.forEach(sprite => sprite.destroy());
        this.nodeSprites.clear();
        this.nodeLabels.forEach(label => label.destroy());
        this.nodeLabels.clear();
        
        this.nodes.forEach(node => {
            node.connections.forEach(connId => {
                const target = this.nodes.find(n => n.id === connId);
                if (!target) return;
                
                const isActive = this.visitedNodeIds.includes(node.id) && this.visitedNodeIds.includes(target.id);
                const color = isActive ? 0x4ecdc4 : 0x4a4a5a;
                const alpha = isActive ? 0.8 : 0.3;
                
                const line = this.add.graphics();
                line.lineStyle(2, color, alpha);
                line.beginPath();
                line.moveTo(node.x, node.y);
                line.lineTo(target.x, target.y);
                line.strokePath();
                this.pathLines.push(line);
            });
        });
        
        this.nodes.forEach(node => {
            const isVisited = this.visitedNodeIds.includes(node.id);
            const isCurrent = node.id === this.currentNodeId;
            const isLocked = node.isLocked && !isVisited;
            
            const size = isCurrent ? 30 : node.type === 'start' ? 24 : node.type === 'end' ? 24 : 20;
            const arc = this.add.arc(node.x, node.y, size, 0, 360, false, node.color, 0.8);
            
            const strokeColor = isCurrent ? 0xffffff : isLocked ? 0x4a4a5a : 0xffffff;
            const strokeAlpha = isLocked ? 0.3 : 0.8;
            arc.setStrokeStyle(isCurrent ? 4 : 2, strokeColor, strokeAlpha);
            
            if (isLocked) {
                arc.setAlpha(0.3);
            }
            
            if (isVisited && !isCurrent && node.type !== 'start') {
                const checkMark = this.add.text(node.x - 8, node.y - 8, '✅', { fontSize: '12px' });
                this.nodeLabels.set(node.id + '_check', checkMark);
            }
            
            // ✅ ทำให้โหนดที่สามารถคลิกได้ interactive
            if (!isLocked && !isCurrent && !isVisited) {
                arc.setInteractive();
                arc.on('pointerdown', () => {
                    console.log(`🖱️ Clicked node: ${node.id} (${node.label})`);
                    this.selectNode(node.id);
                });
                arc.on('pointerover', () => {
                    arc.setStrokeStyle(4, 0xf9ca24, 0.8);
                });
                arc.on('pointerout', () => {
                    arc.setStrokeStyle(isCurrent ? 4 : 2, 0xffffff, 0.8);
                });
            }
            
            this.nodeSprites.set(node.id, arc);
            
            const labelText = isCurrent ? `📍 ${node.icon}` : node.icon;
            const label = this.add.text(node.x - 14, node.y - (isCurrent ? 44 : 34), labelText, {
                fontSize: isCurrent ? '26px' : '20px',
                color: isLocked ? '#636e72' : '#ffffff',
                fontFamily: 'monospace'
            });
            this.nodeLabels.set(node.id, label);
            
            const nameText = this.add.text(node.x - 35, node.y + (isCurrent ? 38 : 28), 
                isCurrent ? `◄ ${node.label}` : isVisited ? `✅ ${node.label}` : node.label, {
                fontSize: isCurrent ? '12px' : '10px',
                color: isLocked ? '#636e72' : isVisited ? '#4ecdc4' : '#b2bec3',
                fontFamily: 'monospace',
                align: 'center',
                wordWrap: { width: 80 }
            });
            this.nodeLabels.set(node.id + '_name', nameText);
            
            if (node.type === 'resource' && node.bonusChance && node.bonusAmount && !isVisited) {
                const bonusText = this.add.text(node.x - 25, node.y + (isCurrent ? 60 : 48), 
                    `⬆️ +${node.bonusChance}% / +${node.bonusAmount}`, {
                    fontSize: '8px',
                    color: '#f9ca24',
                    fontFamily: 'monospace',
                    align: 'center'
                });
                this.nodeLabels.set(node.id + '_bonus', bonusText);
            }
        });
    }

    // ============================================================
    // DAY DIVIDERS
    // ============================================================
    private drawDayDividers(): void {
        this.dayLines.forEach(line => line.destroy());
        this.dayLines = [];
        this.dayLabels.forEach(label => label.destroy());
        this.dayLabels = [];
        
        if (!this.colXPositions || this.colXPositions.length === 0) {
            this.colXPositions = [60, 220, 400, 580, 760, 960];
        }
        
        const topY = 80;
        const bottomY = 520;
        
        for (let i = 0; i < this.colXPositions.length - 1; i++) {
            const midX = (this.colXPositions[i] + this.colXPositions[i + 1]) / 2;
            
            const line = this.add.graphics();
            line.lineStyle(1, 0x4a4a5a, 0.2);
            line.beginPath();
            line.moveTo(midX, topY);
            line.lineTo(midX, bottomY);
            line.strokePath();
            this.dayLines.push(line);
            
            const dashLine = this.add.graphics();
            dashLine.lineStyle(1, 0x4a4a5a, 0.1);
            for (let y = topY + 10; y < bottomY; y += 20) {
                dashLine.beginPath();
                dashLine.moveTo(midX - 5, y);
                dashLine.lineTo(midX + 5, y + 10);
                dashLine.strokePath();
            }
            this.dayLines.push(dashLine);
        }
        
        const labelY = topY - 15;
        const dayLabels = ['DAY 0', 'DAY 1', 'DAY 2', 'DAY 3', 'DAY 4', 'DAY 5'];
        const dayColors = ['#4ecdc4', '#b2bec3', '#b2bec3', '#b2bec3', '#b2bec3', '#ff6b6b'];
        
        this.colXPositions.forEach((x, index) => {
            const label = this.add.text(x, labelY, dayLabels[index], {
                fontSize: '14px',
                color: dayColors[index],
                fontFamily: 'monospace',
                align: 'center',
                backgroundColor: '#0a0a1a',
                padding: { x: 8, y: 2 }
            }).setOrigin(0.5);
            this.dayLabels.push(label);
        });
    }

    // ============================================================
    // SELECTION
    // ============================================================
    private selectNode(nodeId: string): void {
        if (this.isTransitioning) {
            console.log('⏳ Already transitioning, ignoring click');
            return;
        }
        
        const node = this.nodes.find(n => n.id === nodeId);
        if (!node) {
            console.log('❌ Node not found:', nodeId);
            return;
        }
        
        console.log(`🎯 Selecting node: ${node.id} (${node.label}), dayIndex: ${node.dayIndex}, currentDay: ${this.actualDay}`);
        
        if (node.isLocked) {
            this.notificationSystem?.showWarning(`🔒 ${node.label} is locked!`, 1500);
            return;
        }
        
        if (node.isCurrent) {
            this.notificationSystem?.showInfo(`📍 Already at ${node.label}`, 1500);
            return;
        }
        
        if (node.dayIndex !== undefined && node.dayIndex > this.actualDay + 1) {
            this.notificationSystem?.showWarning(
                `⚠️ Cannot skip to Day ${node.dayIndex}! Travel in order.`,
                2000
            );
            return;
        }
        
        // ✅ ตั้ง isTransitioning เป็น true ก่อนไป DayScene
        this.isTransitioning = true;
        
        const travelState = this.saveTravelState();
        this.goToDayScene(node, travelState);
    }

    // ============================================================
    // GO TO DAY SCENE
    // ============================================================
    private goToDayScene(node: MapNode, travelState: TravelState): void {
        node.isVisited = true;
        node.isCurrent = false;
        
        if (!this.visitedNodeIds.includes(node.id)) {
            this.visitedNodeIds.push(node.id);
        }
        
        const targetDay = node.dayIndex !== undefined ? node.dayIndex : this.actualDay + 1;
        this.actualDay = targetDay;
        
        travelState.currentNodeId = node.id;
        travelState.visitedNodeIds = this.visitedNodeIds;
        travelState.day = targetDay;
        travelState.mapNodes = this.nodes;
        travelState.colXPositions = this.colXPositions;
        
        this.updateNodeLockState();
        
        const bonusData = {
            resourceType: node.resourceType,
            bonusChance: node.bonusChance || 0,
            bonusAmount: node.bonusAmount || 0,
            nodeType: node.type,
            relicName: node.relicName,
            monsterName: node.monsterName,
            day: targetDay
        };
        
        this.notificationSystem?.showInfo(`🚀 Traveling to ${node.label} (Day ${targetDay})...`, 1500);
        
        this.time.delayedCall(500, () => {
            this.scene.start('DayScene', {
                crewManager: this.crewManager,
                resourceManager: this.resourceManager,
                mapGenerator: this.mapGenerator,
                timeSystem: this.timeSystem,
                day: targetDay,
                actualDay: targetDay,
                travelBonus: bonusData,
                selectedNode: node,
                travelState: travelState,
                isReturningFromNight: false
            });
        });
    }

    // ============================================================
    // SHOW STATUS
    // ============================================================
    private showCurrentStatus(): void {
        const currentNode = this.nodes.find(n => n.id === this.currentNodeId);
        if (currentNode) {
            const dayLabel = currentNode.dayIndex !== undefined ? `(Day ${currentNode.dayIndex})` : '';
            const nextNodes = this.nodes.filter(n => 
                n.connections.includes(currentNode.id) && 
                !this.visitedNodeIds.includes(n.id) &&
                !n.isLocked
            );
            const nextText = nextNodes.length > 0 ? 
                `Next: ${nextNodes.map(n => n.label).join(', ')}` : 
                '🏁 Goal reached!';
            
            this.nodeInfoText.setText(
                `📍 Current: ${currentNode.icon} ${currentNode.label} ${dayLabel} | ` +
                `Day: ${this.actualDay} | Visited: ${this.visitedNodeIds.length} nodes | ${nextText}`
            );
        }
    }

    // ============================================================
    // UI SETUP
    // ============================================================
    private setupUI(): void {
        this.dayText = this.add.text(20, 20, 
            `🚀 Day ${this.actualDay} - Navigation Phase`,
            { fontSize: '24px', color: '#ffffff', fontFamily: 'monospace' }
        );

        this.createResourceUI();

        this.nodeInfoText = this.add.text(20, 680, 
            '📍 Click any unlocked node to start the day!', {
            fontSize: '14px', color: '#b2bec3', fontFamily: 'monospace'
        });

        this.add.text(20, 60, 
            '🟢 Unlocked | 🔴 Locked | ⭐ Current | ✅ Visited | ⬆️ Bonus', {
            fontSize: '12px', color: '#636e72', fontFamily: 'monospace'
        });
        
        this.add.text(60, 595, '⬅️ START', {
            fontSize: '14px', color: '#4ecdc4', fontFamily: 'monospace'
        });
        this.add.text(940, 595, '➡️ GOAL', {
            fontSize: '14px', color: '#ff6b6b', fontFamily: 'monospace'
        });
    }

    private createResourceUI(): void {
        const x = GAME_CONFIG.WIDTH - 200;
        let y = 60;
        const resourceTypes = ['wood', 'stone', 'iron', 'food', 'water', 'circuit', 'aluminum'];
        resourceTypes.forEach((type) => {
            const amount = this.resourceManager?.getResource(type) || 0;
            const icon = RESOURCE_DATA[type]?.icon || '📦';
            const text = this.add.text(x, y, 
                `${icon} ${Math.floor(amount)}`,
                { fontSize: '14px', color: '#ffffff', fontFamily: 'monospace' }
            );
            this.resourceTexts.set(type, text);
            y += 26;
        });
    }

    private setupKeyboardShortcuts(): void {
        this.input.keyboard?.on('keydown-ESC', () => {});
    }

    update(): void {
        // ✅ รีเซ็ต isTransitioning ถ้าค้างนานเกินไป (กัน error)
        if (this.isTransitioning) {
            // ถ้า transition ค้างเกิน 10 วินาที ให้รีเซ็ต
            // (ปกติจะถูกเปลี่ยนใน goToDayScene)
        }
    }
}