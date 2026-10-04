# Epic: Run Goals v1

> **Layer**: Feature
> **GDD**: design/gdd/event-system.md, part "# Run Goals (v1)" (In Review 2026-10-04: `/design-review` NEEDS REVISION,
> 3 blockers fixed same day)
> **Architecture Module**: `Scripts/Goals` (new): pure C# goal catalogue, seeded bounty draw and goal tracker, plus a
> thin MonoBehaviour that listens to existing `GameEvents`. Accomplishments record in a new minimal `Scripts/Save`
> slice built to `save-profile.md`'s rules. HUD bounty list and career pop-up in `Scripts/UI`; the `livery.ktvr`
> material is Codex's lane. No `architecture.md` exists yet; module mapped from the current code layout.
> **Status**: Ready, scheduled in Sprint 8 (re-scope, task RG-2)
> **Stories**: 8 stories (see table)

## Overview

Every run carries a fixed list of 10 career goals for the mode and map (Tony Hawk's Pro Skater) and 3 KTVR
bounties drawn from the run's seed (Crazy Taxi). Completions pay in-run score bonuses, are written to a per-mode
accomplishments record, and 5 of 10 career goals unlock the KTVR News paint job: one real unlock that proves the
roguelite "goals get you things" loop inside the vertical slice (Andy, 2026-10-04). Goals are tracked only from
events the game already raises (photos, style moves, tosses, storm-cell lifecycle, run end), so the tracker
holds no references to gameplay systems. This epic is also the first code to persist anything, so it builds the
first slice of Save & Profile. The main risks are achievability (`big_air` may be unreachable on the flat
compact map; measured first) and browser save storage (see below).

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0005: Raycast vehicle architecture | Source of the drift, airtime (E2: counted above 1.8 m), near-miss and toss events the goals read | LOW |

**Persistence (Andy, 2026-10-04):** M1 builds a Save & Profile **stem**, not `save-profile.md`. **M1 stem** (Andy, 2026-10-04): one profile file (accomplishments record, unlocks, `LastLoadout.livery`, best score, imported once from the PlayerPrefs `BestScoreStore`) and one device file (Settings), each written tmp + rename, `SchemaVersion` 0 (**disposable**: the full build may reset them; patch notes say so). Stable string IDs for goals and unlocks still apply. Gated by a ~1 h WebGL IndexedDB sync check; if it fails, M1 persistence is session-only. Nothing else from `save-profile.md` is in M1. No ADR is needed for the stem; the WebGL sync check (story 005) is the gate. The full Save & Profile design, review and ADR come after M1.

## GDD Requirements

No `tr-registry.yaml` exists yet; requirement IDs are local to this epic and cite the GDD section.

| ID | Requirement | ADR Coverage |
|----|-------------|--------------|
| RG-R01 | Goal catalogue with stable IDs (`career.<mode>.<map>.<goal>`, `bounty.<mode>.<name>`), types Score / Style / Storm (Rule 1) | N/A (GDD) |
| RG-R02 | Fixed career list of 10 for compact / heartland with the threshold table (Rule 2, Formulas) | N/A |
| RG-R03 | 3 bounties drawn from the eligible v1 pool on `DirectorRng` stream 3; same seed, same bounties; EF-free text (Rule 3, Formulas) | N/A |
| RG-R04 | In-run bonuses once per goal per run; score tiers pay none (Rule 4, Formulas) | N/A |
| RG-R05 | Accomplishments record per mode: first completion (seed, date, version) + count (Rule 5) | Gap: WebGL save ADR |
| RG-R06 | `livery.ktvr` granted at 5 of 10 career first completions; livery choice in `LastLoadout` (Rule 6) | Gap: WebGL save ADR |
| RG-R07 | `GoalCompleted` / `BountyFailed` events; consumers read only these (Rule 7) | N/A |
| RG-R08 | HUD: bounty list ticked live, career pop-up (Rule 8; `design/ux/run-screens.md`) | N/A |
| RG-R09 | Persistence at run complete as one transaction; quit forfeits; write failure keeps progress and retries; read-only records nothing (Rule 9, Edge Cases) | Gap: WebGL save ADR |
| RG-R10 | Achievability: every career goal completed once in a real run before release; `big_air` measured first (Acceptance Criteria) | N/A |

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | [Measure counted airtime and set the big-air threshold](story-001-airtime-measurement.md) | Integration | Ready | ADR-0005 |
| 002 | [Goal catalogue and seeded bounty draw](story-002-catalogue-bounty-draw.md) | Logic | Ready | N/A |
| 003 | [Goal evaluation, bonuses and goal events](story-003-goal-evaluation.md) | Logic | Ready | N/A |
| 004 | [Goal tracker in a live run](story-004-tracker-in-play.md) | Integration | Ready | ADR-0005 |
| 005 | [Accomplishments record and first save slice](story-005-accomplishments-save-slice.md) | Integration | Ready (WebGL sync check first) | N/A (M1 stem) |
| 006 | [KTVR livery reward](story-006-livery-reward.md) | Integration | Ready (after 005) | N/A |
| 007 | [HUD bounty list and career-goal pop-up](story-007-hud-bounties-popup.md) | UI | Ready | N/A |
| 008 | [Achievability check before release](story-008-achievability-check.md) | Config/Data | Ready | N/A |

Order: 001 → 002 → 003 → 004 → (005 → 006) and 007 → 008.

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria in the Run Goals part of `design/gdd/event-system.md` are verified
- All Logic and Integration stories have passing test files in `unity/StormChaserLoop3D/Assets/Tests/`
- All UI stories have retained screenshots in `production/qa/evidence/`

## Next Step

Run `/create-stories run-goals-v1` to break this epic into implementable stories.
