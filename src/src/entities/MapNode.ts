// src/entities/MapNode.ts
import { NodeType, MapNode as IMapNode } from '../state/types';
import { GAME_CONFIG, RESOURCE_ICONS } from '../config';
import { RandomGenerator } from '../utils/RandomGenerator';

export class MapNode implements IMapNode {
    public id: string;
    public type: NodeType;
    public position: { x: number; y: number };
    public connections: string[];
    
    // Resources
    public resourceType?: string;
    public resourceAmount?: { min: number; max: number };
    public rarity?: 'common' | 'uncommon' | 'rare' | 'epic' | 'legendary';
    
    // Monster
    public monsterType?: string;
    public monsterRank?: number;
    public monsterAmount?: { min: number; max: number };
    
    // Relic
    public relicType?: string;
    public relicSize?: 'small' | 'medium' | 'large' | 'huge';
    public threat?: number;
    
    // Shop
    public shopType?: 'general' | 'mercenary' | 'trade';
    
    // State
    public isDiscovered: boolean = false;
    public isVisited: boolean = false;
    
    // Visual (ใช้ใน WorldScene)
    public sprite: Phaser.GameObjects.Arc | null = null;
    public icon: Phaser.GameObjects.Text | null = null;
    public label: Phaser.GameObjects.Text | null = null;
    
    constructor(data: Partial<MapNode>) {
        this.id = data.id || `node_${Date.now()}`;
        this.type = data.type || 'resource';
        this.position = data.position || { x: 0, y: 0 };
        this.connections = data.connections || [];
        
        // Resources
        this.resourceType = data.resourceType;
        this.resourceAmount = data.resourceAmount;
        this.rarity = data.rarity;
        
        // Monster
        this.monsterType = data.monsterType;
        this.monsterRank = data.monsterRank;
        this.monsterAmount = data.monsterAmount;
        
        // Relic
        this.relicType = data.relicType;
        this.relicSize = data.relicSize;
        this.threat = data.threat;
        
        // Shop
        this.shopType = data.shopType;
        
        // State
        this.isDiscovered = data.isDiscovered || false;
        this.isVisited = data.isVisited || false;
    }
    
    // ============================================================
    // FACTORY METHODS
    // ============================================================
    static createStart(id: string, position: { x: number; y: number }): MapNode {
        return new MapNode({
            id,
            type: 'start',
            position,
            isDiscovered: true,
            isVisited: true,
        });
    }
    
    static createFinish(id: string, position: { x: number; y: number }): MapNode {
        return new MapNode({
            id,
            type: 'finish',
            position,
        });
    }
    
    static createJunction(id: string, position: { x: number; y: number }): MapNode {
        return new MapNode({
            id,
            type: 'junction',
            position,
        });
    }
    
    static createResource(id: string, position: { x: number; y: number }): MapNode {
        const rng = RandomGenerator.getInstance();
        
        const resourceType = rng.randomResourceType();
        const range = GAME_CONFIG.RESOURCE_TYPE_RANGE[resourceType as keyof typeof GAME_CONFIG.RESOURCE_TYPE_RANGE] || { min: 10, max: 20 };
        const rarity = rng.randomRarity();
        
        return new MapNode({
            id,
            type: 'resource',
            position,
            resourceType,
            resourceAmount: range,
            rarity,
        });
    }
    
    static createRelic(id: string, position: { x: number; y: number }): MapNode {
        const rng = RandomGenerator.getInstance();
        
        const relicSizes: Array<'small' | 'medium' | 'large' | 'huge'> = 
            ['small', 'medium', 'large', 'huge'];
        const size = rng.pick(relicSizes);
        const threat = rng.randomRange(0, 10);
        
        return new MapNode({
            id,
            type: 'relic',
            position,
            relicSize: size,
            threat,
        });
    }
    
    static createMonster(id: string, position: { x: number; y: number }): MapNode {
        const rng = RandomGenerator.getInstance();
        
        const monsterType = rng.randomMonsterType();
        const monsterData = GAME_CONFIG.MONSTER_TYPES[monsterType as keyof typeof GAME_CONFIG.MONSTER_TYPES];
        const rank = monsterData.rank;
        
        const amountRange = GAME_CONFIG.MONSTER_AMOUNT_BY_RANK[rank as keyof typeof GAME_CONFIG.MONSTER_AMOUNT_BY_RANK];
        
        return new MapNode({
            id,
            type: 'monster',
            position,
            monsterType,
            monsterRank: rank,
            monsterAmount: amountRange,
        });
    }
    
    static createShop(id: string, position: { x: number; y: number }): MapNode {
        const rng = RandomGenerator.getInstance();
        
        const shopTypes: Array<'general' | 'mercenary' | 'trade'> = 
            ['general', 'mercenary', 'trade'];
        const shopType = rng.pick(shopTypes);
        
        return new MapNode({
            id,
            type: 'shop',
            position,
            shopType,
        });
    }
    
    static createEncounter(id: string, position: { x: number; y: number }): MapNode {
        return new MapNode({
            id,
            type: 'encounter',
            position,
        });
    }
    
    // ============================================================
    // UTILITY METHODS
    // ============================================================
    
    // ✅ ดึงสีตาม type
    getColor(): number {
        switch (this.type) {
            case 'start': return 0x00b894;
            case 'finish': return 0xff6b6b;
            case 'junction': return 0x636e72;
            case 'resource': return 0x4ecdc4;
            case 'relic': return 0x6c5ce7;
            case 'monster': return 0xe74c3c;
            case 'shop': return 0xf9ca24;
            case 'encounter': return 0xff9f43;
            default: return 0xffffff;
        }
    }
    
    // ✅ ดึง icon
    getIcon(): string {
        switch (this.type) {
            case 'start': return '🚀';
            case 'finish': return '🏁';
            case 'junction': return '🔀';
            case 'resource': 
                return RESOURCE_ICONS[this.resourceType as keyof typeof RESOURCE_ICONS] || '📦';
            case 'relic': return '🏛️';
            case 'monster': return '👹';
            case 'shop': return '🏪';
            case 'encounter': return '❓';
            default: return '❓';
        }
    }
    
    // ✅ ดึงชื่อ
    getLabel(): string {
        switch (this.type) {
            case 'start': return 'Start';
            case 'finish': return 'Finish';
            case 'junction': return 'Junction';
            case 'resource': 
                return this.resourceType ? this.resourceType.replace('_', ' ') : 'Resource';
            case 'relic': 
                return this.relicSize ? `${this.relicSize} Relic` : 'Relic';
            case 'monster': 
                return this.monsterType ? `Monster ${this.monsterType}` : 'Monster';
            case 'shop': 
                return this.shopType ? `${this.shopType} Shop` : 'Shop';
            case 'encounter': return 'Encounter';
            default: return 'Unknown';
        }
    }
    
    // ✅ ดึง size (สำหรับแสดงผล)
    getSize(): number {
        switch (this.type) {
            case 'start': return 28;
            case 'finish': return 28;
            case 'junction': return 24;
            case 'resource': return 20;
            case 'relic': return 22;
            case 'monster': return 22;
            case 'shop': return 22;
            case 'encounter': return 20;
            default: return 20;
        }
    }
    
    // ✅ เพิ่ม connection
    addConnection(nodeId: string): void {
        if (!this.connections.includes(nodeId)) {
            this.connections.push(nodeId);
        }
    }
    
    // ✅ ลบ connection
    removeConnection(nodeId: string): void {
        this.connections = this.connections.filter(id => id !== nodeId);
    }
}