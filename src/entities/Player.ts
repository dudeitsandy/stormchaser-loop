import Phaser from 'phaser'
import type { TerrainGenerator } from '../systems/TerrainGenerator'
import { VEHICLE_SPEEDS, SpeedConverter } from '../systems/SpeedConverter'
import { SoundService } from '../systems/SoundService'

export class Player extends Phaser.Physics.Arcade.Sprite {
  // Vehicle physics properties (realistic but controllable)
  baseSpeed = VEHICLE_SPEEDS.MAX_SPEED // 90 mph in game units
  currentSpeed = 0 // Forward/backward velocity
  maxSpeed = VEHICLE_SPEEDS.MAX_SPEED // 90 mph
  maxReverseSpeed = VEHICLE_SPEEDS.REVERSE_SPEED // 25 mph
  
  // Improved acceleration (exponential but responsive)
  accelerationRate = 200 // How fast to reach max speed (increased)
  brakingRate = 400 // Braking power (increased)
  friction = 80 // Natural slowdown when not accelerating
  
  // Improved turning (more responsive)
  baseTurnSpeed = 3.5 // Radians per second (increased for better control)
  minSpeedForTurn = SpeedConverter.toGameUnits(3) // 3 mph minimum
  driftCompensation = 0.85 // Reduces sliding feel
  
  // Terrain-based penalties
  terrainMaxSpeedMultiplier = 1.0 // Current terrain's max speed cap
  terrainAccelMultiplier = 1.0 // Current terrain's acceleration penalty
  
  // Game state
  combo = 0
  isAiming = false
  health = 100
  maxHealth = 100
  terrain?: TerrainGenerator
  
  // Input
  cursors!: Phaser.Types.Input.Keyboard.CursorKeys
  wasd!: { W: Phaser.Input.Keyboard.Key, A: Phaser.Input.Keyboard.Key, S: Phaser.Input.Keyboard.Key, D: Phaser.Input.Keyboard.Key }
  keys!: { camera: Phaser.Input.Keyboard.Key }

  // Visual feedback
  private damageFlashTimer = 0
  private lastCollisionSpeed = 0
  
  // Wind effects
  private windForce = { x: 0, y: 0, magnitude: 0 }
  private windResistance = 0.8 // How much the car resists wind

  constructor(scene: Phaser.Scene, x: number, y: number) {
    super(scene, x, y, 'player_0')
    this.setOrigin(0.5, 0.5)
  }

  setTerrain(terrain: TerrainGenerator) {
    this.terrain = terrain
  }

  initInput() {
    this.cursors = this.scene.input.keyboard!.createCursorKeys()
    this.wasd = {
      W: this.scene.input.keyboard!.addKey('W'),
      A: this.scene.input.keyboard!.addKey('A'),
      S: this.scene.input.keyboard!.addKey('S'),
      D: this.scene.input.keyboard!.addKey('D')
    }
    this.keys = { camera: this.scene.input.keyboard!.addKey('SPACE') }
    
    // CRITICAL: Prevent Shift+Arrow key conflicts that cause black screen
    this.scene.input.keyboard!.on('keydown-SHIFT', (event: KeyboardEvent) => {
      console.log('🚨 SHIFT KEY PRESSED - Preventing conflicts')
      event.preventDefault()
      event.stopPropagation()
    })
    
    // Prevent any modifier key conflicts
    this.scene.input.keyboard!.on('keydown-CONTROL', (event: KeyboardEvent) => {
      console.log('🚨 CTRL KEY PRESSED - Preventing conflicts')
      event.preventDefault()
      event.stopPropagation()
    })
    
    this.scene.input.keyboard!.on('keydown-ALT', (event: KeyboardEvent) => {
      console.log('🚨 ALT KEY PRESSED - Preventing conflicts')
      event.preventDefault()
      event.stopPropagation()
    })
  }

  takeDamage(amount: number) {
    this.health = Math.max(0, this.health - amount)
    this.damageFlashTimer = 0.2
    
    // Camera shake scales with damage
    this.scene.cameras.main.shake(100, Math.min(0.01, amount / 200))
    
    return this.health <= 0
  }

  heal(amount: number) {
    this.health = Math.min(this.maxHealth, this.health + amount)
  }

  applyWindForce(windForce: { x: number, y: number, magnitude: number }) {
    this.windForce = windForce
    
    // Apply wind force to vehicle physics
    const body = this.body as Phaser.Physics.Arcade.Body
    if (!body) return
    
    // Wind affects velocity (pulls toward tornado) - STRONGER EFFECT
    const currentVelX = body.velocity.x
    const currentVelY = body.velocity.y
    
    // Apply wind force with reduced resistance (wind is MORE powerful)
    const effectiveResistance = this.windResistance * 0.5 // 50% less resistance = stronger wind
    const windStrength = 150 // Base wind strength multiplier
    const windEffectX = windForce.x * windStrength * (1 - effectiveResistance)
    const windEffectY = windForce.y * windStrength * (1 - effectiveResistance)
    
    // Add wind to current velocity (MORE NOTICEABLE)
    body.setVelocity(
      currentVelX + windEffectX,
      currentVelY + windEffectY
    )
    
    // Visual feedback - car tilts in wind (MORE DRAMATIC)
    if (windForce.magnitude > 0.05) {
      const windAngle = Math.atan2(windForce.y, windForce.x)
      const tiltAmount = windForce.magnitude * 0.3 // Increased tilt
      
      // Blend current rotation with wind direction
      const targetRotation = this.rotation + Math.sin(windAngle - this.rotation) * tiltAmount
      this.setRotation(Phaser.Math.Linear(this.rotation, targetRotation, 0.1))
    }
  }

  handleCollision(speed: number) {
    console.log(`💥 COLLISION START: Speed=${speed.toFixed(1)}, Health=${this.health}`)
    
    // Collision damage based on speed (realistic!)
    const mph = SpeedConverter.toMPH(Math.abs(speed))
    console.log(`💥 COLLISION MPH: ${mph.toFixed(1)}`)
    
    if (mph > 40) {
      // High speed collision = major damage
      const damage = (mph - 40) * 0.5
      console.log(`💥 HIGH SPEED COLLISION: Damage=${damage.toFixed(1)}`)
      this.takeDamage(damage)
      this.currentSpeed *= 0.3 // Lose most speed
      SoundService.playCollision(3) // Loud crash!
    } else if (mph > 20) {
      // Medium speed collision = minor damage
      const damage = (mph - 20) * 0.2
      console.log(`💥 MEDIUM SPEED COLLISION: Damage=${damage.toFixed(1)}`)
      this.takeDamage(damage)
      this.currentSpeed *= 0.5 // Lose half speed
      SoundService.playCollision(1.5) // Medium crash
    } else {
      // Low speed = just bounce
      console.log(`💥 LOW SPEED COLLISION: Just bounce`)
      this.currentSpeed *= 0.7
      SoundService.playCollision(0.5) // Light bump
    }
    
    this.lastCollisionSpeed = Math.abs(speed)
    console.log(`💥 COLLISION END: Health=${this.health}, Speed=${this.currentSpeed.toFixed(1)}`)
    
    // CRITICAL: Check if this collision killed the player
    if (this.health <= 0) {
      console.log(`💀 COLLISION KILLED PLAYER! Health=${this.health}`)
    }
  }

  preUpdate(time: number, delta: number) {
    super.preUpdate(time, delta)
    
    // CRITICAL: If body disabled (game over), don't process anything!
    if (!this.body || !(this.body as Phaser.Physics.Arcade.Body).enable) {
      console.log('🚨 PLAYER BODY DISABLED - Stopping input processing')
      // Also disable camera input to prevent space bar sounds
      if (this.keys?.camera) {
        this.keys.camera.enabled = false
      }
      return
    }
    
    const dt = delta / 1000
    const body = this.body as Phaser.Physics.Arcade.Body

    // Damage flash effect
    if (this.damageFlashTimer > 0) {
      this.damageFlashTimer -= dt
      this.setTint(0xff0000)
    } else {
      this.clearTint()
    }

    // Check terrain under vehicle
    if (this.terrain) {
      const tile = this.terrain.getTerrainAt(this.x, this.y)
      this.terrainMaxSpeedMultiplier = tile.speedModifier
      
      // Terrain affects acceleration too
      if (tile.type === 'mud' || tile.type === 'water') {
        this.terrainAccelMultiplier = 0.3 // Very slow acceleration
      } else if (tile.type === 'forest' || tile.type === 'field') {
        this.terrainAccelMultiplier = 0.6 // Slower acceleration
      } else if (tile.type === 'highway' || tile.type === 'road') {
        this.terrainAccelMultiplier = 1.2 // Better acceleration on pavement
      } else {
        this.terrainAccelMultiplier = 1.0 // Normal
      }
    }

    // Calculate terrain-limited max speed
    const terrainMaxSpeed = this.maxSpeed * this.terrainMaxSpeedMultiplier
    const terrainMaxReverse = this.maxReverseSpeed * Math.max(0.5, this.terrainMaxSpeedMultiplier)

    // Input
    const upPressed = this.cursors.up?.isDown || this.wasd.W.isDown
    const downPressed = this.cursors.down?.isDown || this.wasd.S.isDown
    const leftPressed = this.cursors.left?.isDown || this.wasd.A.isDown
    const rightPressed = this.cursors.right?.isDown || this.wasd.D.isDown

    // Acceleration system (realistic exponential curve)
    if (upPressed && !downPressed) {
      // Forward acceleration - gets harder as you approach max speed
      const speedRatio = Math.abs(this.currentSpeed) / terrainMaxSpeed
      const accelMultiplier = Math.max(0.1, 1 - Math.pow(speedRatio, 2)) // Exponential
      const effectiveAccel = this.accelerationRate * accelMultiplier * this.terrainAccelMultiplier
      
      this.currentSpeed += effectiveAccel * dt
      this.currentSpeed = Math.min(this.currentSpeed, terrainMaxSpeed)
    }
    // Braking or reverse
    else if (downPressed && !upPressed) {
      if (this.currentSpeed > 10) {
        // Braking (strong)
        this.currentSpeed -= this.brakingRate * dt
      } else {
        // Reverse acceleration (slower than forward)
        const speedRatio = Math.abs(this.currentSpeed) / terrainMaxReverse
        const accelMultiplier = Math.max(0.1, 1 - Math.pow(speedRatio, 2))
        const effectiveAccel = (this.accelerationRate * 0.6) * accelMultiplier * this.terrainAccelMultiplier
        
        this.currentSpeed -= effectiveAccel * dt
        this.currentSpeed = Math.max(this.currentSpeed, -terrainMaxReverse)
      }
    }
    // Natural friction when coasting
    else {
      if (this.currentSpeed > 0) {
        this.currentSpeed -= this.friction * dt
        this.currentSpeed = Math.max(0, this.currentSpeed)
      } else if (this.currentSpeed < 0) {
        this.currentSpeed += this.friction * dt
        this.currentSpeed = Math.min(0, this.currentSpeed)
      }
    }

    // Smooth turning (speed-dependent)
    const absSpeed = Math.abs(this.currentSpeed)
    if (absSpeed > this.minSpeedForTurn) {
      // Turn rate decreases with speed (harder to turn when fast)
      // Formula: turnRate = baseTurnSpeed * (1 / (1 + speed/200))
      const speedFactor = 1 / (1 + absSpeed / 200)
      const turnRate = this.baseTurnSpeed * speedFactor
      
      if (leftPressed && !rightPressed) {
        // Turn left (counterclockwise)
        this.rotation -= turnRate * dt * (this.currentSpeed > 0 ? 1 : -1)
      }
      if (rightPressed && !leftPressed) {
        // Turn right (clockwise)
        this.rotation += turnRate * dt * (this.currentSpeed > 0 ? 1 : -1)
      }
    }

    // Apply velocity in the direction the car is facing
    // rotation 0 = facing right, so we offset by -90 degrees
    const angle = this.rotation - Math.PI / 2
    let velocityX = Math.cos(angle) * this.currentSpeed
    let velocityY = Math.sin(angle) * this.currentSpeed
    
    // Drift compensation - reduces sliding, makes keyboard control tighter
    const currentVelX = body.velocity.x
    const currentVelY = body.velocity.y
    velocityX = currentVelX * (1 - this.driftCompensation) + velocityX * this.driftCompensation
    velocityY = currentVelY * (1 - this.driftCompensation) + velocityY * this.driftCompensation
    
    body.setVelocity(velocityX, velocityY)

    // Camera aiming state (only if enabled)
    this.isAiming = this.keys.camera.isDown && this.keys.camera.enabled

    // Visual feedback for terrain (ENHANCED)
    if (this.damageFlashTimer <= 0) {
      if (this.terrainMaxSpeedMultiplier >= 1.5) {
        this.setTint(0x00ff00) // Bright green on highways (50% boost!)
      } else if (this.terrainMaxSpeedMultiplier >= 1.2) {
        this.setTint(0x88ff88) // Light green on roads (20% boost)
      } else if (this.terrainMaxSpeedMultiplier <= 0.3) {
        this.setTint(0xff0000) // Red in mud/water (70%+ penalty!)
      } else if (this.terrainMaxSpeedMultiplier <= 0.5) {
        this.setTint(0xff8844) // Orange in fields (50% penalty)
      } else if (this.terrainMaxSpeedMultiplier <= 0.7) {
        this.setTint(0xffcc00) // Yellow in grass (30% penalty)
      } else {
        this.clearTint() // Normal terrain
      }
    }
  }

  getCurrentSpeed(): number {
    return this.currentSpeed
  }

  getSpeedPercent(): number {
    return Math.abs(this.currentSpeed) / this.maxSpeed
  }
}
