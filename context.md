// Stormchaser Loop — Phaser 3 + TypeScript + Vite
// ===============================================
// Paste these files into a new repo. Run: npm i && npm run dev
// You can use Cursor to generate missing assets and iterate.

// -------------------------
// package.json
// -------------------------
{
  "name": "stormchaser-loop",
  "version": "0.1.0",
  "private": true,
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "phaser": "^3.70.0"
  },
  "devDependencies": {
    "typescript": "^5.5.4",
    "vite": "^5.4.0"
  }
}

// -------------------------
// tsconfig.json
// -------------------------
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "strict": true,
    "jsx": "preserve",
    "sourceMap": true,
    "resolveJsonModule": true,
    "esModuleInterop": true,
    "lib": ["ES2020", "DOM"]
  },
  "include": ["src"]
}

// -------------------------
// vite.config.ts
// -------------------------
import { defineConfig } from 'vite'

export default defineConfig({
  server: { port: 5173 },
  build: { target: 'es2020' }
})

// -------------------------
// index.html
// -------------------------
<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1, maximum-scale=1" />
    <title>Stormchaser Loop</title>
    <style>
      html, body { margin: 0; padding: 0; background:#0a0e12; color:#e6edf3; font-family: system-ui, sans-serif; }
      #game { width: 100vw; height: 100vh; display: block; }
      .overlay { position: fixed; left: 0; right: 0; bottom: 0; padding: 6px 10px; font-size: 12px; opacity: .6; }
    </style>
  </head>
  <body>
    <div id="game"></div>
    <div class="overlay">Stormchaser Loop · dev build</div>
    <script type="module" src="/src/main.ts"></script>
  </body>
</html>

// -------------------------
// src/main.ts
// -------------------------
import Phaser from 'phaser'
import { Boot } from './scenes/Boot'
import { Title } from './scenes/Title'
import { GameScene } from './scenes/Game'
import { Results } from './scenes/Results'

const DPR = Math.min(window.devicePixelRatio || 1, 2)

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  parent: 'game',
  backgroundColor: '#0a0e12',
  scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH, width: 800, height: 450 },
  physics: { default: 'arcade', arcade: { gravity: { y: 0 }, debug: false } },
  render: { pixelArt: true, roundPixels: true, antialias: false },
  scene: [Boot, Title, GameScene, Results]
}

// Respect high-DPI by scaling canvas; Phaser handles internally via resolution
;(config as any).resolution = DPR

new Phaser.Game(config)

document.addEventListener('visibilitychange', () => {
  const g = Phaser.GAMES && Phaser.GAMES[0] as Phaser.Game | undefined
  if (!g) return
  const scene = g.scene.getScene('Game') as GameScene | undefined
  if (document.hidden) scene?.autoPause()
  else scene?.autoResume()
})

// -------------------------
// src/types.ts
// -------------------------
export interface RemoteConfig {
  sessionSeconds: number
  spawnRate: number
  comboDecayPerSecond: number
}

export interface ScoreEntry { id?: string; name: string; score: number; created_at?: string }

// -------------------------
// src/systems/Config.ts
// -------------------------
import type { RemoteConfig } from '../types'

const DEFAULTS: RemoteConfig = {
  sessionSeconds: 90,
  spawnRate: 3,
  comboDecayPerSecond: 0.5
}

export class ConfigService {
  private static _instance: ConfigService
  cfg: RemoteConfig = DEFAULTS

  static get instance() { return (this._instance ??= new ConfigService()) }

  async load(url = '/remote-config.json'): Promise<RemoteConfig> {
    try {
      const res = await fetch(url, { cache: 'no-store' })
      if (!res.ok) throw new Error('bad status')
      this.cfg = { ...DEFAULTS, ...(await res.json()) }
    } catch { this.cfg = DEFAULTS }
    return this.cfg
  }
}

// -------------------------
// public/remote-config.json (create this file in /public)
// -------------------------
{
  "sessionSeconds": 90,
  "spawnRate": 3,
  "comboDecayPerSecond": 0.5
}

// -------------------------
// src/systems/SaveService.ts
// -------------------------
export class SaveService {
  private key = 'stormchaser.save.v1'
  read<T>(fallback: T): T {
    try { return JSON.parse(localStorage.getItem(this.key) || '') as T } catch { return fallback }
  }
  write<T>(data: T) {
    try { localStorage.setItem(this.key, JSON.stringify(data)) } catch {}
  }
}

// -------------------------
// src/systems/NetService.ts
// -------------------------
export class NetService {
  async get<T>(url: string): Promise<T> {
    const res = await fetch(url)
    if (!res.ok) throw new Error('Network error')
    return await res.json() as T
  }
  async post<T>(url: string, body: unknown): Promise<T> {
    const res = await fetch(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    if (!res.ok) throw new Error('Network error')
    return await res.json() as T
  }
}

// -------------------------
// src/systems/LeaderboardService.ts (stubbed; wire to Supabase later)
// -------------------------
import type { ScoreEntry } from '../types'

export class LeaderboardService {
  async getTop(limit = 10): Promise<ScoreEntry[]> {
    // TODO: Replace with Supabase or your API
    const raw = localStorage.getItem('stormchaser.leaderboard')
    const arr: ScoreEntry[] = raw ? JSON.parse(raw) : []
    return arr.sort((a,b)=>b.score-a.score).slice(0, limit)
  }
  async submitScore(entry: ScoreEntry): Promise<void> {
    const raw = localStorage.getItem('stormchaser.leaderboard')
    const arr: ScoreEntry[] = raw ? JSON.parse(raw) : []
    arr.push({ ...entry, created_at: new Date().toISOString() })
    localStorage.setItem('stormchaser.leaderboard', JSON.stringify(arr))
  }
}

// -------------------------
// src/entities/Player.ts
// -------------------------
import Phaser from 'phaser'

export class Player extends Phaser.Physics.Arcade.Sprite {
  speed = 140
  accel = 400
  drag = 420
  maxSpeed = 220
  combo = 0
  filming = false

  cursors!: Phaser.Types.Input.Keyboard.CursorKeys
  keys!: { film: Phaser.Input.Keyboard.Key }

  constructor(scene: Phaser.Scene, x: number, y: number) {
    super(scene, x, y, 'player')
  }

  initInput() {
    this.cursors = this.scene.input.keyboard!.createCursorKeys()
    this.keys = { film: this.scene.input.keyboard!.addKey('SPACE') }
  }

  preUpdate(time: number, delta: number) {
    super.preUpdate(time, delta)
    const body = this.body as Phaser.Physics.Arcade.Body
    const accel = this.accel

    let vx = 0, vy = 0
    if (this.cursors.left?.isDown) vx -= accel
    if (this.cursors.right?.isDown) vx += accel
    if (this.cursors.up?.isDown) vy -= accel
    if (this.cursors.down?.isDown) vy += accel

    body.setDrag(this.drag, this.drag)
    body.velocity.x = Phaser.Math.Clamp(body.velocity.x + vx * (delta/1000), -this.maxSpeed, this.maxSpeed)
    body.velocity.y = Phaser.Math.Clamp(body.velocity.y + vy * (delta/1000), -this.maxSpeed, this.maxSpeed)

    this.filming = this.keys.film.isDown
  }
}

// -------------------------
// src/ui/HUD.ts
// -------------------------
import Phaser from 'phaser'

export class HUD {
  scene: Phaser.Scene
  timeText!: Phaser.GameObjects.Text
  scoreText!: Phaser.GameObjects.Text
  comboText!: Phaser.GameObjects.Text

  constructor(scene: Phaser.Scene) {
    this.scene = scene
  }

  create() {
    const s = this.scene
    const style: Phaser.Types.GameObjects.Text.TextStyle = { fontFamily: 'monospace', fontSize: '16px', color: '#e6edf3' }
    this.timeText = s.add.text(12, 10, 'Time: 90', style).setScrollFactor(0)
    this.scoreText = s.add.text(12, 30, 'Score: 0', style).setScrollFactor(0)
    this.comboText = s.add.text(12, 50, 'Combo: 0x', style).setScrollFactor(0)
  }

  update(timeLeft: number, score: number, combo: number) {
    this.timeText.setText(`Time: ${Math.ceil(timeLeft)}`)
    this.scoreText.setText(`Score: ${score}`)
    this.comboText.setText(`Combo: ${combo.toFixed(1)}x`)
  }
}

// -------------------------
// src/scenes/Boot.ts
// -------------------------
import Phaser from 'phaser'

export class Boot extends Phaser.Scene {
  constructor() { super('Boot') }
  preload() {
    // Create simple procedurally-generated textures as placeholders
    const g = this.add.graphics()
    g.fillStyle(0x6cf0ff, 1).fillCircle(8, 8, 8)
    g.generateTexture('player', 16, 16)
    g.clear()

    g.fillStyle(0xffe066, 1).fillCircle(6, 6, 6)
    g.generateTexture('pickup_fuel', 12, 12)
    g.clear()

    g.fillStyle(0xa077ff, 1).fillCircle(6, 6, 6)
    g.generateTexture('pickup_combo', 12, 12)
    g.clear()

    g.fillStyle(0xffffff, 1).fillCircle(10, 10, 10)
    g.generateTexture('tornado', 20, 20)
    g.destroy()
  }
  create() { this.scene.start('Title') }
}

// -------------------------
// src/scenes/Title.ts
// -------------------------
import Phaser from 'phaser'
import { ConfigService } from '../systems/Config'

export class Title extends Phaser.Scene {
  constructor() { super('Title') }
  async create() {
    await ConfigService.instance.load('/remote-config.json')
    const { width, height } = this.scale
    this.add.text(width/2, height/2 - 30, 'STORMCHASER LOOP', { fontFamily: 'monospace', fontSize: '28px', color: '#e6edf3' }).setOrigin(0.5)
    this.add.text(width/2, height/2 + 10, 'Arrow keys/WASD to drive — Space to FILM', { fontFamily: 'monospace', fontSize: '14px', color: '#aab3c2' }).setOrigin(0.5)
    this.add.text(width/2, height/2 + 40, 'Press ENTER to start', { fontFamily: 'monospace', fontSize: '14px', color: '#93e1ff' }).setOrigin(0.5)

    this.input.keyboard!.once('keydown-ENTER', () => this.scene.start('Game'))
  }
}

// -------------------------
// src/scenes/Game.ts
// -------------------------
import Phaser from 'phaser'
import { Player } from '../entities/Player'
import { HUD } from '../ui/HUD'
import { ConfigService } from '../systems/Config'

class Collectable extends Phaser.Physics.Arcade.Sprite {
  type: 'fuel' | 'combo' | 'shot' = 'fuel'
}

export class GameScene extends Phaser.Scene {
  player!: Player
  hud!: HUD
  timeLeft = 90
  score = 0
  combo = 1
  comboDecay = 0.5
  paused = false
  spawnTimer = 0

  pickups!: Phaser.Physics.Arcade.Group
  tornado!: Phaser.Physics.Arcade.Sprite

  constructor() { super('Game') }

  create() {
    const cfg = ConfigService.instance.cfg
    this.timeLeft = cfg.sessionSeconds
    this.comboDecay = cfg.comboDecayPerSecond

    // World bounds
    this.physics.world.setBounds(0,0,1600,900)
    this.cameras.main.setBounds(0,0,1600,900)

    // Background hint grid
    const grid = this.add.grid(800, 450, 1600, 900, 50, 50, 0x0d1319, 0.9, 0x12202b, 0.25)
    grid.setDepth(-5)

    // Player
    this.player = new Player(this, 800, 450)
    this.add.existing(this.player)
    this.physics.add.existing(this.player)
    this.player.setCollideWorldBounds(true)
    this.player.initInput()
    this.cameras.main.startFollow(this.player, true, 0.1, 0.1)

    // Tornado (target to film)
    this.tornado = this.physics.add.sprite(Phaser.Math.Between(200,1400), Phaser.Math.Between(200,700), 'tornado')
    this.tornado.setAlpha(0.9)

    // Pickups
    this.pickups = this.physics.add.group({ classType: Collectable, runChildUpdate: false })

    // Collisions
    this.physics.add.overlap(this.player, this.pickups, (_p, c) => this.onPickup(c as Collectable))

    // HUD
    this.hud = new HUD(this)
    this.hud.create()

    // Pause/resume
    this.input.keyboard!.on('keydown-ESC', () => this.togglePause())
  }

  update(_time: number, delta: number) {
    if (this.paused) return
    const dt = delta / 1000

    // Countdown
    this.timeLeft -= dt
    if (this.timeLeft <= 0) { this.endRun(); return }

    // Move tornado slowly to random points
    const tw = this.tornado
    if (!tw.active || !tw.body) return this.endRun()
    const body = tw.body as Phaser.Physics.Arcade.Body
    if (body.speed < 20) {
      const tx = Phaser.Math.Between(100, 1500)
      const ty = Phaser.Math.Between(100, 800)
      this.physics.moveTo(tw, tx, ty, 50)
    }

    // Spawns
    this.spawnTimer += dt
    if (this.spawnTimer >= ConfigService.instance.cfg.spawnRate) {
      this.spawnTimer = 0
      this.spawnPickup()
    }

    // Filming mechanic: if filming and near tornado, award points and build combo
    const dist = Phaser.Math.Distance.Between(this.player.x, this.player.y, tw.x, tw.y)
    const inRange = dist < 140
    if (this.player.filming && inRange) {
      const gain = 10 * this.combo * dt
      this.score += gain
      this.combo = Math.min(this.combo + 0.6 * dt, 5)
      this.addScorePopup(`+${gain.toFixed(0)}`, tw.x, tw.y)
    } else {
      // decay combo toward 1x
      this.combo = Math.max(1, this.combo - this.comboDecay * dt)
    }

    this.hud.update(this.timeLeft, Math.floor(this.score), this.combo)
  }

  spawnPickup() {
    const type = Math.random() < 0.5 ? 'fuel' : 'combo'
    const p = this.pickups.get(Phaser.Math.Between(60, 1540), Phaser.Math.Between(60, 840)) as Collectable
    if (!p) return
    p.type = type as any
    p.setTexture(type === 'fuel' ? 'pickup_fuel' : 'pickup_combo')
    p.setActive(true).setVisible(true)
  }

  onPickup(p: Collectable) {
    if (p.type === 'fuel') this.timeLeft = Math.min(this.timeLeft + 6, ConfigService.instance.cfg.sessionSeconds)
    if (p.type === 'combo') this.combo = Math.min(this.combo + 0.8, 5)
    p.disableBody(true, true)
  }

  addScorePopup(text: string, x: number, y: number) {
    const t = this.add.text(x, y - 20, text, { fontFamily: 'monospace', fontSize: '12px', color: '#9be9a8' }).setOrigin(0.5)
    this.tweens.add({ targets: t, y: y - 50, alpha: 0, duration: 500, onComplete: () => t.destroy() })
  }

  togglePause() { this.paused = !this.paused }
  autoPause() { this.paused = true }
  autoResume() { this.paused = false }

  endRun() {
    this.scene.start('Results', { score: Math.floor(this.score) })
  }
}

// -------------------------
// src/scenes/Results.ts
// -------------------------
import Phaser from 'phaser'
import { LeaderboardService } from '../systems/LeaderboardService'

export class Results extends Phaser.Scene {
  private score = 0
  private lb = new LeaderboardService()
  constructor() { super('Results') }

  init(data: { score: number }) { this.score = data?.score ?? 0 }

  async create() {
    const { width, height } = this.scale
    this.add.text(width/2, 140, 'RESULTS', { fontFamily: 'monospace', fontSize: '28px', color: '#e6edf3' }).setOrigin(0.5)
    this.add.text(width/2, 200, `Score: ${this.score}`, { fontFamily: 'monospace', fontSize: '18px', color: '#9be9a8' }).setOrigin(0.5)

    // Name input (super simple)
    const name = 'YOU'
    await this.lb.submitScore({ name, score: this.score })
    const top = await this.lb.getTop(10)

    let y = 250
    this.add.text(width/2, y-24, 'TOP SCORES', { fontFamily: 'monospace', fontSize: '16px', color: '#aab3c2' }).setOrigin(0.5)
    top.forEach((s, i) => {
      this.add.text(width/2, y + i*18, `${String(i+1).padStart(2,'0')}  ${s.name.padEnd(6,' ')}  ${s.score}`, { fontFamily: 'monospace', fontSize: '14px', color: '#e6edf3' }).setOrigin(0.5)
    })

    this.add.text(width/2, height - 80, 'ENTER = Retry   ·   T = Title', { fontFamily: 'monospace', fontSize: '14px', color: '#93e1ff' }).setOrigin(0.5)

    this.input.keyboard!.once('keydown-ENTER', () => this.scene.start('Game'))
    this.input.keyboard!.once('keydown-T', () => this.scene.start('Title'))
  }
}

// -------------------------
// NOTES & NEXT STEPS
// -------------------------
// 1) Mobile controls: add a virtual joystick; cap particles and reduce draw calls on iOS.
// 2) Supabase Leaderboard: replace LeaderboardService stub with real PostgREST calls + RLS.
// 3) Art pass: use sprite atlas for player/car, tornado with swirl frames, pickups, UI icons.
// 4) Live ops: tweak /public/remote-config.json spawnRate & sessionSeconds without redeploys.
// 5) Packaging: deploy to Netlify/Cloudflare; for desktop later, wrap with Tauri.
