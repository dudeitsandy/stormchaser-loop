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
| `--spike-log` | off | Add `&spikeLog=1` (builds with `FrameSpikeLog`) and match each `[SPIKE]` line to its slow frame |
| `--query K=V` | – | Extra URL parameter; repeat for more (e.g. `--query stormHarness=hold`) |
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
   overrides this. An unchanged snapshot is reused. A rebuild that keeps the same version number goes to
   `<version>-<build time>` (e.g. `0.8.5-20261005-1510`); existing snapshots are never overwritten. A folder
   under `builds/perf-snapshot/` passed to `--build` is served as is. It checks the copy's file list and sizes,
   and the run fails if the player doesn't start within 120 s.
2. **Serve.** A local server on `127.0.0.1` (random port) serves the snapshot. Only the served `index.html` is
   altered in memory: it adds a timing script and CSS that sizes the canvas to 1280 × 800 (the template pins
   it at 960 × 600). Build files on disk are never changed.
3. **Title phase.** Focuses the canvas with `canvas.focus()` (no click, which would start a run early), waits
   until frames are steady (Unity stalls the main thread for a few seconds after load; up to 30 s), then
   records the attract mode.
4. **Run phase.** Presses Enter and, with `physProbe`, waits for the `[PHYS] start` line to confirm the run
   started. Then drives: hold W, tap a steering key for 0.6 s every 2 s (three A taps per D tap, so the truck loops around
   the map instead of leaving it), coast (W released) for 0.75 s every 3 s to keep impact damage down, jump
   (Space) every 10 s, and back up (S) for 1.5 s every 20 s so the truck doesn't stay wedged against scenery.
   Holding full lock rolls the truck. A wreck still happens sometimes; the next key press retries it (the title is
   skipped) and the run carries on. With `physProbe` it keeps driving up to 15 s past
   `--seconds` until the final `[PHYS-RESULT]` line arrives.
5. **Measure.** `requestAnimationFrame` deltas and long tasks per phase, Web Audio activity (contexts,
   buffer sources started, peak active), the GPU string from `WEBGL_debug_renderer_info` and the whole console.
   `[PHYS]` and `[SPIKE]` lines are also stamped with the page clock so they line up with individual frames.
6. **Machine load.** Records machine CPU busy % before launch and every 5 s during each phase, and the
   number of `Unity.exe` processes before, during (every 30 s) and after.

## Output

`production/qa/perf/frametime-<version>-seed<seed>-<date>.json` and a matching `.md`. A second run on the
same day gets `-2`, `-3` and so on.

- `.md`: setup, a title vs run table (frames, mean, p50, p95, p99, max, counts over 33.3 ms and 50 ms, long
  tasks, machine CPU, audio), the physics line, console errors and notes.
- `.json`: the same plus raw frame deltas and long-task durations per phase, the stamped probe lines
  (`raw.probeLogs`) and the whole console (`console.all`).
- **Wrecks:** an early `PARTIAL` `[PHYS-RESULT]` line means the scene unloaded. The run phase then also gets
  "reloads excluded" stats, which leave out frames from 5 s before that line (wreck slow-mo and results screen)
  to 2 s after the next `[PHYS] start`. Both sets of numbers are reported; the window is listed in the notes.
- **Spike log** (`--spike-log`): for each run frame over 30 ms, the `[SPIKE]` line the game printed for it
  (within 100 ms): Unity's own frame time, GC count, heap change, game events and scene counts. The `.md`
  lists the 15 worst; `spikes.allSlowFrames` in the `.json` has all of them. A heavy frame with no spike line
  means Unity's own frame interval stayed under its threshold, i.e. the time went outside Unity's frame.
- Screenshots go to `builds/perf-captures/<report name>/` (gitignored; local only).

**Notes** in the report flag conditions that make numbers less trustworthy: a software GPU renderer,
`Unity.exe` running during the capture, high CPU load before launch, a suspended or missing audio context,
a canvas that isn't the requested size, a forced snapshot, a scene reload mid-run, or a missing physics result.

## Reading the numbers

- Chrome usually caps `requestAnimationFrame` at the display rate (60 Hz), so the mean sits near 16.7 ms.
  p95 and the counts show whether frames stay under budget, not how much headroom there is.
- The `verdict` field inside `[PHYS-RESULT]` is printed by the game's `PhysicsBudgetProbe`, not by this tool.
- The "reloads excluded" window is an estimate built from the physics probe's lines; check the notes and
  screenshots if a wreck lands near a frame you care about.
- `builds/perf-snapshot/` keeps one full build copy per version; delete old ones when disk space matters.
