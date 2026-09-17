// src/scenes/WorldScene.ts
import { Scene } from 'phaser';
import { GAME_CONFIG } from '../config';
import { GameState } from '../state/GameState';
import { EventBus, EVENTS } from '../state/EventBus';
import { Systems } from '../main';
import { MapNode } from '../entities/MapNode';
import { HUD } from '../ui/HUD';
import { Minimap } from '../ui/Minimap';
import { FogOfWar } from '../ui/FogOfWar';
import { JunctionPopup } from '../ui/JunctionPopup';
import { MinimizedBase } from '../ui/MinimizedBase';

export class WorldScene extends Scene {
    // Systems
    private hud!: HUD;
    private minimap!: Minimap;
    private fog!: FogOfWar;
    private junctionPopup!: JunctionPopup;
    private minimizedBase!: MinimizedBase;
    
    // ✅ UI Camera แยก (zoom = 1)
    private uiCamera!: Phaser.Cameras.Scene2D.Camera;
    
    // Map Objects
    private nodeSprites: Map<string, Phaser.GameObjects.Arc> = new Map();
    private nodeLabels: Map<string, Phaser.GameObjects.Text> = new Map();
    private nodeIcons: Map<string, Phaser.GameObjects.Text> = new Map();
    private pathGraphics!: Phaser.GameObjects.Graphics;
    
    // Base
    private baseSprite!: Phaser.GameObjects.Arc;
    private baseLabel!: Phaser.GameObjects.Text;
    private baseGlow!: Phaser.GameObjects.Arc;
    
    // Fast Forward UI
    private speedButtons: Phaser.GameObjects.Text[] = [];
    private speedBg!: Phaser.GameObjects.Graphics;
    
    // ✅ รายการ UI objects ที่ต้องใช้ UI Camera
    private uiObjects: Phaser.GameObjects.GameObject[] = [];
    
    constructor() {
        super('WorldScene');
    }
    
    create(): void {
        // ✅ Background
        this.cameras.main.setBackgroundColor(GAME_CONFIG.COLORS.BG_DARK);
        
        // ✅ Generate Map
        const seed = GameState.getData().seed;
        Systems.map.init(seed);
        Systems.map.debugPrint();
        
        // ✅ Init Base Movement
        Systems.baseMovement.init(this);
        
        // ✅ Create Map Visuals (world objects)
        this.createMapVisuals();
        
        // ✅ Create Base (world object)
        this.createBaseVisual();
        
        // ✅ Setup Camera (world camera, zoom 0.5)
        this.setupCamera();
        
        // ✅ Create UI Camera (zoom 1)
        this.createUICamera();
        
        // ✅ Create UI (จะ auto-register กับ UI Camera)
        this.hud = new HUD(this);
        this.minimap = new Minimap(this);
        this.fog = new FogOfWar(this);
        this.junctionPopup = new JunctionPopup(this);
        this.minimizedBase = new MinimizedBase(this);
        
        // ✅ Fast Forward UI
        this.createSpeedButtons();
        
        // ✅ Setup Events
        this.setupEventListeners();
        
        // ✅ Setup Input
        this.setupInput();
        
        // ✅ Show tutorial
        this.showTutorial();
    }
    
    // ============================================================
    // ✅ UI CAMERA
    // ============================================================
    private createUICamera(): void {
        // ✅ UI Camera: ตำแหน่ง (0,0), ขนาดเท่าจอ, zoom 1
        this.uiCamera = this.cameras.add(
            0, 0,
            GAME_CONFIG.WIDTH,
            GAME_CONFIG.HEIGHT
        );
        this.uiCamera.setZoom(1);
        this.uiCamera.setScroll(0, 0);
        this.uiCamera.setName('uiCamera');
        this.uiCamera.setBackgroundColor('rgba(0,0,0,0)');
        
        console.log('📷 UI Camera created');
    }
    
    // ============================================================
    // ✅ REGISTER UI OBJECT
    // ============================================================
    private registerUI(obj: Phaser.GameObjects.GameObject): void {
        // ✅ Main camera ไม่เห็น object นี้
        this.cameras.main.ignore(obj);
        // ✅ UI camera เห็น
        this.uiObjects.push(obj);
    }
    
    // ============================================================
    // FAST FORWARD BUTTONS (มุมขวาบน)
    // ============================================================
    private createSpeedButtons(): void {
        const startX = GAME_CONFIG.WIDTH - 200;
        const startY = 15;
        const btnWidth = 55;
        const btnHeight = 40;
        const spacing = 8;
        
        const speeds = [1, 2, 4];
        
        // ✅ Background
        this.speedBg = this.add.graphics();
        this.speedBg.fillStyle(GAME_CONFIG.COLORS.BG_MEDIUM, 0.95);
        this.speedBg.fillRoundedRect(
            startX - 10,
            startY - 5,
            speeds.length * (btnWidth + spacing) + 12,
            btnHeight + 10,
            8
        );
        this.speedBg.lineStyle(2, GAME_CONFIG.COLORS.PATH_ACTIVE, 0.8);
        this.speedBg.strokeRoundedRect(
            startX - 10,
            startY - 5,
            speeds.length * (btnWidth + spacing) + 12,
            btnHeight + 10,
            8
        );
        this.speedBg.setScrollFactor(0);
        this.speedBg.setDepth(1000);
        this.registerUI(this.speedBg);
        
        speeds.forEach((speed, index) => {
            const x = startX + index * (btnWidth + spacing) + btnWidth / 2;
            const y = startY + btnHeight / 2;
            
            const btn = this.add.text(x, y, `x${speed}`, {
                fontSize: '18px',
                color: '#b0c4de',
                fontFamily: 'monospace',
                fontStyle: 'bold',
                backgroundColor: '#1a2332',
                padding: { x: 10, y: 6 },
            }).setOrigin(0.5).setScrollFactor(0).setDepth(1001).setInteractive({ useHandCursor: true });
            
            btn.on('pointerdown', () => {
                Systems.time.setTimeScale(speed);
                this.updateSpeedButtons();
            });
            
            btn.on('pointerover', () => {
                if (GameState.getTime().timeScale !== speed) {
                    btn.setStyle({ color: '#ffffff' });
                }
            });
            
            btn.on('pointerout', () => {
                this.updateSpeedButtons();
            });
            
            this.speedButtons.push(btn);
            this.registerUI(btn);
        });
        
        this.updateSpeedButtons();
    }
    
    private updateSpeedButtons(): void {
        const currentScale = GameState.getTime().timeScale;
        const speeds = [1, 2, 4];
        
        this.speedButtons.forEach((btn, index) => {
            const speed = speeds[index];
            const isActive = speed === currentScale;
            
            if (isActive) {
                btn.setStyle({
                    color: '#00e5cc',
                    backgroundColor: '#2d3436',
                });
            } else {
                btn.setStyle({
                    color: '#b0c4de',
                    backgroundColor: '#1a2332',
                });
            }
        });
    }
    
    // ============================================================
    // MAP (World objects)
    // ============================================================
    private createMapVisuals(): void {
        this.pathGraphics = this.add.graphics();
        this.pathGraphics.setDepth(5);
        
        this.drawPaths();
        this.drawNodes();
    }
    
    private drawPaths(): void {
        const paths = Systems.map.getPaths();
        const drawnPaths = new Set<string>();
        const fog = GameState.getFog();
        
        this.pathGraphics.clear();
        
        paths.forEach(path => {
            const key = [path.from, path.to].sort().join('_');
            if (drawnPaths.has(key)) return;
            drawnPaths.add(key);
            
            const fromNode = Systems.map.getNode(path.from);
            const toNode = Systems.map.getNode(path.to);
            
            if (!fromNode || !toNode) return;
            if (!fog.revealedNodes.has(fromNode.id) && !fog.revealedNodes.has(toNode.id)) return;
            
            const isVisited = fromNode.isVisited && toNode.isVisited;
            const color = isVisited 
                ? GAME_CONFIG.COLORS.PATH_ACTIVE 
                : GAME_CONFIG.COLORS.PATH_INACTIVE;
            const lineWidth = isVisited ? 4 : 2;
            const alpha = isVisited ? 1.0 : 0.6;
            
            this.pathGraphics.lineStyle(lineWidth, color, alpha);
            this.pathGraphics.beginPath();
            this.pathGraphics.moveTo(fromNode.position.x, fromNode.position.y);
            this.pathGraphics.lineTo(toNode.position.x, toNode.position.y);
            this.pathGraphics.strokePath();
        });
    }
    
    private drawNodes(): void {
        this.nodeSprites.forEach(sprite => sprite.destroy());
        this.nodeSprites.clear();
        this.nodeLabels.forEach(label => label.destroy());
        this.nodeLabels.clear();
        this.nodeIcons.forEach(icon => icon.destroy());
        this.nodeIcons.clear();
        
        const nodes = Systems.map.getAllNodes();
        const fog = GameState.getFog();
        
        nodes.forEach(node => {
            const isRevealed = fog.revealedNodes.has(node.id);
            const isVisited = node.isVisited;
            
            const sprite = this.add.circle(
                node.position.x,
                node.position.y,
                node.getSize(),
                node.getColor(),
                isRevealed ? 0.9 : 0.15
            );
            
            sprite.setStrokeStyle(
                isVisited ? 3 : 2,
                isRevealed ? 0xffffff : 0x4a5568,
                isRevealed ? 1.0 : 0.3
            );
            sprite.setDepth(10);
            this.nodeSprites.set(node.id, sprite);
            
            const icon = this.add.text(
                node.position.x - 12,
                node.position.y - 10,
                node.getIcon(),
                { fontSize: '20px' }
            );
            icon.setDepth(11);
            icon.setAlpha(isRevealed ? 1.0 : 0.2);
            this.nodeIcons.set(node.id, icon);
            
            const label = this.add.text(
                node.position.x - 30,
                node.position.y + 25,
                node.getLabel(),
                {
                    fontSize: '11px',
                    color: isRevealed ? '#b0c4de' : '#5a6a7a',
                    fontFamily: 'monospace',
                    align: 'center',
                    backgroundColor: isRevealed ? '#1a2332' : 'transparent',
                    padding: { x: 3, y: 2 },
                }
            );
            label.setDepth(11);
            this.nodeLabels.set(node.id, label);
        });
    }
    
    private createBaseVisual(): void {
        const basePos = GameState.getBasePosition();
        
        this.baseGlow = this.add.circle(basePos.x, basePos.y, 45, 0x00ff9d, 0.2);
        this.baseGlow.setDepth(49);
        
        this.tweens.add({
            targets: this.baseGlow,
            scale: 1.3,
            alpha: 0.05,
            duration: 1500,
            yoyo: true,
            repeat: -1,
        });
        
        this.baseSprite = this.add.circle(basePos.x, basePos.y, 32, 0x00ff9d, 1);
        this.baseSprite.setStrokeStyle(5, 0xffffff, 1);
        this.baseSprite.setDepth(50);
        
        this.baseLabel = this.add.text(basePos.x - 45, basePos.y - 12, '🚂 CLOG', {
            fontSize: '16px',
            color: '#ffffff',
            fontFamily: 'monospace',
            fontStyle: 'bold',
            backgroundColor: '#1a2332',
            padding: { x: 5, y: 3 },
        });
        this.baseLabel.setDepth(51);
    }
    
    private setupCamera(): void {
        const camera = this.cameras.main;
        camera.startFollow(this.baseSprite, true, 0.1, 0.1);
        camera.setZoom(0.5);
        // ✅ ไม่ตั้ง bounds
    }
    
    private setupEventListeners(): void {
        EventBus.on(EVENTS.BASE_MOVED, this.onBaseMoved, this);
        EventBus.on(EVENTS.MAP_FOG_UPDATED, this.onFogUpdated, this);
        EventBus.on(EVENTS.UI_JUNCTION_POPUP, this.onJunctionPopup, this);
        EventBus.on(EVENTS.TIME_PHASE_CHANGE, this.onPhaseChange, this);
        EventBus.on(EVENTS.RAID_TRIGGERED, this.onRaidTriggered, this);
        EventBus.on(EVENTS.TIME_SCALE_CHANGE, this.updateSpeedButtons, this);
        
        EventBus.on('ui:openBaseInterior', () => this.toggleBaseInterior());
    }
    
    private onBaseMoved(data: any): void {
        const basePos = GameState.getBasePosition();
        
        if (this.baseSprite) {
            this.baseSprite.x = basePos.x;
            this.baseSprite.y = basePos.y;
        }
        if (this.baseLabel) {
            this.baseLabel.x = basePos.x - 45;
            this.baseLabel.y = basePos.y - 12;
        }
        if (this.baseGlow) {
            this.baseGlow.x = basePos.x;
            this.baseGlow.y = basePos.y;
        }
    }
    
    private onFogUpdated(): void {
        this.drawPaths();
    }
    
    private onJunctionPopup(data: any): void {
        this.junctionPopup.show(data.junction, data.choices);
    }
    
    private onPhaseChange(data: any): void {
        if (data.phase === 'night') {
            this.cameras.main.setBackgroundColor('#0f1821');
        } else {
            this.cameras.main.setBackgroundColor(GAME_CONFIG.COLORS.BG_DARK);
        }
    }
    
    private onRaidTriggered(raid: any): void {
        this.scene.launch('DefenseScene', {
            raidId: raid.id,
            durationHours: raid.durationHours,
        });
    }
    
    private setupInput(): void {
        this.input.keyboard?.on('keydown-SPACE', () => {
            Systems.time.cycleTimeScale();
        });
        
        this.input.keyboard?.on('keydown-P', () => {
            this.openPauseMenu();
        });
        
        this.input.keyboard?.on('keydown-ESC', () => {
            this.openPauseMenu();
        });
        
        this.input.keyboard?.on('keydown-TAB', () => {
            this.toggleBaseInterior();
        });
    }
    
    private openPauseMenu(): void {
        if (this.scene.isActive('PauseScene')) return;
        this.scene.launch('PauseScene');
        this.scene.pause();
        Systems.time.pause();
    }
    
    private toggleBaseInterior(): void {
        if (this.scene.isActive('BaseInteriorScene')) {
            this.scene.stop('BaseInteriorScene');
        } else {
            this.scene.launch('BaseInteriorScene');
        }
    }
    
    private showTutorial(): void {
        const text = this.add.text(
            GAME_CONFIG.WIDTH / 2,
            GAME_CONFIG.HEIGHT - 50,
            '🚂 CLOG is moving... Watch the map!',
            {
                fontSize: '18px',
                color: '#00e5cc',
                fontFamily: 'monospace',
                fontStyle: 'bold',
                backgroundColor: '#1a2332',
                padding: { x: 15, y: 8 },
            }
        );
        text.setOrigin(0.5);
        text.setScrollFactor(0);
        text.setDepth(2000);
        this.registerUI(text);
        
        this.tweens.add({
            targets: text,
            alpha: 0,
            duration: 3000,
            delay: 2000,
            onComplete: () => text.destroy(),
        });
    }
    
    update(time: number, delta: number): void {
        if (this.fog) this.fog.update();
        if (this.minimap) this.minimap.update();
    }
    
    shutdown(): void {
        if (this.hud) this.hud.destroy();
        if (this.minimap) this.minimap.destroy();
        if (this.fog) this.fog.destroy();
        if (this.junctionPopup) this.junctionPopup.destroy();
        if (this.minimizedBase) this.minimizedBase.destroy();
        
        EventBus.off(EVENTS.BASE_MOVED, this.onBaseMoved, this);
        EventBus.off(EVENTS.MAP_FOG_UPDATED, this.onFogUpdated, this);
        EventBus.off(EVENTS.UI_JUNCTION_POPUP, this.onJunctionPopup, this);
        EventBus.off(EVENTS.TIME_PHASE_CHANGE, this.onPhaseChange, this);
        EventBus.off(EVENTS.RAID_TRIGGERED, this.onRaidTriggered, this);
        EventBus.off(EVENTS.TIME_SCALE_CHANGE, this.updateSpeedButtons, this);
    }
}