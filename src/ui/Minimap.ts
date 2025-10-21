import Phaser from 'phaser'
import type { TerrainGenerator } from '../systems/TerrainGenerator'

export class Minimap {
  scene: Phaser.Scene
  container: Phaser.GameObjects.Container
  mapGraphics: Phaser.GameObjects.Graphics
  playerDot: Phaser.GameObjects.Graphics
  tornadoDot: Phaser.GameObjects.Graphics
  
  worldWidth: number
  worldHeight: number
  mapSize = 150
  scale: number

  constructor(scene: Phaser.Scene, worldWidth: number, worldHeight: number) {
    this.scene = scene
    this.worldWidth = worldWidth
    this.worldHeight = worldHeight
    this.scale = this.mapSize / Math.max(worldWidth, worldHeight)

    this.container = scene.add.container(
      scene.scale.width - this.mapSize - 10,
      10
    )
    this.container.setDepth(100).setScrollFactor(0)

    // Background
    const bg = scene.add.graphics()
    bg.fillStyle(0x000000, 0.5)
    bg.fillRoundedRect(0, 0, this.mapSize, this.mapSize, 4)
    bg.lineStyle(2, 0x444444, 1)
    bg.strokeRoundedRect(0, 0, this.mapSize, this.mapSize, 4)
    this.container.add(bg)

    // Map terrain
    this.mapGraphics = scene.add.graphics()
    this.container.add(this.mapGraphics)

    // Player marker
    this.playerDot = scene.add.graphics()
    this.container.add(this.playerDot)

    // Tornado marker
    this.tornadoDot = scene.add.graphics()
    this.container.add(this.tornadoDot)
  }

  renderTerrain(terrain: TerrainGenerator) {
    this.mapGraphics.clear()
    
    // Sample terrain at lower resolution for minimap
    const step = 50 // Sample every 50 pixels
    for (let y = 0; y < this.worldHeight; y += step) {
      for (let x = 0; x < this.worldWidth; x += step) {
        const tile = terrain.getTerrainAt(x, y)
        const mx = (x / this.worldWidth) * this.mapSize
        const my = (y / this.worldHeight) * this.mapSize
        const size = (step / this.worldWidth) * this.mapSize

        // Darker version of terrain colors for minimap
        const color = this.darkenColor(tile.color)
        this.mapGraphics.fillStyle(color, 0.8)
        this.mapGraphics.fillRect(mx, my, size, size)
      }
    }
  }

  update(playerX: number, playerY: number, tornadoX: number, tornadoY: number) {
    // Update player position
    this.playerDot.clear()
    const px = (playerX / this.worldWidth) * this.mapSize
    const py = (playerY / this.worldHeight) * this.mapSize
    this.playerDot.fillStyle(0x00ff00, 1)
    this.playerDot.fillCircle(px, py, 3)
    this.playerDot.lineStyle(1, 0x00ff00, 1)
    this.playerDot.strokeCircle(px, py, 5)

    // Update tornado position (hide if tornado destroyed)
    this.tornadoDot.clear()
    if (tornadoX === 0 && tornadoY === 0) {
      // Tornado destroyed - don't draw anything
      return
    }
    
    const tx = (tornadoX / this.worldWidth) * this.mapSize
    const ty = (tornadoY / this.worldHeight) * this.mapSize
    this.tornadoDot.fillStyle(0xff0000, 1)
    this.tornadoDot.fillCircle(tx, ty, 4)
    this.tornadoDot.lineStyle(1, 0xff4444, 1)
    this.tornadoDot.strokeCircle(tx, ty, 8)
  }

  private darkenColor(color: number): number {
    const r = ((color >> 16) & 0xff) * 0.6
    const g = ((color >> 8) & 0xff) * 0.6
    const b = (color & 0xff) * 0.6
    return (r << 16) | (g << 8) | b
  }
}

