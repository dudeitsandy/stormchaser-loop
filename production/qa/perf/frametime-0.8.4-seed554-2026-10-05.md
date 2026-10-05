# WebGL frame time: 0.8.4, seed 554

Generated 2026-10-05T18:39:29.066Z by `tools/perf/webgl-frametime.mjs`. Observations only; budgets are applied by the reader.

## Setup

- Build: `builds/perf-snapshot/0.8.4` (copied from `builds/webgl`)
- URL: `http://127.0.0.1:<port>/?seed=554&physProbe=1`
- Browser: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/154.0.0.0 Safari/537.36
- GPU: Google Inc. (AMD) / ANGLE (AMD, AMD Radeon(TM) 890M Graphics (0x0000150E) Direct3D11 vs_5_0 ps_5_0, D3D11)
- Canvas: 1280x800, headless
- Machine: AMD Ryzen AI 9 HX 370 w/ Radeon 890M, 24 logical cores, 31.1 GB, Windows_NT 10.0.26300
- Unity.exe processes: 0 before, up to 0 during, 0 after
- Machine CPU busy before launch: 9.3%

## Frame times (requestAnimationFrame deltas)

| | Title | Run |
|---|---|---|
| Duration (s) | 20 | 189.9 |
| Frames | 1190 | 11150 |
| Mean (ms) | 16.81 | 17.04 |
| p50 (ms) | 16.7 | 16.7 |
| p95 (ms) | 17.1 | 17.1 |
| p99 (ms) | 17.4 | 33.4 |
| Max (ms) | 33.9 | 50.1 |
| Frames > 33.3 ms | 6 | 128 |
| Frames > 50 ms | 0 | 1 |
| Long tasks (count / total ms) | 0 / 0 | 1 / 158 |
| Machine CPU busy (mean / max %) | 22 / 24 | 31.8 / 42.3 |
| Audio sources started / max active | 0 / 6 | 138 / 11 |

Worst run frames: 50.1 ms at 122.3 s, 49.9 ms at 93.8 s, 35.6 ms at 139 s, 34.7 ms at 104.5 s, 34 ms at 155.4 s

## Physics probe

```
[PHYS-RESULT] {"verdict":"PASS_ONE_SPIKE","steps":9001,"physMaxMs":4.50,"physMaxAtS":74,"physAvgMs":0.319,"stepsOverBudget":1,"impacts":46,"peakAwakeBodies":4}

```

The verdict field inside this line is printed by the game's `PhysicsBudgetProbe`, not by this tool.

## Console

95 lines, 0 errors.

## Notes

- None.

## Files

- Raw frame deltas, long tasks and every `[PHYS]` line are in the matching `.json`.
- Screenshots (local only, gitignored): `builds/perf-captures/frametime-0.8.4-seed554-2026-10-05/`
