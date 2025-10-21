import Phaser from 'phaser'
import { ConfigService } from '../systems/Config'
import { SoundService } from '../systems/SoundService'

export class Title extends Phaser.Scene {
  constructor() { super('Title') }
  
  async create() {
    await ConfigService.instance.load('/remote-config.json')
    const { width, height } = this.scale

    // Dark stormy background gradient
    const bg = this.add.graphics()
    bg.fillGradientStyle(0x0a0e12, 0x0a0e12, 0x1a1e22, 0x1a1e22, 1)
    bg.fillRect(0, 0, width, height)

    // Create pixel art scene
    this.createPixelArtScene()

    // Title
    const title = this.add.text(width/2, 80, 'STORMCHASER', {
      fontFamily: 'monospace',
      fontSize: '38px',
      color: '#e6edf3',
      stroke: '#000000',
      strokeThickness: 6,
      fontStyle: 'bold'
    }).setOrigin(0.5)

    const subtitle = this.add.text(width/2, 120, 'LOOP', {
      fontFamily: 'monospace',
      fontSize: '32px',
      color: '#93e1ff',
      stroke: '#000000',
      strokeThickness: 5
    }).setOrigin(0.5)

    // Pulse animation on title
    this.tweens.add({
      targets: [title, subtitle],
      scale: 1.02,
      duration: 1500,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    })

    // Instructions
    this.add.text(width/2, height/2 + 30, 'WASD / Arrows - Drive', {
      fontFamily: 'monospace',
      fontSize: '14px',
      color: '#aab3c2',
      stroke: '#000000',
      strokeThickness: 3
    }).setOrigin(0.5)

    this.add.text(width/2, height/2 + 50, 'SPACE - Aim Camera (hold 2 seconds)', {
      fontFamily: 'monospace',
      fontSize: '14px',
      color: '#aab3c2',
      stroke: '#000000',
      strokeThickness: 3
    }).setOrigin(0.5)

    this.add.text(width/2, height/2 + 70, 'X - Dismiss Alerts', {
      fontFamily: 'monospace',
      fontSize: '14px',
      color: '#aab3c2',
      stroke: '#000000',
      strokeThickness: 3
    }).setOrigin(0.5)

    // Start prompt
    const startText = this.add.text(width/2, height - 60, 'Press ENTER to start', {
      fontFamily: 'monospace',
      fontSize: '18px',
      color: '#93e1ff',
      stroke: '#000000',
      strokeThickness: 4,
      fontStyle: 'bold'
    }).setOrigin(0.5)

    // Pulse start button
    this.tweens.add({
      targets: startText,
      alpha: 0.6,
      duration: 1000,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    })

    // Copyright - Ghostweave Games
    this.add.text(width/2, height - 25, '© 2025 Ghostweave Games', {
      fontFamily: 'monospace',
      fontSize: '11px',
      color: '#666666',
      fontStyle: 'italic'
    }).setOrigin(0.5)

    // Version
    this.add.text(10, height - 15, 'v0.1.0', {
      fontFamily: 'monospace',
      fontSize: '10px',
      color: '#444444'
    })

    this.input.keyboard!.once('keydown-ENTER', () => {
      SoundService.playClick()
      this.scene.start('Game')
    })
  }

  createPixelArtScene() {
    // Pixel art: Tornado chasing car chasing tornado
    const { width } = this.scale
    const centerY = 220

    // Create pixel art graphics
    const pixelArt = this.add.graphics()

    // Ground (green fields)
    pixelArt.fillStyle(0x7cb342, 1)
    pixelArt.fillRect(width/2 - 200, centerY + 40, 400, 60)
    
    // Road (gray)
    pixelArt.fillStyle(0x666666, 1)
    pixelArt.fillRect(width/2 - 200, centerY + 50, 400, 30)
    
    // Road center line (yellow dashes)
    pixelArt.fillStyle(0xffdd00, 1)
    for (let i = 0; i < 400; i += 30) {
      if ((i / 30) % 2 === 0) {
        pixelArt.fillRect(width/2 - 200 + i, centerY + 63, 20, 3)
      }
    }

    // Car (chase vehicle) - Left side
    const carX = width/2 - 80
    const carY = centerY + 58
    
    // Car body
    pixelArt.fillStyle(0x2c3e50, 1)
    pixelArt.fillRect(carX, carY, 20, 14)
    
    // Car hood (red)
    pixelArt.fillStyle(0xe74c3c, 1)
    pixelArt.fillRect(carX + 16, carY + 2, 4, 10)
    
    // Windshield
    pixelArt.fillStyle(0x34495e, 1)
    pixelArt.fillRect(carX + 12, carY + 3, 4, 8)
    
    // Wheels
    pixelArt.fillStyle(0x1a1a1a, 1)
    pixelArt.fillRect(carX + 2, carY - 1, 3, 3)
    pixelArt.fillRect(carX + 2, carY + 12, 3, 3)
    pixelArt.fillRect(carX + 14, carY - 1, 3, 3)
    pixelArt.fillRect(carX + 14, carY + 12, 3, 3)

    // Tornado - Right side (larger, more dramatic)
    const tornadoX = width/2 + 80
    const tornadoY = centerY + 20
    
    // Tornado funnel (simplified)
    pixelArt.fillStyle(0x666666, 0.8)
    pixelArt.fillTriangle(
      tornadoX, tornadoY + 60,
      tornadoX - 25, tornadoY,
      tornadoX + 25, tornadoY
    )
    
    // Tornado core (white)
    pixelArt.fillStyle(0xeeeeee, 0.9)
    pixelArt.fillTriangle(
      tornadoX, tornadoY + 60,
      tornadoX - 18, tornadoY + 10,
      tornadoX + 18, tornadoY + 10
    )
    
    // Debris around tornado
    for (let i = 0; i < 15; i++) {
      const debrisX = tornadoX + Phaser.Math.Between(-40, 40)
      const debrisY = tornadoY + Phaser.Math.Between(0, 60)
      pixelArt.fillStyle(0x8b7355, 0.7)
      pixelArt.fillRect(debrisX, debrisY, 2, 2)
    }

    // Storm clouds
    pixelArt.fillStyle(0x2a2a3a, 0.8)
    pixelArt.fillCircle(tornadoX - 40, tornadoY - 10, 30)
    pixelArt.fillCircle(tornadoX, tornadoY - 15, 35)
    pixelArt.fillCircle(tornadoX + 40, tornadoY - 10, 30)
    
    pixelArt.fillStyle(0x3a3a4a, 0.6)
    pixelArt.fillCircle(tornadoX - 20, tornadoY - 20, 25)
    pixelArt.fillCircle(tornadoX + 20, tornadoY - 20, 25)

    // Motion lines behind car (speed effect)
    pixelArt.lineStyle(2, 0x93e1ff, 0.5)
    for (let i = 0; i < 5; i++) {
      const y = carY + 3 + (i * 2)
      pixelArt.lineBetween(carX - 20 - (i * 3), y, carX - 5 - (i * 3), y)
    }

    // Add spinning animation to tornado debris
    this.tweens.add({
      targets: pixelArt,
      rotation: 0.02,
      duration: 100,
      yoyo: true,
      repeat: -1
    })
  }
}
