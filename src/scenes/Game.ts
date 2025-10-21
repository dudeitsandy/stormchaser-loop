import Phaser from 'phaser'
import { Player } from '../entities/Player'
import { Tornado } from '../entities/Tornado'
import { HUD } from '../ui/HUD'
import { Minimap } from '../ui/Minimap'
import { WeatherAlert } from '../ui/WeatherAlert'
import { ConfigService } from '../systems/Config'
import { TerrainGenerator } from '../systems/TerrainGenerator'
import { TerrainRenderer } from '../systems/TerrainRenderer'
import { CameraSystem } from '../systems/CameraSystem'
import { SoundService } from '../systems/SoundService'
import { DebugPanel } from '../systems/DebugPanel'

class Collectable extends Phaser.Physics.Arcade.Sprite {
  type: 'fuel' | 'combo' | 'shot' = 'fuel'
}

export class GameScene extends Phaser.Scene {
  player!: Player
  hud!: HUD
  minimap!: Minimap
  cameraSystem!: CameraSystem
  weatherAlert!: WeatherAlert
  debugPanel!: DebugPanel
  timeLeft = 90
  score = 0
  combo = 1
  comboDecay = 0.5
  paused = false
  spawnTimer = 0
  gameOver = false
  debugText!: Phaser.GameObjects.Text

  pickups!: Phaser.Physics.Arcade.Group
  tornado!: Tornado
  terrain!: TerrainGenerator
  terrainRenderer!: TerrainRenderer
  collisionTiles: Phaser.GameObjects.Rectangle[] = []
  weatherParticles!: Phaser.GameObjects.Particles.ParticleEmitter
  
  private wasAiming = false // Track previous aim state
  private lastTornadoStrength = 1 // Track strength changes

  constructor() { super('Game') }

  create() {
    console.log('🎮 Game scene starting...')
    
    const cfg = ConfigService.instance.cfg
    this.timeLeft = cfg.sessionSeconds
    this.comboDecay = cfg.comboDecayPerSecond
    this.gameOver = false

    // World bounds (optimized size for performance)
    this.physics.world.setBounds(0,0,2400,1350)
    this.cameras.main.setBounds(0,0,2400,1350)

    // Generate procedural terrain with larger tiles for better performance
    this.terrain = new TerrainGenerator(Math.random(), 75) // Larger tiles = fewer objects
    this.terrainRenderer = new TerrainRenderer(this, 75)
    const tiles = this.terrain.generateTerrain(2400, 1350)
    
    console.log(`Rendering ${tiles.length} terrain tiles...`)
    
    // Render terrain tiles with enhanced visuals
    // Performance: Render in batches to avoid frame drops
    let tilesRendered = 0
    tiles.forEach(tile => {
      const tileContainer = this.terrainRenderer.renderTile(tile)
      tilesRendered++
      
      // Create physics collision for solid objects
      if (tile.collidable) {
        const collider = this.add.rectangle(tile.x, tile.y, 50, 50, 0xff0000, 0) // Invisible
        this.physics.add.existing(collider, true) // true = immovable
        this.collisionTiles.push(collider)
      }
    })
    
    console.log(`✓ Rendered ${tilesRendered} tiles, ${this.collisionTiles.length} collision objects`)

    // Add weather effects (rain/wind)
    const weatherGraphics = this.add.graphics()
    weatherGraphics.fillStyle(0xffffff, 1).fillRect(0, 0, 2, 8)
    weatherGraphics.generateTexture('raindrop', 2, 8)
    weatherGraphics.destroy()
    
    // Reduced particle count for better performance
    // Disable weather particles for performance
    // this.weatherParticles = this.add.particles(1200, 0, 'raindrop', {
    //   x: { min: 0, max: 2400 },
    //   y: -10,
    //   speedY: { min: 400, max: 600 },
    //   speedX: { min: -50, max: -20 },
    //   alpha: { start: 0.4, end: 0 },
    //   lifespan: 2000,
    //   frequency: 300,
    //   scale: 0.5,
    //   tint: 0xaaaaaa
    // })
    // this.weatherParticles.setDepth(50)

    // Player vehicle (spawns in center of world)
    this.player = new Player(this, 1200, 675)
    this.add.existing(this.player)
    this.physics.add.existing(this.player)
    this.player.setCollideWorldBounds(true)
    this.player.setTerrain(this.terrain)
    this.player.initInput()
    this.cameras.main.startFollow(this.player, true, 0.1, 0.1)

    // Add collision with buildings/trees (with damage based on speed)
    this.collisionTiles.forEach(tile => {
      this.physics.add.collider(this.player, tile, () => {
        console.log(`🎯 GAME SCENE COLLISION DETECTED`)
        console.log(`🎯 Player Health Before: ${this.player.health}`)
        console.log(`🎯 Player Speed: ${this.player.getCurrentSpeed().toFixed(1)}`)
        console.log(`🎯 Game Over Status: ${this.gameOver}`)
        console.log(`🎯 Player Position: (${this.player.x.toFixed(1)}, ${this.player.y.toFixed(1)})`)
        
        // Prevent multiple collisions in same frame
        if (this.gameOver) {
          console.log(`🎯 COLLISION IGNORED - Game already over`)
          return
        }
        
        // CRITICAL: Check if player is already dead
        if (this.player.health <= 0) {
          console.log(`🎯 COLLISION IGNORED - Player already dead`)
          return
        }
        
        // Store health before collision for debugging
        const healthBefore = this.player.health
        
        // Collision callback - damage based on impact speed
        this.player.handleCollision(this.player.getCurrentSpeed())
        
        console.log(`🎯 Player Health After: ${this.player.health}`)
        console.log(`🎯 Health Change: ${healthBefore} -> ${this.player.health} (${this.player.health - healthBefore})`)
        console.log(`🎯 Game Over Status: ${this.gameOver}`)
        
        // IMMEDIATE health check after collision
        if (this.player.health <= 0 && !this.gameOver) {
          console.log(`🎯 COLLISION KILLED PLAYER - Triggering death sequence`)
          console.log(`🎯 Health is ${this.player.health}, GameOver is ${this.gameOver}`)
          this.gameOver = true
          try {
            this.triggerVehicleExplosion()
            console.log(`🎯 Death sequence triggered successfully`)
          } catch (error) {
            console.error(`🎯 ERROR in death sequence:`, error)
            // Emergency fallback
            this.endSession('crash', 'Vehicle Destroyed')
          }
        } else {
          console.log(`🎯 Player survived collision - Health: ${this.player.health}`)
        }
      })
    })

    // Tornado (animated) - spawns randomly in world
    this.tornado = new Tornado(this, Phaser.Math.Between(400,2000), Phaser.Math.Between(400,1000))
    this.tornado.setDepth(10)
    this.physics.add.existing(this.tornado)
    const tornadoBody = this.tornado.body as Phaser.Physics.Arcade.Body
    tornadoBody.setCircle(50)

    // Pickups
    this.pickups = this.physics.add.group({ classType: Collectable, runChildUpdate: false })

    // Collisions
    this.physics.add.overlap(this.player, this.pickups, (_p, c) => this.onPickup(c as Collectable))

    // Camera System
    this.cameraSystem = new CameraSystem(this)
    this.cameraSystem.createCrosshair()

    // HUD
    this.hud = new HUD(this)
    this.hud.create()

    // Minimap (updated for world size)
    this.minimap = new Minimap(this, 2400, 1350)
    this.minimap.renderTerrain(this.terrain)

    // Weather Alert System
    this.weatherAlert = new WeatherAlert(this)
    
    // Show initial tornado warning
    this.time.delayedCall(1000, () => {
      const efRating = Math.floor(this.tornado.getStrength())
      this.weatherAlert.showTornadoAlert(efRating, 'YOUR AREA')
      this.weatherAlert.showTicker(`TORNADO WARNING: EF${efRating} tornado detected in your area. Seek shelter immediately. · BREAKING: Storm chasers report large tornado on the ground. · Stay tuned for updates.`)
    })

    // Debug Panel (toggle with ` key)
    this.debugPanel = new DebugPanel(this)
    
    // ALWAYS VISIBLE DEBUG INFO (for troubleshooting black screen)
    this.createAlwaysVisibleDebug()

    // Pause/resume
    this.input.keyboard!.on('keydown-ESC', () => this.togglePause())
    
    // CRITICAL: Prevent Shift+Arrow crashes
    this.input.keyboard!.on('keydown-SHIFT', (event: KeyboardEvent) => {
      console.log('🚨 SHIFT KEY IN GAME SCENE - Preventing crash')
      event.preventDefault()
      event.stopPropagation()
    })
    
    // Emergency scene restart if something goes wrong
    this.input.keyboard!.on('keydown-F5', () => {
      console.log('🚨 EMERGENCY RESTART (F5)')
      this.scene.restart()
    })
    
    // TEST: Force collision for debugging (F6 key)
    this.input.keyboard!.on('keydown-F6', () => {
      console.log('🧪 TEST COLLISION - Forcing collision damage')
      console.log(`🧪 Health before: ${this.player.health}`)
      this.player.takeDamage(50) // Force 50 damage
      console.log(`🧪 Health after: ${this.player.health}`)
      
      if (this.player.health <= 0) {
        console.log('🧪 TEST DEATH - Triggering death sequence')
        this.gameOver = true
        this.triggerVehicleExplosion()
      }
    })
    
    console.log('✅ Game scene create() complete')
  }

  update(_time: number, delta: number) {
    // FAILSAFE: Stop update if game is over
    if (this.gameOver) {
      console.warn('⚠️ Update running while gameOver=true - stopping update loop')
      return
    }
    
    // ALWAYS VISIBLE DEBUG INFO (for troubleshooting black screen)
    if (this.debugText) {
      this.debugText.setText([
        `Health: ${this.player?.health?.toFixed(1) ?? 'N/A'}`,
        `GameOver: ${this.gameOver}`,
        `Paused: ${this.paused}`,
        `TimeLeft: ${this.timeLeft.toFixed(1)}`,
        `Player: ${this.player ? 'EXISTS' : 'MISSING'}`,
        `Tornado: ${this.tornado ? 'EXISTS' : 'MISSING'}`,
        `Status: ${this.gameOver ? 'DEATH SEQUENCE' : 'PLAYING'}`
      ].join('\n'))
    }
    
    // Always update debug panel (even when paused/gameover)
    if (this.debugPanel) {
      this.debugPanel.update({
        paused: this.paused,
        gameOver: this.gameOver,
        health: this.player?.health?.toFixed(1) ?? 'N/A',
        timeLeft: this.timeLeft,
        playerExists: !!this.player,
        tornadoExists: !!this.tornado,
        playerPos: this.player ? `(${Math.round(this.player.x)}, ${Math.round(this.player.y)})` : 'N/A',
        tornadoPos: this.tornado ? `(${Math.round(this.tornado.x)}, ${Math.round(this.tornado.y)})` : 'N/A',
        cameraPos: `(${Math.round(this.cameras.main.scrollX)}, ${Math.round(this.cameras.main.scrollY)})`
      })
    }
    
    // FAILSAFE: If gameOver but still in this scene (shouldn't happen but just in case)
    if (this.gameOver) {
      console.warn('⚠️ Update running while gameOver=true. This should not happen!')
      console.warn('⚠️ Press ENTER to force restart')
      
      // Emergency restart - ENTER key
      const enterKey = this.input.keyboard?.addKey('ENTER')
      if (enterKey?.isDown) {
        console.warn('⚠️ EMERGENCY: Force restarting game!')
        this.scene.stop()
        this.scene.start('Game')
      }
      return
    }
    
    if (this.paused) return
    const dt = delta / 1000

    // Update tornado (only if it exists)
    if (this.tornado) {
      this.tornado.preUpdate(_time, delta)
    }
    
    // Check for significant tornado strength changes (only if tornado exists)
    if (this.tornado) {
      const currentStrength = Math.floor(this.tornado.getStrength())
      if (currentStrength !== Math.floor(this.lastTornadoStrength) && Math.abs(currentStrength - this.lastTornadoStrength) >= 1) {
        // Strength changed by at least one EF level
        if (currentStrength > this.lastTornadoStrength) {
          this.weatherAlert.showTicker(`TORNADO INTENSIFYING: Now EF${currentStrength}! EXTREMELY DANGEROUS SITUATION!`)
        } else {
          this.weatherAlert.showTicker(`Tornado weakening to EF${currentStrength}. Continue to monitor conditions.`)
        }
      }
      this.lastTornadoStrength = this.tornado.getStrength()
    }

    // Countdown
    this.timeLeft -= dt
    if (this.timeLeft <= 0 && !this.gameOver) { 
      console.log('⏰ TIME UP TRIGGERED - TimeLeft:', this.timeLeft)
      this.endSession('time', 'Session Complete')
      return // Stop update loop
    }

    // Check if player died - ENHANCED DEBUGGING
    console.log(`🔍 HEALTH CHECK: Health=${this.player.health}, GameOver=${this.gameOver}`)
    
    if (this.player.health <= 0 && !this.gameOver) {
      console.log('💀 DEATH TRIGGERED - Health:', this.player.health)
      console.log('💀 Player exists:', !!this.player)
      console.log('💀 GameOver status:', this.gameOver)
      
      try {
        this.triggerVehicleExplosion()
        console.log('💀 Explosion triggered successfully')
      } catch (error) {
        console.error('💀 ERROR in triggerVehicleExplosion:', error)
        // Fallback: immediate transition
        this.endSession('crash', 'Vehicle Destroyed')
      }
      return // Stop update loop
    }

    // Move tornado - prefers roads and avoids water (only if tornado exists)
    if (!this.tornado) {
      return // No tornado to move
    }
    
    const tw = this.tornado
    const body = tw.body as Phaser.Physics.Arcade.Body
    if (!body) {
      // Tornado body invalid - end session gracefully
      console.warn('⚠️ TORNADO ERROR - Ending session')
      if (!this.gameOver) {
        this.endSession('error', 'Session Ended')
      }
      return
    }
    
    // CRITICAL: Check if tornado is off the map and destroy it
    const worldBounds = this.physics.world.bounds
    if (tw.x < worldBounds.x - 100 || tw.x > worldBounds.x + worldBounds.width + 100 ||
        tw.y < worldBounds.y - 100 || tw.y > worldBounds.y + worldBounds.height + 100) {
      
      console.log('🌪️ TORNADO OFF MAP - Destroying tornado')
      console.log(`🌪️ Tornado position: (${tw.x}, ${tw.y})`)
      console.log(`🌪️ World bounds: (${worldBounds.x}, ${worldBounds.y}) to (${worldBounds.x + worldBounds.width}, ${worldBounds.y + worldBounds.height})`)
      
      // Destroy tornado safely
      try {
        tw.destroy()
        this.tornado = null
        console.log('🌪️ Tornado destroyed successfully')
        
        // Clear any weather alerts
        if (this.weatherAlert) {
          this.weatherAlert.dismiss()
        }
        
        // Show tornado dissipated message
        this.weatherAlert.showTicker('TORNADO DISSIPATED: Storm has moved out of the area. Session continues.')
        
      } catch (error) {
        console.error('🌪️ ERROR destroying tornado:', error)
      }
      
      return // Skip tornado movement for this frame
    }
    
    // Use realistic tornado movement speeds based on strength
    const tornadoSpeed = tw.getMoveSpeed()
    if (body.speed < 20) {
      let tx, ty, attempts = 0
      do {
        tx = Phaser.Math.Between(150, 2250)
        ty = Phaser.Math.Between(150, 1200)
        attempts++
      } while (attempts < 10 && this.terrain.getTerrainAt(tx, ty).type === 'water')
      
      // Tornado moves at speed based on its strength (EF0=15mph to EF5=70mph)
      this.physics.moveTo(tw, tx, ty, tornadoSpeed)
    }
    
    // Tornado path destruction - damage terrain as it moves
    this.destroyTerrainInPath(this.tornado.x, this.tornado.y, this.tornado.getDestructionRadius())

    // Spawns
    this.spawnTimer += dt
    if (this.spawnTimer >= ConfigService.instance.cfg.spawnRate) {
      this.spawnTimer = 0
      this.spawnPickup()
    }

    // Distance to tornado (only if tornado exists)
    let dist = 0
    let damage = 0
    let inDanger = false
    
    if (this.tornado) {
      dist = Phaser.Math.Distance.Between(this.player.x, this.player.y, tw.x, tw.y)
      
      // Danger zone - take damage if too close
      damage = this.tornado.shouldDealDamage(dist)
      inDanger = damage > 0
      if (damage > 0) {
        this.player.takeDamage(damage * dt)
      }
      
      // Wind effects - pull car toward tornado
      const windForce = this.tornado.getWindForce(dist, this.player.x, this.player.y)
      if (windForce.magnitude > 0) {
        this.player.applyWindForce(windForce)
        
        // Visual wind effect on camera
        if (windForce.magnitude > 0.2) {
          this.cameras.main.shake(50, windForce.magnitude * 0.001)
        }
      }
    }

    // Camera system - aim and take photos (only if tornado exists)
    if (this.player.isAiming && this.tornado) {
      this.cameraSystem.startAiming()
      this.cameraSystem.updateAiming(delta, this.player.x, this.player.y, this.player.rotation, this.tornado.x, this.tornado.y)
      this.wasAiming = true
    } else if (this.wasAiming && this.tornado) {
      // Just released camera button - take photo!
      const photo = this.cameraSystem.takePhoto(dist, this.tornado.getStrength())
      this.score += photo.score
      this.addScorePopup(`📸 +${photo.score} (${photo.quality})`, this.player.x, this.player.y - 30)
      this.combo = Math.min(this.combo + 0.5, 5) // Bonus combo for photos
      
      // Play quality feedback sound
      SoundService.playPhotoQuality(photo.quality)
      
      this.wasAiming = false
    } else {
      this.cameraSystem.stopAiming()
      // Decay combo when not taking photos
      this.combo = Math.max(1, this.combo - this.comboDecay * dt)
    }

    // Update minimap (only if tornado exists)
    if (this.tornado) {
      this.minimap.update(this.player.x, this.player.y, this.tornado.x, this.tornado.y)
    } else {
      this.minimap.update(this.player.x, this.player.y, 0, 0) // No tornado to show
    }

    // Update HUD
    const windForce = this.tornado ? this.tornado.getWindForce(dist, this.player.x, this.player.y).magnitude : 0
    this.hud.update(
      this.timeLeft, 
      Math.floor(this.score), 
      this.combo,
      this.player.health,
      this.player.getCurrentSpeed(),
      inDanger,
      windForce
    )
  }

  spawnPickup() {
    // Spawn pickups on roads preferably
    let x, y, attempts = 0
    do {
      x = Phaser.Math.Between(100, 2300)
      y = Phaser.Math.Between(100, 1250)
      attempts++
    } while (attempts < 20 && this.terrain.getTerrainAt(x, y).collidable)
    
    const type = Math.random() < 0.5 ? 'fuel' : 'combo'
    const p = this.pickups.get(x, y) as Collectable
    if (!p) return
    p.type = type as any
    p.setTexture(type === 'fuel' ? 'pickup_fuel' : 'pickup_combo')
    p.setActive(true).setVisible(true)
  }

  onPickup(p: Collectable) {
    if (p.type === 'fuel') {
      this.timeLeft = Math.min(this.timeLeft + 6, ConfigService.instance.cfg.sessionSeconds)
      this.player.heal(10) // Also heal a bit
      SoundService.playPickup('fuel')
    }
    if (p.type === 'combo') {
      this.combo = Math.min(this.combo + 0.8, 5)
      SoundService.playPickup('combo')
    }
    p.disableBody(true, true)
  }

  addScorePopup(text: string, x: number, y: number) {
    const t = this.add.text(x, y - 20, text, { fontFamily: 'monospace', fontSize: '12px', color: '#9be9a8' }).setOrigin(0.5)
    this.tweens.add({ targets: t, y: y - 50, alpha: 0, duration: 500, onComplete: () => t.destroy() })
  }

  endSession(cause: 'time' | 'crash' | 'error', message: string) {
    if (this.gameOver) return // Already ending
    
    console.log(`🎯 ENDING SESSION: ${cause} - ${message}`)
    this.gameOver = true
    
    // IMMEDIATELY disable all player input and physics
    this.player.disableBody(true, false)
    this.player.keys.camera.enabled = false
    
    // Stop all game systems
    this.paused = true
    
    // Clear any existing overlays
    this.children.list.forEach(child => {
      if (child.depth >= 1000) child.destroy()
    })
    
    // Show immediate overlay based on cause
    const colors = {
      time: 0x00aa00,    // Green
      crash: 0xff0000,   // Red  
      error: 0xffaa00    // Orange
    }
    
    const overlay = this.add.rectangle(
      this.scale.width/2,
      this.scale.height/2,
      this.scale.width,
      this.scale.height,
      colors[cause],
      0.9
    ).setScrollFactor(0).setDepth(1000)
    
    const endText = this.add.text(
      this.scale.width/2,
      this.scale.height/2,
      `${message.toUpperCase()}!\n\nLoading Results...`,
      {
        fontFamily: 'monospace',
        fontSize: '36px',
        color: '#ffffff',
        stroke: '#000000',
        strokeThickness: 6,
        align: 'center'
      }
    ).setOrigin(0.5).setScrollFactor(0).setDepth(1001)
    
    // Play appropriate sound
    if (cause === 'time') {
      SoundService.playVictory()
    } else {
      SoundService.playGameOver()
    }
    
    // Transition to Results scene after delay
    this.time.delayedCall(500, () => {
      console.log(`🎯 Transitioning to Results scene`)
      this.scene.stop() // Stop this scene completely
      this.scene.start('Results', {
        score: Math.floor(this.score),
        photos: this.cameraSystem.getPhotos(),
        cause: cause
      })
    })
  }

  createAlwaysVisibleDebug() {
    // Create debug text that's always visible (even during black screen)
    this.debugText = this.add.text(10, 10, '', {
      fontFamily: 'monospace',
      fontSize: '12px',
      color: '#00ff00',
      backgroundColor: '#000000',
      padding: { x: 5, y: 2 }
    }).setScrollFactor(0).setDepth(9999)
  }

  destroyTerrainInPath(tornadoX: number, tornadoY: number, destructionRadius: number) {
    // Check collision tiles within destruction radius
    this.collisionTiles.forEach(tile => {
      const distance = Phaser.Math.Distance.Between(tornadoX, tornadoY, tile.x, tile.y)
      
      if (distance <= destructionRadius && tile.active) {
        // Destroy buildings/trees in tornado path
        const terrainType = this.terrain.getTerrainAt(tile.x, tile.y).type
        
        if (terrainType === 'building' || terrainType === 'tree') {
          // Remove collision
          tile.destroy()
          
          // Update terrain to be destroyed (debris)
          this.terrain.setTerrainAt(tile.x, tile.y, 'debris')
          
          // Visual effect - debris particles
          this.createDebrisEffect(tile.x, tile.y)
          
          console.log(`🌪️ Destroyed ${terrainType} at (${tile.x}, ${tile.y})`)
        }
      }
    })
  }

  createDebrisEffect(x: number, y: number) {
    // Create debris particle effect
    const debrisGraphics = this.add.graphics()
    debrisGraphics.fillStyle(0x8b7355, 1)
    debrisGraphics.fillCircle(0, 0, 1)
    debrisGraphics.generateTexture('debris_particle', 2, 2)
    debrisGraphics.destroy()
    
    const debris = this.add.particles(x, y, 'debris_particle', {
      speed: { min: 50, max: 150 },
      scale: { start: 1, end: 0 },
      alpha: { start: 1, end: 0 },
      lifespan: 1000,
      quantity: 5,
      tint: [0x8b7355, 0x654321, 0x4a2c2a]
    })
    
    // Clean up particles
    this.time.delayedCall(1000, () => {
      debris.destroy()
    })
  }

  triggerVehicleExplosion() {
    console.log('💥 VEHICLE EXPLOSION START')
    console.log('💥 Player position:', this.player.x, this.player.y)
    console.log('💥 Player visible:', this.player.visible)
    
    this.gameOver = true
    
    // IMMEDIATELY disable all player input and physics
    console.log('💥 Disabling player body...')
    this.player.disableBody(true, false)
    this.player.keys.camera.enabled = false
    
    // Stop all game systems
    this.paused = true
    console.log('💥 Game paused, player disabled')
    
    // CRITICAL: Clean up tornado to prevent interference
    if (this.tornado) {
      console.log('💥 Cleaning up tornado...')
      try {
        this.tornado.destroy()
        this.tornado = null
        console.log('💥 Tornado cleaned up successfully')
      } catch (error) {
        console.error('💥 ERROR cleaning up tornado:', error)
      }
    }
    
    // Clear weather alerts
    if (this.weatherAlert) {
      this.weatherAlert.dismiss()
    }
    
    // SIMPLIFIED EXPLOSION - avoid complex particle systems that might crash
    try {
      // Simple explosion flash effect
      const flash = this.add.rectangle(
        this.scale.width/2,
        this.scale.height/2,
        this.scale.width,
        this.scale.height,
        0xff0000, // Red flash instead of white
        0.9
      ).setScrollFactor(0).setDepth(1000)
      
      console.log('💥 Flash created')
      
      // Hide the player vehicle
      this.player.setVisible(false)
      console.log('💥 Player hidden')
      
      // Explosion sound
      SoundService.playGameOver()
      console.log('💥 Sound played')
      
      // Flash animation
      this.tweens.add({
        targets: flash,
        alpha: 0,
        duration: 500,
        ease: 'Power2',
        onComplete: () => {
          console.log('💥 Flash animation complete')
          flash.destroy()
        }
      })
      
      console.log('💥 Flash animation started')
      
    } catch (error) {
      console.error('💥 ERROR in explosion effects:', error)
    }
    
    // Transition to GameOver scene after 3 seconds
    this.time.delayedCall(3000, () => {
      console.log('💥 EXPLOSION COMPLETE - Transitioning to GameOver')
      try {
        this.scene.stop() // Stop this scene completely
        this.scene.start('GameOver', {
          score: Math.floor(this.score),
          cause: 'crash',
          photos: this.cameraSystem.getPhotos()
        })
        console.log('💥 Scene transition successful')
      } catch (error) {
        console.error('💥 ERROR in scene transition:', error)
        // Emergency fallback
        this.scene.restart()
      }
    })
    
    console.log('💥 Explosion sequence initiated')
  }

  togglePause() { this.paused = !this.paused }
  autoPause() { this.paused = true }
  autoResume() { this.paused = false }
}
