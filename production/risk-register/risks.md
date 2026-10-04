# Risk Register

**Last Updated:** 2026-10-04

---

## Active Risks

| ID | Risk | Probability | Impact | Status | Mitigation |
|----|------|------------|--------|--------|------------|
| R02 | PiP / viewfinder cost pushes WebGL over budget | Low | Medium | Open | X7-06 GPU subchecks pass (PiP ≈ 0.16–0.19 ms mean); certify the full 3 ms budget with a Unity CPU capture in Sprint 8; fallback PiP 30 Hz / 160×120 |
| R03 | Design runs ahead of the build: new GDDs/concepts and repeated review rounds displace implementation | High | High | Open | **Happened** in Sprint 7 (5 Storm Director review rounds, 3 unscheduled GDDs, Story/Career concept). Design freeze until M1 closes (milestone-1); playtest findings go to the sprint backlog, not new docs |
| R04 | Steam Deck can't hold 30 FPS | Medium | High | Open | Not tested yet; WebGL budget is the proxy until M2; test on Deck at M1 close |
| R06 | Art production bottleneck (solo, no artist) | Medium | Medium | Open | Headless Blender script pipeline proven (G2); final art pass deferred to polish; commission the hero truck if the script hits a ceiling |
| R07 | Funnel Runners update closes the differentiation gap | Low | High | Open | Protect the differentiators: photography + Storm Director tension + roguelite run |
| R09 | Storm Director tuning (regime weights, jog rate, forecast error) eats Sprint 8 | Medium | Medium | Open | Ship GDD placeholder values first; tune only from playtest data; tuning continues into Sprint 9 |
| R10 | Andy-time bottleneck: gates and live checks queue up (Sprint 7: G3 + 5 Codex acceptances waiting) | High | Medium | Open | Batch all Andy checks into one session per sprint; Claude verifies what it can headlessly first |
| R11 | Two agents editing the same Unity tree (Claude + Codex) cause recompile-in-play NREs or lane collisions | Medium | Medium | Open | AGENTS.md lanes; "Recompile After Finished Playing"; commit own lane only |
| R12 | WebGL physics budget under real debris (G1 condition, ≤ 4 ms) unmeasured | Medium | High | Open | Measure in Sprint 7's closing session with X7-07 props; cap debris or warm-up if it fails; 1 km fallback (ADR-0004 Alt 5) |

---

## Closed Risks

| ID | Risk | Resolution | Date Closed |
|----|------|------------|-------------|
| R01 | URP Render Graph complexity blocks post-FX | Toon shader, outline pass and lens shipped on Render Graph (Sprint 6, 0.5.0) | 2026-10-04 |
| R05 | C# / Unity learning curve slows the port | Port complete; vehicle rewrite shipped in 0.6.0 | 2026-10-04 |
| R08 | Unity 6.3 LTS URP bug blocks development | Moved to 6.6 (6000.6.0f1) in Sprint 5 without blockers; superseded by R11-style tooling risks | 2026-10-04 |
| R03-old | Scope creep stretches M1 past 16 weeks | Materialized; M1 rewritten 2026-10-04 with a new date. Ongoing pattern tracked as R03 | 2026-10-04 |
