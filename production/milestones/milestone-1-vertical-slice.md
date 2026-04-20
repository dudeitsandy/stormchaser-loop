# Milestone 1 — Vertical Slice

**Target Date:** 2026-07-31 (estimated ~16 weeks at part-time pace)
**Status:** In Progress

## Goal

A fully playable 90-second Stormchaser Loop session running in Unity 6.3 LTS URP
with the complete 3D visual target: low-poly world, pixel post-process filter,
CRT post-FX stack, PiP photography mechanic, and gamepad support.

Must match or exceed v0.3.0 Phaser gameplay feel. This is the internal milestone
that validates the 3D pivot before investing in Steam integration.

## Success Criteria

- [ ] 90-second session playable start to finish in Unity
- [ ] All post-FX active: bloom (Kawase), CRT scanlines, film grain, chromatic aberration, teal-orange LUT
- [ ] Player vehicle drives with physics-based feel matching Phaser v0.3.0
- [ ] Tornado: EF0-EF5 lifecycle, cone mesh + GPU particles, correct movement behavior
- [ ] PiP photography system: hold L1/camera key → right stick/mouse to aim → shoot
- [ ] Photo quality scoring matches Phaser formula within 5% for equivalent skill
- [ ] Procedural terrain: road + fields + at least 3 biomes, correct speed modifiers
- [ ] HUD: speed, time, score, combo, health (UI Toolkit)
- [ ] Minimap functional
- [ ] TV weather alert system functional
- [ ] Gamepad fully supported (PS5 DualSense + Xbox compatible)
- [ ] 60 FPS on GTX 1060 equivalent with all post-FX enabled
- [ ] Full scene flow: Title → Game → GameOver → Results

## Phases (from ADR-0001)

| Phase | Description | Sprint Target |
|-------|-------------|---------------|
| 1 | Unity project setup + URP post-FX pipeline | Sprint 1 |
| 2 | Core systems port (vehicle, tornado, scoring) | Sprint 2–3 |
| 3 | 3D environment (terrain, road, sky) | Sprint 3–4 |
| 4 | Photography system (PiP camera, aim mechanic) | Sprint 4–5 |
| 5 | UI rebuild (HUD, minimap, alerts, results) | Sprint 5–6 |
| 6 | Vertical slice integration + polish | Sprint 7–8 |

## Notes

- Phaser v0.3.0 remains live on itch.io throughout this milestone
- No Steam integration work during this milestone
- Low-poly art style reduces 3D asset production cost
- Procedural generation carries over from 2D (logic ports, mesh generation rebuilds)
