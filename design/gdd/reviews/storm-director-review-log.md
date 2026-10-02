# Review Log — Storm Director

## Review — 2026-10-02 — Verdict: NEEDS REVISION
Scope signal: L (compact mode); XL once Epic integrates (ADR-0004 streaming, fog-exempt Render Graph pass)
Specialists: none (lean mode)
Blocking items: 6 | Recommended: 8
Summary: Strong design; all F3 scale, F12 lift/toss and F1 worked-example values reproduce exactly. Blockers were integration gaps rather than design flaws: Cataclysm Heat ranks predated the director and contradicted it, compact mode (the first ship target) was never pinned, and two internal numbers drifted from their own ACs. All six were resolved in the same session (Andy's decisions 2026-10-02); the revised doc is In Review pending a fresh re-review.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Dependencies / economy-progression: Heat rank 1 (+40% speed, suction pull) and rank 5 (two high-threat at all times) contradict Rule 3, no-homing and speed caps → ranks rewritten (k_H 1.25; EF4 co-anchor in Sequence/Outbreak)
- [BLOCKING] Core Rule 12 / F2: compact mode session length, regime draw, satellite distances undefined → mini-Epic, T = 180 s, satellites 40–90 m, cap 2
- [BLOCKING] F2: Quiet satellites (EF ≤ 2) could exceed an EF0/EF1 anchor, contradicting Rule 3 and AC-6 → EF ≤ min(2, EF_a)
- [BLOCKING] F4: close-range ETA error was ±7.5% (1.5 × 0.05), failing AC-16 for ~32% of cells → factor (0.033 + 0.267·g)
- [BLOCKING] F1: Chaos per-cell EF table incomplete; one-EF5 enforcement unspecified → full table, later EF5 draws become EF4
- [BLOCKING] Rule 4 / F3: intensity I curve, Rope-out lift/damage and I = 0 division undefined → linear I, lift and damage radius scale with I in Rope-out, guard at I < 0.01
- [RECOMMENDED] AC-5: targets 4.2% / 13% vs exact 4.39% / 13.85%; Heat 5 sat 0.14 pp from the tolerance edge → re-centred
- [RECOMMENDED] F3: "core can't be out-driven" was imprecise (EF3 wind at D is 18.9 m/s; F11 saturates at 1.2 g) → restated
- [RECOMMENDED] Rule 4: reachability measured from P0 overclaimed "always reachable" → reworded
- [RECOMMENDED] F2: cap delay vs T − 30 clamp → anchors never wait
- [RECOMMENDED] Tuning Knobs: section missing (advisory at standard) → added, 17 knobs with safe ranges
- [RECOMMENDED] Dependencies: missing back-links in economy-progression, photo-scoring, save-profile; stale Open Question → added / cleared
- [RECOMMENDED] F4: b_i unbounded → clamped ±3
- [RECOMMENDED] F5: overlapping jogs unspecified → 4.5 s refractory
Reviewed-Content-Hash: design/gdd/storm-director.md 4be37f389cf75abac41bee6d92f884eed3a96628
Reviewed-Content-Hash: design/registry/entities.yaml 381af056949156ddc2b8e5da749bc8799e4db329
