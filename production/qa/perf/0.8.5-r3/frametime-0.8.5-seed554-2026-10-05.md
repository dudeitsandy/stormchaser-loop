# WebGL frame time: 0.8.5, seed 554

Generated 2026-10-05T20:01:24.650Z by `tools/perf/webgl-frametime.mjs`. Observations only; budgets are applied by the reader.

## Setup

- Build: `builds/perf-snapshot/0.8.5` (copied from `builds/webgl`, reused)
- URL: `http://127.0.0.1:<port>/?seed=554&physProbe=1`
- Browser: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/154.0.0.0 Safari/537.36
- GPU: Google Inc. (AMD) / ANGLE (AMD, AMD Radeon(TM) 890M Graphics (0x0000150E) Direct3D11 vs_5_0 ps_5_0, D3D11)
- Canvas: 1280x800, headless
- Machine: AMD Ryzen AI 9 HX 370 w/ Radeon 890M, 24 logical cores, 31.1 GB, Windows_NT 10.0.26300
- Unity.exe processes: 0 before, up to 0 during, 0 after
- Machine CPU busy before launch: 5.3%

## Frame times (requestAnimationFrame deltas)

| | Title | Run |
|---|---|---|
| Duration (s) | 20 | 190 |
| Frames | 1142 | 11092 |
| Mean (ms) | 17.54 | 17.13 |
| p50 (ms) | 16.7 | 16.7 |
| p95 (ms) | 33 | 17.1 |
| p99 (ms) | 33.6 | 33.4 |
| Max (ms) | 33.8 | 50.3 |
| Frames > 33.3 ms | 31 | 144 |
| Frames > 50 ms | 0 | 3 |
| Long tasks (count / total ms) | 0 / 0 | 1 / 152 |
| Machine CPU busy (mean / max %) | 9.9 / 10.9 | 11.7 / 14.5 |
| Audio sources started / max active | 0 / 5 | 134 / 11 |

Worst run frames: 50.3 ms at 65.1 s, 50.1 ms at 90 s, 50.1 ms at 138.9 s, 49.9 ms at 57.2 s, 49.7 ms at 106.1 s

## Physics probe

```
[PHYS-RESULT] {"verdict":"PASS","steps":9001,"physMaxMs":1.80,"physMaxAtS":111,"physAvgMs":0.203,"stepsOverBudget":0,"impacts":52,"peakAwakeBodies":4}

```

The verdict field inside this line is printed by the game's `PhysicsBudgetProbe`, not by this tool.

## Console

95 lines, 0 errors.

## Notes

- None.

## Files

- Raw frame deltas, long tasks and every `[PHYS]` line are in the matching `.json`.
- Screenshots (local only, gitignored): `builds/perf-captures/frametime-0.8.5-seed554-2026-10-05/`
