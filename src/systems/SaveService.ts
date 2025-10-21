export class SaveService {
  private key = 'stormchaser.save.v1'
  read<T>(fallback: T): T {
    try { return JSON.parse(localStorage.getItem(this.key) || '') as T } catch { return fallback }
  }
  write<T>(data: T) {
    try { localStorage.setItem(this.key, JSON.stringify(data)) } catch {}
  }
}

