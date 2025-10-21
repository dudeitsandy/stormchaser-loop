import Phaser from 'phaser'
import { TORNADO_SPEEDS } from '../systems/SpeedConverter'

export class Tornado extends Phaser.GameObjects.Container {
  strength = 1 // 0-5 scale
  targetStrength = 1
  spinning = true
  moveSpeed = TORNADO_SPEEDS.EF1 // Current movement speed
  
  private core: Phaser.GameObjects.Sprite
  private particles: Phaser.GameObjects.Particles.ParticleEmitter
  private clouds: Phaser.GameObjects.Graphics[]
  private vortex: Phaser.GameObjects.Graphics
  private strengthChangeTimer = 0

  constructor(scene: Phaser.Scene, x: number, y: number) {
    super(scene, x, y)

    // Create vortex graphics (spinning funnel)
    this.vortex = scene.add.graphics()
    this.add(this.vortex)

    // Create cloud base
    this.clouds = []
    for (let i = 0; i < 3; i++) {
      const cloud = scene.add.graphics()
      this.clouds.push(cloud)
      this.add(cloud)
    }

    // Core sprite (the bright center)
    this.core = scene.add.sprite(0, 0, 'tornado')
    this.core.setAlpha(0.8)
    this.add(this.core)

    // Create particle system for debris
    const particleTexture = scene.add.graphics()
    particleTexture.fillStyle(0xffffff, 1)
    particleTexture.fillCircle(2, 2, 2)
    particleTexture.generateTexture('debris', 4, 4)
    particleTexture.destroy()

    this.particles = scene.add.particles(0, 0, 'debris', {
      speed: { min: 20, max: 60 },
      scale: { start: 0.8, end: 0.2 },
      alpha: { start: 0.8, end: 0 },
      lifespan: 2000,
      frequency: 50,
      blendMode: 'ADD',
      emitting: false,
      rotate: { min: 0, max: 360 },
      tint: [0xaaaaaa, 0x888888, 0x666666]
    })
    this.add(this.particles)

    this.updateVisuals()
    scene.add.existing(this)
  }

  preUpdate(time: number, delta: number) {
    const dt = delta / 1000

    // Gradually change strength toward target
    if (Math.abs(this.strength - this.targetStrength) > 0.01) {
      const changeRate = 0.3 * dt
      if (this.strength < this.targetStrength) {
        this.strength = Math.min(this.strength + changeRate, this.targetStrength)
      } else {
        this.strength = Math.max(this.strength - changeRate, this.targetStrength)
      }
      this.updateVisuals()
    }

    // Randomly change target strength
    this.strengthChangeTimer += dt
    if (this.strengthChangeTimer > 5) {
      this.strengthChangeTimer = 0
      this.targetStrength = Phaser.Math.FloatBetween(0.5, 5)
    }

    // Rotate core and particles
    if (this.spinning) {
      this.core.rotation += (2 + this.strength * 0.5) * dt
      this.vortex.rotation += (1.5 + this.strength * 0.3) * dt
    }
  }

  private updateVisuals() {
    // Scale everything based on strength
    const scale = 0.5 + this.strength * 0.3
    const radius = 30 + this.strength * 20
    
    // Update move speed based on strength (EF0=15mph to EF5=70mph)
    const strengthLevel = Math.floor(this.strength)
    switch (strengthLevel) {
      case 0: this.moveSpeed = TORNADO_SPEEDS.EF0; break
      case 1: this.moveSpeed = TORNADO_SPEEDS.EF1; break
      case 2: this.moveSpeed = TORNADO_SPEEDS.EF2; break
      case 3: this.moveSpeed = TORNADO_SPEEDS.EF3; break
      case 4: this.moveSpeed = TORNADO_SPEEDS.EF4; break
      case 5: this.moveSpeed = TORNADO_SPEEDS.EF5; break
      default: this.moveSpeed = TORNADO_SPEEDS.EF1;
    }
    
    this.core.setScale(scale)

    // Draw vortex funnel
    this.vortex.clear()
    this.vortex.lineStyle(4, 0xffffff, 0.3)
    
    // Draw spiral
    const segments = 20
    for (let i = 0; i < segments; i++) {
      const angle = (i / segments) * Math.PI * 4
      const r = (i / segments) * radius
      const x = Math.cos(angle) * r
      const y = Math.sin(angle) * r
      if (i === 0) this.vortex.beginPath()
      this.vortex.lineTo(x, y)
    }
    this.vortex.strokePath()

    // Draw clouds at top
    this.clouds.forEach((cloud, i) => {
      cloud.clear()
      const cloudRadius = radius * 1.5 + i * 10
      const alpha = 0.3 - i * 0.1
      cloud.fillStyle(0x888888, alpha)
      cloud.fillCircle(Math.cos(i * 2) * 20, -radius - 20 + i * 5, cloudRadius / 2)
    })

    // Update particle emission based on strength
    this.particles.setFrequency(Math.max(10, 100 - this.strength * 15))
    this.particles.setEmitZone({
      type: 'random',
      source: new Phaser.Geom.Circle(0, 0, radius)
    })

    // Start/stop particles based on strength
    if (this.strength > 0.3) {
      this.particles.start()
    } else {
      this.particles.stop()
    }
  }

  setStrength(value: number) {
    this.targetStrength = Phaser.Math.Clamp(value, 0, 5)
  }

  getStrength(): number {
    return this.strength
  }

  getRadius(): number {
    return 30 + this.strength * 20
  }

  getDangerRadius(): number {
    // Inner danger zone - too close = damage
    return this.getRadius() * 0.4
  }

  getFilmingRadius(): number {
    // Optimal filming distance
    return this.getRadius() + 80
  }

  shouldDealDamage(distance: number): number {
    const dangerRadius = this.getDangerRadius()
    if (distance < dangerRadius) {
      // Damage scales with how close you are and tornado strength
      const closeness = 1 - (distance / dangerRadius)
      return this.strength * closeness * 5 // Up to 25 damage per second at EF5 center
    }
    return 0
  }

  getWindRadius(): number {
    // Wind effect radius (much larger than danger radius)
    return this.getRadius() * 3
  }

  getDestructionRadius(): number {
    // Path destruction radius
    return this.getRadius() * 0.8
  }

  getWindForce(distance: number, playerX: number, playerY: number): { x: number, y: number, magnitude: number } {
    const windRadius = this.getWindRadius()
    if (distance > windRadius) return { x: 0, y: 0, magnitude: 0 }
    
    // Wind force decreases with distance
    const force = (1 - (distance / windRadius)) * this.strength * 0.3
    
    // Calculate direction from tornado to player (pulling toward tornado)
    const angle = Math.atan2(this.y - playerY, this.x - playerX)
    
    return {
      x: Math.cos(angle) * force,
      y: Math.sin(angle) * force,
      magnitude: force
    }
  }

  getMoveSpeed(): number {
    return this.moveSpeed
  }
}

