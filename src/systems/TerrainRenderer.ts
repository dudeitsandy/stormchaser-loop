import Phaser from 'phaser'
import { TerrainTile } from './TerrainGenerator'

export class TerrainRenderer {
  scene: Phaser.Scene
  tileSize: number

  constructor(scene: Phaser.Scene, tileSize: number = 50) {
    this.scene = scene
    this.tileSize = tileSize
  }

  renderTile(tile: TerrainTile): Phaser.GameObjects.Container {
    const container = this.scene.add.container(tile.x, tile.y)
    container.setDepth(-10)

    // SIMPLIFIED RENDERING FOR PERFORMANCE
    // Just basic shapes, no complex loops or textures
    
    const half = this.tileSize / 2
    const g = this.scene.add.graphics()

    switch (tile.type) {
      case 'highway':
      case 'highway_horizontal':
        // Dark asphalt with yellow line (horizontal)
        g.fillStyle(0x3d3d3d, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        g.fillStyle(0xffdd00, 1)
        g.fillRect(-half, -2, this.tileSize, 4) // Horizontal center line
        break

      case 'highway_vertical':
        // Dark asphalt with yellow line (vertical)
        g.fillStyle(0x3d3d3d, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        g.fillStyle(0xffdd00, 1)
        g.fillRect(-2, -half, 4, this.tileSize) // Vertical center line
        break

      case 'road':
      case 'road_horizontal':
        // Gray road with dashed line (horizontal)
        g.fillStyle(0x666666, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        g.fillStyle(0xdddddd, 1)
        g.fillRect(-half, -1, this.tileSize / 3, 2)
        g.fillRect(this.tileSize / 6, -1, this.tileSize / 3, 2)
        break

      case 'road_vertical':
        // Gray road with dashed line (vertical)
        g.fillStyle(0x666666, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        g.fillStyle(0xdddddd, 1)
        g.fillRect(-1, -half, 2, this.tileSize / 3)
        g.fillRect(-1, this.tileSize / 6, 2, this.tileSize / 3)
        break

      case 'field':
        // Yellow-green field
        g.fillStyle(0x9acd32, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Simple row lines
        g.lineStyle(1, 0x88bb22, 0.4)
        for (let i = 0; i < this.tileSize; i += 8) {
          g.lineBetween(-half, -half + i, half, -half + i)
        }
        break

      case 'forest':
        // Dark green forest
        g.fillStyle(0x4a7c2c, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Minimal texture
        g.fillStyle(0x3a6c1c, 0.5)
        g.fillCircle(-half/2, -half/2, 8)
        g.fillCircle(half/2, half/2, 8)
        break

      case 'grass':
        // Simple grass
        g.fillStyle(0x7cb342, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        break

      case 'water':
        // Blue water
        g.fillStyle(0x2196f3, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Simple highlight
        g.fillStyle(0x64b5f6, 0.3)
        g.fillCircle(0, 0, this.tileSize / 3)
        break

      case 'mud':
        // Brown mud
        g.fillStyle(0x795548, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        break

      case 'building':
        // Brown building with 3D depth effect
        // Base shadow
        g.fillStyle(0x2a2a2a, 0.8)
        g.fillRect(-half + 3, -half + 3, this.tileSize - 6, this.tileSize - 6)
        // Main building
        g.fillStyle(0x8b7355, 1)
        g.fillRect(-half, -half, this.tileSize - 6, this.tileSize - 6)
        // Top face (lighter)
        g.fillStyle(0x9a8365, 1)
        g.fillRect(-half, -half, this.tileSize - 6, 3)
        // Right face (darker)
        g.fillStyle(0x6b5a47, 1)
        g.fillRect(half - 9, -half, 3, this.tileSize - 6)
        // Windows with depth
        g.fillStyle(0x1a1a1a, 1)
        g.fillRect(-half + 8, -half + 8, 6, 6)
        g.fillRect(-half + 20, -half + 8, 6, 6)
        g.fillRect(-half + 8, -half + 20, 6, 6)
        g.fillRect(-half + 20, -half + 20, 6, 6)
        // Window frames
        g.lineStyle(1, 0x4a4a4a, 1)
        g.strokeRect(-half + 8, -half + 8, 6, 6)
        g.strokeRect(-half + 20, -half + 8, 6, 6)
        g.strokeRect(-half + 8, -half + 20, 6, 6)
        g.strokeRect(-half + 20, -half + 20, 6, 6)
        break

      case 'house':
        // Residential house with 3D depth effect
        // Base shadow
        g.fillStyle(0x2a2a2a, 0.6)
        g.fillRect(-half + 2, -half + 2, this.tileSize - 4, this.tileSize - 4)
        // Main house body
        g.fillStyle(0x8b4513, 1)
        g.fillRect(-half, -half, this.tileSize - 4, this.tileSize - 4)
        // Right face (darker)
        g.fillStyle(0x6b3a0a, 1)
        g.fillRect(half - 6, -half, 4, this.tileSize - 4)
        // Roof triangle with depth
        g.fillStyle(0x654321, 1)
        g.beginPath()
        g.moveTo(-half, -half)
        g.lineTo(half - 4, -half)
        g.lineTo(0, -half - 8)
        g.closePath()
        g.fillPath()
        // Roof shadow
        g.fillStyle(0x4a2c2a, 1)
        g.beginPath()
        g.moveTo(half - 4, -half)
        g.lineTo(half, -half + 2)
        g.lineTo(2, -half - 6)
        g.closePath()
        g.fillPath()
        // Door with depth
        g.fillStyle(0x2a1a1a, 1)
        g.fillRect(-half + 15, -half + 10, 8, 15)
        g.fillStyle(0x4a2c2a, 1)
        g.fillRect(-half + 14, -half + 9, 8, 15)
        // Window with depth
        g.fillStyle(0x1a1a1a, 1)
        g.fillRect(-half + 5, -half + 5, 6, 6)
        g.fillStyle(0x2a2a2a, 1)
        g.fillRect(-half + 4, -half + 4, 6, 6)
        g.lineStyle(1, 0x4a4a4a, 1)
        g.strokeRect(-half + 4, -half + 4, 6, 6)
        break

      case 'barn':
        // Red barn with 3D depth effect
        // Base shadow
        g.fillStyle(0x2a2a2a, 0.7)
        g.fillRect(-half + 3, -half + 3, this.tileSize - 6, this.tileSize - 6)
        // Main barn body
        g.fillStyle(0x8b0000, 1)
        g.fillRect(-half, -half, this.tileSize - 6, this.tileSize - 6)
        // Right face (darker)
        g.fillStyle(0x6b0000, 1)
        g.fillRect(half - 9, -half, 6, this.tileSize - 6)
        // White trim with depth
        g.fillStyle(0xffffff, 1)
        g.fillRect(-half, -half, this.tileSize - 6, 4)
        g.fillRect(-half, half - 10, this.tileSize - 6, 4)
        g.fillRect(-half, -half, 4, this.tileSize - 6)
        g.fillRect(half - 10, -half, 4, this.tileSize - 6)
        // Barn door with depth
        g.fillStyle(0x2a1a1a, 1)
        g.fillRect(-half + 10, -half + 15, 20, 15)
        g.fillStyle(0x4a2c2a, 1)
        g.fillRect(-half + 9, -half + 14, 20, 15)
        // Windows with depth
        g.fillStyle(0x1a1a1a, 1)
        g.fillRect(-half + 5, -half + 5, 8, 6)
        g.fillRect(half - 19, -half + 5, 8, 6)
        g.fillStyle(0x2a2a2a, 1)
        g.fillRect(-half + 4, -half + 4, 8, 6)
        g.fillRect(half - 20, -half + 4, 8, 6)
        break

      case 'silo':
        // Tall cylindrical silo
        g.fillStyle(0x696969, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Silo dome
        g.fillStyle(0x4a4a4a, 1)
        g.fillEllipse(0, -half - 4, this.tileSize - 4, 8)
        // Silo bands
        g.lineStyle(2, 0x8a8a8a, 1)
        g.lineBetween(-half + 2, -half + 10, half - 2, -half + 10)
        g.lineBetween(-half + 2, -half + 20, half - 2, -half + 20)
        g.lineBetween(-half + 2, half - 10, half - 2, half - 10)
        break

      case 'office':
        // Modern office building
        g.fillStyle(0x708090, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Glass windows
        g.fillStyle(0x4682b4, 1)
        g.fillRect(-half + 3, -half + 3, this.tileSize - 6, this.tileSize - 6)
        // Window grid
        g.lineStyle(1, 0x2a4a6a, 1)
        for (let i = 0; i < this.tileSize - 6; i += 6) {
          g.lineBetween(-half + 3, -half + 3 + i, half - 3, -half + 3 + i)
          g.lineBetween(-half + 3 + i, -half + 3, -half + 3 + i, half - 3)
        }
        break

      case 'gas_station':
        // Gas station with pumps
        g.fillStyle(0xff6347, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Building
        g.fillStyle(0xffffff, 1)
        g.fillRect(-half + 5, -half + 5, 20, 20)
        // Gas pumps
        g.fillStyle(0x4a4a4a, 1)
        g.fillRect(-half + 8, -half + 25, 4, 8)
        g.fillRect(-half + 18, -half + 25, 4, 8)
        // Sign
        g.fillStyle(0xff0000, 1)
        g.fillRect(-half + 10, -half + 2, 10, 3)
        break

      case 'parking':
        // Parking lot with lines
        g.fillStyle(0x696969, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Parking lines
        g.lineStyle(1, 0xffffff, 1)
        g.lineBetween(-half, -half + 8, half, -half + 8)
        g.lineBetween(-half, -half + 16, half, -half + 16)
        g.lineBetween(-half, -half + 24, half, -half + 24)
        g.lineBetween(-half, -half + 32, half, -half + 32)
        break

      case 'sidewalk':
        // Sidewalk with texture
        g.fillStyle(0x696969, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        // Sidewalk pattern
        g.lineStyle(1, 0x8a8a8a, 0.5)
        for (let i = 0; i < this.tileSize; i += 8) {
          g.lineBetween(-half + i, -half, -half + i, half)
        }
        break

      case 'tree':
        // Simple tree
        g.fillStyle(0x7cb342, 1) // Grass
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
        g.fillStyle(0x2d5016, 1) // Foliage
        g.fillCircle(0, 0, 16)
        g.fillStyle(0x4e342e, 1) // Trunk
        g.fillRect(-2, 8, 4, 12)
        break

      default:
        g.fillStyle(tile.color, 1)
        g.fillRect(-half, -half, this.tileSize, this.tileSize)
    }

    container.add(g)
    return container
  }
}
