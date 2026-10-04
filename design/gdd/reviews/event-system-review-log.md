# Review Log — event-system.md

## Review — 2026-10-04 — Verdict: NEEDS REVISION
Scope: the "# Run Goals (v1)" part only (RG sections); the older event taxonomy was out of scope.
Scope signal: M–L
Specialists: none (lean mode)
Blocking items: 3 | Recommended: 6
Summary: Two-layer structure (fixed career goals + seeded KTVR bounties), per-mode accomplishments record and
goal-to-unlock hook are sound. Blockers: the bounty pool could not fill 3 slots, quitting contradicted
save-profile's forfeit rule, and `big_air` was likely unreachable on the compact map.
Prior verdict resolved: First review
Findings:
- [BLOCKING] RG Detailed Rules / Formulas: bounty pool had ≤ 2 implementable bounties; most runs would draw 0–1
- [BLOCKING] RG Edge Cases: quit checkpoint recorded goals; save-profile says quitting mid-run banks nothing
- [BLOCKING] RG Formulas: `big_air` 1.5 s likely impossible; counted airtime only accrues above 1.8 m (vehicle-feel E2)
- [RECOMMENDED] RG Formulas: `storm_drift` "whole slide" needs per-frame sampling; drift event fires once at slide end
- [RECOMMENDED] RG Detailed Rules: goal-complete event not defined, though ACs and presentation depend on it
- [RECOMMENDED] RG Detailed Rules: no bounty ID scheme
- [RECOMMENDED] RG Detailed Rules: timed bounty text named an EF (crawl Rule 7 keeps EFs out of text)
- [RECOMMENDED] RG Detailed Rules: livery toggle state not persisted
- [RECOMMENDED] Section 3: "12 units" should be metres
Revisions applied the same day (Andy chose "revise now"; quit forfeits goals): 7-bounty v1 pool on existing
events; quit forfeits; `big_air` on counted airtime with a provisional 0.5 s and a measure-first AC;
`storm_drift` evaluated at slide end; `GoalCompleted` / `BountyFailed` defined; `bounty.<mode>.<name>` IDs;
EF-free bounty text; livery in `LastLoadout`; metres. Status: In Review (Andy accepted revisions pending a
re-review).
Post-revision content hash (NOT yet reviewed, so not a receipt): design/gdd/event-system.md 5e5d6e4d7838dfbf47142fca5d9ed259bd497721
