# tools/perf — WebGL frame-time capture

`webgl-frametime.mjs` measures frame times of the Unity WebGL build in Chrome and writes a report to
`production/qa/perf/`. It reports **observations only**. It never says pass or fail; whoever reads the
report applies the budget (for M1: no frame > 50 ms, p95 ≤ 33.3 ms, physics ≤ 4 ms).

Owner: Cursor (see `AGENTS.md`, lane `tools/perf/**`). It never edits `unity/**` and never runs Unity.

## Setup

Needs Node 20+ and Google Chrome installed (it drives the installed Chrome, not a downloaded Chromium,
so the real GPU is used).

```
cd tools/perf
npm install
```

## Run

From the repo root:

```
node tools/perf/webgl-frametime.mjs                 # 20 s title + 190 s run, seed 554, physProbe on
node tools/perf/webgl-frametime.mjs --seconds 30 --title-seconds 5 --out %TEMP%/perf-smoke   # quick check
node tools/perf/webgl-frametime.mjs --help
```

| Option | Default | Meaning |
|---|---|---|
| `--seconds N` | 190 | Run-phase length. `physProbe` warms up 5 s then measures 180 s, so its result lands at ~185 s |
| `--title-seconds N` | 20 | Record the title screen (it runs a demo storm) first; 0 skips it |
| `--seed N` | 554 | Sent as `?seed=N` |
| `--no-phys` | off | Leave out `&physProbe=1`. A normal run then ends at 90 s |
| `--build DIR` | `builds/webgl` | Build folder to snapshot and serve; a folder under `builds/perf-snapshot/` is served as is |
| `--url URL` | – | Measure an already-served build; no snapshot (add `--version`) |
| `--out DIR` | `production/qa/perf` | Report folder |
| `--width/--height` | 1280 × 800 | Viewport and canvas size |
| `--screenshot-every N` | 30 | Seconds between screenshots; 0 turns them off |
| `--headed` | off | Show the browser window |
| `--force` | off | Snapshot even though the build changed in the last 60 s |

## What it does

1. **Snapshot.** Reads the version from `productVersion` in `builds/webgl/index.html` and copies the build to
   `builds/perf-snapshot/<version>/`, so a rebuild during the capture can't affect it. It refuses to copy if
   any build file changed during a 5 s check or in the last 60 s (another agent may be mid-build); `--force`
   overrides this. An unchanged snapshot is reused. It checks the copy's file list and sizes, and the run
   fails if the player doesn't start within 120 s.
2. **Serve.** A local server on `127.0.0.1` (random port) serves the snapshot. Only the served `index.html` is
   altered in memory: it adds a timing script and CSS that sizes the canvas to 1280 × 800 (the template pins
   it at 960 × 600). Build files on disk are never changed.
3. **Title phase.** Focuses the canvas with `canvas.focus()` (no click, which would start a run early), waits
   until frames are steady (Unity stalls the main thread for a few seconds after load; up to 30 s), then
   records the attract mode.
4. **Run phase.** Presses Enter and, with `physProbe`, waits for the `[PHYS] start` line to confirm the run
   started. Then drives: hold W, tap a steering key for 0.6 s every 2 s (three A taps per D tap, so the truck loops around
   the map instead of leaving it), jump (Space) every 10 s, and back
   up (S) for 1.5 s every 20 s so the truck doesn't stay wedged against scenery. Holding full lock rolls the truck. With `physProbe` it keeps driving up to 15 s past
   `--seconds` until the final `[PHYS-RESULT]` line arrives.
5. **Measure.** `requestAnimationFrame` deltas and long tasks per phase, Web Audio activity (contexts,
   buffer sources started, peak active), the GPU string from `WEBGL_debug_renderer_info`, all console errors
   and every `[PHYS]` line.
6. **Machine load.** Records machine CPU busy % before launch and every 5 s during each phase, and the
   number of `Unity.exe` processes before, during (every 30 s) and after.

## Output

`production/qa/perf/frametime-<version>-seed<seed>-<date>.json` and a matching `.md`. A second run on the
same day gets `-2`, `-3` and so on.

- `.md`: setup, a title vs run table (frames, mean, p50, p95, p99, max, counts over 33.3 ms and 50 ms, long
  tasks, machine CPU, audio), the physics line, console errors and notes.
- `.json`: the same plus raw frame deltas and long-task durations per phase.
- Screenshots go to `builds/perf-captures/<report name>/` (gitignored; local only).

**Notes** in the report flag conditions that make numbers less trustworthy: a software GPU renderer,
`Unity.exe` running during the capture, high CPU load before launch, a suspended or missing audio context,
a canvas that isn't the requested size, a forced snapshot, a scene reload mid-run, or a missing physics result.

## Reading the numbers

- Chrome usually caps `requestAnimationFrame` at the display rate (60 Hz), so the mean sits near 16.7 ms.
  p95 and the counts show whether frames stay under budget, not how much headroom there is.
- The `verdict` field inside `[PHYS-RESULT]` is printed by the game's `PhysicsBudgetProbe`, not by this tool.
- Run phase frames can include a wreck and the results screen; check the notes and screenshots.
- `builds/perf-snapshot/` keeps one full build copy per version; delete old ones when disk space matters.
