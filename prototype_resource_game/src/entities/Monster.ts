// src/entities/Monster.ts
export class Monster {
    public id: string;
    public name: string;
    public x: number;
    public y: number;
    public hp: number;
    public maxHp: number;
    public damage: number;
    public speed: number;
    public difficulty: number;
    public sprite: Phaser.GameObjects.Arc | null;  // ✅ เปลี่ยนเป็น Arc
    public icon: Phaser.GameObjects.Text | null;

    constructor(data: {
        id: string;
        name: string;
        x: number;
        y: number;
        difficulty: number;
    }) {
        this.id = data.id;
        this.name = data.name;
        this.x = data.x;
        this.y = data.y;
        this.difficulty = data.difficulty;
        this.hp = 20 + data.difficulty * 10;
        this.maxHp = this.hp;
        this.damage = 5 + data.difficulty * 3;
        this.speed = 50 + data.difficulty * 20;
        this.sprite = null;
        this.icon = null;
    }

    takeDamage(damage: number): boolean {
        this.hp -= damage;
        return this.hp <= 0;
    }

    isDead(): boolean {
        return this.hp <= 0;
    }

    getColor(): number {
        const colors = [0xe74c3c, 0xff6b6b, 0xc0392b, 0x8e44ad];
        return colors[Math.min(this.difficulty - 1, colors.length - 1)];
    }
}