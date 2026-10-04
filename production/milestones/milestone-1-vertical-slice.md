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

## Success Criteria

- [ ] **Storm Director compact mode** running in the shipping build: all 9 stories in
      `production/epics/storm-director-compact/` closed; EF3+ dangerous up close, EF5 rare (EF3 wind
      PlayMode test green)
- [ ] **Feel tuned from G3**: `vehicle-feel.md` feel criteria pass with Andy + 2 outside players; tuned
      defaults recorded in the GDD
- [ ] **Durability decided**: S8-C1 tuning shipped at minimum (light bumps cost 0 HP, HP cost back on);
      S8-C2 per-truck HP redesign either shipped or explicitly deferred past M1
- [ ] **Run goals v1**: THPS-style goal list in a run (style moves and storm goals score, not just photos);
      designed in `event-system.md`, reviewed, implemented
- [ ] **Run screens**: title, pause/quit and game-over/results flows designed (`/ux-design`) and built;
      results show seed, regime and "the big one got away"
- [ ] **WebGL budget holds**: no frame > 50 ms over a 3-min run with live storms, debris and presentation;
      X7-06 3 ms presentation budget certified
- [ ] **Outside playtest**: 0.9 on itch, at least 3 outside players, findings logged

## Out of scope for M1 (do not pull in)

2 km streamed Epic world, roads (S7-09), per-tile scatter (X7-05), Tier B fracture (S7-10), Wildfire,
Disaster Alchemy, Civilians & Rescue, Loadout/Garage/Storm Dollars meta loop, Story/Career mode, more
worlds/biomes, Steam integration. These are the next milestone's candidates; `systems-index.md` tiers them.
Parked ideas (biomes and generated locations, etc.) live in `production/backlog.md`.

## Design freeze

No new GDDs or concept docs until M1 closes, except the two M1 needs: the run-goals section of
`event-system.md` and the run-screens UX spec. Playtest findings go to the backlog in the active sprint file.

## Sprints

| Sprint | Window | Focus |
|--------|--------|-------|
| 7 | 2026-10-01 → G3 | Vehicle feel build (closing: G3 + Codex live checks) |
| 8 | 2026-10-06 → 10-19 | Storm Director compact epic + S8-C1 durability tuning |
| 9 | 2026-10-20 → 11-02 | Run goals v1, run screens, S8-C2 decision |
| 10 | 2026-11-03 → 11-16 | Integration, WebGL budget, outside playtest, 0.9 release |
