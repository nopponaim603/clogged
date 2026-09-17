// src/entities/Monster.ts
import { GAME_CONFIG } from '../config';
import { RandomGenerator } from '../utils/RandomGenerator';

export type MonsterType = 'A' | 'B' | 'C' | 'D' | 'E' | 'F' | 'G' | 'H';

export class Monster {
    public id: string;
    public type: MonsterType;
    public rank: number;
    public hp: number;
    public maxHp: number;
    public atk: number;
    
    // Position (DefenseScene)
    public x: number;
    public y: number;
    
    // Visual
    public sprite: Phaser.GameObjects.Arc | null = null;
    public icon: Phaser.GameObjects.Text | null = null;
    public hpBar: Phaser.GameObjects.Graphics | null = null;
    public hpBarBg: Phaser.GameObjects.Graphics | null = null;
    
    constructor(data: Partial<Monster>) {
        this.id = data.id || `monster_${Date.now()}_${Math.random()}`;
        this.type = data.type || 'A';
        
        const monsterData = GAME_CONFIG.MONSTER_TYPES[this.type];
        this.rank = monsterData.rank;
        this.hp = data.hp || monsterData.hp;
        this.maxHp = data.maxHp || monsterData.hp;
        this.atk = data.atk || monsterData.atk;
        
        this.x = data.x || 0;
        this.y = data.y || 0;
    }
    
    // ============================================================
    // STATIC FACTORY
    // ============================================================
    static create(type: MonsterType, x: number, y: number): Monster {
        return new Monster({
            type,
            x,
            y,
        });
    }
    
    static createRandom(x: number, y: number): Monster {
        const rng = RandomGenerator.getInstance();
        const type = rng.randomMonsterType() as MonsterType;
        return new Monster({ type, x, y });
    }
    
    // ============================================================
    // COMBAT
    // ============================================================
    takeDamage(amount: number): boolean {
        this.hp = Math.max(0, this.hp - amount);
        return this.hp <= 0;
    }
    
    isDead(): boolean {
        return this.hp <= 0;
    }
    
    // ============================================================
    // VISUAL
    // ============================================================
    getColor(): number {
        const colors: { [key: string]: number } = {
            A: 0x95a5a6,
            B: 0x7f8c8d,
            C: 0x3498db,
            D: 0x2980b9,
            E: 0x9b59b6,
            F: 0x8e44ad,
            G: 0xe74c3c,
            H: 0xff0000,
        };
        return colors[this.type] || 0xe74c3c;
    }
    
    getIcon(): string {
        const icons: { [key: string]: string } = {
            A: '👾',
            B: '👾',
            C: '👹',
            D: '👹',
            E: '👺',
            F: '👺',
            G: '🐉',
            H: '💀',
        };
        return icons[this.type] || '👹';
    }
    
    getSize(): number {
        if (this.rank >= 5) return 30;
        if (this.rank >= 4) return 25;
        if (this.rank >= 3) return 22;
        return 18;
    }
    
    // ============================================================
    // SERIALIZATION
    // ============================================================
    toJSON(): object {
        return {
            id: this.id,
            type: this.type,
            rank: this.rank,
            hp: this.hp,
            maxHp: this.maxHp,
            atk: this.atk,
            x: this.x,
            y: this.y,
        };
    }
}