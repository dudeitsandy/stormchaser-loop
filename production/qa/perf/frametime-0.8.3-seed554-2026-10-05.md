# WebGL frame time: 0.8.3, seed 554

Generated 2026-10-05T17:39:58.655Z by `tools/perf/webgl-frametime.mjs`. Observations only; budgets are applied by the reader.

## Setup

- Build: `builds/perf-snapshot/0.8.3` (existing snapshot, served as is)
- URL: `http://127.0.0.1:<port>/?seed=554&physProbe=1`
- Browser: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) HeadlessChrome/154.0.0.0 Safari/537.36
- GPU: Google Inc. (AMD) / ANGLE (AMD, AMD Radeon(TM) 890M Graphics (0x0000150E) Direct3D11 vs_5_0 ps_5_0, D3D11)
- Canvas: 1280x800, headless
- Machine: AMD Ryzen AI 9 HX 370 w/ Radeon 890M, 24 logical cores, 31.1 GB, Windows_NT 10.0.26300
- Unity.exe processes: 0 before, up to 0 during, 0 after
- Machine CPU busy before launch: 24.5%

## Frame times (requestAnimationFrame deltas)

| | Title | Run |
|---|---|---|
| Duration (s) | 20.1 | 190.1 |
| Frames | 1200 | 11258 |
| Mean (ms) | 16.74 | 16.88 |
| p50 (ms) | 16.7 | 16.7 |
| p95 (ms) | 17 | 17.1 |
| p99 (ms) | 17.2 | 33.1 |
| Max (ms) | 33.6 | 66.6 |
| Frames > 33.3 ms | 3 | 66 |
| Frames > 50 ms | 0 | 2 |
| Long tasks (count / total ms) | 0 / 0 | 0 / 0 |
| Machine CPU busy (mean / max %) | 41 / 53.8 | 32.2 / 49.6 |
| Audio sources started / max active | 0 / 8 | 80 / 14 |

Worst run frames: 66.6 ms at 175.4 s, 50.2 ms at 40.9 s, 33.8 ms at 67.3 s, 33.8 ms at 151.2 s, 33.8 ms at 160.5 s

## Physics probe

```
[PHYS-RESULT] {"verdict":"PASS","steps":9001,"physMaxMs":3.10,"physMaxAtS":11,"physAvgMs":0.287,"stepsOverBudget":0,"impacts":21,"peakAwakeBodies":2}

```

The verdict field inside this line is printed by the game's `PhysicsBudgetProbe`, not by this tool.

## Console

91 lines, 0 errors.

## Notes

- None from the tool.
- From the screenshots (added by hand): the truck is upright at 0–90 s and on its side at 120, 150 and 180 s,
  in a different place each time, so the last ~70 s were driven on its side without auto-righting. Storms stayed
  in range throughout (EF2 at 79 m and EF5 at 91 m at 120 s).

## Files

- Raw frame deltas, long tasks and every `[PHYS]` line are in the matching `.json`.
- Screenshots (local only, gitignored): `builds/perf-captures/frametime-0.8.3-seed554-2026-10-05/`
