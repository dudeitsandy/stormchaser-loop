# Sprint 1 — 2026-03-31 to 2026-04-14

## Sprint Goal
Unity 6.3 LTS project created, URP configured with Render Graph, and the full
post-FX visual target (CRT scanlines, bloom, film grain, CA, teal-orange grade)
verified running at 60 FPS on a placeholder scene. The engine foundation is
ready for gameplay work in Sprint 2.

## Capacity
- Sprint length: 2 weeks
- Hours/week: 8–10
- Total hours: 16–20
- Buffer (20%): 3–4 hours reserved for Unity install issues, unfamiliar APIs, etc.
- **Available: ~13–16 hours**

---

## Tasks

### Must Have (Critical Path)

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S1-01 | Install Unity 6.3 LTS via Unity Hub, create project with URP 3D template | 1h | — | ✅ DONE |
| S1-02 | Configure Git for Unity project (.gitignore for Unity, Git LFS for assets) | 1h | S1-01 | ✅ DONE |
| S1-03 | Verify URP Render Graph is active (no Compatibility Mode) | 0.5h | S1-01 | ✅ DONE — Forward+ confirmed, Compatibility Mode deprecated/removed |
| S1-04 | Implement URP Full Screen Renderer Feature: CRT scanlines shader | 2h | S1-03 | ✅ DONE — Fullscreen Shader Graph + Full Screen Pass Renderer Feature |
| S1-05 | Implement post-FX Volume stack: Bloom (Kawase), Film Grain, Chromatic Aberration, Vignette | 2h | S1-03 | ✅ DONE — Global Volume configured, ACES tonemapping, all overrides active |
| S1-06 | Implement teal-orange LUT color grading | 1.5h | S1-05 | ✅ DONE — NeutralLUT generated in Blender, graded to teal-orange, imported with sRGB off, applied via Color Lookup override |
| S1-07 | Build placeholder verification scene: flat plane + directional light + cube "truck" | 1h | S1-04, S1-05 | ✅ DONE — VerificationScene with plane + cube, full post-FX stack active in Play mode |
| S1-08 | Performance baseline: confirm 60 FPS with full post-FX stack on target hardware | 0.5h | S1-07 | ✅ DONE — Frame time under 16.6ms confirmed via Unity Profiler |

**Must Have total: ~9.5 hours**

### Should Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S1-09 | Install and configure New Input System package; map PS5 DualSense + Xbox gamepad | 2h | S1-01 | ✅ DONE — Input System v1.18.0 already in template; StormChaserControls asset created with Move (left stick), AimCamera (right stick), AimHold (L1); C# class generated |
| S1-10 | Create project folder structure matching `docs/directory-structure.md` (`Assets/Scripts/`, `Assets/Scenes/`, `Assets/Prefabs/`, etc.) | 0.5h | S1-01 | ✅ DONE — Scripts, Scenes, Prefabs, Materials, Textures, Audio, Animations folders created |

**Should Have total: ~2.5 hours**

### Nice to Have

| ID | Task | Est. Hours | Dependencies | Acceptance Criteria |
|----|------|-----------|--------------|---------------------|
| S1-11 | Screenshot the post-FX result and save to `docs/visual-targets/sprint-1-postfx.png` as reference | 0.5h | S1-07 | ✅ DONE — Screenshot saved to docs/visual-targets/ |
| S1-12 | Research URP RenderTexture setup for PiP camera (no implementation — read docs, note approach) | 1h | S1-03 | ✅ DONE — ADR-0002 created: second Base Camera → RenderTexture → UI RawImage approach documented |

**Nice to Have total: ~1.5 hours**

---

## Carryover from Previous Sprint
None — first sprint.

---

## Risks This Sprint

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| Unity 6.3 install / licensing issues eat first session | Medium | Low | Have Unity Hub ready before sprint starts; Unity Personal is free under $200k revenue |
| CRT scanlines require writing a custom HLSL shader (unfamiliar) | Medium | Medium | Reference `docs/engine-reference/unity/modules/rendering.md`; use WebSearch for URP 6.3 Renderer Feature examples |
| Render Graph API unfamiliar for custom Renderer Feature | Medium | High | R01 from risk register — reference URP docs before writing any rendering code |

---

## Dependencies on External Factors
- Unity Hub download + Unity 6.3 LTS install (one-time, ~30 min)
- Unity Personal license activation (free, requires Unity account)

---

## Definition of Done for Sprint 1
- [x] S1-01 through S1-08 all complete (Must Have)
- [x] Full post-FX stack running at 60 FPS confirmed via Profiler
- [ ] Unity project committed to Git with clean history
- [ ] No unresolved console errors in Play mode
- [ ] Sprint 2 scope drafted based on what was learned
