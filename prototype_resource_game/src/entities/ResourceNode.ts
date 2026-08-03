// src/entities/ResourceNode.ts
import { GAME_CONFIG } from '../config';

export interface ResourceNodeData {
    id: string;
    type: string | 'relic' | 'monster';
    x: number;
    y: number;
    amount: number;
    icon: string;
    isRelic?: boolean;
    isMonster?: boolean;
    difficulty?: number;
    monsterName?: string;
    gatherTime?: number;
    // ✅ เพิ่ม
    hp?: number;
    rank?: number;
}

export class ResourceNode {
    public id: string;
    public type: string | 'relic' | 'monster';
    public position: { x: number; y: number };
    public amount: number;
    public icon: string;
    public isRelic: boolean;
    public isMonster: boolean;
    public difficulty: number;
    public monsterName: string;
    public gatherTime: number;
    public sprite: Phaser.GameObjects.Arc | null;
    public label: Phaser.GameObjects.Text | null;
    public isDepleted: boolean;
    
    // ✅ ระบบ HP ใหม่
    public hp: number;
    public maxHp: number;
    public rank: number; // 0-3
    public rankName: string;

    constructor(data: ResourceNodeData) {
        this.id = data.id;
        this.type = data.type;
        this.position = { x: data.x, y: data.y };
        this.amount = data.amount;
        this.icon = data.icon;
        this.isRelic = data.isRelic || false;
        this.isMonster = data.isMonster || false;
        this.difficulty = data.difficulty || 1;
        this.monsterName = data.monsterName || 'Unknown Monster';
        this.gatherTime = data.gatherTime || 3000;
        this.sprite = null;
        this.label = null;
        this.isDepleted = false;
        
        // ✅ คำนวณ HP
        this.rank = data.rank || 0;
        this.hp = this.calculateHP();
        this.maxHp = this.hp;
        this.rankName = this.getRankName();
    }

    // ✅ คำนวณ HP ของโหนด
    private calculateHP(): number {
        if (this.isRelic) {
            return this.calculateRelicHP();
        } else if (this.isMonster) {
            return this.calculateMonsterHP();
        } else {
            return this.calculateGatherHP();
        }
    }

    // ✅ Gathering HP
    private calculateGatherHP(): number {
        // สุ่ม rank 0-3 ตาม amount
        const amount = this.amount;
        let rank = 0;
        if (amount > 80) rank = 3;
        else if (amount > 60) rank = 2;
        else if (amount > 40) rank = 1;
        else rank = 0;
        
        this.rank = rank;
        const hpPerUnit = GAME_CONFIG.GATHER_HP_PER_UNIT[rank] || 50;
        return this.amount * hpPerUnit;
    }

    // ✅ Relic HP
    private calculateRelicHP(): number {
        const baseHP = GAME_CONFIG.RELIC_BASE_HP_RANGE;
        const multipliers = GAME_CONFIG.RELIC_RARITY_MULTIPLY;
        
        // สุ่ม rank 0-3
        this.rank = Math.floor(Math.random() * 4);
        const base = baseHP.min + Math.random() * (baseHP.max - baseHP.min);
        const hp = base * multipliers[this.rank];
        return Math.floor(hp);
    }

    // ✅ Monster HP
    private calculateMonsterHP(): number {
        const baseHP = GAME_CONFIG.MONSTER_BASE_HP_RANGE;
        const multipliers = GAME_CONFIG.MONSTER_RARITY_MULTIPLY;
        
        // rank ขึ้นอยู่กับ difficulty
        this.rank = Math.min(this.difficulty - 1, 3);
        const base = baseHP.min + Math.random() * (baseHP.max - baseHP.min);
        const hp = base * multipliers[this.rank];
        return Math.floor(hp);
    }

    // ✅ ดึงชื่อ Rank
    private getRankName(): string {
        if (this.isRelic) {
            return GAME_CONFIG.RELIC_RANKS[this.rank] || 'Common';
        } else if (this.isMonster) {
            return GAME_CONFIG.MONSTER_RANKS[this.rank] || 'Common';
        } else {
            return GAME_CONFIG.GATHER_RANKS[this.rank] || 'Common';
        }
    }

    // ✅ คำนวณเวลาที่ใช้ในการทำภารกิจ (ตามระบบใหม่)
    getActionTime(crewProficiency: number): number {
        // ✅ ห้ามหารด้วย 0
        const proficiency = Math.max(0.1, crewProficiency);
        // ✅ เวลา = HP / Proficiency (หน่วยเวลา)
        return Math.ceil(this.hp / proficiency);
    }

    // ✅ รับความเสียหาย (ใช้ในการเก็บ/ล่า)
    takeDamage(damage: number): boolean {
        this.hp -= damage;
        if (this.hp <= 0) {
            this.hp = 0;
            this.isDepleted = true;
            return true; // โหนดถูกทำลาย
        }
        return false;
    }

    // ✅ ดึงเปอร์เซ็นต์ HP ที่เหลือ
    getHpPercentage(): number {
        return (this.hp / this.maxHp) * 100;
    }

    getColor(): number {
        if (this.isRelic) return 0x6c5ce7;
        if (this.isMonster) return 0xe74c3c;
        return 0xf9ca24;
    }
}   