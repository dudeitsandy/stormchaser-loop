# Run Goals story 005 — WebGL save check (Save & Profile M1 stem)

**Date:** 2026-10-05 · **Build:** 0.7.9 source + save stem · **Method:** `?savecheck=1` counts one launch per page
load and writes the profile; Playwright reloads the page in the same browser context and reads the console.

| Build | Launch counter across reloads | Result |
|---|---|---|
| With explicit `FS.syncfs` after each write (`Plugins/WebGL/ProfileSync.jslib`) | 1 → 2 → 3 | Persists |
| Sync removed (testing whether Unity 6 syncs on its own) | 4 → 4 | **Write lost on reload** |
| Sync restored | 4 → 5 → 6 | Persists |

**Result: PASS.** M1 persistence is real (not session-only), and it depends on the explicit sync. The first build
logged "[Profile] IndexedDB sync failed: [object Object]" on each write while still persisting. The restored build
logs the error's detail instead; none appeared in the final run. The legacy PlayerPrefs best (1,249) imported once.
