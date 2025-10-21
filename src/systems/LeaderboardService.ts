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

