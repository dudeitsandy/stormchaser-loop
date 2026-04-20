# Session Modes

**Status:** Design — not yet implemented
**Target Sprint:** Sprint 6–7
**Owner:** Game Designer

---

## Overview

Stormchaser Loop has two session modes: Sprint (90 seconds, score attack) and Chase
(4–6 minutes, storm front with escalating events). Sprint is the arcade hook and
onboarding mode. Chase is the heart of the game — long enough to generate stories,
short enough to replay immediately.

---

## Player Fantasy

**Sprint:** "I am a precision photographer. I have 90 seconds and one chance to
frame the perfect shot."

**Chase:** "I am a storm chaser. I drove through a barn at 80mph with an EF4 overhead,
saved a family in a truck, and got the photo of my life with three seconds left."

---

## Detailed Rules

### Sprint Mode
- Session length: 90 seconds (fixed)
- Tornado count: 1–3 (random EF rating per spawn)
- No event windows — pure photo scoring
- Unlocked by default

### Chase Mode
- Session length: 4–6 minutes (varies by storm front size)
- Storm front moves across the map; spawns 3–6 tornados in sequence
- EF rating escalates: front starts at EF0–EF1, peaks at EF3–EF5 midway, dissipates
- Event windows generated procedurally in tornado path (see event-system.md)
- Unlocked after: first Sprint run completed

### Session End Conditions
- Timer reaches zero (normal end)
- Vehicle destroyed by debris (run ends early — partial score kept)
- Storm front fully dissipates (Chase mode only — may end before timer)

---

## Formulas

### Chase Session Length
`SessionLength = BaseDuration + (TornadoCount × 45s)`
- Base: 3 minutes
- Each tornado adds ~45 seconds of meaningful chase window
- Result: 3–6 minute runs depending on storm front

### Storm Front EF Escalation
```
Phase 1 (0–30%):   EF0–EF1  — scouting, low danger
Phase 2 (30–70%):  EF2–EF4  — peak danger, best photo opportunities
Phase 3 (70–100%): EF3–EF1  — dissipating, decreasing risk and reward
```

---

## Edge Cases

- Player ignores all tornados: session ends with zero score (valid outcome)
- Storm dissipates before timer: session ends early, score is final
- Vehicle destroyed: session ends, 80% of earned score is kept ("film survived")

---

## Dependencies

- SessionTimer.cs (S2-05) — extend to support variable duration
- TornadoSpawner.cs (S3-01) — extend to support storm front sequencing
- EventSystem (see event-system.md) — Chase mode only
- ScoreAccumulator.cs (S3-03)
- UI: mode select screen (Phase 5)

---

## Tuning Knobs

- `SprintDuration`: default 90s
- `ChaseBaseDuration`: default 180s
- `SecondsPerTornado`: default 45s
- `EFEscalationCurve`: AnimationCurve — adjustable in ScriptableObject
- `PartialScoreOnDeath`: default 0.8 (80%)

---

## Acceptance Criteria

- [ ] Sprint mode plays identically to current implementation
- [ ] Chase mode spawns multiple tornados in EF-escalating sequence
- [ ] Session length varies correctly based on storm front size
- [ ] Vehicle destruction ends session early with partial score
- [ ] Both modes accessible from mode select screen
