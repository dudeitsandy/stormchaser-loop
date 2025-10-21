import Phaser from 'phaser'

export class Boot extends Phaser.Scene {
  constructor() { super('Boot') }
  preload() {
    // Create procedurally-generated textures
    const g = this.add.graphics()
    
    // Single vehicle sprite that rotates smoothly
    this.createVehicleSprite(g)

    // Fuel pickup
    g.fillStyle(0xffe066, 1).fillCircle(6, 6, 6)
    g.fillStyle(0xffd700, 1).fillCircle(6, 6, 4)
    g.generateTexture('pickup_fuel', 12, 12)
    g.clear()

    // Combo pickup
    g.fillStyle(0xa077ff, 1).fillCircle(6, 6, 6)
    g.fillStyle(0xc298ff, 1).fillCircle(6, 6, 4)
    g.generateTexture('pickup_combo', 12, 12)
    g.clear()

    // Tornado core (will be enhanced with particles)
    g.fillStyle(0xdddddd, 1).fillCircle(10, 10, 10)
    g.fillStyle(0xffffff, 1).fillCircle(10, 10, 6)
    g.generateTexture('tornado', 20, 20)
    g.destroy()
  }

  createVehicleSprite(g: Phaser.GameObjects.Graphics) {
    g.clear()
    
    // Vehicle dimensions (pointing UP by default, will rotate)
    const centerX = 12
    const centerY = 12
    const length = 16
    const width = 11
    
    // Colors
    const bodyColor = 0x2c3e50  // Dark blue-gray
    const hoodColor = 0xe74c3c  // Red
    const windowColor = 0x34495e // Dark gray
    const wheelColor = 0x1a1a1a // Black
    const accentColor = 0xff6b6b // Light red
    const lightColor = 0xffaa00 // Orange/yellow
    
    // Draw vehicle pointing UP (will rotate in game)
    
    // Back bumper
    g.fillStyle(bodyColor, 1)
    g.fillRect(centerX - width/2, centerY + length/2 - 2, width, 2)
    
    // Back wheels (outside body)
    g.fillStyle(wheelColor, 1)
    g.fillRect(centerX - width/2 - 1, centerY + length/4 - 1, 2, 4)
    g.fillRect(centerX + width/2 - 1, centerY + length/4 - 1, 2, 4)
    
    // Main body
    g.fillStyle(bodyColor, 1)
    g.fillRect(centerX - width/2, centerY - length/2 + 3, width, length - 5)
    
    // Windshield (dark)
    g.fillStyle(windowColor, 1)
    g.fillRect(centerX - width/2 + 2, centerY - length/2 + 8, width - 4, 5)
    
    // Front wheels (outside body)
    g.fillStyle(wheelColor, 1)
    g.fillRect(centerX - width/2 - 1, centerY - length/4 - 1, 2, 4)
    g.fillRect(centerX + width/2 - 1, centerY - length/4 - 1, 2, 4)
    
    // Hood (red front)
    g.fillStyle(hoodColor, 1)
    g.fillRect(centerX - width/2, centerY - length/2 + 3, width, 5)
    
    // Hood accent stripe
    g.fillStyle(accentColor, 1)
    g.fillRect(centerX - width/2 + 1, centerY - length/2 + 4, width - 2, 2)
    
    // Roof equipment/lights
    g.fillStyle(lightColor, 1)
    g.fillCircle(centerX - 2, centerY + 1, 1.5)
    g.fillCircle(centerX + 2, centerY + 1, 1.5)
    
    // Front bumper detail
    g.fillStyle(0x444444, 1)
    g.fillRect(centerX - width/2 + 1, centerY - length/2 + 2, width - 2, 1)
    
    g.generateTexture('player_0', 24, 24)
  }

  create() { this.scene.start('Title') }
}

