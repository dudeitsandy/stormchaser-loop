import Phaser from 'phaser'
import { Photo } from './CameraSystem'

export class PhotoRenderer {
  scene: Phaser.Scene

  constructor(scene: Phaser.Scene) {
    this.scene = scene
  }

  /**
   * Generate a pixelated ground-level view of the tornado
   * Returns a container with the rendered "photo"
   */
  generatePhoto(photo: Photo, width: number = 200, height: number = 150): Phaser.GameObjects.Container {
    const container = this.scene.add.container(0, 0)
    
    // Create render texture for the photo
    const rt = this.scene.add.renderTexture(0, 0, width, height)
    
    // Background (sky gradient - stormy)
    const skyGradient = this.scene.add.graphics()
    skyGradient.fillGradientStyle(0x2a2a3a, 0x2a2a3a, 0x4a4a5a, 0x4a4a5a, 1)
    skyGradient.fillRect(0, 0, width, height * 0.6)
    rt.draw(skyGradient, 0, 0)
    skyGradient.destroy()

    // Ground (green/brown)
    const ground = this.scene.add.graphics()
    ground.fillGradientStyle(0x3a5a2a, 0x3a5a2a, 0x2a4a1a, 0x2a4a1a, 1)
    ground.fillRect(0, height * 0.6, width, height * 0.4)
    rt.draw(ground, 0, 0)
    ground.destroy()

    // Clouds (dark storm clouds)
    for (let i = 0; i < 8; i++) {
      const cloud = this.scene.add.graphics()
      const x = (i / 8) * width + Phaser.Math.Between(-20, 20)
      const y = Phaser.Math.Between(10, height * 0.3)
      const size = Phaser.Math.Between(20, 40)
      cloud.fillStyle(0x1a1a2a, 0.7)
      cloud.fillCircle(0, 0, size)
      rt.draw(cloud, x, y)
      cloud.destroy()
    }

    // Tornado funnel (perspective view from ground)
    const tornadoG = this.scene.add.graphics()
    const tornadoScale = photo.tornadoStrength / 5 // 0-1 scale
    const baseWidth = 40 + (tornadoScale * 60) // Wider for stronger tornados
    const topWidth = 60 + (tornadoScale * 80)
    const tornadoHeight = 80 + (tornadoScale * 40)
    const tornadoX = width / 2
    const tornadoY = height * 0.6

    // Distance affects size (closer = bigger)
    const distanceScale = Math.max(0.5, 1 - (photo.distance / 300))
    
    // Tornado body (funnel shape)
    tornadoG.fillStyle(0x4a4a4a, 0.9)
    tornadoG.beginPath()
    tornadoG.moveTo(tornadoX - baseWidth * distanceScale / 2, tornadoY)
    tornadoG.lineTo(tornadoX + baseWidth * distanceScale / 2, tornadoY)
    tornadoG.lineTo(tornadoX + topWidth * distanceScale / 2, tornadoY - tornadoHeight * distanceScale)
    tornadoG.lineTo(tornadoX - topWidth * distanceScale / 2, tornadoY - tornadoHeight * distanceScale)
    tornadoG.closePath()
    tornadoG.fillPath()

    // Tornado details (darker swirls)
    for (let i = 0; i < 5; i++) {
      tornadoG.fillStyle(0x2a2a2a, 0.5)
      const offset = (i / 5) * tornadoHeight * distanceScale
      const width1 = baseWidth + (i / 5) * (topWidth - baseWidth)
      tornadoG.fillEllipse(
        tornadoX + Phaser.Math.Between(-10, 10),
        tornadoY - offset,
        width1 * distanceScale * 0.8,
        8
      )
    }

    // Debris particles
    for (let i = 0; i < 15; i++) {
      tornadoG.fillStyle(0x6a5a4a, 0.7)
      const debrisX = tornadoX + Phaser.Math.Between(-baseWidth * distanceScale, baseWidth * distanceScale)
      const debrisY = tornadoY - Phaser.Math.Between(0, tornadoHeight * distanceScale)
      tornadoG.fillCircle(debrisX, debrisY, Phaser.Math.Between(1, 3))
    }

    rt.draw(tornadoG, 0, 0)
    tornadoG.destroy()

    // Lightning (random chance)
    if (photo.quality === 'excellent' || photo.quality === 'perfect') {
      const lightning = this.scene.add.graphics()
      lightning.lineStyle(2, 0xffff00, 0.8)
      const lx = Phaser.Math.Between(width * 0.2, width * 0.8)
      lightning.beginPath()
      lightning.moveTo(lx, 0)
      for (let i = 0; i < 5; i++) {
        lightning.lineTo(lx + Phaser.Math.Between(-15, 15), i * 20)
      }
      lightning.strokePath()
      rt.draw(lightning, 0, 0)
      lightning.destroy()
    }

    // Apply pixelation effect
    rt.setScale(0.5) // Make it smaller
    rt.setScale(2)   // Scale it back up (creates pixel effect)

    // Photo border
    const border = this.scene.add.graphics()
    border.lineStyle(3, 0xffffff, 1)
    border.strokeRect(0, 0, width, height)
    border.lineStyle(1, 0x000000, 1)
    border.strokeRect(-2, -2, width + 4, height + 4)
    
    container.add(rt)
    container.add(border)

    // Quality stamp
    const qualityText = this.scene.add.text(width - 5, height - 20, photo.quality.toUpperCase(), {
      fontFamily: 'monospace',
      fontSize: '10px',
      color: this.getQualityColor(photo.quality),
      backgroundColor: '#000000',
      padding: { x: 3, y: 2 }
    }).setOrigin(1, 0)
    container.add(qualityText)

    // Score
    const scoreText = this.scene.add.text(5, height - 20, `${photo.score} pts`, {
      fontFamily: 'monospace',
      fontSize: '10px',
      color: '#ffffff',
      backgroundColor: '#000000',
      padding: { x: 3, y: 2 }
    })
    container.add(scoreText)

    return container
  }

  private getQualityColor(quality: Photo['quality']): string {
    switch (quality) {
      case 'perfect': return '#ff00ff'
      case 'excellent': return '#00ff00'
      case 'good': return '#ffff00'
      case 'decent': return '#ffaa00'
      case 'poor': return '#ff0000'
      default: return '#ffffff'
    }
  }
}

