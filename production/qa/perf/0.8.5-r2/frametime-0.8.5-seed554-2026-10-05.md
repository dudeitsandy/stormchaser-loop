# WebGL frame time: 0.8.5, seed 554

Generated 2026-10-05T19:57:38.200Z by `tools/perf/webgl-frametime.mjs`. Observations only; budgets are applied by the reader.

## Setup

- Build: `builds/perf-snapshot/0.8.5` (copied from `builds/webgl`, reused)
- URL: `http://127.0.0.1:<port>/?seed=554&physProbe=1`
- Browser: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/154.0.0.0 Safari/537.36
- GPU: Google Inc. (AMD) / ANGLE (AMD, AMD Radeon(TM) 890M Graphics (0x0000150E) Direct3D11 vs_5_0 ps_5_0, D3D11)
- Canvas: 1280x800, headless
- Machine: AMD Ryzen AI 9 HX 370 w/ Radeon 890M, 24 logical cores, 31.1 GB, Windows_NT 10.0.26300
- Unity.exe processes: 0 before, up to 0 during, 0 after
- Machine CPU busy before launch: 6.1%

## Frame times (requestAnimationFrame deltas)

| | Title | Run |
|---|---|---|
| Duration (s) | 20 | 190 |
| Frames | 1157 | 10997 |
| Mean (ms) | 17.31 | 17.28 |
| p50 (ms) | 16.7 | 16.7 |
| p95 (ms) | 17.1 | 17.1 |
| p99 (ms) | 33.5 | 33.5 |
| Max (ms) | 33.8 | 50 |
| Frames > 33.3 ms | 26 | 214 |
| Frames > 50 ms | 0 | 0 |
| Long tasks (count / total ms) | 0 / 0 | 1 / 148 |
| Machine CPU busy (mean / max %) | 10.5 / 12.4 | 10.8 / 14.8 |
| Audio sources started / max active | 0 / 5 | 106 / 9 |

Worst run frames: 50 ms at 138.1 s, 49.9 ms at 117.7 s, 49.9 ms at 183.9 s, 49.8 ms at 159.7 s, 34.5 ms at 91.7 s

## Physics probe

```
[PHYS-RESULT] {"verdict":"PASS","steps":9001,"physMaxMs":1.20,"physMaxAtS":11,"physAvgMs":0.218,"stepsOverBudget":0,"impacts":44,"peakAwakeBodies":3}

```

The verdict field inside this line is printed by the game's `PhysicsBudgetProbe`, not by this tool.

## Console

96 lines, 0 errors.

## Notes

- None.

## Files

- Raw frame deltas, long tasks and every `[PHYS]` line are in the matching `.json`.
- Screenshots (local only, gitignored): `builds/perf-captures/frametime-0.8.5-seed554-2026-10-05/`
