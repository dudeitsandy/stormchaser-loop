# Story 007: Forecast model and HUD forecast panel (F4)

> **Epic**: Storm Director (compact mode)
> **Status**: Done (2026-10-04; WebGL seed 554, 0 console errors)
> **Layer**: Core
> **Type**: UI
> **Estimate**: 3 h (M)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-006`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: N/A — UI Toolkit HUD per technical preferences
**ADR Decision Summary**: N/A
**ADR Version**: N/A

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

The deliberately uncertain forecast: each cell shows a bearing, an EF estimate that can be off by ±1 far away and is exact inside 150 m, a distance and "peak in ~N s". The compact panel lists the nearest 3 plus the anchor (and the Heat 5 co-anchor), then "+N more".

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-14: for every EF, bias in [−3, 3] and d ≤ 150 m the shown EF equals the true EF; for any d, |shown − true| ≤ 1
- [ ] AC-15: 1,000 evaluations at a fixed d are identical; d oscillating ±5 m across a flip boundary never changes the shown EF (10 m hysteresis); 10,000 EF2 cells at 800 m are shown wrong 47.5 % ±1.5 pp
- [ ] AC-16: ETA ≥ 0 and a multiple of 5 s; inside 150 m its error ≤ 5 % of t_true + 2.5 s; never more than 45 % of t_true + 2.5 s
- [ ] Forecast panel (GDD UI Requirements): rows for the nearest 3 plus anchor/co-anchor (highlighted), "~EF3" while uncertain and plain inside 150 m, "+N more"; screenshot retained

---

## Implementation Notes

- F4: `σ(d) = 0.7 · clamp01((d − 150)/650)`; `EF_shown = clamp(round(EF + clamp(b_i · σ, −1, 1)), 0, 5)`; ETA factor `1 + clamp(b_t, ±1.5) · (0.033 + 0.267 g)`, rounded to 5 s; b_i and b_t seeded per cell.
- HUD in `HudController` (Claude lane), sharing the off-screen indicator's bearing logic; gamepad-readable and legible on Steam Deck.

**Performance**: HUD rows update ≤ 5 Hz; no per-frame allocation (cache labels, StringBuilder for text)

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- AC-24 Epic-scale cases (5–6 live cells): Epic-mode epic

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: UI
**Required evidence**: UI: retained screenshot of each screen touched in `production/qa/evidence/` (+ unit tests for the model) — Unit `Assets/Tests/Director/ForecastTests.cs` + screenshot `production/qa/evidence/forecast-panel.png`

**Status**: [x] Created and passing (`Tests/Director/ForecastTests.cs`, 8 cases); screenshots `production/qa/evidence/forecast-panel.png` (EF1 inside 150 m, exact) and `forecast-panel-ef5.png` (EF2 + highlighted EF5 anchor, ON GROUND ETAs, TORNADO EMERGENCY crawl)

---

## Dependencies

- Depends on: Story 006 DONE
- Unlocks: None
