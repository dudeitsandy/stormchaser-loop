import Phaser from 'phaser'
import { SpeedConverter } from '../systems/SpeedConverter'

export class HUD {
  scene: Phaser.Scene
  timeText!: Phaser.GameObjects.Text
  scoreText!: Phaser.GameObjects.Text
  comboText!: Phaser.GameObjects.Text
  healthBar!: Phaser.GameObjects.Graphics
  healthBarBg!: Phaser.GameObjects.Graphics
  healthText!: Phaser.GameObjects.Text  // Reusable health text
  speedometer!: Phaser.GameObjects.Text
  warningText!: Phaser.GameObjects.Text

  constructor(scene: Phaser.Scene) {
    this.scene = scene
  }

  create() {
    const s = this.scene
    const style: Phaser.Types.GameObjects.Text.TextStyle = { fontFamily: 'monospace', fontSize: '15px', color: '#e6edf3', stroke: '#000000', strokeThickness: 3 }
    
    this.timeText = s.add.text(12, 10, 'Time: 90', style).setScrollFactor(0).setDepth(100)
    this.scoreText = s.add.text(12, 30, 'Score: 0', style).setScrollFactor(0).setDepth(100)
    this.comboText = s.add.text(12, 50, 'Combo: 0x', style).setScrollFactor(0).setDepth(100)
    this.speedometer = s.add.text(12, 70, 'Speed: 0', style).setScrollFactor(0).setDepth(100)
    
    // Health bar background (moved down to avoid overlap)
    this.healthBarBg = s.add.graphics().setScrollFactor(0).setDepth(100)
    this.healthBarBg.fillStyle(0x000000, 0.7)
    this.healthBarBg.fillRect(10, 135, 204, 26)
    this.healthBarBg.lineStyle(2, 0x666666, 1)
    this.healthBarBg.strokeRect(10, 135, 204, 26)
    
    // Health bar
    this.healthBar = s.add.graphics().setScrollFactor(0).setDepth(101)
    
    // Health text (persistent, not recreated every frame!)
    this.healthText = s.add.text(112, 148, 'Health: 100%', {
      fontFamily: 'monospace',
      fontSize: '13px',
      color: '#ffffff',
      stroke: '#000000',
      strokeThickness: 3
    }).setOrigin(0.5).setScrollFactor(0).setDepth(102)
    
    // Warning text (appears when in danger)
    this.warningText = s.add.text(s.scale.width / 2, 40, '⚠️ DANGER ZONE ⚠️', {
      fontFamily: 'monospace',
      fontSize: '24px',
      color: '#ff0000',
      stroke: '#000000',
      strokeThickness: 4
    }).setOrigin(0.5).setScrollFactor(0).setDepth(100).setVisible(false)
  }

  update(timeLeft: number, score: number, combo: number, health: number, speed: number, inDanger: boolean, windForce?: number) {
    this.timeText.setText(`Time: ${Math.ceil(timeLeft)}`)
    this.scoreText.setText(`Score: ${score}`)
    this.comboText.setText(`Combo: ${combo.toFixed(1)}x`)
    const mph = Math.round(SpeedConverter.toMPH(speed))
    this.speedometer.setText(`Speed: ${mph} mph ${speed < -10 ? '(R)' : ''}`)
    
    // Update health bar
    this.healthBar.clear()
    const healthPercent = Math.max(0, health / 100)
    const barWidth = 200 * healthPercent
    
    // Color based on health
    let color = 0x00ff00
    if (healthPercent < 0.3) color = 0xff0000
    else if (healthPercent < 0.6) color = 0xffaa00
    
    this.healthBar.fillStyle(color, 1)
    this.healthBar.fillRect(12, 137, barWidth, 22)
    
    // Update health text (reuse existing text object - HUGE performance fix!)
    this.healthText.setText(`Health: ${Math.ceil(health)}%`)
    
    // Flash health bar when low
    if (healthPercent < 0.3) {
      this.healthBar.alpha = 0.5 + Math.sin(Date.now() / 100) * 0.5
    } else {
      this.healthBar.alpha = 1
    }
    
    // Show wind force and danger warning
    if (windForce && windForce > 0.1) {
      this.warningText.setText(`🌪️ WIND: ${(windForce * 100).toFixed(0)}%`)
      this.warningText.setVisible(true)
      this.warningText.setAlpha(0.7 + Math.sin(Date.now() / 200) * 0.3)
    } else if (inDanger) {
      this.warningText.setText('⚠️ TORNADO DANGER!')
      this.warningText.setVisible(true)
      this.warningText.setAlpha(0.5 + Math.sin(Date.now() / 100) * 0.5)
    } else {
      this.warningText.setVisible(false)
    }
  }
}

