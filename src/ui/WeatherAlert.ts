import Phaser from 'phaser'
import { SoundService } from '../systems/SoundService'

export class WeatherAlert {
  scene: Phaser.Scene
  container: Phaser.GameObjects.Container | null = null
  isShowing = false

  constructor(scene: Phaser.Scene) {
    this.scene = scene
  }

  showTornadoAlert(efRating: number, location: string = 'NEARBY') {
    if (this.isShowing) return
    this.isShowing = true

    const width = 350
    const height = 120
    // Position on LEFT side to avoid minimap (which is on right)
    const x = 20
    const y = 180 // Below HUD elements

    this.container = this.scene.add.container(x, y)
    this.container.setScrollFactor(0)
    this.container.setDepth(150)

    // Alert background (semi-transparent red)
    const bg = this.scene.add.graphics()
    bg.fillStyle(0xcc0000, 0.9)
    bg.fillRoundedRect(0, 0, width, height, 8)
    bg.lineStyle(3, 0xff0000, 1)
    bg.strokeRoundedRect(0, 0, width, height, 8)
    this.container.add(bg)

    // "TORNADO WARNING" header
    const headerBg = this.scene.add.graphics()
    headerBg.fillStyle(0xff0000, 1)
    headerBg.fillRoundedRect(0, 0, width, 30, {tl: 8, tr: 8, bl: 0, br: 0} as any)
    this.container.add(headerBg)

    const header = this.scene.add.text(width / 2, 15, '⚠️ TORNADO WARNING ⚠️', {
      fontFamily: 'monospace',
      fontSize: '16px',
      color: '#ffffff',
      fontStyle: 'bold'
    }).setOrigin(0.5)
    this.container.add(header)

    // EF Rating
    const efText = this.scene.add.text(10, 40, `EF${Math.floor(efRating)} TORNADO DETECTED`, {
      fontFamily: 'monospace',
      fontSize: '14px',
      color: '#ffff00',
      fontStyle: 'bold'
    })
    this.container.add(efText)

    // Location
    const locText = this.scene.add.text(10, 60, `Location: ${location}`, {
      fontFamily: 'monospace',
      fontSize: '12px',
      color: '#ffffff'
    })
    this.container.add(locText)

    // Advice
    const adviceText = this.scene.add.text(10, 80, 'SEEK SHELTER IMMEDIATELY', {
      fontFamily: 'monospace',
      fontSize: '11px',
      color: '#ffffff',
      fontStyle: 'italic'
    })
    this.container.add(adviceText)

    // Close button (X)
    const closeBtn = this.scene.add.text(width - 25, 15, '✕', {
      fontFamily: 'sans-serif',
      fontSize: '20px',
      color: '#ffffff'
    }).setOrigin(0.5).setInteractive({ useHandCursor: true })
    
    closeBtn.on('pointerdown', () => this.dismiss())
    closeBtn.on('pointerover', () => closeBtn.setColor('#ffff00'))
    closeBtn.on('pointerout', () => closeBtn.setColor('#ffffff'))
    this.container.add(closeBtn)

    // "(Press X to dismiss)" hint
    const hintText = this.scene.add.text(10, 100, 'Press X to dismiss', {
      fontFamily: 'monospace',
      fontSize: '9px',
      color: '#cccccc',
      fontStyle: 'italic'
    })
    this.container.add(hintText)

    // Live indicator (flashing)
    const liveIndicator = this.scene.add.graphics()
    liveIndicator.fillStyle(0xff0000, 1)
    liveIndicator.fillCircle(width - 50, 105, 5)
    this.container.add(liveIndicator)

    const liveText = this.scene.add.text(width - 50, 100, 'LIVE', {
      fontFamily: 'monospace',
      fontSize: '10px',
      color: '#ff0000',
      fontStyle: 'bold'
    })
    this.container.add(liveText)

    // Flashing animation
    this.scene.tweens.add({
      targets: liveIndicator,
      alpha: 0,
      duration: 500,
      yoyo: true,
      repeat: -1
    })

    // Slide in from left
    this.container.x = -width
    this.scene.tweens.add({
      targets: this.container,
      x: x,
      duration: 500,
      ease: 'Power2'
    })

    // Set up X key listener for dismissal
    const xKey = this.scene.input.keyboard!.addKey('X')
    const xKeyHandler = () => {
      SoundService.playClick()
      this.dismiss()
      xKey.off('down', xKeyHandler)
    }
    xKey.on('down', xKeyHandler)

    // Show ticker - RUNS TWICE THEN CLOSES
    this.showTickerWithCallback(`TORNADO WARNING: EF${Math.floor(efRating)} tornado detected in ${location}. Take shelter immediately!`, () => {
      // First ticker complete, run second ticker
      this.showTickerWithCallback(`CONTINUED COVERAGE: EF${Math.floor(efRating)} tornado moving through ${location}. Stay tuned for updates.`, () => {
        // Second ticker complete, dismiss alert
        this.dismiss()
        xKey.off('down', xKeyHandler)
      })
    })

    // Alert sound!
    SoundService.playAlert()
  }

  dismiss() {
    if (!this.container || !this.isShowing) return

    // Slide out to right
    this.scene.tweens.add({
      targets: this.container,
      x: this.scene.scale.width,
      duration: 500,
      ease: 'Power2',
      onComplete: () => {
        this.container?.destroy()
        this.container = null
        this.isShowing = false
      }
    })
  }

  showTicker(message: string) {
    // News ticker at bottom of screen - RUNS ONCE THEN CLOSES (for tornado end)
    this.showTickerWithCallback(message, () => {
      // Single ticker complete, no callback needed
    })
  }

  showTickerWithCallback(message: string, onComplete?: () => void) {
    // Clear any existing ticker to prevent overlap
    this.clearExistingTicker()

    const tickerHeight = 35
    const tickerY = this.scene.scale.height - tickerHeight - 5

    const tickerBg = this.scene.add.graphics()
    tickerBg.fillStyle(0x000000, 0.9)
    tickerBg.fillRect(0, tickerY, this.scene.scale.width, tickerHeight)
    tickerBg.setScrollFactor(0)
    tickerBg.setDepth(149)

    // "BREAKING NEWS" label
    const breakingLabel = this.scene.add.text(15, tickerY + 17, 'BREAKING NEWS', {
      fontFamily: 'monospace',
      fontSize: '12px',
      color: '#ff0000',
      backgroundColor: '#ffffff',
      padding: { x: 8, y: 3 },
      stroke: '#000000',
      strokeThickness: 1
    }).setOrigin(0, 0.5).setScrollFactor(0).setDepth(150)

    // Scrolling message
    const tickerText = this.scene.add.text(180, tickerY + 17, message, {
      fontFamily: 'monospace',
      fontSize: '12px',
      color: '#ffffff',
      stroke: '#000000',
      strokeThickness: 1
    }).setOrigin(0, 0.5).setScrollFactor(0).setDepth(150)

    // Scroll animation
    this.scene.tweens.add({
      targets: tickerText,
      x: -tickerText.width - 50,
      duration: 15000, // Faster scroll for better UX
      ease: 'Linear',
      onComplete: () => {
        tickerText.destroy()
        tickerBg.destroy()
        breakingLabel.destroy()
        if (onComplete) onComplete()
      }
    })
  }

  private clearExistingTicker() {
    // Clear any existing ticker elements
    this.scene.children.list.forEach(child => {
      if (child.depth === 149 || child.depth === 150) {
        if (child instanceof Phaser.GameObjects.Text || child instanceof Phaser.GameObjects.Graphics) {
          child.destroy()
        }
      }
    })
  }
}

