// src/utils/Helpers.ts
export class Helpers {
    static randomRange(min: number, max: number): number {
        return min + Math.random() * (max - min);
    }

    static randomInt(min: number, max: number): number {
        return Math.floor(this.randomRange(min, max + 1));
    }

    static clamp(value: number, min: number, max: number): number {
        return Math.max(min, Math.min(max, value));
    }

    static distance(x1: number, y1: number, x2: number, y2: number): number {
        return Math.sqrt(Math.pow(x2 - x1, 2) + Math.pow(y2 - y1, 2));
    }

    static formatTime(seconds: number): string {
        const mins = Math.floor(seconds / 60);
        const secs = Math.floor(seconds % 60);
        return `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
    }

    static shuffleArray<T>(array: T[]): T[] {
        for (let i = array.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1));
            [array[i], array[j]] = [array[j], array[i]];
        }
        return array;
    }

    static pickRandom<T>(array: T[]): T {
        return array[Math.floor(Math.random() * array.length)];
    }

    static getResourceIcon(type: string): string {
        const icons: { [key: string]: string } = {
            wood: '🪵',
            stone: '🪨',
            iron: '⛏️',
            food: '🍖',
            water: '💧',
            circuit: '⚡',
            aluminum: '🔩',
            relic: '🏛️',
            monster: '👹'
        };
        return icons[type] || '📦';
    }

    static getMonsterEmoji(difficulty: number): string {
        const emojis = ['👾', '👹', '🐉', '💀'];
        return emojis[Math.min(difficulty - 1, emojis.length - 1)];
    }
}