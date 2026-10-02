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

## Review — 2026-10-02 — Verdict: NEEDS REVISION
Scope signal: L (compact mode); XL once Epic integrates
Specialists: none (lean mode)
Blocking items: 2 | Recommended: 8
Summary: All six prior blockers verified fixed; F1–F5 and every AC numeric target re-derived exactly. Both new blockers came from the first revision itself: the Heat 5 EF4 co-anchor could outrank an EF2/EF3 anchor (~64–69% of Heat 5 Sequence/Outbreak plans), and AC-24 assumed 6 live cells with an anchor, which no regime allows. All items resolved in the same session (Andy's decisions 2026-10-02); awaiting a fresh re-review.
Prior verdict resolved: Yes
Findings:
- [BLOCKING] Rule 3 / F1: Heat 5 co-anchor (EF4) could exceed the anchor → Heat 5 Seq/Outbreak anchor floored at EF4 (EF4 .91 / EF5 .09), AC-6 extended
- [BLOCKING] Acceptance Criteria / AC-24: 6 live cells only occur in Chaos, which has no anchor → split into Chaos and non-Chaos cases
- [RECOMMENDED] F2: Sequence co-anchor collided with satellite S's slot → co-anchor replaces satellite S
- [RECOMMENDED] F3 / AC-20: early rope-out from Forming jumped I to 1 and broke event order → rope-out from current I0, Peak skipped
- [RECOMMENDED] F3: rope-out lift at full R extended past the R·I wind field → F12 evaluated at R·I
- [RECOMMENDED] Tuning Knobs: k_H safe range 1.3 broke v_cap → 1.0–1.25
- [RECOMMENDED] Edge Cases: compact EF5 toss core "≈ 16 m across" → ≈ 31 m across (15.7 m radius)
- [RECOMMENDED] F2 / Rule 12: compact cap 2 collapsed Outbreak → compact Sequence S 1–3, Outbreak S 1–2
- [RECOMMENDED] AC-8: T range applied to compact plans → scoped to Epic
- [RECOMMENDED] UI Requirements: co-anchor forecast row unspecified → always shown, highlighted
Reviewed-Content-Hash: design/gdd/storm-director.md 53f370b475793e413b93a53f42f5ee82a36e079c
Reviewed-Content-Hash: design/registry/entities.yaml bc9e31499be46bbb1ff18c57deeeb74b6416bacc

## Review — 2026-10-02 — Verdict: NEEDS REVISION
Scope signal: L (compact mode); XL once Epic integrates
Specialists: none (lean mode)
Blocking items: 2 | Recommended: 4
Summary: All ten second-pass items verified fixed and every number they touched re-derived (Heat 5 floor .91/.09, EF5 4.39%/13.85% unchanged, rope-out lift at R·I, k_H ceiling, AC-24 reachability). Both new blockers were implementability gaps, not design flaws: the capped frame step made track positions frame-rate dependent (breaking seed replay), and satellite EF/count draws were only caps. All items resolved in the same session using proposed defaults; awaiting a fresh re-review, which should be scoped light unless fixes spread.
Prior verdict resolved: Yes
Findings:
- [BLOCKING] Edge Cases / AC-23: capped step dropped motion, so positions depended on frame rate and broke Rule 10 replay → fixed 0.05 s substeps; AC-23 compares 5 FPS vs 60 FPS positions
- [BLOCKING] F2: satellite EF and S/N distributions unspecified (caps only) → uniform integer counts, uniform EF over 0..cap, deterministic Sequence EF_k = max(0, EF_top − (S' − k)); AC-6b added
- [RECOMMENDED] F3: cell evicted while Forming gained damage radius in Rope-out → fails to touch down, lift and damage stay 0; visual spec + AC-10b updated
- [RECOMMENDED] F2: N_cells in T ambiguous vs later drops → counted as drawn, before drops
- [RECOMMENDED] F2 CapDelay: lowest-EF drop rule could leave a 30 s waiter waiting forever → a cell that has waited 30 s is dropped
- [RECOMMENDED] F2 CapDelay: plan-time lifetime unspecified → Form + Mature + Rope
- [NICE] Tuning Knobs: p5_0 Heat 5 added mass is 2 × p5_0, not × 3 → fixed
- [NICE] AC-24: co-anchor forecast case uncovered → case (c) added
- [NICE] F2: T − 30 clamp never binds → noted as safety net
Reviewed-Content-Hash: design/gdd/storm-director.md bf40ab6 (short form; pre-revision version reviewed this pass)
Reviewed-Content-Hash: design/registry/entities.yaml bc9e31499be46bbb1ff18c57deeeb74b6416bacc
