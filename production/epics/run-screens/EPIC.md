# Epic: Run Screens

> **Layer**: Presentation
> **GDD**: design/ux/run-screens.md (UX spec; `/ux-review` 2026-10-04: 3 blockers + 4 advisories fixed, re-review
> NOT ASSESSED for accessibility tier and pattern library only). Requirements also from `save-profile.md` UI
> Requirements and `event-system.md` Run Goals Rule 8.
> **Architecture Module**: `Scripts/Core/RunScreens.cs` and `Scripts/Core/RunManager.cs` (gains Paused state,
> forfeit and quit; owns time scale), Settings applied in `Scripts/UI` with values from Save & Profile's device
> settings file. No `architecture.md` exists yet; module mapped from the current code layout.
> **Status**: Ready, scheduled in Sprint 8 (re-scope, task RS-2)
> **Stories**: Not yet created — run `/create-stories run-screens`

## Overview

The screens that frame a run: Title (now with the career strip, career page and paint toggle), Pause (Resume,
Settings, Quit Run, Quit to Desktop on Windows), Settings v1 (camera preset, sensitivity, invert Y, volumes,
fullscreen, resolution on Windows), a 1.5 s slow-mo Wrecked beat, and a two-column Results that adds the goals
tally, unlock banner and save messages to the existing score, weather and seed block. Pause, forfeit and quit
are `RunManager` state; the UI only requests them. Every screen must fit the 960×600 itch embed. Andy asked for
a proper pause menu and a better way to quit the Windows build (2026-10-04). Results and Title goal elements
depend on the Run Goals v1 epic.

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| — | No ADR governs these screens | — |

Settings and the paint choice persist through Save & Profile, so the same WebGL save-storage gap noted in
`run-goals-v1` applies to the Settings story. Advisory at `workflow: standard`.

## GDD Requirements

Requirement IDs are local to this epic and cite the UX spec section.

| ID | Requirement | ADR Coverage |
|----|-------------|--------------|
| RS-R01 | Pause: P / Start / Esc, auto-pause on focus loss / fullscreen exit / controller disconnect, never auto-resume; RunManager owns time scale (Entry & Exit, States, Data) | N/A |
| RS-R02 | Quit Run and Quit to Desktop (Windows only) with KEEP PLAYING default confirm; quit forfeits the run; Title Esc-Esc quits on Windows (Entry & Exit) | N/A |
| RS-R03 | Settings v1 panel from Title and Pause; live apply; saved on close; resolution row Windows only (Layout, Interaction Map) | Gap: WebGL save ADR |
| RS-R04 | Wrecked beat: 0.3× time for 1.5 s real time, HUD fade, WRECKED slam; pause ignored (Transitions) | N/A |
| RS-R05 | Results: two columns (one below 1000 px wide), goals column with short names, NEW, unlock banner, empty state, save toast / read-only lines, 1.0 s input lock (Layout, States) | N/A |
| RS-R06 | Title: career strip, career page, paint toggle, profile-loading state, storage banner; O / Select settings key (Layout, States) | N/A |
| RS-R07 | All screens fit 960×600, 1280×800, 1920×1080; keyboard-only, gamepad-only and mouse reach every item; retained screenshots of every screen (Acceptance Criteria) | N/A |

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria in `design/ux/run-screens.md` are verified
- All Logic and Integration stories have passing test files in `unity/StormChaserLoop3D/Assets/Tests/`
- All UI stories have retained screenshots of each screen touched in `production/qa/evidence/`

## Next Step

Run `/create-stories run-screens` to break this epic into implementable stories.
