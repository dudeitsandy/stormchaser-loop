# Milestone 2 — Steam Ready

**Target Date:** TBD (after M1 ships)
**Status:** Not Started

## Goal

Integrate Steamworks SDK, implement achievements, cloud saves, and pass Steam Deck
compatibility verification. Game is ready to submit for Early Access.

## Success Criteria

- [ ] Steamworks SDK integrated (achievements, cloud saves, overlay)
- [ ] All achievements from design doc implemented and firing correctly
- [ ] Steam Cloud saves working (run save data syncs across machines)
- [ ] Steam Deck verified (30 FPS, controller-only playable, no keyboard-required flows)
- [ ] Steam store page complete (capsule art, screenshots, trailer, description)
- [ ] Build passes Steamworks technical checklist
- [ ] **Desktop is the primary build** (Andy, 2026-10-05): Windows exe for Steam; performance budgets move from
      WebGL to desktop and Steam Deck (60 / 30 FPS). Record the switch in an ADR (amends ADR-0001's platform
      notes) before the first post-M1 sprint
- [ ] **Web build's fate decided:** keep a free WebGL demo of the compact slice on itch (gzip with decompression
      fallback, short playlist, Addressables if content grows) or retire it. Either way the itch page points
      to Steam
- [ ] WebGL-only constraints reviewed and lifted where desktop allows: no-VFX-Graph rule for Codex, single-
      thread physics budgets, IndexedDB save sync in the save stem
- [x] Itch.io Phaser version archived / sunset *(superseded: the Unity builds replaced it on itch in 0.4–0.7)*
