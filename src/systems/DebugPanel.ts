import Phaser from 'phaser'

export class DebugPanel {
  scene: Phaser.Scene
  container: Phaser.GameObjects.Container
  debugText: Phaser.GameObjects.Text
  fpsText: Phaser.GameObjects.Text
  visible: boolean = false

  constructor(scene: Phaser.Scene) {
    this.scene = scene

    // Background
    const bg = scene.add.rectangle(0, 0, 300, 200, 0x000000, 0.8)
    bg.setOrigin(0, 0)

    // Debug info text
    this.debugText = scene.add.text(10, 10, '', {
      fontFamily: 'monospace',
      fontSize: '11px',
      color: '#00ff00'
    })

    // FPS text
    this.fpsText = scene.add.text(10, 180, '', {
      fontFamily: 'monospace',
      fontSize: '11px',
      color: '#ffff00'
    })

    this.container = scene.add.container(10, 10)
    this.container.add([bg, this.debugText, this.fpsText])
    this.container.setScrollFactor(0)
    this.container.setDepth(1000)
    this.container.setVisible(this.visible)

    // Toggle with backtick key
    scene.input.keyboard!.on('keydown-BACKQUOTE', () => {
      this.visible = !this.visible
      this.container.setVisible(this.visible)
    })
  }

  update(additionalInfo: any = {}) {
    if (!this.visible) return

    const scene = this.scene
    const game = scene.game

    // Get active scenes
    const activeScenes = game.scene.scenes
      .filter(s => s.scene.isActive())
      .map(s => s.scene.key)
      .join(', ')

    // Build debug info
    const info = [
      `DEBUG PANEL (~ to toggle)`,
      `─────────────────────────`,
      `Scene: ${scene.scene.key}`,
      `Active Scenes: ${activeScenes || 'NONE!'}`,
      ``,
      `Game State:`,
      `  paused: ${additionalInfo.paused ?? 'N/A'}`,
      `  gameOver: ${additionalInfo.gameOver ?? 'N/A'}`,
      `  health: ${additionalInfo.health ?? 'N/A'}`,
      `  timeLeft: ${additionalInfo.timeLeft?.toFixed(1) ?? 'N/A'}`,
      ``,
      `Objects:`,
      `  Player exists: ${additionalInfo.playerExists ?? 'N/A'}`,
      `  Tornado exists: ${additionalInfo.tornadoExists ?? 'N/A'}`,
      `  Camera following: ${scene.cameras?.main?.worldView ? 'YES' : 'NO'}`,
    ].join('\n')

    this.debugText.setText(info)

    // FPS
    const fps = Math.round(game.loop.actualFps)
    const fpsColor = fps >= 50 ? '#00ff00' : fps >= 30 ? '#ffff00' : '#ff0000'
    this.fpsText.setColor(fpsColor)
    this.fpsText.setText(`FPS: ${fps}`)
  }
}

