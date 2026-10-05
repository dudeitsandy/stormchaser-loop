# WebGL frame time: 0.8.5, seed 554

Generated 2026-10-05T19:53:36.613Z by `tools/perf/webgl-frametime.mjs`. Observations only; budgets are applied by the reader.

## Setup

- Build: `builds/perf-snapshot/0.8.5` (copied from `builds/webgl`)
- URL: `http://127.0.0.1:<port>/?seed=554&physProbe=1`
- Browser: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/154.0.0.0 Safari/537.36
- GPU: Google Inc. (AMD) / ANGLE (AMD, AMD Radeon(TM) 890M Graphics (0x0000150E) Direct3D11 vs_5_0 ps_5_0, D3D11)
- Canvas: 1280x800, headless
- Machine: AMD Ryzen AI 9 HX 370 w/ Radeon 890M, 24 logical cores, 31.1 GB, Windows_NT 10.0.26300
- Unity.exe processes: 0 before, up to 0 during, 0 after
- Machine CPU busy before launch: 9.1%

## Frame times (requestAnimationFrame deltas)

| | Title | Run |
|---|---|---|
| Duration (s) | 20 | 204.9 |
| Frames | 1180 | 11865 |
| Mean (ms) | 16.96 | 17.27 |
| p50 (ms) | 16.7 | 16.7 |
| p95 (ms) | 17 | 17.1 |
| p99 (ms) | 33.4 | 33.4 |
| Max (ms) | 33.6 | 333.8 |
| Frames > 33.3 ms | 12 | 177 |
| Frames > 50 ms | 0 | 1 |
| Long tasks (count / total ms) | 0 / 0 | 2 / 486 |
| Machine CPU busy (mean / max %) | 10.9 / 12.1 | 12.7 / 24.3 |
| Audio sources started / max active | 0 / 5 | 144 / 11 |

Worst run frames: 333.8 ms at 176.3 s, 34 ms at 143.4 s, 33.9 ms at 81.4 s, 33.8 ms at 13.7 s, 33.8 ms at 26.9 s

## Physics probe

```
[PHYS-RESULT] {"verdict":"PARTIAL_PASS","steps":8404,"physMaxMs":1.60,"physMaxAtS":11,"physAvgMs":0.190,"stepsOverBudget":0,"impacts":37,"peakAwakeBodies":2}

```

The verdict field inside this line is printed by the game's `PhysicsBudgetProbe`, not by this tool.

## Console

102 lines, 0 errors.

## Notes

- t=203.1s: Physics probe reported early (PARTIAL): the scene was destroyed mid-window, e.g. wreck or retry.
- t=203.1s: Physics probe started a second time (scene reloaded, new run).

## Files

- Raw frame deltas, long tasks and every `[PHYS]` line are in the matching `.json`.
- Screenshots (local only, gitignored): `builds/perf-captures/frametime-0.8.5-seed554-2026-10-05/`
