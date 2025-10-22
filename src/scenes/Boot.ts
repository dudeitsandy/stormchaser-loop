import Phaser from 'phaser'

export class Boot extends Phaser.Scene {
  constructor() { super('Boot') }
  preload() {
    // Create procedurally-generated textures
    const g = this.add.graphics()
    
    // Player chase truck (improved look)
    this.createChaseTruckSprite(g)
    
    // NPC vehicles
    this.createCivilianCarSprite(g)
    this.createStormchaserSprite(g)
    
    // Civilian pedestrians
    this.createCivilianSprite(g)

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

  createChaseTruckSprite(g: Phaser.GameObjects.Graphics) {
    g.clear()
    
    // Truck dimensions - LARGER and more truck-like (pointing UP by default)
    const centerX = 14
    const centerY = 14
    const length = 20
    const width = 13
    
    // Colors for chase truck
    const bodyColor = 0x2c3e50  // Dark blue-gray body
    const cabColor = 0x1a252f   // Darker cab
    const hoodColor = 0xe74c3c  // Red hood
    const windowColor = 0x5dade2 // Blue-tinted windshield
    const wheelColor = 0x1a1a1a // Black wheels
    const accentColor = 0xff6b6b // Light red stripe
    const lightColor = 0xffdd00  // Bright yellow lightbar
    const bumperColor = 0x34495e // Gray bumper
    const grillColor = 0x2c2c2c  // Dark grill
    
    // Draw chase truck pointing UP (will rotate in game)
    
    // Truck bed / rear section
    g.fillStyle(bodyColor, 1)
    g.fillRect(centerX - width/2, centerY + length/2 - 6, width, 6)
    
    // Equipment/cargo in bed
    g.fillStyle(0x34495e, 1)
    g.fillRect(centerX - width/2 + 2, centerY + length/2 - 5, width - 4, 3)
    
    // Back wheels (large truck wheels)
    g.fillStyle(wheelColor, 1)
    g.fillRect(centerX - width/2 - 2, centerY + length/4, 3, 5)
    g.fillRect(centerX + width/2 - 1, centerY + length/4, 3, 5)
    
    // Main cab body
    g.fillStyle(cabColor, 1)
    g.fillRect(centerX - width/2, centerY - 1, width, length/2 + 1)
    
    // Side stripes/panels
    g.fillStyle(accentColor, 1)
    g.fillRect(centerX - width/2, centerY + 2, width, 2)
    
    // Windshield (larger, more prominent)
    g.fillStyle(windowColor, 1)
    g.fillRect(centerX - width/2 + 1, centerY - length/2 + 10, width - 2, 6)
    
    // Front wheels
    g.fillStyle(wheelColor, 1)
    g.fillRect(centerX - width/2 - 2, centerY - length/4 - 2, 3, 5)
    g.fillRect(centerX + width/2 - 1, centerY - length/4 - 2, 3, 5)
    
    // Hood (red, prominent)
    g.fillStyle(hoodColor, 1)
    g.fillRect(centerX - width/2, centerY - length/2 + 4, width, 6)
    
    // Hood accent stripe
    g.fillStyle(accentColor, 1)
    g.fillRect(centerX - 1, centerY - length/2 + 5, 2, 4)
    
    // Roof lightbar (prominent yellow bar)
    g.fillStyle(lightColor, 1)
    g.fillRect(centerX - width/2 + 2, centerY + 1, width - 4, 2)
    g.fillCircle(centerX - 3, centerY + 2, 1)
    g.fillCircle(centerX + 3, centerY + 2, 1)
    
    // Front grill
    g.fillStyle(grillColor, 1)
    g.fillRect(centerX - width/2 + 1, centerY - length/2 + 3, width - 2, 2)
    
    // Front bumper
    g.fillStyle(bumperColor, 1)
    g.fillRect(centerX - width/2, centerY - length/2 + 2, width, 2)
    
    // Headlights
    g.fillStyle(0xffffdd, 1)
    g.fillCircle(centerX - width/2 + 2, centerY - length/2 + 3, 1)
    g.fillCircle(centerX + width/2 - 2, centerY - length/2 + 3, 1)
    
    g.generateTexture('player_0', 28, 28)
  }

  createCivilianCarSprite(g: Phaser.GameObjects.Graphics) {
    g.clear()
    
    // Smaller civilian car
    const centerX = 10
    const centerY = 10
    const length = 14
    const width = 9
    
    const bodyColor = 0x3498db  // Blue car
    const windowColor = 0x2c3e50
    const wheelColor = 0x1a1a1a
    
    // Body
    g.fillStyle(bodyColor, 1)
    g.fillRect(centerX - width/2, centerY - length/2 + 2, width, length - 4)
    
    // Windshield
    g.fillStyle(windowColor, 1)
    g.fillRect(centerX - width/2 + 1, centerY - length/2 + 6, width - 2, 4)
    
    // Wheels
    g.fillStyle(wheelColor, 1)
    g.fillRect(centerX - width/2 - 1, centerY + length/4 - 1, 2, 3)
    g.fillRect(centerX + width/2 - 1, centerY + length/4 - 1, 2, 3)
    g.fillRect(centerX - width/2 - 1, centerY - length/4 - 1, 2, 3)
    g.fillRect(centerX + width/2 - 1, centerY - length/4 - 1, 2, 3)
    
    g.generateTexture('civilian_car', 20, 20)
  }

  createStormchaserSprite(g: Phaser.GameObjects.Graphics) {
    g.clear()
    
    // Other storm chaser (similar to player but different color)
    const centerX = 14
    const centerY = 14
    const length = 18
    const width = 12
    
    const bodyColor = 0xf39c12  // Orange chase truck
    const cabColor = 0xd68910
    const windowColor = 0x34495e
    const wheelColor = 0x1a1a1a
    const lightColor = 0xff0000  // Red lightbar
    
    // Body
    g.fillStyle(cabColor, 1)
    g.fillRect(centerX - width/2, centerY - length/2 + 3, width, length - 6)
    
    // Windshield
    g.fillStyle(windowColor, 1)
    g.fillRect(centerX - width/2 + 1, centerY - length/2 + 8, width - 2, 5)
    
    // Wheels
    g.fillStyle(wheelColor, 1)
    g.fillRect(centerX - width/2 - 2, centerY + length/4 - 1, 3, 4)
    g.fillRect(centerX + width/2 - 1, centerY + length/4 - 1, 3, 4)
    g.fillRect(centerX - width/2 - 2, centerY - length/4 - 1, 3, 4)
    g.fillRect(centerX + width/2 - 1, centerY - length/4 - 1, 3, 4)
    
    // Hood
    g.fillStyle(bodyColor, 1)
    g.fillRect(centerX - width/2, centerY - length/2 + 3, width, 5)
    
    // Roof lights
    g.fillStyle(lightColor, 1)
    g.fillRect(centerX - width/2 + 2, centerY + 1, width - 4, 1.5)
    
    g.generateTexture('stormchaser', 28, 28)
  }

  createCivilianSprite(g: Phaser.GameObjects.Graphics) {
    g.clear()
    
    // Simple pedestrian sprite
    const centerX = 8
    const centerY = 8
    
    // Body (oval)
    g.fillStyle(0xe74c3c, 1)  // Red shirt
    g.fillEllipse(centerX, centerY + 2, 6, 8)
    
    // Head
    g.fillStyle(0xf0c090, 1)  // Skin tone
    g.fillCircle(centerX, centerY - 2, 3)
    
    // Legs
    g.fillStyle(0x2c3e50, 1)  // Blue pants
    g.fillRect(centerX - 2, centerY + 5, 2, 4)
    g.fillRect(centerX, centerY + 5, 2, 4)
    
    g.generateTexture('civilian', 16, 16)
  }

  create() { this.scene.start('Title') }
}

