# Milestone 1 — Vertical Slice 0.9

**Target Date:** 2026-11-16 (Sprints 8–10)
**Status:** In Progress
**Rewritten:** 2026-10-04 (producer audit). The original M1 (target 2026-07-31) described the Phaser-port
slice: 90 s session, world-wide CRT post-FX, Phaser feel parity, cone-mesh tornado. ADR-0003, the
Rocket League vehicle target (`vehicle-feel.md`) and the Storm Director (`storm-director.md`) replaced all
four, so those criteria are retired, not failed. History is in git.

## Goal

One compact run, from title screen to results, that answers the question "is this fun and tense?" with
outside players: the Rocket League-feel truck, a Storm Director-driven sky of storms you can see coming,
photos plus style goals that score, damage that matters, and a run that ends cleanly. Built on today's
±85 m arena (compact mode). The 2 km streamed Epic world is **not** in this milestone.

**Platform (Andy, 2026-10-05):** WebGL on itch is the home of this slice: browser play is the fastest way to
get outside players into it. **After M1 the game moves to a Windows desktop build (Steam) as the primary
target**, for performance and player experience. WebGL becomes at most a free demo of the compact slice
(see `milestone-2-steam-ready.md` and risk R13).

## Success Criteria

- [x] **Storm Director compact mode** running in the shipping build: all 9 stories in
      `production/epics/storm-director-compact/` closed; EF3+ dangerous up close, EF5 rare (EF3 wind
      PlayMode test green) *(2026-10-04: 0.7.4/0.7.5, G4 passed)*
- [ ] **Feel tuned from G3**: `vehicle-feel.md` feel criteria pass with Andy + 2 outside players; tuned
      defaults recorded in the GDD
- [x] **Durability decided**: S8-C1 tuning shipped at minimum (light bumps cost 0 HP, HP cost back on);
      S8-C2 per-truck HP redesign either shipped or explicitly deferred past M1 *(S8-C1 in 0.7.5, G5 passed;
      S8-C2 **deferred past M1**, Andy 2026-10-04)*
- [ ] **Run goals v1**: THPS-style goal list in a run (style moves and storm goals score, not just photos);
      designed in `event-system.md`, reviewed, implemented
- [x] **Run screens**: title, pause/quit and game-over/results flows designed (`/ux-design`) and built;
      results show seed, regime and "the big one got away" *(results part done in 0.7.5, story 009)*
      *(Done 2026-10-05: run-screens epic stories 001–006 closed; Andy's Windows check passed: pause → Quit to
      Desktop, Title Esc-Esc, Alt-tab auto-pause.)*
- [ ] **WebGL budget holds** *(Andy 2026-10-05: screen transitions (load, title → run, run → results) are outside
      "the 3-min run". The 111 ms title → run frame turned out to be a `physProbe` run; without it StartRun costs 3–5 ms and the frame ≤ 33 ms, so no fix is needed. S9-04 on 0.8.5: p95 17.1 ms,
      physics ≤ 1.8 ms, in-play frames ≤ 50 ms; see `sprint-09-feel-and-playtest.md` S9-04)*: over a 3-min run with live storms, debris and full presentation (storm sky,
      sirens, PiP), no frame > 50 ms and p95 frame ≤ 33.3 ms on the reference laptop (Radeon 890M, Chrome),
      physics step ≤ 4 ms (`?physProbe=1`). Measured headlessly, no Profiler session needed *(reworded
      2026-10-04: X7-06's per-component 3 ms CPU breakdown is a diagnostic, run only if this fails, and
      otherwise moves to the post-M1 performance pass)*
- [ ] **Outside playtest**: 0.9 on itch, at least 3 outside players, findings logged

## Out of scope for M1 (do not pull in)

2 km streamed Epic world, roads (S7-09), per-tile scatter (X7-05), Tier B fracture (S7-10), Wildfire,
Disaster Alchemy, Civilians & Rescue, S8-C2 per-truck HP redesign (deferred 2026-10-04), Loadout/Garage/Storm Dollars meta loop
(**except** the Save & Profile M1 stem for run goals and Settings, below), Story/Career mode, more
worlds/biomes, Steam integration. These are the next milestone's candidates; `systems-index.md` tiers them.
Parked ideas (biomes and generated locations, etc.) live in `production/backlog.md`.
After M1: the plan to Early Access (milestones M2–M6, modes, descoping ladder) is `production/roadmap.md`.

**In M1 by decision (Andy, 2026-10-04):** run-goals persistence and Settings through a Save & Profile stem. **M1 stem** (Andy, 2026-10-04): one profile file (accomplishments record, unlocks, `LastLoadout.livery`, best score, imported once from the PlayerPrefs `BestScoreStore`) and one device file (Settings), each written tmp + rename, `SchemaVersion` 0 (**disposable**: the full build may reset them; patch notes say so). Stable string IDs for goals and unlocks still apply. Gated by a ~1 h WebGL IndexedDB sync check; if it fails, M1 persistence is session-only. Nothing else from `save-profile.md` is in M1.

## Design freeze

No new GDDs or concept docs until M1 closes, except the two M1 needs: the run-goals section of
`event-system.md` and the run-screens UX spec (plus the Save & Profile M1 stem they use).

**Freeze rule for new requests (Andy, 2026-10-04). Every agent applies it: Claude sessions and Codex.**
A new idea or request goes straight in only if it passes all three tests:
1. It's about 2 h of work or less.
2. It adds no new system: no new GDD, no new disaster or mode, no new meta or progression.
3. It touches no persistence (save or profile data).

Anything else gets one entry in `production/backlog.md` ("parked, not designed"), and the agent tells Andy
it was parked. Andy can override any single item explicitly. If you're unsure, park it and ask. Playtest
findings go to the active sprint file and are triaged there.

## Sprints

| Sprint | Window | Focus |
|--------|--------|-------|
| 7 | 2026-10-01 → G3 | Vehicle feel build (closing: G3 + Codex live checks) |
| 8 | 2026-10-06 → 10-19 | Storm Director compact epic + S8-C1 durability *(done early)*; re-scoped 2026-10-04 to add run goals v1 and run screens |
| 9 | **2026-10-06 → 10-19** (re-dated 2026-10-05, Andy) | Driving feel pass from outside playtest round 1, round 2 + G6 feel sign-off (`sprint-09-feel-and-playtest.md`) |
| 10 | **2026-10-20 → 11-02** (draft: `sprint-10-m1-close.md`) | Integration, WebGL budget, outside playtest, 0.9 release; M1 can close ≈ 2 weeks before the 11-16 target |
