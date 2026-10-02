# Review Log: Disaster Entity Framework

## Review — 2026-10-01 — Verdict: NEEDS REVISION
Scope signal: M
Specialists: none (lean mode)
Blocking items: 3 | Recommended: 5
Summary: Faithful reverse-doc; F1, F5 and F6 arithmetic checks out against the code and examples. The blockers were sequencing and testability: the Storm Director's claim on the spawner timer was undecided, ThreatMultiplier was never tabulated, and several ACs were not deterministic as written. Andy decided the director owns F1, F2 and F3, and the revisions for all three blockers plus Player Fantasy, Tuning Knobs, the zero-radius contact edge case (AC-15) and the F4 bound were applied the same day; the receipt hashes below are of the text as reviewed, before those revisions, so the next /design-review runs in full.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Dependencies / F1-F3 / AC-12 to AC-14: Storm Director boundary undecided; spawner generalization risked rework [fixed: director owns F1 to F3]
- [BLOCKING] Core Rule 2 / F5: ThreatMultiplier per EF never tabulated; EF5 2.0x wind scale unverifiable; no registry entries for this system [fixed: table added; registry entries still open]
- [BLOCKING] Acceptance Criteria: AC-07 needs an injectable RNG; AC-02/AC-03 EditMode tags do not fit OnEnable-driven Active [fixed]
- [RECOMMENDED] Edge Cases / Rule 5: DamageRadius 0 still matches at distance exactly 0 and masks later disasters [fixed: AC-15, Target]
- [RECOMMENDED] F4: "wind unbounded" misleading; bounded by MaxConcurrent, clamp owned by vehicle-feel F11 [fixed]
- [RECOMMENDED] Player Fantasy / Tuning Knobs: absent; danger spread EF0 to EF5 is only 1.6x [fixed: added, Player Fantasy is a draft for Andy]
- [RECOMMENDED] Concept alignment: no hook for Disaster Alchemy (combination) in the base contract [open: Open Questions]
- [RECOMMENDED] Dependencies: back-links missing in vehicle-damage.md and event-system.md; systems-index dependency list stale; no GDDs for Tiled World Streaming, Game Events Bus, Tornado, Wildfire, Hailstorm, Civilians [open: Follow-Up Work]
Reviewed-Content-Hash: design/gdd/disaster-entity-framework.md 346e698de1787326ac23723ab2f6c275cabfbf01
Reviewed-Content-Hash: design/registry/entities.yaml 601848c62b5b06056323baeb5ed0f5b8d0639def
