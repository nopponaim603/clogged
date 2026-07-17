// src/data/Constants.ts
import { GAME_CONFIG } from '../config';

export const COLORS = {
    PRIMARY: '#1a1a2e',
    SECONDARY: '#2d3436',
    ACCENT: '#00b894',
    DANGER: '#ff6b6b',
    WARNING: '#f9ca24',
    SUCCESS: '#4ecdc4',
    PURPLE: '#6c5ce7',
    TEXT: '#ffffff',
    TEXT_MUTED: '#b2bec3',
};

export const CREW_NAMES = ['Sarah', 'John', 'Emma', 'Mike', 'Lisa', 'David', 'Anna', 'Tom', 'Katie', 'James'];

// ✅ Perks ทั้งหมด (ตรงกับที่ใช้ใน Crew.ts)
export const PERKS = [
    'sprinter',      // +20% Speed
    'fast_hands',    // +25% Gathering
    'night_vision',  // +20% Searching
    'gunslinger',    // +30% Hunting
    'blacksmith',    // +15% Gathering
    'quick_reflex',  // +15% Speed
    'tough',         // +30% HP
    'lucky',         // +10% Gathering, +10% Searching
    'strong',        // +20% Hunting
    'scout',         // +10% Speed, +10% Searching
    'medic',         // +20% Healing
];

// ✅ Perk Descriptions
export const PERK_DESCRIPTIONS: { [key: string]: string } = {
    sprinter: '🏃 +20% Movement Speed',
    fast_hands: '✋ +25% Gathering Speed',
    night_vision: '🌙 +20% Searching Efficiency',
    gunslinger: '🔫 +30% Hunting Damage',
    blacksmith: '⚒️ +15% Gathering Speed (Metal)',
    quick_reflex: '⚡ +15% Movement Speed',
    tough: '💪 +30% Max HP',
    lucky: '🍀 +10% Gathering, +10% Searching',
    strong: '💥 +20% Hunting Damage',
    scout: '🔭 +10% Speed, +10% Searching',
    medic: '💊 +20% Healing Effect',
};

export const MONSTER_NAMES = ['Goblin', 'Wolf', 'Giant Spider', 'Shadow Demon', 'Night Stalker', 'Boss'];

// ✅ Rank System
export const RANK_NAMES: { [key: string]: string } = {
    'F': 'F',
    'E': 'E',
    'D': 'D',
    'C': 'C',
    'B': 'B',
    'A': 'A',
    'AA': 'AA',
    'AAA': 'AAA',
    'S': 'S',
    'SS': 'SS',
    'SSS': 'SSS',
    'EX': 'EX',
};

export const RANK_COLORS: { [key: string]: string } = {
    'F': '#808080',
    'E': '#4a90d9',
    'D': '#50c878',
    'C': '#f9ca24',
    'B': '#ff8c00',
    'A': '#ff6b6b',
    'AA': '#e74c3c',
    'AAA': '#8e44ad',
    'S': '#ff00ff',
    'SS': '#ffd700',
    'SSS': '#ff4500',
    'EX': '#ff0000',
};