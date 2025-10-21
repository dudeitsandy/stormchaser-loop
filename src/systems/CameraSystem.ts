import Phaser from 'phaser'
import { SoundService } from './SoundService'

export interface Photo {
  timestamp: number
  distance: number
  tornadoStrength: number
  score: number
  quality: 'poor' | 'decent' | 'good' | 'excellent' | 'perfect'
}

export class CameraSystem {
  scene: Phaser.Scene
  crosshair: Phaser.GameObjects.Graphics | null = null
  aimLine: Phaser.GameObjects.Graphics | null = null
  photos: Photo[] = []
  isAiming = false
  aimDuration = 0
  maxAimTime = 2.0 // 2 seconds for perfect shot
  
  // Camera shake for photo snap
  private flashRect: Phaser.GameObjects.Rectangle | null = null

  constructor(scene: Phaser.Scene) {
    this.scene = scene
  }

  createCrosshair() {
    // Crosshair that appears when aiming
    this.crosshair = this.scene.add.graphics()
    this.crosshair.setScrollFactor(0)
    this.crosshair.setDepth(150)
    this.crosshair.setVisible(false)

    // Aim line (from vehicle to target)
    this.aimLine = this.scene.add.graphics()
    this.aimLine.setDepth(5)
    this.aimLine.setVisible(false)

    // Flash overlay for photo snap
    this.flashRect = this.scene.add.rectangle(
      this.scene.scale.width / 2,
      this.scene.scale.height / 2,
      this.scene.scale.width,
      this.scene.scale.height,
      0xffffff,
      0
    )
    this.flashRect.setScrollFactor(0)
    this.flashRect.setDepth(200)
  }

  startAiming() {
    this.isAiming = true
    this.aimDuration = 0
    if (this.crosshair) this.crosshair.setVisible(true)
    if (this.aimLine) this.aimLine.setVisible(true)
  }

  updateAiming(delta: number, playerX: number, playerY: number, playerAngle: number, tornadoX: number, tornadoY: number) {
    if (!this.isAiming) return

    const dt = delta / 1000
    this.aimDuration += dt

    // Color changes as you aim longer (green = ready)
    const progress = Math.min(this.aimDuration / this.maxAimTime, 1)
    let color = 0xff0000 // Red
    if (progress > 0.8) color = 0x00ff00 // Green (perfect)
    else if (progress > 0.5) color = 0xffff00 // Yellow (good)
    else if (progress > 0.25) color = 0xffaa00 // Orange (decent)

    // Update crosshair appearance based on aim duration
    if (this.crosshair) {
      this.crosshair.clear()
      const centerX = this.scene.scale.width / 2
      const centerY = this.scene.scale.height / 2

      // Crosshair size shrinks as you aim (more precise)
      const size = 30 - (progress * 20)
      const thickness = 2 + (progress * 2)

      this.crosshair.lineStyle(thickness, color, 1)
      // Horizontal line
      this.crosshair.lineBetween(centerX - size, centerY, centerX - 5, centerY)
      this.crosshair.lineBetween(centerX + 5, centerY, centerX + size, centerY)
      // Vertical line
      this.crosshair.lineBetween(centerX, centerY - size, centerX, centerY - 5)
      this.crosshair.lineBetween(centerX, centerY + 5, centerX, centerY + size)
      
      // Center dot
      this.crosshair.fillStyle(color, 1)
      this.crosshair.fillCircle(centerX, centerY, 2)

      // Aim progress circle
      this.crosshair.lineStyle(3, color, 0.5)
      this.crosshair.beginPath()
      this.crosshair.arc(centerX, centerY, 40, -Math.PI / 2, -Math.PI / 2 + (progress * Math.PI * 2))
      this.crosshair.strokePath()
    }

    // Draw camera cone from vehicle (shows field of view)
    if (this.aimLine) {
      this.aimLine.clear()
      
      // Camera cone angle (45 degrees total, 22.5 each side)
      const coneAngle = Math.PI / 8 // 22.5 degrees
      const coneLength = 250
      
      // Vehicle facing angle (adjust by -90 degrees as Phaser sprites default to right)
      const vehicleAngle = playerAngle - Math.PI / 2
      
      // Calculate cone edges
      const leftAngle = vehicleAngle - coneAngle
      const rightAngle = vehicleAngle + coneAngle
      
      const leftX = playerX + Math.cos(leftAngle) * coneLength
      const leftY = playerY + Math.sin(leftAngle) * coneLength
      const rightX = playerX + Math.cos(rightAngle) * coneLength
      const rightY = playerY + Math.sin(rightAngle) * coneLength
      
      // Draw cone
      this.aimLine.fillStyle(color, 0.15)
      this.aimLine.beginPath()
      this.aimLine.moveTo(playerX, playerY)
      this.aimLine.lineTo(leftX, leftY)
      this.aimLine.lineTo(rightX, rightY)
      this.aimLine.closePath()
      this.aimLine.fillPath()
      
      // Draw cone outline
      this.aimLine.lineStyle(2, color, 0.5)
      this.aimLine.beginPath()
      this.aimLine.moveTo(playerX, playerY)
      this.aimLine.lineTo(leftX, leftY)
      this.aimLine.strokePath()
      
      this.aimLine.beginPath()
      this.aimLine.moveTo(playerX, playerY)
      this.aimLine.lineTo(rightX, rightY)
      this.aimLine.strokePath()
    }
  }

  takePhoto(distance: number, tornadoStrength: number): Photo {
    this.isAiming = false
    if (this.crosshair) this.crosshair.setVisible(false)
    if (this.aimLine) this.aimLine.setVisible(false)

    // IMPROVED SCORING SYSTEM - More forgiving for good positioning
    const aimScore = Math.min(this.aimDuration / this.maxAimTime, 1)
    
    // Optimal distance: 100-250 units (wider range for better gameplay)
    let distanceScore = 1.0
    if (distance < 100) {
      distanceScore = Math.max(0.3, distance / 100) // Closer shots still get some points
    } else if (distance > 250) {
      distanceScore = Math.max(0.1, 1 - (distance - 250) / 200) // Far shots get fewer points
    }
    
    // Base score from tornado strength
    let baseScore = tornadoStrength * 50
    
    // Aim bonus (holding aim longer gives better score)
    const aimBonus = aimScore * 30
    
    // Distance bonus (optimal distance gets max bonus)
    const distanceBonus = distanceScore * 20
    
    // Total score calculation
    const totalScore = baseScore + aimBonus + distanceBonus

    // IMPROVED QUALITY SYSTEM - More forgiving thresholds
    let quality: Photo['quality'] = 'poor'
    
    // If within reasonable distance and aimed for any time, at least decent
    if (distanceScore > 0.2 && aimScore > 0.1) {
      if (aimScore > 0.8 && distanceScore > 0.7) {
        quality = 'perfect'
      } else if (aimScore > 0.6 && distanceScore > 0.5) {
        quality = 'excellent'
      } else if (aimScore > 0.4 && distanceScore > 0.3) {
        quality = 'good'
      } else {
        quality = 'decent'
      }
    }

    const photo: Photo = {
      timestamp: Date.now(),
      distance,
      tornadoStrength,
      score: Math.floor(totalScore),
      quality
    }

    this.photos.push(photo)
    this.aimDuration = 0

    // Flash effect
    this.flashPhoto()

    return photo
  }

  private flashPhoto() {
    if (!this.flashRect) return

    this.flashRect.setAlpha(0.8)
    this.scene.tweens.add({
      targets: this.flashRect,
      alpha: 0,
      duration: 200,
      ease: 'Power2'
    })

    // Camera shake
    this.scene.cameras.main.shake(150, 0.005)

    // Camera snap sound!
    SoundService.playCamera()
  }

  stopAiming() {
    this.isAiming = false
    this.aimDuration = 0
    if (this.crosshair) this.crosshair.setVisible(false)
    if (this.aimLine) this.aimLine.setVisible(false)
  }

  getPhotos(): Photo[] {
    return this.photos
  }

  getBestPhoto(): Photo | null {
    if (this.photos.length === 0) return null
    return this.photos.reduce((best, current) => 
      current.score > best.score ? current : best
    )
  }
}

