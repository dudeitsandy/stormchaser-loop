import Phaser from 'phaser'
import { Player } from '../entities/Player'
import { Tornado } from '../entities/Tornado'
import { NPC } from '../entities/NPC'
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
  
  // NPCs
  npcs: NPC[] = []
  npcSpawnTimer = 0
  npcSpawnInterval = 5 // Spawn NPC every 5 seconds
  maxNPCs = 15 // Maximum NPCs on screen
  
  // Tornado lifecycle management
  tornadoCount = 0 // Which tornado (1st, 2nd, 3rd)
  tornadoPhase: 'watch' | 'warning' | 'active' | 'ended' | 'waiting' = 'watch'
  nextTornadoTimer = 0
  nextTornadoDelay = 0
  
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

    // Initialize tornado lifecycle - start with watch phase
    this.tornadoCount = 0
    this.tornadoPhase = 'watch'
    this.tornado = null! // Will be spawned after watch phase

    // Pickups
    this.pickups = this.physics.add.group({ classType: Collectable, runChildUpdate: false })

    // Collisions
    this.physics.add.overlap(this.player, this.pickups, (_p, c) => this.onPickup(c as Collectable))
    
    // Spawn initial NPCs
    this.spawnInitialNPCs()

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
    
    // Show initial tornado WATCH (conditions favorable)
    this.time.delayedCall(1000, () => {
      this.weatherAlert.showTicker(`TORNADO WATCH: Conditions favorable for tornado development in your area. · Storm chasers on standby. · Monitor weather conditions closely.`)
    })
    
    // Spawn first tornado after watch period (5 seconds)
    this.time.delayedCall(5000, () => {
      this.spawnNextTornado()
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
          this.weatherAlert.showTornadoAlert(currentStrength, 'YOUR AREA')
          
          // Visual feedback - screen flash red
          this.cameras.main.flash(200, 255, 0, 0, false, undefined, 0.3)
          this.cameras.main.shake(300, 0.01)
          
          SoundService.playAlert()
        } else {
          this.weatherAlert.showTicker(`Tornado weakening to EF${currentStrength}. Continue to monitor conditions.`)
          
          // Visual feedback - screen flash green (relief)
          this.cameras.main.flash(200, 0, 255, 0, false, undefined, 0.2)
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

    // Check if player died
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

    // Tornado lifecycle management
    if (this.tornadoPhase === 'waiting') {
      // Waiting for next tornado
      this.nextTornadoTimer += dt
      if (this.nextTornadoTimer >= this.nextTornadoDelay) {
        this.spawnNextTornado()
      }
      // Skip tornado logic during waiting phase
      if (!this.tornado) return
    }
    
    // Move tornado - prefers roads and avoids water (only if tornado exists)
    if (!this.tornado) {
      return // No tornado during watch/waiting phase
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
    
    // CRITICAL: Check if tornado hit edge of map - END tornado lifecycle
    const worldBounds = this.physics.world.bounds
    const edgeBuffer = 100 // How close to edge before ending
    
    if (tw.x < worldBounds.x + edgeBuffer || 
        tw.x > worldBounds.x + worldBounds.width - edgeBuffer ||
        tw.y < worldBounds.y + edgeBuffer || 
        tw.y > worldBounds.y + worldBounds.height - edgeBuffer) {
      
      console.log(`🌪️ Tornado ${this.tornadoCount} reached edge of map - ENDING EVENT`)
      this.endTornadoEvent()
      return // Skip rest of tornado logic
    }
    
    // Use realistic tornado movement speeds based on strength with ERRATIC patterns
    const tornadoSpeed = tw.getMoveSpeed()
    if (body.speed < 20) {
      let tx, ty, attempts = 0
      do {
        tx = Phaser.Math.Between(150, 2250)
        ty = Phaser.Math.Between(150, 1200)
        attempts++
      } while (attempts < 10 && this.terrain.getTerrainAt(tx, ty).type === 'water')
      
      // Set the base target
      tw.setMovementTarget(tx, ty)
      
      // Get erratic target based on movement pattern (constrained to world bounds)
      const erraticTarget = tw.getErraticTarget(tw.x, tw.y, 2400, 1350)
      
      // Ensure target is within world bounds
      const constrainedTargetX = Phaser.Math.Clamp(erraticTarget.x, 200, 2200)
      const constrainedTargetY = Phaser.Math.Clamp(erraticTarget.y, 200, 1150)
      
      // Tornado moves at speed based on its strength with erratic behavior
      this.physics.moveTo(tw, constrainedTargetX, constrainedTargetY, tornadoSpeed)
    }
    
    // Tornado path destruction - damage terrain as it moves
    this.destroyTerrainInPath(this.tornado.x, this.tornado.y, this.tornado.getDestructionRadius())

    // Spawns (pickups)
    this.spawnTimer += dt
    if (this.spawnTimer >= ConfigService.instance.cfg.spawnRate) {
      this.spawnTimer = 0
      this.spawnPickup()
    }
    
    // Spawn NPCs periodically
    this.npcSpawnTimer += dt
    if (this.npcSpawnTimer >= this.npcSpawnInterval && this.npcs.length < this.maxNPCs) {
      this.npcSpawnTimer = 0
      this.spawnNPC()
    }
    
    // Update NPCs
    this.updateNPCs(_time, delta)

    // Distance to tornado (only if tornado exists)
    let dist = 0
    let damage = 0
    let inDanger = false
    
    if (this.tornado) {
      dist = Phaser.Math.Distance.Between(this.player.x, this.player.y, tw.x, tw.y)
      
      // Danger zone - take damage if too close (MORE ACCURATE)
      damage = this.tornado.shouldDealDamage(dist)
      inDanger = damage > 0
      if (damage > 0) {
        const actualDamage = damage * dt
        // Only log damage occasionally to avoid spam
        if (Math.random() < 0.01) {
          console.log(`⚠️ TORNADO DAMAGE: ${actualDamage.toFixed(2)} dmg/frame (${damage.toFixed(1)} dmg/sec), Health: ${this.player.health.toFixed(1)}`)
        }
        this.player.takeDamage(actualDamage)
        
        // Visual feedback for danger
        this.cameras.main.shake(50, 0.002 * damage)
        
        // Sound warning when taking damage
        if (Math.random() < 0.05) { // 5% chance per frame
          SoundService.playAlert()
        }
      }
      
      // Wind effects - pull car toward tornado (MORE PROMINENT)
      const windForce = this.tornado.getWindForce(dist, this.player.x, this.player.y)
      if (windForce.magnitude > 0) {
        this.player.applyWindForce(windForce)
        
        // Visual wind effect on camera (ENHANCED)
        if (windForce.magnitude > 0.1) {
          this.cameras.main.shake(50, windForce.magnitude * 0.005) // More shake
          
          // Dust particles in wind
          if (Math.random() < windForce.magnitude && Math.random() < 0.2) {
            this.createWindDustEffect(this.player.x, this.player.y, windForce.magnitude)
          }
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
          // Calculate direction from tornado to object for directional damage
          const angle = Math.atan2(tile.y - tornadoY, tile.x - tornadoX)
          
          // Remove collision
          tile.destroy()
          
          // Update terrain to be destroyed (debris) with directional scatter
          this.createDirectionalDebris(tile.x, tile.y, angle, terrainType)
          
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

  createDirectionalDebris(x: number, y: number, angle: number, terrainType: string) {
    // Create directional debris that flies away from tornado
    const debrisGraphics = this.add.graphics()
    debrisGraphics.fillStyle(terrainType === 'building' ? 0x8b7355 : 0x654321, 1)
    debrisGraphics.fillRect(0, 0, 4, 4)
    debrisGraphics.generateTexture(`directional_debris_${x}_${y}`, 4, 4)
    debrisGraphics.destroy()
    
    // Create debris flying in the tornado's throw direction
    const debris = this.add.particles(x, y, `directional_debris_${x}_${y}`, {
      speed: { min: 100, max: 300 },
      angle: { min: (angle * 180 / Math.PI) - 30, max: (angle * 180 / Math.PI) + 30 },
      scale: { start: 1.5, end: 0 },
      alpha: { start: 1, end: 0 },
      lifespan: 2000,
      quantity: 10,
      gravityY: 100, // Debris falls down
      tint: terrainType === 'building' ? [0x8b7355, 0x654321, 0x4a2c2a] : [0x654321, 0x4a2c2a, 0x8b4513],
      rotate: { min: 0, max: 360 }
    })
    
    // Create damaged terrain tile where object was
    const damageRect = this.add.rectangle(x, y, 50, 50, 0x4a2c2a, 0.5)
    
    // Clean up particles and damaged tile
    this.time.delayedCall(2000, () => {
      debris.destroy()
    })
    
    this.time.delayedCall(10000, () => {
      damageRect.destroy()
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
  
  spawnNextTornado() {
    this.tornadoCount++
    this.tornadoPhase = 'warning'
    
    // Spawn location
    const x = Phaser.Math.Between(400, 2000)
    const y = Phaser.Math.Between(400, 950)
    
    console.log(`🌪️ Spawning Tornado #${this.tornadoCount} at (${x}, ${y})`)
    
    // Create tornado
    this.tornado = new Tornado(this, x, y)
    this.tornado.setDepth(10)
    this.physics.add.existing(this.tornado)
    const tornadoBody = this.tornado.body as Phaser.Physics.Arcade.Body
    tornadoBody.setCircle(50)
    
    // Progressive difficulty based on tornado count
    let initialStrength: number
    let movementPattern: 'straight' | 'zigzag' | 'circular' | 'chaotic'
    
    if (this.tornadoCount === 1) {
      // First tornado: Moderate difficulty
      initialStrength = Phaser.Math.Between(1, 3)
      // Let it use default erratic patterns
    } else if (this.tornadoCount === 2) {
      // Second tornado: More erratic, stronger
      initialStrength = Phaser.Math.Between(2, 4)
      // Force more erratic patterns by reducing pattern change interval
      this.time.addEvent({
        delay: 4000, // Change pattern every 4 seconds (more erratic)
        loop: true,
        callback: () => {
          if (this.tornado) {
            // Force chaotic or circular patterns more often
            const patterns: Array<'zigzag' | 'circular' | 'chaotic'> = ['zigzag', 'circular', 'chaotic']
            const pattern = patterns[Math.floor(Math.random() * patterns.length)]
            console.log(`🌪️ Tornado 2 forcing erratic pattern: ${pattern}`)
          }
        }
      })
    } else {
      // Final tornado: EXTREMELY fast and chaotic
      initialStrength = Phaser.Math.Between(3, 5)
      // Super fast movement by increasing base speed (2x faster)
      this.tornado.setSpeedMultiplier(2.0)
      // Force extremely erratic patterns
      this.time.addEvent({
        delay: 2000, // Change pattern every 2 seconds (VERY erratic)
        loop: true,
        callback: () => {
          if (this.tornado) {
            // Always use chaotic pattern for final tornado
            console.log(`🌪️ FINAL TORNADO - EXTREME CHAOS MODE`)
          }
        }
      })
      console.log(`🌪️ FINAL TORNADO - EXTREME DIFFICULTY - 2X SPEED`)
    }
    
    this.tornado.setStrength(initialStrength)
    
    // Show WARNING alert
    const efRating = Math.floor(this.tornado.getStrength())
    const tornadoLabel = this.tornadoCount === 3 ? 'FINAL TORNADO' : `TORNADO #${this.tornadoCount}`
    
    this.weatherAlert.showTornadoAlert(efRating, 'YOUR AREA')
    
    if (this.tornadoCount === 1) {
      this.weatherAlert.showTicker(`TORNADO WARNING: EF${efRating} tornado has formed! · Storm chasers report funnel cloud on the ground. · Seek shelter immediately!`)
    } else if (this.tornadoCount === 2) {
      this.weatherAlert.showTicker(`SECOND TORNADO WARNING: Another EF${efRating} tornado detected! · Multiple vortices reported. · EXTREMELY DANGEROUS SITUATION!`)
    } else {
      this.weatherAlert.showTicker(`FINAL TORNADO WARNING: Massive EF${efRating} tornado approaching! · LIFE THREATENING SITUATION! · THIS IS YOUR LAST CHANCE FOR PHOTOS!`)
    }
    
    SoundService.playAlert()
    
    // Camera flash for dramatic effect
    this.cameras.main.flash(300, 255, 255, 255, false, undefined, 0.5)
    
    this.tornadoPhase = 'active'
  }
  
  endTornadoEvent() {
    console.log(`🌪️ Ending Tornado #${this.tornadoCount} event`)
    
    // Destroy tornado
    if (this.tornado) {
      this.tornado.destroy()
      this.tornado = null!
    }
    
    // Dismiss weather alerts
    this.weatherAlert.dismiss()
    this.weatherAlert.showTicker(`Tornado has moved out of the area. · Storm system continuing to develop. · Remain alert for additional tornados.`)
    
    this.tornadoPhase = 'ended'
    
    // Determine if we should spawn another tornado
    if (this.tornadoCount < 3 && this.timeLeft > 20) {
      // Spawn another tornado after delay
      this.tornadoPhase = 'waiting'
      this.nextTornadoTimer = 0
      
      if (this.tornadoCount === 1) {
        // 10-15 seconds until second tornado
        this.nextTornadoDelay = Phaser.Math.Between(10, 15)
        console.log(`⏱️ Second tornado will spawn in ${this.nextTornadoDelay} seconds`)
        this.weatherAlert.showTicker(`Tornado dissipated. · Radar shows another storm system developing. · Stay vigilant.`)
      } else if (this.tornadoCount === 2) {
        // Final tornado spawns based on remaining time
        const remainingTime = this.timeLeft
        this.nextTornadoDelay = Math.max(5, Math.min(remainingTime - 15, 10)) // Spawn with 15s left minimum
        console.log(`⏱️ FINAL tornado will spawn in ${this.nextTornadoDelay} seconds`)
        this.weatherAlert.showTicker(`Second tornado dissipated. · SEVERE WARNING: Major supercell developing. · Prepare for intense final tornado!`)
      }
    } else if (this.tornadoCount === 3) {
      // FINAL tornado has ended - END SESSION after 5 seconds
      console.log(`🏁 FINAL tornado dissipated - Session will end in 5 seconds`)
      this.tornadoPhase = 'ended'
      this.weatherAlert.showTicker(`FINAL tornado dissipated. · All tornado activity has ceased. · Storm system moving out. · Session ending...`)
      
      // End session after 5 seconds
      this.time.delayedCall(5000, () => {
        if (!this.gameOver) {
          console.log(`🏁 FINAL TORNADO DELAY COMPLETE - Ending session`)
          this.endSession('time', 'Storm Chasing Complete')
        }
      })
    } else {
      console.log(`🌪️ No more tornados will spawn (count: ${this.tornadoCount}, time: ${this.timeLeft.toFixed(1)}s)`)
      this.tornadoPhase = 'ended'
      this.weatherAlert.showTicker(`All tornado activity has ended. · Storm system moving away. · Session will complete soon.`)
    }
  }
  
  spawnInitialNPCs() {
    // Spawn 10 initial NPCs scattered around the map
    for (let i = 0; i < 10; i++) {
      this.spawnNPC()
    }
  }
  
  spawnNPC() {
    // Randomly choose NPC type
    const types: Array<'civilian_car' | 'stormchaser' | 'civilian'> = ['civilian_car', 'stormchaser', 'civilian']
    const weights = [0.5, 0.2, 0.3] // 50% cars, 20% stormchasers, 30% pedestrians
    
    const random = Math.random()
    let npcType: 'civilian_car' | 'stormchaser' | 'civilian'
    if (random < weights[0]) {
      npcType = 'civilian_car'
    } else if (random < weights[0] + weights[1]) {
      npcType = 'stormchaser'
    } else {
      npcType = 'civilian'
    }
    
    // Spawn at random location (avoid tornado)
    let x, y, attempts = 0
    do {
      x = Phaser.Math.Between(200, 2200)
      y = Phaser.Math.Between(200, 1150)
      attempts++
      
      // Check distance from tornado
      const distToTornado = this.tornado ? Phaser.Math.Distance.Between(x, y, this.tornado.x, this.tornado.y) : 1000
      
      // Check distance from player
      const distToPlayer = Phaser.Math.Distance.Between(x, y, this.player.x, this.player.y)
      
      // Good spawn if far from tornado and player
      if (distToTornado > 400 && distToPlayer > 200) break
    } while (attempts < 20)
    
    const npc = new NPC(this, x, y, npcType)
    this.add.existing(npc)
    this.physics.add.existing(npc)
    npc.setCollideWorldBounds(true)
    
    // Random initial rotation for vehicles
    if (npc.isVehicle) {
      npc.rotation = Phaser.Math.FloatBetween(0, Math.PI * 2)
    }
    
    // Add collision with player (can bump into NPCs)
    this.physics.add.collider(this.player, npc, () => {
      // Minor collision feedback
      SoundService.playCollision(0.3)
      this.player.takeDamage(2) // Small damage from hitting NPCs
    })
    
    // Add collision with terrain
    this.collisionTiles.forEach(tile => {
      this.physics.add.collider(npc, tile)
    })
    
    this.npcs.push(npc)
    console.log(`👤 Spawned ${npcType} at (${x}, ${y})`)
  }
  
  updateNPCs(time: number, delta: number) {
    if (!this.tornado) return
    
    // Update each NPC's behavior
    this.npcs.forEach((npc, index) => {
      if (!npc.active) return
      
      npc.updateBehavior(time, delta, this.tornado.x, this.tornado.y, this.tornado.getStrength())
      
      // Check if NPC is too far from player (despawn to save performance)
      const distToPlayer = Phaser.Math.Distance.Between(npc.x, npc.y, this.player.x, this.player.y)
      if (distToPlayer > 1000) {
        npc.destroy()
        this.npcs.splice(index, 1)
        console.log(`👤 Despawned ${npc.npcType} (too far)`)
      }
      
      // Check if NPC hit by tornado (destroy with effect)
      const distToTornado = Phaser.Math.Distance.Between(npc.x, npc.y, this.tornado.x, this.tornado.y)
      if (distToTornado < this.tornado.getDangerRadius()) {
        // NPC destroyed by tornado!
        this.createDebrisEffect(npc.x, npc.y)
        npc.destroy()
        this.npcs.splice(index, 1)
        console.log(`💀 ${npc.npcType} destroyed by tornado!`)
      }
    })
  }
  
  createWindDustEffect(x: number, y: number, magnitude: number) {
    // Create dust particles for wind visualization
    const dustGraphics = this.add.graphics()
    dustGraphics.fillStyle(0xcccccc, 1)
    dustGraphics.fillCircle(0, 0, 1)
    dustGraphics.generateTexture(`wind_dust_${x}_${y}`, 2, 2)
    dustGraphics.destroy()
    
    const dust = this.add.particles(x, y, `wind_dust_${x}_${y}`, {
      speed: { min: 50, max: 150 },
      angle: { min: 0, max: 360 },
      scale: { start: magnitude, end: 0 },
      alpha: { start: 0.6, end: 0 },
      lifespan: 500,
      quantity: 2,
      tint: [0xcccccc, 0xaaaaaa, 0x888888]
    })
    
    // Clean up
    this.time.delayedCall(500, () => {
      dust.destroy()
    })
  }
}

