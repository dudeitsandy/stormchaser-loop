import Phaser from 'phaser'
import { LeaderboardService } from '../systems/LeaderboardService'
import { Photo } from '../systems/CameraSystem'
import { PhotoRenderer } from '../systems/PhotoRenderer'
import { SoundService } from '../systems/SoundService'

export class Results extends Phaser.Scene {
  private score = 0
  private photos: Photo[] = []
  private lb = new LeaderboardService()
  private photoRenderer!: PhotoRenderer
  
  constructor() { super('Results') }

  init(data: { score: number, photos: Photo[] }) {
    console.log('🏆 Results scene init:', data)
    this.score = data?.score ?? 0
    this.photos = data?.photos ?? []
  }

  async create() {
    console.log('🏆 Results scene creating...')
    
    const { width, height } = this.scale
    
    // Immediate visible background
    this.add.rectangle(width/2, height/2, width, height, 0x0a0e12, 1)
    
    // Confirmation text
    this.add.text(width/2, 10, '✓ Results Scene Loaded', {
      fontFamily: 'monospace',
      fontSize: '11px',
      color: '#00ff00'
    }).setOrigin(0.5)
    
    this.photoRenderer = new PhotoRenderer(this)
    
    // Title
    this.add.text(width/2, 30, 'SESSION COMPLETE', { 
      fontFamily: 'monospace', 
      fontSize: '24px', 
      color: '#e6edf3' 
    }).setOrigin(0.5)
    
    // Score
    this.add.text(width/2, 65, `Final Score: ${this.score}`, { 
      fontFamily: 'monospace', 
      fontSize: '18px', 
      color: '#9be9a8' 
    }).setOrigin(0.5)
    
    // Photos taken
    this.add.text(width/2, 90, `Photos Taken: ${this.photos.length}`, { 
      fontFamily: 'monospace', 
      fontSize: '14px', 
      color: '#aab3c2' 
    }).setOrigin(0.5)

    // Photo gallery with polaroid frames - ALWAYS show 3 frames
    this.add.text(width/2, 120, 'YOUR BEST SHOTS', { 
      fontFamily: 'monospace', 
      fontSize: '16px', 
      color: '#ffaa00',
      stroke: '#000000',
      strokeThickness: 2
    }).setOrigin(0.5)
    
    // Get top 3 photos (or null for blank frames)
    const topPhotos = this.getTop3Photos()
    
    // Always show 3 polaroid frames
    for (let i = 0; i < 3; i++) {
      const photo = topPhotos[i]
      const polaroidContainer = this.createPolaroidFrame(photo, i)
      polaroidContainer.setPosition(
        width/2 - 200 + (i * 200),
        200
      )
      this.add.existing(polaroidContainer)
    }
    
    // Show tip if no photos were taken
    if (this.photos.length === 0) {
      this.add.text(width/2, 320, 'Tip: Hold SPACE to aim, release to snap!', { 
        fontFamily: 'monospace', 
        fontSize: '12px', 
        color: '#aab3c2',
        stroke: '#000000',
        strokeThickness: 1
      }).setOrigin(0.5)
    }

    // Leaderboard
    const name = 'YOU'
    await this.lb.submitScore({ name, score: this.score })
    const top = await this.lb.getTop(5) // Show top 5 to save space

    let y = 385
    this.add.text(width/2, y-24, 'LEADERBOARD', { 
      fontFamily: 'monospace', 
      fontSize: '14px', 
      color: '#aab3c2' 
    }).setOrigin(0.5)
    
    top.forEach((s, i) => {
      this.add.text(width/2, y + i*16, `${String(i+1).padStart(2,'0')}  ${s.name.padEnd(6,' ')}  ${s.score}`, { 
        fontFamily: 'monospace', 
        fontSize: '12px', 
        color: '#e6edf3' 
      }).setOrigin(0.5)
    })

    // Controls
    this.add.text(width/2, height - 20, 'ENTER = Retry   ·   T = Title', { 
      fontFamily: 'monospace', 
      fontSize: '14px', 
      color: '#93e1ff' 
    }).setOrigin(0.5)

    this.input.keyboard!.once('keydown-ENTER', () => {
      SoundService.playClick()
      this.scene.start('Game')
    })
    this.input.keyboard!.once('keydown-T', () => {
      SoundService.playClick()
      this.scene.start('Title')
    })
  }

  getTop3Photos(): (Photo | null)[] {
    // Get top 3 photos by score, pad with null for blank frames
    const sortedPhotos = [...this.photos].sort((a, b) => b.score - a.score)
    const topPhotos = sortedPhotos.slice(0, 3)
    
    // Pad with null to always have 3 items
    while (topPhotos.length < 3) {
      topPhotos.push(null)
    }
    
    return topPhotos
  }

  createPolaroidFrame(photo: Photo | null, index: number): Phaser.GameObjects.Container {
    const container = this.add.container(0, 0)
    
    // Polaroid frame background (white with shadow)
    const frameBg = this.add.graphics()
    frameBg.fillStyle(0xffffff, 1)
    frameBg.fillRoundedRect(-5, -5, 160, 200, 8)
    frameBg.fillStyle(0x000000, 0.3)
    frameBg.fillRoundedRect(0, 0, 150, 190, 6)
    container.add(frameBg)
    
    // Photo area (white background)
    const photoBg = this.add.graphics()
    photoBg.fillStyle(0xffffff, 1)
    photoBg.fillRoundedRect(5, 5, 140, 120, 4)
    container.add(photoBg)
    
    if (photo) {
      // Generate the actual photo
      const photoContainer = this.photoRenderer.generatePhoto(photo, 130, 110)
      photoContainer.setPosition(70, 65) // Center in frame
      container.add(photoContainer)
    } else {
      // Blank photo area with placeholder text
      const blankText = this.add.text(70, 65, 'NO PHOTO', {
        fontFamily: 'monospace',
        fontSize: '14px',
        color: '#999999',
        fontStyle: 'italic'
      }).setOrigin(0.5)
      container.add(blankText)
    }
    
    // Polaroid caption area
    const captionBg = this.add.graphics()
    captionBg.fillStyle(0xffffff, 1)
    captionBg.fillRoundedRect(5, 130, 140, 55, 4)
    container.add(captionBg)

    if (photo) {
      // Photo quality label
      const qualityText = this.add.text(70, 140, photo.quality.toUpperCase(), {
        fontFamily: 'monospace',
        fontSize: '10px',
        color: '#333333',
        fontStyle: 'bold'
      }).setOrigin(0.5)
      container.add(qualityText)

      // Score
      const scoreText = this.add.text(70, 155, `Score: ${photo.score}`, {
        fontFamily: 'monospace',
        fontSize: '9px',
        color: '#666666'
      }).setOrigin(0.5)
      container.add(scoreText)
    } else {
      // Blank photo caption
      const blankCaption = this.add.text(70, 140, 'EMPTY FRAME', {
        fontFamily: 'monospace',
        fontSize: '10px',
        color: '#999999',
        fontStyle: 'italic'
      }).setOrigin(0.5)
      container.add(blankCaption)
    }

    // Rank badge (always show position)
    const rankColors = ['#ffd700', '#c0c0c0', '#cd7f32'] // Gold, Silver, Bronze
    const rankText = this.add.text(70, 170, `#${index + 1}`, {
      fontFamily: 'monospace',
      fontSize: '12px',
      color: rankColors[index] || '#666666',
      fontStyle: 'bold',
      stroke: '#000000',
      strokeThickness: 1
    }).setOrigin(0.5)
    container.add(rankText)
    
    // Slight rotation for authentic polaroid look
    container.setRotation(Phaser.Math.Between(-0.1, 0.1))
    
    return container
  }
}
