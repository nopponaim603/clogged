// src/entities/Base.ts
export class Base {
    public x: number;
    public y: number;
    public hp: number;
    public maxHp: number;
    public sprite: Phaser.GameObjects.Arc | null;  // ✅ เปลี่ยนเป็น Arc

    constructor(x: number, y: number, maxHp: number = 100) {
        this.x = x;
        this.y = y;
        this.hp = maxHp;
        this.maxHp = maxHp;
        this.sprite = null;
    }

    takeDamage(damage: number): boolean {
        this.hp -= damage;
        return this.hp <= 0;
    }

    getHpPercentage(): number {
        return (this.hp / this.maxHp) * 100;
    }

    isDestroyed(): boolean {
        return this.hp <= 0;
    }

    setPosition(x: number, y: number): void {
        this.x = x;
        this.y = y;
        if (this.sprite) {
            this.sprite.x = x;
            this.sprite.y = y;
        }
    }
}