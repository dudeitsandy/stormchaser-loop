import Phaser from 'phaser'
import { Photo } from '../systems/CameraSystem'
import { SoundService } from '../systems/SoundService'

export class GameOver extends Phaser.Scene {
  private score = 0
  private cause = 'tornado'
  private photos: Photo[] = []
  
  constructor() { super('GameOver') }

  init(data: { score: number, cause: string, photos?: Photo[] }) {
    console.log('💀 GameOver scene init:', data)
    this.score = data?.score ?? 0
    this.cause = data?.cause ?? 'tornado'
    this.photos = data?.photos ?? []
  }

  create() {
    console.log('💀 GameOver scene create() START')
    
    const { width, height } = this.scale

    // IMMEDIATE SOLID BACKGROUND - ALWAYS VISIBLE
    this.add.rectangle(width/2, height/2, width, height, 0x1a0a0a, 1)
    
    // BIG CONFIRMATION TEXT USERS CAN SEE
    this.add.text(width/2, 20, '★ GAMEOVER SCREEN ★', {
      fontFamily: 'monospace',
      fontSize: '16px',
      color: '#00ff00',
      stroke: '#000000',
      strokeThickness: 3
    }).setOrigin(0.5)

    // Main title - LARGE and VISIBLE
    const mainText = 'GAME OVER'
    const mainColor = '#ff4444'
    
    this.add.text(width/2, height/2 - 80, mainText, {
      fontFamily: 'monospace',
      fontSize: '48px',
      color: mainColor,
      stroke: '#000000',
      strokeThickness: 8,
      fontStyle: 'bold'
    }).setOrigin(0.5)

    // Cause
    const causeText = this.cause === 'tornado' ? 'Destroyed by Tornado' : 'Session Complete'
    this.add.text(width/2, height/2 - 30, causeText, {
      fontFamily: 'monospace',
      fontSize: '18px',
      color: '#ffffff',
      stroke: '#000000',
      strokeThickness: 4
    }).setOrigin(0.5)

    // Score - BIG
    this.add.text(width/2, height/2 + 10, `Score: ${this.score}`, {
      fontFamily: 'monospace',
      fontSize: '28px',
      color: '#9be9a8',
      stroke: '#000000',
      strokeThickness: 5
    }).setOrigin(0.5)

    // Photos count  
    this.add.text(width/2, height/2 + 50, `Photos: ${this.photos.length}`, {
      fontFamily: 'monospace',
      fontSize: '18px',
      color: '#aab3c2',
      stroke: '#000000',
      strokeThickness: 3
    }).setOrigin(0.5)

    // OPTIONS - LARGE AND CLEAR
    const optionsY = height/2 + 110

    const retryText = this.add.text(width/2, optionsY, '[ ENTER ] RETRY', {
      fontFamily: 'monospace',
      fontSize: '24px',
      color: '#93e1ff',
      stroke: '#000000',
      strokeThickness: 5,
      fontStyle: 'bold'
    }).setOrigin(0.5)

    const titleText = this.add.text(width/2, optionsY + 45, '[ T ] MENU', {
      fontFamily: 'monospace',
      fontSize: '20px',
      color: '#aab3c2',
      stroke: '#000000',
      strokeThickness: 4
    }).setOrigin(0.5)

    // Pulse retry button
    this.tweens.add({
      targets: retryText,
      scale: 1.1,
      duration: 800,
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut'
    })

    // Keyboard handlers - WORK IMMEDIATELY
    this.input.keyboard!.on('keydown-ENTER', () => {
      console.log('ENTER pressed in GameOver')
      SoundService.playClick()
      this.scene.start('Game')
    })
    
    this.input.keyboard!.on('keydown-T', () => {
      console.log('T pressed in GameOver')
      SoundService.playClick()
      this.scene.start('Title')
    })
    
    console.log('✅ GameOver scene create() COMPLETE')
  }
}
