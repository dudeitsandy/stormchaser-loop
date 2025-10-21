import Phaser from 'phaser'
import { Boot } from './scenes/Boot'
import { Title } from './scenes/Title'
import { GameScene } from './scenes/Game'
import { GameOver } from './scenes/GameOver'
import { Results } from './scenes/Results'

const DPR = Math.min(window.devicePixelRatio || 1, 2)

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  parent: 'game',
  backgroundColor: '#0a0e12',
  scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH, width: 800, height: 450 },
  physics: { default: 'arcade', arcade: { gravity: { y: 0 }, debug: false } },
  render: { pixelArt: true, roundPixels: true, antialias: false },
  scene: [Boot, Title, GameScene, GameOver, Results]
}

// Respect high-DPI by scaling canvas; Phaser handles internally via resolution
;(config as any).resolution = DPR

const game = new Phaser.Game(config)

// Global error handler to catch and log issues
window.addEventListener('error', (e) => {
  console.error('🚨 GLOBAL ERROR CAUGHT:', e.error)
  console.error('🚨 Stack:', e.error?.stack)
  console.error('🚨 Event:', e)
  
  // Show error on screen for debugging
  const errorDiv = document.createElement('div')
  errorDiv.style.cssText = `
    position: fixed; top: 50px; left: 10px; z-index: 99999;
    background: red; color: white; padding: 10px; font-family: monospace;
    border: 2px solid white; max-width: 400px;
  `
  errorDiv.textContent = `ERROR: ${e.error?.message || 'Unknown error'}`
  document.body.appendChild(errorDiv)
})

// Catch unhandled promise rejections
window.addEventListener('unhandledrejection', (e) => {
  console.error('🚨 UNHANDLED PROMISE REJECTION:', e.reason)
  
  // Show error on screen for debugging
  const errorDiv = document.createElement('div')
  errorDiv.style.cssText = `
    position: fixed; top: 100px; left: 10px; z-index: 99999;
    background: orange; color: white; padding: 10px; font-family: monospace;
    border: 2px solid white; max-width: 400px;
  `
  errorDiv.textContent = `PROMISE ERROR: ${e.reason}`
  document.body.appendChild(errorDiv)
})

document.addEventListener('visibilitychange', () => {
  const g = Phaser.GAMES && Phaser.GAMES[0] as Phaser.Game | undefined
  if (!g) return
  const scene = g.scene.getScene('Game') as GameScene | undefined
  if (document.hidden) scene?.autoPause()
  else scene?.autoResume()
})

// Debug: Log scene changes
game.events.on('prestep', () => {
  const activeScenes = game.scene.scenes.filter(s => s.scene.isActive())
  if (activeScenes.length === 0) {
    console.warn('⚠️ No active scenes!')
  }
})

