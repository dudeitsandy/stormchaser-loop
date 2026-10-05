# WebGL frame time: 0.8.5, seed 554

Generated 2026-10-05T21:37:54.561Z by `tools/perf/webgl-frametime.mjs`. Observations only; budgets are applied by the reader.

## Setup

- Build: `builds/perf-snapshot/0.8.5-20261005-1510` (existing snapshot, served as is)
- URL: `http://127.0.0.1:<port>/?seed=554&physProbe=1&spikeLog=1`
- Browser: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/154.0.0.0 Safari/537.36
- GPU: Google Inc. (AMD) / ANGLE (AMD, AMD Radeon(TM) 890M Graphics (0x0000150E) Direct3D11 vs_5_0 ps_5_0, D3D11)
- Canvas: 1280x800, headless
- Machine: AMD Ryzen AI 9 HX 370 w/ Radeon 890M, 24 logical cores, 31.1 GB, Windows_NT 10.0.26300
- Unity.exe processes: 0 before, up to 1 during, 2 after
- Machine CPU busy before launch: 17%

## Frame times (requestAnimationFrame deltas)

| | Title | Run |
|---|---|---|
| Duration (s) | 20.1 | 190 |
| Frames | 1193 | 11311 |
| Mean (ms) | 16.82 | 16.8 |
| p50 (ms) | 16.7 | 16.7 |
| p95 (ms) | 17.1 | 17 |
| p99 (ms) | 32.9 | 17.3 |
| Max (ms) | 33.6 | 33.8 |
| Frames > 33.3 ms | 7 | 52 |
| Frames > 50 ms | 0 | 0 |
| Long tasks (count / total ms) | 0 / 0 | 0 / 0 |
| Machine CPU busy (mean / max %) | 29.8 / 34.9 | 30.6 / 50.9 |
| Audio sources started / max active | 0 / 5 | 44 / 8 |

Worst run frames: 33.8 ms at 85.2 s, 33.8 ms at 162.7 s, 33.7 ms at 7 s, 33.7 ms at 21.4 s, 33.7 ms at 28.9 s

## Spike log (`?spikeLog=1`)

91 `[SPIKE]` lines during the run phase; 51 of 52 run frames over 33.3 ms (reloads excluded) matched one within 100 ms. Of those, 0 had a GC and 0 had a game event; 0 lines matched no frame.

Events on matched heavy frames: none.

| At (s) | rAF (ms) | Unity (ms) | GC | Heap Δ (KB) | Events | Scene |
|---|---|---|---|---|---|---|
| 85.2 | 33.8 | 34 | 0 | 0 |  | funnels 2, audioPlaying 5, bodiesAwake 2/49 |
| 162.7 | 33.8 | 34 | 0 | 0 |  | funnels 1, audioPlaying 4, bodiesAwake 2/49 |
| 7 | 33.7 | 34 | 0 | 0 |  | funnels 0, audioPlaying 4, bodiesAwake 2/49 |
| 21.4 | 33.7 | 34 | 0 | 0 |  | funnels 1, audioPlaying 5, bodiesAwake 2/49 |
| 28.9 | 33.7 | 34 | 0 | 0 |  | funnels 1, audioPlaying 4, bodiesAwake 2/49 |
| 42.3 | 33.7 | 34 | 0 | 0 |  | funnels 1, audioPlaying 4, bodiesAwake 2/49 |
| 54.3 | 33.7 | 33 | 0 | 0 |  | funnels 1, audioPlaying 4, bodiesAwake 3/49 |
| 67.1 | 33.7 | 34 | 0 | 0 |  | funnels 1, audioPlaying 6, bodiesAwake 2/49 |
| 81.3 | 33.7 | 34 | 0 | 0 |  | funnels 2, audioPlaying 5, bodiesAwake 2/49 |
| 82.8 | 33.7 | 33 | 0 | 0 |  | funnels 2, audioPlaying 5, bodiesAwake 2/49 |
| 84.7 | 33.7 | 34 | 0 | 0 |  | funnels 2, audioPlaying 5, bodiesAwake 2/49 |
| 143.9 | 33.7 | 34 | 0 | 0 |  | funnels 2, audioPlaying 4, bodiesAwake 2/49 |
| 146.6 | 33.7 | 34 | 0 | 0 |  | funnels 1, audioPlaying 4, bodiesAwake 2/49 |
| 23.5 | 33.6 | 33 | 0 | 0 |  | funnels 1, audioPlaying 4, bodiesAwake 2/49 |
| 42.5 | 33.6 | 34 | 0 | 0 |  | funnels 1, audioPlaying 4, bodiesAwake 2/49 |

At = seconds into the run phase (page clock). Unity (ms) is the game's own frame interval for the same frame. Every slow frame is in the `.json` (`spikes.allSlowFrames`).

## Physics probe

```
[PHYS-RESULT] {"verdict":"PASS","steps":9001,"physMaxMs":2.30,"physMaxAtS":6,"physAvgMs":0.350,"stepsOverBudget":0,"impacts":12,"peakAwakeBodies":5}

```

The verdict field inside this line is printed by the game's `PhysicsBudgetProbe`, not by this tool.

## Console

205 lines, 0 errors.

## Notes

- From the screenshots (added by hand): truck upright through 90 s (beside a silo), on its side at 120, 150 and
  180 s and never auto-righted, as in the 0.8.3 report. Storms stayed in range (EF2 92 m, EF5 116 m at 120 s).
- Build: the 3:10 PM rebuild still labelled 0.8.5 (includes `FrameSpikeLog`), not the 0.8.5 behind the r1–r3 reports.
- The drive now coasts 0.75 s every 3 s, so impacts (12) and speed are lower than in earlier reports; frame counts
  are not directly comparable.

- Unity.exe was running on this machine (0 before launch, up to 1 during); another agent's editor or build may have competed for CPU/GPU.
- t=209.8s: Unity.exe process count rose to 1 during the capture.

## Files

- Raw frame deltas, long tasks and every `[PHYS]` line are in the matching `.json`.
- Screenshots (local only, gitignored): `builds/perf-captures/frametime-0.8.5-seed554-2026-10-05/`
