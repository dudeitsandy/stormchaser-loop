import Phaser from 'phaser'

export type NPCType = 'civilian_car' | 'stormchaser' | 'civilian'

export class NPC extends Phaser.Physics.Arcade.Sprite {
  npcType: NPCType
  speed = 50
  panicMode = false
  isVehicle = false
  
  constructor(scene: Phaser.Scene, x: number, y: number, npcType: NPCType) {
    super(scene, x, y, npcType)
    this.npcType = npcType
    this.isVehicle = npcType !== 'civilian'
    
    // Different speeds for different types
    switch (npcType) {
      case 'civilian_car':
        this.speed = 100 // Normal car speed
        break
      case 'stormchaser':
        this.speed = 140 // Fast like player
        break
      case 'civilian':
        this.speed = 30 // Pedestrian walking speed
        break
    }
  }
  
  updateBehavior(time: number, delta: number, tornadoX: number, tornadoY: number, tornadoStrength: number) {
    const dt = delta / 1000
    
    // Calculate distance to tornado
    const distanceToTornado = Phaser.Math.Distance.Between(this.x, this.y, tornadoX, tornadoY)
    const dangerThreshold = 300 + tornadoStrength * 100 // Flee when tornado is close
    
    // Panic if tornado is too close
    if (distanceToTornado < dangerThreshold) {
      this.panicMode = true
      
      // Flee away from tornado
      const angleAwayFromTornado = Math.atan2(this.y - tornadoY, this.x - tornadoX)
      const fleeSpeed = this.speed * 1.5 // Move faster when fleeing
      
      const velocityX = Math.cos(angleAwayFromTornado) * fleeSpeed
      const velocityY = Math.sin(angleAwayFromTornado) * fleeSpeed
      
      this.body!.setVelocity(velocityX, velocityY)
      
      // Rotate to face movement direction
      if (this.isVehicle) {
        this.rotation = angleAwayFromTornado + Math.PI / 2
      }
    } else {
      this.panicMode = false
      
      // Normal behavior - move randomly or follow road
      if (this.isVehicle) {
        // Vehicles move along their current direction
        const body = this.body as Phaser.Physics.Arcade.Body
        if (body.speed < 10) {
          // Pick a random direction to move
          const randomAngle = Phaser.Math.FloatBetween(0, Math.PI * 2)
          const velocityX = Math.cos(randomAngle) * this.speed
          const velocityY = Math.sin(randomAngle) * this.speed
          body.setVelocity(velocityX, velocityY)
          this.rotation = randomAngle + Math.PI / 2
        }
      } else {
        // Pedestrians move slowly and randomly
        if (Math.random() < 0.02) { // 2% chance per update to change direction
          const randomAngle = Phaser.Math.FloatBetween(0, Math.PI * 2)
          const velocityX = Math.cos(randomAngle) * this.speed
          const velocityY = Math.sin(randomAngle) * this.speed
          this.body!.setVelocity(velocityX, velocityY)
        }
      }
    }
    
    // Visual feedback when panicking
    if (this.panicMode && this.npcType !== 'civilian') {
      this.setTint(0xff0000) // Red tint when fleeing
    } else {
      this.clearTint()
    }
  }
  
  canBeHit(): boolean {
    // NPCs can be hit by player
    return true
  }
}

