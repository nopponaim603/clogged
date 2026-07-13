// src/systems/MapGenerator.ts - เฉพาะส่วนที่แก้ไข
import { ResourceNode, ResourceNodeData } from '../entities/ResourceNode';
import { GAME_CONFIG } from '../config';

const RESOURCE_TYPES: string[] = ['wood', 'stone', 'iron', 'food', 'water'];
const RESOURCE_ICONS: { [key: string]: string } = {
    wood: '🪵',
    stone: '🪨', 
    iron: '⛏️',
    food: '🍖',
    water: '💧'
};

export class MapGenerator {
    public resourceNodes: ResourceNode[];
    private baseX: number;
    private baseY: number;

    constructor() {
        this.resourceNodes = [];
        this.baseX = GAME_CONFIG.WIDTH / 2 - 100;
        this.baseY = GAME_CONFIG.HEIGHT / 2 - 80;
    }

    generateResources(): ResourceNode[] {
        this.resourceNodes = [];
        this.baseX = 100 + Math.random() * (GAME_CONFIG.WIDTH - 300);
        this.baseY = 80 + Math.random() * (GAME_CONFIG.HEIGHT - 300);

        const resourceCount = 4 + Math.floor(Math.random() * 4);
        for (let i = 0; i < resourceCount; i++) {
            const type = RESOURCE_TYPES[Math.floor(Math.random() * RESOURCE_TYPES.length)];
            const node = this.createResourceNode(type);
            this.resourceNodes.push(node);
        }

        this.resourceNodes.push(this.createRelicNode());
        this.resourceNodes.push(this.createMonsterNode());

        return this.resourceNodes;
    }

    private createResourceNode(type: string): ResourceNode {
        // ✅ ปรับให้ทรัพยากรอยู่ห่างจากฐานมากขึ้น
        const basePos = this.getBasePosition();
        let x, y;
        let attempts = 0;
        const minDistance = 150; // ระยะห่างขั้นต่ำจากฐาน
        
        do {
            x = 50 + Math.random() * (GAME_CONFIG.WIDTH - 250);
            y = 80 + Math.random() * (GAME_CONFIG.HEIGHT - 250);
            attempts++;
        } while (
            (Math.sqrt(Math.pow(x - basePos.x, 2) + Math.pow(y - basePos.y, 2)) < minDistance) &&
            attempts < 50
        );
        
        const amount = 30 + Math.floor(Math.random() * 120); // เพิ่ม amount
        
        return new ResourceNode({
            id: `res_${Date.now()}_${Math.random()}`,
            type: type,
            x: x,
            y: y,
            amount: amount,
            icon: RESOURCE_ICONS[type] || '📦',
            gatherTime: 3000 + Math.random() * 4000
        });
    }

    private createRelicNode(): ResourceNode {
        const basePos = this.getBasePosition();
        let x, y;
        let attempts = 0;
        const minDistance = 200;
        
        do {
            x = 80 + Math.random() * (GAME_CONFIG.WIDTH - 300);
            y = 100 + Math.random() * (GAME_CONFIG.HEIGHT - 280);
            attempts++;
        } while (
            (Math.sqrt(Math.pow(x - basePos.x, 2) + Math.pow(y - basePos.y, 2)) < minDistance) &&
            attempts < 50
        );

        return new ResourceNode({
            id: `relic_${Date.now()}`,
            type: 'relic',
            x: x,
            y: y,
            amount: 1,
            icon: '🏛️',
            isRelic: true,
            gatherTime: 5000
        });
    }

    private createMonsterNode(): ResourceNode {
        const basePos = this.getBasePosition();
        let x, y;
        let attempts = 0;
        const minDistance = 180;
        
        do {
            x = 120 + Math.random() * (GAME_CONFIG.WIDTH - 350);
            y = 90 + Math.random() * (GAME_CONFIG.HEIGHT - 270);
            attempts++;
        } while (
            (Math.sqrt(Math.pow(x - basePos.x, 2) + Math.pow(y - basePos.y, 2)) < minDistance) &&
            attempts < 50
        );

        return new ResourceNode({
            id: `monster_${Date.now()}`,
            type: 'monster',
            x: x,
            y: y,
            amount: 1,
            icon: '👹',
            isMonster: true,
            difficulty: 1 + Math.floor(Math.random() * 3),
            monsterName: this.getRandomMonsterName(),
            gatherTime: 6000
        });
    }

    private getRandomMonsterName(): string {
        const names = ['Goblin', 'Wolf', 'Giant Spider', 'Shadow Demon', 'Night Stalker', 'Mutant'];
        return names[Math.floor(Math.random() * names.length)];
    }

    getBasePosition(): { x: number; y: number } {
        return { x: this.baseX, y: this.baseY };
    }
}