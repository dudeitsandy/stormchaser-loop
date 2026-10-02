# Review Log: Save & Profile

Document: `design/gdd/save-profile.md`

## Review — 2026-10-01 — Verdict: NEEDS REVISION
Scope signal: L
Specialists: none (lean mode)
Blocking items: 2 | Recommended: 8
Summary: Strong, well-quantified Foundation GDD: 6/6 required sections, 39 criteria, and the F2 arithmetic, registry entries and cross-references all check out. Two spec gaps sat in the central flow: the run-complete checkpoint contradicted itself (one write vs an asynchronous PNG encode), and ReadOnly had three causes but one exit. Revisions for both, plus items 3-6 and 10, were applied the same day and AC-40 to AC-43 were added; the receipt hashes below are of the text as reviewed, before those revisions, so the next /design-review runs in full.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Core Rule 4 / Defaults: run-complete "one write" conflicts with the PNG encoded during the Results transition; the checkpoint timing and the interrupted-flush outcome were undefined [fixed]
- [BLOCKING] States: ReadOnly had one cause and one exit in the table but three causes in Edge Cases (newer schema, failed migration, instance lock); in-memory contents of a failed migration undefined [fixed]
- [RECOMMENDED] F1: float RunSummary.Score to int conversion unspecified (rounding mode, negative, NaN, overflow) [fixed]
- [RECOMMENDED] Core Rule 5: a save after a `.bak` fallback load rotates the damaged current over the only good `.bak` [fixed]
- [RECOMMENDED] Core Rule 6 / F1: load order omits `.tmp` promotion; eviction wording "holds AlbumCap" should be "or more"; AlbumCap range differs between F1 and F2 [fixed]
- [RECOMMENDED] F5 / Core Rule 4: Settings missing from the screens that flush a pending debounce [fixed]
- [RECOMMENDED] Dependencies: RunSummary / GameEvents contract not listed as an upstream dependency [fixed]
- [RECOMMENDED] Dependencies / Edge Cases: Steam Auto-Cloud conflict behaviour when two machines advance one profile is unspecified [open: Open Questions]
- [RECOMMENDED] AC-39: "desktop reference machine" is not defined in any doc [open: Open Questions, needs Andy]
- [RECOMMENDED] Acceptance Criteria: no criteria for delete-active-slot / empty-LastSlot boot, manual cover delete, or the focused-seconds play-time rule [open]
Reviewed-Content-Hash: design/gdd/save-profile.md e30dbe6944b18209d2e2a3c8d597ea2165f40877
Reviewed-Content-Hash: design/registry/entities.yaml 601848c62b5b06056323baeb5ed0f5b8d0639def
