// src/ui/Minimap.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Systems } from '../main';
import { MapNode } from '../entities/MapNode';
import { Path } from '../state/types';

export class Minimap {
    private scene: Scene;
    private container: Phaser.GameObjects.Container;
    private mapGraphics: Phaser.GameObjects.Graphics;
    
    private readonly MAP_WIDTH = 220;
    private readonly MAP_HEIGHT = 180;
    private readonly MAP_X: number;
    private readonly MAP_Y: number;
    
    constructor(scene: Scene) {
        this.scene = scene;
        
        // ✅ ใช้ camera width/height จริง
        const cam = scene.cameras.main;
        this.MAP_X = GAME_CONFIG.WIDTH - this.MAP_WIDTH - 20;
        this.MAP_Y = GAME_CONFIG.HEIGHT - this.MAP_HEIGHT - 20;
        
        console.log(`🗺️ Minimap position: (${this.MAP_X}, ${this.MAP_Y})`);
        
        this.container = scene.add.container(0, 0);
        this.container.setDepth(1000);
        this.container.setScrollFactor(0);
        
        // ✅ ให้ UI camera เห็นเท่านั้น
        scene.cameras.main.ignore(this.container);
        
        // ✅ Background
        const bg = scene.add.graphics();
        bg.fillStyle(GAME_CONFIG.COLORS.BG_MEDIUM, 0.95);
        bg.fillRoundedRect(this.MAP_X, this.MAP_Y, this.MAP_WIDTH, this.MAP_HEIGHT, 8);
        bg.lineStyle(2, GAME_CONFIG.COLORS.PATH_ACTIVE, 0.8);
        bg.strokeRoundedRect(this.MAP_X, this.MAP_Y, this.MAP_WIDTH, this.MAP_HEIGHT, 8);
        this.container.add(bg);
        
        // ✅ Title
        const title = scene.add.text(this.MAP_X + 10, this.MAP_Y + 8, '🗺️ MAP', {
            fontSize: '12px',
            color: '#b0c4de',
            fontFamily: 'monospace',
            fontStyle: 'bold',
        });
        this.container.add(title);
        
        // ✅ Map Graphics
        this.mapGraphics = scene.add.graphics();
        this.container.add(this.mapGraphics);
        
        this.update();
        
        EventBus.on(EVENTS.BASE_MOVED, this.update, this);
        EventBus.on(EVENTS.MAP_FOG_UPDATED, this.update, this);
    }
    
    update(): void {
        const state = GameState;
        const basePos = state.getBasePosition();
        const fog = state.getFog();
        const mapSize = GAME_CONFIG.MAP_SIZE;
        
        // ✅ Defensive
        if (!Systems.map) return;
        if (!fog || !fog.revealedNodes) return;
        
        const innerWidth = this.MAP_WIDTH - 20;
        const innerHeight = this.MAP_HEIGHT - 40;
        const scaleX = innerWidth / mapSize;
        const scaleY = innerHeight / mapSize;
        
        this.mapGraphics.clear();
        
        // ✅ Paths
        const paths: Path[] = Systems.map.getPaths();
        this.mapGraphics.lineStyle(1, GAME_CONFIG.COLORS.PATH_INACTIVE, 0.6);
        
        const drawnPaths = new Set<string>();
        paths.forEach((path: Path) => {
            const key = [path.from, path.to].sort().join('_');
            if (drawnPaths.has(key)) return;
            drawnPaths.add(key);
            
            const fromNode = Systems.map.getNode(path.from);
            const toNode = Systems.map.getNode(path.to);
            
            if (!fromNode || !toNode) return;
            if (!fog.revealedNodes.has(fromNode.id) && !fog.revealedNodes.has(toNode.id)) return;
            
            const fromX = this.MAP_X + 10 + fromNode.position.x * scaleX;
            const fromY = this.MAP_Y + 25 + fromNode.position.y * scaleY;
            const toX = this.MAP_X + 10 + toNode.position.x * scaleX;
            const toY = this.MAP_Y + 25 + toNode.position.y * scaleY;
            
            this.mapGraphics.beginPath();
            this.mapGraphics.moveTo(fromX, fromY);
            this.mapGraphics.lineTo(toX, toY);
            this.mapGraphics.strokePath();
        });
        
        // ✅ Nodes
        const nodes: MapNode[] = Systems.map.getAllNodes();
        nodes.forEach((node: MapNode) => {
            if (!fog.revealedNodes.has(node.id)) return;
            
            const x = this.MAP_X + 10 + node.position.x * scaleX;
            const y = this.MAP_Y + 25 + node.position.y * scaleY;
            
            this.mapGraphics.fillStyle(node.getColor(), 1);
            this.mapGraphics.fillCircle(x, y, 3);
        });
        
        // ✅ Base
        const baseX = this.MAP_X + 10 + basePos.x * scaleX;
        const baseY = this.MAP_Y + 25 + basePos.y * scaleY;
        
        this.mapGraphics.fillStyle(0x00ff9d, 1);
        this.mapGraphics.fillCircle(baseX, baseY, 5);
        this.mapGraphics.lineStyle(2, 0xffffff);
        this.mapGraphics.strokeCircle(baseX, baseY, 5);
    }
    
    destroy(): void {
        this.container.destroy();
        EventBus.off(EVENTS.BASE_MOVED, this.update, this);
        EventBus.off(EVENTS.MAP_FOG_UPDATED, this.update, this);
    }
}