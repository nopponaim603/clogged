// src/systems/WorldMapSystem.ts
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { MapNode } from '../entities/MapNode';
import { Path } from '../state/types';
import { RandomGenerator } from '../utils/RandomGenerator';

export class WorldMapSystem {
    private nodes: Map<string, MapNode> = new Map();
    private paths: Map<string, Path> = new Map();
    private startNodeId: string = '';
    private finishNodeId: string = '';
    private finishPosition: { x: number; y: number } = { x: 0, y: 0 };
    
    // ============================================================
    // INITIALIZE
    // ============================================================
    init(seed: number): void {
        RandomGenerator.reset(seed);
        
        this.nodes.clear();
        this.paths.clear();
        
        this.generateMap();
        
        const state = GameState;
        state.setNodes(Array.from(this.nodes.values()));
        state.setPaths(Array.from(this.paths.values()));
    }
    
    // ============================================================
    // MAP GENERATION
    // ============================================================
    private generateMap(): void {
        const rng = RandomGenerator.getInstance();
        const mapSize = GAME_CONFIG.MAP_SIZE;
        const padding = 2000;
        
        const startPos = {
            x: rng.randomRange(padding, mapSize * 0.2),
            y: rng.randomRange(padding, mapSize - padding),
        };
        
        const startNode = MapNode.createStart('node_start', startPos);
        this.nodes.set(startNode.id, startNode);
        this.startNodeId = startNode.id;
        
        this.finishPosition = {
            x: rng.randomRange(mapSize * 0.8, mapSize - padding),
            y: rng.randomRange(padding, mapSize - padding),
        };
        
        const finishNode = MapNode.createFinish('node_finish', this.finishPosition);
        this.nodes.set(finishNode.id, finishNode);
        this.finishNodeId = finishNode.id;
        
        this.generateMainPath(startNode, finishNode);
        this.generateSideBranches();
        this.generateCrossLinks();
        this.syncToGameState();
    }
    
    private generateMainPath(startNode: MapNode, finishNode: MapNode): void {
        const rng = RandomGenerator.getInstance();
        
        const totalDistance = this.calculateDistance(startNode.position, finishNode.position);
        const avgPathDistance = (GAME_CONFIG.PATH_DISTANCE_MIN_HOURS + GAME_CONFIG.PATH_DISTANCE_MAX_HOURS) / 2;
        const avgPixelsPerHour = 500;
        const avgPathPixels = avgPathDistance * avgPixelsPerHour;
        
        const nodeCount = Math.max(5, Math.floor(totalDistance / avgPathPixels));
        
        console.log(`🗺️ Generating main path with ${nodeCount} nodes`);
        
        let prevNode = startNode;
        
        for (let i = 1; i < nodeCount; i++) {
            const t = i / nodeCount;
            
            const baseX = startNode.position.x + (finishNode.position.x - startNode.position.x) * t;
            const baseY = startNode.position.y + (finishNode.position.y - startNode.position.y) * t;
            
            const noiseRange = 3000;
            const x = baseX + rng.randomRange(-noiseRange, noiseRange);
            const y = baseY + rng.randomRange(-noiseRange, noiseRange);
            
            const typeRoll = rng.random();
            let node: MapNode;
            
            if (typeRoll < 0.20) {
                node = MapNode.createJunction(`node_${i}`, { x, y });
            } else if (typeRoll < 0.70) {
                node = MapNode.createResource(`node_${i}`, { x, y });
            } else if (typeRoll < 0.90) {
                node = MapNode.createMonster(`node_${i}`, { x, y });
            } else if (typeRoll < 0.95) {
                node = MapNode.createShop(`node_${i}`, { x, y });
            } else {
                node = MapNode.createEncounter(`node_${i}`, { x, y });
            }
            
            this.nodes.set(node.id, node);
            this.addPath(prevNode, node);
            prevNode = node;
        }
        
        this.addPath(prevNode, finishNode);
    }
    
    private generateSideBranches(): void {
        const rng = RandomGenerator.getInstance();
        
        const junctions = Array.from(this.nodes.values()).filter(n => n.type === 'junction');
        
        console.log(`🌿 Generating side branches from ${junctions.length} junctions`);
        
        junctions.forEach(junction => {
            const branchCount = rng.randomInt(2, 4);
            
            for (let i = 0; i < branchCount; i++) {
                const angle = rng.randomRange(0, Math.PI * 2);
                const distance = rng.randomRange(2000, 8000);
                
                const branchPos = {
                    x: junction.position.x + Math.cos(angle) * distance,
                    y: junction.position.y + Math.sin(angle) * distance,
                };
                
                if (branchPos.x < 0 || branchPos.x > GAME_CONFIG.MAP_SIZE
                    || branchPos.y < 0 || branchPos.y > GAME_CONFIG.MAP_SIZE) {
                    continue;
                }
                
                const typeRoll = rng.random();
                let node: MapNode;
                const nodeId = `node_branch_${junction.id}_${i}`;
                
                if (typeRoll < 0.60) {
                    node = MapNode.createResource(nodeId, branchPos);
                } else if (typeRoll < 0.80) {
                    node = MapNode.createMonster(nodeId, branchPos);
                } else if (typeRoll < 0.95) {
                    node = MapNode.createRelic(nodeId, branchPos);
                } else {
                    node = MapNode.createShop(nodeId, branchPos);
                }
                
                this.nodes.set(node.id, node);
                this.addPath(junction, node);
            }
        });
    }
    
    private generateCrossLinks(): void {
        const rng = RandomGenerator.getInstance();
        
        const allNodes = Array.from(this.nodes.values());
        const crossLinkCount = Math.floor(allNodes.length * 0.1);
        
        console.log(`🔗 Generating ${crossLinkCount} cross links`);
        
        for (let i = 0; i < crossLinkCount; i++) {
            const nodeA = rng.pick(allNodes);
            const nodeB = rng.pick(allNodes);
            
            if (nodeA.id === nodeB.id) continue;
            if (nodeA.type === 'start' || nodeA.type === 'finish') continue;
            if (nodeB.type === 'start' || nodeB.type === 'finish') continue;
            
            const distance = this.calculateDistance(nodeA.position, nodeB.position);
            if (distance > 5000) continue;
            
            const pathKey1 = `${nodeA.id}_${nodeB.id}`;
            const pathKey2 = `${nodeB.id}_${nodeA.id}`;
            if (this.paths.has(pathKey1) || this.paths.has(pathKey2)) continue;
            
            this.addPath(nodeA, nodeB);
        }
    }
    
    // ============================================================
    // PATH MANAGEMENT
    // ============================================================
    private addPath(from: MapNode, to: MapNode): void {
        const distancePixels = this.calculateDistance(from.position, to.position);
        const distanceHours = this.pixelsToHours(distancePixels);
        
        const pathForward: Path = {
            from: from.id,
            to: to.id,
            distanceHours,
            isActive: true,
        };
        
        const pathBackward: Path = {
            from: to.id,
            to: from.id,
            distanceHours,
            isActive: true,
        };
        
        this.paths.set(`${from.id}_${to.id}`, pathForward);
        this.paths.set(`${to.id}_${from.id}`, pathBackward);
        
        from.addConnection(to.id);
        to.addConnection(from.id);
    }
    
    // ============================================================
    // UTILITY
    // ============================================================
    private calculateDistance(posA: { x: number; y: number }, posB: { x: number; y: number }): number {
        return Math.sqrt(
            Math.pow(posB.x - posA.x, 2) + Math.pow(posB.y - posA.y, 2)
        );
    }
    
    private pixelsToHours(pixels: number): number {
        return pixels / 1000;
    }
    
    private syncToGameState(): void {
        const state = GameState;
        state.setNodes(Array.from(this.nodes.values()));
        state.setPaths(Array.from(this.paths.values()));
    }
    
    // ============================================================
    // ✅ GETTERS (เพิ่ม getPaths)
    // ============================================================
    getNode(id: string): MapNode | undefined {
        return this.nodes.get(id);
    }
    
    getAllNodes(): MapNode[] {
        return Array.from(this.nodes.values());
    }
    
    // ✅ เพิ่ม getPaths()
    getPaths(): Path[] {
        return Array.from(this.paths.values());
    }
    
    getStartNode(): MapNode {
        return this.nodes.get(this.startNodeId)!;
    }
    
    getFinishNode(): MapNode {
        return this.nodes.get(this.finishNodeId)!;
    }
    
    getPath(fromId: string, toId: string): Path | undefined {
        return this.paths.get(`${fromId}_${toId}`);
    }
    
    getConnections(nodeId: string): MapNode[] {
        const node = this.nodes.get(nodeId);
        if (!node) return [];
        
        return node.connections
            .map(id => this.nodes.get(id))
            .filter((n): n is MapNode => n !== undefined);
    }
    
    getDistanceBetween(fromId: string, toId: string): number {
        const path = this.getPath(fromId, toId);
        return path ? path.distanceHours : 0;
    }
    
    // ============================================================
    // FOG OF WAR
    // ============================================================
    getVisibleNodes(basePosition: { x: number; y: number }): MapNode[] {
        const radius = GAME_CONFIG.FOG_VISIBILITY_RADIUS;
        
        return Array.from(this.nodes.values()).filter(node => {
            const distance = this.calculateDistance(node.position, basePosition);
            return distance <= radius;
        });
    }
    
    updateFog(basePosition: { x: number; y: number }): void {
        const visibleNodes = this.getVisibleNodes(basePosition);
        const state = GameState;
        const fog = state.getFog();
        
        if (GAME_CONFIG.FOG_FORGET_ENABLED) {
            fog.revealedNodes.clear();
        }
        
        let hasNewDiscovery = false;
        
        visibleNodes.forEach(node => {
            if (!fog.revealedNodes.has(node.id)) {
                fog.revealedNodes.add(node.id);
                node.isDiscovered = true;
                hasNewDiscovery = true;
                
                EventBus.emit(EVENTS.MAP_NODE_DISCOVERED, node);
            }
        });
        
        if (hasNewDiscovery) {
            EventBus.emit(EVENTS.MAP_FOG_UPDATED);
        }
    }
    
    isNodeVisible(nodeId: string, basePosition: { x: number; y: number }): boolean {
        const node = this.nodes.get(nodeId);
        if (!node) return false;
        
        const distance = this.calculateDistance(node.position, basePosition);
        return distance <= GAME_CONFIG.FOG_VISIBILITY_RADIUS;
    }
    
    isNodeDiscovered(nodeId: string): boolean {
        const state = GameState;
        return state.getFog().revealedNodes.has(nodeId);
    }
    
    // ============================================================
    // JUNCTION
    // ============================================================
    getNextJunction(currentNodeId: string): MapNode | null {
        const currentNode = this.nodes.get(currentNodeId);
        if (!currentNode) return null;
        
        for (const connId of currentNode.connections) {
            const connNode = this.nodes.get(connId);
            if (connNode && connNode.type === 'junction') {
                return connNode;
            }
        }
        
        return null;
    }
    
    // ============================================================
    // VISUALIZATION
    // ============================================================
    getBoundingBox(): { minX: number; minY: number; maxX: number; maxY: number } {
        const nodes = this.getAllNodes();
        
        if (nodes.length === 0) {
            return { minX: 0, minY: 0, maxX: 0, maxY: 0 };
        }
        
        let minX = Infinity, minY = Infinity;
        let maxX = -Infinity, maxY = -Infinity;
        
        nodes.forEach(node => {
            minX = Math.min(minX, node.position.x);
            minY = Math.min(minY, node.position.y);
            maxX = Math.max(maxX, node.position.x);
            maxY = Math.max(maxY, node.position.y);
        });
        
        return { minX, minY, maxX, maxY };
    }
    
    debugPrint(): void {
        console.log('=== MAP DEBUG ===');
        console.log(`Total nodes: ${this.nodes.size}`);
        console.log(`Total paths: ${this.paths.size}`);
        
        const typeCounts: { [key: string]: number } = {};
        this.nodes.forEach(node => {
            typeCounts[node.type] = (typeCounts[node.type] || 0) + 1;
        });
        
        console.log('Node types:', typeCounts);
        console.log(`Start: ${this.startNodeId}`);
        console.log(`Finish: ${this.finishNodeId}`);
        console.log('=================');
    }
}