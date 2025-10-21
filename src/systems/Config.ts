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

  async load(url = './remote-config.json'): Promise<RemoteConfig> {
    try {
      const res = await fetch(url, { cache: 'no-store' })
      if (!res.ok) throw new Error('bad status')
      this.cfg = { ...DEFAULTS, ...(await res.json()) }
    } catch (e) { 
      console.warn('Failed to load remote config, using defaults:', e)
      this.cfg = DEFAULTS 
    }
    return this.cfg
  }
}

