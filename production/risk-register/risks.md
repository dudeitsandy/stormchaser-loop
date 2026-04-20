# Risk Register

**Last Updated:** 2026-03-31

---

## Active Risks

| ID | Risk | Probability | Impact | Status | Mitigation |
|----|------|------------|--------|--------|------------|
| R01 | URP Render Graph complexity blocks post-FX implementation | Medium | High | Open | Reference `docs/engine-reference/unity/modules/rendering.md`; no Compatibility Mode in 6.3 — must use Render Graph API from day one |
| R02 | PiP camera (RenderTexture) GPU cost drops below 60 FPS | Medium | High | Open | Render PiP at 50% resolution; only active when L1 held; profile early in Sprint 4 |
| R03 | Solo dev scope creep stretches M1 past 16 weeks | High | Medium | Open | Strict sprint scope; defer cosmetic polish to post-M1; vertical slice ≠ shippable game |
| R04 | Steam Deck performance — post-FX stack can't hit 30 FPS | Medium | High | Open | Scalable quality settings; post-FX toggle in options; test on Deck at M1 completion |
| R05 | C# / Unity learning curve slows Phase 2–3 significantly | Medium | Medium | Open | Port TypeScript logic first (scoring, EF scale); keep gameplay code isolated from Unity API; use Unity docs heavily |
| R06 | Low-poly 3D art production bottleneck (solo dev, no artist) | High | Medium | Open | Procedural generation + primitive shapes for prototype; acquire asset packs for EA; defer hand-authored assets to post-EA |
| R07 | Funnel Runners ships major update that closes differentiation gap | Low | High | Open | Monitor Steam page; Stormchaser differentiator is photography mechanic + roguelike session — protect that distinction |
| R08 | Unity 6.3 LTS has breaking bug in URP that blocks development | Low | High | Open | Pin to Unity 6.3 LTS point release; monitor Unity release notes; have rollback plan to 6.2 |

---

## Closed Risks

| ID | Risk | Resolution | Date Closed |
|----|------|------------|-------------|
| — | — | — | — |
