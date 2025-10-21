import { zzfx } from 'zzfx'

/**
 * Sound service using ZzFX for procedural audio generation
 * All sounds are generated at runtime - no audio files needed!
 */
export class SoundService {
  private static enabled = true

  static setEnabled(enabled: boolean) {
    this.enabled = enabled
  }

  /**
   * Camera snap - crisp, satisfying shutter sound
   */
  static playCamera() {
    if (!this.enabled) return
    // Sharp click with quick decay - sounds like a professional camera
    zzfx(...[1.5,,925,.04,.3,.6,1,.3,,6.27,-184,.09,.17])
  }

  /**
   * UI click - subtle button feedback
   */
  static playClick() {
    if (!this.enabled) return
    // Soft beep
    zzfx(...[0.5,,261,.02,.04,.09,1,1.88,,,,,,,,.1])
  }

  /**
   * Collision sound - impact thud that scales with severity
   */
  static playCollision(severity: number = 1) {
    if (!this.enabled) return
    // Thud/crash sound - louder for harder impacts
    const volume = Math.min(2, 0.5 + severity * 0.5)
    zzfx(...[volume,,315,.01,.14,.41,3,.67,-0.1,,,,.14])
  }

  /**
   * Pickup collection - positive chime
   */
  static playPickup(type: 'fuel' | 'combo' = 'fuel') {
    if (!this.enabled) return
    if (type === 'fuel') {
      // Lower pitch for fuel (gold)
      zzfx(...[0.8,,329,.01,.04,.17,1,1.88,7.83,,,,.14])
    } else {
      // Higher pitch for combo (purple)
      zzfx(...[0.8,,629,.01,.04,.17,1,1.88,7.83,,,,.14])
    }
  }

  /**
   * Weather alert beep - urgent warning sound
   */
  static playAlert() {
    if (!this.enabled) return
    // Two-tone emergency alert
    zzfx(...[1,,400,.01,.04,.05,1,2,,,,,,,,.1])
    setTimeout(() => {
      zzfx(...[1,,350,.01,.04,.05,1,2,,,,,,,,.1])
    }, 150)
  }

  /**
   * Danger warning - plays when in tornado danger zone
   */
  static playDanger() {
    if (!this.enabled) return
    // Low rumble/warning
    zzfx(...[0.3,,100,.01,.1,.2,4,.1,,,,,,,,.1])
  }

  /**
   * Health critical - plays when health low
   */
  static playHealthCritical() {
    if (!this.enabled) return
    // Alarm beep
    zzfx(...[0.7,,800,.01,.02,.04,1,2,,,,,,,,.1])
  }

  /**
   * Photo quality feedback sounds
   */
  static playPhotoQuality(quality: 'poor' | 'decent' | 'good' | 'excellent' | 'perfect') {
    if (!this.enabled) return
    switch (quality) {
      case 'poor':
        zzfx(...[0.5,,200,.02,.05,.05,1,1,,,,,,,,.1])
        break
      case 'decent':
        zzfx(...[0.6,,350,.02,.06,.08,1,1.2,,,,,,,,.1])
        break
      case 'good':
        zzfx(...[0.7,,500,.02,.08,.12,1,1.5,,,,,,,,.1])
        break
      case 'excellent':
        zzfx(...[0.8,,700,.02,.1,.15,1,1.8,,,,,,,,.1])
        break
      case 'perfect':
        // Special multi-note for perfect!
        zzfx(...[1,,900,.02,.12,.2,1,2,,,,,,,,.1])
        setTimeout(() => zzfx(...[1,,1200,.02,.12,.2,1,2,,,,,,,,.1]), 80)
        setTimeout(() => zzfx(...[1,,1500,.02,.12,.2,1,2,,,,,,,,.1]), 160)
        break
    }
  }

  /**
   * Game over sound
   */
  static playGameOver() {
    if (!this.enabled) return
    // Descending tone (sad trombone effect)
    zzfx(...[1.5,,400,.1,.3,.5,2,.1,,-2,,,,.5])
  }

  /**
   * Victory/results sound
   */
  static playVictory() {
    if (!this.enabled) return
    // Ascending victory fanfare
    zzfx(...[1,,500,.02,.08,.2,1,2,,,,,,,,.1])
    setTimeout(() => zzfx(...[1,,700,.02,.08,.2,1,2,,,,,,,,.1]), 100)
    setTimeout(() => zzfx(...[1,,900,.02,.1,.3,1,2,,,,,,,,.1]), 200)
  }
}

