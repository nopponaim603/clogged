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
    public gatherTime: number; // ✅ เก็บค่าตรงๆ ไม่แปลง
    public sprite: Phaser.GameObjects.Arc | null;
    public label: Phaser.GameObjects.Text | null;
    public isDepleted: boolean;

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
        
        // ✅ เก็บค่าตรงๆ ไม่แปลง
        this.gatherTime = data.gatherTime || 3000;
        
        this.sprite = null;
        this.label = null;
        this.isDepleted = false;
    }

    getActionTime(crewProficiency: number): number {
        let baseTime = 0;
        let difficultyFactor = 1;
        
        if (this.isRelic) {
            baseTime = GAME_CONFIG.BASE_SEARCH_TIME; // 4500
            difficultyFactor = 1.2 + Math.random() * 0.3;
        } else if (this.isMonster) {
            baseTime = GAME_CONFIG.BASE_HUNT_TIME; // 5000
            difficultyFactor = 1 + (this.difficulty || 1) * 0.4;
        } else {
            baseTime = GAME_CONFIG.BASE_GATHER_TIME; // 3000
            const amountFactor = 1 + (this.amount / 100) * 0.6;
            difficultyFactor = amountFactor;
        }
        
        // ✅ crewProficiency เป็นค่า 0-100 แล้ว
        const proficiencyFactor = Math.max(0.1, crewProficiency / 100);
        const actionTime = (baseTime * difficultyFactor) / proficiencyFactor;
        
        return actionTime;
    }

    getColor(): number {
        if (this.isRelic) return 0x6c5ce7;
        if (this.isMonster) return 0xe74c3c;
        return 0xf9ca24;
    }
}