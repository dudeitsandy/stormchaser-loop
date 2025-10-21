export interface RemoteConfig {
  sessionSeconds: number
  spawnRate: number
  comboDecayPerSecond: number
}

export interface ScoreEntry { id?: string; name: string; score: number; created_at?: string }

