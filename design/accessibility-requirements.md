# Accessibility Requirements: Doomsday

> **Status**: Committed (tier chosen by Andy, 2026-10-04)
> **Author**: Andy + Claude
> **Last Updated**: 2026-10-04
> **Accessibility Tier Target**: Basic
> **Platform(s)**: PC (WebGL on itch, Windows; Steam / Steam Deck later)
> **External Standards Targeted**: WCAG 2.1 Level AA contrast as a guide for menu text only; no platform
> certification yet
> **Accessibility Consultant**: None engaged
> **Linked Documents**: `design/gdd/systems-index.md` (#37 Accessibility, Full Vision), `design/ux/run-screens.md`

> **Scope of this document:** M1 is a vertical-slice proof of concept, so this records the tier and what it
> means for this game today. It is a stem, not the full template: the feature matrix, test plan and audit
> history get filled in when Accessibility (#37) is designed after M1. Per-screen notes stay in the UX specs.

---

## This Project's Commitment

**Target Tier: Basic.** Critical text is readable at standard resolution. No feature depends on telling colors
apart alone. Volume is adjustable per audio channel. The game can be played without photosensitivity risk.

**Rationale:** Doomsday is a solo-developed vertical slice whose core verbs (driving, aiming the camcorder)
are already motor- and timing-heavy, so the cheapest wins come from design constraints rather than new
systems. Basic is the tier that `/ux-review` and `/gate-check` can hold the run screens and HUD to now, without
adding systems during the M1 design freeze. Standard (input remapping, text scaling, colorblind modes,
subtitles) is the target to revisit before Early Access, when Settings and Accessibility (#28, #37) are
designed in full. The controller-plus-keyboard support that already exists covers the most common motor need.

**Explicitly out of scope for M1** (also in Known Intentional Limitations):
- Input remapping, text-size scaling, colorblind modes, reduced-motion mode: Standard tier, after M1.

---

## Basic tier, applied to this game

| Commitment | What it means here | Status |
|---|---|---|
| Readable critical text | HUD (HP, boost, timer, score, film, forecast panel, bounty list), news crawl and menus are readable at 1080p and in the WebGL canvas default size. Menu body text meets 4.5:1 contrast (WCAG AA) against its background | To audit (run-screens build, RS-2) |
| No color-only information | Every color signal has a text, shape or position backup. See the audit below | To audit |
| Independent volume | Separate volume for music, effects and the master channel (`run-screens.md` Settings). No voice-over exists yet; when the KTVR DJ gets a voice (post-M1 radio system), it needs its own slider | In spec (run-screens Settings) |
| No photosensitivity risk | No full-screen flashing above 3 flashes per second. Applies to the EF5 TORNADO EMERGENCY presentation, the camera shutter flash, impact sparks and, later, lightning (`production/backlog.md`) | To audit |
| Pause anywhere | A run can be paused at any time (`run-screens.md` Pause) | In spec |
| Brightness control | A brightness slider in `run-screens.md` Settings (Andy, 2026-10-04), saved with the other settings in the save stem | Decided; in RS-2 |
| Audio-only gameplay information has a visual backup | Sirens and broadcast tones (X7-10, X8-01) are backed by the news crawl and the forecast panel | Met by design |

---

## Color-as-only-indicator audit

Candidates to check during the RS-2 build. Each needs a non-color backup that survives the three common
colorblind types (check screenshots with a simulator such as Coblis).

| Location | Color signal | What it communicates | Non-color backup | Status |
|---|---|---|---|---|
| Forecast panel | Amber row | The run's main storm (anchor) | (check: label or icon?) | To audit |
| News crawl | Red TORNADO EMERGENCY | Violent storm forming | The words themselves | Likely met; confirm |
| HUD HP blocks | Block color | Truck health | Block count | Likely met; confirm |
| Bounty list | Tick / struck-through | Bounty done or failed | Tick mark, strikethrough | In spec; confirm |
| Viewfinder / photo result | Tier color (if any) | Shot quality tier | Tier text (e.g. PERFECT) | To audit |

---

## Known Intentional Limitations

| Feature | Tier required | Why not included | Risk / impact | Mitigation |
|---|---|---|---|---|
| Input remapping | Standard | New system; M1 design freeze | Players who can't use the default layout | Keyboard and controller both supported; revisit before Early Access |
| Colorblind modes | Standard | New system; M1 design freeze | Color-reliant cues | The Basic color-only audit above removes color-only signals instead |
| Text scaling | Standard | New system; M1 design freeze | Small text on handheld / Steam Deck | Basic readability audit; Steam Deck is post-M1 |
| Reduced motion | Standard | New system; M1 design freeze | Camera motion, shake and wind lean can cause discomfort | Note for the post-M1 Settings design |

---

## Open Questions

| Question | Owner | Deadline | Resolution |
|---|---|---|---|
| Add a brightness slider to `run-screens.md` Settings (Basic tier expects one), or accept the gap for M1? | Andy | Before RS-2 build | **Add it** (Andy, 2026-10-04): one more row on the Settings panel, saved in the device file |
