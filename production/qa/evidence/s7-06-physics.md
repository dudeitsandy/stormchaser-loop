# S7-06 — Physics budget under real debris + vehicle (G1 condition)

**Date:** 2026-10-04 · **Build:** WebGL development build of the shipping scene (`builds/webgl-dev`), working
tree at `1f2207c` plus in-flight Storm Director story 003 code · **Browser:** Playwright Chromium, local
`http.server`, `?physProbe=1` · **Probe:** `Assets/Scripts/World/PhysicsBudgetProbe.cs`, the same method as
G1's `SpikeMetrics`: script-mode stepping and a Stopwatch around each `Physics.Simulate`.

**Pass rule (G1, `s7-01-spike-results.md`):** physics step ≤ 4 ms. One isolated first-contact spike is allowed;
a repeat is not.

## Run

A scripted ~190 s drive: full throttle, boost about every 2nd–3rd steer, alternating left/right steering,
handbrake slides and jumps across the ±85 m arena with its X7-07 knock-loose props and live tornadoes. The
run ended twice before the 180 s window finished: the scene reloaded at about 110 s and again at about
197 s, and scripted Space presses restarted it. So the result is three windows, with the last values logged
in each (from the raw `[PHYS]` lines):

| Window | Measured | Steps | physMax | physAvg | Steps > 4 ms | Vehicle impacts | Peak awake bodies |
|---|---|---|---|---|---|---|---|
| 1 | ~105 s | 4,836 | 1.10 ms | 0.252 ms | 0 | 12 | 3 |
| 2 | ~60 s | 3,001+ | 1.20 ms | 0.260 ms | 0 | 10 | 3 |
| 3 | ~15 s | 750 | 1.00 ms | 0.269 ms | 0 | 3 | 2 |

**Max physics step 1.20 ms across ~180 s of play, 0 steps over 4 ms.** The 7.3 ms one-off seen in an earlier
real-Chrome G1 attempt did not appear.

## Verdict: PASS, with a stress caveat

The budget holds for vehicle, tornado wind and lift, solid scenery and knock-loose props, at about a third
of the 4 ms budget. The caveat is debris load: scripted steering produced 25 vehicle impacts but **peaked at 3 awake bodies**.
That is far below X7-07's ≤ 40 loose-body cap and G1's 60-piece drops. G1 already measured 60-piece debris
drops at ≤ 1.5 ms in WebGL (run 2), and this run adds the vehicle and tornado load on top at ≤ 1.2 ms.
Together they cover the condition. A deliberate prop-cluster smash (Andy driving, Part B of the G3 session)
with `?physProbe=1` would close the gap. Andy accepts the caveat or asks for that run (G3 checklist C3).

## Follow-ups
- The probe now logs a `PARTIAL_PASS` / `PARTIAL_FAIL` `[PHYS-RESULT]` when a reload ends the window early
  (added after this run).
- Run in Playwright Chromium, not Andy's real Chrome. Frame time is not authoritative here, but
  physics step time is CPU-side and comparable to G1 run 2.
