# Save & Profile

> **Status**: In Design
> **Author**: Andy Styx + Claude
> **Last Updated**: 2026-10-01
> **Last Verified**: 2026-10-01
> **Implements Pillar**: 5 — Roguelite Identity & Cataclysm Heat (game-concept.md)

## Summary

Save & Profile keeps everything that outlives a run — Storm Dollars, unlocks, best scores and the Newspaper
Album — in three versioned, checksummed slot files, written atomically with a backup and only at
checkpoints. It is the Foundation layer every meta-progression system reads through, and it replaces the
0.x `BestScoreStore`.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: Platform storage
> (`persistentDataPath` / IndexedDB), `economy-progression.md`, `session-modes.md`

## Overview

Save & Profile is the persistence layer: it holds everything that has to outlive a single
run. Each of three **profile slots** is a single versioned JSON file in the platform's
persistent data folder. A slot records:

- the Storm Dollars balance;
- garage unlocks and permanent perks;
- mode and Heat unlocks;
- best scores;
- lifetime stats;
- the last-used loadout;
- the index of saved Newspaper Album covers.

Album covers are stored as separate image files in the slot's folder, so the profile
file stays small. **Device settings** (audio, video, control remapping) live in one
shared file outside the slots, because they belong to the machine rather than the
player.

The game saves only at **checkpoints**: run end, purchases, unlocks, settings changes and
slot operations. An in-progress run is never saved; quitting mid-run forfeits it.

Every write is **atomic**: the data is written to a temp file, then swapped in, and the
previous version is kept as a backup. Each file carries a checksum that detects
corruption, so a damaged save falls back to the backup instead of loading garbage. The
checksum isn't anti-cheat.

Each file has a schema version and runs forward migrations on load. The first migration
imports the 0.x Sprint best score from `PlayerPrefs` into slot 1.

The layout works unchanged on Windows, in the browser build (IndexedDB) and with Steam
Auto-Cloud. Other systems never touch files: they read and write typed profile data
through this system, which owns all serialization and storage.

## Detailed Design

### Core Rules

1. **Layout** in `persistentDataPath`:
   ```
   device.json (+ .bak)              settings, LastSlot, LegacyImported
   slot1/ profile.json (+ .bak)      the profile
   slot1/ album/<coverId>.png        saved covers
   slot2/, slot3/                    same shape
   ```
2. **Profile contents (schema v1):**
   - `SchemaVersion`, `CreatedAt`, `LastPlayedAt`, `DisplayName` (printed on the
     Newspaper footer)
   - `StormDollars` (integer, never negative), `LifetimeStormDollars`
   - `Unlocks`: a set of stable string IDs covering vehicles, lenses, scanners,
     payloads and perks (`vehicle.pickup`, `lens.telephoto`, `perk.photojournalist`)
   - `ModeUnlocks` (`chase`, `heat`), `HighestHeatCleared` (0–5)
   - `BestScores`: Sprint, plus Chase at each Heat level 0–5
   - `Stats`: runs completed, photos taken, NPCs rescued, play time
   - `LastLoadout`: vehicle, lens, payloads and Heat, all as IDs
   - `Album`: entries of `{CoverId, Date, Mode, Heat, Score, Headline, Pinned}`
   - `OnboardingFlags`: a set of string flags

   IDs are never display names or enum numbers, so renaming content in a patch doesn't
   break saves. An unlock ID the current build doesn't recognize is **kept** on the next
   write, so a purchase survives a patch that removes its content.
3. **Access.** Systems read a typed view of the active profile and change it only
   through this system's operations, which mark the profile dirty. Spending is one
   **transaction**: check the balance, subtract, add the unlock, save. It completes
   whole or not at all.
4. **Checkpoints** (the only times it writes):
   - **run complete**: banked Storm Dollars, best scores, stats, unlocks and the album
     cover, all in one write;
   - **purchase**;
   - **leaving the settings menu**, which writes the device file;
   - **slot operations**: create, switch, delete, reset;
   - **album operations**: pin, unpin, delete;
   - **app quit, browser tab hidden, window losing focus**: these only flush changes
     that are already pending. In-run state is never saved, and quitting mid-run
     forfeits the run.
5. **Write protocol.**
   1. Serialize the data inside an envelope `{schema, checksum, payload}`.
   2. Write it to a temp file.
   3. Rename the current file to `.bak`.
   4. Rename the temp file to current.

   In the browser build, the IndexedDB sync happens after step 4; how that works is an
   implementation detail for an ADR (see Open Questions).
6. **Load protocol.** The current file loads if its checksum and parse both pass.
   Otherwise the `.bak` loads. If both fail, the slot becomes **Corrupt**: nothing
   overwrites it automatically, and the player is offered "Reset slot". On reset, the
   damaged files are first renamed `.corrupt-<timestamp>` and kept.
7. **Migration.** On load, migrations run in order (v1→v2→…). The file before migration
   is kept as `.v<n>.bak`, and the migrated profile is saved immediately. A file with a
   newer schema than this build knows opens **read-only**: it can be played, but nothing
   is written, so an old build can't wipe newer data.
8. **0.x import.** On the very first boot (no `device.json`), the old
   `Doomsday.Sprint.BestScore` value is copied into slot 1's Sprint best score. The
   `LegacyImported` flag makes this happen exactly once. The old key itself is left in
   place.
9. **Slots.**
   - There are 3 slots. The game boots straight into `LastSlot`, and the first boot
     creates slot 1 without asking.
   - A Profiles menu (on the title screen) offers switch, new (into an empty slot),
     rename, reset and delete. Reset and delete need a second confirmation.
   - Slots can't be switched during a run.
10. **Album.**
    - A cover is saved only when the player owns `perk.photojournalist`
      (`economy-progression.md`), and each slot holds at most `AlbumCap` covers.
    - When the album is full, a new cover replaces the lowest-scoring **unpinned** cover.
      If it scores lower than every unpinned cover, it isn't saved.
    - If every cover is pinned, the new one isn't saved, and the Results screen says
      "Album full: unpin a cover to keep new front pages."
    - Deleting a cover removes its image and its entry together.
11. **Device file.** It holds settings, `LastSlot` and `LegacyImported`, and has the same
    atomic write, backup and checksum as a profile. A corrupt device file falls back
    to its `.bak`; if both fail, settings reset to defaults and `LastSlot` = 1. The slots are unaffected.

> `systems-designer` not consulted — Lean mode. Review manually before production.

### States and Transitions

Per slot:

| State | Meaning | Enter when | Exit to |
|---|---|---|---|
| Empty | No `profile.json` | First boot (slots 2–3), after delete | Ready on create |
| Ready | Loaded and clean | Load or save succeeded | Dirty on any change |
| Dirty | Changes waiting for a checkpoint | An operation changed data | Saving at a checkpoint |
| Saving | Write in progress | Checkpoint reached | Ready on success; Dirty + warning on failure |
| Corrupt | Neither current nor `.bak` loads | Load failed twice | Ready after reset (damaged files kept) |
| ReadOnly | Schema newer than this build | Load found a newer version | Only by running a newer build; no writes |

### Interactions with Other Systems

| System | Reads | Writes | Notes |
|---|---|---|---|
| Run Manager & Session Modes | mode unlocks, best scores, `LastLoadout` | run-complete record (one checkpoint) | Triggers the run-complete checkpoint |
| Storm Dollars & HQ Garage | balance, unlocks | purchase transaction, banked dollars | Earning formula stays in `economy-progression.md` |
| Cataclysm Heat | `ModeUnlocks.heat`, `HighestHeatCleared` | updated on run complete | |
| Vehicle Loadout & Archetypes | unlocks, `LastLoadout` | `LastLoadout` | |
| Run Screens (Newspaper Cover) | album entries and images | new cover (if Photojournalist) | |
| Settings | device file | device file | Settings owns the values; Save owns the file |
| Onboarding | `OnboardingFlags` | flags | |
| Online Leaderboard | best scores | — | Submits from the profile; doesn't store identity here (Open Questions) |
| HUD / Title | best score, balance, `DisplayName` | — | |

## Formulas

> `systems-designer` consulted 2026-10-01 (lean mode, high-risk section). Its integer-score assumption was
> corrected: run scores are floats (`RunSummary.Score`) and are stored rounded.

**F1. Album eviction**

The album eviction formula is defined as:

`victim = argmin over unpinned e of (e.Score, e.Date, e.CoverId)` (lexicographic: lowest score, then
oldest, then lowest ID). A new cover N is saved only if `(N.Score, N.Date, N.CoverId) > victim` under
the same ordering.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| e.Score | int | 0 to 2³¹−1 | data file | Run score, rounded from `RunSummary.Score` |
| e.Date | int64 | UTC Unix seconds | data file | Save time; new covers use `max(now, latestDate + 1)` (monotonic if the clock goes back) |
| e.CoverId | string | GUID, ordinal compare | data file | Final deterministic tiebreak |
| e.Pinned | bool | true / false | data file | Pinned entries are never candidates |
| AlbumCap | int | 1–100 | constant (per platform) | See F2 |

**Output Range:** one victim, or none (all pinned, or N loses). Runs only when the album holds
`AlbumCap` covers. Deletion order: PNG first, then entry; on load, entries with a missing PNG are dropped
and orphan PNGs are swept.
**Example:** AlbumCap 24, full, 3 pinned; weakest unpinned A(1200, Jun 3), B(1200, Jun 1),
C(1500, May 2). New N(1200, Oct 1) → victim B (lowest score, then oldest); N beats B on date → B evicted,
N saved. N(1100, Oct 1) loses to B → not saved.

**F2. Storage budget**

The storage budget formula is defined as:

`slotBytes = AlbumCap × CoverBytes + 3 × ProfileBytes` (current + `.bak` + one `.v<n>.bak`)
`totalBytes = 3 × slotBytes + DeviceBytes + CorruptWorstCase`

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| AlbumCap | int | 12–48 | constant | **24 desktop / Steam Deck, 16 WebGL** (Andy, 2026-10-01) |
| CoverBytes | int | 0.4–1.0 MB | constant (estimate) | One 512×640 PNG; budget 0.6 MB; re-measure with real covers |
| ProfileBytes | int | 5–40 KB | constant (estimate) | Endgame JSON; budget 40 KB |
| DeviceBytes | int | 2–5 KB | constant | Settings + `.bak` |
| CorruptWorstCase | int | ≤ 0.5 MB | calculated | From F4 |

**Output Range:** desktop ≈ 44 MB (≈ 56 % headroom under an assumed 100 MB Steam Auto-Cloud quota);
WebGL ≈ 29 MB (≈ 42 % under a 50 MB IndexedDB target); ≈ 85 files total (confirm Auto-Cloud file-count
limits in Steamworks). A cloud-synced profile with more covers than the local cap is **never trimmed on
load**; it shrinks only through normal eviction. **Acceptance:** measured total with a full album of real
covers ≤ 44 MB desktop / ≤ 29 MB WebGL.
**Example:** desktop 3 × (24 × 0.6 MB + 3 × 40 KB) ≈ 43.6 MB + device + corrupt ≈ 44 MB.

**F3. Checksum**

The checksum formula is defined as:

`checksum = CRC32(UTF8(payloadBytes))`, stored as 8 lowercase hex characters.

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| payloadBytes | bytes | any | calculated | Payload serialized with fixed field order, no whitespace, invariant culture, sets sorted ordinally, written as one contiguous block |
| checksum | string | 8 hex | calculated | Stored in the `{schema, checksum, payload}` envelope |

**Output Range:** 0 to 2³²−1; false-pass ≈ 1 in 4.3 billion. Corruption detection, not anti-cheat.
On load the CRC is recomputed over the payload **exactly as read from disk** (not re-serialized);
mismatch or parse failure → `.bak` fallback.
**Example:** the expected value for a fixed reference payload is pinned in a unit test.

**F4. Retention**

The retention rule is defined as:

`keepCorrupt = min(count, 3)` per slot (and for `device.json`); `keepVersionBak = 1` per slot (newest).

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| count | int | ≥ 0 | calculated | `.corrupt-*` files present for that file |
| CorruptKeep | int | 1–10 | constant | 3 |

**Output Range:** ≤ 6 extra files per slot (3 corrupt, 1 version backup, 1 `.bak`, 1 `.tmp`). Cleanup
runs **only after a successful save**, never at load (read-only slots stay untouched); orphan `.tmp`
files from a crash are deleted at load; `.corrupt-*` is excluded from Steam Cloud.
**Example:** a fourth corruption of slot 2 deletes the oldest `.corrupt-*` after the reset save succeeds.

**F5. Write rate**

The write-rate bound is defined as:

`writesPerMin ≤ 60 / Debounce` (desktop); `≤ 60 / BrowserSyncInterval` (WebGL)

**Variables:**
| Variable | Type | Range | Source | Description |
|----------|------|-------|--------|-------------|
| Debounce | float | 1–5 s | constant | 2 s quiet period for pin / unpin / rename / settings edits |
| BrowserSyncInterval | float | 3–10 s | constant | At most one IndexedDB sync per 5 s |

**Output Range:** ≤ 30 writes/min desktop, ≤ 12 WebGL. **Immediate (no debounce):** run complete,
purchase, slot create / switch / delete / reset, and quit / tab-hidden / focus-loss flushes; leaving the
Album, Results or Profiles screen also flushes a pending debounce. A flush with nothing pending is a no-op.
**Example:** 10 pin toggles within 6 s → one write at t ≈ 8 s holding the final state.

**Defaults for previously undefined values:** `DisplayName` ≤ 24 chars, `Headline` ≤ 60 chars; all
timestamps UTC; a failed write retries once after 1 s, then the slot returns to Dirty with a warning and
waits for the next checkpoint; at run complete the cover PNG is written **before** `profile.json`. The PNG is **encoded during the
Results transition**, outside the run-complete checkpoint's main-thread budget (WebGL is single-threaded,
so a synchronous 512×640 encode cannot fit inside it).

## Edge Cases

> `systems-designer` not consulted for this section — Lean mode. Review manually before production.

**Writes and crashes**
- **If the app dies after the temp write but before the swap**: on load the orphan `.tmp` is deleted and the
  current file loads; at most the in-flight checkpoint is lost.
- **If the app dies between "current → `.bak`" and "temp → current"**: current is missing; load promotes
  `.tmp` if its checksum passes, else loads `.bak`.
- **If a write fails (disk full, permissions)**: retry once after 1 s, then show "Couldn't save — progress
  kept, will retry"; the slot stays Dirty; `.bak` is never deleted.
- **If the cover PNG write fails at run complete**: the profile still saves without that album entry; the
  run's dollars and scores bank normally.
- **If a schema migration throws partway**: the pre-migration file opens **ReadOnly**, the error is logged,
  nothing is written; `.v<n>.bak` is kept.

**Boot & slots**
- **If `LastSlot` is Corrupt at boot**: boot to the Profiles menu with that slot marked Corrupt; never
  auto-reset; other slots remain playable.
- **If `LastSlot` is Empty or was deleted**: boot into the first Ready slot; if none, create slot 1.
- **If the player deletes the active slot**: switch to the next Ready slot, or show "Create profile" on the
  title screen if none remain.
- **If two game instances run at once (desktop)**: a per-slot lock file makes the second instance open that
  slot **ReadOnly** with a notice. A lock whose owning process is no longer running (crash) is reclaimed.
- **If Steam Cloud delivers a profile with more covers than the local `AlbumCap`**: keep all of them (F2);
  never trim on load.

**Device file & legacy import**
- **If `device.json` is corrupt**: default settings; `LastSlot` = 1. The 0.x import writes
  `max(slot1SprintBest, legacyBest)`, so losing `LegacyImported` with the device file makes re-import
  harmless (idempotent).
- **If slot 1's Sprint best already exceeds the legacy PlayerPrefs value**: keep the higher value.

**Economy**
- **If a purchase costs more than the balance**: rejected; nothing changes; no write.
- **If a purchase succeeds but its save fails**: purchase and deduction stay together in memory (Dirty) and
  retry at the next checkpoint; a crash before that loses both together, never one.
- **If Storm Dollars would overflow**: `StormDollars` is int32 clamped at its max; `LifetimeStormDollars`
  is int64.

**Browser (WebGL)**
- **If IndexedDB is unavailable (private browsing, blocked or cleared storage)**: play on an in-memory
  profile with a title banner: "Progress won't be saved in this browser mode."
- **If browser storage is cleared between sessions**: treated as a first boot; the legacy import re-runs
  harmlessly (max rule).

**Player input**
- **If a rename is empty or whitespace**: rejected. **If over 24 characters**: truncated. Names are displayed
  as plain text, never interpreted as markup.
- **If the player quits mid-run**: the run is forfeited and nothing from it is banked; earlier checkpoints
  are already saved.
- **If a run completes while the slot is ReadOnly**: the run plays normally; Results shows "Progress not
  saved (this save is from a newer version)."
- **If the system clock moves backward**: cover dates stay monotonic (F1); play time accumulates whole
  seconds of focused session time (paused-in-menu counts; unfocused or minimized does not), never clock
  differences.

## Dependencies

**Depends on (upstream):**

| Dependency | Type | Interface |
|------------|------|-----------|
| Platform storage — `Application.persistentDataPath` (desktop filesystem; IndexedDB via Emscripten on WebGL) | Hard | File write / rename / delete / list. WebGL sync behaviour is an open ADR question |
| Steam Auto-Cloud | Soft | Syncs slot folders, excluding `.corrupt-*`, `.tmp`, and lock files. Fully functional offline |
| `economy-progression.md` (unlock catalogue) | Soft | Defines unlock IDs and `perk.photojournalist`; this system stores IDs and never interprets their effects |

**Depended on by (downstream):**

| Dependent | Type | Needs from this system | Back-link |
|-----------|------|------------------------|-----------|
| Run Manager & Session Modes (`session-modes.md`) | Hard | Mode unlocks, best scores, `LastLoadout`; calls the run-complete checkpoint | Added 2026-10-01 |
| Storm Dollars & HQ Garage (`economy-progression.md`) | Hard | Balance, unlocks, purchase transaction | Updated 2026-10-01 |
| Cataclysm Heat | Hard | `ModeUnlocks.heat`, `HighestHeatCleared` | No GDD yet |
| Vehicle Loadout & Archetypes | Soft | Unlocks, `LastLoadout` | No GDD yet |
| Run Screens (Newspaper Cover, Results) | Soft | Album entries and images | No GDD yet |
| Settings (#28) | Hard | Device file (Settings owns values; Save owns the file) | No GDD yet |
| Onboarding | Soft | `OnboardingFlags` | No GDD yet |
| Online Leaderboard (#35) | Soft | Best scores (read-only) | No GDD yet |
| HUD / Title | Soft | Best score, balance, `DisplayName` | No GDD yet |

**Code impact:** replaces `BestScoreStore` (the 0.x PlayerPrefs wrapper); its `Load`/`TrySave` callers in
`RunManager` and `RunScreens` move to the profile API.

## Visual/Audio Requirements

- **Save indicator:** a small newsprint stamp ("FILED") in the bottom-right corner, shown for 1 s after a
  successful run-complete or purchase save. Never shown in-run (nothing saves in-run). Debounced writes
  (pins, settings) show nothing.
- **Failure:** the "Couldn't save" toast uses the warning colour and the existing UI error cue; no sound on
  success beyond the purchase/Results sounds those screens already play.
- **Slot badges:** Corrupt (torn-page icon) and ReadOnly (padlock) on the Profiles menu, readable without
  colour alone.
- **Album thumbnails** use the saved PNG downscaled in UI; no extra art per cover.

## Game Feel

Saving should be invisible. Every checkpoint lands on a natural pause (Results, Garage, menus), so a save
never blocks input or drops a frame during driving (AC-39). The only times the player thinks about saves
are the rules they need to know: quitting mid-run forfeits the run, and a full pinned album stops new
covers. Failures read as calm and recoverable ("progress kept, will retry"), never as data loss, because
the backup makes that true.

## UI Requirements

Built in UI Toolkit; every screen is fully navigable with a gamepad (Steam Deck).

- **Profiles menu** (title screen): 3 slot cards showing `DisplayName`, Storm Dollars, best Sprint score,
  `HighestHeatCleared`, last played (local time), and the newest cover thumbnail. Empty cards show
  "New profile". Corrupt cards offer only "Reset slot"; ReadOnly cards offer only "Play (not saved)".
  Actions: switch, new, rename, reset, delete; reset and delete use two confirmations, the second naming
  the slot ("Delete *Andy* — this can't be undone").
- **Rename:** text field, 24-char limit enforced while typing, plain text only; uses the Steam on-screen
  keyboard on Deck.
- **Album screen:** grid of covers, newest first, with a counter "n / AlbumCap"; pin toggle and delete
  (one confirmation) per cover. Leaving the screen flushes pending changes (F5).
- **Pause menu:** "Quit run" states "Nothing from this run is banked."
- **Messages** (strings fixed in Core Rules and Edge Cases): "Couldn't save — progress kept, will retry"
  (toast); "Album full: unpin a cover to keep new front pages." and "Progress not saved (this save is from
  a newer version)." (Results); "Progress won't be saved in this browser mode." (title banner); the
  two-instance ReadOnly notice (Profiles menu).

## Cross-References

| Referenced doc | What this GDD relies on | Used in |
|----------------|-------------------------|---------|
| `economy-progression.md` | Storm Dollars (earning formula stays there), garage unlock catalogue, `perk.photojournalist` | Overview; Core Rules 2, 10; F1 |
| `session-modes.md` | Sprint / Chase modes; run-complete is the only run save | Core Rules 2, 4; Interactions |
| `vehicle-feel.md` / `vision-1.0.md` archetypes | Vehicle unlock IDs (`vehicle.pickup`, …) in `LastLoadout` | Core Rule 2 |
| `game-concept.md` | Pillar 5 (Roguelite Identity & Cataclysm Heat), Heat 0–5 | Header; Core Rule 2 |
| `systems-index.md` | Layer/priority (#27); downstream systems without GDDs | Quick reference; Dependencies |
| Code: `BestScoreStore` / PlayerPrefs key `Doomsday.Sprint.BestScore` | 0.x import source | Core Rule 8; Edge Cases |

## Acceptance Criteria

> `qa-lead` consulted 2026-10-01 (lean mode). Unit tests run against an in-memory `IProfileStorage` fake
> with an injected clock and a fault injector that can throw at any write/rename step. Test class:
> `SaveProfileTests`. Tags: **[Unit]** EditMode · **[PlayMode]** · **[Disk]** real `persistentDataPath`
> (Windows + Steam Deck) · **[WebGL]** browser build · **[Manual]**.

**File protocol**
- **AC-01 [Unit] (R1)** GIVEN an empty store, WHEN first boot completes, THEN exactly `device.json` and
  `slot1/profile.json` exist; slots 2–3 have no profile.
- **AC-02 [Unit] (R5)** GIVEN slot 1 Ready at version A, WHEN a purchase saves version B, THEN
  `profile.json` holds B, `profile.json.bak` holds A, and no `.tmp` remains.
- **AC-03 [Unit] (F3)** GIVEN the pinned reference payload, WHEN serialized, THEN the checksum equals the
  pinned 8-char lowercase hex; serializing the same profile twice (sets inserted in different order,
  thread culture de-DE) is byte-identical.
- **AC-04 [Unit] (R6)** GIVEN `profile.json` with one payload byte flipped, WHEN the slot loads, THEN the
  `.bak` contents load and the state is Ready.
- **AC-05 [Unit] (R6)** GIVEN current and `.bak` both fail the checksum, WHEN the slot loads, THEN the state
  is Corrupt and no write touches the slot; after the player confirms Reset, both damaged files exist as
  `.corrupt-<timestamp>` and a fresh profile is saved.
- **AC-06 [Unit] (crash between renames)** GIVEN the save is killed after "current → `.bak`", WHEN the slot
  reloads, THEN a `.tmp` with a valid checksum is promoted and B loads; with an invalid `.tmp`, A loads
  from `.bak`.
- **AC-07 [Unit] (crash before swap)** GIVEN an orphan `.tmp` beside a valid current file, WHEN the slot
  loads, THEN the current file loads and the `.tmp` is deleted.
- **AC-08 [Unit] (F4)** GIVEN slot 2 holds 3 `.corrupt-*` files, WHEN a 4th corruption is reset and the reset
  save succeeds, THEN 3 remain and the oldest is gone; a load alone never deletes one.
- **AC-09 [Unit] (write failure)** GIVEN every write throws, WHEN a checkpoint fires, THEN exactly 2 attempts
  are made 1 s apart, the state is Dirty, "Couldn't save — progress kept, will retry" is raised, and `.bak`
  is untouched.
- **AC-10 [Disk]** AC-02, AC-06 and AC-07 pass against the real filesystem on Windows and Steam Deck
  (rename atomicity is a filesystem property the fake cannot prove).

**Load / Migration**
- **AC-11 [Unit] (R7)** GIVEN a v1 file and a test-only v1→v2 migration registered, WHEN the slot loads,
  THEN `.v1.bak` holds the original bytes, the v2 profile is on disk before load returns, and only the
  newest `.v<n>.bak` is kept.
- **AC-12 [Unit] (R7)** GIVEN `SchemaVersion` = current + 1, WHEN it loads and a run completes, THEN the
  state is ReadOnly, zero writes reach the slot folder, and Results shows "Progress not saved (this save is
  from a newer version)."
- **AC-13 [Unit]** GIVEN a migration that throws partway, WHEN the slot loads, THEN the state is ReadOnly,
  the error is logged, and the file bytes are unchanged.

**Slots**
- **AC-14 [Unit] (R9)** GIVEN `LastSlot` = 2 and slot 2 Ready, WHEN the game boots, THEN slot 2 is active
  with no menu shown.
- **AC-15 [Unit]** GIVEN `LastSlot` is Corrupt, WHEN the game boots, THEN the Profiles menu opens with that
  slot marked Corrupt and the other slots selectable.
- **AC-16 [PlayMode] (R9)** GIVEN a run in progress, WHEN a slot switch is requested, THEN it is rejected;
  reset and delete each require 2 confirmations.
- **AC-17 [Unit]** GIVEN rename input `"   "`, THEN it is rejected; a 30-char name is stored as its first 24
  chars; `<b>x</b>` displays literally.
- **AC-18 [Disk]** GIVEN instance A holds slot 1, WHEN instance B opens slot 1, THEN B is ReadOnly with a
  notice and A's saves still succeed; GIVEN a lock left by a dead process, WHEN the game boots, THEN the
  lock is reclaimed and the slot opens Ready.

**Economy**
- **AC-19 [Unit] (R3)** GIVEN balance 100 and cost 150, WHEN the player buys, THEN balance, unlocks and
  write count are unchanged.
- **AC-20 [Unit] (atomicity)** GIVEN balance 200, cost 150 and every write failing, WHEN the player buys,
  THEN memory holds 50 + the unlock (Dirty) and disk holds 200 with no unlock; never one without the other.
- **AC-21 [Unit] (R2)** GIVEN a profile containing unknown unlock `vehicle.removed`, WHEN saved and
  reloaded, THEN the ID is still present.
- **AC-22 [Unit]** GIVEN `StormDollars` = int.MaxValue − 10, WHEN 100 are banked, THEN it equals
  int.MaxValue and `LifetimeStormDollars` grows by 100.

**Album**
- **AC-23 [Unit] (R10, F1)** GIVEN the F1 example (24 full, 3 pinned, A 1200/Jun 3, B 1200/Jun 1,
  C 1500/May 2), WHEN N(1200, Oct 1) arrives, THEN B is evicted (PNG, then entry) and N is saved;
  N(1100) is not saved.
- **AC-24 [Unit]** GIVEN 24 pinned covers, WHEN a run completes, THEN no cover is saved and Results shows
  "Album full: unpin a cover to keep new front pages."
- **AC-25 [Unit]** GIVEN no `perk.photojournalist`, WHEN a run completes, THEN no PNG is written and no
  entry added.
- **AC-26 [Unit] (F1 date)** GIVEN the newest cover dated T and the clock at T − 3600, WHEN a cover saves,
  THEN its Date = T + 1.
- **AC-27 [Unit] (F2)** GIVEN a cloud profile with 30 covers and AlbumCap 24, WHEN it loads, THEN all 30
  are kept; the next qualifying cover evicts one, leaving 30.
- **AC-28 [Unit]** GIVEN an entry whose PNG is missing and an orphan PNG, WHEN the slot loads, THEN both
  are removed.
- **AC-29 [Unit]** GIVEN the PNG write throws, WHEN a run completes, THEN dollars and scores are saved and
  the album is unchanged.

**Device & legacy**
- **AC-30 [Unit] (R11)** GIVEN corrupt `device.json` and corrupt `.bak`, WHEN the game boots, THEN settings
  are default, `LastSlot` = 1, and slot files are byte-identical; with a valid `.bak`, its settings load.
- **AC-31 [Unit] (R8)** GIVEN no `device.json` and PlayerPrefs `Doomsday.Sprint.BestScore` = 5000, WHEN the
  game boots, THEN slot 1 Sprint best = 5000, `LegacyImported` = true, and the key still exists; a second
  boot changes nothing.
- **AC-32 [Unit] (idempotence)** GIVEN slot 1 Sprint best 7000, legacy 5000, device file deleted, WHEN the
  game boots, THEN the best stays 7000.

**Platform & write rate**
- **AC-33 [Unit] (F5)** GIVEN the clock fake, WHEN 10 pin toggles occur within 6 s, THEN exactly 1 write
  occurs at t ≈ 8 s holding the final state; leaving the Album screen at t = 3 s writes immediately instead.
- **AC-34 [Unit] (R4)** GIVEN a Ready slot, WHEN quit / focus-loss fires, THEN 0 writes; GIVEN Dirty,
  THEN 1 write; quitting mid-run banks nothing.
- **AC-35 [WebGL] (F5)** GIVEN 20 checkpoints in 10 s, WHEN IndexedDB syncs are counted, THEN no 5 s window
  has more than 1, and the final state survives a tab reload. *(Provisional until the save ADR fixes sync
  semantics — Open Questions.)*
- **AC-36 [WebGL]** GIVEN a Firefox private window, WHEN the game boots, THEN play works and the title shows
  "Progress won't be saved in this browser mode."
- **AC-37 [Manual] (Auto-Cloud)** GIVEN two PCs on one Steam account, WHEN PC1 completes a run, THEN PC2
  loads the new best; no `.corrupt-*`, `.tmp` or lock file appears in Steam's remote file list.
- **AC-38 [Manual] (F2)** GIVEN all 3 slots with full albums of real covers, WHEN storage is measured, THEN
  total ≤ 44 MB desktop / ≤ 29 MB WebGL.

**Performance**
- **AC-39 [PlayMode] (perf)** GIVEN an endgame profile (40 KB) and a pre-encoded cover, WHEN the run-complete
  checkpoint fires 20 times, measured on a `Save.Checkpoint` profiler marker, THEN main-thread time is
  ≤ 2 ms median / ≤ 4 ms max on the desktop reference machine, ≤ 4 ms median on Steam Deck, ≤ 8 ms max on
  WebGL; no frame at the checkpoint exceeds 16.6 ms (PC) / 33.3 ms (Deck). PNG encode is excluded (it runs
  during the Results transition) and is measured separately: no Results-transition frame exceeds
  33.3 ms on WebGL.

## Open Questions

| Question | Owner | Resolve by |
|----------|-------|-----------|
| WebGL IndexedDB sync: debounced or throttled to `BrowserSyncInterval`, and does tab-hidden force a sync? (Core Rule 5, AC-35) | Save ADR (technical-director) | Before implementation |
| Steam Auto-Cloud quota and file-count limit for ≈ 85 files (F2, AC-37) | release-manager (Steamworks config) | Before Steam page setup |
| Online Leaderboard identity: does it need a stable player ID in the profile, or only the Steam ID? | Leaderboard GDD (#35) | When #35 is designed |
| AC-39 timing: approve the Unity Performance Testing package, or keep it a manual Profiler capture | Andy (technical-preferences.md) | Before the save story |
| CoverBytes / ProfileBytes are estimates; re-measure with real covers (AC-38) | qa-lead | First playable album |
