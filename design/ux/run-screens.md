# UX Spec: Run Screens (Title, Pause, Settings, Quit, Wrecked, Results)

> **Status**: Reviewed 2026-10-04 — NEEDS REVISION (3 blockers, 4 advisories) fixed same day; re-review NOT ASSESSED (no accessibility tier, no pattern library), 0 issues
> **Author**: Andy + Claude (ux-designer)
> **Last Updated**: 2026-10-04
> **Journey Phase(s)**: run start, mid-run interruption, run end (no player-journey map yet)
> **Platform Target**: WebGL (itch) + Windows, Steam Deck later; keyboard/mouse and full gamepad
> **Template**: UX Spec
> **Sprint**: 8, task RS-1 (M1 design-freeze exception, `milestone-1-vertical-slice.md`)

---

## Purpose & Player Need

These four screens frame a run. **Title** gets the player into the truck fast and, from M1 on, shows what
they are chasing: career progress and the next reward (`event-system.md` Run Goals). **Pause** lets them stop
safely, reach **Settings**, and quit (to title, or to the desktop on Windows), and is honest that quitting a run
banks nothing (`save-profile.md`). **Wrecked** turns failure into a moment
instead of a cut. **Results** answers "how did I do, what did I achieve, what's new": score, goals with NEW
tags, any unlock, and the seed to replay. Without results the roguelite loop has no payoff screen; with a slow
title, outside players bounce before their first storm.

**Out of scope for M1** (`save-profile.md` UI Requirements, built with the full Save & Profile system after
M1): the Profiles menu (3 slots), Rename, and the Album screen. M1 persists through a **Save & Profile stem**
(Andy, 2026-10-04: profile + device file, no ReadOnly state), so the three read-only reason lines are post-M1;
the write-failure toast and the storage banner (session-only fallback) apply. The vehicle-damage "DAMAGED" / "CRITICAL"
stage flash is HUD, not a run screen: today the HP label reads CRITICAL (`HudController`); a DAMAGED flash goes
to a future HUD spec (`/ux-design hud`).

---

## Player Context on Arrival

| Screen | When / from where | Emotional state | Voluntary? |
|---|---|---|---|
| Title | Game load (first launch), or Esc from Results | First launch: curious, no context. Returning: chasing a goal ("one more for the livery") | Yes |
| Pause | Mid-run, often mid-danger; or the browser tab / window loses focus | Wants out instantly and back in instantly | Yes (or automatic on focus loss) |
| Wrecked | HP reaches 0 | Peak adrenaline; needs a beat to register what happened, not a menu | No, sent by the game |
| Results | Timer ends, or after Wrecked | Buzzing ("did I get it?") or frustrated ("one more go"); retry must be one press | No, sent by the game |

---

## Navigation Position

A flat loop with no deep menus: **Title → Run → (Pause) → Wrecked or timer end → Results → Retry (new run) or
Title**. The Career page and the livery toggle live on Title only; **Settings** opens from Title and from Pause
(same panel). **Quit to desktop** exists on Windows only (Title: Esc, confirmed; Pause: Quit to Desktop); WebGL
hides it because a browser tab cannot be closed by the game. No screen is more than one press from a new run.

---

## Entry & Exit Points

| Entry Source | Trigger | Player carries this context |
|---|---|---|
| Game load | Boot | Profile (best score, accomplishments, unlocks, `LastLoadout` livery) |
| Results → Title | Esc | Same profile, updated by the run-complete checkpoint |
| Run → Pause | P, gamepad Start, or Esc where the browser passes it; automatic on tab hidden / window focus loss / leaving fullscreen | Frozen run state |
| Run → Wrecked | HP reaches 0 (`VehicleHealth.OnWrecked`) | Run summary so far |
| Run → Results | Timer reaches 0 | `RunSummary` (score, photos, storm info, goals) |
| Wrecked → Results | Automatic after the wreck beat | `RunSummary` with `Wrecked` |

| Exit Destination | Trigger | Notes |
|---|---|---|
| Title → Run | Any key / button **except** the Career and livery keys (C / Y, L / X) | New seed unless `?seed=N` |
| Title ↔ Career page | C / Y opens; C / Y or Esc / B closes | Overlay on Title |
| Title ↔ Settings | O / Select opens; Esc / B closes (changes apply live, saved on close) | Overlay on Title |
| Title → Desktop (Windows only) | Esc, then Esc again to confirm ("QUIT GAME? ESC AGAIN") | Profile already saved at the last checkpoint |
| Pause → Run | P / Start / Esc, or **Resume** | Unfreezes exactly where it stopped |
| Pause ↔ Settings | **Settings** | Same panel as Title; Esc / B returns to Pause, still frozen |
| Pause → Title | **Quit Run**, then confirm | **Irreversible: the run is forfeited**, nothing banked, goals not recorded |
| Pause → Desktop (Windows only) | **Quit to Desktop**, then confirm | Same forfeit as Quit Run; the app exits |
| Results → Run | Any button | New run, new seed (unless the URL pins one) |
| Results → Title | Esc | — |

---

## Layout Specification

### ASCII Wireframe

```
TITLE (built: banner, season line, KTVR radio slam lines, controls, BEST, prompt)
┌──────────────────────────────────────────────┐
│            [ DOOMSDAY key-art banner ]        │
│        STORM SEASON · PROTOTYPE 0.8.0         │
│   KTVR STORM RADIO — LIVE  "GET IN THE TRUCK!"│
│        controls (4 lines)   BEST 1,249        │
│  NEW ▸ CAREER 4/10 ▪▪▪▪▫ 1 MORE: KTVR PAINT   │
│  NEW ▸ C / Y  CAREER      PAINT ◂ KTVR ▸ L/X  │  ← paint line only once owned
│   O / SELECT SETTINGS    ESC QUIT (Windows)   │
│            PRESS ANYTHING. GO GO GO.          │  ← C, Y, L, X, O, Select, Esc don't start the run
└──────────────────────────────────────────────┘

CAREER PAGE (overlay on Title; C / Y or Esc / B closes)
┌──────────────────────────────────────────────┐
│  HEARTLAND CAREER            4 / 10           │
│  DONE  ROOKIE SCORE 750        —  PRO 1,500   │
│  DONE  BIG AIR                 —  EF4 AT PEAK │
│  ... 10 rows, two columns ...                 │
│  REWARD  ▪▪▪▪▫  5 GOALS: KTVR PAINT JOB        │
└──────────────────────────────────────────────┘

IN-RUN HUD (additions only)
 TRUCK ▪▪▪▪▪▪                              2:29
 BOOST ━━━━━                                  0
 KTVR WANTS                              FILM 23
  · GET THE ROPE-OUT                 forecast ↓
  ✕ SHOOT THE NEXT WARNED STORM  (struck = failed)
  ✓ DRIFT PAST A TWISTER  +250
        GOAL! BIG AIR +150      ← career pop-up, upper centre, 2 s
                                  (style pops stay at 44 % height)
 ═══ KTVR crawl (bottom) ═══

PAUSE (world frozen and dimmed; vertical menu, d-pad / W S / mouse to move, A / Enter / click to pick)
          PAUSED
      ▸ RESUME
        SETTINGS
        QUIT RUN
        QUIT TO DESKTOP        ← Windows only
   this run's bounties + goals done so far (dim, below the menu)
 → QUIT RUN / QUIT TO DESKTOP opens a confirm:
       QUIT RUN? NOTHING FROM THIS RUN IS BANKED.
       ▸ KEEP PLAYING     QUIT          (default focus: KEEP PLAYING)

SETTINGS (overlay; same panel from Title and Pause; changes apply live)
          SETTINGS
      ▸ CAMERA         ◂ CLASSIC ▸      SKY · CLASSIC · HIGH  (CAB later)
        SENSITIVITY    ◂ ━━━━○━━ ▸
        INVERT Y       ◂ OFF ▸
        MASTER VOLUME  ◂ ━━━━━○━ ▸
        EFFECTS        ◂ ━━━━━○━ ▸
        MUSIC          ◂ ━━━○━━━ ▸
        FULLSCREEN     ◂ ON ▸
        RESOLUTION     ◂ 1920×1080 ▸    ← Windows only
        BACK                            (Esc / B also backs out; saved on close)

WRECKED (1.5 s at 0.3× time; HUD fades; truck keeps tumbling)
            W R E C K E D        ← slams in, red, then cut to results

RESULTS (two columns)
┌───────────────────────┬──────────────────────┐
│ SESSION OVER/WRECKED  │ GOALS                │
│        1,650          │ ✓ BIG AIR   +150  NEW│
│ BEST 1,249 → NEW BEST │ ✓ PRO SCORE       NEW│
│ PHOTOS 9 BEST SHOT 400│ KTVR ✓ ROPE-OUT  +200│  ← results use short names (≤ 16)
│ WEATHER  LONE GIANT   │      ✕ WARNED STORM  │
│ THE BIG ONE GOT AWAY  │ CAREER 5/10          │
├───────────────────────┴──────────────────────┤
│  UNLOCKED: KTVR PAINT JOB (title: L / X)      │ ← only when earned
│  SEED 554 · v0.8.0 · REPLAY WITH ?seed=554    │
│  ANY BUTTON RETRY      ESC TITLE              │
└──────────────────────────────────────────────┘
```

### Layout Zones

**Target sizes:** 960×600 (itch WebGL embed, the smallest), 1280×800 (Steam Deck), 1920×1080. Below 1000 px
wide, Results stacks into one column (goals under the score block) and the Title career strip drops the
reward-name text to "1 MORE". Panels are anchored to a 16 px safe margin.

| Screen | Zones |
|---|---|
| Title | Banner (top), identity + radio (upper middle), controls + best (middle), **career strip** (lower middle, new), prompt (bottom) |
| Career page | Full-screen overlay: header with count, two-column goal list, reward progress (bottom) |
| HUD | Left column: truck HP, boost, **KTVR WANTS bounty list** (new). Right column unchanged (timer, score, film, forecast). Upper centre: **career pop-up** (new, above Codex's style pops at 44 % height). Bottom: KTVR crawl |
| Pause | Centred vertical menu over the dimmed, frozen world; run goals dim underneath |
| Settings | Centred panel (label column left, value column right); same panel from Title and Pause |
| Wrecked | Centred slam over the slowed world |
| Results | Two columns (score/stats/weather left, goals right), full-width footer (unlock, seed, prompt) |

### Component Inventory

| Component | Screen | Built? |
|---|---|---|
| Key-art banner, season line, KTVR radio slam lines, controls lines, BEST, prompt | Title | Built (0.7.x) |
| Career strip (count, reward progress, next reward) | Title | New |
| Career key hint, livery toggle (only once `livery.ktvr` is owned) | Title | New |
| Career page (10 goals, done or not, reward progress) | Title overlay | New |
| KTVR WANTS bounty list (3 rows: open · done ✓ + bonus · failed ✕ struck) | HUD | New |
| Career-goal pop-up ("GOAL! BIG AIR +150", "NEW" on first ever) | HUD | New |
| Pause menu (Resume, Settings, Quit Run, Quit to Desktop on Windows, confirm, this run's goals) | Pause | New |
| Settings panel (camera preset, sensitivity, invert Y, master / effects / music volume, fullscreen, resolution on Windows) | Title + Pause overlay | New |
| Quit-game confirm ("QUIT GAME? ESC AGAIN") | Title, Windows only | New |
| WRECKED slam + slow-mo | Wrecked | New (today: results header only) |
| Score, best / NEW BEST, photos, best shot | Results | Built |
| WEATHER, THE BIG ONE GOT AWAY, seed/version line, version-mismatch line | Results | Built (story 009) |
| Goals column (goals + bounties this run, bonuses, NEW, career count) | Results | New |
| Unlock banner | Results | New |
| Save-write toast "COULDN'T SAVE — PROGRESS KEPT, WILL RETRY" (`save-profile.md` string) | Results / any checkpoint | New |
| Read-only reason line (one of the three `save-profile.md` "Progress not saved (...)" strings) | Results | New |
| Storage banner "PROGRESS WON'T BE SAVED IN THIS BROWSER MODE." (`save-profile.md` string) | Title | New |

Glyph note: the HUD font lacks ★ (found in story 008) and may lack ✓ / ✕ / ▪ / ◂ ▸. Fallbacks: "DONE",
"MISS", "#", "<" ">". Verify in WebGL before relying on any glyph.

### Information Hierarchy

1. **Results:** the score, then goals (NEW tags pop in amber), then the unlock banner when earned, then the
   retry prompt. Weather, seed and version are dim, for players who want them.
2. **Title:** the prompt and banner, then career progress ("1 MORE: KTVR PAINT"), then controls.
3. **HUD goals:** the bounty list is secondary to driving: dim when open, bright for 2 s on a change. The
   career pop-up is the only goal element that interrupts the eye, and only for 2 s.
4. **Pause:** Resume first (default focus), then Settings, then the quits; the forfeit warning only appears on
   the confirm step, whose default focus is KEEP PLAYING so a mashed button never quits.

---

## States & Variants

| State / Variant | Trigger | What Changes |
|---|---|---|
| Title, profile loading | Boot until the profile load completes (async on WebGL / IndexedDB) | Career strip and BEST line hidden (no placeholder text); start, Settings and quit work; Career page and paint toggle wait for the load |
| Title, default | Profile loaded | Career strip shows count, reward progress and the next reward; BEST line |
| Title, first launch | Empty profile | "CAREER 0/10 · 5 GOALS UNLOCK KTVR PAINT"; no BEST line; no paint toggle |
| Title, livery owned | `livery.ktvr` in unlocks | Paint toggle line appears (STOCK / KTVR) |
| Title, storage unavailable | Browser storage blocked / ReadOnly profile | Banner "PROGRESS WON'T BE SAVED IN THIS BROWSER MODE." (`save-profile.md` string, uppercased); play is unaffected |
| Title, WebGL | Browser build | No Esc-to-quit hint or behaviour |
| Career page, complete | 10 / 10 | Header "CAREER COMPLETE"; reward line shows the unlock as earned |
| HUD, no bounties | Legacy spawner (`?spawner=legacy`) or no plan | KTVR WANTS block hidden |
| HUD, fewer bounties | Fewer than 3 eligible | Block shows only the drawn rows |
| Pause, automatic | Tab hidden, focus lost, fullscreen left, or controller disconnected | Same menu as a manual pause; never auto-resumes, Resume needs a press |
| Pause, quit confirm | Quit Run / Quit to Desktop picked | Confirm prompt replaces the menu; default focus KEEP PLAYING |
| Settings, WebGL | Browser build | Resolution row hidden; fullscreen row uses the browser fullscreen API |
| Wrecked | HP reaches 0 | 1.5 s slow-mo beat, then Results with the WRECKED header |
| Results, timer end | Timer reaches 0 | Header "SESSION OVER" |
| Results, wrecked | After Wrecked | Header "WRECKED" |
| Results, no goals | No goal or bounty completed | Goals column: "NO GOALS THIS RUN" + career count |
| Results, first-ever completion | `FirstEver` | NEW tag (amber) on that row |
| Results, unlock earned | Reward threshold crossed this run | Unlock banner in the footer |
| Results, save write failed | Run-complete checkpoint write failed after its one retry | Toast "COULDN'T SAVE — PROGRESS KEPT, WILL RETRY"; NEW tags and the unlock banner still show (progress is kept in memory and retried, `save-profile.md`) |
| Results, profile read-only | Slot is ReadOnly (NewerSchema / MigrationFailed / InstanceLock) | The matching reason line replaces the NEW tags: "PROGRESS NOT SAVED (THIS SAVE IS FROM A NEWER VERSION)." · "PROGRESS NOT SAVED (THIS SAVE COULDN'T BE UPDATED)." · "PROGRESS NOT SAVED (THIS PROFILE IS OPEN IN ANOTHER WINDOW)." Unlock banner hidden (nothing banked) |
| Results, legacy / no plan | No storm info | Storm block hidden (built, story 009) |

---

## Interaction Map

Mapping interactions for keyboard/mouse and full gamepad (Platform Target). Touch is not supported.

| Component | Keyboard | Mouse | Gamepad | Feedback | Outcome |
|---|---|---|---|---|---|
| Title: start run | Any key except C, O, L, Esc | Click | Any button except Y, Select, X | Prompt flash, engine rev | Run starts |
| Title: Career page | C | — | Y | Page fade-in | Career overlay open / closed |
| Title: Settings | O (Options; S is brake) | — | Select (View) | Panel fade-in | Settings overlay |
| Title: paint toggle (owned only) | L | Click the arrows | X | Truck on title swaps paint, tick sound | `LastLoadout.livery` saved |
| Title: quit (Windows) | Esc, Esc again | — | — | "QUIT GAME? ESC AGAIN" | App exits |
| Pause open / Resume | P or Esc | — | Start | Instant freeze, 0.15 s dim | Paused / resumed |
| Menu move | W / S, ↑ / ↓ | Hover | D-pad, left stick | Focus marker ▸ moves, tick sound | — |
| Menu pick | Enter / Space | Click | A | Clunk sound | Item action |
| Menu back | Esc | — | B | — | Previous panel (Settings → Pause / Title; confirm → menu) |
| Settings value | A / D, ← / → | Click ◂ ▸ | D-pad / stick left-right | Value changes live, tick | Applied live; saved when the panel closes |
| Quit confirm | Enter on QUIT | Click QUIT | A on QUIT | — | Forfeit; Title or desktop |
| Results: retry | Any key except Esc (after 1.0 s) | Click | Any button except B (after 1.0 s) | Prompt flash | New run |
| Results: title | Esc | — | B | — | Title |

**Input lock:** Results ignores input for its first **1.0 s** so a shutter mashed at the buzzer cannot skip the
goals tally. Menu sounds (move tick, pick clunk) are a Codex presentation request.

---

## Data Requirements

| Data | Source System | Read / Write | Notes |
|---|---|---|---|
| Score, best, photos, best shot, wrecked flag | `RunSummary` (`RunManager`) | Read | Built |
| Weather, seed, version, got-away EF, version mismatch | `StormRunInfo` (`StormDirector.RunInfo`) | Read | Built (story 009) |
| This run's bounties, their state | Goal tracker (`event-system.md` Run Goals) | Read | Via `GoalCompleted` / `BountyFailed` and the drawn list |
| Goals completed this run, bonus, FirstEver | Goal tracker | Read | `GoalCompleted` |
| Career count, per-goal done, reward progress | Accomplishments record (`save-profile.md`) | Read | Title strip and Career page |
| Unlocks (livery owned) | `save-profile.md` `Unlocks` | Read | |
| Chosen livery | `save-profile.md` `LastLoadout.livery` | **Write** | Persistent write on toggle |
| Settings values | `save-profile.md` device settings file | **Write** | Persistent write on panel close |
| Profile loaded, slot state (ReadOnly + reason), last write failed | Save & Profile | Read | Drives the loading, storage-banner, write-toast and read-only states |
| Paused | `RunManager` (owns pause, time scale, audio pause) | Read | **The UI never sets time scale itself** |
| Platform (Windows vs WebGL) | `Application.platform` | Read | Hides quit-to-desktop and resolution on WebGL |
| Time left | `SessionTimer` | Read | Freezes while paused |

Architecture note: pause, forfeit and quit are `RunManager` state, not UI state. The panels only request them.

---

## Events Fired

| Player Action | Event Fired | Payload / Data |
|---|---|---|
| Pause / Resume (manual or automatic) | `GameEvents.PauseChanged` (new) | `bool paused`, reason (Manual / FocusLost / ControllerLost) |
| Quit Run confirmed | `GameEvents.RunForfeited` (new) | none; **no run-complete checkpoint** |
| Quit to Desktop confirmed | `RunForfeited` if in a run, then `Application.Quit()` | — |
| Settings changed | none per change (applied live) | — |
| Settings panel closed | Settings saved (**persistent write**) | Full settings record |
| Paint toggled | `LastLoadout.livery` saved (**persistent write**) | Livery ID |
| Retry | `GameEvents.RunStarted` (existing) | — |
| Career page / Settings opened | none | Deliberate: no analytics in M1 |

Persistent writes (settings file, `LastLoadout`) go through Save & Profile's checkpoint rules; flag for the
architecture pass in RS-2.

---

## Transitions & Animations

| Screen / change | Enter | Exit |
|---|---|---|
| Title | Fade in 0.3 s; radio lines slam (built) | Cut to run |
| Career page / Settings | Fade in 0.15 s | Fade out 0.15 s |
| Pause | Instant freeze; dim fades in 0.15 s (unscaled time) | Dim fades out 0.15 s, then unfreeze |
| Wrecked | Time scale 0.3 for **1.5 s real time**; HUD fades out 0.3 s; "WRECKED" slams in red, scale 1.4 → 1.0 in 0.2 s | Cut to Results |
| Results | Fade in 0.25 s; goal rows pop in 0.1 s apart; NEW pulses amber once; unlock banner slides up last | Cut to run or Title |
| HUD bounty row | On change: row brightens for 2 s; done rows show the bonus; failed rows strike through | — |
| Career pop-up | Pops in, holds 2 s, fades 0.3 s | — |

Pause input is ignored during the Wrecked beat. Reduced motion: no setting in v1 (Open Questions).

---

## Input Method Completeness Checklist

**Keyboard**
- [x] Every interactive element reachable (Interaction Map)
- [x] Logical up/down order in Pause and Settings; Title uses dedicated keys
- [x] Visible focus marker (▸) on the focused item
- [x] Modal focus trap: the quit confirm and Settings keep focus until closed
- [x] Esc backs out of every overlay; on Title (Windows) it asks to quit

**Gamepad**
- [x] Every element reachable (d-pad / stick + A / B, dedicated Y / Select / X / Start)
- [x] Controller disconnect auto-pauses (States)
- [ ] Steam Deck layout check (Open Questions)

**Mouse**
- [x] Menu items and setting arrows clickable; hover moves focus
- [ ] Minimum hit-target size not yet set (Open Questions)

**Touch:** not supported (Platform Target).

---

## Accessibility

No accessibility tier is defined for the project yet (Open Questions; WCAG AA suggested as the baseline).
Built in regardless:
- Goal states never rely on colour alone: every state has a text marker (DONE / MISS / open) as well as
  colour and strike-through.
- Menu and HUD goal text at least 18 px at 1080p. **Flag:** the existing seed/version line is 14 px.
- Text sits on dark backing panels (existing HUD style) for contrast over a bright or dark sky.
- Full keyboard-only and gamepad-only paths (checklist above).
- Motion: the WRECKED slam, NEW pulse and slow-mo have no reduced-motion alternative in v1 (Open Questions).

---

## Localization Considerations

English only for M1. Layout-critical elements (HIGH PRIORITY if translated; plan for +40 % length):
- Results goals column: **short names, ≤ 16 characters**, plus bonus and NEW. Bounty short names: `point_blank_ef5`
  POINT-BLANK, `warning_cell` WARNED STORM, `before_touchdown` CAUGHT FORMING, `rope_out` ROPE-OUT, `close_call`
  CLOSE CALL, `double_near_miss` DOUBLE MISS, `drift_by` DRIFT-BY. Career short names: ROOKIE, PRO, SICK,
  EF4 AT PEAK, POINT BLANK, TOSS SURVIVOR, STORM DRIFT, BIG AIR, NEAR MISSES, FRONT PAGE.
- HUD bounty list: full text, ≤ 28 characters (longest today: "SHOOT THE NEXT WARNED STORM", 27).
- Fixed `save-profile.md` strings (toast, banner, read-only lines) are the longest messages; they wrap to two
  lines at 960 px width.
- Settings labels (left column) and the quit confirm line.
Numbers use the current culture's `N0` formatting (a German system shows "1.249"); decide in RS-2 whether
scores should be culture-invariant.

---

## Acceptance Criteria

- [ ] Pause opens within 50 ms of P / Start / Esc; the world and timer freeze; Resume continues with the same
      time left (±0.05 s)
- [ ] WebGL: switching tabs or leaving fullscreen auto-pauses; returning needs a press to resume
- [ ] Quit Run and Quit to Desktop open a confirm whose default focus is KEEP PLAYING; confirming Quit Run returns
      to Title and the career count is unchanged (nothing from the run recorded)
- [ ] Windows: Quit to Desktop and Esc-Esc on Title exit the app; WebGL: neither option is shown
- [ ] Settings: every value applies live; after a full reload all values persist; the resolution row is absent
      on WebGL
- [ ] Wrecked: slow-mo lasts 1.5 ± 0.1 s of real time, then Results with the WRECKED header; pause is ignored
      during it
- [ ] Results goals column lists each completion with its bonus, NEW on first-ever completions, the unlock banner
      when earned, and "NO GOALS THIS RUN" when none
- [ ] A failed save write (simulated) shows the toast "COULDN'T SAVE — PROGRESS KEPT, WILL RETRY" and still shows NEW tags; a ReadOnly profile
      shows its exact reason line from `save-profile.md` and no NEW tags or unlock banner
- [ ] Title during the WebGL profile load shows no career strip or BEST line, and a run can still be started
- [ ] Every screen in this spec fits with no overlap or clipped text at 960×600, 1280×800 and 1920×1080; Results is
      one column at 960×600
- [ ] Results ignore input for the first 1.0 s
- [ ] First launch: Title reads "CAREER 0/10 · 5 GOALS UNLOCK KTVR PAINT"; the paint toggle appears only once owned
- [ ] Keyboard-only, gamepad-only and mouse can each reach every menu item
- [ ] Every goal state is readable without colour (text markers present)
- [ ] Retained screenshots in `production/qa/evidence/`: Title, Career page, HUD with bounties, Pause, quit
      confirm, Settings, Wrecked, Results (with goals and unlock)

---

## Open Questions

- Player journey map not yet created. Author it from the template at
  `.claude/docs/templates/player-journey.md` to establish player context for these screens.
- Accessibility tier not yet defined: consider WCAG AA as a baseline. Run `/gate-check` to see whether this
  blocks any phase gates.
- Reduced motion: add a setting for the WRECKED slam, NEW pulse and slow-mo, or accept for M1?
- Glyphs: verify ✓ ✕ ▪ ◂ ▸ in the HUD font on WebGL; fall back to text if missing.
- ~~Music volume label~~ Resolved (Andy, 2026-10-04): keep **MUSIC**; title music is in M1 (Sprint 8 S8-09). A RADIO
  label belongs to the post-M1 KTVR Storm Radio system (`production/backlog.md`).
- Steam Deck: verify layout, text size and button prompts on the Deck (post-M1 target).
- Mouse hit-target minimum size.
- Culture-invariant score formatting (Localization).
