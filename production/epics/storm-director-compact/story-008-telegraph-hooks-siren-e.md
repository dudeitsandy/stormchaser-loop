# Story 008: Telegraph hooks: siren caption and environmental-intensity query

> **Epic**: Storm Director (compact mode)
> **Status**: Ready
> **Layer**: Core
> **Type**: Integration
> **Estimate**: 2 h (S)
> **Manifest Version**: N/A (no control manifest yet; workflow: standard)
> **Last Updated**: [set by /dev-story when implementation begins]

## Context

**GDD**: `design/gdd/storm-director.md`
**Requirement**: `TR-storm-director-007`
*(Requirement IDs are local to `production/epics/storm-director-compact/EPIC.md`; there is no tr-registry yet.)*

**ADR Governing Implementation**: ADR-0003: Art direction — stylized world, retro lens
**ADR Decision Summary**: Telegraphs are toon cards / world-grade cues; camcorder treatment only in viewfinder and photos.
**ADR Version**: 2026-10-01

**Engine**: Unity 6.6 (6000.6.0f1) | **Risk**: LOW
**Engine Notes**: none — plain C# and existing Unity APIs; no post-cutoff API in this story.

**Control Manifest Rules (this layer)**: N/A (no control manifest). Project rules apply: data-driven values,
doc comments on public APIs, no `FindObjectOfType` in Update, no Legacy Input Manager.

Gives presentation what Rule 7 needs: the environmental intensity `e` from the summed per-cell wind magnitude (Codex's X7-09 computes a provisional value; this swaps it to the director query), and the siren/radio caption on a true EF3+ forming that never names an EF.

---

## Acceptance Criteria

*From GDD `design/gdd/storm-director.md`, scoped to this story:*

- [ ] AC-26 (Unit): with an EF0 5 m away and an EF5 Mature 30 m away, `e` = 1 at every relative bearing (2.72 + 20.24 m/s summed magnitudes)
- [ ] AC-26 (Unit): the siren caption for a true EF5 contains no EF number ("Tornado warning: severe cell forming, bearing NW")
- [ ] The caption appears on `StormCellForming` with true EF ≥ 3 and on the anchor's Peak; screenshot of the caption retained

---

## Implementation Notes

- `e = clamp01(Σ|W_i(player)| / 20)`; physics keeps the vector sum. Expose it on the director query surface and tell Codex in AGENTS.md to switch X7-09's provider.
- The caption is HUD (Claude lane); audio stays Codex's. Bearing is an 8-point compass from the player to the cell.

**Performance**: e is summed once per frame over live cells; 0 B GC

---

## Out of Scope

*Handled by neighbouring stories — do not implement here:*

- Codex X7-09: sky, gust, roar and siren audio presentation

---

## QA Test Cases

*N/A — no qa-lead specs at this tier (lean review mode); implement against the Acceptance Criteria above.*

---

## Test Evidence

**Story Type**: Integration
**Required evidence**: Integration: automated PlayMode test (or a documented playtest) — Unit `Assets/Tests/Director/TelegraphTests.cs` + screenshot `production/qa/evidence/siren-caption.png`

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: Story 006 DONE
- Unlocks: None
